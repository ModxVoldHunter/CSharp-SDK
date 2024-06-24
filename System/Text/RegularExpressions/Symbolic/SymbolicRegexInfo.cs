namespace System.Text.RegularExpressions.Symbolic;

internal readonly struct SymbolicRegexInfo : IEquatable<SymbolicRegexInfo>
{
	private readonly uint _info;

	public bool IsNullable => (_info & 1) != 0;

	public bool CanBeNullable => (_info & 8) != 0;

	public bool StartsWithLineAnchor => (_info & 2) != 0;

	public bool ContainsLineAnchor => (_info & 0x100) != 0;

	public bool StartsWithSomeAnchor => (_info & 0x20) != 0;

	public bool ContainsSomeAnchor => (_info & 0x10) != 0;

	public bool IsLazyLoop => (_info & 4) != 0;

	public bool IsHighPriorityNullable => (_info & 0x40) != 0;

	public bool ContainsEffect => (_info & 0x80) != 0;

	private SymbolicRegexInfo(uint i)
	{
		_info = i;
	}

	private static SymbolicRegexInfo Create(bool isAlwaysNullable = false, bool canBeNullable = false, bool startsWithLineAnchor = false, bool containsLineAnchor = false, bool startsWithSomeAnchor = false, bool containsSomeAnchor = false, bool isHighPriorityNullable = false, bool containsEffect = false)
	{
		return new SymbolicRegexInfo((isAlwaysNullable ? 1u : 0u) | (canBeNullable ? 8u : 0u) | (startsWithLineAnchor ? 2u : 0u) | (containsLineAnchor ? 256u : 0u) | (startsWithSomeAnchor ? 32u : 0u) | (containsSomeAnchor ? 16u : 0u) | (isHighPriorityNullable ? 64u : 0u) | (containsEffect ? 128u : 0u));
	}

	public static SymbolicRegexInfo Epsilon()
	{
		return Create(isAlwaysNullable: true, canBeNullable: true, startsWithLineAnchor: false, containsLineAnchor: false, startsWithSomeAnchor: false, containsSomeAnchor: false, isHighPriorityNullable: true);
	}

	public static SymbolicRegexInfo Anchor(bool isLineAnchor)
	{
		return Create(isAlwaysNullable: false, canBeNullable: true, isLineAnchor, isLineAnchor, startsWithSomeAnchor: true, containsSomeAnchor: true);
	}

	public static SymbolicRegexInfo Alternate(SymbolicRegexInfo left_info, SymbolicRegexInfo right_info)
	{
		return Create(left_info.IsNullable || right_info.IsNullable, left_info.CanBeNullable || right_info.CanBeNullable, left_info.StartsWithLineAnchor || right_info.StartsWithLineAnchor, left_info.ContainsLineAnchor || right_info.ContainsLineAnchor, left_info.StartsWithSomeAnchor || right_info.StartsWithSomeAnchor, left_info.ContainsSomeAnchor || right_info.ContainsSomeAnchor, left_info.IsHighPriorityNullable, left_info.ContainsEffect || right_info.ContainsEffect);
	}

	public static SymbolicRegexInfo Concat(SymbolicRegexInfo left_info, SymbolicRegexInfo right_info)
	{
		return Create(left_info.IsNullable && right_info.IsNullable, left_info.CanBeNullable && right_info.CanBeNullable, left_info.StartsWithLineAnchor || (left_info.CanBeNullable && right_info.StartsWithLineAnchor), left_info.ContainsLineAnchor || right_info.ContainsLineAnchor, left_info.StartsWithSomeAnchor || (left_info.CanBeNullable && right_info.StartsWithSomeAnchor), left_info.ContainsSomeAnchor || right_info.ContainsSomeAnchor, left_info.IsHighPriorityNullable && right_info.IsHighPriorityNullable, left_info.ContainsEffect || right_info.ContainsEffect);
	}

	public static SymbolicRegexInfo Loop(SymbolicRegexInfo body_info, int lowerBound, bool isLazy)
	{
		uint num = body_info._info;
		if (lowerBound == 0)
		{
			num |= 9u;
			if (isLazy)
			{
				num |= 0x40u;
			}
		}
		num = ((!isLazy) ? (num & 0xFFFFFFFBu) : (num | 4u));
		return new SymbolicRegexInfo(num);
	}

	public static SymbolicRegexInfo Effect(SymbolicRegexInfo childInfo)
	{
		return new SymbolicRegexInfo(childInfo._info | 0x80u);
	}

	public override bool Equals(object obj)
	{
		if (obj is SymbolicRegexInfo other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(SymbolicRegexInfo other)
	{
		return _info == other._info;
	}

	public override int GetHashCode()
	{
		return _info.GetHashCode();
	}
}
