using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Text;

[DebuggerDisplay("{Format}")]
public sealed class CompositeFormat
{
	internal readonly (string Literal, int ArgIndex, int Alignment, string Format)[] _segments;

	internal readonly int _literalLength;

	internal readonly int _formattedCount;

	internal readonly int _argsRequired;

	public string Format { get; }

	public int MinimumArgumentCount => _argsRequired;

	private CompositeFormat(string format, (string Literal, int ArgIndex, int Alignment, string Format)[] segments)
	{
		Format = format;
		_segments = segments;
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < segments.Length; i++)
		{
			(string, int, int, string) tuple = segments[i];
			var (text, _, _, _) = tuple;
			if (text != null)
			{
				num += text.Length;
			}
			else if (tuple.Item2 >= 0)
			{
				num2++;
				num3 = Math.Max(num3, tuple.Item2 + 1);
			}
		}
		_literalLength = num;
		_formattedCount = num2;
		_argsRequired = num3;
	}

	public static CompositeFormat Parse([StringSyntax("CompositeFormat")] string format)
	{
		ArgumentNullException.ThrowIfNull(format, "format");
		List<(string, int, int, string)> list = new List<(string, int, int, string)>();
		int failureOffset = 0;
		ExceptionResource failureReason = ExceptionResource.ArgumentOutOfRange_IndexMustBeLessOrEqual;
		if (!TryParseLiterals(format, list, ref failureOffset, ref failureReason))
		{
			ThrowHelper.ThrowFormatInvalidString(failureOffset, failureReason);
		}
		return new CompositeFormat(format, list.ToArray());
	}

	internal void ValidateNumberOfArgs(int numArgs)
	{
		if (numArgs < _argsRequired)
		{
			ThrowHelper.ThrowFormatIndexOutOfRange();
		}
	}

	private static bool TryParseLiterals(ReadOnlySpan<char> format, List<(string Literal, int ArgIndex, int Alignment, string Format)> segments, ref int failureOffset, ref ExceptionResource failureReason)
	{
		Span<char> initialBuffer = stackalloc char[256];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		int num = 0;
		while (true)
		{
			ReadOnlySpan<char> readOnlySpan = format.Slice(num);
			int num2 = readOnlySpan.IndexOfAny('{', '}');
			if (num2 < 0)
			{
				valueStringBuilder.Append(readOnlySpan);
				segments.Add((valueStringBuilder.ToString(), -1, 0, null));
				return true;
			}
			valueStringBuilder.Append(readOnlySpan.Slice(0, num2));
			num += num2;
			char c = format[num];
			string item2;
			int num3;
			int item;
			if (TryMoveNext(format, ref num, out var nextChar2))
			{
				if (c == nextChar2)
				{
					valueStringBuilder.Append(nextChar2);
					num++;
					continue;
				}
				if (c != '{')
				{
					failureReason = ExceptionResource.Format_UnexpectedClosingBrace;
					failureOffset = num;
					return false;
				}
				segments.Add((valueStringBuilder.ToString(), -1, 0, null));
				valueStringBuilder.Length = 0;
				item = 0;
				item2 = null;
				num3 = nextChar2 - 48;
				if ((uint)num3 >= 10u)
				{
					break;
				}
				if (TryMoveNext(format, ref num, out nextChar2))
				{
					if (nextChar2 == '}')
					{
						goto IL_0214;
					}
					while (char.IsAsciiDigit(nextChar2))
					{
						num3 = num3 * 10 + nextChar2 - 48;
						if (TryMoveNext(format, ref num, out nextChar2))
						{
							continue;
						}
						goto IL_0238;
					}
					while (nextChar2 == ' ')
					{
						if (TryMoveNext(format, ref num, out nextChar2))
						{
							continue;
						}
						goto IL_0238;
					}
					if (nextChar2 != ',')
					{
						goto IL_01cb;
					}
					while (TryMoveNext(format, ref num, out nextChar2))
					{
						if (nextChar2 == ' ')
						{
							continue;
						}
						goto IL_0154;
					}
				}
			}
			goto IL_0238;
			IL_0238:
			failureReason = ExceptionResource.Format_UnclosedFormatItem;
			failureOffset = num;
			return false;
			IL_0154:
			int num4 = 1;
			if (nextChar2 == '-')
			{
				num4 = -1;
				if (!TryMoveNext(format, ref num, out nextChar2))
				{
					goto IL_0238;
				}
			}
			item = nextChar2 - 48;
			if ((uint)item >= 10u)
			{
				break;
			}
			if (!TryMoveNext(format, ref num, out nextChar2))
			{
				goto IL_0238;
			}
			while (char.IsAsciiDigit(nextChar2))
			{
				item = item * 10 + nextChar2 - 48;
				if (TryMoveNext(format, ref num, out nextChar2))
				{
					continue;
				}
				goto IL_0238;
			}
			item *= num4;
			while (nextChar2 == ' ')
			{
				if (TryMoveNext(format, ref num, out nextChar2))
				{
					continue;
				}
				goto IL_0238;
			}
			goto IL_01cb;
			IL_01cb:
			if (nextChar2 == '}')
			{
				goto IL_0214;
			}
			int num5;
			if (nextChar2 == ':')
			{
				num5 = num;
				while (TryMoveNext(format, ref num, out nextChar2))
				{
					if (nextChar2 != '}')
					{
						if (nextChar2 == '{')
						{
							break;
						}
						continue;
					}
					goto IL_01f0;
				}
			}
			goto IL_0238;
			IL_01f0:
			num5++;
			item2 = format.Slice(num5, num - num5).ToString();
			goto IL_0214;
			IL_0214:
			num++;
			segments.Add((null, num3, item, item2));
		}
		failureReason = ExceptionResource.Format_ExpectedAsciiDigit;
		failureOffset = num;
		return false;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool TryMoveNext(ReadOnlySpan<char> format, ref int pos, out char nextChar)
		{
			pos++;
			if ((uint)pos >= (uint)format.Length)
			{
				nextChar = '\0';
				return false;
			}
			nextChar = format[pos];
			return true;
		}
	}
}
