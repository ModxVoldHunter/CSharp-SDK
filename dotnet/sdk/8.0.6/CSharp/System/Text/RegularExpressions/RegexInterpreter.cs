using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions;

internal sealed class RegexInterpreter : RegexRunner
{
	private readonly RegexInterpreterCode _code;

	private readonly CultureInfo _culture;

	private RegexCaseBehavior _caseBehavior;

	private RegexOpcode _operator;

	private int _codepos;

	private bool _rightToLeft;

	public RegexInterpreter(RegexInterpreterCode code, CultureInfo culture)
	{
		_code = code;
		_culture = culture;
	}

	protected override void InitTrackCount()
	{
		runtrackcount = _code.TrackCount;
	}

	private void Advance(int i)
	{
		_codepos += i + 1;
		SetOperator((RegexOpcode)_code.Codes[_codepos]);
	}

	private void Goto(int newpos)
	{
		if (newpos < _codepos)
		{
			EnsureStorage();
		}
		_codepos = newpos;
		SetOperator((RegexOpcode)_code.Codes[newpos]);
	}

	private void Trackto(int newpos)
	{
		runtrackpos = runtrack.Length - newpos;
	}

	private int Trackpos()
	{
		return runtrack.Length - runtrackpos;
	}

	private void TrackPush()
	{
		runtrack[--runtrackpos] = _codepos;
	}

	private void TrackPush(int i1)
	{
		int[] array = runtrack;
		int num = runtrackpos;
		array[--num] = i1;
		array[--num] = _codepos;
		runtrackpos = num;
	}

	private void TrackPush(int i1, int i2)
	{
		int[] array = runtrack;
		int num = runtrackpos;
		array[--num] = i1;
		array[--num] = i2;
		array[--num] = _codepos;
		runtrackpos = num;
	}

	private void TrackPush(int i1, int i2, int i3)
	{
		int[] array = runtrack;
		int num = runtrackpos;
		array[--num] = i1;
		array[--num] = i2;
		array[--num] = i3;
		array[--num] = _codepos;
		runtrackpos = num;
	}

	private void TrackPush2(int i1)
	{
		int[] array = runtrack;
		int num = runtrackpos;
		array[--num] = i1;
		array[--num] = -_codepos;
		runtrackpos = num;
	}

	private void TrackPush2(int i1, int i2)
	{
		int[] array = runtrack;
		int num = runtrackpos;
		array[--num] = i1;
		array[--num] = i2;
		array[--num] = -_codepos;
		runtrackpos = num;
	}

	private void Backtrack()
	{
		CheckTimeout();
		int num = runtrack[runtrackpos];
		runtrackpos++;
		int num2 = 128;
		if (num < 0)
		{
			num = -num;
			num2 = 256;
		}
		SetOperator((RegexOpcode)(_code.Codes[num] | num2));
		if (num < _codepos)
		{
			EnsureStorage();
		}
		_codepos = num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetOperator(RegexOpcode op)
	{
		_operator = op & (RegexOpcode)(-65);
		_rightToLeft = (op & RegexOpcode.RightToLeft) != 0;
	}

	private void TrackPop()
	{
		runtrackpos++;
	}

	private void TrackPop(int framesize)
	{
		runtrackpos += framesize;
	}

	private int TrackPeek()
	{
		return runtrack[runtrackpos - 1];
	}

	private int TrackPeek(int i)
	{
		return runtrack[runtrackpos - i - 1];
	}

	private void StackPush(int i1)
	{
		runstack[--runstackpos] = i1;
	}

	private void StackPush(int i1, int i2)
	{
		int[] array = runstack;
		int num = runstackpos;
		array[--num] = i1;
		array[--num] = i2;
		runstackpos = num;
	}

	private void StackPop()
	{
		runstackpos++;
	}

	private void StackPop(int framesize)
	{
		runstackpos += framesize;
	}

	private int StackPeek()
	{
		return runstack[runstackpos - 1];
	}

	private int StackPeek(int i)
	{
		return runstack[runstackpos - i - 1];
	}

	private int Operand(int i)
	{
		return _code.Codes[_codepos + i + 1];
	}

	private int Bump()
	{
		if (!_rightToLeft)
		{
			return 1;
		}
		return -1;
	}

	private int Forwardchars()
	{
		if (!_rightToLeft)
		{
			return runtextend - runtextpos;
		}
		return runtextpos;
	}

	private char Forwardcharnext(ReadOnlySpan<char> inputSpan)
	{
		int index = (_rightToLeft ? (--runtextpos) : runtextpos++);
		return inputSpan[index];
	}

	private bool MatchString(string str, ReadOnlySpan<char> inputSpan)
	{
		int num = str.Length;
		int num2;
		if (!_rightToLeft)
		{
			if (inputSpan.Length - runtextpos < num)
			{
				return false;
			}
			num2 = runtextpos + num;
		}
		else
		{
			if (runtextpos < num)
			{
				return false;
			}
			num2 = runtextpos;
		}
		while (num != 0)
		{
			if (str[--num] != inputSpan[--num2])
			{
				return false;
			}
		}
		if (!_rightToLeft)
		{
			num2 += str.Length;
		}
		runtextpos = num2;
		return true;
	}

	private bool MatchRef(int index, int length, ReadOnlySpan<char> inputSpan, bool caseInsensitive)
	{
		int num;
		if (!_rightToLeft)
		{
			if (inputSpan.Length - runtextpos < length)
			{
				return false;
			}
			num = runtextpos + length;
		}
		else
		{
			if (runtextpos < length)
			{
				return false;
			}
			num = runtextpos;
		}
		int num2 = index + length;
		int num3 = length;
		if (!caseInsensitive)
		{
			while (num3-- != 0)
			{
				if (inputSpan[--num2] != inputSpan[--num])
				{
					return false;
				}
			}
		}
		else
		{
			while (num3-- != 0)
			{
				char c = inputSpan[--num2];
				char c2 = inputSpan[--num];
				if (c != c2 && (!RegexCaseEquivalences.TryFindCaseEquivalencesForCharWithIBehavior(c, _culture, ref _caseBehavior, out var equivalences) || equivalences.IndexOf(inputSpan[num]) < 0))
				{
					return false;
				}
			}
		}
		if (!_rightToLeft)
		{
			num += length;
		}
		runtextpos = num;
		return true;
	}

	private void Backwardnext()
	{
		runtextpos += (_rightToLeft ? 1 : (-1));
	}

	protected internal override void Scan(ReadOnlySpan<char> text)
	{
		if (runregex.RightToLeft)
		{
			while (_code.FindOptimizations.TryFindNextStartingPositionRightToLeft(text, ref runtextpos, runtextstart))
			{
				CheckTimeout();
				if (TryMatchAtCurrentPosition(text) || runtextpos == 0)
				{
					break;
				}
				runtrackpos = runtrack.Length;
				runstackpos = runstack.Length;
				runcrawlpos = runcrawl.Length;
				runtextpos--;
			}
			return;
		}
		while (_code.FindOptimizations.TryFindNextStartingPositionLeftToRight(text, ref runtextpos, runtextstart))
		{
			CheckTimeout();
			if (TryMatchAtCurrentPosition(text) || runtextpos == text.Length)
			{
				break;
			}
			runtrackpos = runtrack.Length;
			runstackpos = runstack.Length;
			runcrawlpos = runcrawl.Length;
			runtextpos++;
		}
	}

	private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)
	{
		SetOperator((RegexOpcode)_code.Codes[0]);
		_codepos = 0;
		int num = -1;
		while (true)
		{
			if (num >= 0)
			{
				Advance(num);
				num = -1;
			}
			switch (_operator)
			{
			case RegexOpcode.Stop:
				return runmatch.FoundMatch;
			case RegexOpcode.Goto:
				Goto(Operand(0));
				continue;
			case RegexOpcode.TestBackreference:
				if (IsMatched(Operand(0)))
				{
					num = 1;
					continue;
				}
				break;
			case RegexOpcode.Lazybranch:
				TrackPush(runtextpos);
				num = 1;
				continue;
			case (RegexOpcode)151:
				TrackPop();
				runtextpos = TrackPeek();
				Goto(Operand(0));
				continue;
			case RegexOpcode.Setmark:
				StackPush(runtextpos);
				TrackPush();
				num = 0;
				continue;
			case RegexOpcode.Nullmark:
				StackPush(-1);
				TrackPush();
				num = 0;
				continue;
			case (RegexOpcode)158:
			case (RegexOpcode)159:
				StackPop();
				break;
			case RegexOpcode.Getmark:
				StackPop();
				TrackPush(StackPeek());
				runtextpos = StackPeek();
				num = 0;
				continue;
			case (RegexOpcode)161:
				TrackPop();
				StackPush(TrackPeek());
				break;
			case RegexOpcode.Capturemark:
				if (Operand(1) == -1 || IsMatched(Operand(1)))
				{
					StackPop();
					if (Operand(1) != -1)
					{
						TransferCapture(Operand(0), Operand(1), StackPeek(), runtextpos);
					}
					else
					{
						Capture(Operand(0), StackPeek(), runtextpos);
					}
					TrackPush(StackPeek());
					num = 2;
					continue;
				}
				break;
			case (RegexOpcode)160:
				TrackPop();
				StackPush(TrackPeek());
				Uncapture();
				if (Operand(0) != -1 && Operand(1) != -1)
				{
					Uncapture();
				}
				break;
			case RegexOpcode.Branchmark:
				StackPop();
				if (runtextpos != StackPeek())
				{
					TrackPush(StackPeek(), runtextpos);
					StackPush(runtextpos);
					Goto(Operand(0));
				}
				else
				{
					TrackPush2(StackPeek());
					num = 1;
				}
				continue;
			case (RegexOpcode)152:
				TrackPop(2);
				StackPop();
				runtextpos = TrackPeek(1);
				TrackPush2(TrackPeek());
				num = 1;
				continue;
			case (RegexOpcode)280:
				TrackPop();
				StackPush(TrackPeek());
				break;
			case RegexOpcode.Lazybranchmark:
			{
				StackPop();
				int num29 = StackPeek();
				if (runtextpos != num29)
				{
					if (num29 != -1)
					{
						TrackPush(num29, runtextpos);
					}
					else
					{
						TrackPush(runtextpos, runtextpos);
					}
				}
				else
				{
					StackPush(num29);
					TrackPush2(StackPeek());
				}
				num = 1;
				continue;
			}
			case (RegexOpcode)153:
			{
				TrackPop(2);
				int i2 = TrackPeek(1);
				TrackPush2(TrackPeek());
				StackPush(i2);
				runtextpos = i2;
				Goto(Operand(0));
				continue;
			}
			case (RegexOpcode)281:
				StackPop();
				TrackPop();
				StackPush(TrackPeek());
				break;
			case RegexOpcode.Setcount:
				StackPush(runtextpos, Operand(0));
				TrackPush();
				num = 1;
				continue;
			case RegexOpcode.Nullcount:
				StackPush(-1, Operand(0));
				TrackPush();
				num = 1;
				continue;
			case (RegexOpcode)154:
			case (RegexOpcode)155:
			case (RegexOpcode)162:
				StackPop(2);
				break;
			case RegexOpcode.Branchcount:
			{
				StackPop(2);
				int num20 = StackPeek();
				int num21 = StackPeek(1);
				int num22 = runtextpos - num20;
				if (num21 >= Operand(1) || (num22 == 0 && num21 >= 0))
				{
					TrackPush2(num20, num21);
					num = 2;
				}
				else
				{
					TrackPush(num20);
					StackPush(runtextpos, num21 + 1);
					Goto(Operand(0));
				}
				continue;
			}
			case (RegexOpcode)156:
				TrackPop();
				StackPop(2);
				if (StackPeek(1) > 0)
				{
					runtextpos = StackPeek();
					TrackPush2(TrackPeek(), StackPeek(1) - 1);
					num = 2;
					continue;
				}
				StackPush(TrackPeek(), StackPeek(1) - 1);
				break;
			case (RegexOpcode)284:
				TrackPop(2);
				StackPush(TrackPeek(), TrackPeek(1));
				break;
			case RegexOpcode.Lazybranchcount:
			{
				StackPop(2);
				int i = StackPeek();
				int num7 = StackPeek(1);
				if (num7 < 0)
				{
					TrackPush2(i);
					StackPush(runtextpos, num7 + 1);
					Goto(Operand(0));
				}
				else
				{
					TrackPush(i, num7, runtextpos);
					num = 2;
				}
				continue;
			}
			case (RegexOpcode)157:
			{
				TrackPop(3);
				int num2 = TrackPeek();
				int num3 = TrackPeek(2);
				if (TrackPeek(1) < Operand(1) && num3 != num2)
				{
					runtextpos = num3;
					StackPush(num3, TrackPeek(1) + 1);
					TrackPush2(num2);
					Goto(Operand(0));
					continue;
				}
				StackPush(TrackPeek(), TrackPeek(1));
				break;
			}
			case (RegexOpcode)285:
				TrackPop();
				StackPop(2);
				StackPush(TrackPeek(), StackPeek(1) - 1);
				break;
			case RegexOpcode.Setjump:
				CheckTimeout();
				StackPush(Trackpos(), Crawlpos());
				TrackPush();
				num = 0;
				continue;
			case RegexOpcode.Backjump:
				StackPop(2);
				Trackto(StackPeek());
				while (Crawlpos() != StackPeek(1))
				{
					Uncapture();
				}
				break;
			case RegexOpcode.Forejump:
				StackPop(2);
				Trackto(StackPeek());
				TrackPush(StackPeek(1));
				num = 0;
				continue;
			case (RegexOpcode)164:
				TrackPop();
				while (Crawlpos() != TrackPeek())
				{
					Uncapture();
				}
				break;
			case RegexOpcode.Bol:
			{
				int num28 = runtextpos - 1;
				if ((uint)num28 >= (uint)inputSpan.Length || inputSpan[num28] == '\n')
				{
					num = 0;
					continue;
				}
				break;
			}
			case RegexOpcode.Eol:
			{
				int num27 = runtextpos;
				if ((uint)num27 >= (uint)inputSpan.Length || inputSpan[num27] == '\n')
				{
					num = 0;
					continue;
				}
				break;
			}
			case RegexOpcode.Boundary:
				if (RegexRunner.IsBoundary(inputSpan, runtextpos))
				{
					num = 0;
					continue;
				}
				break;
			case RegexOpcode.NonBoundary:
				if (!RegexRunner.IsBoundary(inputSpan, runtextpos))
				{
					num = 0;
					continue;
				}
				break;
			case RegexOpcode.ECMABoundary:
				if (RegexRunner.IsECMABoundary(inputSpan, runtextpos))
				{
					num = 0;
					continue;
				}
				break;
			case RegexOpcode.NonECMABoundary:
				if (!RegexRunner.IsECMABoundary(inputSpan, runtextpos))
				{
					num = 0;
					continue;
				}
				break;
			case RegexOpcode.Beginning:
				if (runtextpos <= 0)
				{
					num = 0;
					continue;
				}
				break;
			case RegexOpcode.Start:
				if (runtextpos == runtextstart)
				{
					num = 0;
					continue;
				}
				break;
			case RegexOpcode.EndZ:
			{
				int num16 = runtextpos;
				if (num16 >= inputSpan.Length - 1 && ((uint)num16 >= (uint)inputSpan.Length || inputSpan[num16] == '\n'))
				{
					num = 0;
					continue;
				}
				break;
			}
			case RegexOpcode.End:
				if (runtextpos >= inputSpan.Length)
				{
					num = 0;
					continue;
				}
				break;
			case RegexOpcode.One:
				if (Forwardchars() >= 1 && Forwardcharnext(inputSpan) == (ushort)Operand(0))
				{
					num = 1;
					continue;
				}
				break;
			case RegexOpcode.Notone:
				if (Forwardchars() >= 1 && Forwardcharnext(inputSpan) != (ushort)Operand(0))
				{
					num = 1;
					continue;
				}
				break;
			case RegexOpcode.Set:
				if (Forwardchars() >= 1)
				{
					int num8 = Operand(0);
					if (RegexCharClass.CharInClass(Forwardcharnext(inputSpan), _code.Strings[num8], ref _code.StringsAsciiLookup[num8]))
					{
						num = 1;
						continue;
					}
				}
				break;
			case RegexOpcode.Multi:
				if (MatchString(_code.Strings[Operand(0)], inputSpan))
				{
					num = 1;
					continue;
				}
				break;
			case RegexOpcode.Backreference:
			case (RegexOpcode)525:
			{
				int cap = Operand(0);
				if (IsMatched(cap))
				{
					if (!MatchRef(MatchIndex(cap), MatchLength(cap), inputSpan, (_operator & RegexOpcode.CaseInsensitive) != 0))
					{
						break;
					}
				}
				else if ((runregex.roptions & RegexOptions.ECMAScript) == 0)
				{
					break;
				}
				num = 1;
				continue;
			}
			case RegexOpcode.Onerep:
			{
				int num33 = Operand(1);
				if (Forwardchars() < num33)
				{
					break;
				}
				char c4 = (char)Operand(0);
				while (num33-- > 0)
				{
					if (Forwardcharnext(inputSpan) != c4)
					{
						goto end_IL_0036;
					}
				}
				num = 2;
				continue;
			}
			case RegexOpcode.Notonerep:
			{
				int num32 = Operand(1);
				if (Forwardchars() < num32)
				{
					break;
				}
				char c3 = (char)Operand(0);
				while (num32-- > 0)
				{
					if (Forwardcharnext(inputSpan) == c3)
					{
						goto end_IL_0036;
					}
				}
				num = 2;
				continue;
			}
			case RegexOpcode.Setrep:
			{
				int num30 = Operand(1);
				if (Forwardchars() < num30)
				{
					break;
				}
				int num31 = Operand(0);
				string set2 = _code.Strings[num31];
				ref uint[] asciiLazyCache2 = ref _code.StringsAsciiLookup[num31];
				while (num30-- > 0)
				{
					if (!RegexCharClass.CharInClass(Forwardcharnext(inputSpan), set2, ref asciiLazyCache2))
					{
						goto end_IL_0036;
					}
				}
				num = 2;
				continue;
			}
			case RegexOpcode.Oneloop:
			case RegexOpcode.Oneloopatomic:
			{
				int num25 = Math.Min(Operand(1), Forwardchars());
				char c2 = (char)Operand(0);
				int num26;
				for (num26 = num25; num26 > 0; num26--)
				{
					if (Forwardcharnext(inputSpan) != c2)
					{
						Backwardnext();
						break;
					}
				}
				if (num25 > num26 && _operator == RegexOpcode.Oneloop)
				{
					TrackPush(num25 - num26 - 1, runtextpos - Bump());
				}
				num = 2;
				continue;
			}
			case RegexOpcode.Notoneloop:
			case RegexOpcode.Notoneloopatomic:
			{
				int num23 = Math.Min(Operand(1), Forwardchars());
				char c = (char)Operand(0);
				int num24;
				if (!_rightToLeft)
				{
					num24 = inputSpan.Slice(runtextpos, num23).IndexOf(c);
					if (num24 == -1)
					{
						runtextpos += num23;
						num24 = 0;
					}
					else
					{
						runtextpos += num24;
						num24 = num23 - num24;
					}
				}
				else
				{
					for (num24 = num23; num24 > 0; num24--)
					{
						if (Forwardcharnext(inputSpan) == c)
						{
							Backwardnext();
							break;
						}
					}
				}
				if (num23 > num24 && _operator == RegexOpcode.Notoneloop)
				{
					TrackPush(num23 - num24 - 1, runtextpos - Bump());
				}
				num = 2;
				continue;
			}
			case RegexOpcode.Setloop:
			case RegexOpcode.Setloopatomic:
			{
				int num17 = Math.Min(Operand(1), Forwardchars());
				int num18 = Operand(0);
				string set = _code.Strings[num18];
				ref uint[] asciiLazyCache = ref _code.StringsAsciiLookup[num18];
				int num19;
				for (num19 = num17; num19 > 0; num19--)
				{
					if (!RegexCharClass.CharInClass(Forwardcharnext(inputSpan), set, ref asciiLazyCache))
					{
						Backwardnext();
						break;
					}
				}
				if (num17 > num19 && _operator == RegexOpcode.Setloop)
				{
					TrackPush(num17 - num19 - 1, runtextpos - Bump());
				}
				num = 2;
				continue;
			}
			case (RegexOpcode)131:
			case (RegexOpcode)132:
			case (RegexOpcode)133:
			{
				TrackPop(2);
				int num14 = TrackPeek();
				int num15 = (runtextpos = TrackPeek(1));
				if (num14 > 0)
				{
					TrackPush(num14 - 1, num15 - Bump());
				}
				num = 2;
				continue;
			}
			case RegexOpcode.Onelazy:
			case RegexOpcode.Notonelazy:
			case RegexOpcode.Setlazy:
			{
				int num13 = Math.Min(Operand(1), Forwardchars());
				if (num13 > 0)
				{
					TrackPush(num13 - 1, runtextpos);
				}
				num = 2;
				continue;
			}
			case (RegexOpcode)134:
			{
				TrackPop(2);
				int num11 = (runtextpos = TrackPeek(1));
				if (Forwardcharnext(inputSpan) == (ushort)Operand(0))
				{
					int num12 = TrackPeek();
					if (num12 > 0)
					{
						TrackPush(num12 - 1, num11 + Bump());
					}
					num = 2;
					continue;
				}
				break;
			}
			case (RegexOpcode)135:
			{
				TrackPop(2);
				int num9 = (runtextpos = TrackPeek(1));
				if (Forwardcharnext(inputSpan) != (ushort)Operand(0))
				{
					int num10 = TrackPeek();
					if (num10 > 0)
					{
						TrackPush(num10 - 1, num9 + Bump());
					}
					num = 2;
					continue;
				}
				break;
			}
			case (RegexOpcode)136:
			{
				TrackPop(2);
				int num4 = (runtextpos = TrackPeek(1));
				int num5 = Operand(0);
				if (RegexCharClass.CharInClass(Forwardcharnext(inputSpan), _code.Strings[num5], ref _code.StringsAsciiLookup[num5]))
				{
					int num6 = TrackPeek();
					if (num6 > 0)
					{
						TrackPush(num6 - 1, num4 + Bump());
					}
					num = 2;
					continue;
				}
				break;
			}
			case RegexOpcode.UpdateBumpalong:
				{
					ref int reference = ref runtrack[runtrack.Length - 1];
					if (reference < runtextpos)
					{
						reference = runtextpos;
					}
					num = 0;
					continue;
				}
				end_IL_0036:
				break;
			}
			Backtrack();
		}
	}
}
