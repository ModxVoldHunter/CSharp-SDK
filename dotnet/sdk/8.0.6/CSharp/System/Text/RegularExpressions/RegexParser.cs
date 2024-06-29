using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace System.Text.RegularExpressions;

internal ref struct RegexParser
{
	private RegexNode _stack;

	private RegexNode _group;

	private RegexNode _alternation;

	private RegexNode _concatenation;

	private RegexNode _unit;

	private readonly string _pattern;

	private int _pos;

	private readonly CultureInfo _culture;

	private RegexCaseBehavior _caseBehavior;

	private bool _hasIgnoreCaseBackreferenceNodes;

	private int _autocap;

	private int _capcount;

	private int _captop;

	private readonly int _capsize;

	private readonly Hashtable _caps;

	private Hashtable _capnames;

	private int[] _capnumlist;

	private List<string> _capnamelist;

	private RegexOptions _options;

	private System.Collections.Generic.ValueListBuilder<int> _optionsStack;

	private bool _ignoreNextParen;

	private static readonly SearchValues<char> s_metachars = SearchValues.Create("\t\n\f\r #$()*+.?[\\^{|");

	private static ReadOnlySpan<byte> Category => new byte[128]
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 1,
		1, 0, 1, 1, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 1, 0, 0, 2, 3, 0, 0, 0,
		3, 3, 4, 4, 0, 0, 3, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 4, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 3, 3, 0, 3, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 4, 3, 0, 0, 0
	};

	private RegexParser(string pattern, RegexOptions options, CultureInfo culture, Hashtable caps, int capsize, Hashtable capnames, Span<int> optionSpan)
	{
		_pattern = pattern;
		_options = options;
		_culture = culture;
		_caseBehavior = RegexCaseBehavior.NotSet;
		_hasIgnoreCaseBackreferenceNodes = false;
		_caps = caps;
		_capsize = capsize;
		_capnames = capnames;
		_optionsStack = new System.Collections.Generic.ValueListBuilder<int>(optionSpan);
		_stack = null;
		_group = null;
		_alternation = null;
		_concatenation = null;
		_unit = null;
		_pos = 0;
		_autocap = 0;
		_capcount = 0;
		_captop = 0;
		_capnumlist = null;
		_capnamelist = null;
		_ignoreNextParen = false;
	}

	internal static CultureInfo GetTargetCulture(RegexOptions options)
	{
		if ((options & RegexOptions.CultureInvariant) == 0)
		{
			return CultureInfo.CurrentCulture;
		}
		return CultureInfo.InvariantCulture;
	}

	public static RegexTree Parse(string pattern, RegexOptions options, CultureInfo culture)
	{
		Hashtable caps = new Hashtable();
		Span<int> optionSpan = stackalloc int[32];
		RegexParser regexParser = new RegexParser(pattern, options, culture, caps, 0, null, optionSpan);
		try
		{
			regexParser.CountCaptures(out var _);
			regexParser.Reset(options);
			RegexNode root = regexParser.ScanRegex();
			int[] capnumlist = regexParser._capnumlist;
			Hashtable hashtable = regexParser._caps;
			int captop = regexParser._captop;
			int captureCount;
			if (capnumlist == null || captop == capnumlist.Length)
			{
				captureCount = captop;
				hashtable = null;
			}
			else
			{
				captureCount = capnumlist.Length;
				for (int i = 0; i < capnumlist.Length; i++)
				{
					hashtable[capnumlist[i]] = i;
				}
			}
			return new RegexTree(root, captureCount, regexParser._capnamelist?.ToArray(), regexParser._capnames, hashtable, options, regexParser._hasIgnoreCaseBackreferenceNodes ? culture : null);
		}
		finally
		{
			regexParser.Dispose();
		}
	}

	public static RegexReplacement ParseReplacement(string pattern, RegexOptions options, Hashtable caps, int capsize, Hashtable capnames)
	{
		CultureInfo cultureInfo = (((options & RegexOptions.CultureInvariant) != 0) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
		CultureInfo culture = cultureInfo;
		Span<int> optionSpan = stackalloc int[32];
		using RegexParser regexParser = new RegexParser(pattern, options, culture, caps, capsize, capnames, optionSpan);
		RegexNode concat = regexParser.ScanReplacement();
		return new RegexReplacement(pattern, concat, caps);
	}

	public static string Escape(string input)
	{
		int num = IndexOfMetachar(input.AsSpan());
		if (num >= 0)
		{
			return EscapeImpl(input.AsSpan(), num);
		}
		return input;
	}

	private static string EscapeImpl(ReadOnlySpan<char> input, int indexOfMetachar)
	{
		System.Text.ValueStringBuilder valueStringBuilder;
		if (input.Length <= 85)
		{
			Span<char> initialBuffer = stackalloc char[256];
			valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		}
		else
		{
			valueStringBuilder = new System.Text.ValueStringBuilder(input.Length + 200);
		}
		System.Text.ValueStringBuilder valueStringBuilder2 = valueStringBuilder;
		while (true)
		{
			valueStringBuilder2.Append(input.Slice(0, indexOfMetachar));
			input = input.Slice(indexOfMetachar);
			if (input.IsEmpty)
			{
				break;
			}
			char c = input[0];
			switch (c)
			{
			case '\n':
				c = 'n';
				break;
			case '\r':
				c = 'r';
				break;
			case '\t':
				c = 't';
				break;
			case '\f':
				c = 'f';
				break;
			}
			valueStringBuilder2.Append('\\');
			valueStringBuilder2.Append(c);
			input = input.Slice(1);
			indexOfMetachar = IndexOfMetachar(input);
			if (indexOfMetachar < 0)
			{
				indexOfMetachar = input.Length;
			}
		}
		return valueStringBuilder2.ToString();
	}

	public static string Unescape(string input)
	{
		int num = input.IndexOf('\\');
		if (num < 0)
		{
			return input;
		}
		return UnescapeImpl(input, num);
	}

	private static string UnescapeImpl(string input, int i)
	{
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		Hashtable caps = new Hashtable();
		Span<int> optionSpan = stackalloc int[32];
		RegexParser regexParser = new RegexParser(input, RegexOptions.None, invariantCulture, caps, 0, null, optionSpan);
		System.Text.ValueStringBuilder valueStringBuilder;
		if (input.Length <= 256)
		{
			Span<char> initialBuffer = stackalloc char[256];
			valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		}
		else
		{
			valueStringBuilder = new System.Text.ValueStringBuilder(input.Length);
		}
		System.Text.ValueStringBuilder valueStringBuilder2 = valueStringBuilder;
		valueStringBuilder2.Append(input.AsSpan(0, i));
		do
		{
			i++;
			regexParser._pos = i;
			if (i < input.Length)
			{
				valueStringBuilder2.Append(regexParser.ScanCharEscape());
			}
			i = regexParser._pos;
			int num = i;
			while (i < input.Length && input[i] != '\\')
			{
				i++;
			}
			valueStringBuilder2.Append(input.AsSpan(num, i - num));
		}
		while (i < input.Length);
		regexParser.Dispose();
		return valueStringBuilder2.ToString();
	}

	private void Reset(RegexOptions options)
	{
		_pos = 0;
		_autocap = 1;
		_ignoreNextParen = false;
		_optionsStack.Length = 0;
		_options = options;
		_stack = null;
	}

	public void Dispose()
	{
		_optionsStack.Dispose();
	}

	private RegexNode ScanRegex()
	{
		bool flag = false;
		StartGroup(new RegexNode(RegexNodeKind.Capture, _options & ~RegexOptions.IgnoreCase, 0, -1));
		while (_pos < _pattern.Length)
		{
			bool flag2 = flag;
			flag = false;
			ScanBlank();
			int pos = _pos;
			char c;
			if ((_options & RegexOptions.IgnorePatternWhitespace) != 0)
			{
				while (_pos < _pattern.Length && (!IsSpecialOrSpace(c = _pattern[_pos]) || (c == '{' && !IsTrueQuantifier())))
				{
					_pos++;
				}
			}
			else
			{
				while (_pos < _pattern.Length && (!IsSpecial(c = _pattern[_pos]) || (c == '{' && !IsTrueQuantifier())))
				{
					_pos++;
				}
			}
			int pos2 = _pos;
			ScanBlank();
			if (_pos == _pattern.Length)
			{
				c = '!';
			}
			else if (IsSpecial(c = _pattern[_pos]))
			{
				flag = IsQuantifier(c);
				_pos++;
			}
			else
			{
				c = ' ';
			}
			if (pos < pos2)
			{
				int num = pos2 - pos - (flag ? 1 : 0);
				flag2 = false;
				if (num > 0)
				{
					AddToConcatenate(pos, num, isReplacement: false);
				}
				if (flag)
				{
					_unit = RegexNode.CreateOneWithCaseConversion(_pattern[pos2 - 1], _options, _culture, ref _caseBehavior);
				}
			}
			switch (c)
			{
			case '[':
			{
				string str = ScanCharClass((_options & RegexOptions.IgnoreCase) != 0, scanOnly: false).ToStringClass();
				_unit = new RegexNode(RegexNodeKind.Set, _options & ~RegexOptions.IgnoreCase, str);
				goto default;
			}
			case '(':
			{
				_optionsStack.Append((int)_options);
				RegexNode regexNode = ScanGroupOpen();
				if (regexNode != null)
				{
					PushGroup();
					StartGroup(regexNode);
				}
				else
				{
					_optionsStack.Length--;
				}
				continue;
			}
			case '|':
				AddAlternate();
				continue;
			case ')':
				if (_stack == null)
				{
					throw MakeException(RegexParseError.InsufficientOpeningParentheses, System.SR.InsufficientOpeningParentheses);
				}
				AddGroup();
				PopGroup();
				_options = (RegexOptions)_optionsStack.Pop();
				if (_unit == null)
				{
					continue;
				}
				goto default;
			case '\\':
				if (_pos == _pattern.Length)
				{
					throw MakeException(RegexParseError.UnescapedEndingBackslash, System.SR.UnescapedEndingBackslash);
				}
				_unit = ScanBackslash(scanOnly: false);
				goto default;
			case '^':
				_unit = new RegexNode(((_options & RegexOptions.Multiline) != 0) ? RegexNodeKind.Bol : RegexNodeKind.Beginning, _options);
				goto default;
			case '$':
				_unit = new RegexNode(((_options & RegexOptions.Multiline) != 0) ? RegexNodeKind.Eol : RegexNodeKind.EndZ, _options);
				goto default;
			case '.':
				_unit = (((_options & RegexOptions.Singleline) != 0) ? new RegexNode(RegexNodeKind.Set, _options & ~RegexOptions.IgnoreCase, "\0\u0001\0\0") : new RegexNode(RegexNodeKind.Notone, _options & ~RegexOptions.IgnoreCase, '\n'));
				goto default;
			case '*':
			case '+':
			case '?':
			case '{':
				if (_unit == null)
				{
					throw flag2 ? MakeException(RegexParseError.NestedQuantifiersNotParenthesized, System.SR.Format(System.SR.NestedQuantifiersNotParenthesized, c)) : MakeException(RegexParseError.QuantifierAfterNothing, System.SR.Format(System.SR.QuantifierAfterNothing, c));
				}
				_pos--;
				goto default;
			default:
				ScanBlank();
				if (_pos == _pattern.Length || !(flag = IsTrueQuantifier()))
				{
					_concatenation.AddChild(_unit);
					_unit = null;
					continue;
				}
				c = _pattern[_pos++];
				while (_unit != null)
				{
					int num2 = 0;
					int num3 = 0;
					if ((uint)c <= 43u)
					{
						switch (c)
						{
						case '*':
							num3 = int.MaxValue;
							break;
						case '+':
							num2 = 1;
							num3 = int.MaxValue;
							break;
						}
					}
					else if (c != '?')
					{
						if (c == '{')
						{
							pos = _pos;
							num3 = (num2 = ScanDecimal());
							if (pos < _pos && _pos < _pattern.Length && _pattern[_pos] == ',')
							{
								_pos++;
								num3 = ((_pos == _pattern.Length || _pattern[_pos] == '}') ? int.MaxValue : ScanDecimal());
							}
							if (pos == _pos || _pos == _pattern.Length || _pattern[_pos++] != '}')
							{
								_concatenation.AddChild(_unit);
								_unit = null;
								_pos = pos - 1;
								break;
							}
						}
					}
					else
					{
						num3 = 1;
					}
					ScanBlank();
					bool lazy = false;
					if (_pos < _pattern.Length && _pattern[_pos] == '?')
					{
						_pos++;
						lazy = true;
					}
					if (num2 > num3)
					{
						throw MakeException(RegexParseError.ReversedQuantifierRange, System.SR.ReversedQuantifierRange);
					}
					_concatenation.AddChild(_unit.MakeQuantifier(lazy, num2, num3));
					_unit = null;
				}
				continue;
			case ' ':
				continue;
			case '!':
				break;
			}
			break;
		}
		if (_stack != null)
		{
			throw MakeException(RegexParseError.InsufficientClosingParentheses, System.SR.InsufficientClosingParentheses);
		}
		AddGroup();
		return _unit.FinalOptimize();
	}

	private RegexNode ScanReplacement()
	{
		_concatenation = new RegexNode(RegexNodeKind.Concatenate, _options);
		while (_pos < _pattern.Length)
		{
			int pos = _pos;
			_pos = _pattern.IndexOf('$', _pos);
			if (_pos < 0)
			{
				_pos = _pattern.Length;
			}
			AddToConcatenate(pos, _pos - pos, isReplacement: true);
			if (_pos < _pattern.Length)
			{
				_pos++;
				_concatenation.AddChild(ScanDollar());
				_unit = null;
			}
		}
		return _concatenation;
	}

	private RegexCharClass ScanCharClass(bool caseInsensitive, bool scanOnly)
	{
		char c = '\0';
		bool flag = false;
		bool flag2 = true;
		bool flag3 = false;
		RegexCharClass regexCharClass = (scanOnly ? null : new RegexCharClass());
		if (_pos < _pattern.Length && _pattern[_pos] == '^')
		{
			_pos++;
			if (!scanOnly)
			{
				regexCharClass.Negate = true;
			}
			if ((_options & RegexOptions.ECMAScript) != 0 && _pattern[_pos] == ']')
			{
				flag2 = false;
			}
		}
		for (; _pos < _pattern.Length; flag2 = false)
		{
			bool flag4 = false;
			char c2 = _pattern[_pos++];
			if (c2 == ']')
			{
				if (!flag2)
				{
					flag3 = true;
					break;
				}
			}
			else if (c2 == '\\' && _pos < _pattern.Length)
			{
				switch (c2 = _pattern[_pos++])
				{
				case 'D':
				case 'd':
					if (!scanOnly)
					{
						if (flag)
						{
							throw MakeException(RegexParseError.ShorthandClassInCharacterRange, System.SR.Format(System.SR.ShorthandClassInCharacterRange, c2));
						}
						regexCharClass.AddDigit((_options & RegexOptions.ECMAScript) != 0, c2 == 'D', _pattern, _pos);
					}
					continue;
				case 'S':
				case 's':
					if (!scanOnly)
					{
						if (flag)
						{
							throw MakeException(RegexParseError.ShorthandClassInCharacterRange, System.SR.Format(System.SR.ShorthandClassInCharacterRange, c2));
						}
						regexCharClass.AddSpace((_options & RegexOptions.ECMAScript) != 0, c2 == 'S');
					}
					continue;
				case 'W':
				case 'w':
					if (!scanOnly)
					{
						if (flag)
						{
							throw MakeException(RegexParseError.ShorthandClassInCharacterRange, System.SR.Format(System.SR.ShorthandClassInCharacterRange, c2));
						}
						regexCharClass.AddWord((_options & RegexOptions.ECMAScript) != 0, c2 == 'W');
					}
					continue;
				case 'P':
				case 'p':
					if (!scanOnly)
					{
						if (flag)
						{
							throw MakeException(RegexParseError.ShorthandClassInCharacterRange, System.SR.Format(System.SR.ShorthandClassInCharacterRange, c2));
						}
						regexCharClass.AddCategoryFromName(ParseProperty(), c2 != 'p', caseInsensitive, _pattern, _pos);
					}
					else
					{
						ParseProperty();
					}
					continue;
				case '-':
					if (scanOnly)
					{
						continue;
					}
					if (flag)
					{
						if (c > c2)
						{
							throw MakeException(RegexParseError.ReversedCharacterRange, System.SR.ReversedCharacterRange);
						}
						regexCharClass.AddRange(c, c2);
						flag = false;
						c = '\0';
					}
					else
					{
						regexCharClass.AddRange(c2, c2);
					}
					continue;
				}
				_pos--;
				c2 = ScanCharEscape();
				flag4 = true;
			}
			if (flag)
			{
				flag = false;
				if (scanOnly)
				{
					continue;
				}
				if (c2 == '[' && !flag4 && !flag2)
				{
					regexCharClass.AddChar(c);
					regexCharClass.AddSubtraction(ScanCharClass(caseInsensitive, scanOnly));
					if (_pos < _pattern.Length && _pattern[_pos] != ']')
					{
						throw MakeException(RegexParseError.ExclusionGroupNotLast, System.SR.ExclusionGroupNotLast);
					}
				}
				else
				{
					if (c > c2)
					{
						throw MakeException(RegexParseError.ReversedCharacterRange, System.SR.ReversedCharacterRange);
					}
					regexCharClass.AddRange(c, c2);
				}
			}
			else if (_pos + 1 < _pattern.Length && _pattern[_pos] == '-' && _pattern[_pos + 1] != ']')
			{
				c = c2;
				flag = true;
				_pos++;
			}
			else if (_pos < _pattern.Length && c2 == '-' && !flag4 && _pattern[_pos] == '[' && !flag2)
			{
				_pos++;
				RegexCharClass sub = ScanCharClass(caseInsensitive, scanOnly);
				if (!scanOnly)
				{
					regexCharClass.AddSubtraction(sub);
					if (_pos < _pattern.Length && _pattern[_pos] != ']')
					{
						throw MakeException(RegexParseError.ExclusionGroupNotLast, System.SR.ExclusionGroupNotLast);
					}
				}
			}
			else if (!scanOnly)
			{
				regexCharClass.AddRange(c2, c2);
			}
		}
		if (!flag3)
		{
			throw MakeException(RegexParseError.UnterminatedBracket, System.SR.UnterminatedBracket);
		}
		if (!scanOnly && caseInsensitive)
		{
			regexCharClass.AddCaseEquivalences(_culture);
		}
		return regexCharClass;
	}

	private RegexNode ScanGroupOpen()
	{
		if (_pos == _pattern.Length || _pattern[_pos] != '?' || (_pos + 1 < _pattern.Length && _pattern[_pos + 1] == ')'))
		{
			if ((_options & RegexOptions.ExplicitCapture) != 0 || _ignoreNextParen)
			{
				_ignoreNextParen = false;
				return new RegexNode(RegexNodeKind.Group, _options);
			}
			return new RegexNode(RegexNodeKind.Capture, _options, _autocap++, -1);
		}
		_pos++;
		if (_pos != _pattern.Length)
		{
			char c = '>';
			RegexNodeKind kind;
			switch (_pattern[_pos++])
			{
			case ':':
				kind = RegexNodeKind.Group;
				goto IL_0767;
			case '=':
				_options &= ~RegexOptions.RightToLeft;
				kind = RegexNodeKind.PositiveLookaround;
				goto IL_0767;
			case '!':
				_options &= ~RegexOptions.RightToLeft;
				kind = RegexNodeKind.NegativeLookaround;
				goto IL_0767;
			case '>':
				kind = RegexNodeKind.Atomic;
				goto IL_0767;
			case '\'':
				c = '\'';
				goto case '<';
			case '<':
			{
				if (_pos == _pattern.Length)
				{
					break;
				}
				char c2;
				char c3 = (c2 = _pattern[_pos++]);
				if (c3 != '!')
				{
					if (c3 != '=')
					{
						_pos--;
						int num = -1;
						int num2 = -1;
						bool flag = false;
						if ((uint)(c2 - 48) <= 9u)
						{
							num = ScanDecimal();
							if (!IsCaptureSlot(num))
							{
								num = -1;
							}
							if (_pos < _pattern.Length && _pattern[_pos] != c && _pattern[_pos] != '-')
							{
								throw MakeException(RegexParseError.CaptureGroupNameInvalid, System.SR.CaptureGroupNameInvalid);
							}
							if (num == 0)
							{
								throw MakeException(RegexParseError.CaptureGroupOfZero, System.SR.CaptureGroupOfZero);
							}
						}
						else if (RegexCharClass.IsBoundaryWordChar(c2))
						{
							string key = ScanCapname();
							if (_capnames?[key] is int num3)
							{
								num = num3;
							}
							if (_pos < _pattern.Length && _pattern[_pos] != c && _pattern[_pos] != '-')
							{
								throw MakeException(RegexParseError.CaptureGroupNameInvalid, System.SR.CaptureGroupNameInvalid);
							}
						}
						else
						{
							if (c2 != '-')
							{
								throw MakeException(RegexParseError.CaptureGroupNameInvalid, System.SR.CaptureGroupNameInvalid);
							}
							flag = true;
						}
						if ((num != -1 || flag) && _pos + 1 < _pattern.Length && _pattern[_pos] == '-')
						{
							_pos++;
							c2 = _pattern[_pos];
							if ((uint)(c2 - 48) <= 9u)
							{
								num2 = ScanDecimal();
								if (!IsCaptureSlot(num2))
								{
									throw MakeException(RegexParseError.UndefinedNumberedReference, System.SR.Format(System.SR.UndefinedNumberedReference, num2));
								}
								if (_pos < _pattern.Length && _pattern[_pos] != c)
								{
									throw MakeException(RegexParseError.CaptureGroupNameInvalid, System.SR.CaptureGroupNameInvalid);
								}
							}
							else
							{
								if (!RegexCharClass.IsBoundaryWordChar(c2))
								{
									throw MakeException(RegexParseError.CaptureGroupNameInvalid, System.SR.CaptureGroupNameInvalid);
								}
								string text = ScanCapname();
								if (!(_capnames?[text] is int num4))
								{
									throw MakeException(RegexParseError.UndefinedNamedReference, System.SR.Format(System.SR.UndefinedNamedReference, text));
								}
								num2 = num4;
								if (_pos < _pattern.Length && _pattern[_pos] != c)
								{
									throw MakeException(RegexParseError.CaptureGroupNameInvalid, System.SR.CaptureGroupNameInvalid);
								}
							}
						}
						if ((num != -1 || num2 != -1) && _pos < _pattern.Length && _pattern[_pos++] == c)
						{
							return new RegexNode(RegexNodeKind.Capture, _options, num, num2);
						}
						break;
					}
					if (c == '\'')
					{
						break;
					}
					_options |= RegexOptions.RightToLeft;
					kind = RegexNodeKind.PositiveLookaround;
				}
				else
				{
					if (c == '\'')
					{
						break;
					}
					_options |= RegexOptions.RightToLeft;
					kind = RegexNodeKind.NegativeLookaround;
				}
				goto IL_0767;
			}
			case '(':
			{
				int pos = _pos;
				if (_pos < _pattern.Length)
				{
					char c2 = _pattern[_pos];
					if (c2 >= '0' && c2 <= '9')
					{
						int num5 = ScanDecimal();
						if (_pos < _pattern.Length && _pattern[_pos++] == ')')
						{
							if (IsCaptureSlot(num5))
							{
								return new RegexNode(RegexNodeKind.BackreferenceConditional, _options, num5);
							}
							throw MakeException(RegexParseError.AlternationHasUndefinedReference, System.SR.Format(System.SR.AlternationHasUndefinedReference, num5.ToString()));
						}
						throw MakeException(RegexParseError.AlternationHasMalformedReference, System.SR.Format(System.SR.AlternationHasMalformedReference, num5.ToString()));
					}
					if (RegexCharClass.IsBoundaryWordChar(c2))
					{
						string key2 = ScanCapname();
						if (_capnames?[key2] is int m && _pos < _pattern.Length && _pattern[_pos++] == ')')
						{
							return new RegexNode(RegexNodeKind.BackreferenceConditional, _options, m);
						}
					}
				}
				kind = RegexNodeKind.ExpressionConditional;
				_pos = pos - 1;
				_ignoreNextParen = true;
				if (_pos + 2 < _pattern.Length && _pattern[_pos + 1] == '?')
				{
					if (_pattern[_pos + 2] == '#')
					{
						throw MakeException(RegexParseError.AlternationHasComment, System.SR.AlternationHasComment);
					}
					if (_pattern[_pos + 2] == '\'' || (_pos + 3 < _pattern.Length && _pattern[_pos + 2] == '<' && _pattern[_pos + 3] != '!' && _pattern[_pos + 3] != '='))
					{
						throw MakeException(RegexParseError.AlternationHasNamedCapture, System.SR.AlternationHasNamedCapture);
					}
				}
				goto IL_0767;
			}
			default:
				{
					_pos--;
					kind = RegexNodeKind.Group;
					if (_group.Kind != RegexNodeKind.ExpressionConditional)
					{
						ScanOptions();
					}
					if (_pos == _pattern.Length)
					{
						break;
					}
					char c2;
					if ((c2 = _pattern[_pos++]) == ')')
					{
						return null;
					}
					if (c2 != ':')
					{
						break;
					}
					goto IL_0767;
				}
				IL_0767:
				return new RegexNode(kind, _options);
			}
		}
		throw MakeException(RegexParseError.InvalidGroupingConstruct, System.SR.InvalidGroupingConstruct);
	}

	private void ScanBlank()
	{
		while (true)
		{
			if ((_options & RegexOptions.IgnorePatternWhitespace) != 0)
			{
				while (_pos < _pattern.Length && IsSpace(_pattern[_pos]))
				{
					_pos++;
				}
			}
			if ((_options & RegexOptions.IgnorePatternWhitespace) != 0 && _pos < _pattern.Length && _pattern[_pos] == '#')
			{
				_pos = _pattern.IndexOf('\n', _pos);
				if (_pos < 0)
				{
					_pos = _pattern.Length;
				}
				continue;
			}
			if (_pos + 2 < _pattern.Length && _pattern[_pos + 2] == '#' && _pattern[_pos + 1] == '?' && _pattern[_pos] == '(')
			{
				_pos = _pattern.IndexOf(')', _pos);
				if (_pos < 0)
				{
					_pos = _pattern.Length;
					throw MakeException(RegexParseError.UnterminatedComment, System.SR.UnterminatedComment);
				}
				_pos++;
				continue;
			}
			break;
		}
	}

	private RegexNode ScanBackslash(bool scanOnly)
	{
		char c;
		switch (c = _pattern[_pos])
		{
		case 'A':
		case 'B':
		case 'G':
		case 'Z':
		case 'b':
		case 'z':
			_pos++;
			if (!scanOnly)
			{
				return new RegexNode(TypeFromCode(c), _options);
			}
			return null;
		case 'w':
			_pos++;
			if (!scanOnly)
			{
				return new RegexNode(RegexNodeKind.Set, _options & ~RegexOptions.IgnoreCase, ((_options & RegexOptions.ECMAScript) != 0) ? "\0\n\00:A[_`a{İı" : "\0\0\n\0\u0002\u0004\u0005\u0003\u0001\u0006\t\u0013\0");
			}
			return null;
		case 'W':
			_pos++;
			if (!scanOnly)
			{
				return new RegexNode(RegexNodeKind.Set, _options & ~RegexOptions.IgnoreCase, ((_options & RegexOptions.ECMAScript) != 0) ? "\u0001\n\00:A[_`a{İı" : "\0\0\n\0\ufffe￼\ufffb\ufffd\uffff\ufffa\ufff7￭\0");
			}
			return null;
		case 's':
			_pos++;
			if (!scanOnly)
			{
				return new RegexNode(RegexNodeKind.Set, _options & ~RegexOptions.IgnoreCase, ((_options & RegexOptions.ECMAScript) != 0) ? "\0\u0004\0\t\u000e !" : "\0\0\u0001d");
			}
			return null;
		case 'S':
			_pos++;
			if (!scanOnly)
			{
				return new RegexNode(RegexNodeKind.Set, _options & ~RegexOptions.IgnoreCase, ((_options & RegexOptions.ECMAScript) != 0) ? "\u0001\u0004\0\t\u000e !" : "\0\0\u0001ﾜ");
			}
			return null;
		case 'd':
			_pos++;
			if (!scanOnly)
			{
				return new RegexNode(RegexNodeKind.Set, _options & ~RegexOptions.IgnoreCase, ((_options & RegexOptions.ECMAScript) != 0) ? "\0\u0002\00:" : "\0\0\u0001\t");
			}
			return null;
		case 'D':
			_pos++;
			if (!scanOnly)
			{
				return new RegexNode(RegexNodeKind.Set, _options & ~RegexOptions.IgnoreCase, ((_options & RegexOptions.ECMAScript) != 0) ? "\u0001\u0002\00:" : "\0\0\u0001\ufff7");
			}
			return null;
		case 'P':
		case 'p':
		{
			_pos++;
			if (scanOnly)
			{
				return null;
			}
			RegexCharClass regexCharClass = new RegexCharClass();
			regexCharClass.AddCategoryFromName(ParseProperty(), c != 'p', (_options & RegexOptions.IgnoreCase) != 0, _pattern, _pos);
			if ((_options & RegexOptions.IgnoreCase) != 0)
			{
				regexCharClass.AddCaseEquivalences(_culture);
			}
			return new RegexNode(RegexNodeKind.Set, _options & ~RegexOptions.IgnoreCase, regexCharClass.ToStringClass());
		}
		default:
		{
			RegexNode regexNode = ScanBasicBackslash(scanOnly);
			if (regexNode != null && regexNode.Kind == RegexNodeKind.Backreference && (regexNode.Options & RegexOptions.IgnoreCase) != 0)
			{
				_hasIgnoreCaseBackreferenceNodes = true;
			}
			return regexNode;
		}
		}
	}

	private RegexNode ScanBasicBackslash(bool scanOnly)
	{
		int pos = _pos;
		char c = '\0';
		bool flag = false;
		char c2 = _pattern[_pos];
		switch (c2)
		{
		case 'k':
			if (_pos + 1 < _pattern.Length)
			{
				_pos++;
				c2 = _pattern[_pos++];
				if ((c2 == '\'' || c2 == '<') ? true : false)
				{
					flag = true;
					c = ((c2 == '\'') ? '\'' : '>');
				}
			}
			if (!flag || _pos == _pattern.Length)
			{
				throw MakeException(RegexParseError.MalformedNamedReference, System.SR.MalformedNamedReference);
			}
			c2 = _pattern[_pos];
			break;
		case '\'':
		case '<':
			if (_pos + 1 < _pattern.Length)
			{
				flag = true;
				c = ((c2 == '\'') ? '\'' : '>');
				_pos++;
				c2 = _pattern[_pos];
			}
			break;
		}
		if (flag && c2 >= '0' && c2 <= '9')
		{
			int num = ScanDecimal();
			if (_pos < _pattern.Length && _pattern[_pos++] == c)
			{
				if (!scanOnly)
				{
					if (!IsCaptureSlot(num))
					{
						throw MakeException(RegexParseError.UndefinedNumberedReference, System.SR.Format(System.SR.UndefinedNumberedReference, num.ToString()));
					}
					return new RegexNode(RegexNodeKind.Backreference, _options, num);
				}
				return null;
			}
		}
		else if (!flag && c2 >= '1' && c2 <= '9')
		{
			if ((_options & RegexOptions.ECMAScript) != 0)
			{
				int num2 = -1;
				int num3 = c2 - 48;
				int num4 = _pos - 1;
				while (num3 <= _captop)
				{
					if (IsCaptureSlot(num3) && (_caps == null || (int)_caps[num3] < num4))
					{
						num2 = num3;
					}
					_pos++;
					if (_pos == _pattern.Length || (c2 = _pattern[_pos]) < '0' || c2 > '9')
					{
						break;
					}
					num3 = num3 * 10 + (c2 - 48);
				}
				if (num2 >= 0)
				{
					if (!scanOnly)
					{
						return new RegexNode(RegexNodeKind.Backreference, _options, num2);
					}
					return null;
				}
			}
			else
			{
				int num5 = ScanDecimal();
				if (scanOnly)
				{
					return null;
				}
				if (IsCaptureSlot(num5))
				{
					return new RegexNode(RegexNodeKind.Backreference, _options, num5);
				}
				if (num5 <= 9)
				{
					throw MakeException(RegexParseError.UndefinedNumberedReference, System.SR.Format(System.SR.UndefinedNumberedReference, num5.ToString()));
				}
			}
		}
		else if (flag && RegexCharClass.IsBoundaryWordChar(c2))
		{
			string text = ScanCapname();
			if (_pos < _pattern.Length && _pattern[_pos++] == c)
			{
				if (!scanOnly)
				{
					if (!(_capnames?[text] is int m))
					{
						throw MakeException(RegexParseError.UndefinedNamedReference, System.SR.Format(System.SR.UndefinedNamedReference, text));
					}
					return new RegexNode(RegexNodeKind.Backreference, _options, m);
				}
				return null;
			}
		}
		_pos = pos;
		c2 = ScanCharEscape();
		if (scanOnly)
		{
			return null;
		}
		return RegexNode.CreateOneWithCaseConversion(c2, _options, _culture, ref _caseBehavior);
	}

	private RegexNode ScanDollar()
	{
		if (_pos == _pattern.Length)
		{
			return RegexNode.CreateOneWithCaseConversion('$', _options, _culture, ref _caseBehavior);
		}
		char c = _pattern[_pos];
		int pos = _pos;
		int pos2 = pos;
		bool flag;
		if (c == '{' && _pos + 1 < _pattern.Length)
		{
			flag = true;
			_pos++;
			c = _pattern[_pos];
		}
		else
		{
			flag = false;
		}
		if (c >= '0' && c <= '9')
		{
			if (!flag && (_options & RegexOptions.ECMAScript) != 0)
			{
				int num = -1;
				int num2 = c - 48;
				_pos++;
				if (IsCaptureSlot(num2))
				{
					num = num2;
					pos2 = _pos;
				}
				while (_pos < _pattern.Length && (c = _pattern[_pos]) >= '0' && c <= '9')
				{
					int num3 = c - 48;
					if (num2 > 214748364 || (num2 == 214748364 && num3 > 7))
					{
						throw MakeException(RegexParseError.QuantifierOrCaptureGroupOutOfRange, System.SR.QuantifierOrCaptureGroupOutOfRange);
					}
					num2 = num2 * 10 + num3;
					_pos++;
					if (IsCaptureSlot(num2))
					{
						num = num2;
						pos2 = _pos;
					}
				}
				_pos = pos2;
				if (num >= 0)
				{
					return new RegexNode(RegexNodeKind.Backreference, _options, num);
				}
			}
			else
			{
				int num4 = ScanDecimal();
				if ((!flag || (_pos < _pattern.Length && _pattern[_pos++] == '}')) && IsCaptureSlot(num4))
				{
					return new RegexNode(RegexNodeKind.Backreference, _options, num4);
				}
			}
		}
		else if (flag && RegexCharClass.IsBoundaryWordChar(c))
		{
			string key = ScanCapname();
			if (_pos < _pattern.Length && _pattern[_pos++] == '}' && _capnames?[key] is int m)
			{
				return new RegexNode(RegexNodeKind.Backreference, _options, m);
			}
		}
		else if (!flag)
		{
			int num5 = 1;
			switch (c)
			{
			case '$':
				_pos++;
				return RegexNode.CreateOneWithCaseConversion('$', _options, _culture, ref _caseBehavior);
			case '&':
				num5 = 0;
				break;
			case '`':
				num5 = -1;
				break;
			case '\'':
				num5 = -2;
				break;
			case '+':
				num5 = -3;
				break;
			case '_':
				num5 = -4;
				break;
			}
			if (num5 != 1)
			{
				_pos++;
				return new RegexNode(RegexNodeKind.Backreference, _options, num5);
			}
		}
		_pos = pos;
		return RegexNode.CreateOneWithCaseConversion('$', _options, _culture, ref _caseBehavior);
	}

	private string ScanCapname()
	{
		int pos = _pos;
		while (_pos < _pattern.Length)
		{
			if (!RegexCharClass.IsBoundaryWordChar(_pattern[_pos++]))
			{
				_pos--;
				break;
			}
		}
		return _pattern.Substring(pos, _pos - pos);
	}

	private char ScanOctal()
	{
		int num = Math.Min(3, _pattern.Length - _pos);
		int num2 = 0;
		int num3;
		while (num > 0 && (uint)(num3 = _pattern[_pos] - 48) <= 7u)
		{
			_pos++;
			num2 = num2 * 8 + num3;
			if ((_options & RegexOptions.ECMAScript) != 0 && num2 >= 32)
			{
				break;
			}
			num--;
		}
		num2 &= 0xFF;
		return (char)num2;
	}

	private int ScanDecimal()
	{
		int num = 0;
		int num2;
		while (_pos < _pattern.Length && (uint)(num2 = (ushort)(_pattern[_pos] - 48)) <= 9u)
		{
			_pos++;
			if (num > 214748364 || (num == 214748364 && num2 > 7))
			{
				throw MakeException(RegexParseError.QuantifierOrCaptureGroupOutOfRange, System.SR.QuantifierOrCaptureGroupOutOfRange);
			}
			num = num * 10 + num2;
		}
		return num;
	}

	private char ScanHex(int c)
	{
		int num = 0;
		if (_pos + c <= _pattern.Length)
		{
			while (c > 0)
			{
				char c2 = _pattern[_pos++];
				int num2 = System.HexConverter.FromChar(c2);
				if (num2 == 255)
				{
					break;
				}
				num = num * 16 + num2;
				c--;
			}
		}
		if (c > 0)
		{
			throw MakeException(RegexParseError.InsufficientOrInvalidHexDigits, System.SR.InsufficientOrInvalidHexDigits);
		}
		return (char)num;
	}

	private char ScanControl()
	{
		if (_pos == _pattern.Length)
		{
			throw MakeException(RegexParseError.MissingControlCharacter, System.SR.MissingControlCharacter);
		}
		char c = _pattern[_pos++];
		if ((uint)(c - 97) <= 25u)
		{
			c = (char)(c - 32);
		}
		if ((c = (char)(c - 64)) < ' ')
		{
			return c;
		}
		throw MakeException(RegexParseError.UnrecognizedControlCharacter, System.SR.UnrecognizedControlCharacter);
	}

	private void ScanOptions()
	{
		bool flag = false;
		while (_pos < _pattern.Length)
		{
			char c = _pattern[_pos];
			switch (c)
			{
			case '-':
				flag = true;
				break;
			case '+':
				flag = false;
				break;
			default:
			{
				RegexOptions regexOptions = (char)(ushort)(c | 0x20) switch
				{
					'i' => RegexOptions.IgnoreCase, 
					'm' => RegexOptions.Multiline, 
					'n' => RegexOptions.ExplicitCapture, 
					's' => RegexOptions.Singleline, 
					'x' => RegexOptions.IgnorePatternWhitespace, 
					_ => RegexOptions.None, 
				};
				if (regexOptions == RegexOptions.None)
				{
					return;
				}
				if (flag)
				{
					_options &= ~regexOptions;
				}
				else
				{
					_options |= regexOptions;
				}
				break;
			}
			}
			_pos++;
		}
	}

	private char ScanCharEscape()
	{
		char c = _pattern[_pos++];
		if (c >= '0' && c <= '7')
		{
			_pos--;
			return ScanOctal();
		}
		switch (c)
		{
		case 'x':
			return ScanHex(2);
		case 'u':
			return ScanHex(4);
		case 'a':
			return '\a';
		case 'b':
			return '\b';
		case 'e':
			return '\u001b';
		case 'f':
			return '\f';
		case 'n':
			return '\n';
		case 'r':
			return '\r';
		case 't':
			return '\t';
		case 'v':
			return '\v';
		case 'c':
			return ScanControl();
		default:
			if ((_options & RegexOptions.ECMAScript) == 0 && RegexCharClass.IsBoundaryWordChar(c))
			{
				throw MakeException(RegexParseError.UnrecognizedEscape, System.SR.Format(System.SR.UnrecognizedEscape, c));
			}
			return c;
		}
	}

	private string ParseProperty()
	{
		if (_pos + 2 >= _pattern.Length)
		{
			throw MakeException(RegexParseError.InvalidUnicodePropertyEscape, System.SR.InvalidUnicodePropertyEscape);
		}
		char c = _pattern[_pos++];
		if (c != '{')
		{
			throw MakeException(RegexParseError.MalformedUnicodePropertyEscape, System.SR.MalformedUnicodePropertyEscape);
		}
		int pos = _pos;
		while (_pos < _pattern.Length)
		{
			c = _pattern[_pos++];
			if (!RegexCharClass.IsBoundaryWordChar(c) && c != '-')
			{
				_pos--;
				break;
			}
		}
		string result = _pattern.Substring(pos, _pos - pos);
		if (_pos == _pattern.Length || _pattern[_pos++] != '}')
		{
			throw MakeException(RegexParseError.InvalidUnicodePropertyEscape, System.SR.InvalidUnicodePropertyEscape);
		}
		return result;
	}

	private readonly RegexNodeKind TypeFromCode(char ch)
	{
		return ch switch
		{
			'b' => ((_options & RegexOptions.ECMAScript) != 0) ? RegexNodeKind.ECMABoundary : RegexNodeKind.Boundary, 
			'B' => ((_options & RegexOptions.ECMAScript) != 0) ? RegexNodeKind.NonECMABoundary : RegexNodeKind.NonBoundary, 
			'A' => RegexNodeKind.Beginning, 
			'G' => RegexNodeKind.Start, 
			'Z' => RegexNodeKind.EndZ, 
			'z' => RegexNodeKind.End, 
			_ => RegexNodeKind.Nothing, 
		};
	}

	private void CountCaptures(out RegexOptions optionsFoundInPattern)
	{
		NoteCaptureSlot(0, 0);
		optionsFoundInPattern = RegexOptions.None;
		_autocap = 1;
		while (_pos < _pattern.Length)
		{
			int pos = _pos;
			switch (_pattern[_pos++])
			{
			case '\\':
				if (_pos < _pattern.Length)
				{
					ScanBackslash(scanOnly: true);
				}
				break;
			case '#':
				if ((_options & RegexOptions.IgnorePatternWhitespace) != 0)
				{
					_pos--;
					ScanBlank();
				}
				break;
			case '[':
				ScanCharClass(caseInsensitive: false, scanOnly: true);
				break;
			case ')':
				if (_optionsStack.Length != 0)
				{
					_options = (RegexOptions)_optionsStack.Pop();
				}
				break;
			case '(':
				if (_pos + 1 < _pattern.Length && _pattern[_pos + 1] == '#' && _pattern[_pos] == '?')
				{
					_pos--;
					ScanBlank();
				}
				else
				{
					_optionsStack.Append((int)_options);
					if (_pos < _pattern.Length && _pattern[_pos] == '?')
					{
						_pos++;
						if (_pos + 1 < _pattern.Length && (_pattern[_pos] == '<' || _pattern[_pos] == '\''))
						{
							_pos++;
							char c = _pattern[_pos];
							if (c != '0' && RegexCharClass.IsBoundaryWordChar(c))
							{
								if ((uint)(c - 49) <= 8u)
								{
									NoteCaptureSlot(ScanDecimal(), pos);
								}
								else
								{
									NoteCaptureName(ScanCapname(), pos);
								}
							}
						}
						else
						{
							ScanOptions();
							optionsFoundInPattern |= _options;
							if (_pos < _pattern.Length)
							{
								if (_pattern[_pos] == ')')
								{
									_pos++;
									_optionsStack.Length--;
								}
								else if (_pattern[_pos] == '(')
								{
									_ignoreNextParen = true;
									break;
								}
							}
						}
					}
					else if ((_options & RegexOptions.ExplicitCapture) == 0 && !_ignoreNextParen)
					{
						NoteCaptureSlot(_autocap++, pos);
					}
				}
				_ignoreNextParen = false;
				break;
			}
		}
		AssignNameSlots();
	}

	private void NoteCaptureSlot(int i, int pos)
	{
		object key = i;
		if (!_caps.ContainsKey(key))
		{
			_caps.Add(key, pos);
			_capcount++;
			if (_captop <= i)
			{
				_captop = ((i == int.MaxValue) ? i : (i + 1));
			}
		}
	}

	private void NoteCaptureName(string name, int pos)
	{
		if (_capnames == null)
		{
			_capnames = new Hashtable();
			_capnamelist = new List<string>();
		}
		if (!_capnames.ContainsKey(name))
		{
			_capnames.Add(name, pos);
			_capnamelist.Add(name);
		}
	}

	private void AssignNameSlots()
	{
		if (_capnames != null)
		{
			for (int i = 0; i < _capnamelist.Count; i++)
			{
				while (IsCaptureSlot(_autocap))
				{
					_autocap++;
				}
				string key = _capnamelist[i];
				int pos = (int)_capnames[key];
				_capnames[key] = _autocap;
				NoteCaptureSlot(_autocap, pos);
				_autocap++;
			}
		}
		if (_capcount < _captop)
		{
			_capnumlist = new int[_capcount];
			int num = 0;
			IDictionaryEnumerator enumerator = _caps.GetEnumerator();
			while (enumerator.MoveNext())
			{
				_capnumlist[num++] = (int)enumerator.Key;
			}
			Array.Sort(_capnumlist);
		}
		if (_capnames == null && _capnumlist == null)
		{
			return;
		}
		int num2 = 0;
		List<string> list;
		int num3;
		if (_capnames == null)
		{
			list = null;
			_capnames = new Hashtable();
			_capnamelist = new List<string>();
			num3 = -1;
		}
		else
		{
			list = _capnamelist;
			_capnamelist = new List<string>();
			num3 = (int)_capnames[list[0]];
		}
		for (int j = 0; j < _capcount; j++)
		{
			int num4 = ((_capnumlist == null) ? j : _capnumlist[j]);
			if (num3 == num4)
			{
				_capnamelist.Add(list[num2++]);
				num3 = ((num2 == list.Count) ? (-1) : ((int)_capnames[list[num2]]));
			}
			else
			{
				string text = num4.ToString(_culture);
				_capnamelist.Add(text);
				_capnames[text] = num4;
			}
		}
	}

	private readonly bool IsCaptureSlot(int i)
	{
		if (_caps != null)
		{
			return _caps.ContainsKey(i);
		}
		if (i >= 0)
		{
			return i < _capsize;
		}
		return false;
	}

	internal static int MapCaptureNumber(int capnum, Hashtable caps)
	{
		if (capnum != -1)
		{
			if (caps == null)
			{
				return capnum;
			}
			return (int)caps[capnum];
		}
		return -1;
	}

	private static int IndexOfMetachar(ReadOnlySpan<char> input)
	{
		return input.IndexOfAny(s_metachars);
	}

	private static bool IsSpecial(char ch)
	{
		if (ch <= '|')
		{
			return Category[ch] >= 3;
		}
		return false;
	}

	private static bool IsSpecialOrSpace(char ch)
	{
		if (ch <= '|')
		{
			return Category[ch] >= 1;
		}
		return false;
	}

	private static bool IsQuantifier(char ch)
	{
		if (ch <= '{')
		{
			return Category[ch] == 4;
		}
		return false;
	}

	private static bool IsSpace(char ch)
	{
		if (ch <= ' ')
		{
			return Category[ch] == 1;
		}
		return false;
	}

	private readonly bool IsTrueQuantifier()
	{
		int pos = _pos;
		char c = _pattern[pos];
		if (c != '{')
		{
			if (c <= '{')
			{
				return Category[c] >= 4;
			}
			return false;
		}
		int num = pos;
		int num2 = _pattern.Length - _pos;
		while (--num2 > 0 && (uint)((c = _pattern[++num]) - 48) <= 9u)
		{
		}
		if (num2 == 0 || num - pos == 1)
		{
			return false;
		}
		switch (c)
		{
		case '}':
			return true;
		default:
			return false;
		case ',':
			break;
		}
		while (--num2 > 0 && (uint)((c = _pattern[++num]) - 48) <= 9u)
		{
		}
		if (num2 > 0)
		{
			return c == '}';
		}
		return false;
	}

	private void AddToConcatenate(int pos, int cch, bool isReplacement)
	{
		if (cch <= 1)
		{
			switch (cch)
			{
			case 0:
				return;
			case 1:
				_concatenation.AddChild(RegexNode.CreateOneWithCaseConversion(_pattern[pos], isReplacement ? (_options & ~RegexOptions.IgnoreCase) : _options, _culture, ref _caseBehavior));
				return;
			}
		}
		else if ((_options & RegexOptions.IgnoreCase) == 0 || isReplacement || !RegexCharClass.ParticipatesInCaseConversion(_pattern.AsSpan(pos, cch)))
		{
			_concatenation.AddChild(new RegexNode(RegexNodeKind.Multi, _options & ~RegexOptions.IgnoreCase, _pattern.Substring(pos, cch)));
			return;
		}
		ReadOnlySpan<char> readOnlySpan = _pattern.AsSpan(pos, cch);
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			char ch = readOnlySpan[i];
			_concatenation.AddChild(RegexNode.CreateOneWithCaseConversion(ch, _options, _culture, ref _caseBehavior));
		}
	}

	private void PushGroup()
	{
		_group.Parent = _stack;
		_alternation.Parent = _group;
		_concatenation.Parent = _alternation;
		_stack = _concatenation;
	}

	private void PopGroup()
	{
		_concatenation = _stack;
		_alternation = _concatenation.Parent;
		_group = _alternation.Parent;
		_stack = _group.Parent;
		if (_group.Kind == RegexNodeKind.ExpressionConditional && _group.ChildCount() == 0)
		{
			if (_unit == null)
			{
				throw MakeException(RegexParseError.AlternationHasMalformedCondition, System.SR.AlternationHasMalformedCondition);
			}
			_group.AddChild(_unit);
			_unit = null;
		}
	}

	private void StartGroup(RegexNode openGroup)
	{
		_group = openGroup;
		_alternation = new RegexNode(RegexNodeKind.Alternate, _options);
		_concatenation = new RegexNode(RegexNodeKind.Concatenate, _options);
	}

	private void AddAlternate()
	{
		RegexNodeKind kind = _group.Kind;
		if (kind - 33 <= (RegexNodeKind)1)
		{
			_group.AddChild(_concatenation.ReverseConcatenationIfRightToLeft());
		}
		else
		{
			_alternation.AddChild(_concatenation.ReverseConcatenationIfRightToLeft());
		}
		_concatenation = new RegexNode(RegexNodeKind.Concatenate, _options);
	}

	private void AddGroup()
	{
		RegexNodeKind kind = _group.Kind;
		if (kind - 33 <= (RegexNodeKind)1)
		{
			_group.AddChild(_concatenation.ReverseConcatenationIfRightToLeft());
			if ((_group.Kind == RegexNodeKind.BackreferenceConditional && _group.ChildCount() > 2) || _group.ChildCount() > 3)
			{
				throw MakeException(RegexParseError.AlternationHasTooManyConditions, System.SR.AlternationHasTooManyConditions);
			}
		}
		else
		{
			_alternation.AddChild(_concatenation.ReverseConcatenationIfRightToLeft());
			_group.AddChild(_alternation);
		}
		_unit = _group;
	}

	private readonly RegexParseException MakeException(RegexParseError error, string message)
	{
		return new RegexParseException(error, _pos, System.SR.Format(System.SR.MakeException, _pattern, _pos, message));
	}

	internal static string GroupNameFromNumber(Hashtable caps, string[] capslist, int capsize, int i)
	{
		if (capslist == null)
		{
			if ((uint)i < (uint)capsize)
			{
				uint num = (uint)i;
				return num.ToString();
			}
		}
		else if ((caps == null || caps.TryGetValue<int>(i, out i)) && (uint)i < (uint)capslist.Length)
		{
			return capslist[i];
		}
		return string.Empty;
	}
}
