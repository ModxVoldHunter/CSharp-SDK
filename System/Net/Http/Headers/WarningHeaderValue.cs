using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers;

public class WarningHeaderValue : ICloneable
{
	private readonly int _code;

	private readonly string _agent;

	private readonly string _text;

	private readonly DateTimeOffset _date;

	private readonly bool _dateHasValue;

	public int Code => _code;

	public string Agent => _agent;

	public string Text => _text;

	public DateTimeOffset? Date
	{
		get
		{
			if (!_dateHasValue)
			{
				return null;
			}
			return _date;
		}
	}

	public WarningHeaderValue(int code, string agent, string text)
	{
		CheckCode(code);
		CheckAgent(agent);
		HeaderUtilities.CheckValidQuotedString(text, "text");
		_code = code;
		_agent = agent;
		_text = text;
	}

	public WarningHeaderValue(int code, string agent, string text, DateTimeOffset date)
	{
		CheckCode(code);
		CheckAgent(agent);
		HeaderUtilities.CheckValidQuotedString(text, "text");
		_code = code;
		_agent = agent;
		_text = text;
		_date = date;
		_dateHasValue = true;
	}

	private WarningHeaderValue(WarningHeaderValue source)
	{
		_code = source._code;
		_agent = source._agent;
		_text = source._text;
		_date = source._date;
		_dateHasValue = source._dateHasValue;
	}

	public override string ToString()
	{
		Span<char> initialBuffer = stackalloc char[256];
		System.Text.ValueStringBuilder valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		valueStringBuilder.AppendSpanFormattable(_code, "000", NumberFormatInfo.InvariantInfo);
		valueStringBuilder.Append(' ');
		valueStringBuilder.Append(_agent);
		valueStringBuilder.Append(' ');
		valueStringBuilder.Append(_text);
		if (_dateHasValue)
		{
			valueStringBuilder.Append(" \"");
			valueStringBuilder.AppendSpanFormattable(_date, "r");
			valueStringBuilder.Append('"');
		}
		return valueStringBuilder.ToString();
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is WarningHeaderValue warningHeaderValue && _code == warningHeaderValue._code && string.Equals(_agent, warningHeaderValue._agent, StringComparison.OrdinalIgnoreCase) && string.Equals(_text, warningHeaderValue._text, StringComparison.Ordinal) && _dateHasValue == warningHeaderValue._dateHasValue)
		{
			return _date == warningHeaderValue._date;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(_code, StringComparer.OrdinalIgnoreCase.GetHashCode(_agent), _text, _dateHasValue, _date);
	}

	public static WarningHeaderValue Parse(string input)
	{
		int index = 0;
		return (WarningHeaderValue)GenericHeaderParser.SingleValueWarningParser.ParseValue(input, null, ref index);
	}

	public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out WarningHeaderValue? parsedValue)
	{
		int index = 0;
		parsedValue = null;
		if (GenericHeaderParser.SingleValueWarningParser.TryParseValue(input, null, ref index, out var parsedValue2))
		{
			parsedValue = (WarningHeaderValue)parsedValue2;
			return true;
		}
		return false;
	}

	internal static int GetWarningLength(string input, int startIndex, out object parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
		{
			return 0;
		}
		int current = startIndex;
		if (!TryReadCode(input, ref current, out var code))
		{
			return 0;
		}
		if (!TryReadAgent(input, ref current, out var agent))
		{
			return 0;
		}
		int startIndex2 = current;
		if (HttpRuleParser.GetQuotedStringLength(input, current, out var length) != 0)
		{
			return 0;
		}
		string text = input.Substring(startIndex2, length);
		current += length;
		if (!TryReadDate(input, ref current, out var date))
		{
			return 0;
		}
		parsedValue = ((!date.HasValue) ? new WarningHeaderValue(code, agent, text) : new WarningHeaderValue(code, agent, text, date.Value));
		return current - startIndex;
	}

	private static bool TryReadAgent(string input, ref int current, [NotNullWhen(true)] out string agent)
	{
		agent = null;
		int hostLength = HttpRuleParser.GetHostLength(input, current, allowToken: true);
		if (hostLength == 0)
		{
			return false;
		}
		agent = input.Substring(current, hostLength);
		current += hostLength;
		int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
		current += whitespaceLength;
		if (whitespaceLength == 0 || current == input.Length)
		{
			return false;
		}
		return true;
	}

	private static bool TryReadCode(string input, ref int current, out int code)
	{
		code = 0;
		int numberLength = HttpRuleParser.GetNumberLength(input, current, allowDecimal: false);
		if (numberLength == 0 || numberLength > 3)
		{
			return false;
		}
		if (!HeaderUtilities.TryParseInt32(input, current, numberLength, out code))
		{
			return false;
		}
		current += numberLength;
		int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
		current += whitespaceLength;
		if (whitespaceLength == 0 || current == input.Length)
		{
			return false;
		}
		return true;
	}

	private static bool TryReadDate(string input, ref int current, out DateTimeOffset? date)
	{
		date = null;
		int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
		current += whitespaceLength;
		if (current < input.Length && input[current] == '"')
		{
			if (whitespaceLength == 0)
			{
				return false;
			}
			current++;
			int num = current;
			int num2 = input.AsSpan(current).IndexOf('"');
			if (num2 <= 0)
			{
				return false;
			}
			current += num2;
			if (!HttpDateParser.TryParse(input.AsSpan(num, current - num), out var result))
			{
				return false;
			}
			date = result;
			current++;
			current += HttpRuleParser.GetWhitespaceLength(input, current);
		}
		return true;
	}

	object ICloneable.Clone()
	{
		return new WarningHeaderValue(this);
	}

	private static void CheckCode(int code)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(code, "code");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(code, 999, "code");
	}

	private static void CheckAgent(string agent)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(agent, "agent");
		if (HttpRuleParser.GetHostLength(agent, 0, allowToken: true) != agent.Length)
		{
			throw new FormatException(System.SR.Format(System.SR.net_http_headers_invalid_value, agent));
		}
	}
}
