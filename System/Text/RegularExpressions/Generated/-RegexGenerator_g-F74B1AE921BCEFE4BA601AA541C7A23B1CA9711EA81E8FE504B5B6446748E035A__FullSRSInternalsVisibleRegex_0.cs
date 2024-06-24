using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "8.0.10.26715")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__FullSRSInternalsVisibleRegex_0 : Regex
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
				if (num <= inputSpan.Length - 359 && num == 0)
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
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (num != 0)
				{
					return false;
				}
				int i;
				for (i = 0; (uint)i < (uint)span.Length && char.IsWhiteSpace(span[i]); i++)
				{
				}
				span = span.Slice(i);
				num += i;
				if (!span.StartsWith("System.Runtime.Serialization"))
				{
					return false;
				}
				int j;
				for (j = 28; (uint)j < (uint)span.Length && char.IsWhiteSpace(span[j]); j++)
				{
				}
				span = span.Slice(j);
				num += j;
				if (span.IsEmpty || span[0] != ',')
				{
					return false;
				}
				int k;
				for (k = 1; (uint)k < (uint)span.Length && char.IsWhiteSpace(span[k]); k++)
				{
				}
				span = span.Slice(k);
				num += k;
				if (!span.StartsWith("PublicKey"))
				{
					return false;
				}
				int l;
				for (l = 9; (uint)l < (uint)span.Length && char.IsWhiteSpace(span[l]); l++)
				{
				}
				span = span.Slice(l);
				num += l;
				if (span.IsEmpty || span[0] != '=')
				{
					return false;
				}
				int m;
				for (m = 1; (uint)m < (uint)span.Length && char.IsWhiteSpace(span[m]); m++)
				{
				}
				span = span.Slice(m);
				num += m;
				if ((uint)span.Length < 320u || !span.StartsWith("00240000048000009400000006020000002400005253413100040000010001008d56c76f9e8649383049f383c44be0ec204181822a6c31cf5eb7ef486944d032188ea1d3920763712ccb12d75fb77e9811149e6148e5d32fbaab37611c1878ddc19e20ef135d0cb2cff2bfec3d115810c3d9069638fe4be215dbf795861920e5ab6f7db2e2ceef136ac23d5dd2bf031700aec232f6c6b1c785b4305c123b37ab", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
				num += 320;
				span = inputSpan.Slice(num);
				num2 = num;
				int n;
				for (n = 0; (uint)n < (uint)span.Length && char.IsWhiteSpace(span[n]); n++)
				{
				}
				span = span.Slice(n);
				num += n;
				num3 = num;
				while (num < inputSpan.Length - 1 || ((uint)num < (uint)inputSpan.Length && inputSpan[num] != '\n'))
				{
					if (System.Text.RegularExpressions.Generated._003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num2 >= num3)
					{
						return false;
					}
					num = --num3;
					span = inputSpan.Slice(num);
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

	internal static readonly _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__FullSRSInternalsVisibleRegex_0 Instance = new _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__FullSRSInternalsVisibleRegex_0();

	private _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__FullSRSInternalsVisibleRegex_0()
	{
		pattern = "^[\\s]*System\\.Runtime\\.Serialization[\\s]*,[\\s]*PublicKey[\\s]*=[\\s]*(?i:00240000048000009400000006020000002400005253413100040000010001008d56c76f9e8649383049f383c44be0ec204181822a6c31cf5eb7ef486944d032188ea1d3920763712ccb12d75fb77e9811149e6148e5d32fbaab37611c1878ddc19e20ef135d0cb2cff2bfec3d115810c3d9069638fe4be215dbf795861920e5ab6f7db2e2ceef136ac23d5dd2bf031700aec232f6c6b1c785b4305c123b37ab)[\\s]*$";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(System.Text.RegularExpressions.Generated._003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Utilities.s_defaultTimeout);
		internalMatchTimeout = System.Text.RegularExpressions.Generated._003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
