using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http.Headers;

public class RangeConditionHeaderValue : ICloneable
{
	private readonly DateTimeOffset _date;

	private readonly EntityTagHeaderValue _entityTag;

	public DateTimeOffset? Date
	{
		get
		{
			if (_entityTag != null)
			{
				return null;
			}
			return _date;
		}
	}

	public EntityTagHeaderValue? EntityTag => _entityTag;

	public RangeConditionHeaderValue(DateTimeOffset date)
	{
		_date = date;
	}

	public RangeConditionHeaderValue(EntityTagHeaderValue entityTag)
	{
		ArgumentNullException.ThrowIfNull(entityTag, "entityTag");
		_entityTag = entityTag;
	}

	public RangeConditionHeaderValue(string entityTag)
		: this(new EntityTagHeaderValue(entityTag))
	{
	}

	private RangeConditionHeaderValue(RangeConditionHeaderValue source)
	{
		_entityTag = source._entityTag;
		_date = source._date;
	}

	public override string ToString()
	{
		return _entityTag?.ToString() ?? _date.ToString("r");
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is RangeConditionHeaderValue rangeConditionHeaderValue && ((_entityTag == null) ? (rangeConditionHeaderValue._entityTag == null) : _entityTag.Equals(rangeConditionHeaderValue._entityTag)))
		{
			return _date == rangeConditionHeaderValue._date;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _entityTag?.GetHashCode() ?? _date.GetHashCode();
	}

	public static RangeConditionHeaderValue Parse(string input)
	{
		int index = 0;
		return (RangeConditionHeaderValue)GenericHeaderParser.RangeConditionParser.ParseValue(input, null, ref index);
	}

	public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out RangeConditionHeaderValue? parsedValue)
	{
		int index = 0;
		parsedValue = null;
		if (GenericHeaderParser.RangeConditionParser.TryParseValue(input, null, ref index, out var parsedValue2))
		{
			parsedValue = (RangeConditionHeaderValue)parsedValue2;
			return true;
		}
		return false;
	}

	internal static int GetRangeConditionLength(string input, int startIndex, out object parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(input) || startIndex + 1 >= input.Length)
		{
			return 0;
		}
		int num = startIndex;
		DateTimeOffset result = DateTimeOffset.MinValue;
		EntityTagHeaderValue parsedValue2 = null;
		char c = input[num];
		char c2 = input[num + 1];
		if (c == '"' || ((c == 'w' || c == 'W') && c2 == '/'))
		{
			int entityTagLength = EntityTagHeaderValue.GetEntityTagLength(input, num, out parsedValue2);
			if (entityTagLength == 0)
			{
				return 0;
			}
			num += entityTagLength;
			if (num != input.Length)
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
		if (parsedValue2 == null)
		{
			parsedValue = new RangeConditionHeaderValue(result);
		}
		else
		{
			parsedValue = new RangeConditionHeaderValue(parsedValue2);
		}
		return num - startIndex;
	}

	object ICloneable.Clone()
	{
		return new RangeConditionHeaderValue(this);
	}
}
