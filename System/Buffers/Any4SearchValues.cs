using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers;

internal sealed class Any4SearchValues<T, TImpl> : SearchValues<T> where T : struct, IEquatable<T> where TImpl : struct, INumber<TImpl>
{
	private readonly TImpl _e0;

	private readonly TImpl _e1;

	private readonly TImpl _e2;

	private readonly TImpl _e3;

	public Any4SearchValues(ReadOnlySpan<TImpl> values)
	{
		TImpl e = values[0];
		TImpl e2 = values[1];
		TImpl e3 = values[2];
		TImpl e4 = values[3];
		_e0 = e;
		_e1 = e2;
		_e2 = e3;
		_e3 = e4;
	}

	internal unsafe override T[] GetValues()
	{
		TImpl e = _e0;
		TImpl e2 = _e1;
		TImpl e3 = _e2;
		TImpl e4 = _e3;
		return new T[4]
		{
			Unsafe.Read<T>(&e),
			Unsafe.Read<T>(&e2),
			Unsafe.Read<T>(&e3),
			Unsafe.Read<T>(&e4)
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe override bool ContainsCore(T value)
	{
		if (!(Unsafe.Read<TImpl?>(&value) == _e0) && !(Unsafe.Read<TImpl?>(&value) == _e1) && !(Unsafe.Read<TImpl?>(&value) == _e2))
		{
			return Unsafe.Read<TImpl?>(&value) == _e3;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int IndexOfAny(ReadOnlySpan<T> span)
	{
		return SpanHelpers.IndexOfAnyValueType(ref Unsafe.As<T, TImpl>(ref MemoryMarshal.GetReference(span)), _e0, _e1, _e2, _e3, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int IndexOfAnyExcept(ReadOnlySpan<T> span)
	{
		return SpanHelpers.IndexOfAnyExceptValueType(ref Unsafe.As<T, TImpl>(ref MemoryMarshal.GetReference(span)), _e0, _e1, _e2, _e3, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int LastIndexOfAny(ReadOnlySpan<T> span)
	{
		return SpanHelpers.LastIndexOfAnyValueType(ref Unsafe.As<T, TImpl>(ref MemoryMarshal.GetReference(span)), _e0, _e1, _e2, _e3, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int LastIndexOfAnyExcept(ReadOnlySpan<T> span)
	{
		return SpanHelpers.LastIndexOfAnyExceptValueType(ref Unsafe.As<T, TImpl>(ref MemoryMarshal.GetReference(span)), _e0, _e1, _e2, _e3, span.Length);
	}
}
