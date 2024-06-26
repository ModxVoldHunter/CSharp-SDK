using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Text.RegularExpressions;

[RequiresDynamicCode("Compiling a RegEx requires dynamic code.")]
internal abstract class RegexCompiler
{
	private struct RentedLocalBuilder : IDisposable
	{
		private readonly Stack<LocalBuilder> _pool;

		private readonly LocalBuilder _local;

		internal RentedLocalBuilder(Stack<LocalBuilder> pool, LocalBuilder local)
		{
			_local = local;
			_pool = pool;
		}

		public static implicit operator LocalBuilder(RentedLocalBuilder local)
		{
			return local._local;
		}

		public void Dispose()
		{
			_pool.Push(_local);
			this = default(RentedLocalBuilder);
		}
	}

	private static readonly FieldInfo s_runtextstartField = RegexRunnerField("runtextstart");

	private static readonly FieldInfo s_runtextposField = RegexRunnerField("runtextpos");

	private static readonly FieldInfo s_runtrackposField = RegexRunnerField("runtrackpos");

	private static readonly FieldInfo s_runstackField = RegexRunnerField("runstack");

	private static readonly FieldInfo s_cultureField = typeof(CompiledRegexRunner).GetField("_culture", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo s_caseBehaviorField = typeof(CompiledRegexRunner).GetField("_caseBehavior", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo s_searchValuesArrayField = typeof(CompiledRegexRunner).GetField("_searchValues", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly MethodInfo s_captureMethod = RegexRunnerMethod("Capture");

	private static readonly MethodInfo s_transferCaptureMethod = RegexRunnerMethod("TransferCapture");

	private static readonly MethodInfo s_uncaptureMethod = RegexRunnerMethod("Uncapture");

	private static readonly MethodInfo s_isMatchedMethod = RegexRunnerMethod("IsMatched");

	private static readonly MethodInfo s_matchLengthMethod = RegexRunnerMethod("MatchLength");

	private static readonly MethodInfo s_matchIndexMethod = RegexRunnerMethod("MatchIndex");

	private static readonly MethodInfo s_isBoundaryMethod = typeof(RegexRunner).GetMethod("IsBoundary", BindingFlags.Static | BindingFlags.NonPublic, new Type[2]
	{
		typeof(ReadOnlySpan<char>),
		typeof(int)
	});

	private static readonly MethodInfo s_isWordCharMethod = RegexRunnerMethod("IsWordChar");

	private static readonly MethodInfo s_isECMABoundaryMethod = typeof(RegexRunner).GetMethod("IsECMABoundary", BindingFlags.Static | BindingFlags.NonPublic, new Type[2]
	{
		typeof(ReadOnlySpan<char>),
		typeof(int)
	});

	private static readonly MethodInfo s_crawlposMethod = RegexRunnerMethod("Crawlpos");

	private static readonly MethodInfo s_charInClassMethod = RegexRunnerMethod("CharInClass");

	private static readonly MethodInfo s_checkTimeoutMethod = RegexRunnerMethod("CheckTimeout");

	private static readonly MethodInfo s_regexCaseEquivalencesTryFindCaseEquivalencesForCharWithIBehaviorMethod = typeof(RegexCaseEquivalences).GetMethod("TryFindCaseEquivalencesForCharWithIBehavior", BindingFlags.Static | BindingFlags.Public);

	private static readonly MethodInfo s_charIsDigitMethod = typeof(char).GetMethod("IsDigit", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsWhiteSpaceMethod = typeof(char).GetMethod("IsWhiteSpace", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsControlMethod = typeof(char).GetMethod("IsControl", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsLetterMethod = typeof(char).GetMethod("IsLetter", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsAsciiDigitMethod = typeof(char).GetMethod("IsAsciiDigit", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsAsciiLetterMethod = typeof(char).GetMethod("IsAsciiLetter", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsAsciiLetterLowerMethod = typeof(char).GetMethod("IsAsciiLetterLower", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsAsciiLetterUpperMethod = typeof(char).GetMethod("IsAsciiLetterUpper", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsAsciiLetterOrDigitMethod = typeof(char).GetMethod("IsAsciiLetterOrDigit", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsAsciiHexDigitMethod = typeof(char).GetMethod("IsAsciiHexDigit", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsAsciiHexDigitLowerMethod = typeof(char).GetMethod("IsAsciiHexDigitLower", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsAsciiHexDigitUpperMethod = typeof(char).GetMethod("IsAsciiHexDigitUpper", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsLetterOrDigitMethod = typeof(char).GetMethod("IsLetterOrDigit", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsLowerMethod = typeof(char).GetMethod("IsLower", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsUpperMethod = typeof(char).GetMethod("IsUpper", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsNumberMethod = typeof(char).GetMethod("IsNumber", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsPunctuationMethod = typeof(char).GetMethod("IsPunctuation", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsSeparatorMethod = typeof(char).GetMethod("IsSeparator", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsSymbolMethod = typeof(char).GetMethod("IsSymbol", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charGetUnicodeInfo = typeof(char).GetMethod("GetUnicodeCategory", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_spanGetItemMethod = typeof(ReadOnlySpan<char>).GetMethod("get_Item", new Type[1] { typeof(int) });

	private static readonly MethodInfo s_spanGetLengthMethod = typeof(ReadOnlySpan<char>).GetMethod("get_Length");

	private static readonly MethodInfo s_spanIndexOfChar = typeof(MemoryExtensions).GetMethod("IndexOf", new Type[2]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanIndexOfSpan = typeof(MemoryExtensions).GetMethod("IndexOf", new Type[2]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanIndexOfSpanStringComparison = typeof(MemoryExtensions).GetMethod("IndexOf", new Type[3]
	{
		typeof(ReadOnlySpan<char>),
		typeof(ReadOnlySpan<char>),
		typeof(StringComparison)
	});

	private static readonly MethodInfo s_spanIndexOfAnyCharChar = typeof(MemoryExtensions).GetMethod("IndexOfAny", new Type[3]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanIndexOfAnyCharCharChar = typeof(MemoryExtensions).GetMethod("IndexOfAny", new Type[4]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanIndexOfAnySpan = typeof(MemoryExtensions).GetMethod("IndexOfAny", new Type[2]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanIndexOfAnySearchValues = typeof(MemoryExtensions).GetMethod("IndexOfAny", new Type[2]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		typeof(SearchValues<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanIndexOfAnyExceptChar = typeof(MemoryExtensions).GetMethod("IndexOfAnyExcept", new Type[2]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanIndexOfAnyExceptCharChar = typeof(MemoryExtensions).GetMethod("IndexOfAnyExcept", new Type[3]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanIndexOfAnyExceptCharCharChar = typeof(MemoryExtensions).GetMethod("IndexOfAnyExcept", new Type[4]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanIndexOfAnyExceptSpan = typeof(MemoryExtensions).GetMethod("IndexOfAnyExcept", new Type[2]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanIndexOfAnyExceptSearchValues = typeof(MemoryExtensions).GetMethod("IndexOfAnyExcept", new Type[2]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		typeof(SearchValues<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanIndexOfAnyInRange = typeof(MemoryExtensions).GetMethod("IndexOfAnyInRange", new Type[3]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanIndexOfAnyExceptInRange = typeof(MemoryExtensions).GetMethod("IndexOfAnyExceptInRange", new Type[3]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanLastIndexOfChar = typeof(MemoryExtensions).GetMethod("LastIndexOf", new Type[2]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanLastIndexOfAnyCharChar = typeof(MemoryExtensions).GetMethod("LastIndexOfAny", new Type[3]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanLastIndexOfAnyCharCharChar = typeof(MemoryExtensions).GetMethod("LastIndexOfAny", new Type[4]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanLastIndexOfAnySpan = typeof(MemoryExtensions).GetMethod("LastIndexOfAny", new Type[2]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanLastIndexOfAnySearchValues = typeof(MemoryExtensions).GetMethod("LastIndexOfAny", new Type[2]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		typeof(SearchValues<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanLastIndexOfSpan = typeof(MemoryExtensions).GetMethod("LastIndexOf", new Type[2]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanLastIndexOfAnyExceptChar = typeof(MemoryExtensions).GetMethod("LastIndexOfAnyExcept", new Type[2]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanLastIndexOfAnyExceptCharChar = typeof(MemoryExtensions).GetMethod("LastIndexOfAnyExcept", new Type[3]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanLastIndexOfAnyExceptCharCharChar = typeof(MemoryExtensions).GetMethod("LastIndexOfAnyExcept", new Type[4]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanLastIndexOfAnyExceptSpan = typeof(MemoryExtensions).GetMethod("LastIndexOfAnyExcept", new Type[2]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanLastIndexOfAnyExceptSearchValues = typeof(MemoryExtensions).GetMethod("LastIndexOfAnyExcept", new Type[2]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		typeof(SearchValues<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanLastIndexOfAnyInRange = typeof(MemoryExtensions).GetMethod("LastIndexOfAnyInRange", new Type[3]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanLastIndexOfAnyExceptInRange = typeof(MemoryExtensions).GetMethod("LastIndexOfAnyExceptInRange", new Type[3]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanSliceIntMethod = typeof(ReadOnlySpan<char>).GetMethod("Slice", new Type[1] { typeof(int) });

	private static readonly MethodInfo s_spanSliceIntIntMethod = typeof(ReadOnlySpan<char>).GetMethod("Slice", new Type[2]
	{
		typeof(int),
		typeof(int)
	});

	private static readonly MethodInfo s_spanStartsWithSpan = typeof(MemoryExtensions).GetMethod("StartsWith", new Type[2]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanStartsWithSpanComparison = typeof(MemoryExtensions).GetMethod("StartsWith", new Type[3]
	{
		typeof(ReadOnlySpan<char>),
		typeof(ReadOnlySpan<char>),
		typeof(StringComparison)
	});

	private static readonly MethodInfo s_stringAsSpanMethod = typeof(MemoryExtensions).GetMethod("AsSpan", new Type[1] { typeof(string) });

	private static readonly MethodInfo s_stringGetCharsMethod = typeof(string).GetMethod("get_Chars", new Type[1] { typeof(int) });

	private static readonly MethodInfo s_arrayResize = typeof(Array).GetMethod("Resize").MakeGenericMethod(typeof(int));

	private static readonly MethodInfo s_mathMinIntInt = typeof(Math).GetMethod("Min", new Type[2]
	{
		typeof(int),
		typeof(int)
	});

	private static readonly MethodInfo s_memoryMarshalGetArrayDataReferenceSearchValues = typeof(MemoryMarshal).GetMethod("GetArrayDataReference", new Type[1] { Type.MakeGenericMethodParameter(0).MakeArrayType() }).MakeGenericMethod(typeof(SearchValues<char>));

	private static readonly MethodInfo s_unsafeAs = typeof(Unsafe).GetMethod("As", new Type[1] { typeof(object) });

	protected ILGenerator _ilg;

	protected RegexOptions _options;

	protected RegexTree _regexTree;

	protected bool _hasTimeout;

	protected List<SearchValues<char>> _searchValues;

	private Stack<LocalBuilder> _int32LocalsPool;

	private Stack<LocalBuilder> _readOnlySpanCharLocalsPool;

	private static FieldInfo RegexRunnerField(string fieldname)
	{
		return typeof(RegexRunner).GetField(fieldname, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}

	private static MethodInfo RegexRunnerMethod(string methname)
	{
		return typeof(RegexRunner).GetMethod(methname, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}

	internal static RegexRunnerFactory Compile(string pattern, RegexTree regexTree, RegexOptions options, bool hasTimeout)
	{
		return new RegexLWCGCompiler().FactoryInstanceFromCode(pattern, regexTree, options, hasTimeout);
	}

	private Label DefineLabel()
	{
		return _ilg.DefineLabel();
	}

	private void MarkLabel(Label l)
	{
		_ilg.MarkLabel(l);
	}

	protected void Ldstr(string str)
	{
		_ilg.Emit(OpCodes.Ldstr, str);
	}

	protected void Ldc(int i)
	{
		_ilg.Emit(OpCodes.Ldc_I4, i);
	}

	protected void LdcI8(long i)
	{
		_ilg.Emit(OpCodes.Ldc_I8, i);
	}

	protected void Ret()
	{
		_ilg.Emit(OpCodes.Ret);
	}

	protected void Dup()
	{
		_ilg.Emit(OpCodes.Dup);
	}

	private void Ceq()
	{
		_ilg.Emit(OpCodes.Ceq);
	}

	private void CgtUn()
	{
		_ilg.Emit(OpCodes.Cgt_Un);
	}

	private void CltUn()
	{
		_ilg.Emit(OpCodes.Clt_Un);
	}

	private void Pop()
	{
		_ilg.Emit(OpCodes.Pop);
	}

	private void Add()
	{
		_ilg.Emit(OpCodes.Add);
	}

	private void Sub()
	{
		_ilg.Emit(OpCodes.Sub);
	}

	private void Mul()
	{
		_ilg.Emit(OpCodes.Mul);
	}

	private void And()
	{
		_ilg.Emit(OpCodes.And);
	}

	private void Or()
	{
		_ilg.Emit(OpCodes.Or);
	}

	private void Shl()
	{
		_ilg.Emit(OpCodes.Shl);
	}

	private void Shr()
	{
		_ilg.Emit(OpCodes.Shr);
	}

	private void Ldloc(LocalBuilder lt)
	{
		_ilg.Emit(OpCodes.Ldloc, lt);
	}

	private void Ldloca(LocalBuilder lt)
	{
		_ilg.Emit(OpCodes.Ldloca, lt);
	}

	private void LdindU2()
	{
		_ilg.Emit(OpCodes.Ldind_U2);
	}

	private void Stloc(LocalBuilder lt)
	{
		_ilg.Emit(OpCodes.Stloc, lt);
	}

	protected void Ldthis()
	{
		_ilg.Emit(OpCodes.Ldarg_0);
	}

	private void Ldarg_1()
	{
		_ilg.Emit(OpCodes.Ldarg_1);
	}

	protected void Ldthisfld(FieldInfo ft)
	{
		Ldthis();
		_ilg.Emit(OpCodes.Ldfld, ft);
	}

	protected void Ldthisflda(FieldInfo ft)
	{
		Ldthis();
		_ilg.Emit(OpCodes.Ldflda, ft);
	}

	private void Ldarga_s(int position)
	{
		_ilg.Emit(OpCodes.Ldarga_S, position);
	}

	private void Mvfldloc(FieldInfo ft, LocalBuilder lt)
	{
		Ldthisfld(ft);
		Stloc(lt);
	}

	protected void Stfld(FieldInfo ft)
	{
		_ilg.Emit(OpCodes.Stfld, ft);
	}

	protected void Call(MethodInfo mt)
	{
		_ilg.Emit(OpCodes.Call, mt);
	}

	private void Brfalse(Label l)
	{
		_ilg.Emit(OpCodes.Brfalse_S, l);
	}

	private void BrfalseFar(Label l)
	{
		_ilg.Emit(OpCodes.Brfalse, l);
	}

	private void BrtrueFar(Label l)
	{
		_ilg.Emit(OpCodes.Brtrue, l);
	}

	private void BrFar(Label l)
	{
		_ilg.Emit(OpCodes.Br, l);
	}

	private void BleFar(Label l)
	{
		_ilg.Emit(OpCodes.Ble, l);
	}

	private void BltFar(Label l)
	{
		_ilg.Emit(OpCodes.Blt, l);
	}

	private void BltUnFar(Label l)
	{
		_ilg.Emit(OpCodes.Blt_Un, l);
	}

	private void BgeFar(Label l)
	{
		_ilg.Emit(OpCodes.Bge, l);
	}

	private void BgeUnFar(Label l)
	{
		_ilg.Emit(OpCodes.Bge_Un, l);
	}

	private void BneFar(Label l)
	{
		_ilg.Emit(OpCodes.Bne_Un, l);
	}

	private void BeqFar(Label l)
	{
		_ilg.Emit(OpCodes.Beq, l);
	}

	private void Brtrue(Label l)
	{
		_ilg.Emit(OpCodes.Brtrue_S, l);
	}

	private void Br(Label l)
	{
		_ilg.Emit(OpCodes.Br_S, l);
	}

	private void Ble(Label l)
	{
		_ilg.Emit(OpCodes.Ble_S, l);
	}

	private void Blt(Label l)
	{
		_ilg.Emit(OpCodes.Blt_S, l);
	}

	private void Bge(Label l)
	{
		_ilg.Emit(OpCodes.Bge_S, l);
	}

	private void BgeUn(Label l)
	{
		_ilg.Emit(OpCodes.Bge_Un_S, l);
	}

	private void Bgt(Label l)
	{
		_ilg.Emit(OpCodes.Bgt_S, l);
	}

	private void Bne(Label l)
	{
		_ilg.Emit(OpCodes.Bne_Un_S, l);
	}

	private void Beq(Label l)
	{
		_ilg.Emit(OpCodes.Beq_S, l);
	}

	private void Ldlen()
	{
		_ilg.Emit(OpCodes.Ldlen);
	}

	private void LdelemI4()
	{
		_ilg.Emit(OpCodes.Ldelem_I4);
	}

	private void StelemI4()
	{
		_ilg.Emit(OpCodes.Stelem_I4);
	}

	private void Switch(Label[] table)
	{
		_ilg.Emit(OpCodes.Switch, table);
	}

	private LocalBuilder DeclareInt32()
	{
		return _ilg.DeclareLocal(typeof(int));
	}

	private LocalBuilder DeclareReadOnlySpanChar()
	{
		return _ilg.DeclareLocal(typeof(ReadOnlySpan<char>));
	}

	private RentedLocalBuilder RentInt32Local()
	{
		LocalBuilder result;
		return new RentedLocalBuilder(_int32LocalsPool ?? (_int32LocalsPool = new Stack<LocalBuilder>()), _int32LocalsPool.TryPop(out result) ? result : DeclareInt32());
	}

	private RentedLocalBuilder RentReadOnlySpanCharLocal()
	{
		LocalBuilder result;
		return new RentedLocalBuilder(_readOnlySpanCharLocalsPool ?? (_readOnlySpanCharLocalsPool = new Stack<LocalBuilder>(1)), _readOnlySpanCharLocalsPool.TryPop(out result) ? result : DeclareReadOnlySpanChar());
	}

	protected void EmitTryFindNextPossibleStartingPosition()
	{
		_int32LocalsPool?.Clear();
		_readOnlySpanCharLocalsPool?.Clear();
		LocalBuilder inputSpan = DeclareReadOnlySpanChar();
		LocalBuilder pos = DeclareInt32();
		bool rtl = (_options & RegexOptions.RightToLeft) != 0;
		Mvfldloc(s_runtextposField, pos);
		Ldarg_1();
		Stloc(inputSpan);
		int minRequiredLength = _regexTree.FindOptimizations.MinRequiredLength;
		Label returnFalse = DefineLabel();
		Label l = DefineLabel();
		Ldloc(pos);
		if (!rtl)
		{
			Ldloca(inputSpan);
			Call(s_spanGetLengthMethod);
			if (minRequiredLength > 0)
			{
				Ldc(minRequiredLength);
				Sub();
			}
			Ble(l);
		}
		else
		{
			Ldc(minRequiredLength);
			Bge(l);
		}
		MarkLabel(returnFalse);
		Ldthis();
		if (!rtl)
		{
			Ldloca(inputSpan);
			Call(s_spanGetLengthMethod);
		}
		else
		{
			Ldc(0);
		}
		Stfld(s_runtextposField);
		Ldc(0);
		Ret();
		MarkLabel(l);
		if (!EmitAnchors())
		{
			switch (_regexTree.FindOptimizations.FindMode)
			{
			case FindNextStartingPositionMode.LeadingString_LeftToRight:
			case FindNextStartingPositionMode.LeadingString_OrdinalIgnoreCase_LeftToRight:
			case FindNextStartingPositionMode.FixedDistanceString_LeftToRight:
				EmitIndexOf_LeftToRight();
				break;
			case FindNextStartingPositionMode.LeadingString_RightToLeft:
				EmitIndexOf_RightToLeft();
				break;
			case FindNextStartingPositionMode.LeadingSet_LeftToRight:
			case FindNextStartingPositionMode.FixedDistanceSets_LeftToRight:
				EmitFixedSet_LeftToRight();
				break;
			case FindNextStartingPositionMode.LeadingSet_RightToLeft:
				EmitFixedSet_RightToLeft();
				break;
			case FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight:
				EmitLiteralAfterAtomicLoop();
				break;
			default:
				Ldc(1);
				Ret();
				break;
			}
		}
		bool EmitAnchors()
		{
			switch (_regexTree.FindOptimizations.FindMode)
			{
			case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning:
				Ldloc(pos);
				Ldc(0);
				Bne(returnFalse);
				Ldc(1);
				Ret();
				return true;
			case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Start:
			case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start:
				Ldloc(pos);
				Ldthisfld(s_runtextstartField);
				Bne(returnFalse);
				Ldc(1);
				Ret();
				return true;
			case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_EndZ:
			{
				Label l13 = DefineLabel();
				Ldloc(pos);
				Ldloca(inputSpan);
				Call(s_spanGetLengthMethod);
				Ldc(1);
				Sub();
				Bge(l13);
				Ldthis();
				Ldloca(inputSpan);
				Call(s_spanGetLengthMethod);
				Ldc(1);
				Sub();
				Stfld(s_runtextposField);
				MarkLabel(l13);
				Ldc(1);
				Ret();
				return true;
			}
			case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_End:
			{
				Label l13 = DefineLabel();
				Ldloc(pos);
				Ldloca(inputSpan);
				Call(s_spanGetLengthMethod);
				Bge(l13);
				Ldthis();
				Ldloca(inputSpan);
				Call(s_spanGetLengthMethod);
				Stfld(s_runtextposField);
				MarkLabel(l13);
				Ldc(1);
				Ret();
				return true;
			}
			case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Beginning:
			{
				Label l13 = DefineLabel();
				Ldloc(pos);
				Ldc(0);
				Beq(l13);
				Ldthis();
				Ldc(0);
				Stfld(s_runtextposField);
				MarkLabel(l13);
				Ldc(1);
				Ret();
				return true;
			}
			case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_EndZ:
			{
				Label l13 = DefineLabel();
				Ldloc(pos);
				Ldloca(inputSpan);
				Call(s_spanGetLengthMethod);
				Ldc(1);
				Sub();
				Blt(returnFalse);
				Ldloc(pos);
				Ldloca(inputSpan);
				Call(s_spanGetLengthMethod);
				BgeUn(l13);
				Ldloca(inputSpan);
				Ldloc(pos);
				Call(s_spanGetItemMethod);
				LdindU2();
				Ldc(10);
				Bne(returnFalse);
				MarkLabel(l13);
				Ldc(1);
				Ret();
				return true;
			}
			case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End:
				Ldloc(pos);
				Ldloca(inputSpan);
				Call(s_spanGetLengthMethod);
				Blt(returnFalse);
				Ldc(1);
				Ret();
				return true;
			case FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_End:
			case FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_EndZ:
			{
				int num4 = ((_regexTree.FindOptimizations.FindMode == FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_EndZ) ? 1 : 0);
				Label l13 = DefineLabel();
				Ldloc(pos);
				Ldloca(inputSpan);
				Call(s_spanGetLengthMethod);
				Ldc(_regexTree.FindOptimizations.MinRequiredLength + num4);
				Sub();
				Bge(l13);
				Ldthis();
				Ldloca(inputSpan);
				Call(s_spanGetLengthMethod);
				Ldc(_regexTree.FindOptimizations.MinRequiredLength + num4);
				Sub();
				Stfld(s_runtextposField);
				MarkLabel(l13);
				Ldc(1);
				Ret();
				return true;
			}
			default:
				if (!rtl)
				{
					RegexNodeKind leadingAnchor = _regexTree.FindOptimizations.LeadingAnchor;
					if (leadingAnchor == RegexNodeKind.Bol)
					{
						Label l13 = DefineLabel();
						Ldloc(pos);
						Ldc(0);
						Ble(l13);
						Ldloca(inputSpan);
						Ldloc(pos);
						Ldc(1);
						Sub();
						Call(s_spanGetItemMethod);
						LdindU2();
						Ldc(10);
						Beq(l13);
						Ldloca(inputSpan);
						Ldloc(pos);
						Call(s_spanSliceIntMethod);
						Ldc(10);
						Call(s_spanIndexOfChar);
						using (RentedLocalBuilder rentedLocalBuilder10 = RentInt32Local())
						{
							Stloc(rentedLocalBuilder10);
							Ldloc(rentedLocalBuilder10);
							Ldc(0);
							Blt(returnFalse);
							Ldloc(rentedLocalBuilder10);
							Ldloc(pos);
							Add();
							Ldc(1);
							Add();
							Ldloca(inputSpan);
							Call(s_spanGetLengthMethod);
							Bgt(returnFalse);
							Ldloc(pos);
							Ldloc(rentedLocalBuilder10);
							Add();
							Ldc(1);
							Add();
							Stloc(pos);
							Ldloca(inputSpan);
							Call(s_spanGetLengthMethod);
							if (minRequiredLength != 0)
							{
								Ldc(minRequiredLength);
								Sub();
							}
							Ldloc(pos);
							BltFar(returnFalse);
						}
						MarkLabel(l13);
					}
					RegexNodeKind trailingAnchor = _regexTree.FindOptimizations.TrailingAnchor;
					if (trailingAnchor - 20 <= (RegexNodeKind)1)
					{
						int? maxPossibleLength = _regexTree.FindOptimizations.MaxPossibleLength;
						if (maxPossibleLength.HasValue)
						{
							int valueOrDefault = maxPossibleLength.GetValueOrDefault();
							int num3 = ((_regexTree.FindOptimizations.FindMode == FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_EndZ) ? 1 : 0);
							Label l13 = DefineLabel();
							Ldloc(pos);
							Ldloca(inputSpan);
							Call(s_spanGetLengthMethod);
							Ldc(valueOrDefault + num3);
							Sub();
							Bge(l13);
							Ldloca(inputSpan);
							Call(s_spanGetLengthMethod);
							Ldc(valueOrDefault + num3);
							Sub();
							Stloc(pos);
							MarkLabel(l13);
						}
					}
				}
				return false;
			}
		}
		void EmitFixedSet_LeftToRight()
		{
			List<RegexFindOptimizations.FixedDistanceSet> fixedDistanceSets = _regexTree.FindOptimizations.FixedDistanceSets;
			RegexFindOptimizations.FixedDistanceSet fixedDistanceSet = fixedDistanceSets[0];
			int num = Math.Min(fixedDistanceSets.Count, 4);
			using RentedLocalBuilder rentedLocalBuilder3 = RentInt32Local();
			using RentedLocalBuilder rentedLocalBuilder2 = RentReadOnlySpanCharLocal();
			Ldloca(inputSpan);
			Ldloc(pos);
			Call(s_spanSliceIntMethod);
			Stloc(rentedLocalBuilder2);
			int i = 0;
			bool flag2 = fixedDistanceSet.Set != "\u0001\u0002\0\n\v" && fixedDistanceSet.Set != "\0\u0001\0\0";
			bool flag3 = !flag2 || num > 1;
			Label l2 = default(Label);
			Label l3 = default(Label);
			Label l4 = default(Label);
			if (flag3)
			{
				l2 = DefineLabel();
				l3 = DefineLabel();
				l4 = DefineLabel();
				Ldc(0);
				Stloc(rentedLocalBuilder3);
				BrFar(l2);
				MarkLabel(l4);
			}
			if (flag2)
			{
				i = 1;
				if (flag3)
				{
					Ldloca(rentedLocalBuilder2);
					Ldloc(rentedLocalBuilder3);
					if (fixedDistanceSet.Distance != 0)
					{
						Ldc(fixedDistanceSet.Distance);
						Add();
					}
					Call(s_spanSliceIntMethod);
				}
				else if (fixedDistanceSet.Distance != 0)
				{
					Ldloca(rentedLocalBuilder2);
					Ldc(fixedDistanceSet.Distance);
					Call(s_spanSliceIntMethod);
				}
				else
				{
					Ldloc(rentedLocalBuilder2);
				}
				if (fixedDistanceSet.Chars != null)
				{
					switch (fixedDistanceSet.Chars.Length)
					{
					case 1:
						Ldc(fixedDistanceSet.Chars[0]);
						Call(fixedDistanceSet.Negated ? s_spanIndexOfAnyExceptChar : s_spanIndexOfChar);
						break;
					case 2:
						Ldc(fixedDistanceSet.Chars[0]);
						Ldc(fixedDistanceSet.Chars[1]);
						Call(fixedDistanceSet.Negated ? s_spanIndexOfAnyExceptCharChar : s_spanIndexOfAnyCharChar);
						break;
					case 3:
						Ldc(fixedDistanceSet.Chars[0]);
						Ldc(fixedDistanceSet.Chars[1]);
						Ldc(fixedDistanceSet.Chars[2]);
						Call(fixedDistanceSet.Negated ? s_spanIndexOfAnyExceptCharCharChar : s_spanIndexOfAnyCharCharChar);
						break;
					case 4:
					case 5:
						Ldstr(new string(fixedDistanceSet.Chars));
						Call(s_stringAsSpanMethod);
						Call(fixedDistanceSet.Negated ? s_spanIndexOfAnyExceptSpan : s_spanIndexOfAnySpan);
						break;
					default:
						LoadSearchValues(fixedDistanceSet.Chars);
						Call(fixedDistanceSet.Negated ? s_spanIndexOfAnyExceptSearchValues : s_spanIndexOfAnySearchValues);
						break;
					}
				}
				else
				{
					(char, char)? range = fixedDistanceSet.Range;
					if (range.HasValue)
					{
						if (fixedDistanceSet.Range.Value.LowInclusive == fixedDistanceSet.Range.Value.HighInclusive)
						{
							Ldc(fixedDistanceSet.Range.Value.LowInclusive);
							Call(fixedDistanceSet.Negated ? s_spanIndexOfAnyExceptChar : s_spanIndexOfChar);
						}
						else
						{
							Ldc(fixedDistanceSet.Range.Value.LowInclusive);
							Ldc(fixedDistanceSet.Range.Value.HighInclusive);
							Call(fixedDistanceSet.Negated ? s_spanIndexOfAnyExceptInRange : s_spanIndexOfAnyInRange);
						}
					}
					else
					{
						List<char> list = new List<char>();
						for (int j = 0; j <= 127; j++)
						{
							if (!RegexCharClass.CharInClass((char)j, fixedDistanceSet.Set))
							{
								list.Add((char)j);
							}
						}
						using RentedLocalBuilder rentedLocalBuilder4 = RentReadOnlySpanCharLocal();
						using RentedLocalBuilder rentedLocalBuilder5 = RentInt32Local();
						Stloc(rentedLocalBuilder4);
						Ldloc(rentedLocalBuilder4);
						if (list.Count == 128)
						{
							Ldc(0);
							Ldc(127);
							Call(s_spanIndexOfAnyExceptInRange);
						}
						else
						{
							LoadSearchValues(CollectionsMarshal.AsSpan(list));
							Call(s_spanIndexOfAnyExceptSearchValues);
						}
						Stloc(rentedLocalBuilder5);
						Label l5 = DefineLabel();
						Ldloc(rentedLocalBuilder5);
						Ldloca(rentedLocalBuilder4);
						Call(s_spanGetLengthMethod);
						BgeUnFar(l5);
						Ldc(127);
						Ldloca(rentedLocalBuilder4);
						Ldloc(rentedLocalBuilder5);
						Call(s_spanGetItemMethod);
						LdindU2();
						BgeUnFar(l5);
						Label l6 = DefineLabel();
						MarkLabel(l6);
						Ldloca(rentedLocalBuilder4);
						Ldloc(rentedLocalBuilder5);
						Call(s_spanGetItemMethod);
						LdindU2();
						EmitMatchCharacterClass(fixedDistanceSet.Set);
						Brtrue(l5);
						Ldloc(rentedLocalBuilder5);
						Ldc(1);
						Add();
						Stloc(rentedLocalBuilder5);
						Ldloc(rentedLocalBuilder5);
						Ldloca(rentedLocalBuilder4);
						Call(s_spanGetLengthMethod);
						BltUnFar(l6);
						Ldc(-1);
						Stloc(rentedLocalBuilder5);
						MarkLabel(l5);
						Ldloc(rentedLocalBuilder5);
					}
				}
				if (flag3)
				{
					using RentedLocalBuilder rentedLocalBuilder6 = RentInt32Local();
					Stloc(rentedLocalBuilder6);
					Ldloc(rentedLocalBuilder3);
					Ldloc(rentedLocalBuilder6);
					Add();
					Stloc(rentedLocalBuilder3);
					Ldloc(rentedLocalBuilder6);
					Ldc(0);
					BltFar(returnFalse);
				}
				else
				{
					Stloc(rentedLocalBuilder3);
					Ldloc(rentedLocalBuilder3);
					Ldc(0);
					BltFar(returnFalse);
				}
				if (num > 1)
				{
					int num2 = fixedDistanceSets[1].Distance;
					for (int k = 2; k < num; k++)
					{
						num2 = Math.Max(num2, fixedDistanceSets[k].Distance);
					}
					if (num2 > fixedDistanceSet.Distance && num > 1)
					{
						Ldloc(rentedLocalBuilder3);
						Ldc(num2);
						Add();
						Ldloca(rentedLocalBuilder2);
						Call(s_spanGetLengthMethod);
						_ilg.Emit(OpCodes.Bge_Un, returnFalse);
					}
				}
			}
			for (; i < num; i++)
			{
				Ldloca(rentedLocalBuilder2);
				Ldloc(rentedLocalBuilder3);
				if (fixedDistanceSets[i].Distance != 0)
				{
					Ldc(fixedDistanceSets[i].Distance);
					Add();
				}
				Call(s_spanGetItemMethod);
				LdindU2();
				EmitMatchCharacterClass(fixedDistanceSets[i].Set);
				BrfalseFar(l3);
			}
			Ldthis();
			Ldloc(pos);
			Ldloc(rentedLocalBuilder3);
			Add();
			Stfld(s_runtextposField);
			Ldc(1);
			Ret();
			if (flag3)
			{
				MarkLabel(l3);
				Ldloc(rentedLocalBuilder3);
				Ldc(1);
				Add();
				Stloc(rentedLocalBuilder3);
				MarkLabel(l2);
				Ldloc(rentedLocalBuilder3);
				Ldloca(rentedLocalBuilder2);
				Call(s_spanGetLengthMethod);
				if (num > 1 || fixedDistanceSet.Distance != 0)
				{
					Ldc(minRequiredLength - 1);
					Sub();
				}
				BltFar(l4);
				BrFar(returnFalse);
			}
		}
		void EmitFixedSet_RightToLeft()
		{
			RegexFindOptimizations.FixedDistanceSet fixedDistanceSet2 = _regexTree.FindOptimizations.FixedDistanceSets[0];
			char[] chars = fixedDistanceSet2.Chars;
			if (chars != null && chars.Length == 1)
			{
				Ldloca(inputSpan);
				Ldc(0);
				Ldloc(pos);
				Call(s_spanSliceIntIntMethod);
				Ldc(fixedDistanceSet2.Chars[0]);
				Call(s_spanLastIndexOfChar);
				Stloc(pos);
				Ldloc(pos);
				Ldc(0);
				BltFar(returnFalse);
				Ldthis();
				Ldloc(pos);
				Ldc(1);
				Add();
				Stfld(s_runtextposField);
				Ldc(1);
				Ret();
			}
			else
			{
				Label l7 = DefineLabel();
				MarkLabel(l7);
				Ldloc(pos);
				Ldc(1);
				Sub();
				Stloc(pos);
				Ldloc(pos);
				Ldloca(inputSpan);
				Call(s_spanGetLengthMethod);
				BgeUnFar(returnFalse);
				Ldloca(inputSpan);
				Ldloc(pos);
				Call(s_spanGetItemMethod);
				LdindU2();
				EmitMatchCharacterClass(fixedDistanceSet2.Set);
				Brfalse(l7);
				Ldthis();
				Ldloc(pos);
				Ldc(1);
				Add();
				Stfld(s_runtextposField);
				Ldc(1);
				Ret();
			}
		}
		void EmitIndexOf_LeftToRight()
		{
			RegexFindOptimizations findOptimizations = _regexTree.FindOptimizations;
			using RentedLocalBuilder rentedLocalBuilder = RentInt32Local();
			Ldloca(inputSpan);
			Ldloc(pos);
			if (findOptimizations.FindMode == FindNextStartingPositionMode.FixedDistanceString_LeftToRight)
			{
				(char, string, int) fixedDistanceLiteral = findOptimizations.FixedDistanceLiteral;
				if (fixedDistanceLiteral.Item3 > 0)
				{
					Ldc(fixedDistanceLiteral.Item3);
					Add();
				}
			}
			Call(s_spanSliceIntMethod);
			FindNextStartingPositionMode findMode = findOptimizations.FindMode;
			bool flag = ((findMode == FindNextStartingPositionMode.LeadingString_LeftToRight || findMode == FindNextStartingPositionMode.LeadingString_OrdinalIgnoreCase_LeftToRight) ? true : false);
			Ldstr(flag ? findOptimizations.LeadingPrefix : findOptimizations.FixedDistanceLiteral.String);
			Call(s_stringAsSpanMethod);
			if (findOptimizations.FindMode == FindNextStartingPositionMode.LeadingString_OrdinalIgnoreCase_LeftToRight)
			{
				Ldc(5);
				Call(s_spanIndexOfSpanStringComparison);
			}
			else
			{
				Call(s_spanIndexOfSpan);
			}
			Stloc(rentedLocalBuilder);
			Ldloc(rentedLocalBuilder);
			Ldc(0);
			BltFar(returnFalse);
			Ldthis();
			Ldloc(pos);
			Ldloc(rentedLocalBuilder);
			Add();
			Stfld(s_runtextposField);
			Ldc(1);
			Ret();
		}
		void EmitIndexOf_RightToLeft()
		{
			string leadingPrefix = _regexTree.FindOptimizations.LeadingPrefix;
			Ldloca(inputSpan);
			Ldc(0);
			Ldloc(pos);
			Call(s_spanSliceIntIntMethod);
			Ldstr(leadingPrefix);
			Call(s_stringAsSpanMethod);
			Call(s_spanLastIndexOfSpan);
			Stloc(pos);
			Ldloc(pos);
			Ldc(0);
			BltFar(returnFalse);
			Ldthis();
			Ldloc(pos);
			Ldc(leadingPrefix.Length);
			Add();
			Stfld(s_runtextposField);
			Ldc(1);
			Ret();
		}
		void EmitLiteralAfterAtomicLoop()
		{
			(RegexNode, (char, string, char[])) value = _regexTree.FindOptimizations.LiteralAfterLoop.Value;
			Label l8 = DefineLabel();
			Label l9 = DefineLabel();
			MarkLabel(l8);
			using RentedLocalBuilder rentedLocalBuilder7 = RentReadOnlySpanCharLocal();
			Ldloca(inputSpan);
			Ldloc(pos);
			Call(s_spanSliceIntMethod);
			Stloc(rentedLocalBuilder7);
			using RentedLocalBuilder rentedLocalBuilder8 = RentInt32Local();
			Ldloc(rentedLocalBuilder7);
			string item = value.Item2.Item2;
			if (item != null)
			{
				Ldstr(item);
				Call(s_stringAsSpanMethod);
				Call(s_spanIndexOfSpan);
			}
			else
			{
				char[] item2 = value.Item2.Item3;
				if (item2 == null)
				{
					Ldc(value.Item2.Item1);
					Call(s_spanIndexOfChar);
				}
				else
				{
					switch (item2.Length)
					{
					case 2:
						Ldc(item2[0]);
						Ldc(item2[1]);
						Call(s_spanIndexOfAnyCharChar);
						break;
					case 3:
						Ldc(item2[0]);
						Ldc(item2[1]);
						Ldc(item2[2]);
						Call(s_spanIndexOfAnyCharCharChar);
						break;
					default:
						Ldstr(new string(item2));
						Call(s_stringAsSpanMethod);
						Call(s_spanIndexOfAnySpan);
						break;
					}
				}
			}
			Stloc(rentedLocalBuilder8);
			Ldloc(rentedLocalBuilder8);
			Ldc(0);
			BltFar(l9);
			using RentedLocalBuilder rentedLocalBuilder9 = RentInt32Local();
			Ldloc(rentedLocalBuilder8);
			Stloc(rentedLocalBuilder9);
			Label l10 = DefineLabel();
			Label l11 = DefineLabel();
			MarkLabel(l10);
			Ldloc(rentedLocalBuilder9);
			Ldc(1);
			Sub();
			Stloc(rentedLocalBuilder9);
			Ldloc(rentedLocalBuilder9);
			Ldloca(rentedLocalBuilder7);
			Call(s_spanGetLengthMethod);
			BgeUn(l11);
			Ldloca(rentedLocalBuilder7);
			Ldloc(rentedLocalBuilder9);
			Call(s_spanGetItemMethod);
			LdindU2();
			EmitMatchCharacterClass(value.Item1.Str);
			BrtrueFar(l10);
			MarkLabel(l11);
			if (value.Item1.M > 0)
			{
				Label l12 = DefineLabel();
				Ldloc(rentedLocalBuilder8);
				Ldloc(rentedLocalBuilder9);
				Sub();
				Ldc(1);
				Sub();
				Ldc(value.Item1.M);
				Bge(l12);
				Ldloc(pos);
				Ldloc(rentedLocalBuilder8);
				Add();
				Ldc(1);
				Add();
				Stloc(pos);
				BrFar(l8);
				MarkLabel(l12);
			}
			Ldthis();
			Ldloc(pos);
			Ldloc(rentedLocalBuilder9);
			Add();
			Ldc(1);
			Add();
			Stfld(s_runtextposField);
			Ldthis();
			Ldloc(pos);
			Ldloc(rentedLocalBuilder8);
			Add();
			Stfld(s_runtrackposField);
			Ldc(1);
			Ret();
			MarkLabel(l9);
			BrFar(returnFalse);
		}
	}

	protected void EmitTryMatchAtCurrentPosition()
	{
		_int32LocalsPool?.Clear();
		_readOnlySpanCharLocalsPool?.Clear();
		RegexNode root = _regexTree.Root;
		root = root.Child(0);
		RegexNodeKind kind = root.Kind;
		if (kind - 9 <= RegexNodeKind.Oneloop)
		{
			int num = ((root.Kind != RegexNodeKind.Multi) ? 1 : root.Str.Length);
			if ((root.Options & RegexOptions.RightToLeft) != 0)
			{
				num = -num;
			}
			Ldthis();
			Dup();
			Ldc(0);
			Ldthisfld(s_runtextposField);
			Dup();
			Ldc(num);
			Add();
			Call(s_captureMethod);
			Ldthisfld(s_runtextposField);
			Ldc(num);
			Add();
			Stfld(s_runtextposField);
			Ldc(1);
			Ret();
			return;
		}
		AnalysisResults analysis = RegexTreeAnalyzer.Analyze(_regexTree);
		LocalBuilder inputSpan = DeclareReadOnlySpanChar();
		LocalBuilder lt = DeclareInt32();
		LocalBuilder pos = DeclareInt32();
		LocalBuilder slice = DeclareReadOnlySpanChar();
		Label doneLabel = DefineLabel();
		Label l = doneLabel;
		Ldarg_1();
		Stloc(inputSpan);
		Ldthisfld(s_runtextposField);
		Stloc(pos);
		Ldloc(pos);
		Stloc(lt);
		LocalBuilder stackpos = DeclareInt32();
		Ldc(0);
		Stloc(stackpos);
		int sliceStaticPos = 0;
		SliceInputSpan();
		bool expressionHasCaptures = analysis.MayContainCapture(root);
		EmitNode(root);
		Ldthis();
		Ldloc(pos);
		if (sliceStaticPos > 0)
		{
			Ldc(sliceStaticPos);
			Add();
			Stloc(pos);
			Ldloc(pos);
		}
		Stfld(s_runtextposField);
		Ldthis();
		Ldc(0);
		Ldloc(lt);
		Ldloc(pos);
		Call(s_captureMethod);
		Ldc(1);
		Ret();
		if (expressionHasCaptures)
		{
			Label l2 = DefineLabel();
			Br(l2);
			MarkLabel(l);
			Label l3 = DefineLabel();
			Label l4 = DefineLabel();
			Br(l3);
			MarkLabel(l4);
			Ldthis();
			Call(s_uncaptureMethod);
			MarkLabel(l3);
			Ldthis();
			Call(s_crawlposMethod);
			Brtrue(l4);
			MarkLabel(l2);
		}
		else
		{
			MarkLabel(l);
		}
		Ldc(0);
		Ret();
		static bool CanEmitIndexOf(RegexNode node, out int literalLength)
		{
			if (node.Kind == RegexNodeKind.Multi)
			{
				literalLength = node.Str.Length;
				return true;
			}
			if (node.IsOneFamily || node.IsNotoneFamily)
			{
				literalLength = 1;
				return true;
			}
			if (node.IsSetFamily)
			{
				Span<char> chars2 = stackalloc char[5];
				int setChars2;
				if ((setChars2 = RegexCharClass.GetSetChars(node.Str, chars2)) > 0)
				{
					literalLength = 1;
					return true;
				}
				if (RegexCharClass.TryGetSingleRange(node.Str, out var _, out var _))
				{
					literalLength = 1;
					return true;
				}
				if (RegexCharClass.TryGetAsciiSetChars(node.Str, out var _))
				{
					literalLength = 1;
					return true;
				}
			}
			literalLength = 0;
			return false;
		}
		void EmitAlternation(RegexNode node)
		{
			int num8 = node.ChildCount();
			Label label12 = doneLabel;
			bool flag19 = analysis.IsAtomicByAncestor(node);
			Label l33 = DefineLabel();
			LocalBuilder startingPos = DeclareInt32();
			Ldloc(pos);
			Stloc(startingPos);
			int num9 = sliceStaticPos;
			LocalBuilder startingCapturePos = null;
			if (expressionHasCaptures && (analysis.MayContainCapture(node) || !flag19))
			{
				startingCapturePos = DeclareInt32();
				Ldthis();
				Call(s_crawlposMethod);
				Stloc(startingCapturePos);
			}
			bool flag20 = !flag19 && !analysis.IsInLoop(node);
			Label[] array2 = new Label[num8];
			Label label13 = DefineLabel();
			LocalBuilder localBuilder3 = (flag20 ? DeclareInt32() : null);
			int i;
			for (i = 0; i < num8; i++)
			{
				bool flag21 = i == num8 - 1;
				Label l34 = default(Label);
				if (!flag21)
				{
					l34 = (doneLabel = DefineLabel());
				}
				else
				{
					doneLabel = label12;
				}
				EmitNode(node.Child(i));
				if (!flag19)
				{
					if (localBuilder3 == null)
					{
						EmitStackResizeIfNeeded(2 + ((startingCapturePos != null) ? 1 : 0));
						EmitStackPush(delegate
						{
							Ldc(i);
						});
						if (startingCapturePos != null)
						{
							EmitStackPush(delegate
							{
								Ldloc(startingCapturePos);
							});
						}
						EmitStackPush(delegate
						{
							Ldloc(startingPos);
						});
					}
					else
					{
						Ldc(i);
						Stloc(localBuilder3);
					}
				}
				array2[i] = doneLabel;
				TransferSliceStaticPosToPos();
				BrFar(l33);
				if (!flag21)
				{
					MarkLabel(l34);
					Ldloc(startingPos);
					Stloc(pos);
					SliceInputSpan();
					sliceStaticPos = num9;
					if (startingCapturePos != null)
					{
						EmitUncaptureUntil(startingCapturePos);
					}
				}
			}
			if (flag19)
			{
				doneLabel = label12;
			}
			else
			{
				doneLabel = label13;
				MarkLabel(label13);
				EmitTimeoutCheckIfNeeded();
				if (localBuilder3 == null)
				{
					EmitStackPop();
					Stloc(startingPos);
					if (startingCapturePos != null)
					{
						EmitStackPop();
						Stloc(startingCapturePos);
					}
					EmitStackPop();
				}
				else
				{
					Ldloc(localBuilder3);
				}
				Switch(array2);
			}
			MarkLabel(l33);
		}
		void EmitAnchors(RegexNode node)
		{
			switch (node.Kind)
			{
			default:
				return;
			case RegexNodeKind.Beginning:
			case RegexNodeKind.Start:
				if (sliceStaticPos > 0)
				{
					BrFar(doneLabel);
				}
				else
				{
					Ldloc(pos);
					if (node.Kind == RegexNodeKind.Beginning)
					{
						Ldc(0);
					}
					else
					{
						Ldthisfld(s_runtextstartField);
					}
					BneFar(doneLabel);
				}
				return;
			case RegexNodeKind.Bol:
				if (sliceStaticPos > 0)
				{
					Ldloca(slice);
					Ldc(sliceStaticPos - 1);
					Call(s_spanGetItemMethod);
					LdindU2();
					Ldc(10);
					BneFar(doneLabel);
				}
				else
				{
					Label l5 = DefineLabel();
					Ldloc(pos);
					Ldc(0);
					Ble(l5);
					Ldloca(inputSpan);
					Ldloc(pos);
					Ldc(1);
					Sub();
					Call(s_spanGetItemMethod);
					LdindU2();
					Ldc(10);
					BneFar(doneLabel);
					MarkLabel(l5);
				}
				return;
			case RegexNodeKind.End:
				if (sliceStaticPos > 0)
				{
					Ldc(sliceStaticPos);
					Ldloca(slice);
				}
				else
				{
					Ldloc(pos);
					Ldloca(inputSpan);
				}
				Call(s_spanGetLengthMethod);
				BltUnFar(doneLabel);
				return;
			case RegexNodeKind.EndZ:
				if (sliceStaticPos > 0)
				{
					Ldc(sliceStaticPos);
					Ldloca(slice);
				}
				else
				{
					Ldloc(pos);
					Ldloca(inputSpan);
				}
				Call(s_spanGetLengthMethod);
				Ldc(1);
				Sub();
				BltFar(doneLabel);
				break;
			case RegexNodeKind.Eol:
				break;
			case RegexNodeKind.Boundary:
			case RegexNodeKind.NonBoundary:
				return;
			}
			if (sliceStaticPos > 0)
			{
				Label l6 = DefineLabel();
				Ldc(sliceStaticPos);
				Ldloca(slice);
				Call(s_spanGetLengthMethod);
				BgeUn(l6);
				Ldloca(slice);
				Ldc(sliceStaticPos);
				Call(s_spanGetItemMethod);
				LdindU2();
				Ldc(10);
				BneFar(doneLabel);
				MarkLabel(l6);
			}
			else
			{
				Label l7 = DefineLabel();
				Ldloc(pos);
				Ldloca(inputSpan);
				Call(s_spanGetLengthMethod);
				BgeUn(l7);
				Ldloca(inputSpan);
				Ldloc(pos);
				Call(s_spanGetItemMethod);
				LdindU2();
				Ldc(10);
				BneFar(doneLabel);
				MarkLabel(l7);
			}
		}
		void EmitAtomic(RegexNode node, RegexNode subsequent)
		{
			RegexNode node7 = node.Child(0);
			if (!analysis.MayBacktrack(node7))
			{
				EmitNode(node7, subsequent);
				return;
			}
			Label label24 = doneLabel;
			using RentedLocalBuilder rentedLocalBuilder10 = RentInt32Local();
			Ldloc(stackpos);
			Stloc(rentedLocalBuilder10);
			EmitNode(node7, subsequent);
			Ldloc(rentedLocalBuilder10);
			Stloc(stackpos);
			doneLabel = label24;
		}
		void EmitAtomicSingleCharZeroOrOne(RegexNode node)
		{
			bool flag5 = (node.Options & RegexOptions.RightToLeft) != 0;
			if (flag5)
			{
				TransferSliceStaticPosToPos();
			}
			Label l11 = DefineLabel();
			if (!flag5)
			{
				Ldc(sliceStaticPos);
				Ldloca(slice);
				Call(s_spanGetLengthMethod);
				BgeUnFar(l11);
			}
			else
			{
				Ldloc(pos);
				Ldc(0);
				BeqFar(l11);
			}
			if (!flag5)
			{
				Ldloca(slice);
				Ldc(sliceStaticPos);
			}
			else
			{
				Ldloca(inputSpan);
				Ldloc(pos);
				Ldc(1);
				Sub();
			}
			Call(s_spanGetItemMethod);
			LdindU2();
			if (node.IsSetFamily)
			{
				EmitMatchCharacterClass(node.Str);
				BrfalseFar(l11);
			}
			else
			{
				Ldc(node.Ch);
				if (node.IsOneFamily)
				{
					BneFar(l11);
				}
				else
				{
					BeqFar(l11);
				}
			}
			if (!flag5)
			{
				Ldloca(slice);
				Ldc(1);
				Call(s_spanSliceIntMethod);
				Stloc(slice);
				Ldloc(pos);
				Ldc(1);
				Add();
				Stloc(pos);
			}
			else
			{
				Ldloc(pos);
				Ldc(1);
				Sub();
				Stloc(pos);
			}
			MarkLabel(l11);
		}
		void EmitBackreference(RegexNode node)
		{
			int i2 = RegexParser.MapCaptureNumber(node.M, _regexTree.CaptureNumberSparseMapping);
			bool flag22 = (node.Options & RegexOptions.RightToLeft) != 0;
			TransferSliceStaticPosToPos();
			Label label14 = DefineLabel();
			Ldthis();
			Ldc(i2);
			Call(s_isMatchedMethod);
			BrfalseFar(((node.Options & RegexOptions.ECMAScript) == 0) ? doneLabel : label14);
			using RentedLocalBuilder rentedLocalBuilder7 = RentInt32Local();
			using RentedLocalBuilder rentedLocalBuilder8 = RentInt32Local();
			using RentedLocalBuilder rentedLocalBuilder9 = RentInt32Local();
			Ldthis();
			Ldc(i2);
			Call(s_matchLengthMethod);
			Stloc(rentedLocalBuilder7);
			if (!flag22)
			{
				Ldloca(slice);
				Call(s_spanGetLengthMethod);
			}
			else
			{
				Ldloc(pos);
			}
			Ldloc(rentedLocalBuilder7);
			BltFar(doneLabel);
			Ldthis();
			Ldc(i2);
			Call(s_matchIndexMethod);
			Stloc(rentedLocalBuilder8);
			Label l35 = DefineLabel();
			Label l36 = DefineLabel();
			Label l37 = DefineLabel();
			LocalBuilder lt2 = _ilg.DeclareLocal(typeof(char));
			LocalBuilder lt3 = _ilg.DeclareLocal(typeof(char));
			Ldc(0);
			Stloc(rentedLocalBuilder9);
			Br(l35);
			MarkLabel(l36);
			Ldloca(inputSpan);
			Ldloc(rentedLocalBuilder8);
			Ldloc(rentedLocalBuilder9);
			Add();
			Call(s_spanGetItemMethod);
			LdindU2();
			Stloc(lt2);
			if (!flag22)
			{
				Ldloca(slice);
				Ldloc(rentedLocalBuilder9);
			}
			else
			{
				Ldloca(inputSpan);
				Ldloc(pos);
				Ldloc(rentedLocalBuilder7);
				Sub();
				Ldloc(rentedLocalBuilder9);
				Add();
			}
			Call(s_spanGetItemMethod);
			LdindU2();
			Stloc(lt3);
			if ((node.Options & RegexOptions.IgnoreCase) != 0)
			{
				LocalBuilder lt4 = DeclareReadOnlySpanChar();
				Ldloc(lt2);
				Ldloc(lt3);
				Ceq();
				BrtrueFar(l37);
				Ldloc(lt2);
				Ldthisfld(s_cultureField);
				Ldthisflda(s_caseBehaviorField);
				Ldloca(lt4);
				Call(s_regexCaseEquivalencesTryFindCaseEquivalencesForCharWithIBehaviorMethod);
				BrfalseFar(doneLabel);
				Ldloc(lt4);
				if (!flag22)
				{
					Ldloca(slice);
					Ldloc(rentedLocalBuilder9);
				}
				else
				{
					Ldloca(inputSpan);
					Ldloc(pos);
					Ldloc(rentedLocalBuilder7);
					Sub();
					Ldloc(rentedLocalBuilder9);
					Add();
				}
				Call(s_spanGetItemMethod);
				LdindU2();
				Call(s_spanIndexOfChar);
				Ldc(0);
				BltFar(doneLabel);
			}
			else
			{
				Ldloc(lt2);
				Ldloc(lt3);
				Ceq();
				BrfalseFar(doneLabel);
			}
			MarkLabel(l37);
			Ldloc(rentedLocalBuilder9);
			Ldc(1);
			Add();
			Stloc(rentedLocalBuilder9);
			MarkLabel(l35);
			Ldloc(rentedLocalBuilder9);
			Ldloc(rentedLocalBuilder7);
			Blt(l36);
			Ldloc(pos);
			Ldloc(rentedLocalBuilder7);
			if (!flag22)
			{
				Add();
			}
			else
			{
				Sub();
			}
			Stloc(pos);
			if (!flag22)
			{
				SliceInputSpan();
			}
			MarkLabel(label14);
		}
		void EmitBackreferenceConditional(RegexNode node)
		{
			bool flag23 = analysis.IsAtomicByAncestor(node);
			TransferSliceStaticPosToPos();
			int i3 = RegexParser.MapCaptureNumber(node.M, _regexTree.CaptureNumberSparseMapping);
			RegexNode node2 = node.Child(0);
			RegexNode regexNode6 = node.Child(1);
			RegexNode regexNode7 = ((regexNode6 != null && regexNode6.Kind != RegexNodeKind.Empty) ? regexNode6 : null);
			Label label15 = doneLabel;
			Label l38 = DefineLabel();
			Label l39 = DefineLabel();
			LocalBuilder resumeAt = DeclareInt32();
			bool flag24 = analysis.IsInLoop(node);
			Ldthis();
			Ldc(i3);
			Call(s_isMatchedMethod);
			BrfalseFar(l38);
			EmitNode(node2);
			TransferSliceStaticPosToPos();
			Label label16 = doneLabel;
			if ((!flag23 && label16 != label15) || flag24)
			{
				Ldc(0);
				Stloc(resumeAt);
			}
			bool flag25 = label16 != label15 || regexNode7 != null;
			if (flag25)
			{
				BrFar(l39);
			}
			MarkLabel(l38);
			Label label17 = label15;
			if (regexNode7 != null)
			{
				doneLabel = label15;
				EmitNode(regexNode7);
				TransferSliceStaticPosToPos();
				label17 = doneLabel;
				if ((!flag23 && label17 != label15) || flag24)
				{
					Ldc(1);
					Stloc(resumeAt);
				}
			}
			else if ((!flag23 && label16 != label15) || flag24)
			{
				Ldc(2);
				Stloc(resumeAt);
			}
			if (flag23 || (label16 == label15 && label17 == label15))
			{
				doneLabel = label15;
				if (flag25)
				{
					MarkLabel(l39);
				}
			}
			else
			{
				Br(l39);
				Label l40 = (doneLabel = DefineLabel());
				MarkLabel(l40);
				if (flag24)
				{
					EmitStackPop();
					Stloc(resumeAt);
				}
				if (label16 != label15)
				{
					Ldloc(resumeAt);
					Ldc(0);
					BeqFar(label16);
				}
				if (label17 != label15)
				{
					Ldloc(resumeAt);
					Ldc(1);
					BeqFar(label17);
				}
				BrFar(label15);
				if (flag25)
				{
					MarkLabel(l39);
				}
				if (flag24)
				{
					EmitStackResizeIfNeeded(1);
					EmitStackPush(delegate
					{
						Ldloc(resumeAt);
					});
				}
			}
		}
		void EmitBoundary(RegexNode node)
		{
			if ((node.Options & RegexOptions.RightToLeft) != 0)
			{
				TransferSliceStaticPosToPos();
			}
			Ldloc(inputSpan);
			Ldloc(pos);
			if (sliceStaticPos > 0)
			{
				Ldc(sliceStaticPos);
				Add();
			}
			switch (node.Kind)
			{
			case RegexNodeKind.Boundary:
				Call(s_isBoundaryMethod);
				BrfalseFar(doneLabel);
				break;
			case RegexNodeKind.NonBoundary:
				Call(s_isBoundaryMethod);
				BrtrueFar(doneLabel);
				break;
			case RegexNodeKind.ECMABoundary:
				Call(s_isECMABoundaryMethod);
				BrfalseFar(doneLabel);
				break;
			default:
				Call(s_isECMABoundaryMethod);
				BrtrueFar(doneLabel);
				break;
			}
		}
		void EmitCapture(RegexNode node, RegexNode subsequent = null)
		{
			int i4 = RegexParser.MapCaptureNumber(node.M, _regexTree.CaptureNumberSparseMapping);
			int num13 = RegexParser.MapCaptureNumber(node.N, _regexTree.CaptureNumberSparseMapping);
			bool flag28 = analysis.IsAtomicByAncestor(node);
			bool flag29 = analysis.IsInLoop(node);
			TransferSliceStaticPosToPos();
			LocalBuilder startingPos = DeclareInt32();
			Ldloc(pos);
			Stloc(startingPos);
			RegexNode node5 = node.Child(0);
			if (num13 != -1)
			{
				Ldthis();
				Ldc(num13);
				Call(s_isMatchedMethod);
				BrfalseFar(doneLabel);
			}
			Label label22 = doneLabel;
			EmitNode(node5, subsequent);
			bool flag30 = doneLabel != label22;
			TransferSliceStaticPosToPos();
			if (num13 == -1)
			{
				Ldthis();
				Ldc(i4);
				Ldloc(startingPos);
				Ldloc(pos);
				Call(s_captureMethod);
			}
			else
			{
				Ldthis();
				Ldc(i4);
				Ldc(num13);
				Ldloc(startingPos);
				Ldloc(pos);
				Call(s_transferCaptureMethod);
			}
			if (flag28 || !flag30)
			{
				doneLabel = label22;
			}
			else
			{
				if (flag29)
				{
					EmitStackResizeIfNeeded(1);
					EmitStackPush(delegate
					{
						Ldloc(startingPos);
					});
				}
				Label l46 = DefineLabel();
				Br(l46);
				Label label23 = DefineLabel();
				MarkLabel(label23);
				if (flag29)
				{
					EmitStackPop();
					Stloc(startingPos);
				}
				BrFar(doneLabel);
				doneLabel = label23;
				MarkLabel(l46);
			}
		}
		void EmitConcatenation(RegexNode node, RegexNode subsequent, bool emitLengthChecksIfRequired)
		{
			int num11 = node.ChildCount();
			for (int k = 0; k < num11; k++)
			{
				if ((node.Options & RegexOptions.RightToLeft) == 0 && emitLengthChecksIfRequired && node.TryGetJoinableLengthCheckChildRange(k, out var requiredLength2, out var exclusiveEnd))
				{
					EmitSpanLengthCheck(requiredLength2);
					for (; k < exclusiveEnd; k++)
					{
						if (node.TryGetOrdinalCaseInsensitiveString(k, exclusiveEnd, out var nodesConsumed, out var caseInsensitiveString))
						{
							if (sliceStaticPos > 0)
							{
								Ldloca(slice);
								Ldc(sliceStaticPos);
								Call(s_spanSliceIntMethod);
							}
							else
							{
								Ldloc(slice);
							}
							Ldstr(caseInsensitiveString);
							Call(s_stringAsSpanMethod);
							Ldc(5);
							Call(s_spanStartsWithSpanComparison);
							BrfalseFar(doneLabel);
							sliceStaticPos += caseInsensitiveString.Length;
							k += nodesConsumed - 1;
						}
						else
						{
							EmitNode(node.Child(k), GetSubsequent(k, node, subsequent), emitLengthChecksIfRequired: false);
						}
					}
					k--;
				}
				else
				{
					EmitNode(node.Child(k), GetSubsequent(k, node, subsequent));
				}
			}
		}
		void EmitExpressionConditional(RegexNode node)
		{
			bool flag26 = analysis.IsAtomicByAncestor(node);
			TransferSliceStaticPosToPos();
			RegexNode node3 = node.Child(0);
			RegexNode node4 = node.Child(1);
			RegexNode regexNode8 = node.Child(2);
			RegexNode regexNode9 = ((regexNode8 != null && regexNode8.Kind != RegexNodeKind.Empty) ? regexNode8 : null);
			Label label18 = doneLabel;
			Label label19 = DefineLabel();
			Label l43 = DefineLabel();
			bool flag27 = false;
			LocalBuilder resumeAt = null;
			if (!flag26)
			{
				flag27 = analysis.IsInLoop(node);
				resumeAt = DeclareInt32();
			}
			LocalBuilder localBuilder4 = null;
			if (analysis.MayContainCapture(node3))
			{
				localBuilder4 = DeclareInt32();
				Ldthis();
				Call(s_crawlposMethod);
				Stloc(localBuilder4);
			}
			doneLabel = label19;
			LocalBuilder lt5 = DeclareInt32();
			Ldloc(pos);
			Stloc(lt5);
			int num12 = sliceStaticPos;
			EmitNode(node3);
			doneLabel = label18;
			Ldloc(lt5);
			Stloc(pos);
			SliceInputSpan();
			sliceStaticPos = num12;
			EmitNode(node4);
			TransferSliceStaticPosToPos();
			Label label20 = doneLabel;
			if (!flag26 && label20 != label18)
			{
				Ldc(0);
				Stloc(resumeAt);
			}
			BrFar(l43);
			MarkLabel(label19);
			Ldloc(lt5);
			Stloc(pos);
			SliceInputSpan();
			sliceStaticPos = num12;
			if (localBuilder4 != null)
			{
				EmitUncaptureUntil(localBuilder4);
			}
			Label label21 = label18;
			if (regexNode9 != null)
			{
				doneLabel = label18;
				EmitNode(regexNode9);
				TransferSliceStaticPosToPos();
				label21 = doneLabel;
				if (!flag26 && label21 != label18)
				{
					Ldc(1);
					Stloc(resumeAt);
				}
			}
			else if (!flag26 && label20 != label18)
			{
				Ldc(2);
				Stloc(resumeAt);
			}
			if (flag26 || (label20 == label18 && label21 == label18))
			{
				doneLabel = label18;
				MarkLabel(l43);
			}
			else
			{
				BrFar(l43);
				Label l44 = (doneLabel = DefineLabel());
				MarkLabel(l44);
				if (flag27)
				{
					EmitStackPop();
					Stloc(resumeAt);
				}
				if (label20 != label18)
				{
					Ldloc(resumeAt);
					Ldc(0);
					BeqFar(label20);
				}
				if (label21 != label18)
				{
					Ldloc(resumeAt);
					Ldc(1);
					BeqFar(label21);
				}
				BrFar(label18);
				MarkLabel(l43);
				if (flag27)
				{
					EmitStackResizeIfNeeded(1);
					EmitStackPush(delegate
					{
						Ldloc(resumeAt);
					});
				}
			}
		}
		void EmitIndexOf(RegexNode node, bool useLast, bool negate)
		{
			if (node.Kind == RegexNodeKind.Multi)
			{
				Ldstr(node.Str);
				Call(s_stringAsSpanMethod);
				Call(useLast ? s_spanLastIndexOfSpan : s_spanIndexOfSpan);
			}
			else if (node.IsOneFamily || node.IsNotoneFamily)
			{
				if (node.IsNotoneFamily)
				{
					negate = !negate;
				}
				Ldc(node.Ch);
				MethodInfo mt = ((!useLast) ? (negate ? s_spanIndexOfAnyExceptChar : s_spanIndexOfChar) : (negate ? s_spanLastIndexOfAnyExceptChar : s_spanLastIndexOfChar));
				Call(mt);
			}
			else if (node.IsSetFamily)
			{
				bool flag6 = RegexCharClass.IsNegated(node.Str) ^ negate;
				Span<char> chars = stackalloc char[5];
				int setChars = RegexCharClass.GetSetChars(node.Str, chars);
				bool flag7 = (uint)(setChars - 1) <= 1u;
				char[] asciiChars;
				if (!flag7 && RegexCharClass.TryGetSingleRange(node.Str, out var lowInclusive, out var highInclusive))
				{
					if (lowInclusive == highInclusive)
					{
						Ldc(lowInclusive);
						MethodInfo mt = ((!useLast) ? (flag6 ? s_spanIndexOfAnyExceptChar : s_spanIndexOfChar) : (flag6 ? s_spanLastIndexOfAnyExceptChar : s_spanLastIndexOfChar));
						Call(mt);
					}
					else
					{
						Ldc(lowInclusive);
						Ldc(highInclusive);
						MethodInfo mt = ((!useLast) ? (flag6 ? s_spanIndexOfAnyExceptInRange : s_spanIndexOfAnyInRange) : (flag6 ? s_spanLastIndexOfAnyExceptInRange : s_spanLastIndexOfAnyInRange));
						Call(mt);
					}
				}
				else if (setChars > 0)
				{
					chars = chars.Slice(0, setChars);
					switch (chars.Length)
					{
					case 1:
					{
						Ldc(chars[0]);
						MethodInfo mt = ((!useLast) ? (flag6 ? s_spanIndexOfAnyExceptChar : s_spanIndexOfChar) : (flag6 ? s_spanLastIndexOfAnyExceptChar : s_spanLastIndexOfChar));
						Call(mt);
						break;
					}
					case 2:
					{
						Ldc(chars[0]);
						Ldc(chars[1]);
						MethodInfo mt = ((!useLast) ? (flag6 ? s_spanIndexOfAnyExceptCharChar : s_spanIndexOfAnyCharChar) : (flag6 ? s_spanLastIndexOfAnyExceptCharChar : s_spanLastIndexOfAnyCharChar));
						Call(mt);
						break;
					}
					case 3:
					{
						Ldc(chars[0]);
						Ldc(chars[1]);
						Ldc(chars[2]);
						MethodInfo mt = ((!useLast) ? (flag6 ? s_spanIndexOfAnyExceptCharCharChar : s_spanIndexOfAnyCharCharChar) : (flag6 ? s_spanLastIndexOfAnyExceptCharCharChar : s_spanLastIndexOfAnyCharCharChar));
						Call(mt);
						break;
					}
					default:
					{
						Ldstr(chars.ToString());
						Call(s_stringAsSpanMethod);
						MethodInfo mt = ((!useLast) ? (flag6 ? s_spanIndexOfAnyExceptSpan : s_spanIndexOfAnySpan) : (flag6 ? s_spanLastIndexOfAnyExceptSpan : s_spanLastIndexOfAnySpan));
						Call(mt);
						break;
					}
					}
				}
				else if (RegexCharClass.TryGetAsciiSetChars(node.Str, out asciiChars))
				{
					LoadSearchValues(asciiChars);
					MethodInfo mt = ((!useLast) ? (flag6 ? s_spanIndexOfAnyExceptSearchValues : s_spanIndexOfAnySearchValues) : (flag6 ? s_spanLastIndexOfAnyExceptSearchValues : s_spanLastIndexOfAnySearchValues));
					Call(mt);
				}
			}
		}
		void EmitLazy(RegexNode node)
		{
			RegexNode regexNode4 = node.Child(0);
			int m4 = node.M;
			int n3 = node.N;
			Label label9 = doneLabel;
			if (m4 == n3)
			{
				EmitLoop(node);
			}
			else
			{
				bool flag16 = analysis.IsAtomicByAncestor(node);
				TransferSliceStaticPosToPos();
				Label l27 = DefineLabel();
				Label l28 = DefineLabel();
				LocalBuilder iterationCount = DeclareInt32();
				Ldc(0);
				Stloc(iterationCount);
				bool flag17 = regexNode4.ComputeMinLength() == 0;
				LocalBuilder startingPos = null;
				LocalBuilder sawEmpty = null;
				if (flag17)
				{
					startingPos = DeclareInt32();
					Ldloc(pos);
					Stloc(startingPos);
					sawEmpty = DeclareInt32();
					Ldc(0);
					Stloc(sawEmpty);
				}
				if (m4 == 0)
				{
					BrFar(l28);
				}
				MarkLabel(l27);
				if (!flag16)
				{
					int num7 = 1 + (flag17 ? 2 : 0) + (expressionHasCaptures ? 1 : 0);
					EmitStackResizeIfNeeded(num7);
					EmitStackPush(delegate
					{
						Ldloc(pos);
					});
					if (flag17)
					{
						EmitStackPush(delegate
						{
							Ldloc(startingPos);
						});
						EmitStackPush(delegate
						{
							Ldloc(sawEmpty);
						});
					}
					if (expressionHasCaptures)
					{
						EmitStackPush(delegate
						{
							Ldthis();
							Call(s_crawlposMethod);
						});
					}
					if (flag17)
					{
						Ldloc(pos);
						Stloc(startingPos);
					}
					Ldloc(iterationCount);
					Ldc(1);
					Add();
					Stloc(iterationCount);
					Label label10 = (doneLabel = DefineLabel());
					EmitNode(regexNode4);
					TransferSliceStaticPosToPos();
					if (doneLabel == label10)
					{
						doneLabel = label9;
					}
					if (m4 >= 2)
					{
						Ldloc(iterationCount);
						Ldc(m4);
						BltFar(l27);
					}
					if (flag17)
					{
						Label l29 = DefineLabel();
						Ldloc(pos);
						Ldloc(startingPos);
						Bne(l29);
						Ldc(1);
						Stloc(sawEmpty);
						MarkLabel(l29);
					}
					BrFar(l28);
					MarkLabel(label10);
					Ldloc(iterationCount);
					Ldc(1);
					Sub();
					Stloc(iterationCount);
					EmitUncaptureUntilPopped();
					if (flag17)
					{
						EmitStackPop();
						Stloc(sawEmpty);
						EmitStackPop();
						Stloc(startingPos);
					}
					EmitStackPop();
					Stloc(pos);
					SliceInputSpan();
					if (doneLabel == label9)
					{
						Ldloc(stackpos);
						Ldloc(iterationCount);
						if (num7 > 1)
						{
							Ldc(num7);
							Mul();
						}
						Sub();
						Stloc(stackpos);
						BrFar(label9);
					}
					else
					{
						Ldloc(iterationCount);
						Ldc(0);
						BeqFar(label9);
						if (flag17)
						{
							Ldc(0);
							Stloc(sawEmpty);
						}
						BrFar(doneLabel);
					}
					MarkLabel(l28);
					bool flag18 = analysis.IsInLoop(node);
					EmitStackResizeIfNeeded(1 + (flag18 ? (1 + (flag17 ? 2 : 0)) : 0) + (expressionHasCaptures ? 1 : 0));
					EmitStackPush(delegate
					{
						Ldloc(pos);
					});
					if (flag18)
					{
						EmitStackPush(delegate
						{
							Ldloc(iterationCount);
						});
						if (flag17)
						{
							EmitStackPush(delegate
							{
								Ldloc(startingPos);
							});
							EmitStackPush(delegate
							{
								Ldloc(sawEmpty);
							});
						}
					}
					if (expressionHasCaptures)
					{
						EmitStackPush(delegate
						{
							Ldthis();
							Call(s_crawlposMethod);
						});
					}
					Label l30 = DefineLabel();
					BrFar(l30);
					Label label11 = DefineLabel();
					MarkLabel(label11);
					EmitTimeoutCheckIfNeeded();
					EmitUncaptureUntilPopped();
					if (flag18)
					{
						if (flag17)
						{
							EmitStackPop();
							Stloc(sawEmpty);
							EmitStackPop();
							Stloc(startingPos);
						}
						EmitStackPop();
						Stloc(iterationCount);
					}
					EmitStackPop();
					Stloc(pos);
					SliceInputSpan();
					Label l31 = DefineLabel();
					if (flag17)
					{
						Label l32 = DefineLabel();
						Ldloc(sawEmpty);
						Ldc(0);
						Beq(l32);
						Ldc(0);
						Stloc(sawEmpty);
						Br(l31);
						MarkLabel(l32);
					}
					if (n3 != int.MaxValue)
					{
						Ldloc(iterationCount);
						Ldc(n3);
						Bge(l31);
					}
					BrFar(l27);
					MarkLabel(l31);
					if (doneLabel == label9)
					{
						Ldloc(stackpos);
						Ldc(num7);
						Sub();
						Stloc(stackpos);
					}
					BrFar(doneLabel);
					doneLabel = label11;
					MarkLabel(l30);
				}
			}
		}
		void EmitLoop(RegexNode node)
		{
			RegexNode regexNode3 = node.Child(0);
			int m3 = node.M;
			int n2 = node.N;
			if (m3 == n2)
			{
				int num6 = m3;
				if (num6 <= 1)
				{
					switch (num6)
					{
					case 0:
						return;
					case 1:
						EmitNode(regexNode3);
						return;
					}
				}
				else if (!analysis.MayBacktrack(regexNode3))
				{
					EmitNonBacktrackingRepeater(node);
					return;
				}
			}
			TransferSliceStaticPosToPos();
			bool flag11 = analysis.IsAtomicByAncestor(node);
			LocalBuilder startingStackpos = null;
			if (flag11 || m3 > 1)
			{
				startingStackpos = DeclareInt32();
				Ldloc(stackpos);
				Stloc(startingStackpos);
			}
			Label label5 = doneLabel;
			Label l23 = DefineLabel();
			Label l24 = DefineLabel();
			LocalBuilder iterationCount = DeclareInt32();
			bool flag12 = regexNode3.ComputeMinLength() == 0;
			LocalBuilder startingPos = (flag12 ? DeclareInt32() : null);
			Ldc(0);
			Stloc(iterationCount);
			if (startingPos != null)
			{
				Ldc(0);
				Stloc(startingPos);
			}
			MarkLabel(l23);
			EmitStackResizeIfNeeded(1 + (expressionHasCaptures ? 1 : 0) + ((startingPos != null) ? 1 : 0));
			if (expressionHasCaptures)
			{
				EmitStackPush(delegate
				{
					Ldthis();
					Call(s_crawlposMethod);
				});
			}
			if (startingPos != null)
			{
				EmitStackPush(delegate
				{
					Ldloc(startingPos);
				});
			}
			EmitStackPush(delegate
			{
				Ldloc(pos);
			});
			if (startingPos != null)
			{
				Ldloc(pos);
				Stloc(startingPos);
			}
			Ldloc(iterationCount);
			Ldc(1);
			Add();
			Stloc(iterationCount);
			Label label6 = (doneLabel = DefineLabel());
			EmitNode(regexNode3);
			TransferSliceStaticPosToPos();
			bool flag13 = doneLabel != label6;
			bool flag14 = m3 > 0;
			bool flag15 = n2 == int.MaxValue;
			if (flag14)
			{
				if (flag15)
				{
					if (!flag12)
					{
						goto IL_047a;
					}
					Ldloc(pos);
					Ldloc(startingPos);
					BneFar(l23);
					Ldloc(iterationCount);
					Ldc(m3);
					BltFar(l23);
					BrFar(l24);
				}
				else
				{
					if (!flag12)
					{
						goto IL_0441;
					}
					Ldloc(iterationCount);
					Ldc(n2);
					BgeFar(l24);
					Ldloc(pos);
					Ldloc(startingPos);
					BneFar(l23);
					Ldloc(iterationCount);
					Ldc(m3);
					BltFar(l23);
					BrFar(l24);
				}
			}
			else if (flag15)
			{
				if (!flag12)
				{
					goto IL_047a;
				}
				Ldloc(pos);
				Ldloc(startingPos);
				BneFar(l23);
				BrFar(l24);
			}
			else
			{
				if (!flag12)
				{
					goto IL_0441;
				}
				Ldloc(pos);
				Ldloc(startingPos);
				BeqFar(l24);
				Ldloc(iterationCount);
				Ldc(n2);
				BgeFar(l24);
				BrFar(l23);
			}
			goto IL_0487;
			IL_0487:
			MarkLabel(label6);
			Ldloc(iterationCount);
			Ldc(1);
			Sub();
			Stloc(iterationCount);
			Ldloc(iterationCount);
			Ldc(0);
			BltFar(label5);
			EmitStackPop();
			Stloc(pos);
			SliceInputSpan();
			if (startingPos != null)
			{
				EmitStackPop();
				Stloc(startingPos);
			}
			EmitUncaptureUntilPopped();
			if (m3 > 0)
			{
				if (flag13)
				{
					Ldloc(iterationCount);
					Ldc(0);
					BeqFar(label5);
					if (m3 > 1)
					{
						Ldloc(iterationCount);
						Ldc(m3);
						BltFar(doneLabel);
					}
				}
				else
				{
					Label l25 = DefineLabel();
					Ldloc(iterationCount);
					Ldc(m3);
					Bge(l25);
					if (m3 > 1)
					{
						Ldloc(iterationCount);
						Ldc(0);
						BeqFar(label5);
						Ldloc(startingStackpos);
						Stloc(stackpos);
					}
					BrFar(label5);
					MarkLabel(l25);
				}
			}
			if (flag11)
			{
				doneLabel = label5;
				MarkLabel(l24);
				if (startingStackpos != null)
				{
					Ldloc(startingStackpos);
					Stloc(stackpos);
				}
			}
			else
			{
				if (flag13)
				{
					BrFar(l24);
					Label label7 = DefineLabel();
					MarkLabel(label7);
					EmitTimeoutCheckIfNeeded();
					Ldloc(iterationCount);
					Ldc(0);
					BeqFar(label5);
					BrFar(doneLabel);
					doneLabel = label7;
				}
				MarkLabel(l24);
				if (analysis.IsInLoop(node))
				{
					EmitStackResizeIfNeeded(1 + ((startingPos != null) ? 1 : 0) + ((startingStackpos != null) ? 1 : 0));
					if (startingPos != null)
					{
						EmitStackPush(delegate
						{
							Ldloc(startingPos);
						});
					}
					if (startingStackpos != null)
					{
						EmitStackPush(delegate
						{
							Ldloc(startingStackpos);
						});
					}
					EmitStackPush(delegate
					{
						Ldloc(iterationCount);
					});
					Label l26 = DefineLabel();
					BrFar(l26);
					Label label8 = DefineLabel();
					MarkLabel(label8);
					EmitTimeoutCheckIfNeeded();
					EmitStackPop();
					Stloc(iterationCount);
					if (startingStackpos != null)
					{
						EmitStackPop();
						Stloc(startingStackpos);
					}
					if (startingPos != null)
					{
						EmitStackPop();
						Stloc(startingPos);
					}
					BrFar(doneLabel);
					doneLabel = label8;
					MarkLabel(l26);
				}
			}
			return;
			IL_0441:
			Ldloc(iterationCount);
			Ldc(n2);
			BgeFar(l24);
			BrFar(l23);
			goto IL_0487;
			IL_047a:
			BrFar(l23);
			goto IL_0487;
		}
		void EmitMultiChar(RegexNode node, bool emitLengthCheck)
		{
			EmitMultiCharString(node.Str, emitLengthCheck, (node.Options & RegexOptions.RightToLeft) != 0);
		}
		void EmitMultiCharString(string str, bool emitLengthCheck, bool rightToLeft)
		{
			if (rightToLeft)
			{
				TransferSliceStaticPosToPos();
				Ldloc(pos);
				Ldc(str.Length);
				Sub();
				Ldloca(inputSpan);
				Call(s_spanGetLengthMethod);
				BgeUnFar(doneLabel);
				for (int num3 = str.Length - 1; num3 >= 0; num3--)
				{
					Ldloc(pos);
					Ldc(1);
					Sub();
					Stloc(pos);
					Ldloca(inputSpan);
					Ldloc(pos);
					Call(s_spanGetItemMethod);
					LdindU2();
					Ldc(str[num3]);
					BneFar(doneLabel);
				}
			}
			else
			{
				Ldloca(slice);
				Ldc(sliceStaticPos);
				Call(s_spanSliceIntMethod);
				Ldstr(str);
				Call(s_stringAsSpanMethod);
				Call(s_spanStartsWithSpan);
				BrfalseFar(doneLabel);
				sliceStaticPos += str.Length;
			}
		}
		void EmitNegativeLookaroundAssertion(RegexNode node)
		{
			if (analysis.HasRightToLeft)
			{
				TransferSliceStaticPosToPos(forceSliceReload: true);
			}
			Label label25 = doneLabel;
			LocalBuilder lt7 = DeclareInt32();
			Ldloc(pos);
			Stloc(lt7);
			int num15 = sliceStaticPos;
			Label label26 = (doneLabel = DefineLabel());
			EmitTimeoutCheckIfNeeded();
			RegexNode node8 = node.Child(0);
			if (analysis.MayBacktrack(node8))
			{
				EmitAtomic(node, null);
			}
			else
			{
				EmitNode(node8);
			}
			BrFar(label25);
			MarkLabel(label26);
			if (doneLabel == label26)
			{
				doneLabel = label25;
			}
			Ldloc(lt7);
			Stloc(pos);
			SliceInputSpan();
			sliceStaticPos = num15;
			doneLabel = label25;
		}
		void EmitNode(RegexNode node, RegexNode subsequent = null, bool emitLengthChecksIfRequired = true)
		{
			if (_regexTree.FindOptimizations.FindMode == FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight && _regexTree.FindOptimizations.LiteralAfterLoop?.LoopNode == node)
			{
				Mvfldloc(s_runtrackposField, pos);
				SliceInputSpan();
			}
			else if (!StackHelper.TryEnsureSufficientExecutionStack())
			{
				StackHelper.CallOnEmptyStack(EmitNode, node, subsequent, emitLengthChecksIfRequired);
			}
			else
			{
				if ((node.Options & RegexOptions.RightToLeft) != 0)
				{
					TransferSliceStaticPosToPos();
				}
				switch (node.Kind)
				{
				case RegexNodeKind.Bol:
				case RegexNodeKind.Eol:
				case RegexNodeKind.Beginning:
				case RegexNodeKind.Start:
				case RegexNodeKind.EndZ:
				case RegexNodeKind.End:
					EmitAnchors(node);
					break;
				case RegexNodeKind.Boundary:
				case RegexNodeKind.NonBoundary:
				case RegexNodeKind.ECMABoundary:
				case RegexNodeKind.NonECMABoundary:
					EmitBoundary(node);
					break;
				case RegexNodeKind.Multi:
					EmitMultiChar(node, emitLengthChecksIfRequired);
					break;
				case RegexNodeKind.One:
				case RegexNodeKind.Notone:
				case RegexNodeKind.Set:
					EmitSingleChar(node, emitLengthChecksIfRequired);
					break;
				case RegexNodeKind.Oneloop:
				case RegexNodeKind.Notoneloop:
				case RegexNodeKind.Setloop:
					EmitSingleCharLoop(node, subsequent, emitLengthChecksIfRequired);
					break;
				case RegexNodeKind.Onelazy:
				case RegexNodeKind.Notonelazy:
				case RegexNodeKind.Setlazy:
					EmitSingleCharLazy(node, subsequent, emitLengthChecksIfRequired);
					break;
				case RegexNodeKind.Oneloopatomic:
				case RegexNodeKind.Notoneloopatomic:
				case RegexNodeKind.Setloopatomic:
					EmitSingleCharAtomicLoop(node);
					break;
				case RegexNodeKind.Loop:
					EmitLoop(node);
					break;
				case RegexNodeKind.Lazyloop:
					EmitLazy(node);
					break;
				case RegexNodeKind.Alternate:
					EmitAlternation(node);
					break;
				case RegexNodeKind.Concatenate:
					EmitConcatenation(node, subsequent, emitLengthChecksIfRequired);
					break;
				case RegexNodeKind.Atomic:
					EmitAtomic(node, subsequent);
					break;
				case RegexNodeKind.Backreference:
					EmitBackreference(node);
					break;
				case RegexNodeKind.BackreferenceConditional:
					EmitBackreferenceConditional(node);
					break;
				case RegexNodeKind.ExpressionConditional:
					EmitExpressionConditional(node);
					break;
				case RegexNodeKind.Capture:
					EmitCapture(node, subsequent);
					break;
				case RegexNodeKind.PositiveLookaround:
					EmitPositiveLookaroundAssertion(node);
					break;
				case RegexNodeKind.NegativeLookaround:
					EmitNegativeLookaroundAssertion(node);
					break;
				case RegexNodeKind.Nothing:
					BrFar(doneLabel);
					break;
				case RegexNodeKind.Empty:
					break;
				case RegexNodeKind.UpdateBumpalong:
					EmitUpdateBumpalong(node);
					break;
				case RegexNodeKind.Group:
				case (RegexNodeKind)35:
				case (RegexNodeKind)36:
				case (RegexNodeKind)37:
				case (RegexNodeKind)38:
				case (RegexNodeKind)39:
				case (RegexNodeKind)40:
					break;
				}
			}
		}
		void EmitNonBacktrackingRepeater(RegexNode node)
		{
			TransferSliceStaticPosToPos();
			Label l21 = DefineLabel();
			Label l22 = DefineLabel();
			using RentedLocalBuilder rentedLocalBuilder6 = RentInt32Local();
			Ldc(0);
			Stloc(rentedLocalBuilder6);
			BrFar(l21);
			MarkLabel(l22);
			EmitNode(node.Child(0));
			TransferSliceStaticPosToPos();
			Ldloc(rentedLocalBuilder6);
			Ldc(1);
			Add();
			Stloc(rentedLocalBuilder6);
			MarkLabel(l21);
			Ldloc(rentedLocalBuilder6);
			Ldc(node.M);
			BltFar(l22);
		}
		void EmitPositiveLookaroundAssertion(RegexNode node)
		{
			if (analysis.HasRightToLeft)
			{
				TransferSliceStaticPosToPos(forceSliceReload: true);
			}
			LocalBuilder lt6 = DeclareInt32();
			Ldloc(pos);
			Stloc(lt6);
			int num14 = sliceStaticPos;
			EmitTimeoutCheckIfNeeded();
			RegexNode node6 = node.Child(0);
			if (analysis.MayBacktrack(node6))
			{
				EmitAtomic(node, null);
			}
			else
			{
				EmitNode(node6);
			}
			Ldloc(lt6);
			Stloc(pos);
			SliceInputSpan();
			sliceStaticPos = num14;
		}
		void EmitSingleChar(RegexNode node, bool emitLengthCheck = true, LocalBuilder offset = null)
		{
			bool flag8 = (node.Options & RegexOptions.RightToLeft) != 0;
			if (emitLengthCheck)
			{
				if (!flag8)
				{
					EmitSpanLengthCheck(1, offset);
				}
				else
				{
					Ldloc(pos);
					Ldc(1);
					Sub();
					Ldloca(inputSpan);
					Call(s_spanGetLengthMethod);
					BgeUnFar(doneLabel);
				}
			}
			if (!flag8)
			{
				Ldloca(slice);
				EmitSum(sliceStaticPos, offset);
			}
			else
			{
				Ldloca(inputSpan);
				EmitSum(-1, pos);
			}
			Call(s_spanGetItemMethod);
			LdindU2();
			if (node.IsSetFamily)
			{
				EmitMatchCharacterClass(node.Str);
				BrfalseFar(doneLabel);
			}
			else
			{
				Ldc(node.Ch);
				if (node.IsOneFamily)
				{
					BneFar(doneLabel);
				}
				else
				{
					BeqFar(doneLabel);
				}
			}
			if (!flag8)
			{
				sliceStaticPos++;
			}
			else
			{
				Ldloc(pos);
				Ldc(1);
				Sub();
				Stloc(pos);
			}
		}
		void EmitSingleCharAtomicLoop(RegexNode node)
		{
			if (node.M == node.N)
			{
				EmitSingleCharRepeater(node);
			}
			else
			{
				if (node.M != 0 || node.N != 1)
				{
					int m2 = node.M;
					int n = node.N;
					bool flag10 = (node.Options & RegexOptions.RightToLeft) != 0;
					using RentedLocalBuilder rentedLocalBuilder4 = RentInt32Local();
					Label l16 = DefineLabel();
					int literalLength5;
					if (flag10)
					{
						TransferSliceStaticPosToPos();
						Label l17 = DefineLabel();
						Label l18 = DefineLabel();
						Ldc(0);
						Stloc(rentedLocalBuilder4);
						BrFar(l17);
						MarkLabel(l18);
						Ldloc(pos);
						Ldloc(rentedLocalBuilder4);
						BleFar(l16);
						Ldloca(inputSpan);
						Ldloc(pos);
						Ldloc(rentedLocalBuilder4);
						Sub();
						Ldc(1);
						Sub();
						Call(s_spanGetItemMethod);
						LdindU2();
						if (node.IsSetFamily)
						{
							EmitMatchCharacterClass(node.Str);
							BrfalseFar(l16);
						}
						else
						{
							Ldc(node.Ch);
							if (node.IsOneFamily)
							{
								BneFar(l16);
							}
							else
							{
								BeqFar(l16);
							}
						}
						Ldloc(rentedLocalBuilder4);
						Ldc(1);
						Add();
						Stloc(rentedLocalBuilder4);
						MarkLabel(l17);
						if (n != int.MaxValue)
						{
							Ldloc(rentedLocalBuilder4);
							Ldc(n);
							BltFar(l18);
						}
						else
						{
							BrFar(l18);
						}
					}
					else if (node.IsSetFamily && n == int.MaxValue && node.Str == "\0\u0001\0\0")
					{
						TransferSliceStaticPosToPos();
						Ldloca(inputSpan);
						Call(s_spanGetLengthMethod);
						Ldloc(pos);
						Sub();
						Stloc(rentedLocalBuilder4);
					}
					else if (n == int.MaxValue && CanEmitIndexOf(node, out literalLength5))
					{
						if (sliceStaticPos > 0)
						{
							Ldloca(slice);
							Ldc(sliceStaticPos);
							Call(s_spanSliceIntMethod);
						}
						else
						{
							Ldloc(slice);
						}
						EmitIndexOf(node, useLast: false, negate: true);
						Stloc(rentedLocalBuilder4);
						Ldloc(rentedLocalBuilder4);
						Ldc(0);
						BgeFar(l16);
						Ldloca(slice);
						Call(s_spanGetLengthMethod);
						if (sliceStaticPos > 0)
						{
							Ldc(sliceStaticPos);
							Sub();
						}
						Stloc(rentedLocalBuilder4);
					}
					else
					{
						TransferSliceStaticPosToPos();
						Label l19 = DefineLabel();
						Label l20 = DefineLabel();
						Ldc(0);
						Stloc(rentedLocalBuilder4);
						BrFar(l19);
						MarkLabel(l20);
						Ldloc(rentedLocalBuilder4);
						Ldloca(slice);
						Call(s_spanGetLengthMethod);
						BgeUnFar(l16);
						Ldloca(slice);
						Ldloc(rentedLocalBuilder4);
						Call(s_spanGetItemMethod);
						LdindU2();
						if (node.IsSetFamily)
						{
							EmitMatchCharacterClass(node.Str);
							BrfalseFar(l16);
						}
						else
						{
							Ldc(node.Ch);
							if (node.IsOneFamily)
							{
								BneFar(l16);
							}
							else
							{
								BeqFar(l16);
							}
						}
						Ldloc(rentedLocalBuilder4);
						Ldc(1);
						Add();
						Stloc(rentedLocalBuilder4);
						MarkLabel(l19);
						if (n != int.MaxValue)
						{
							Ldloc(rentedLocalBuilder4);
							Ldc(n);
							BltFar(l20);
						}
						else
						{
							BrFar(l20);
						}
					}
					MarkLabel(l16);
					if (m2 > 0)
					{
						Ldloc(rentedLocalBuilder4);
						Ldc(m2);
						BltFar(doneLabel);
					}
					if (!flag10)
					{
						Ldloca(slice);
						Ldloc(rentedLocalBuilder4);
						Call(s_spanSliceIntMethod);
						Stloc(slice);
						Ldloc(pos);
						Ldloc(rentedLocalBuilder4);
						Add();
						Stloc(pos);
					}
					else
					{
						Ldloc(pos);
						Ldloc(rentedLocalBuilder4);
						Sub();
						Stloc(pos);
					}
					return;
				}
				EmitAtomicSingleCharZeroOrOne(node);
			}
		}
		void EmitSingleCharLazy(RegexNode node, RegexNode subsequent = null, bool emitLengthChecksIfRequired = true)
		{
			if (node.M > 0)
			{
				EmitSingleCharRepeater(node, emitLengthChecksIfRequired);
			}
			if (node.M == node.N || analysis.IsAtomicByAncestor(node))
			{
				return;
			}
			bool flag3 = (node.Options & RegexOptions.RightToLeft) != 0;
			TransferSliceStaticPosToPos();
			LocalBuilder iterationCount = null;
			int? num2 = null;
			if (node.N != int.MaxValue)
			{
				num2 = node.N - node.M;
				iterationCount = DeclareInt32();
				Ldc(0);
				Stloc(iterationCount);
			}
			LocalBuilder capturepos = (expressionHasCaptures ? DeclareInt32() : null);
			LocalBuilder startingPos = DeclareInt32();
			Ldloc(pos);
			Stloc(startingPos);
			Label l9 = DefineLabel();
			BrFar(l9);
			Label label2 = DefineLabel();
			MarkLabel(label2);
			if (capturepos != null)
			{
				EmitUncaptureUntil(capturepos);
			}
			if (num2.HasValue)
			{
				Ldloc(iterationCount);
				Ldc(num2.Value);
				BgeFar(doneLabel);
				Ldloc(iterationCount);
				Ldc(1);
				Add();
				Stloc(iterationCount);
			}
			EmitTimeoutCheckIfNeeded();
			Ldloc(startingPos);
			Stloc(pos);
			SliceInputSpan();
			EmitSingleChar(node);
			TransferSliceStaticPosToPos();
			if (!flag3 && iterationCount == null && node.Kind == RegexNodeKind.Notonelazy)
			{
				RegexNode.StartingLiteralData? startingLiteralData = subsequent?.FindStartingLiteral(4);
				if (startingLiteralData.HasValue)
				{
					RegexNode.StartingLiteralData valueOrDefault = startingLiteralData.GetValueOrDefault();
					if (!valueOrDefault.Negated && (valueOrDefault.String != null || valueOrDefault.SetChars != null || (valueOrDefault.AsciiChars != null && node.Ch < '\u0080') || valueOrDefault.Range.LowInclusive == valueOrDefault.Range.HighInclusive || (valueOrDefault.Range.LowInclusive <= node.Ch && node.Ch <= valueOrDefault.Range.HighInclusive)))
					{
						Ldloc(slice);
						bool flag4;
						if (valueOrDefault.String != null)
						{
							flag4 = valueOrDefault.String[0] == node.Ch;
							if (flag4)
							{
								Ldc(node.Ch);
								Call(s_spanIndexOfChar);
							}
							else
							{
								Ldc(node.Ch);
								Ldc(valueOrDefault.String[0]);
								Call(s_spanIndexOfAnyCharChar);
							}
						}
						else if (valueOrDefault.SetChars != null)
						{
							flag4 = valueOrDefault.SetChars.Contains(node.Ch);
							int length = valueOrDefault.SetChars.Length;
							if (flag4)
							{
								switch (length)
								{
								case 2:
									Ldc(valueOrDefault.SetChars[0]);
									Ldc(valueOrDefault.SetChars[1]);
									Call(s_spanIndexOfAnyCharChar);
									break;
								case 3:
									Ldc(valueOrDefault.SetChars[0]);
									Ldc(valueOrDefault.SetChars[1]);
									Ldc(valueOrDefault.SetChars[2]);
									Call(s_spanIndexOfAnyCharCharChar);
									break;
								default:
									Ldstr(valueOrDefault.SetChars);
									Call(s_stringAsSpanMethod);
									Call(s_spanIndexOfAnySpan);
									break;
								}
							}
							else if (length == 2)
							{
								Ldc(node.Ch);
								Ldc(valueOrDefault.SetChars[0]);
								Ldc(valueOrDefault.SetChars[1]);
								Call(s_spanIndexOfAnyCharCharChar);
							}
							else
							{
								Ldstr($"{node.Ch}{valueOrDefault.SetChars}");
								Call(s_stringAsSpanMethod);
								Call(s_spanIndexOfAnySpan);
							}
						}
						else if (valueOrDefault.AsciiChars != null)
						{
							char[] array = valueOrDefault.AsciiChars;
							flag4 = array.AsSpan().Contains(node.Ch);
							if (!flag4)
							{
								Array.Resize(ref array, array.Length + 1);
								array[^1] = node.Ch;
							}
							LoadSearchValues(array);
							Call(s_spanIndexOfAnySearchValues);
						}
						else if (valueOrDefault.Range.LowInclusive == valueOrDefault.Range.HighInclusive)
						{
							flag4 = valueOrDefault.Range.LowInclusive == node.Ch;
							if (flag4)
							{
								Ldc(node.Ch);
								Call(s_spanIndexOfChar);
							}
							else
							{
								Ldc(node.Ch);
								Ldc(valueOrDefault.Range.LowInclusive);
								Call(s_spanIndexOfAnyCharChar);
							}
						}
						else
						{
							flag4 = true;
							Ldc(valueOrDefault.Range.LowInclusive);
							Ldc(valueOrDefault.Range.HighInclusive);
							Call(s_spanIndexOfAnyInRange);
						}
						Stloc(startingPos);
						if (flag4)
						{
							Ldloc(startingPos);
							Ldc(0);
							BltFar(doneLabel);
						}
						else
						{
							Ldloc(startingPos);
							Ldloca(slice);
							Call(s_spanGetLengthMethod);
							BgeUnFar(doneLabel);
							Ldloca(slice);
							Ldloc(startingPos);
							Call(s_spanGetItemMethod);
							LdindU2();
							Ldc(node.Ch);
							BeqFar(doneLabel);
						}
						Ldloc(pos);
						Ldloc(startingPos);
						Add();
						Stloc(pos);
						SliceInputSpan();
						goto IL_0895;
					}
				}
			}
			if (!flag3 && iterationCount == null && node.Kind == RegexNodeKind.Setlazy && node.Str == "\0\u0001\0\0")
			{
				RegexNode regexNode2 = subsequent?.FindStartingLiteralNode();
				if (regexNode2 != null && CanEmitIndexOf(regexNode2, out var _))
				{
					Ldloc(slice);
					EmitIndexOf(node, useLast: false, negate: false);
					Stloc(startingPos);
					Ldloc(startingPos);
					Ldc(0);
					BltFar(doneLabel);
					Ldloc(pos);
					Ldloc(startingPos);
					Add();
					Stloc(pos);
					SliceInputSpan();
				}
			}
			goto IL_0895;
			IL_0895:
			Ldloc(pos);
			Stloc(startingPos);
			Label label3 = doneLabel;
			doneLabel = label2;
			MarkLabel(l9);
			if (capturepos != null)
			{
				Ldthis();
				Call(s_crawlposMethod);
				Stloc(capturepos);
			}
			if (analysis.IsInLoop(node))
			{
				EmitStackResizeIfNeeded(1 + ((capturepos != null) ? 1 : 0) + ((iterationCount != null) ? 1 : 0));
				EmitStackPush(delegate
				{
					Ldloc(startingPos);
				});
				if (capturepos != null)
				{
					EmitStackPush(delegate
					{
						Ldloc(capturepos);
					});
				}
				if (iterationCount != null)
				{
					EmitStackPush(delegate
					{
						Ldloc(iterationCount);
					});
				}
				Label l10 = DefineLabel();
				BrFar(l10);
				Label label4 = DefineLabel();
				MarkLabel(label4);
				if (iterationCount != null)
				{
					EmitStackPop();
					Stloc(iterationCount);
				}
				if (capturepos != null)
				{
					EmitStackPop();
					Stloc(capturepos);
				}
				EmitStackPop();
				Stloc(startingPos);
				BrFar(doneLabel);
				doneLabel = label4;
				MarkLabel(l10);
			}
		}
		void EmitSingleCharLoop(RegexNode node, RegexNode subsequent = null, bool emitLengthChecksIfRequired = true)
		{
			if (analysis.IsAtomicByAncestor(node))
			{
				EmitSingleCharAtomicLoop(node);
				return;
			}
			if (node.M == node.N)
			{
				EmitSingleCharRepeater(node, emitLengthChecksIfRequired);
				return;
			}
			Label label = DefineLabel();
			Label l8 = DefineLabel();
			LocalBuilder startingPos = DeclareInt32();
			LocalBuilder endingPos = DeclareInt32();
			LocalBuilder localBuilder = (expressionHasCaptures ? DeclareInt32() : null);
			bool flag = (node.Options & RegexOptions.RightToLeft) != 0;
			bool flag2 = analysis.IsInLoop(node);
			TransferSliceStaticPosToPos();
			Ldloc(pos);
			Stloc(startingPos);
			EmitSingleCharAtomicLoop(node);
			TransferSliceStaticPosToPos();
			Ldloc(pos);
			Stloc(endingPos);
			if (node.M > 0)
			{
				Ldloc(startingPos);
				Ldc((!flag) ? node.M : (-node.M));
				Add();
				Stloc(startingPos);
			}
			BrFar(l8);
			MarkLabel(label);
			if (flag2)
			{
				if (localBuilder != null)
				{
					EmitStackPop();
					Stloc(localBuilder);
					EmitUncaptureUntil(localBuilder);
				}
				EmitStackPop();
				Stloc(endingPos);
				EmitStackPop();
				Stloc(startingPos);
			}
			else if (localBuilder != null)
			{
				EmitUncaptureUntil(localBuilder);
			}
			EmitTimeoutCheckIfNeeded();
			Ldloc(startingPos);
			Ldloc(endingPos);
			if (!flag)
			{
				BgeFar(doneLabel);
			}
			else
			{
				BleFar(doneLabel);
			}
			if (!flag && node.N > 1)
			{
				RegexNode regexNode = subsequent?.FindStartingLiteralNode();
				if (regexNode != null && CanEmitIndexOf(regexNode, out var literalLength2))
				{
					Ldloca(inputSpan);
					Ldloc(startingPos);
					if (literalLength2 > 1)
					{
						Ldloca(inputSpan);
						Call(s_spanGetLengthMethod);
						Ldloc(endingPos);
						Ldc(literalLength2 - 1);
						Add();
						Call(s_mathMinIntInt);
					}
					else
					{
						Ldloc(endingPos);
					}
					Ldloc(startingPos);
					Sub();
					Call(s_spanSliceIntIntMethod);
					EmitIndexOf(regexNode, useLast: true, negate: false);
					Stloc(endingPos);
					Ldloc(endingPos);
					Ldc(0);
					BltFar(doneLabel);
					Ldloc(endingPos);
					Ldloc(startingPos);
					Add();
					Stloc(endingPos);
					goto IL_03dc;
				}
			}
			Ldloc(endingPos);
			Ldc((!flag) ? 1 : (-1));
			Sub();
			Stloc(endingPos);
			goto IL_03dc;
			IL_03dc:
			Ldloc(endingPos);
			Stloc(pos);
			if (!flag)
			{
				SliceInputSpan();
			}
			MarkLabel(l8);
			if (flag2)
			{
				EmitStackResizeIfNeeded(2 + ((localBuilder != null) ? 1 : 0));
				EmitStackPush(delegate
				{
					Ldloc(startingPos);
				});
				EmitStackPush(delegate
				{
					Ldloc(endingPos);
				});
				if (localBuilder != null)
				{
					EmitStackPush(delegate
					{
						Ldthis();
						Call(s_crawlposMethod);
					});
				}
			}
			else if (localBuilder != null)
			{
				Ldthis();
				Call(s_crawlposMethod);
				Stloc(localBuilder);
			}
			doneLabel = label;
		}
		void EmitSingleCharRepeater(RegexNode node, bool emitLengthChecksIfRequired = true)
		{
			int m = node.M;
			bool flag9 = (node.Options & RegexOptions.RightToLeft) != 0;
			int num4 = m;
			if (num4 <= 64)
			{
				switch (num4)
				{
				case 0:
					return;
				case 1:
					EmitSingleChar(node, emitLengthChecksIfRequired);
					return;
				}
				if (node.IsOneFamily)
				{
					EmitMultiCharString(new string(node.Ch, m), emitLengthChecksIfRequired, flag9);
					return;
				}
			}
			if (flag9)
			{
				TransferSliceStaticPosToPos();
				Label l12 = DefineLabel();
				Label l13 = DefineLabel();
				using RentedLocalBuilder rentedLocalBuilder = RentInt32Local();
				Ldc(0);
				Stloc(rentedLocalBuilder);
				BrFar(l12);
				MarkLabel(l13);
				EmitSingleChar(node);
				Ldloc(rentedLocalBuilder);
				Ldc(1);
				Add();
				Stloc(rentedLocalBuilder);
				MarkLabel(l12);
				Ldloc(rentedLocalBuilder);
				Ldc(m);
				BltFar(l13);
				return;
			}
			if (emitLengthChecksIfRequired)
			{
				EmitSpanLengthCheck(m);
			}
			if (node.IsSetFamily && node.Str == "\0\u0001\0\0")
			{
				sliceStaticPos += m;
			}
			else if (m <= 16)
			{
				for (int i = 0; i < m; i++)
				{
					EmitSingleChar(node, emitLengthCheck: false);
				}
			}
			else
			{
				Ldloca(slice);
				Ldc(sliceStaticPos);
				Ldc(m);
				Call(s_spanSliceIntIntMethod);
				if (CanEmitIndexOf(node, out var _))
				{
					EmitIndexOf(node, useLast: false, negate: true);
					Ldc(0);
					BgeFar(doneLabel);
				}
				else
				{
					using RentedLocalBuilder rentedLocalBuilder2 = RentReadOnlySpanCharLocal();
					Stloc(rentedLocalBuilder2);
					Label l14 = DefineLabel();
					Label l15 = DefineLabel();
					using RentedLocalBuilder rentedLocalBuilder3 = RentInt32Local();
					Ldc(0);
					Stloc(rentedLocalBuilder3);
					BrFar(l14);
					MarkLabel(l15);
					LocalBuilder localBuilder2 = slice;
					int num5 = sliceStaticPos;
					slice = rentedLocalBuilder2;
					sliceStaticPos = 0;
					EmitSingleChar(node, emitLengthCheck: false, rentedLocalBuilder3);
					slice = localBuilder2;
					sliceStaticPos = num5;
					Ldloc(rentedLocalBuilder3);
					Ldc(1);
					Add();
					Stloc(rentedLocalBuilder3);
					MarkLabel(l14);
					Ldloc(rentedLocalBuilder3);
					Ldloca(rentedLocalBuilder2);
					Call(s_spanGetLengthMethod);
					BltFar(l15);
				}
				sliceStaticPos += m;
			}
		}
		void EmitSpanLengthCheck(int requiredLength, LocalBuilder dynamicRequiredLength = null)
		{
			EmitSum(sliceStaticPos + requiredLength - 1, dynamicRequiredLength);
			Ldloca(slice);
			Call(s_spanGetLengthMethod);
			BgeUnFar(doneLabel);
		}
		void EmitStackPop()
		{
			Ldthisfld(s_runstackField);
			Ldloc(stackpos);
			Ldc(1);
			Sub();
			Stloc(stackpos);
			Ldloc(stackpos);
			LdelemI4();
		}
		void EmitStackPush(Action load)
		{
			Ldthisfld(s_runstackField);
			Ldloc(stackpos);
			load();
			StelemI4();
			Ldloc(stackpos);
			Ldc(1);
			Add();
			Stloc(stackpos);
		}
		void EmitStackResizeIfNeeded(int count)
		{
			Label l45 = DefineLabel();
			Ldloc(stackpos);
			Ldthisfld(s_runstackField);
			Ldlen();
			if (count > 1)
			{
				Ldc(count - 1);
				Sub();
			}
			Blt(l45);
			Ldthis();
			_ilg.Emit(OpCodes.Ldflda, s_runstackField);
			Ldthisfld(s_runstackField);
			Ldlen();
			Ldc(2);
			Mul();
			Call(s_arrayResize);
			MarkLabel(l45);
		}
		void EmitSum(int constant, LocalBuilder local)
		{
			if (local == null)
			{
				Ldc(constant);
			}
			else if (constant == 0)
			{
				Ldloc(local);
			}
			else
			{
				Ldloc(local);
				Ldc(constant);
				Add();
			}
		}
		void EmitUncaptureUntil(LocalBuilder startingCapturePos)
		{
			Label l41 = DefineLabel();
			Label l42 = DefineLabel();
			Br(l41);
			MarkLabel(l42);
			Ldthis();
			Call(s_uncaptureMethod);
			MarkLabel(l41);
			Ldthis();
			Call(s_crawlposMethod);
			Ldloc(startingCapturePos);
			Bgt(l42);
		}
		void EmitUncaptureUntilPopped()
		{
			if (expressionHasCaptures)
			{
				using (RentedLocalBuilder rentedLocalBuilder5 = RentInt32Local())
				{
					EmitStackPop();
					Stloc(rentedLocalBuilder5);
					EmitUncaptureUntil(rentedLocalBuilder5);
				}
			}
		}
		void EmitUpdateBumpalong(RegexNode node)
		{
			TransferSliceStaticPosToPos();
			Ldthisfld(s_runtextposField);
			Ldloc(pos);
			Label l47 = DefineLabel();
			Bge(l47);
			Ldthis();
			Ldloc(pos);
			Stfld(s_runtextposField);
			MarkLabel(l47);
		}
		static RegexNode GetSubsequent(int index, RegexNode node, RegexNode subsequent)
		{
			int num10 = node.ChildCount();
			for (int j = index + 1; j < num10; j++)
			{
				RegexNode regexNode5 = node.Child(j);
				if (regexNode5.Kind != RegexNodeKind.UpdateBumpalong)
				{
					return regexNode5;
				}
			}
			return subsequent;
		}
		void SliceInputSpan()
		{
			Ldloca(inputSpan);
			Ldloc(pos);
			Call(s_spanSliceIntMethod);
			Stloc(slice);
		}
		void TransferSliceStaticPosToPos(bool forceSliceReload = false)
		{
			if (sliceStaticPos > 0)
			{
				Ldloc(pos);
				Ldc(sliceStaticPos);
				Add();
				Stloc(pos);
				sliceStaticPos = 0;
				SliceInputSpan();
			}
			else if (forceSliceReload)
			{
				SliceInputSpan();
			}
		}
	}

	protected void EmitScan(RegexOptions options, DynamicMethod tryFindNextStartingPositionMethod, DynamicMethod tryMatchAtCurrentPositionMethod)
	{
		bool flag = (options & RegexOptions.RightToLeft) != 0;
		RegexNode regexNode = _regexTree.Root.Child(0);
		Label l = DefineLabel();
		RegexNodeKind kind = regexNode.Kind;
		if (kind - 9 <= RegexNodeKind.Oneloop)
		{
			Ldthis();
			Ldarg_1();
			Call(tryFindNextStartingPositionMethod);
			Brfalse(l);
			LocalBuilder lt = DeclareInt32();
			Mvfldloc(s_runtextposField, lt);
			LocalBuilder lt2 = DeclareInt32();
			Ldloc(lt);
			Ldc(((regexNode.Kind != RegexNodeKind.Multi) ? 1 : regexNode.Str.Length) * ((!flag) ? 1 : (-1)));
			Add();
			Stloc(lt2);
			Ldthis();
			Ldloc(lt2);
			Stfld(s_runtextposField);
			Ldthis();
			Ldc(0);
			Ldloc(lt);
			Ldloc(lt2);
			Call(s_captureMethod);
		}
		else
		{
			FindNextStartingPositionMode findMode = _regexTree.FindOptimizations.FindMode;
			if (((uint)findMode <= 1u || findMode == FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start || findMode == FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End) ? true : false)
			{
				Ldthis();
				Ldarg_1();
				Call(tryFindNextStartingPositionMethod);
				Brfalse(l);
				Ldthis();
				Ldarg_1();
				Call(tryMatchAtCurrentPositionMethod);
				Brtrue(l);
				Ldthis();
				if (!flag)
				{
					Ldarga_s(1);
					Call(s_spanGetLengthMethod);
				}
				else
				{
					Ldc(0);
				}
				Stfld(s_runtextposField);
			}
			else
			{
				Label l2 = DefineLabel();
				MarkLabel(l2);
				Ldthis();
				Ldarg_1();
				Call(tryFindNextStartingPositionMethod);
				BrfalseFar(l);
				Ldthis();
				Ldarg_1();
				Call(tryMatchAtCurrentPositionMethod);
				BrtrueFar(l);
				Ldthisfld(s_runtextposField);
				if (!flag)
				{
					Ldarga_s(1);
					Call(s_spanGetLengthMethod);
				}
				else
				{
					Ldc(0);
				}
				Ceq();
				BrtrueFar(l);
				Ldthis();
				Ldthisfld(s_runtextposField);
				Ldc((!flag) ? 1 : (-1));
				Add();
				Stfld(s_runtextposField);
				EmitTimeoutCheckIfNeeded();
				BrFar(l2);
			}
		}
		MarkLabel(l);
		Ret();
	}

	private void EmitMatchCharacterClass(string charClass)
	{
		switch (charClass)
		{
		case "\0\u0001\0\0":
			Pop();
			Ldc(1);
			return;
		case "\0\0\u0001\t":
		case "\0\0\u0001\ufff7":
			Call(s_charIsDigitMethod);
			NegateIf(charClass == "\0\0\u0001\ufff7");
			return;
		case "\0\0\u0001d":
		case "\0\0\u0001ﾜ":
			Call(s_charIsWhiteSpaceMethod);
			NegateIf(charClass == "\0\0\u0001ﾜ");
			return;
		case "\0\0\n\0\u0002\u0004\u0005\u0003\u0001\u0006\t\u0013\0":
		case "\0\0\n\0\ufffe￼\ufffb\ufffd\uffff\ufffa\ufff7￭\0":
			Call(s_isWordCharMethod);
			NegateIf(charClass == "\0\0\n\0\ufffe￼\ufffb\ufffd\uffff\ufffa\ufff7￭\0");
			return;
		case "\0\0\u0001\u000f":
		case "\0\0\u0001\ufff1":
			Call(s_charIsControlMethod);
			NegateIf(charClass == "\0\0\u0001\ufff1");
			return;
		case "\0\0\a\0\u0002\u0004\u0005\u0003\u0001\0":
		case "\0\0\a\0\ufffe￼\ufffb\ufffd\uffff\0":
			Call(s_charIsLetterMethod);
			NegateIf(charClass == "\0\0\a\0\ufffe￼\ufffb\ufffd\uffff\0");
			return;
		case "\0\0\b\0\u0002\u0004\u0005\u0003\u0001\0\t":
		case "\u0001\0\b\0\u0002\u0004\u0005\u0003\u0001\0\t":
			Call(s_charIsLetterOrDigitMethod);
			NegateIf(charClass == "\u0001\0\b\0\u0002\u0004\u0005\u0003\u0001\0\t");
			return;
		case "\0\0\u0001\u0002":
		case "\0\0\u0001\ufffe":
			Call(s_charIsLowerMethod);
			NegateIf(charClass == "\0\0\u0001\ufffe");
			return;
		case "\0\0\u0001\u0001":
		case "\0\0\u0001\uffff":
			Call(s_charIsUpperMethod);
			NegateIf(charClass == "\0\0\u0001\uffff");
			return;
		case "\0\0\u0005\0\ufff7\ufff6\ufff5\0":
		case "\0\0\u0005\0\t\n\v\0":
			Call(s_charIsNumberMethod);
			NegateIf(charClass == "\0\0\u0005\0\ufff7\ufff6\ufff5\0");
			return;
		case "\0\0\t\0\u0013\u0014\u0016\u0019\u0015\u0018\u0017\0":
		case "\0\0\t\0￭￬￪\uffe7￫￨￩\0":
			Call(s_charIsPunctuationMethod);
			NegateIf(charClass == "\0\0\t\0￭￬￪\uffe7￫￨￩\0");
			return;
		case "\0\0\u0005\0\ufff3\ufff2\ufff4\0":
		case "\0\0\u0005\0\r\u000e\f\0":
			Call(s_charIsSeparatorMethod);
			NegateIf(charClass == "\0\0\u0005\0\ufff3\ufff2\ufff4\0");
			return;
		case "\0\0\u0006\0￥￤￦\uffe3\0":
		case "\0\0\u0006\0\u001b\u001c\u001a\u001d\0":
			Call(s_charIsSymbolMethod);
			NegateIf(charClass == "\0\0\u0006\0￥￤￦\uffe3\0");
			return;
		case "\0\u0004\0A[a{":
		case "\u0001\u0004\0A[a{":
			Call(s_charIsAsciiLetterMethod);
			NegateIf(charClass == "\u0001\u0004\0A[a{");
			return;
		case "\0\u0006\00:A[a{":
		case "\u0001\u0006\00:A[a{":
			Call(s_charIsAsciiLetterOrDigitMethod);
			NegateIf(charClass == "\u0001\u0006\00:A[a{");
			return;
		case "\0\u0006\00:AGag":
		case "\u0001\u0006\00:AGag":
			Call(s_charIsAsciiHexDigitMethod);
			NegateIf(charClass == "\u0001\u0006\00:AGag");
			return;
		case "\0\u0004\00:ag":
		case "\u0001\u0004\00:ag":
			Call(s_charIsAsciiHexDigitLowerMethod);
			NegateIf(charClass == "\u0001\u0004\00:ag");
			return;
		case "\0\u0004\00:AG":
		case "\u0001\u0004\00:AG":
			Call(s_charIsAsciiHexDigitUpperMethod);
			NegateIf(charClass == "\u0001\u0004\00:AG");
			return;
		}
		if (RegexCharClass.TryGetSingleRange(charClass, out var lowInclusive, out var highInclusive))
		{
			if (lowInclusive == highInclusive)
			{
				Ldc(lowInclusive);
				Ceq();
			}
			else
			{
				Ldc(lowInclusive);
				Sub();
				Ldc(highInclusive - lowInclusive + 1);
				CltUn();
			}
			NegateIf(RegexCharClass.IsNegated(charClass));
			return;
		}
		Span<UnicodeCategory> categories = stackalloc UnicodeCategory[1];
		if (RegexCharClass.TryGetOnlyCategories(charClass, categories, out var _, out var negated))
		{
			Call(s_charGetUnicodeInfo);
			Ldc((int)categories[0]);
			Ceq();
			NegateIf(negated);
			return;
		}
		RentedLocalBuilder tempLocal = RentInt32Local();
		RentedLocalBuilder resultLocal;
		Label doneLabel;
		Label comparisonLabel;
		try
		{
			Stloc(tempLocal);
			Span<char> chars = stackalloc char[3];
			int setChars = RegexCharClass.GetSetChars(charClass, chars);
			if ((uint)(setChars - 2) <= 1u)
			{
				if (RegexCharClass.DifferByOneBit(chars[0], chars[1], out var mask))
				{
					Ldloc(tempLocal);
					Ldc(mask);
					Or();
					Ldc(chars[1] | mask);
					Ceq();
				}
				else
				{
					Ldloc(tempLocal);
					Ldc(chars[0]);
					Ceq();
					Ldloc(tempLocal);
					Ldc(chars[1]);
					Ceq();
					Or();
				}
				if (setChars == 3)
				{
					Ldloc(tempLocal);
					Ldc(chars[2]);
					Ceq();
					Or();
				}
				NegateIf(RegexCharClass.IsNegated(charClass));
				return;
			}
			if (RegexCharClass.TryGetDoubleRange(charClass, out (char, char) range, out (char, char) range2) && char.IsAsciiLetter(range2.Item1) && char.IsAsciiLetter(range2.Item2) && (range.Item1 | 0x20) == range2.Item1 && (range.Item2 | 0x20) == range2.Item2)
			{
				bool condition = RegexCharClass.IsNegated(charClass);
				Ldloc(tempLocal);
				Ldc(32);
				Or();
				Ldc(range2.Item1);
				Sub();
				Ldc(range2.Item2 - range2.Item1 + 1);
				CltUn();
				NegateIf(condition);
				return;
			}
			RegexCharClass.CharClassAnalysisResults charClassAnalysisResults = RegexCharClass.Analyze(charClass);
			if (charClassAnalysisResults.OnlyRanges && charClassAnalysisResults.UpperBoundExclusiveIfOnlyRanges - charClassAnalysisResults.LowerBoundInclusiveIfOnlyRanges <= 32)
			{
				uint num = 0u;
				bool flag = RegexCharClass.IsNegated(charClass);
				for (int i = charClassAnalysisResults.LowerBoundInclusiveIfOnlyRanges; i < charClassAnalysisResults.UpperBoundExclusiveIfOnlyRanges; i++)
				{
					if (RegexCharClass.CharInClass((char)i, charClass) ^ flag)
					{
						num |= (uint)(1 << 31 - (i - charClassAnalysisResults.LowerBoundInclusiveIfOnlyRanges));
					}
				}
				LocalBuilder lt = _ilg.DeclareLocal(typeof(uint));
				Ldloc(tempLocal);
				Ldc(charClassAnalysisResults.LowerBoundInclusiveIfOnlyRanges);
				Sub();
				_ilg.Emit(OpCodes.Conv_U2);
				Stloc(lt);
				_ilg.Emit(OpCodes.Ldc_I4, num);
				Ldloc(lt);
				_ilg.Emit(OpCodes.Conv_I2);
				Ldc(31);
				And();
				Shl();
				Ldloc(lt);
				Ldc(32);
				_ilg.Emit(OpCodes.Conv_I4);
				Sub();
				And();
				Ldc(0);
				_ilg.Emit(OpCodes.Conv_I4);
				_ilg.Emit(OpCodes.Clt);
				NegateIf(flag);
				return;
			}
			if (IntPtr.Size == 8 && charClassAnalysisResults.OnlyRanges && charClassAnalysisResults.UpperBoundExclusiveIfOnlyRanges - charClassAnalysisResults.LowerBoundInclusiveIfOnlyRanges <= 64)
			{
				ulong num2 = 0uL;
				bool flag2 = RegexCharClass.IsNegated(charClass);
				for (int j = charClassAnalysisResults.LowerBoundInclusiveIfOnlyRanges; j < charClassAnalysisResults.UpperBoundExclusiveIfOnlyRanges; j++)
				{
					if (RegexCharClass.CharInClass((char)j, charClass) ^ flag2)
					{
						num2 |= (ulong)(1L << 63 - (j - charClassAnalysisResults.LowerBoundInclusiveIfOnlyRanges));
					}
				}
				LocalBuilder lt2 = _ilg.DeclareLocal(typeof(ulong));
				Ldloc(tempLocal);
				Ldc(charClassAnalysisResults.LowerBoundInclusiveIfOnlyRanges);
				Sub();
				_ilg.Emit(OpCodes.Conv_U8);
				Stloc(lt2);
				LdcI8((long)num2);
				Ldloc(lt2);
				_ilg.Emit(OpCodes.Conv_I4);
				Ldc(63);
				And();
				Shl();
				Ldloc(lt2);
				Ldc(64);
				_ilg.Emit(OpCodes.Conv_I8);
				Sub();
				And();
				Ldc(0);
				_ilg.Emit(OpCodes.Conv_I8);
				_ilg.Emit(OpCodes.Clt);
				NegateIf(flag2);
				return;
			}
			if (RegexCharClass.TryGetDoubleRange(charClass, out (char, char) range3, out (char, char) range4))
			{
				bool flag3 = RegexCharClass.IsNegated(charClass);
				if (range3.Item1 == range3.Item2)
				{
					Ldloc(tempLocal);
					Ldc(range3.Item1);
					Ceq();
				}
				else
				{
					Ldloc(tempLocal);
					Ldc(range3.Item1);
					Sub();
					Ldc(range3.Item2 - range3.Item1 + 1);
					CltUn();
				}
				NegateIf(flag3);
				if (range4.Item1 == range4.Item2)
				{
					Ldloc(tempLocal);
					Ldc(range4.Item1);
					Ceq();
				}
				else
				{
					Ldloc(tempLocal);
					Ldc(range4.Item1);
					Sub();
					Ldc(range4.Item2 - range4.Item1 + 1);
					CltUn();
				}
				NegateIf(flag3);
				if (flag3)
				{
					And();
				}
				else
				{
					Or();
				}
				return;
			}
			resultLocal = RentInt32Local();
			try
			{
				doneLabel = DefineLabel();
				comparisonLabel = DefineLabel();
				if (charClassAnalysisResults.ContainsNoAscii)
				{
					EmitContainsNoAscii();
					return;
				}
				if (charClassAnalysisResults.AllAsciiContained)
				{
					EmitAllAsciiContained();
					return;
				}
				string text = string.Create(8, charClass, delegate(Span<char> dest, string charClass)
				{
					for (int k = 0; k < 128; k++)
					{
						char ch = (char)k;
						if (RegexCharClass.CharInClass(ch, charClass))
						{
							dest[k >> 4] |= (char)(ushort)(1 << (k & 0xF));
						}
					}
				});
				if (!(text == "\0\0\0\0\0\0\0\0"))
				{
					if (text == "\uffff\uffff\uffff\uffff\uffff\uffff\uffff\uffff")
					{
						EmitAllAsciiContained();
						return;
					}
					Ldloc(tempLocal);
					Ldc(charClassAnalysisResults.ContainsOnlyAscii ? charClassAnalysisResults.UpperBoundExclusiveIfOnlyRanges : 128);
					Bge(comparisonLabel);
					switch (text)
					{
					case "\0\0\0Ͽ\ufffe߿\ufffe߿":
						Ldloc(tempLocal);
						Call(s_charIsAsciiLetterOrDigitMethod);
						break;
					case "\0\0\0Ͽ\0\0\0\0":
						Ldloc(tempLocal);
						Call(s_charIsAsciiDigitMethod);
						break;
					case "\0\0\0\0\ufffe߿\ufffe߿":
						Ldloc(tempLocal);
						Call(s_charIsAsciiLetterMethod);
						break;
					case "\0\0\0\0\0\0\ufffe߿":
						Ldloc(tempLocal);
						Call(s_charIsAsciiLetterLowerMethod);
						break;
					case "\0\0\0\0\ufffe߿\0\0":
						Ldloc(tempLocal);
						Call(s_charIsAsciiLetterUpperMethod);
						break;
					case "\0\0\0Ͽ~\0~\0":
						Ldloc(tempLocal);
						Call(s_charIsAsciiHexDigitMethod);
						break;
					case "\0\0\0Ͽ\0\0~\0":
						Ldloc(tempLocal);
						Call(s_charIsAsciiHexDigitLowerMethod);
						break;
					case "\0\0\0Ͽ~\0\0\0":
						Ldloc(tempLocal);
						Call(s_charIsAsciiHexDigitUpperMethod);
						break;
					default:
						Ldstr(text);
						Ldloc(tempLocal);
						Ldc(4);
						Shr();
						Call(s_stringGetCharsMethod);
						Ldc(1);
						Ldloc(tempLocal);
						Ldc(15);
						And();
						Ldc(31);
						And();
						Shl();
						And();
						Ldc(0);
						CgtUn();
						break;
					}
					Stloc(resultLocal);
					Br(doneLabel);
					MarkLabel(comparisonLabel);
					if (charClassAnalysisResults.ContainsOnlyAscii)
					{
						Ldc(0);
						Stloc(resultLocal);
					}
					else if (charClassAnalysisResults.AllNonAsciiContained)
					{
						Ldc(1);
						Stloc(resultLocal);
					}
					else
					{
						EmitCharInClass();
					}
					MarkLabel(doneLabel);
					Ldloc(resultLocal);
				}
				else
				{
					EmitContainsNoAscii();
				}
			}
			finally
			{
				((IDisposable)resultLocal).Dispose();
			}
		}
		finally
		{
			((IDisposable)tempLocal).Dispose();
		}
		void EmitAllAsciiContained()
		{
			Ldloc(tempLocal);
			Ldc(128);
			Blt(comparisonLabel);
			EmitCharInClass();
			Br(doneLabel);
			MarkLabel(comparisonLabel);
			Ldc(1);
			Stloc(resultLocal);
			MarkLabel(doneLabel);
			Ldloc(resultLocal);
		}
		void EmitCharInClass()
		{
			Ldloc(tempLocal);
			Ldstr(charClass);
			Call(s_charInClassMethod);
			Stloc(resultLocal);
		}
		void EmitContainsNoAscii()
		{
			Ldloc(tempLocal);
			Ldc(128);
			Blt(comparisonLabel);
			EmitCharInClass();
			Br(doneLabel);
			MarkLabel(comparisonLabel);
			Ldc(0);
			Stloc(resultLocal);
			MarkLabel(doneLabel);
			Ldloc(resultLocal);
		}
	}

	private void NegateIf(bool condition)
	{
		if (condition)
		{
			Ldc(0);
			Ceq();
		}
	}

	private void EmitTimeoutCheckIfNeeded()
	{
		if (_hasTimeout)
		{
			Ldthis();
			Call(s_checkTimeoutMethod);
		}
	}

	private void LoadSearchValues(ReadOnlySpan<char> chars)
	{
		List<SearchValues<char>> list = _searchValues ?? (_searchValues = new List<SearchValues<char>>());
		int count = list.Count;
		list.Add(SearchValues.Create(chars));
		Ldthisfld(s_searchValuesArrayField);
		Call(s_memoryMarshalGetArrayDataReferenceSearchValues);
		Ldc(count * IntPtr.Size);
		Add();
		_ilg.Emit(OpCodes.Ldind_Ref);
		Call(MakeUnsafeAs(list[count].GetType()));
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod", Justification = "Calling Unsafe.As<T> is safe since the T doesn't have trimming annotations.")]
		static MethodInfo MakeUnsafeAs(Type type)
		{
			return s_unsafeAs.MakeGenericMethod(type);
		}
	}
}
