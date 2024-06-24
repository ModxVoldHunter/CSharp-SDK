using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "8.0.10.26715")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__CreateConnectionStringQuoteValueRegex_1 : Regex
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
				if (runtextpos == 0)
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
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				if (num != 0)
				{
					return false;
				}
				int i;
				for (i = 0; (uint)i < (uint)readOnlySpan.Length; i++)
				{
					char c;
					if ((((c = readOnlySpan[i]) < '\u0080') ? ("\0\0ｺ\ud7ff\uffff\uffff\uffff翿"[(int)c >> 4] & (1 << (c & 0xF))) : (RegexRunner.CharInClass(c, "\u0001\b\u0002\"#'(;<=>d\u000f") ? 1 : 0)) == 0)
					{
						break;
					}
				}
				readOnlySpan = readOnlySpan.Slice(i);
				num += i;
				if (num < inputSpan.Length - 1 || ((uint)num < (uint)inputSpan.Length && inputSpan[num] != '\n'))
				{
					return false;
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

	internal static readonly _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__CreateConnectionStringQuoteValueRegex_1 Instance = new _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__CreateConnectionStringQuoteValueRegex_1();

	private _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__CreateConnectionStringQuoteValueRegex_1()
	{
		pattern = "^[^\"'=;\\s\\p{Cc}]*$";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(System.Text.RegularExpressions.Generated._003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Utilities.s_defaultTimeout);
		internalMatchTimeout = System.Text.RegularExpressions.Generated._003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
