using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading;

[DebuggerTypeProxy(typeof(SystemThreading_ThreadLocalDebugView<>))]
[DebuggerDisplay("IsValueCreated = {IsValueCreated}, Value = {ValueForDebugDisplay}, Count = {ValuesCountForDebugDisplay}")]
public class ThreadLocal<T> : IDisposable
{
	private struct LinkedSlotVolatile
	{
		internal volatile LinkedSlot Value;
	}

	private sealed class LinkedSlot
	{
		internal volatile LinkedSlot _next;

		internal volatile LinkedSlot _previous;

		internal volatile LinkedSlotVolatile[] _slotArray;

		internal T _value;

		internal LinkedSlot(LinkedSlotVolatile[] slotArray)
		{
			_slotArray = slotArray;
		}
	}

	private sealed class IdManager
	{
		private int _nextIdToTry;

		private volatile int _idsThatDoNotTrackAllValues;

		private readonly Dictionary<int, bool> _usedIdToTracksAllValuesMap = new Dictionary<int, bool>();

		private readonly List<int> _freeIds = new List<int>();

		internal int IdsThatDoNotTrackValuesCount => _idsThatDoNotTrackAllValues;

		internal int GetId(bool trackAllValues)
		{
			lock (_freeIds)
			{
				int count = _freeIds.Count;
				int num = ((count <= 0) ? _nextIdToTry : _freeIds[count - 1]);
				_freeIds.EnsureCapacity(_usedIdToTracksAllValuesMap.Count + 1);
				_usedIdToTracksAllValuesMap.Add(num, trackAllValues);
				if (count > 0)
				{
					_freeIds.RemoveAt(count - 1);
				}
				else
				{
					_nextIdToTry = num + 1;
				}
				if (!trackAllValues)
				{
					_idsThatDoNotTrackAllValues++;
				}
				return num;
			}
		}

		internal bool IdTracksAllValues(int id)
		{
			lock (_freeIds)
			{
				bool value;
				return _usedIdToTracksAllValuesMap.TryGetValue(id, out value) && value;
			}
		}

		internal void ReturnId(int id, bool idTracksAllValues)
		{
			lock (_freeIds)
			{
				if (!idTracksAllValues)
				{
					_idsThatDoNotTrackAllValues--;
				}
				_usedIdToTracksAllValuesMap.Remove(id);
				_freeIds.Add(id);
			}
		}
	}

	private sealed class FinalizationHelper
	{
		internal LinkedSlotVolatile[] SlotArray;

		internal FinalizationHelper(LinkedSlotVolatile[] slotArray)
		{
			SlotArray = slotArray;
		}

		~FinalizationHelper()
		{
			LinkedSlotVolatile[] slotArray = SlotArray;
			int num = ThreadLocal<T>.s_idManager.IdsThatDoNotTrackValuesCount;
			int i = 0;
			for (; i < slotArray.Length; i++)
			{
				LinkedSlot value = slotArray[i].Value;
				if (value == null)
				{
					continue;
				}
				if (num == 0 || ThreadLocal<T>.s_idManager.IdTracksAllValues(i))
				{
					value._slotArray = null;
					continue;
				}
				lock (ThreadLocal<T>.s_idManager)
				{
					if (slotArray[i].Value != null)
					{
						num--;
					}
					if (value._next != null)
					{
						value._next._previous = value._previous;
					}
					value._previous._next = value._next;
				}
			}
		}
	}

	private Func<T> _valueFactory;

	[ThreadStatic]
	private static LinkedSlotVolatile[] ts_slotArray;

	[ThreadStatic]
	private static FinalizationHelper ts_finalizationHelper;

	private int _idComplement;

	private volatile bool _initialized;

	private static readonly IdManager s_idManager = new IdManager();

	private LinkedSlot _linkedSlot = new LinkedSlot(null);

	private bool _trackAllValues;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public T Value
	{
		[return: MaybeNull]
		get
		{
			LinkedSlotVolatile[] array = ts_slotArray;
			int num = ~_idComplement;
			LinkedSlot value;
			if (array != null && num >= 0 && num < array.Length && (value = array[num].Value) != null && _initialized)
			{
				return value._value;
			}
			return GetValueSlow();
		}
		set
		{
			LinkedSlotVolatile[] array = ts_slotArray;
			int num = ~_idComplement;
			LinkedSlot value2;
			if (array != null && num >= 0 && num < array.Length && (value2 = array[num].Value) != null && _initialized)
			{
				value2._value = value;
			}
			else
			{
				SetValueSlow(value, array);
			}
		}
	}

	public IList<T> Values
	{
		get
		{
			if (!_trackAllValues)
			{
				throw new InvalidOperationException(SR.ThreadLocal_ValuesNotAvailable);
			}
			List<T> valuesAsList = GetValuesAsList();
			ObjectDisposedException.ThrowIf(valuesAsList == null, this);
			return valuesAsList;
		}
	}

	private int ValuesCountForDebugDisplay
	{
		get
		{
			int num = 0;
			LinkedSlot linkedSlot = _linkedSlot;
			for (LinkedSlot linkedSlot2 = ((linkedSlot != null) ? linkedSlot._next : null); linkedSlot2 != null; linkedSlot2 = linkedSlot2._next)
			{
				num++;
			}
			return num;
		}
	}

	public bool IsValueCreated
	{
		get
		{
			int num = ~_idComplement;
			ObjectDisposedException.ThrowIf(num < 0, this);
			LinkedSlotVolatile[] array = ts_slotArray;
			if (array != null && num < array.Length)
			{
				return array[num].Value != null;
			}
			return false;
		}
	}

	internal T? ValueForDebugDisplay
	{
		get
		{
			LinkedSlotVolatile[] array = ts_slotArray;
			int num = ~_idComplement;
			LinkedSlot value;
			if (array == null || num >= array.Length || (value = array[num].Value) == null || !_initialized)
			{
				return default(T);
			}
			return value._value;
		}
	}

	internal List<T>? ValuesForDebugDisplay => GetValuesAsList();

	public ThreadLocal()
	{
		Initialize(null, trackAllValues: false);
	}

	public ThreadLocal(bool trackAllValues)
	{
		Initialize(null, trackAllValues);
	}

	public ThreadLocal(Func<T> valueFactory)
	{
		ArgumentNullException.ThrowIfNull(valueFactory, "valueFactory");
		Initialize(valueFactory, trackAllValues: false);
	}

	public ThreadLocal(Func<T> valueFactory, bool trackAllValues)
	{
		ArgumentNullException.ThrowIfNull(valueFactory, "valueFactory");
		Initialize(valueFactory, trackAllValues);
	}

	private void Initialize(Func<T> valueFactory, bool trackAllValues)
	{
		_valueFactory = valueFactory;
		_trackAllValues = trackAllValues;
		_idComplement = ~s_idManager.GetId(trackAllValues);
		_initialized = true;
	}

	~ThreadLocal()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		int num;
		lock (s_idManager)
		{
			num = ~_idComplement;
			_idComplement = 0;
			if (num < 0 || !_initialized)
			{
				return;
			}
			_initialized = false;
			for (LinkedSlot next = _linkedSlot._next; next != null; next = next._next)
			{
				LinkedSlotVolatile[] slotArray = next._slotArray;
				if (slotArray != null)
				{
					next._slotArray = null;
					slotArray[num].Value._value = default(T);
					slotArray[num].Value = null;
				}
			}
		}
		_linkedSlot = null;
		s_idManager.ReturnId(num, _trackAllValues);
	}

	public override string? ToString()
	{
		return Value.ToString();
	}

	private T GetValueSlow()
	{
		int num = ~_idComplement;
		ObjectDisposedException.ThrowIf(num < 0, this);
		Debugger.NotifyOfCrossThreadDependency();
		T val;
		if (_valueFactory == null)
		{
			val = default(T);
		}
		else
		{
			val = _valueFactory();
			if (IsValueCreated)
			{
				throw new InvalidOperationException(SR.ThreadLocal_Value_RecursiveCallsToValue);
			}
		}
		Value = val;
		return val;
	}

	private void SetValueSlow(T value, LinkedSlotVolatile[] slotArray)
	{
		int num = ~_idComplement;
		ObjectDisposedException.ThrowIf(num < 0, this);
		if (slotArray == null)
		{
			slotArray = new LinkedSlotVolatile[GetNewTableSize(num + 1)];
			ts_finalizationHelper = new FinalizationHelper(slotArray);
			ts_slotArray = slotArray;
		}
		if (num >= slotArray.Length)
		{
			GrowTable(ref slotArray, num + 1);
			ts_finalizationHelper.SlotArray = slotArray;
			ts_slotArray = slotArray;
		}
		if (slotArray[num].Value == null)
		{
			CreateLinkedSlot(slotArray, num, value);
			return;
		}
		LinkedSlot value2 = slotArray[num].Value;
		ObjectDisposedException.ThrowIf(!_initialized, this);
		value2._value = value;
	}

	private void CreateLinkedSlot(LinkedSlotVolatile[] slotArray, int id, T value)
	{
		LinkedSlot linkedSlot = new LinkedSlot(slotArray);
		lock (s_idManager)
		{
			ObjectDisposedException.ThrowIf(!_initialized, this);
			LinkedSlot linkedSlot2 = (linkedSlot._next = _linkedSlot._next);
			linkedSlot._previous = _linkedSlot;
			linkedSlot._value = value;
			if (linkedSlot2 != null)
			{
				linkedSlot2._previous = linkedSlot;
			}
			_linkedSlot._next = linkedSlot;
			slotArray[id].Value = linkedSlot;
		}
	}

	private List<T> GetValuesAsList()
	{
		LinkedSlot linkedSlot = _linkedSlot;
		int num = ~_idComplement;
		if (num == -1 || linkedSlot == null)
		{
			return null;
		}
		List<T> list = new List<T>();
		for (linkedSlot = linkedSlot._next; linkedSlot != null; linkedSlot = linkedSlot._next)
		{
			list.Add(linkedSlot._value);
		}
		return list;
	}

	private static void GrowTable(ref LinkedSlotVolatile[] table, int minLength)
	{
		int newTableSize = GetNewTableSize(minLength);
		LinkedSlotVolatile[] array = new LinkedSlotVolatile[newTableSize];
		lock (s_idManager)
		{
			for (int i = 0; i < table.Length; i++)
			{
				LinkedSlot value = table[i].Value;
				if (value != null && value._slotArray != null)
				{
					value._slotArray = array;
					array[i] = table[i];
				}
			}
		}
		table = array;
	}

	private static int GetNewTableSize(int minSize)
	{
		if ((long)(uint)minSize > 2147483591L)
		{
			return int.MaxValue;
		}
		int num = minSize;
		num--;
		num |= num >> 1;
		num |= num >> 2;
		num |= num >> 4;
		num |= num >> 8;
		num |= num >> 16;
		num++;
		if ((long)(uint)num > 2147483591L)
		{
			num = 2147483591;
		}
		return num;
	}
}
