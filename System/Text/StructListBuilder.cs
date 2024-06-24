using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text;

[DebuggerDisplay("Count = {_count}")]
internal struct StructListBuilder<T>
{
	private T[] _array;

	private int _count;

	public int Count => _count;

	public StructListBuilder()
	{
		_count = 0;
		_array = Array.Empty<T>();
	}

	public Span<T> AsSpan()
	{
		return _array.AsSpan(0, _count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(T item)
	{
		T[] array = _array;
		int count = _count;
		if ((uint)count < (uint)array.Length)
		{
			array[count] = item;
			_count = count + 1;
		}
		else
		{
			GrowAndAdd(item);
		}
	}

	public void Dispose()
	{
		if (_array != null)
		{
			ArrayPool<T>.Shared.Return(_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
			_array = null;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void GrowAndAdd(T item)
	{
		T[] array = _array;
		int minimumLength = ((array.Length == 0) ? 256 : (array.Length * 2));
		T[] array2 = (_array = ArrayPool<T>.Shared.Rent(minimumLength));
		Array.Copy(array, array2, _count);
		ArrayPool<T>.Shared.Return(array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
		array2[_count++] = item;
	}
}
