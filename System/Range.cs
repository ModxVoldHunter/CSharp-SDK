using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

public readonly struct Range : IEquatable<Range>
{
	public Index Start { get; }

	public Index End { get; }

	public static Range All => Index.Start..Index.End;

	public Range(Index start, Index end)
	{
		Start = start;
		End = end;
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is Range { Start: var start } range && start.Equals(Start))
		{
			return range.End.Equals(End);
		}
		return false;
	}

	public bool Equals(Range other)
	{
		if (other.Start.Equals(Start))
		{
			return other.End.Equals(End);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Start.GetHashCode(), End.GetHashCode());
	}

	public override string ToString()
	{
		Span<char> span = stackalloc char[24];
		int num = 0;
		if (Start.IsFromEnd)
		{
			span[0] = '^';
			num = 1;
		}
		bool flag = ((uint)Start.Value).TryFormat(span.Slice(num), out var charsWritten);
		num += charsWritten;
		span[num++] = '.';
		span[num++] = '.';
		if (End.IsFromEnd)
		{
			span[num++] = '^';
		}
		flag = ((uint)End.Value).TryFormat(span.Slice(num), out charsWritten);
		return new string(span[..(num + charsWritten)]);
	}

	public static Range StartAt(Index start)
	{
		return start..Index.End;
	}

	public static Range EndAt(Index end)
	{
		return Index.Start..end;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public (int Offset, int Length) GetOffsetAndLength(int length)
	{
		int offset = Start.GetOffset(length);
		int offset2 = End.GetOffset(length);
		if ((uint)offset2 > (uint)length || (uint)offset > (uint)offset2)
		{
			ThrowArgumentOutOfRangeException();
		}
		return (Offset: offset, Length: offset2 - offset);
	}

	private static void ThrowArgumentOutOfRangeException()
	{
		throw new ArgumentOutOfRangeException("length");
	}
}
