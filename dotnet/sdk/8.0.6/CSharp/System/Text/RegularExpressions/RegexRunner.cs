using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions;

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class RegexRunner
{
	protected internal int runtextbeg;

	protected internal int runtextend;

	protected internal int runtextstart;

	protected internal string? runtext;

	protected internal int runtextpos;

	protected internal int[]? runtrack;

	protected internal int runtrackpos;

	protected internal int[]? runstack;

	protected internal int runstackpos;

	protected internal int[]? runcrawl;

	protected internal int runcrawlpos;

	protected internal int runtrackcount;

	protected internal Match? runmatch;

	protected internal Regex? runregex;

	private protected RegexRunnerMode _mode;

	private int _timeout;

	private bool _checkTimeout;

	private long _timeoutOccursAt;

	protected internal virtual void Scan(ReadOnlySpan<char> text)
	{
		string text2 = runtext;
		text2.AsSpan().Overlaps(text, out var elementOffset);
		if (text2 == null || text != text2.AsSpan(elementOffset, text.Length))
		{
			throw new NotSupportedException(System.SR.UsingSpanAPIsWithCompiledToAssembly);
		}
		if (elementOffset != 0)
		{
			runtextbeg = elementOffset;
			runtextstart += elementOffset;
			runtextend += elementOffset;
		}
		InternalScan(runregex, elementOffset, elementOffset + text.Length);
	}

	[Obsolete("This API supports obsolete mechanisms for Regex extensibility. It is not supported.", DiagnosticId = "SYSLIB0052", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	protected Match? Scan(Regex regex, string text, int textbeg, int textend, int textstart, int prevlen, bool quick)
	{
		return Scan(regex, text, textbeg, textend, textstart, prevlen, quick, regex.MatchTimeout);
	}

	[Obsolete("This API supports obsolete mechanisms for Regex extensibility. It is not supported.", DiagnosticId = "SYSLIB0052", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	protected internal Match? Scan(Regex regex, string text, int textbeg, int textend, int textstart, int prevlen, bool quick, TimeSpan timeout)
	{
		InitializeTimeout(timeout);
		RegexRunnerMode mode = ((!quick) ? RegexRunnerMode.FullMatchRequired : RegexRunnerMode.ExistenceRequired);
		runtext = text;
		InitializeForScan(regex, text, textstart, mode);
		runtextstart = textstart;
		runtextend = textend;
		int num = 1;
		int num2 = textend;
		if (regex.RightToLeft)
		{
			num = -1;
			num2 = textbeg;
		}
		if (prevlen == 0)
		{
			if (textstart == num2)
			{
				return Match.Empty;
			}
			runtextpos += num;
		}
		Match match = InternalScan(regex, textbeg, textend);
		runtext = null;
		if (match.FoundMatch)
		{
			if (quick)
			{
				return null;
			}
			runmatch = null;
			match.Tidy(runtextpos, 0, mode);
		}
		else
		{
			runmatch.Text = null;
		}
		return match;
	}

	private Match InternalScan(Regex regex, int textbeg, int textend)
	{
		int num = 1;
		int num2 = textend;
		if (regex.RightToLeft)
		{
			num = -1;
			num2 = textbeg;
		}
		while (true)
		{
			if (FindFirstChar())
			{
				CheckTimeout();
				Go();
				if (runmatch.FoundMatch)
				{
					return runmatch;
				}
				runtrackpos = runtrack.Length;
				runstackpos = runstack.Length;
				runcrawlpos = runcrawl.Length;
			}
			if (runtextpos == num2)
			{
				break;
			}
			runtextpos += num;
		}
		return Match.Empty;
	}

	internal void InitializeForScan(Regex regex, ReadOnlySpan<char> text, int textstart, RegexRunnerMode mode)
	{
		_mode = mode;
		runregex = regex;
		runtextstart = textstart;
		runtextbeg = 0;
		runtextend = text.Length;
		runtextpos = textstart;
		Match match = runmatch;
		if (match == null)
		{
			runmatch = ((runregex.caps == null) ? new Match(runregex, runregex.capsize, runtext, text.Length) : new MatchSparse(runregex, runregex.caps, runregex.capsize, runtext, text.Length));
		}
		else
		{
			match.Reset(runtext, text.Length);
		}
		if (runcrawl != null)
		{
			runtrackpos = runtrack.Length;
			runstackpos = runstack.Length;
			runcrawlpos = runcrawl.Length;
			return;
		}
		InitTrackCount();
		int num;
		int num2 = (num = runtrackcount * 8);
		if (num2 < 32)
		{
			num2 = 32;
		}
		if (num < 16)
		{
			num = 16;
		}
		runtrack = new int[num2];
		runtrackpos = num2;
		runstack = new int[num];
		runstackpos = num;
		runcrawl = new int[32];
		runcrawlpos = 32;
	}

	internal void InitializeTimeout(TimeSpan timeout)
	{
		_checkTimeout = false;
		if (Regex.InfiniteMatchTimeout != timeout)
		{
			ConfigureTimeout(timeout);
		}
		void ConfigureTimeout(TimeSpan timeout)
		{
			_checkTimeout = true;
			_timeout = (int)(timeout.TotalMilliseconds + 0.5);
			_timeoutOccursAt = Environment.TickCount64 + _timeout;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected internal void CheckTimeout()
	{
		if (_checkTimeout && Environment.TickCount64 >= _timeoutOccursAt)
		{
			ThrowRegexTimeout();
		}
		void ThrowRegexTimeout()
		{
			throw new RegexMatchTimeoutException(runtext ?? string.Empty, runregex.pattern, TimeSpan.FromMilliseconds(_timeout));
		}
	}

	protected virtual void Go()
	{
		throw new NotImplementedException();
	}

	protected virtual bool FindFirstChar()
	{
		throw new NotImplementedException();
	}

	protected virtual void InitTrackCount()
	{
	}

	protected void EnsureStorage()
	{
		int num = runtrackcount * 4;
		if (runstackpos < num)
		{
			DoubleStack();
		}
		if (runtrackpos < num)
		{
			DoubleTrack();
		}
	}

	protected bool IsBoundary(int index, int startpos, int endpos)
	{
		return (index > startpos && RegexCharClass.IsBoundaryWordChar(runtext[index - 1])) != (index < endpos && RegexCharClass.IsBoundaryWordChar(runtext[index]));
	}

	internal static bool IsBoundary(ReadOnlySpan<char> inputSpan, int index)
	{
		int num = index - 1;
		return ((uint)num < (uint)inputSpan.Length && RegexCharClass.IsBoundaryWordChar(inputSpan[num])) != ((uint)index < (uint)inputSpan.Length && RegexCharClass.IsBoundaryWordChar(inputSpan[index]));
	}

	internal static bool IsWordChar(char ch)
	{
		return RegexCharClass.IsWordChar(ch);
	}

	protected bool IsECMABoundary(int index, int startpos, int endpos)
	{
		return (index > startpos && RegexCharClass.IsECMAWordChar(runtext[index - 1])) != (index < endpos && RegexCharClass.IsECMAWordChar(runtext[index]));
	}

	internal static bool IsECMABoundary(ReadOnlySpan<char> inputSpan, int index)
	{
		int num = index - 1;
		return ((uint)num < (uint)inputSpan.Length && RegexCharClass.IsECMAWordChar(inputSpan[num])) != ((uint)index < (uint)inputSpan.Length && RegexCharClass.IsECMAWordChar(inputSpan[index]));
	}

	[Obsolete("This API supports obsolete mechanisms for Regex extensibility. It is not supported.", DiagnosticId = "SYSLIB0052", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	protected static bool CharInSet(char ch, string set, string category)
	{
		string set2 = RegexCharClass.ConvertOldStringsToClass(set, category);
		return RegexCharClass.CharInClass(ch, set2);
	}

	public static bool CharInClass(char ch, string charClass)
	{
		return RegexCharClass.CharInClass(ch, charClass);
	}

	protected void DoubleTrack()
	{
		int[] destinationArray = new int[runtrack.Length * 2];
		Array.Copy(runtrack, 0, destinationArray, runtrack.Length, runtrack.Length);
		runtrackpos += runtrack.Length;
		runtrack = destinationArray;
	}

	protected void DoubleStack()
	{
		int[] destinationArray = new int[runstack.Length * 2];
		Array.Copy(runstack, 0, destinationArray, runstack.Length, runstack.Length);
		runstackpos += runstack.Length;
		runstack = destinationArray;
	}

	protected void DoubleCrawl()
	{
		int[] destinationArray = new int[runcrawl.Length * 2];
		Array.Copy(runcrawl, 0, destinationArray, runcrawl.Length, runcrawl.Length);
		runcrawlpos += runcrawl.Length;
		runcrawl = destinationArray;
	}

	protected void Crawl(int i)
	{
		if (runcrawlpos == 0)
		{
			DoubleCrawl();
		}
		runcrawl[--runcrawlpos] = i;
	}

	protected int Popcrawl()
	{
		return runcrawl[runcrawlpos++];
	}

	protected int Crawlpos()
	{
		return runcrawl.Length - runcrawlpos;
	}

	protected void Capture(int capnum, int start, int end)
	{
		if (end < start)
		{
			int num = end;
			end = start;
			start = num;
		}
		Crawl(capnum);
		runmatch.AddMatch(capnum, start, end - start);
	}

	protected void TransferCapture(int capnum, int uncapnum, int start, int end)
	{
		if (end < start)
		{
			int num = end;
			end = start;
			start = num;
		}
		int num2 = MatchIndex(uncapnum);
		int num3 = num2 + MatchLength(uncapnum);
		if (start >= num3)
		{
			end = start;
			start = num3;
		}
		else if (end <= num2)
		{
			start = num2;
		}
		else
		{
			if (end > num3)
			{
				end = num3;
			}
			if (num2 > start)
			{
				start = num2;
			}
		}
		Crawl(uncapnum);
		runmatch.BalanceMatch(uncapnum);
		if (capnum != -1)
		{
			Crawl(capnum);
			runmatch.AddMatch(capnum, start, end - start);
		}
	}

	protected void Uncapture()
	{
		int cap = Popcrawl();
		runmatch.RemoveMatch(cap);
	}

	protected bool IsMatched(int cap)
	{
		return runmatch.IsMatched(cap);
	}

	protected int MatchIndex(int cap)
	{
		return runmatch.MatchIndex(cap);
	}

	protected int MatchLength(int cap)
	{
		return runmatch.MatchLength(cap);
	}
}
