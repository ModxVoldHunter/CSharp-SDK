using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http.Headers;

internal sealed class CookieHeaderParser : HttpHeaderParser
{
	internal static readonly CookieHeaderParser Parser = new CookieHeaderParser();

	private CookieHeaderParser()
		: base(supportsMultipleValues: true, "; ")
	{
	}

	public override bool TryParseValue(string value, object storeValue, ref int index, [NotNullWhen(true)] out object parsedValue)
	{
		if (string.IsNullOrEmpty(value) || index == value.Length)
		{
			parsedValue = null;
			return false;
		}
		parsedValue = value;
		index = value.Length;
		return true;
	}
}
