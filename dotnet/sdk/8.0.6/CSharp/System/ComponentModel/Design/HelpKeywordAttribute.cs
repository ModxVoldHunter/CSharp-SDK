using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.Design;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
public sealed class HelpKeywordAttribute : Attribute
{
	public static readonly HelpKeywordAttribute Default = new HelpKeywordAttribute();

	public string? HelpKeyword { get; }

	public HelpKeywordAttribute()
	{
	}

	public HelpKeywordAttribute(string keyword)
	{
		ArgumentNullException.ThrowIfNull(keyword, "keyword");
		HelpKeyword = keyword;
	}

	public HelpKeywordAttribute(Type t)
	{
		ArgumentNullException.ThrowIfNull(t, "t");
		HelpKeyword = t.FullName;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (obj != null && obj is HelpKeywordAttribute)
		{
			return ((HelpKeywordAttribute)obj).HelpKeyword == HelpKeyword;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool IsDefaultAttribute()
	{
		return Equals(Default);
	}
}
