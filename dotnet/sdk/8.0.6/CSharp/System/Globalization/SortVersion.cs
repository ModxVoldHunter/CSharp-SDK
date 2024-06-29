using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Globalization;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class SortVersion : IEquatable<SortVersion?>
{
	private readonly int m_NlsVersion;

	private readonly Guid m_SortId;

	public int FullVersion => m_NlsVersion;

	public Guid SortId => m_SortId;

	public SortVersion(int fullVersion, Guid sortId)
	{
		m_SortId = sortId;
		m_NlsVersion = fullVersion;
	}

	internal SortVersion(int nlsVersion, int effectiveId, Guid customVersion)
	{
		m_NlsVersion = nlsVersion;
		if (customVersion == Guid.Empty)
		{
			byte h = (byte)(effectiveId >> 24);
			byte i = (byte)((effectiveId & 0xFF0000) >> 16);
			byte j = (byte)((effectiveId & 0xFF00) >> 8);
			byte k = (byte)((uint)effectiveId & 0xFFu);
			customVersion = new Guid(0, 0, 0, 0, 0, 0, 0, h, i, j, k);
		}
		m_SortId = customVersion;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is SortVersion other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals([NotNullWhen(true)] SortVersion? other)
	{
		if (other == null)
		{
			return false;
		}
		if (m_NlsVersion == other.m_NlsVersion)
		{
			return m_SortId == other.m_SortId;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (m_NlsVersion * 7) | m_SortId.GetHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(SortVersion? left, SortVersion? right)
	{
		return right?.Equals(left) ?? ((object)left == null);
	}

	public static bool operator !=(SortVersion? left, SortVersion? right)
	{
		return !(left == right);
	}
}
