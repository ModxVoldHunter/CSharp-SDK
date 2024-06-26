using System.Globalization;

namespace System.Drawing;

internal static class ColorConverterCommon
{
	public static Color ConvertFromString(string strValue, CultureInfo culture)
	{
		string text = strValue.Trim();
		if (text.Length == 0)
		{
			return Color.Empty;
		}
		if (ColorTable.TryGetNamedColor(text, out var result))
		{
			return result;
		}
		char c = culture.TextInfo.ListSeparator[0];
		if (!text.Contains(c))
		{
			if (text.Length >= 2 && (text[0] == '\'' || text[0] == '"') && text[0] == text[text.Length - 1])
			{
				string name = text.Substring(1, text.Length - 2);
				return Color.FromName(name);
			}
			if ((text.Length == 7 && text[0] == '#') || (text.Length == 8 && (text.StartsWith("0x") || text.StartsWith("0X"))) || (text.Length == 8 && (text.StartsWith("&h") || text.StartsWith("&H"))))
			{
				return PossibleKnownColor(Color.FromArgb(-16777216 | IntFromString(text, culture)));
			}
		}
		ReadOnlySpan<char> source = text;
		Span<Range> destination = stackalloc Range[5];
		switch (source.Split(destination, c))
		{
		case 1:
		{
			Range range = destination[0];
			return PossibleKnownColor(Color.FromArgb(IntFromString(source[range.Start..range.End], culture)));
		}
		case 3:
		{
			Range range = destination[0];
			int length2 = source.Length;
			int red2 = IntFromString(source[range.Start.GetOffset(length2)..range.End.GetOffset(length2)], culture);
			range = destination[1];
			int length = source.Length;
			int green2 = IntFromString(source[range.Start.GetOffset(length)..range.End.GetOffset(length)], culture);
			range = destination[2];
			return PossibleKnownColor(Color.FromArgb(red2, green2, IntFromString(source[range.Start..range.End], culture)));
		}
		case 4:
		{
			Range range = destination[0];
			int length = source.Length;
			int alpha = IntFromString(source[range.Start.GetOffset(length)..range.End.GetOffset(length)], culture);
			range = destination[1];
			int length2 = source.Length;
			int red = IntFromString(source[range.Start.GetOffset(length2)..range.End.GetOffset(length2)], culture);
			range = destination[2];
			length = source.Length;
			int green = IntFromString(source[range.Start.GetOffset(length)..range.End.GetOffset(length)], culture);
			range = destination[3];
			return PossibleKnownColor(Color.FromArgb(alpha, red, green, IntFromString(source[range.Start..range.End], culture)));
		}
		default:
			throw new ArgumentException(System.SR.Format(System.SR.InvalidColor, text));
		}
	}

	private static Color PossibleKnownColor(Color color)
	{
		int num = color.ToArgb();
		foreach (Color value in ColorTable.Colors.Values)
		{
			if (value.ToArgb() == num)
			{
				return value;
			}
		}
		return color;
	}

	private static int IntFromString(ReadOnlySpan<char> text, CultureInfo culture)
	{
		text = text.Trim();
		try
		{
			if (text[0] == '#')
			{
				return Convert.ToInt32(text.Slice(1).ToString(), 16);
			}
			if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || text.StartsWith("&h", StringComparison.OrdinalIgnoreCase))
			{
				return Convert.ToInt32(text.Slice(2).ToString(), 16);
			}
			NumberFormatInfo provider = (NumberFormatInfo)culture.GetFormat(typeof(NumberFormatInfo));
			return int.Parse(text, provider);
		}
		catch (Exception innerException)
		{
			throw new ArgumentException(System.SR.Format(System.SR.ConvertInvalidPrimitive, text.ToString(), "Int32"), innerException);
		}
	}
}
