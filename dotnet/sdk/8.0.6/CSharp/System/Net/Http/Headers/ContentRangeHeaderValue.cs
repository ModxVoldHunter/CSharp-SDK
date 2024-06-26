using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Net.Http.Headers;

public class ContentRangeHeaderValue : ICloneable
{
	private string _unit;

	private long _from;

	private long _to;

	private long _length;

	public string Unit
	{
		get
		{
			return _unit;
		}
		set
		{
			HeaderUtilities.CheckValidToken(value, "value");
			_unit = value;
		}
	}

	public long? From
	{
		get
		{
			if (!HasRange)
			{
				return null;
			}
			return _from;
		}
	}

	public long? To
	{
		get
		{
			if (!HasRange)
			{
				return null;
			}
			return _to;
		}
	}

	public long? Length
	{
		get
		{
			if (!HasLength)
			{
				return null;
			}
			return _length;
		}
	}

	public bool HasLength => _length >= 0;

	public bool HasRange => _from >= 0;

	public ContentRangeHeaderValue(long from, long to, long length)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(length, "length");
		ArgumentOutOfRangeException.ThrowIfNegative(to, "to");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(to, length, "to");
		ArgumentOutOfRangeException.ThrowIfNegative(from, "from");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(from, to, "from");
		_from = from;
		_to = to;
		_length = length;
		_unit = "bytes";
	}

	public ContentRangeHeaderValue(long length)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(length, "length");
		_length = length;
		_unit = "bytes";
		_from = -1L;
	}

	public ContentRangeHeaderValue(long from, long to)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(to, "to");
		ArgumentOutOfRangeException.ThrowIfNegative(from, "from");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(from, to, "from");
		_from = from;
		_to = to;
		_unit = "bytes";
		_length = -1L;
	}

	private ContentRangeHeaderValue()
	{
		_from = -1L;
		_length = -1L;
	}

	private ContentRangeHeaderValue(ContentRangeHeaderValue source)
	{
		_from = source._from;
		_to = source._to;
		_length = source._length;
		_unit = source._unit;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is ContentRangeHeaderValue contentRangeHeaderValue && _from == contentRangeHeaderValue._from && _to == contentRangeHeaderValue._to && _length == contentRangeHeaderValue._length)
		{
			return string.Equals(_unit, contentRangeHeaderValue._unit, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(_unit), _from, _to, _length);
	}

	public override string ToString()
	{
		Span<char> initialBuffer = stackalloc char[256];
		System.Text.ValueStringBuilder valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		valueStringBuilder.Append(_unit);
		valueStringBuilder.Append(' ');
		if (HasRange)
		{
			valueStringBuilder.AppendSpanFormattable(_from);
			valueStringBuilder.Append('-');
			valueStringBuilder.AppendSpanFormattable(_to);
		}
		else
		{
			valueStringBuilder.Append('*');
		}
		valueStringBuilder.Append('/');
		if (HasLength)
		{
			valueStringBuilder.AppendSpanFormattable(_length);
		}
		else
		{
			valueStringBuilder.Append('*');
		}
		return valueStringBuilder.ToString();
	}

	public static ContentRangeHeaderValue Parse(string input)
	{
		int index = 0;
		return (ContentRangeHeaderValue)GenericHeaderParser.ContentRangeParser.ParseValue(input, null, ref index);
	}

	public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out ContentRangeHeaderValue? parsedValue)
	{
		int index = 0;
		parsedValue = null;
		if (GenericHeaderParser.ContentRangeParser.TryParseValue(input, null, ref index, out var parsedValue2))
		{
			parsedValue = (ContentRangeHeaderValue)parsedValue2;
			return true;
		}
		return false;
	}

	internal static int GetContentRangeLength(string input, int startIndex, out object parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
		{
			return 0;
		}
		int tokenLength = HttpRuleParser.GetTokenLength(input, startIndex);
		if (tokenLength == 0)
		{
			return 0;
		}
		string unit = input.Substring(startIndex, tokenLength);
		int num = startIndex + tokenLength;
		int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, num);
		if (whitespaceLength == 0)
		{
			return 0;
		}
		num += whitespaceLength;
		if (num == input.Length)
		{
			return 0;
		}
		int fromStartIndex = num;
		if (!TryGetRangeLength(input, ref num, out var fromLength, out var toStartIndex, out var toLength))
		{
			return 0;
		}
		if (num == input.Length || input[num] != '/')
		{
			return 0;
		}
		num++;
		num += HttpRuleParser.GetWhitespaceLength(input, num);
		if (num == input.Length)
		{
			return 0;
		}
		int lengthStartIndex = num;
		if (!TryGetLengthLength(input, ref num, out var lengthLength))
		{
			return 0;
		}
		if (!TryCreateContentRange(input, unit, fromStartIndex, fromLength, toStartIndex, toLength, lengthStartIndex, lengthLength, out parsedValue))
		{
			return 0;
		}
		return num - startIndex;
	}

	private static bool TryGetLengthLength(string input, ref int current, out int lengthLength)
	{
		lengthLength = 0;
		if (input[current] == '*')
		{
			current++;
		}
		else
		{
			lengthLength = HttpRuleParser.GetNumberLength(input, current, allowDecimal: false);
			if (lengthLength == 0 || lengthLength > 19)
			{
				return false;
			}
			current += lengthLength;
		}
		current += HttpRuleParser.GetWhitespaceLength(input, current);
		return true;
	}

	private static bool TryGetRangeLength(string input, ref int current, out int fromLength, out int toStartIndex, out int toLength)
	{
		fromLength = 0;
		toStartIndex = 0;
		toLength = 0;
		if (input[current] == '*')
		{
			current++;
		}
		else
		{
			fromLength = HttpRuleParser.GetNumberLength(input, current, allowDecimal: false);
			if (fromLength == 0 || fromLength > 19)
			{
				return false;
			}
			current += fromLength;
			current += HttpRuleParser.GetWhitespaceLength(input, current);
			if (current == input.Length || input[current] != '-')
			{
				return false;
			}
			current++;
			current += HttpRuleParser.GetWhitespaceLength(input, current);
			if (current == input.Length)
			{
				return false;
			}
			toStartIndex = current;
			toLength = HttpRuleParser.GetNumberLength(input, current, allowDecimal: false);
			if (toLength == 0 || toLength > 19)
			{
				return false;
			}
			current += toLength;
		}
		current += HttpRuleParser.GetWhitespaceLength(input, current);
		return true;
	}

	private static bool TryCreateContentRange(string input, string unit, int fromStartIndex, int fromLength, int toStartIndex, int toLength, int lengthStartIndex, int lengthLength, [NotNullWhen(true)] out object parsedValue)
	{
		parsedValue = null;
		long result = 0L;
		if (fromLength > 0 && !HeaderUtilities.TryParseInt64(input, fromStartIndex, fromLength, out result))
		{
			return false;
		}
		long result2 = 0L;
		if (toLength > 0 && !HeaderUtilities.TryParseInt64(input, toStartIndex, toLength, out result2))
		{
			return false;
		}
		if (fromLength > 0 && toLength > 0 && result > result2)
		{
			return false;
		}
		long result3 = 0L;
		if (lengthLength > 0 && !HeaderUtilities.TryParseInt64(input, lengthStartIndex, lengthLength, out result3))
		{
			return false;
		}
		if (toLength > 0 && lengthLength > 0 && result2 >= result3)
		{
			return false;
		}
		ContentRangeHeaderValue contentRangeHeaderValue = new ContentRangeHeaderValue();
		contentRangeHeaderValue._unit = unit;
		if (fromLength > 0)
		{
			contentRangeHeaderValue._from = result;
			contentRangeHeaderValue._to = result2;
		}
		if (lengthLength > 0)
		{
			contentRangeHeaderValue._length = result3;
		}
		parsedValue = contentRangeHeaderValue;
		return true;
	}

	object ICloneable.Clone()
	{
		return new ContentRangeHeaderValue(this);
	}
}
