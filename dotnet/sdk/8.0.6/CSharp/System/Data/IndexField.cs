using System.Diagnostics.CodeAnalysis;

namespace System.Data;

internal readonly struct IndexField : IEquatable<IndexField>
{
	public readonly DataColumn Column;

	public readonly bool IsDescending;

	internal IndexField(DataColumn column, bool isDescending)
	{
		Column = column;
		IsDescending = isDescending;
	}

	public static bool operator ==(IndexField if1, IndexField if2)
	{
		return if1.Equals(if2);
	}

	public override bool Equals([NotNullWhen(true)] object obj)
	{
		if (obj is IndexField other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(IndexField other)
	{
		if (Column == other.Column)
		{
			return IsDescending == other.IsDescending;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Column.GetHashCode() ^ IsDescending.GetHashCode();
	}
}
