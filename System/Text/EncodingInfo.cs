using System.Diagnostics.CodeAnalysis;

namespace System.Text;

public sealed class EncodingInfo
{
	public int CodePage { get; }

	public string Name { get; }

	public string DisplayName { get; }

	internal EncodingProvider? Provider { get; }

	public EncodingInfo(EncodingProvider provider, int codePage, string name, string displayName)
		: this(codePage, name, displayName)
	{
		ArgumentNullException.ThrowIfNull(provider, "provider");
		ArgumentNullException.ThrowIfNull(name, "name");
		ArgumentNullException.ThrowIfNull(displayName, "displayName");
		Provider = provider;
	}

	internal EncodingInfo(int codePage, string name, string displayName)
	{
		CodePage = codePage;
		Name = name;
		DisplayName = displayName;
	}

	public Encoding GetEncoding()
	{
		return Provider?.GetEncoding(CodePage) ?? Encoding.GetEncoding(CodePage);
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is EncodingInfo encodingInfo)
		{
			return CodePage == encodingInfo.CodePage;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return CodePage;
	}
}
