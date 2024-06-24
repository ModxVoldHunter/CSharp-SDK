using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.Design.Serialization;

public readonly struct MemberRelationship : IEquatable<MemberRelationship>
{
	public static readonly MemberRelationship Empty;

	public bool IsEmpty => Owner == null;

	public MemberDescriptor Member { get; }

	public object? Owner { get; }

	public MemberRelationship(object owner, MemberDescriptor member)
	{
		ArgumentNullException.ThrowIfNull(owner, "owner");
		ArgumentNullException.ThrowIfNull(member, "member");
		Owner = owner;
		Member = member;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is MemberRelationship other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(MemberRelationship other)
	{
		if (other.Owner == Owner)
		{
			return other.Member == Member;
		}
		return false;
	}

	public override int GetHashCode()
	{
		if (Owner != null)
		{
			return Owner.GetHashCode() ^ Member.GetHashCode();
		}
		return base.GetHashCode();
	}

	public static bool operator ==(MemberRelationship left, MemberRelationship right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(MemberRelationship left, MemberRelationship right)
	{
		return !left.Equals(right);
	}
}
