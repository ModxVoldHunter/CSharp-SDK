using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions.Symbolic;
using System.Threading;

namespace System.Text.RegularExpressions;

public class Regex : ISerializable
{
	public ref struct ValueMatchEnumerator
	{
		private readonly Regex _regex;

		private readonly ReadOnlySpan<char> _input;

		private ValueMatch _current;

		private int _startAt;

		private int _prevLen;

		public readonly ValueMatch Current => _current;

		internal ValueMatchEnumerator(Regex regex, ReadOnlySpan<char> input, int startAt)
		{
			_regex = regex;
			_input = input;
			_current = default(ValueMatch);
			_startAt = startAt;
			_prevLen = -1;
		}

		public readonly ValueMatchEnumerator GetEnumerator()
		{
			return this;
		}

		public bool MoveNext()
		{
			(bool, int, int, int) tuple = _regex.RunSingleMatch(RegexRunnerMode.BoundsRequired, _prevLen, _input, _startAt);
			if (tuple.Item1)
			{
				_current = new ValueMatch(tuple.Item2, tuple.Item3);
				_startAt = tuple.Item4;
				_prevLen = tuple.Item3;
				return true;
			}
			return false;
		}
	}

	[StringSyntax("Regex")]
	protected internal string? pattern;

	protected internal RegexOptions roptions;

	protected internal RegexRunnerFactory? factory;

	protected internal Hashtable? caps;

	protected internal Hashtable? capnames;

	protected internal string[]? capslist;

	protected internal int capsize;

	private WeakReference<RegexReplacement> _replref;

	private volatile RegexRunner _runner;

	public static readonly TimeSpan InfiniteMatchTimeout = Timeout.InfiniteTimeSpan;

	internal static readonly TimeSpan s_defaultMatchTimeout = InitDefaultMatchTimeout();

	protected internal TimeSpan internalMatchTimeout;

	[CLSCompliant(false)]
	protected IDictionary? Caps
	{
		get
		{
			return caps;
		}
		[param: DisallowNull]
		set
		{
			if (value == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
			}
			caps = (value as Hashtable) ?? new Hashtable(value);
		}
	}

	[CLSCompliant(false)]
	protected IDictionary? CapNames
	{
		get
		{
			return capnames;
		}
		[param: DisallowNull]
		set
		{
			if (value == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
			}
			capnames = (value as Hashtable) ?? new Hashtable(value);
		}
	}

	public RegexOptions Options => roptions;

	public bool RightToLeft => (roptions & RegexOptions.RightToLeft) != 0;

	internal WeakReference<RegexReplacement?> RegexReplacementWeakReference => _replref ?? Interlocked.CompareExchange(ref _replref, new WeakReference<RegexReplacement>(null), null) ?? _replref;

	public static int CacheSize
	{
		get
		{
			return RegexCache.MaxCacheSize;
		}
		set
		{
			if (value < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
			}
			RegexCache.MaxCacheSize = value;
		}
	}

	public TimeSpan MatchTimeout => internalMatchTimeout;

	protected Regex()
	{
		internalMatchTimeout = s_defaultMatchTimeout;
	}

	public Regex([StringSyntax("Regex")] string pattern)
		: this(pattern, null)
	{
	}

	public Regex([StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options)
		: this(pattern, options, s_defaultMatchTimeout, null)
	{
	}

	public Regex([StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options, TimeSpan matchTimeout)
		: this(pattern, options, matchTimeout, null)
	{
	}

	internal Regex(string pattern, CultureInfo culture)
	{
		ValidatePattern(pattern);
		RegexTree tree = Init(pattern, RegexOptions.None, s_defaultMatchTimeout, ref culture);
		factory = new RegexInterpreterFactory(tree);
	}

	[UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode", Justification = "Compiled Regex is only used when RuntimeFeature.IsDynamicCodeCompiled is true. Workaround https://github.com/dotnet/linker/issues/2715.")]
	internal Regex(string pattern, RegexOptions options, TimeSpan matchTimeout, CultureInfo culture)
	{
		ValidatePattern(pattern);
		ValidateOptions(options);
		ValidateMatchTimeout(matchTimeout);
		RegexTree regexTree = Init(pattern, options, matchTimeout, ref culture);
		if ((options & RegexOptions.NonBacktracking) != 0)
		{
			factory = new SymbolicRegexRunnerFactory(regexTree, options, matchTimeout);
			return;
		}
		if (RuntimeFeature.IsDynamicCodeCompiled && (options & RegexOptions.Compiled) != 0)
		{
			factory = Compile(pattern, regexTree, options, matchTimeout != InfiniteMatchTimeout);
		}
		if (factory == null)
		{
			factory = new RegexInterpreterFactory(regexTree);
		}
	}

	private RegexTree Init(string pattern, RegexOptions options, TimeSpan matchTimeout, [NotNull] ref CultureInfo culture)
	{
		this.pattern = pattern;
		roptions = options;
		internalMatchTimeout = matchTimeout;
		if (culture == null)
		{
			culture = RegexParser.GetTargetCulture(options);
		}
		RegexTree regexTree = RegexParser.Parse(pattern, options, culture);
		capnames = regexTree.CaptureNameToNumberMapping;
		capslist = regexTree.CaptureNames;
		caps = regexTree.CaptureNumberSparseMapping;
		capsize = regexTree.CaptureCount;
		return regexTree;
	}

	internal static void ValidatePattern(string pattern)
	{
		if (pattern == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.pattern);
		}
	}

	internal static void ValidateOptions(RegexOptions options)
	{
		if ((uint)options >> 11 != 0 || ((options & RegexOptions.ECMAScript) != 0 && ((uint)options & 0xFFFFFCF4u) != 0) || ((options & RegexOptions.NonBacktracking) != 0 && (options & (RegexOptions.RightToLeft | RegexOptions.ECMAScript)) != 0))
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.options);
		}
	}

	protected internal static void ValidateMatchTimeout(TimeSpan matchTimeout)
	{
		long ticks = matchTimeout.Ticks;
		if (ticks != -10000 && (ulong)(ticks - 1) >= 21474836460000uL)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.matchTimeout);
		}
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected Regex(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[RequiresDynamicCode("Compiling a RegEx requires dynamic code.")]
	private static RegexRunnerFactory Compile(string pattern, RegexTree regexTree, RegexOptions options, bool hasTimeout)
	{
		return RegexCompiler.Compile(pattern, regexTree, options, hasTimeout);
	}

	[Obsolete("Regex.CompileToAssembly is obsolete and not supported. Use the GeneratedRegexAttribute with the regular expression source generator instead.", DiagnosticId = "SYSLIB0036", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname)
	{
		CompileToAssembly(regexinfos, assemblyname, null, null);
	}

	[Obsolete("Regex.CompileToAssembly is obsolete and not supported. Use the GeneratedRegexAttribute with the regular expression source generator instead.", DiagnosticId = "SYSLIB0036", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname, CustomAttributeBuilder[]? attributes)
	{
		CompileToAssembly(regexinfos, assemblyname, attributes, null);
	}

	[Obsolete("Regex.CompileToAssembly is obsolete and not supported. Use the GeneratedRegexAttribute with the regular expression source generator instead.", DiagnosticId = "SYSLIB0036", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname, CustomAttributeBuilder[]? attributes, string? resourceFile)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CompileToAssembly);
	}

	public static string Escape(string str)
	{
		if (str == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.str);
		}
		return RegexParser.Escape(str);
	}

	public static string Unescape(string str)
	{
		if (str == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.str);
		}
		return RegexParser.Unescape(str);
	}

	public override string ToString()
	{
		return pattern;
	}

	public string[] GetGroupNames()
	{
		string[] array;
		if (capslist == null)
		{
			array = new string[capsize];
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array;
				int num = i;
				uint num2 = (uint)i;
				array2[num] = num2.ToString();
			}
		}
		else
		{
			array = capslist.AsSpan().ToArray();
		}
		return array;
	}

	public int[] GetGroupNumbers()
	{
		int[] array;
		if (caps == null)
		{
			array = new int[capsize];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = i;
			}
		}
		else
		{
			array = new int[caps.Count];
			IDictionaryEnumerator enumerator = caps.GetEnumerator();
			while (enumerator.MoveNext())
			{
				array[(int)enumerator.Value] = (int)enumerator.Key;
			}
			Array.Sort(array);
		}
		return array;
	}

	public string GroupNameFromNumber(int i)
	{
		return RegexParser.GroupNameFromNumber(caps, capslist, capsize, i);
	}

	public int GroupNumberFromName(string name)
	{
		if (name == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.name);
		}
		if (capnames != null)
		{
			if (!capnames.TryGetValue<int>(name, out var value))
			{
				return -1;
			}
			return value;
		}
		if (!uint.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out var result) || result >= capsize)
		{
			return -1;
		}
		return (int)result;
	}

	[Obsolete("This API supports obsolete mechanisms for Regex extensibility. It is not supported.", DiagnosticId = "SYSLIB0052", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected void InitializeReferences()
	{
	}

	internal Match RunSingleMatch(RegexRunnerMode mode, int prevlen, string input, int beginning, int length, int startat)
	{
		if ((uint)startat > (uint)input.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startat, ExceptionResource.BeginIndexNotNegative);
		}
		if ((uint)length > (uint)input.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length, ExceptionResource.LengthNotNegative);
		}
		RegexRunner regexRunner = Interlocked.Exchange(ref _runner, null) ?? CreateRunner();
		try
		{
			regexRunner.InitializeTimeout(internalMatchTimeout);
			regexRunner.runtext = input;
			ReadOnlySpan<char> readOnlySpan = input.AsSpan(beginning, length);
			regexRunner.InitializeForScan(this, readOnlySpan, startat - beginning, mode);
			if (prevlen == 0)
			{
				int num = readOnlySpan.Length;
				int num2 = 1;
				if (RightToLeft)
				{
					num = 0;
					num2 = -1;
				}
				if (regexRunner.runtextstart == num)
				{
					return System.Text.RegularExpressions.Match.Empty;
				}
				regexRunner.runtextpos += num2;
			}
			return ScanInternal(mode, mode == RegexRunnerMode.ExistenceRequired, input, beginning, regexRunner, readOnlySpan, returnNullIfReuseMatchObject: true);
		}
		finally
		{
			regexRunner.runtext = null;
			_runner = regexRunner;
		}
	}

	internal (bool Success, int Index, int Length, int TextPosition) RunSingleMatch(RegexRunnerMode mode, int prevlen, ReadOnlySpan<char> input, int startat)
	{
		RegexRunner regexRunner = Interlocked.Exchange(ref _runner, null) ?? CreateRunner();
		try
		{
			regexRunner.InitializeTimeout(internalMatchTimeout);
			regexRunner.InitializeForScan(this, input, startat, mode);
			if (prevlen == 0)
			{
				if (RightToLeft)
				{
					if (regexRunner.runtextstart == 0)
					{
						return (Success: false, Index: -1, Length: -1, TextPosition: -1);
					}
					regexRunner.runtextpos--;
				}
				else
				{
					if (regexRunner.runtextstart == input.Length)
					{
						return (Success: false, Index: -1, Length: -1, TextPosition: -1);
					}
					regexRunner.runtextpos++;
				}
			}
			regexRunner.Scan(input);
			Match runmatch = regexRunner.runmatch;
			if (runmatch.FoundMatch)
			{
				if (mode == RegexRunnerMode.ExistenceRequired)
				{
					return (Success: true, Index: -1, Length: -1, TextPosition: -1);
				}
				runmatch.Tidy(regexRunner.runtextpos, 0, mode);
				return (Success: true, Index: runmatch.Index, Length: runmatch.Length, TextPosition: runmatch._textpos);
			}
			return (Success: false, Index: -1, Length: -1, TextPosition: -1);
		}
		finally
		{
			_runner = regexRunner;
		}
	}

	internal void RunAllMatchesWithCallback<TState>(string input, int startat, ref TState state, MatchCallback<TState> callback, RegexRunnerMode mode, bool reuseMatchObject)
	{
		RunAllMatchesWithCallback(input, input, startat, ref state, callback, mode, reuseMatchObject);
	}

	internal void RunAllMatchesWithCallback<TState>(ReadOnlySpan<char> input, int startat, ref TState state, MatchCallback<TState> callback, RegexRunnerMode mode, bool reuseMatchObject)
	{
		RunAllMatchesWithCallback(null, input, startat, ref state, callback, mode, reuseMatchObject);
	}

	private void RunAllMatchesWithCallback<TState>(string inputString, ReadOnlySpan<char> inputSpan, int startat, ref TState state, MatchCallback<TState> callback, RegexRunnerMode mode, bool reuseMatchObject)
	{
		RegexRunner regexRunner = Interlocked.Exchange(ref _runner, null) ?? CreateRunner();
		try
		{
			regexRunner.runtext = inputString;
			regexRunner.InitializeTimeout(internalMatchTimeout);
			int num = startat;
			while (true)
			{
				regexRunner.InitializeForScan(this, inputSpan, startat, mode);
				regexRunner.runtextpos = num;
				Match match = ScanInternal(mode, reuseMatchObject, inputString, 0, regexRunner, inputSpan, returnNullIfReuseMatchObject: false);
				if (!match.Success)
				{
					break;
				}
				if (!reuseMatchObject)
				{
					regexRunner.runmatch = null;
				}
				if (!callback(ref state, match))
				{
					break;
				}
				num = (startat = regexRunner.runtextpos);
				if (match.Length == 0)
				{
					int num2 = inputSpan.Length;
					int num3 = 1;
					if (RightToLeft)
					{
						num2 = 0;
						num3 = -1;
					}
					if (num == num2)
					{
						break;
					}
					num += num3;
				}
				regexRunner.runtrackpos = regexRunner.runtrack.Length;
				regexRunner.runstackpos = regexRunner.runstack.Length;
				regexRunner.runcrawlpos = regexRunner.runcrawl.Length;
			}
		}
		finally
		{
			regexRunner.runtext = null;
			_runner = regexRunner;
		}
	}

	private static Match ScanInternal(RegexRunnerMode mode, bool reuseMatchObject, string input, int beginning, RegexRunner runner, ReadOnlySpan<char> span, bool returnNullIfReuseMatchObject)
	{
		runner.Scan(span);
		Match runmatch = runner.runmatch;
		if (runmatch.FoundMatch)
		{
			if (!reuseMatchObject)
			{
				runmatch.Text = input;
				runner.runmatch = null;
			}
			else if (returnNullIfReuseMatchObject)
			{
				runmatch.Text = null;
				return null;
			}
			runmatch.Tidy(runner.runtextpos, beginning, mode);
			return runmatch;
		}
		runmatch.Text = null;
		return System.Text.RegularExpressions.Match.Empty;
	}

	private RegexRunner CreateRunner()
	{
		return factory.CreateInstance();
	}

	[Obsolete("This API supports obsolete mechanisms for Regex extensibility. It is not supported.", DiagnosticId = "SYSLIB0052", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected bool UseOptionC()
	{
		return (roptions & RegexOptions.Compiled) != 0;
	}

	[Obsolete("This API supports obsolete mechanisms for Regex extensibility. It is not supported.", DiagnosticId = "SYSLIB0052", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected bool UseOptionR()
	{
		return RightToLeft;
	}

	public int Count(string input)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		int state = 0;
		RunAllMatchesWithCallback(input, RightToLeft ? input.Length : 0, ref state, delegate(ref int count, Match match)
		{
			count++;
			return true;
		}, RegexRunnerMode.BoundsRequired, reuseMatchObject: true);
		return state;
	}

	public int Count(ReadOnlySpan<char> input)
	{
		return Count(input, RightToLeft ? input.Length : 0);
	}

	public int Count(ReadOnlySpan<char> input, int startat)
	{
		int state = 0;
		RunAllMatchesWithCallback(input, startat, ref state, delegate(ref int count, Match match)
		{
			count++;
			return true;
		}, RegexRunnerMode.BoundsRequired, reuseMatchObject: true);
		return state;
	}

	public static int Count(string input, [StringSyntax("Regex")] string pattern)
	{
		return RegexCache.GetOrAdd(pattern).Count(input);
	}

	public static int Count(string input, [StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options)
	{
		return RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Count(input);
	}

	public static int Count(string input, [StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options, TimeSpan matchTimeout)
	{
		return RegexCache.GetOrAdd(pattern, options, matchTimeout).Count(input);
	}

	public static int Count(ReadOnlySpan<char> input, [StringSyntax("Regex")] string pattern)
	{
		return RegexCache.GetOrAdd(pattern).Count(input);
	}

	public static int Count(ReadOnlySpan<char> input, [StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options)
	{
		return RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Count(input);
	}

	public static int Count(ReadOnlySpan<char> input, [StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options, TimeSpan matchTimeout)
	{
		return RegexCache.GetOrAdd(pattern, options, matchTimeout).Count(input);
	}

	public static bool IsMatch(string input, [StringSyntax("Regex")] string pattern)
	{
		return RegexCache.GetOrAdd(pattern).IsMatch(input);
	}

	public static bool IsMatch(ReadOnlySpan<char> input, [StringSyntax("Regex")] string pattern)
	{
		return RegexCache.GetOrAdd(pattern).IsMatch(input);
	}

	public static bool IsMatch(string input, [StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options)
	{
		return RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).IsMatch(input);
	}

	public static bool IsMatch(ReadOnlySpan<char> input, [StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options)
	{
		return RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).IsMatch(input);
	}

	public static bool IsMatch(string input, [StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options, TimeSpan matchTimeout)
	{
		return RegexCache.GetOrAdd(pattern, options, matchTimeout).IsMatch(input);
	}

	public static bool IsMatch(ReadOnlySpan<char> input, [StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options, TimeSpan matchTimeout)
	{
		return RegexCache.GetOrAdd(pattern, options, matchTimeout).IsMatch(input);
	}

	public bool IsMatch(string input)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return RunSingleMatch(RegexRunnerMode.ExistenceRequired, -1, input, 0, input.Length, RightToLeft ? input.Length : 0) == null;
	}

	public bool IsMatch(string input, int startat)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return RunSingleMatch(RegexRunnerMode.ExistenceRequired, -1, input, 0, input.Length, startat) == null;
	}

	public bool IsMatch(ReadOnlySpan<char> input)
	{
		return IsMatch(input, RightToLeft ? input.Length : 0);
	}

	public bool IsMatch(ReadOnlySpan<char> input, int startat)
	{
		return RunSingleMatch(RegexRunnerMode.ExistenceRequired, -1, input, startat).Success;
	}

	public static Match Match(string input, [StringSyntax("Regex")] string pattern)
	{
		return RegexCache.GetOrAdd(pattern).Match(input);
	}

	public static Match Match(string input, [StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options)
	{
		return RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Match(input);
	}

	public static Match Match(string input, [StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options, TimeSpan matchTimeout)
	{
		return RegexCache.GetOrAdd(pattern, options, matchTimeout).Match(input);
	}

	public Match Match(string input)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return RunSingleMatch(RegexRunnerMode.FullMatchRequired, -1, input, 0, input.Length, RightToLeft ? input.Length : 0);
	}

	public Match Match(string input, int startat)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return RunSingleMatch(RegexRunnerMode.FullMatchRequired, -1, input, 0, input.Length, startat);
	}

	public Match Match(string input, int beginning, int length)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return RunSingleMatch(RegexRunnerMode.FullMatchRequired, -1, input, beginning, length, RightToLeft ? (beginning + length) : beginning);
	}

	public static MatchCollection Matches(string input, [StringSyntax("Regex")] string pattern)
	{
		return RegexCache.GetOrAdd(pattern).Matches(input);
	}

	public static MatchCollection Matches(string input, [StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options)
	{
		return RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Matches(input);
	}

	public static MatchCollection Matches(string input, [StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options, TimeSpan matchTimeout)
	{
		return RegexCache.GetOrAdd(pattern, options, matchTimeout).Matches(input);
	}

	public MatchCollection Matches(string input)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return new MatchCollection(this, input, RightToLeft ? input.Length : 0);
	}

	public MatchCollection Matches(string input, int startat)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return new MatchCollection(this, input, startat);
	}

	public static string Replace(string input, [StringSyntax("Regex")] string pattern, string replacement)
	{
		return RegexCache.GetOrAdd(pattern).Replace(input, replacement);
	}

	public static string Replace(string input, [StringSyntax("Regex", new object[] { "options" })] string pattern, string replacement, RegexOptions options)
	{
		return RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Replace(input, replacement);
	}

	public static string Replace(string input, [StringSyntax("Regex", new object[] { "options" })] string pattern, string replacement, RegexOptions options, TimeSpan matchTimeout)
	{
		return RegexCache.GetOrAdd(pattern, options, matchTimeout).Replace(input, replacement);
	}

	public string Replace(string input, string replacement)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Replace(input, replacement, -1, RightToLeft ? input.Length : 0);
	}

	public string Replace(string input, string replacement, int count)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Replace(input, replacement, count, RightToLeft ? input.Length : 0);
	}

	public string Replace(string input, string replacement, int count, int startat)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		if (replacement == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.replacement);
		}
		return RegexReplacement.GetOrCreate(RegexReplacementWeakReference, replacement, caps, capsize, capnames, roptions).Replace(this, input, count, startat);
	}

	public static string Replace(string input, [StringSyntax("Regex")] string pattern, MatchEvaluator evaluator)
	{
		return RegexCache.GetOrAdd(pattern).Replace(input, evaluator);
	}

	public static string Replace(string input, [StringSyntax("Regex", new object[] { "options" })] string pattern, MatchEvaluator evaluator, RegexOptions options)
	{
		return RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Replace(input, evaluator);
	}

	public static string Replace(string input, [StringSyntax("Regex", new object[] { "options" })] string pattern, MatchEvaluator evaluator, RegexOptions options, TimeSpan matchTimeout)
	{
		return RegexCache.GetOrAdd(pattern, options, matchTimeout).Replace(input, evaluator);
	}

	public string Replace(string input, MatchEvaluator evaluator)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Replace(evaluator, this, input, -1, RightToLeft ? input.Length : 0);
	}

	public string Replace(string input, MatchEvaluator evaluator, int count)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Replace(evaluator, this, input, count, RightToLeft ? input.Length : 0);
	}

	public string Replace(string input, MatchEvaluator evaluator, int count, int startat)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Replace(evaluator, this, input, count, startat);
	}

	private static string Replace(MatchEvaluator evaluator, Regex regex, string input, int count, int startat)
	{
		if (evaluator == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.evaluator);
		}
		if (count < -1)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.CountTooSmall);
		}
		if ((uint)startat > (uint)input.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startat, ExceptionResource.BeginIndexNotNegative);
		}
		if (count == 0)
		{
			return input;
		}
		(StructListBuilder<ReadOnlyMemory<char>>, MatchEvaluator, int, string, int) state2 = (new StructListBuilder<ReadOnlyMemory<char>>(), evaluator, 0, input, count);
		if (!regex.RightToLeft)
		{
			regex.RunAllMatchesWithCallback<(StructListBuilder<ReadOnlyMemory<char>>, MatchEvaluator, int, string, int)>(input, startat, ref state2, delegate(ref (StructListBuilder<ReadOnlyMemory<char>> segments, MatchEvaluator evaluator, int prevat, string input, int count) state, Match match)
			{
				state.segments.Add(state.input.AsMemory(state.prevat, match.Index - state.prevat));
				state.prevat = match.Index + match.Length;
				state.segments.Add(state.evaluator(match).AsMemory());
				return --state.count != 0;
			}, RegexRunnerMode.FullMatchRequired, reuseMatchObject: false);
			if (state2.Item1.Count == 0)
			{
				return input;
			}
			state2.Item1.Add(input.AsMemory(state2.Item3, input.Length - state2.Item3));
		}
		else
		{
			state2.Item3 = input.Length;
			regex.RunAllMatchesWithCallback<(StructListBuilder<ReadOnlyMemory<char>>, MatchEvaluator, int, string, int)>(input, startat, ref state2, delegate(ref (StructListBuilder<ReadOnlyMemory<char>> segments, MatchEvaluator evaluator, int prevat, string input, int count) state, Match match)
			{
				state.segments.Add(state.input.AsMemory(match.Index + match.Length, state.prevat - match.Index - match.Length));
				state.prevat = match.Index;
				state.segments.Add(state.evaluator(match).AsMemory());
				return --state.count != 0;
			}, RegexRunnerMode.FullMatchRequired, reuseMatchObject: false);
			if (state2.Item1.Count == 0)
			{
				return input;
			}
			state2.Item1.Add(input.AsMemory(0, state2.Item3));
			state2.Item1.AsSpan().Reverse();
		}
		return SegmentsToStringAndDispose(ref state2.Item1);
	}

	internal unsafe static string SegmentsToStringAndDispose(ref StructListBuilder<ReadOnlyMemory<char>> segments)
	{
		Span<ReadOnlyMemory<char>> span = segments.AsSpan();
		int num = 0;
		for (int i = 0; i < span.Length; i++)
		{
			num += span[i].Length;
		}
		ReadOnlySpan<ReadOnlyMemory<char>> readOnlySpan = span;
		string result = string.Create(num, (nint)(&readOnlySpan), delegate(Span<char> dest, nint spanPtr)
		{
			Span<ReadOnlyMemory<char>> span2 = Unsafe.Read<Span<ReadOnlyMemory<char>>>((void*)spanPtr);
			for (int j = 0; j < span2.Length; j++)
			{
				ReadOnlySpan<char> span3 = span2[j].Span;
				span3.CopyTo(dest);
				dest = dest.Slice(span3.Length);
			}
		});
		segments.Dispose();
		return result;
	}

	public static string[] Split(string input, [StringSyntax("Regex")] string pattern)
	{
		return RegexCache.GetOrAdd(pattern).Split(input);
	}

	public static string[] Split(string input, [StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options)
	{
		return RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Split(input);
	}

	public static string[] Split(string input, [StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options, TimeSpan matchTimeout)
	{
		return RegexCache.GetOrAdd(pattern, options, matchTimeout).Split(input);
	}

	public string[] Split(string input)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Split(this, input, 0, RightToLeft ? input.Length : 0);
	}

	public string[] Split(string input, int count)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Split(this, input, count, RightToLeft ? input.Length : 0);
	}

	public string[] Split(string input, int count, int startat)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Split(this, input, count, startat);
	}

	private static string[] Split(Regex regex, string input, int count, int startat)
	{
		if (count < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.CountTooSmall);
		}
		if ((uint)startat > (uint)input.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startat, ExceptionResource.BeginIndexNotNegative);
		}
		if (count == 1)
		{
			return new string[1] { input };
		}
		count--;
		(List<string>, int, string, int) state2 = (new List<string>(), 0, input, count);
		if (!regex.RightToLeft)
		{
			regex.RunAllMatchesWithCallback<(List<string>, int, string, int)>(input, startat, ref state2, delegate(ref (List<string> results, int prevat, string input, int count) state, Match match)
			{
				state.results.Add(state.input.Substring(state.prevat, match.Index - state.prevat));
				state.prevat = match.Index + match.Length;
				for (int j = 1; j < match.Groups.Count; j++)
				{
					if (match.IsMatched(j))
					{
						state.results.Add(match.Groups[j].Value);
					}
				}
				return --state.count != 0;
			}, RegexRunnerMode.FullMatchRequired, reuseMatchObject: true);
			if (state2.Item1.Count == 0)
			{
				return new string[1] { input };
			}
			state2.Item1.Add(input.Substring(state2.Item2));
		}
		else
		{
			state2.Item2 = input.Length;
			regex.RunAllMatchesWithCallback<(List<string>, int, string, int)>(input, startat, ref state2, delegate(ref (List<string> results, int prevat, string input, int count) state, Match match)
			{
				state.results.Add(state.input.Substring(match.Index + match.Length, state.prevat - match.Index - match.Length));
				state.prevat = match.Index;
				for (int i = 1; i < match.Groups.Count; i++)
				{
					if (match.IsMatched(i))
					{
						state.results.Add(match.Groups[i].Value);
					}
				}
				return --state.count != 0;
			}, RegexRunnerMode.FullMatchRequired, reuseMatchObject: true);
			if (state2.Item1.Count == 0)
			{
				return new string[1] { input };
			}
			state2.Item1.Add(input.Substring(0, state2.Item2));
			state2.Item1.Reverse(0, state2.Item1.Count);
		}
		return state2.Item1.ToArray();
	}

	public static ValueMatchEnumerator EnumerateMatches(ReadOnlySpan<char> input, [StringSyntax("Regex")] string pattern)
	{
		return RegexCache.GetOrAdd(pattern).EnumerateMatches(input);
	}

	public static ValueMatchEnumerator EnumerateMatches(ReadOnlySpan<char> input, [StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options)
	{
		return RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).EnumerateMatches(input);
	}

	public static ValueMatchEnumerator EnumerateMatches(ReadOnlySpan<char> input, [StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options, TimeSpan matchTimeout)
	{
		return RegexCache.GetOrAdd(pattern, options, matchTimeout).EnumerateMatches(input);
	}

	public ValueMatchEnumerator EnumerateMatches(ReadOnlySpan<char> input)
	{
		return new ValueMatchEnumerator(this, input, RightToLeft ? input.Length : 0);
	}

	public ValueMatchEnumerator EnumerateMatches(ReadOnlySpan<char> input, int startat)
	{
		return new ValueMatchEnumerator(this, input, startat);
	}

	private static TimeSpan InitDefaultMatchTimeout()
	{
		object data = AppContext.GetData("REGEX_DEFAULT_MATCH_TIMEOUT");
		if (!(data is TimeSpan timeSpan))
		{
			if (data == null)
			{
				return InfiniteMatchTimeout;
			}
			throw new InvalidCastException(System.SR.Format(System.SR.IllegalDefaultRegexMatchTimeoutInAppDomain, "REGEX_DEFAULT_MATCH_TIMEOUT", data));
		}
		try
		{
			ValidateMatchTimeout(timeSpan);
			return timeSpan;
		}
		catch (ArgumentOutOfRangeException)
		{
			throw new ArgumentOutOfRangeException(System.SR.Format(System.SR.IllegalDefaultRegexMatchTimeoutInAppDomain, "REGEX_DEFAULT_MATCH_TIMEOUT", timeSpan));
		}
	}
}
