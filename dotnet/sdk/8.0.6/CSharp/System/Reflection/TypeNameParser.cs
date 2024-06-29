using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading;

namespace System.Reflection;

[StructLayout(LayoutKind.Auto)]
internal ref struct TypeNameParser
{
	private enum TokenType
	{
		End,
		OpenSqBracket,
		CloseSqBracket,
		Comma,
		Plus,
		Asterisk,
		Ampersand,
		Other
	}

	private abstract class TypeName
	{
		public abstract Type ResolveType(ref TypeNameParser parser, string containingAssemblyIfAny);
	}

	private sealed class AssemblyQualifiedTypeName : TypeName
	{
		private readonly string _assemblyName;

		private readonly TypeName _nonQualifiedTypeName;

		public AssemblyQualifiedTypeName(TypeName nonQualifiedTypeName, string assemblyName)
		{
			_nonQualifiedTypeName = nonQualifiedTypeName;
			_assemblyName = assemblyName;
		}

		public override Type ResolveType(ref TypeNameParser parser, string containingAssemblyIfAny)
		{
			return _nonQualifiedTypeName.ResolveType(ref parser, _assemblyName);
		}
	}

	private sealed class NamespaceTypeName : TypeName
	{
		private readonly string _fullName;

		public NamespaceTypeName(string fullName)
		{
			_fullName = fullName;
		}

		public override Type ResolveType(ref TypeNameParser parser, string containingAssemblyIfAny)
		{
			return parser.GetType(_fullName, default(ReadOnlySpan<string>), containingAssemblyIfAny);
		}
	}

	private sealed class NestedNamespaceTypeName : TypeName
	{
		private readonly string _fullName;

		private readonly string[] _nestedNames;

		private readonly int _nestedNamesCount;

		public NestedNamespaceTypeName(string fullName, string[] nestedNames, int nestedNamesCount)
		{
			_fullName = fullName;
			_nestedNames = nestedNames;
			_nestedNamesCount = nestedNamesCount;
		}

		public override Type ResolveType(ref TypeNameParser parser, string containingAssemblyIfAny)
		{
			return parser.GetType(_fullName, _nestedNames.AsSpan(0, _nestedNamesCount), containingAssemblyIfAny);
		}
	}

	private sealed class ModifierTypeName : TypeName
	{
		private readonly TypeName _elementTypeName;

		private readonly int _rankOrModifier;

		public ModifierTypeName(TypeName elementTypeName, int rankOrModifier)
		{
			_elementTypeName = elementTypeName;
			_rankOrModifier = rankOrModifier;
		}

		[UnconditionalSuppressMessage("AotAnalysis", "IL3050:AotUnfriendlyApi", Justification = "Used to implement resolving types from strings.")]
		public override Type ResolveType(ref TypeNameParser parser, string containingAssemblyIfAny)
		{
			Type type = _elementTypeName.ResolveType(ref parser, containingAssemblyIfAny);
			if ((object)type == null)
			{
				return null;
			}
			return _rankOrModifier switch
			{
				-1 => type.MakeArrayType(), 
				-2 => type.MakePointerType(), 
				-3 => type.MakeByRefType(), 
				_ => type.MakeArrayType(_rankOrModifier), 
			};
		}
	}

	private sealed class GenericTypeName : TypeName
	{
		private readonly TypeName _typeDefinition;

		private readonly TypeName[] _typeArguments;

		private readonly int _typeArgumentsCount;

		public GenericTypeName(TypeName genericTypeDefinition, TypeName[] typeArguments, int typeArgumentsCount)
		{
			_typeDefinition = genericTypeDefinition;
			_typeArguments = typeArguments;
			_typeArgumentsCount = typeArgumentsCount;
		}

		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern", Justification = "Used to implement resolving types from strings.")]
		[UnconditionalSuppressMessage("AotAnalysis", "IL3050:AotUnfriendlyApi", Justification = "Used to implement resolving types from strings.")]
		public override Type ResolveType(ref TypeNameParser parser, string containingAssemblyIfAny)
		{
			Type type = _typeDefinition.ResolveType(ref parser, containingAssemblyIfAny);
			if ((object)type == null)
			{
				return null;
			}
			Type[] array = new Type[_typeArgumentsCount];
			for (int i = 0; i < array.Length; i++)
			{
				Type type2 = _typeArguments[i].ResolveType(ref parser, null);
				if ((object)type2 == null)
				{
					return null;
				}
				array[i] = type2;
			}
			return type.MakeGenericType(array);
		}
	}

	private Func<AssemblyName, Assembly> _assemblyResolver;

	private Func<Assembly, string, bool, Type> _typeResolver;

	private bool _throwOnError;

	private bool _ignoreCase;

	private bool _extensibleParser;

	private bool _requireAssemblyQualifiedName;

	private bool _suppressContextualReflectionContext;

	private Assembly _requestingAssembly;

	private Assembly _topLevelAssembly;

	private ReadOnlySpan<char> _input;

	private int _index;

	private int _errorIndex;

	private TokenType Peek
	{
		get
		{
			SkipWhiteSpace();
			char c = ((_index < _input.Length) ? _input[_index] : '\0');
			return CharToToken(c);
		}
	}

	private TokenType PeekSecond
	{
		get
		{
			SkipWhiteSpace();
			int i;
			for (i = _index + 1; i < _input.Length && char.IsWhiteSpace(_input[i]); i++)
			{
			}
			char c = ((i < _input.Length) ? _input[i] : '\0');
			return CharToToken(c);
		}
	}

	private static ReadOnlySpan<char> CharsToEscape => "\\[]+*&,";

	[RequiresUnreferencedCode("The type might be removed")]
	internal static Type GetType(string typeName, Assembly requestingAssembly, bool throwOnError = false, bool ignoreCase = false)
	{
		return GetType(typeName, null, null, requestingAssembly, throwOnError, ignoreCase, extensibleParser: false);
	}

	[RequiresUnreferencedCode("The type might be removed")]
	internal static Type GetType(string typeName, Func<AssemblyName, Assembly> assemblyResolver, Func<Assembly, string, bool, Type> typeResolver, Assembly requestingAssembly, bool throwOnError = false, bool ignoreCase = false, bool extensibleParser = true)
	{
		ArgumentNullException.ThrowIfNull(typeName, "typeName");
		if (typeName.Length == 0)
		{
			if (throwOnError)
			{
				throw new TypeLoadException(SR.Arg_TypeLoadNullStr);
			}
			return null;
		}
		TypeNameParser typeNameParser = new TypeNameParser(typeName);
		typeNameParser._assemblyResolver = assemblyResolver;
		typeNameParser._typeResolver = typeResolver;
		typeNameParser._throwOnError = throwOnError;
		typeNameParser._ignoreCase = ignoreCase;
		typeNameParser._extensibleParser = extensibleParser;
		typeNameParser._requestingAssembly = requestingAssembly;
		return typeNameParser.Parse();
	}

	[RequiresUnreferencedCode("The type might be removed")]
	internal static Type GetType(string typeName, bool throwOnError, bool ignoreCase, Assembly topLevelAssembly)
	{
		TypeNameParser typeNameParser = new TypeNameParser(typeName);
		typeNameParser._throwOnError = throwOnError;
		typeNameParser._ignoreCase = ignoreCase;
		typeNameParser._topLevelAssembly = topLevelAssembly;
		typeNameParser._requestingAssembly = topLevelAssembly;
		return typeNameParser.Parse();
	}

	internal static RuntimeType GetTypeReferencedByCustomAttribute(string typeName, RuntimeModule scope)
	{
		ArgumentException.ThrowIfNullOrEmpty(typeName, "typeName");
		RuntimeAssembly runtimeAssembly = scope.GetRuntimeAssembly();
		TypeNameParser typeNameParser = new TypeNameParser(typeName);
		typeNameParser._throwOnError = true;
		typeNameParser._suppressContextualReflectionContext = true;
		typeNameParser._requestingAssembly = runtimeAssembly;
		RuntimeType runtimeType = (RuntimeType)typeNameParser.Parse();
		RuntimeTypeHandle.RegisterCollectibleTypeDependency(runtimeType, runtimeAssembly);
		return runtimeType;
	}

	internal unsafe static RuntimeType GetTypeHelper(char* pTypeName, RuntimeAssembly requestingAssembly, bool throwOnError, bool requireAssemblyQualifiedName)
	{
		ReadOnlySpan<char> name = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(pTypeName);
		if (name.Length == 0)
		{
			if (throwOnError)
			{
				throw new TypeLoadException(SR.Arg_TypeLoadNullStr);
			}
			return null;
		}
		TypeNameParser typeNameParser = new TypeNameParser(name);
		typeNameParser._requestingAssembly = requestingAssembly;
		typeNameParser._throwOnError = throwOnError;
		typeNameParser._suppressContextualReflectionContext = true;
		typeNameParser._requireAssemblyQualifiedName = requireAssemblyQualifiedName;
		RuntimeType runtimeType = (RuntimeType)typeNameParser.Parse();
		if (runtimeType != null)
		{
			RuntimeTypeHandle.RegisterCollectibleTypeDependency(runtimeType, requestingAssembly);
		}
		return runtimeType;
	}

	private bool CheckTopLevelAssemblyQualifiedName()
	{
		if ((object)_topLevelAssembly != null)
		{
			if (_throwOnError)
			{
				throw new ArgumentException(SR.Argument_AssemblyGetTypeCannotSpecifyAssembly);
			}
			return false;
		}
		return true;
	}

	private Assembly ResolveAssembly(string assemblyName)
	{
		Assembly assembly;
		if (_assemblyResolver != null)
		{
			assembly = _assemblyResolver(new AssemblyName(assemblyName));
			if ((object)assembly == null && _throwOnError)
			{
				throw new FileNotFoundException(SR.Format(SR.FileNotFound_ResolveAssembly, assemblyName));
			}
		}
		else
		{
			assembly = RuntimeAssembly.InternalLoad(new AssemblyName(assemblyName), ref Unsafe.NullRef<StackCrawlMark>(), _suppressContextualReflectionContext ? null : AssemblyLoadContext.CurrentContextualReflectionContext, (RuntimeAssembly)_requestingAssembly, _throwOnError);
		}
		return assembly;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "TypeNameParser.GetType is marked as RequiresUnreferencedCode.")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "TypeNameParser.GetType is marked as RequiresUnreferencedCode.")]
	private Type GetType(string typeName, ReadOnlySpan<string> nestedTypeNames, string assemblyNameIfAny)
	{
		Assembly assembly;
		if (assemblyNameIfAny != null)
		{
			assembly = ResolveAssembly(assemblyNameIfAny);
			if ((object)assembly == null)
			{
				return null;
			}
		}
		else
		{
			assembly = _topLevelAssembly;
		}
		Type type;
		if (_typeResolver != null)
		{
			string text = EscapeTypeName(typeName);
			type = _typeResolver(assembly, text, _ignoreCase);
			if ((object)type == null)
			{
				if (_throwOnError)
				{
					throw new TypeLoadException(((object)assembly == null) ? SR.Format(SR.TypeLoad_ResolveType, text) : SR.Format(SR.TypeLoad_ResolveTypeFromAssembly, text, assembly.FullName));
				}
				return null;
			}
		}
		else
		{
			if ((object)assembly == null)
			{
				if (_requireAssemblyQualifiedName)
				{
					if (_throwOnError)
					{
						throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveType, EscapeTypeName(typeName)));
					}
					return null;
				}
				return GetTypeFromDefaultAssemblies(typeName, nestedTypeNames);
			}
			if (assembly is RuntimeAssembly runtimeAssembly)
			{
				if (!_extensibleParser || !_ignoreCase)
				{
					return runtimeAssembly.GetTypeCore(typeName, nestedTypeNames, _throwOnError, _ignoreCase);
				}
				type = runtimeAssembly.GetTypeCore(typeName, default(ReadOnlySpan<string>), _throwOnError, _ignoreCase);
			}
			else
			{
				type = assembly.GetType(EscapeTypeName(typeName), _throwOnError, _ignoreCase);
			}
			if ((object)type == null)
			{
				return null;
			}
		}
		for (int i = 0; i < nestedTypeNames.Length; i++)
		{
			BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
			if (_ignoreCase)
			{
				bindingFlags |= BindingFlags.IgnoreCase;
			}
			type = type.GetNestedType(nestedTypeNames[i], bindingFlags);
			if ((object)type == null)
			{
				if (_throwOnError)
				{
					throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveNestedType, nestedTypeNames[i], (i > 0) ? nestedTypeNames[i - 1] : typeName));
				}
				return null;
			}
		}
		return type;
	}

	private Type GetTypeFromDefaultAssemblies(string typeName, ReadOnlySpan<string> nestedTypeNames)
	{
		RuntimeAssembly runtimeAssembly = (RuntimeAssembly)_requestingAssembly;
		if ((object)runtimeAssembly != null)
		{
			Type typeCore = runtimeAssembly.GetTypeCore(typeName, nestedTypeNames, throwOnError: false, _ignoreCase);
			if ((object)typeCore != null)
			{
				return typeCore;
			}
		}
		RuntimeAssembly runtimeAssembly2 = (RuntimeAssembly)typeof(object).Assembly;
		if (runtimeAssembly != runtimeAssembly2)
		{
			Type typeCore2 = runtimeAssembly2.GetTypeCore(typeName, nestedTypeNames, throwOnError: false, _ignoreCase);
			if ((object)typeCore2 != null)
			{
				return typeCore2;
			}
		}
		RuntimeAssembly runtimeAssembly3 = AssemblyLoadContext.OnTypeResolve(runtimeAssembly, EscapeTypeName(typeName, nestedTypeNames));
		if ((object)runtimeAssembly3 != null)
		{
			Type typeCore3 = runtimeAssembly3.GetTypeCore(typeName, nestedTypeNames, throwOnError: false, _ignoreCase);
			if ((object)typeCore3 != null)
			{
				return typeCore3;
			}
		}
		if (_throwOnError)
		{
			throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveTypeFromAssembly, EscapeTypeName(typeName), (runtimeAssembly ?? runtimeAssembly2).FullName));
		}
		return null;
	}

	private TypeNameParser(ReadOnlySpan<char> name)
	{
		_assemblyResolver = null;
		_typeResolver = null;
		_throwOnError = false;
		_ignoreCase = false;
		_extensibleParser = false;
		_requireAssemblyQualifiedName = false;
		_suppressContextualReflectionContext = false;
		_requestingAssembly = null;
		_topLevelAssembly = null;
		_input = name;
		_errorIndex = (_index = 0);
	}

	private Type Parse()
	{
		TypeName typeName = ParseNonQualifiedTypeName();
		if (typeName == null)
		{
			return null;
		}
		string text = null;
		switch (GetNextToken())
		{
		default:
			ParseError();
			return null;
		case TokenType.Comma:
			if (!CheckTopLevelAssemblyQualifiedName())
			{
				return null;
			}
			text = GetNextAssemblyName();
			if (text == null)
			{
				return null;
			}
			break;
		case TokenType.End:
			break;
		}
		return typeName.ResolveType(ref this, text);
	}

	private TypeName ParseNonQualifiedTypeName()
	{
		TypeName typeName = ParseNamedOrConstructedGenericTypeName();
		if (typeName == null)
		{
			return null;
		}
		while (true)
		{
			switch (Peek)
			{
			case TokenType.Asterisk:
				Skip();
				typeName = new ModifierTypeName(typeName, -2);
				break;
			case TokenType.Ampersand:
				Skip();
				typeName = new ModifierTypeName(typeName, -3);
				break;
			case TokenType.OpenSqBracket:
			{
				Skip();
				TokenType nextToken = GetNextToken();
				if (nextToken == TokenType.Asterisk)
				{
					typeName = new ModifierTypeName(typeName, 1);
					nextToken = GetNextToken();
				}
				else
				{
					int num = 1;
					while (nextToken == TokenType.Comma)
					{
						nextToken = GetNextToken();
						num++;
					}
					typeName = ((num != 1) ? new ModifierTypeName(typeName, num) : new ModifierTypeName(typeName, -1));
				}
				if (nextToken != TokenType.CloseSqBracket)
				{
					ParseError();
					return null;
				}
				break;
			}
			default:
				return typeName;
			}
		}
	}

	private TypeName ParseNamedOrConstructedGenericTypeName()
	{
		TypeName typeName = ParseNamedTypeName();
		if (typeName == null)
		{
			return null;
		}
		bool flag = Peek == TokenType.OpenSqBracket;
		bool flag2 = flag;
		if (flag2)
		{
			TokenType peekSecond = PeekSecond;
			bool flag3 = ((peekSecond == TokenType.OpenSqBracket || peekSecond == TokenType.Other) ? true : false);
			flag2 = flag3;
		}
		if (!flag2)
		{
			return typeName;
		}
		Skip();
		TypeName[] array = new TypeName[2];
		int num = 0;
		while (true)
		{
			TypeName typeName2 = ParseGenericTypeArgument();
			if (typeName2 == null)
			{
				break;
			}
			if (num >= array.Length)
			{
				Array.Resize(ref array, 2 * num);
			}
			array[num++] = typeName2;
			switch (GetNextToken())
			{
			case TokenType.Comma:
				continue;
			case TokenType.CloseSqBracket:
				return new GenericTypeName(typeName, array, num);
			}
			ParseError();
			return null;
		}
		return null;
	}

	private TypeName ParseNamedTypeName()
	{
		string nextIdentifier = GetNextIdentifier();
		if (nextIdentifier == null)
		{
			return null;
		}
		nextIdentifier = ApplyLeadingDotCompatQuirk(nextIdentifier);
		if (Peek == TokenType.Plus)
		{
			string[] array = new string[1];
			int num = 0;
			do
			{
				Skip();
				string nextIdentifier2 = GetNextIdentifier();
				if (nextIdentifier2 == null)
				{
					return null;
				}
				nextIdentifier2 = ApplyLeadingDotCompatQuirk(nextIdentifier2);
				if (num >= array.Length)
				{
					Array.Resize(ref array, 2 * num);
				}
				array[num++] = nextIdentifier2;
			}
			while (Peek == TokenType.Plus);
			return new NestedNamespaceTypeName(nextIdentifier, array, num);
		}
		return new NamespaceTypeName(nextIdentifier);
		static string ApplyLeadingDotCompatQuirk(string typeName)
		{
			if (!typeName.StartsWith('.') || typeName.AsSpan(1).Contains('.'))
			{
				return typeName;
			}
			return typeName.Substring(1);
		}
	}

	private TypeName ParseGenericTypeArgument()
	{
		switch (GetNextToken())
		{
		case TokenType.Other:
			return ParseNonQualifiedTypeName();
		default:
			ParseError();
			return null;
		case TokenType.OpenSqBracket:
		{
			string text = null;
			TypeName typeName = ParseNonQualifiedTypeName();
			if (typeName == null)
			{
				return null;
			}
			TokenType nextToken = GetNextToken();
			if (nextToken == TokenType.Comma)
			{
				text = GetNextEmbeddedAssemblyName();
				nextToken = GetNextToken();
			}
			if (nextToken != TokenType.CloseSqBracket)
			{
				ParseError();
				return null;
			}
			if (text == null)
			{
				return typeName;
			}
			return new AssemblyQualifiedTypeName(typeName, text);
		}
		}
	}

	private void Skip()
	{
		SkipWhiteSpace();
		if (_index < _input.Length)
		{
			_index++;
		}
	}

	private TokenType GetNextToken()
	{
		_errorIndex = _index;
		TokenType peek = Peek;
		if (peek == TokenType.End || peek == TokenType.Other)
		{
			return peek;
		}
		Skip();
		return peek;
	}

	private string GetNextIdentifier()
	{
		SkipWhiteSpace();
		Span<char> initialBuffer = stackalloc char[64];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		int num = _index;
		while (num < _input.Length)
		{
			char c = _input[num];
			TokenType tokenType = CharToToken(c);
			if (tokenType != TokenType.Other)
			{
				break;
			}
			num++;
			if (c == '\\')
			{
				_errorIndex = num - 1;
				c = ((num < _input.Length) ? _input[num++] : '\0');
				if (!NeedsEscapingInTypeName(c))
				{
					ParseError();
					return null;
				}
			}
			valueStringBuilder.Append(c);
		}
		_index = num;
		if (valueStringBuilder.Length == 0)
		{
			_errorIndex = num;
			ParseError();
			return null;
		}
		return valueStringBuilder.ToString();
	}

	private string GetNextAssemblyName()
	{
		if (!StartAssemblyName())
		{
			return null;
		}
		string result = _input.Slice(_index).ToString();
		_index = _input.Length;
		return result;
	}

	private string GetNextEmbeddedAssemblyName()
	{
		if (!StartAssemblyName())
		{
			return null;
		}
		Span<char> initialBuffer = stackalloc char[64];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		int num = _index;
		while (true)
		{
			if (num >= _input.Length)
			{
				ParseError();
				return null;
			}
			char c = _input[num];
			if (c == ']')
			{
				break;
			}
			num++;
			if (c == '\\' && num < _input.Length && _input[num] == ']')
			{
				c = _input[num++];
			}
			valueStringBuilder.Append(c);
		}
		_index = num;
		if (valueStringBuilder.Length == 0)
		{
			_errorIndex = num;
			ParseError();
			return null;
		}
		return valueStringBuilder.ToString();
	}

	private bool StartAssemblyName()
	{
		TokenType peek = Peek;
		if ((peek == TokenType.End || peek == TokenType.Comma) ? true : false)
		{
			ParseError();
			return false;
		}
		return true;
	}

	private static TokenType CharToToken(char c)
	{
		return c switch
		{
			'\0' => TokenType.End, 
			'[' => TokenType.OpenSqBracket, 
			']' => TokenType.CloseSqBracket, 
			',' => TokenType.Comma, 
			'+' => TokenType.Plus, 
			'*' => TokenType.Asterisk, 
			'&' => TokenType.Ampersand, 
			_ => TokenType.Other, 
		};
	}

	private void SkipWhiteSpace()
	{
		while (_index < _input.Length && char.IsWhiteSpace(_input[_index]))
		{
			_index++;
		}
	}

	private static bool NeedsEscapingInTypeName(char c)
	{
		return CharsToEscape.Contains(c);
	}

	private static string EscapeTypeName(string name)
	{
		if (name.AsSpan().IndexOfAny(CharsToEscape) < 0)
		{
			return name;
		}
		Span<char> initialBuffer = stackalloc char[64];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		foreach (char c in name)
		{
			if (NeedsEscapingInTypeName(c))
			{
				valueStringBuilder.Append('\\');
			}
			valueStringBuilder.Append(c);
		}
		return valueStringBuilder.ToString();
	}

	private static string EscapeTypeName(string typeName, ReadOnlySpan<string> nestedTypeNames)
	{
		string text = EscapeTypeName(typeName);
		if (nestedTypeNames.Length > 0)
		{
			StringBuilder stringBuilder = new StringBuilder(text);
			for (int i = 0; i < nestedTypeNames.Length; i++)
			{
				stringBuilder.Append('+');
				stringBuilder.Append(EscapeTypeName(nestedTypeNames[i]));
			}
			text = stringBuilder.ToString();
		}
		return text;
	}

	private void ParseError()
	{
		if (_throwOnError)
		{
			throw new ArgumentException(SR.Arg_ArgumentException, $"typeName@{_errorIndex}");
		}
	}
}
