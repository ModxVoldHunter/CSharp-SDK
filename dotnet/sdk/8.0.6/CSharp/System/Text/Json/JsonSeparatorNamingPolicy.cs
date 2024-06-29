using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Text.Json;

internal abstract class JsonSeparatorNamingPolicy : JsonNamingPolicy
{
	private enum SeparatorState
	{
		NotStarted,
		UppercaseLetter,
		LowercaseLetterOrDigit,
		SpaceSeparator
	}

	private readonly bool _lowercase;

	private readonly char _separator;

	internal JsonSeparatorNamingPolicy(bool lowercase, char separator)
	{
		_lowercase = lowercase;
		_separator = separator;
	}

	public sealed override string ConvertName(string name)
	{
		if (name == null)
		{
			ThrowHelper.ThrowArgumentNullException("name");
		}
		return ConvertNameCore(_separator, _lowercase, name.AsSpan());
	}

	private static string ConvertNameCore(char separator, bool lowercase, ReadOnlySpan<char> chars)
	{
		char[] rentedBuffer = null;
		int num = (int)(1.2 * (double)chars.Length);
		Span<char> span = ((num > 128) ? ((Span<char>)(rentedBuffer = ArrayPool<char>.Shared.Rent(num))) : stackalloc char[128]);
		Span<char> destination2 = span;
		SeparatorState separatorState = SeparatorState.NotStarted;
		int charsWritten = 0;
		for (int i = 0; i < chars.Length; i++)
		{
			char c = chars[i];
			UnicodeCategory unicodeCategory = char.GetUnicodeCategory(c);
			switch (unicodeCategory)
			{
			case UnicodeCategory.UppercaseLetter:
				switch (separatorState)
				{
				case SeparatorState.LowercaseLetterOrDigit:
				case SeparatorState.SpaceSeparator:
					WriteChar(separator, ref destination2);
					break;
				case SeparatorState.UppercaseLetter:
					if (i + 1 < chars.Length && char.IsLower(chars[i + 1]))
					{
						WriteChar(separator, ref destination2);
					}
					break;
				}
				if (lowercase)
				{
					c = char.ToLowerInvariant(c);
				}
				WriteChar(c, ref destination2);
				separatorState = SeparatorState.UppercaseLetter;
				break;
			case UnicodeCategory.LowercaseLetter:
			case UnicodeCategory.DecimalDigitNumber:
				if (separatorState == SeparatorState.SpaceSeparator)
				{
					WriteChar(separator, ref destination2);
				}
				if (!lowercase && unicodeCategory == UnicodeCategory.LowercaseLetter)
				{
					c = char.ToUpperInvariant(c);
				}
				WriteChar(c, ref destination2);
				separatorState = SeparatorState.LowercaseLetterOrDigit;
				break;
			case UnicodeCategory.SpaceSeparator:
				if (separatorState != 0)
				{
					separatorState = SeparatorState.SpaceSeparator;
				}
				break;
			default:
				WriteChar(c, ref destination2);
				separatorState = SeparatorState.NotStarted;
				break;
			}
		}
		string result = destination2.Slice(0, charsWritten).ToString();
		if (rentedBuffer != null)
		{
			destination2.Slice(0, charsWritten).Clear();
			ArrayPool<char>.Shared.Return(rentedBuffer);
		}
		return result;
		void ExpandBuffer(ref Span<char> destination)
		{
			int minimumLength = checked(destination.Length * 2);
			char[] array = ArrayPool<char>.Shared.Rent(minimumLength);
			destination.CopyTo(array);
			if (rentedBuffer != null)
			{
				destination.Slice(0, charsWritten).Clear();
				ArrayPool<char>.Shared.Return(rentedBuffer);
			}
			rentedBuffer = array;
			destination = rentedBuffer;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void WriteChar(char value, ref Span<char> destination)
		{
			if (charsWritten == destination.Length)
			{
				ExpandBuffer(ref destination);
			}
			destination[charsWritten++] = value;
		}
	}
}
