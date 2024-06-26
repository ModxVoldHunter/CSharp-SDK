using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Threading;

internal sealed class ThreadInt64PersistentCounter
{
	private sealed class ThreadLocalNode
	{
		private uint _count;

		private readonly ThreadInt64PersistentCounter _counter;

		internal ThreadLocalNode _prev;

		internal ThreadLocalNode _next;

		public uint Count => _count;

		public ThreadLocalNode(ThreadInt64PersistentCounter counter)
		{
			_counter = counter;
			_prev = this;
			_next = this;
		}

		public void Dispose()
		{
			ThreadInt64PersistentCounter counter = _counter;
			counter._lock.Acquire();
			try
			{
				counter._overflowCount += _count;
				_prev._next = _next;
				_next._prev = _prev;
			}
			finally
			{
				counter._lock.Release();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Increment()
		{
			uint num = _count + 1;
			if (num != 0)
			{
				_count = num;
			}
			else
			{
				OnAddOverflow(1u);
			}
		}

		public void Add(uint count)
		{
			uint num = _count + count;
			if (num >= count)
			{
				_count = num;
			}
			else
			{
				OnAddOverflow(count);
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnAddOverflow(uint count)
		{
			ThreadInt64PersistentCounter counter = _counter;
			counter._lock.Acquire();
			try
			{
				counter._overflowCount += (long)_count + (long)count;
				_count = 0u;
			}
			finally
			{
				counter._lock.Release();
			}
		}
	}

	private sealed class ThreadLocalNodeFinalizationHelper
	{
		private readonly ThreadLocalNode _node;

		public ThreadLocalNodeFinalizationHelper(ThreadLocalNode node)
		{
			_node = node;
		}

		~ThreadLocalNodeFinalizationHelper()
		{
			_node.Dispose();
		}
	}

	private readonly LowLevelLock _lock = new LowLevelLock();

	[ThreadStatic]
	private static List<ThreadLocalNodeFinalizationHelper> t_nodeFinalizationHelpers;

	private long _overflowCount;

	private readonly ThreadLocalNode _nodes;

	public long Count
	{
		get
		{
			_lock.Acquire();
			long num = _overflowCount;
			try
			{
				ThreadLocalNode nodes = _nodes;
				for (ThreadLocalNode next = nodes._next; next != nodes; next = next._next)
				{
					num += next.Count;
				}
				return num;
			}
			finally
			{
				_lock.Release();
			}
		}
	}

	public ThreadInt64PersistentCounter()
	{
		_nodes = new ThreadLocalNode(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Increment(object threadLocalCountObject)
	{
		Unsafe.As<ThreadLocalNode>(threadLocalCountObject).Increment();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Add(object threadLocalCountObject, uint count)
	{
		Unsafe.As<ThreadLocalNode>(threadLocalCountObject).Add(count);
	}

	public object CreateThreadLocalCountObject()
	{
		ThreadLocalNode threadLocalNode = new ThreadLocalNode(this);
		List<ThreadLocalNodeFinalizationHelper> list = t_nodeFinalizationHelpers ?? (t_nodeFinalizationHelpers = new List<ThreadLocalNodeFinalizationHelper>(1));
		list.Add(new ThreadLocalNodeFinalizationHelper(threadLocalNode));
		_lock.Acquire();
		try
		{
			threadLocalNode._next = _nodes._next;
			threadLocalNode._prev = _nodes;
			_nodes._next._prev = threadLocalNode;
			_nodes._next = threadLocalNode;
			return threadLocalNode;
		}
		finally
		{
			_lock.Release();
		}
	}
}
