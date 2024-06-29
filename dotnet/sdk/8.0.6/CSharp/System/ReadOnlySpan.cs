using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;

namespace System;

[DebuggerTypeProxy(typeof(SpanDebugView<>))]
[DebuggerDisplay("{ToString(),raw}")]
[NonVersionable]
[NativeMarshalling(typeof(ReadOnlySpanMarshaller<, >))]
public readonly ref struct ReadOnlySpan<T>
{
	public ref struct Enumerator
	{
		private readonly ReadOnlySpan<T> _span;

		private int _index;

		public ref readonly T Current
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return ref _span[_index];
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Enumerator(ReadOnlySpan<T> span)
		{
			_span = span;
			_index = -1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool MoveNext()
		{
			int num = _index + 1;
			if (num < _span.Length)
			{
				_index = num;
				return true;
			}
			return false;
		}
	}

	internal readonly ref T _reference;

	private readonly int _length;

	public ref readonly T this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		[NonVersionable]
		get
		{
			if ((uint)index >= (uint)_length)
			{
				ThrowHelper.ThrowIndexOutOfRangeException();
			}
			return ref Unsafe.Add(ref _reference, (nint)(uint)index);
		}
	}

	public int Length
	{
		[Intrinsic]
		[NonVersionable]
		get
		{
			return _length;
		}
	}

	public bool IsEmpty
	{
		[NonVersionable]
		get
		{
			return _length == 0;
		}
	}

	public static ReadOnlySpan<T> Empty => default(ReadOnlySpan<T>);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan(T[]? array)
	{
		if (array == null)
		{
			this = default(ReadOnlySpan<T>);
			return;
		}
		_reference = ref MemoryMarshal.GetArrayDataReference(array);
		_length = array.Length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan(T[]? array, int start, int length)
	{
		if (array == null)
		{
			if (start != 0 || length != 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException();
			}
			this = default(ReadOnlySpan<T>);
			return;
		}
		if ((ulong)((long)(uint)start + (long)(uint)length) > (ulong)(uint)array.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		_reference = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)start);
		_length = length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public unsafe ReadOnlySpan(void* pointer, int length)
	{
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
		}
		if (length < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		_reference = ref Unsafe.As<byte, T>(ref *(byte*)pointer);
		_length = length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan([In][RequiresLocation] ref T reference)
	{
		_reference = ref Unsafe.AsRef(ref reference);
		_length = 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ReadOnlySpan(ref T reference, int length)
	{
		_reference = ref reference;
		_length = length;
	}

	public static bool operator !=(ReadOnlySpan<T> left, ReadOnlySpan<T> right)
	{
		return !(left == right);
	}

	[Obsolete("Equals() on ReadOnlySpan will always throw an exception. Use the equality operator instead.")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override bool Equals(object? obj)
	{
		throw new NotSupportedException(SR.NotSupported_CannotCallEqualsOnSpan);
	}

	[Obsolete("GetHashCode() on ReadOnlySpan will always throw an exception.")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override int GetHashCode()
	{
		throw new NotSupportedException(SR.NotSupported_CannotCallGetHashCodeOnSpan);
	}

	public static implicit operator ReadOnlySpan<T>(T[]? array)
	{
		return new ReadOnlySpan<T>(array);
	}

	public static implicit operator ReadOnlySpan<T>(ArraySegment<T> segment)
	{
		return new ReadOnlySpan<T>(segment.Array, segment.Offset, segment.Count);
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public ref readonly T GetPinnableReference()
	{
		ref T result = ref Unsafe.NullRef<T>();
		if (_length != 0)
		{
			result = ref _reference;
		}
		return ref result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyTo(Span<T> destination)
	{
		if ((uint)_length <= (uint)destination.Length)
		{
			Buffer.Memmove<T>(ref destination._reference, ref _reference, (uint)_length);
		}
		else
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
	}

	public bool TryCopyTo(Span<T> destination)
	{
		bool result = false;
		if ((uint)_length <= (uint)destination.Length)
		{
			Buffer.Memmove<T>(ref destination._reference, ref _reference, (uint)_length);
			result = true;
		}
		return result;
	}

	public static bool operator ==(ReadOnlySpan<T> left, ReadOnlySpan<T> right)
	{
		if (left._length == right._length)
		{
			return Unsafe.AreSame(ref left._reference, ref right._reference);
		}
		return false;
	}

	public override string ToString()
	{
		if (typeof(T) == typeof(char))
		{
			return new string(new ReadOnlySpan<char>(ref Unsafe.As<T, char>(ref _reference), _length));
		}
		return $"System.ReadOnlySpan<{typeof(T).Name}>[{_length}]";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<T> Slice(int start)
	{
		if ((uint)start > (uint)_length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		return new ReadOnlySpan<T>(ref Unsafe.Add(ref _reference, (nint)(uint)start), _length - start);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<T> Slice(int start, int length)
	{
		if ((ulong)((long)(uint)start + (long)(uint)length) > (ulong)(uint)_length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		return new ReadOnlySpan<T>(ref Unsafe.Add(ref _reference, (nint)(uint)start), length);
	}

	public T[] ToArray()
	{
		if (_length == 0)
		{
			return Array.Empty<T>();
		}
		T[] array = new T[_length];
		Buffer.Memmove(ref MemoryMarshal.GetArrayDataReference(array), ref _reference, (uint)_length);
		return array;
	}
}
