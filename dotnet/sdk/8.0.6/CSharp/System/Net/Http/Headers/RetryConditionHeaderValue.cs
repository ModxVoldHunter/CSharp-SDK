using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Net.Http.Headers;

public class RetryConditionHeaderValue : ICloneable
{
	private readonly DateTimeOffset _date;

	private readonly TimeSpan _delta;

	public DateTimeOffset? Date
	{
		get
		{
			if (_delta.Ticks != long.MaxValue)
			{
				return null;
			}
			return _date;
		}
	}

	public TimeSpan? Delta
	{
		get
		{
			if (_delta.Ticks != long.MaxValue)
			{
				return _delta;
			}
			return null;
		}
	}

	public RetryConditionHeaderValue(DateTimeOffset date)
	{
		_date = date;
		_delta = new TimeSpan(long.MaxValue);
	}

	public RetryConditionHeaderValue(TimeSpan delta)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan(delta.TotalSeconds, 2147483647.0, "delta.TotalSeconds");
		_delta = delta;
	}

	private RetryConditionHeaderValue(RetryConditionHeaderValue source)
	{
		_delta = source._delta;
		_date = source._date;
	}

	public override string ToString()
	{
		if (_delta.Ticks == long.MaxValue)
		{
			return _date.ToString("r");
		}
		return ((int)_delta.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is RetryConditionHeaderValue retryConditionHeaderValue && _delta == retryConditionHeaderValue._delta)
		{
			return _date == retryConditionHeaderValue._date;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(_delta, _date);
	}

	public static RetryConditionHeaderValue Parse(string input)
	{
		int index = 0;
		return (RetryConditionHeaderValue)GenericHeaderParser.RetryConditionParser.ParseValue(input, null, ref index);
	}

	public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out RetryConditionHeaderValue? parsedValue)
	{
		int index = 0;
		parsedValue = null;
		if (GenericHeaderParser.RetryConditionParser.TryParseValue(input, null, ref index, out var parsedValue2))
		{
			parsedValue = (RetryConditionHeaderValue)parsedValue2;
			return true;
		}
		return false;
	}

	internal static int GetRetryConditionLength(string input, int startIndex, out object parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
		{
			return 0;
		}
		int num = startIndex;
		DateTimeOffset result = DateTimeOffset.MinValue;
		int result2 = -1;
		char c = input[num];
		if (char.IsAsciiDigit(c))
		{
			int offset = num;
			int numberLength = HttpRuleParser.GetNumberLength(input, num, allowDecimal: false);
			if (numberLength == 0 || numberLength > 10)
			{
				return 0;
			}
			num += numberLength;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			if (num != input.Length)
			{
				return 0;
			}
			if (!HeaderUtilities.TryParseInt32(input, offset, numberLength, out result2))
			{
				return 0;
			}
		}
		else
		{
			if (!HttpDateParser.TryParse(input.AsSpan(num), out result))
			{
				return 0;
			}
			num = input.Length;
		}
		if (result2 == -1)
		{
			parsedValue = new RetryConditionHeaderValue(result);
		}
		else
		{
			parsedValue = new RetryConditionHeaderValue(new TimeSpan(0, 0, result2));
		}
		return num - startIndex;
	}

	object ICloneable.Clone()
	{
		return new RetryConditionHeaderValue(this);
	}
}
