using System.CodeDom.Compiler;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "8.0.10.26715")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__CreateConnectionStringValidKeyRegex_0 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				if (TryFindNextPossibleStartingPosition(inputSpan) && !TryMatchAtCurrentPosition(inputSpan))
				{
					runtextpos = inputSpan.Length;
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if ((uint)num < (uint)inputSpan.Length && num == 0)
				{
					return true;
				}
				runtextpos = inputSpan.Length;
				return false;
			}

			private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				int start = num;
				int num2 = 0;
				int num3 = 0;
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				if (num != 0)
				{
					return false;
				}
				readOnlySpan = inputSpan.Slice(num);
				int num4 = num;
				if (System.Text.RegularExpressions.Generated._003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				char c;
				if (!readOnlySpan.IsEmpty && !(((c = readOnlySpan[0]) < '\u0080') ? (("㸀\0\u0001ࠀ\0\0\0\0"[(int)c >> 4] & (1 << (c & 0xF))) == 0) : (!RegexRunner.CharInClass(c, "\0\u0002\u0001;<d"))))
				{
					return false;
				}
				num = num4;
				readOnlySpan = inputSpan.Slice(num);
				num2 = num;
				int i;
				for (i = 0; (uint)i < (uint)readOnlySpan.Length && char.GetUnicodeCategory(readOnlySpan[i]) != UnicodeCategory.Control; i++)
				{
				}
				if (i == 0)
				{
					return false;
				}
				readOnlySpan = readOnlySpan.Slice(i);
				num += i;
				num3 = num;
				num2++;
				while (true)
				{
					readOnlySpan = inputSpan.Slice(num);
					int num5 = num;
					if (System.Text.RegularExpressions.Generated._003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if ((uint)(num - 1) < inputSpan.Length && char.IsWhiteSpace(inputSpan[num - 1]))
					{
						num--;
					}
					else
					{
						num = num5;
						readOnlySpan = inputSpan.Slice(num);
						if (num >= inputSpan.Length - 1 && ((uint)num >= (uint)inputSpan.Length || inputSpan[num] == '\n'))
						{
							break;
						}
					}
					if (System.Text.RegularExpressions.Generated._003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num2 >= num3)
					{
						return false;
					}
					num = --num3;
					readOnlySpan = inputSpan.Slice(num);
				}
				runtextpos = num;
				Capture(0, start, num);
				return true;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__CreateConnectionStringValidKeyRegex_0 Instance = new _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__CreateConnectionStringValidKeyRegex_0();

	private _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__CreateConnectionStringValidKeyRegex_0()
	{
		pattern = "^(?![;\\s])[^\\p{Cc}]+(?<!\\s)$";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(System.Text.RegularExpressions.Generated._003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Utilities.s_defaultTimeout);
		internalMatchTimeout = System.Text.RegularExpressions.Generated._003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
