using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "8.0.10.26715")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__EnsureArrayIndexRegex_5 : Regex
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
				if (num <= inputSpan.Length - 35)
				{
					int num2 = inputSpan.Slice(num).IndexOfAnyExcept(' ');
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
				int num8 = 0;
				int num9 = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				num2 = num;
				int num10 = span.IndexOf(' ');
				if (num10 < 0)
				{
					num10 = span.Length;
				}
				if (num10 == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				span = span.Slice(num10);
				num += num10;
				Capture(1, num2, num);
				if (!span.StartsWith(" = "))
				{
					UncaptureUntil(0);
					return false;
				}
				num += 3;
				span = inputSpan.Slice(num);
				num8 = num;
				int num11 = span.IndexOf('\n');
				if (num11 < 0)
				{
					num11 = span.Length;
				}
				if (num11 == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				span = span.Slice(num11);
				num += num11;
				num9 = num;
				num8++;
				while (true)
				{
					num7 = Crawlpos();
					if (span.StartsWith("EnsureArrayIndex("))
					{
						num += 17;
						span = inputSpan.Slice(num);
						num3 = num;
						int num12 = span.IndexOf(',');
						if (num12 < 0)
						{
							num12 = span.Length;
						}
						if (num12 != 0)
						{
							span = span.Slice(num12);
							num += num12;
							Capture(2, num3, num);
							if (span.StartsWith(", "))
							{
								num += 2;
								span = inputSpan.Slice(num);
								num4 = num;
								int num13 = span.IndexOf(',');
								if (num13 < 0)
								{
									num13 = span.Length;
								}
								if (num13 != 0)
								{
									span = span.Slice(num13);
									num += num13;
									Capture(3, num4, num);
									if (!span.IsEmpty && span[0] == ',')
									{
										int num14 = span.Slice(1).IndexOf(';');
										if (num14 < 0)
										{
											num14 = span.Length - 1;
										}
										if (num14 != 0)
										{
											span = span.Slice(num14);
											num += num14;
											if ((uint)span.Length >= 2u && span[1] == ';')
											{
												num += 2;
												span = inputSpan.Slice(num);
												num5 = num;
												int num15 = span.IndexOf('[');
												if (num15 < 0)
												{
													num15 = span.Length;
												}
												if (num15 != 0)
												{
													span = span.Slice(num15);
													num += num15;
													Capture(4, num5, num);
													if (!span.IsEmpty && span[0] == '[')
													{
														num++;
														span = inputSpan.Slice(num);
														num6 = num;
														int num16 = span.IndexOf('+');
														if (num16 < 0)
														{
															num16 = span.Length;
														}
														if (num16 != 0)
														{
															span = span.Slice(num16);
															num += num16;
															Capture(5, num6, num);
															if (span.StartsWith("++]"))
															{
																break;
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
					UncaptureUntil(num7);
					if (_003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num8 >= num9 || (num9 = inputSpan.Slice(num8, Math.Min(inputSpan.Length, num9 + 16) - num8).LastIndexOf("EnsureArrayIndex(")) < 0)
					{
						UncaptureUntil(0);
						return false;
					}
					num9 += num8;
					num = num9;
					span = inputSpan.Slice(num);
				}
				Capture(0, start, runtextpos = num + 3);
				return true;
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

	internal static readonly _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__EnsureArrayIndexRegex_5 Instance = new _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__EnsureArrayIndexRegex_5();

	private _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__EnsureArrayIndexRegex_5()
	{
		pattern = "(?<locA1>[^ ]+) = .+EnsureArrayIndex[(](?<locA2>[^,]+), (?<locI1>[^,]+),[^;]+;(?<locA3>[^[]+)[[](?<locI2>[^+]+)[+][+][]]";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		base.CapNames = new Hashtable
		{
			{ "0", 0 },
			{ "locA1", 1 },
			{ "locA2", 2 },
			{ "locA3", 4 },
			{ "locI1", 3 },
			{ "locI2", 5 }
		};
		capslist = new string[6] { "0", "locA1", "locA2", "locI1", "locA3", "locI2" };
		capsize = 6;
	}
}
