using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Net.Http.Headers;

internal sealed class UriHeaderParser : HttpHeaderParser
{
	private readonly UriKind _uriKind;

	internal static readonly UriHeaderParser RelativeOrAbsoluteUriParser = new UriHeaderParser(UriKind.RelativeOrAbsolute);

	private UriHeaderParser(UriKind uriKind)
		: base(supportsMultipleValues: false)
	{
		_uriKind = uriKind;
	}

	public override bool TryParseValue([NotNullWhen(true)] string value, object storeValue, ref int index, [NotNullWhen(true)] out object parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(value) || index == value.Length)
		{
			return false;
		}
		string text = value;
		if (index > 0)
		{
			text = value.Substring(index);
		}
		if (!Uri.TryCreate(text, _uriKind, out Uri result))
		{
			text = DecodeUtf8FromString(text);
			if (!Uri.TryCreate(text, _uriKind, out result))
			{
				return false;
			}
		}
		index = value.Length;
		parsedValue = result;
		return true;
	}

	internal static string DecodeUtf8FromString(string input)
	{
		if (!string.IsNullOrWhiteSpace(input))
		{
			int num = input.AsSpan().IndexOfAnyExceptInRange('\0', '\u007f');
			if (num >= 0 && !input.AsSpan(num).ContainsAnyExceptInRange('\0', 'ÿ'))
			{
				Span<byte> span = ((input.Length > 256) ? ((Span<byte>)new byte[input.Length]) : stackalloc byte[input.Length]);
				Span<byte> span2 = span;
				for (int i = 0; i < input.Length; i++)
				{
					span2[i] = (byte)input[i];
				}
				try
				{
					Encoding encoding = Encoding.GetEncoding("utf-8", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
					return encoding.GetString(span2);
				}
				catch (ArgumentException)
				{
				}
			}
		}
		return input;
	}

	public override string ToString(object value)
	{
		Uri uri = (Uri)value;
		if (uri.IsAbsoluteUri)
		{
			return uri.AbsoluteUri;
		}
		return uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
	}
}
