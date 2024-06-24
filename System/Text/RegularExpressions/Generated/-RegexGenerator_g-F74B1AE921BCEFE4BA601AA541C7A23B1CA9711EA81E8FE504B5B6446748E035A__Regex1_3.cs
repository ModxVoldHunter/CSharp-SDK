using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "8.0.10.26715")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Regex1_3 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				while (TryFindNextPossibleStartingPosition(inputSpan) && !TryMatchAtCurrentPosition(inputSpan) && runtextpos != inputSpan.Length)
				{
					runtextpos++;
					if (_003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if (num <= inputSpan.Length - 4)
				{
					int num2 = inputSpan.Slice(num).IndexOfAnyExcept('[');
					if (num2 >= 0)
					{
						runtextpos = num + num2;
						return true;
					}
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
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				int num7 = 0;
				int pos = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				num7 = 0;
				while (true)
				{
					_003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
					num7++;
					int start2 = num;
					if (span.StartsWith("(("))
					{
						num += 2;
						span = inputSpan.Slice(num);
						int start3 = num;
						int num8 = span.IndexOf(')');
						if (num8 < 0)
						{
							num8 = span.Length;
						}
						if (num8 != 0)
						{
							span = span.Slice(num8);
							num += num8;
							Capture(2, start3, num);
							if (!span.IsEmpty && span[0] == ')')
							{
								num++;
								span = inputSpan.Slice(num);
								Capture(1, start2, num);
								if (num7 == 0)
								{
									continue;
								}
								goto IL_0118;
							}
						}
					}
					goto IL_00d8;
					IL_0118:
					num2 = num;
					int num9 = span.IndexOf('[');
					if (num9 < 0)
					{
						num9 = span.Length;
					}
					if (num9 != 0)
					{
						span = span.Slice(num9);
						num += num9;
						Capture(3, num2, num);
						if (!span.IsEmpty && span[0] == '[')
						{
							num++;
							span = inputSpan.Slice(num);
							num3 = num;
							num5 = num;
							int num10 = span.IndexOf('\n');
							if (num10 < 0)
							{
								num10 = span.Length;
							}
							if (num10 != 0)
							{
								span = span.Slice(num10);
								num += num10;
								num6 = num;
								num5++;
								while (true)
								{
									num4 = Crawlpos();
									Capture(4, num3, num);
									if (span.IsEmpty || span[0] != ']')
									{
										UncaptureUntil(num4);
										if (_003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Utilities.s_hasTimeout)
										{
											CheckTimeout();
										}
										if (num5 >= num6 || (num6 = inputSpan.Slice(num5, num6 - num5).LastIndexOf(']')) < 0)
										{
											break;
										}
										num6 += num5;
										num = num6;
										span = inputSpan.Slice(num);
										continue;
									}
									if ((uint)span.Length > 1u && span[1] == ')')
									{
										span = span.Slice(1);
										num++;
									}
									Capture(0, start, runtextpos = num + 1);
									return true;
								}
							}
						}
					}
					goto IL_00d8;
					IL_00d8:
					if (--num7 < 0)
					{
						break;
					}
					num = runstack[--pos];
					UncaptureUntil(runstack[--pos]);
					span = inputSpan.Slice(num);
					goto IL_0118;
				}
				UncaptureUntil(0);
				return false;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				void UncaptureUntil(int capturePosition)
				{
					while (Crawlpos() > capturePosition)
					{
						Uncapture();
					}
				}
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Regex1_3 Instance = new _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Regex1_3();

	private _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Regex1_3()
	{
		pattern = "([(][(](?<t>[^)]+)[)])?(?<a>[^[]+)[[](?<ia>.+)[]][)]?";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		base.CapNames = new Hashtable
		{
			{ "0", 0 },
			{ "1", 1 },
			{ "a", 3 },
			{ "ia", 4 },
			{ "t", 2 }
		};
		capslist = new string[5] { "0", "1", "t", "a", "ia" };
		capsize = 5;
	}
}
