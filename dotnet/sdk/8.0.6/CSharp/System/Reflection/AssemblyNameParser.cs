using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Reflection;

internal ref struct AssemblyNameParser
{
	public readonly struct AssemblyNameParts
	{
		public readonly string _name;

		public readonly Version _version;

		public readonly string _cultureName;

		public readonly AssemblyNameFlags _flags;

		public readonly byte[] _publicKeyOrToken;

		public AssemblyNameParts(string name, Version version, string cultureName, AssemblyNameFlags flags, byte[] publicKeyOrToken)
		{
			_name = name;
			_version = version;
			_cultureName = cultureName;
			_flags = flags;
			_publicKeyOrToken = publicKeyOrToken;
		}
	}

	private enum Token
	{
		Equals = 1,
		Comma,
		String,
		End
	}

	private enum AttributeKind
	{
		Version = 1,
		Culture = 2,
		PublicKeyOrToken = 4,
		ProcessorArchitecture = 8,
		Retargetable = 0x10,
		ContentType = 0x20
	}

	private readonly ReadOnlySpan<char> _input;

	private int _index;

	private AssemblyNameParser(ReadOnlySpan<char> input)
	{
		if (input.Length == 0)
		{
			throw new ArgumentException(SR.Format_StringZeroLength);
		}
		_input = input;
		_index = 0;
	}

	public static AssemblyNameParts Parse(string name)
	{
		return new AssemblyNameParser(name).Parse();
	}

	public static AssemblyNameParts Parse(ReadOnlySpan<char> name)
	{
		return new AssemblyNameParser(name).Parse();
	}

	private void RecordNewSeenOrThrow(scoped ref AttributeKind seenAttributes, AttributeKind newAttribute)
	{
		if ((seenAttributes & newAttribute) != 0)
		{
			ThrowInvalidAssemblyName();
		}
		seenAttributes |= newAttribute;
	}

	private AssemblyNameParts Parse()
	{
		Token nextToken = GetNextToken(out var tokenString);
		if (nextToken != Token.String)
		{
			ThrowInvalidAssemblyName();
		}
		if (string.IsNullOrEmpty(tokenString) || tokenString.AsSpan().ContainsAny('/', '\\', ':'))
		{
			ThrowInvalidAssemblyName();
		}
		Version version = null;
		string cultureName = null;
		byte[] publicKeyOrToken = null;
		AssemblyNameFlags assemblyNameFlags = AssemblyNameFlags.None;
		AttributeKind seenAttributes = (AttributeKind)0;
		for (nextToken = GetNextToken(); nextToken != Token.End; nextToken = GetNextToken())
		{
			if (nextToken != Token.Comma)
			{
				ThrowInvalidAssemblyName();
			}
			nextToken = GetNextToken(out var tokenString2);
			if (nextToken != Token.String)
			{
				ThrowInvalidAssemblyName();
			}
			nextToken = GetNextToken();
			if (nextToken != Token.Equals)
			{
				ThrowInvalidAssemblyName();
			}
			nextToken = GetNextToken(out var tokenString3);
			if (nextToken != Token.String)
			{
				ThrowInvalidAssemblyName();
			}
			if (tokenString2 == string.Empty)
			{
				ThrowInvalidAssemblyName();
			}
			if (tokenString2.Equals("Version", StringComparison.OrdinalIgnoreCase))
			{
				RecordNewSeenOrThrow(ref seenAttributes, AttributeKind.Version);
				version = ParseVersion(tokenString3);
			}
			if (tokenString2.Equals("Culture", StringComparison.OrdinalIgnoreCase))
			{
				RecordNewSeenOrThrow(ref seenAttributes, AttributeKind.Culture);
				cultureName = ParseCulture(tokenString3);
			}
			if (tokenString2.Equals("PublicKey", StringComparison.OrdinalIgnoreCase))
			{
				RecordNewSeenOrThrow(ref seenAttributes, AttributeKind.PublicKeyOrToken);
				publicKeyOrToken = ParsePKT(tokenString3, isToken: false);
				assemblyNameFlags |= AssemblyNameFlags.PublicKey;
			}
			if (tokenString2.Equals("PublicKeyToken", StringComparison.OrdinalIgnoreCase))
			{
				RecordNewSeenOrThrow(ref seenAttributes, AttributeKind.PublicKeyOrToken);
				publicKeyOrToken = ParsePKT(tokenString3, isToken: true);
			}
			if (tokenString2.Equals("ProcessorArchitecture", StringComparison.OrdinalIgnoreCase))
			{
				RecordNewSeenOrThrow(ref seenAttributes, AttributeKind.ProcessorArchitecture);
				assemblyNameFlags = (AssemblyNameFlags)((int)assemblyNameFlags | ((int)ParseProcessorArchitecture(tokenString3) << 4));
			}
			if (tokenString2.Equals("Retargetable", StringComparison.OrdinalIgnoreCase))
			{
				RecordNewSeenOrThrow(ref seenAttributes, AttributeKind.Retargetable);
				if (tokenString3.Equals("Yes", StringComparison.OrdinalIgnoreCase))
				{
					assemblyNameFlags |= AssemblyNameFlags.Retargetable;
				}
				else if (!tokenString3.Equals("No", StringComparison.OrdinalIgnoreCase))
				{
					ThrowInvalidAssemblyName();
				}
			}
			if (tokenString2.Equals("ContentType", StringComparison.OrdinalIgnoreCase))
			{
				RecordNewSeenOrThrow(ref seenAttributes, AttributeKind.ContentType);
				if (tokenString3.Equals("WindowsRuntime", StringComparison.OrdinalIgnoreCase))
				{
					assemblyNameFlags |= (AssemblyNameFlags)512;
				}
				else
				{
					ThrowInvalidAssemblyName();
				}
			}
		}
		return new AssemblyNameParts(tokenString, version, cultureName, assemblyNameFlags, publicKeyOrToken);
	}

	private Version ParseVersion(string attributeValue)
	{
		ReadOnlySpan<char> source = attributeValue;
		Span<Range> destination = stackalloc Range[5];
		destination = destination.Slice(0, source.Split(destination, '.'));
		int length = destination.Length;
		if ((length < 2 || length > 4) ? true : false)
		{
			ThrowInvalidAssemblyName();
		}
		Span<ushort> span = stackalloc ushort[4];
		for (int i = 0; i < span.Length; i++)
		{
			if ((uint)i >= (uint)destination.Length)
			{
				span[i] = ushort.MaxValue;
				break;
			}
			Range range = destination[i];
			if (!ushort.TryParse(source[range.Start..range.End], NumberStyles.None, NumberFormatInfo.InvariantInfo, out span[i]))
			{
				ThrowInvalidAssemblyName();
			}
		}
		if (span[0] == ushort.MaxValue || span[1] == ushort.MaxValue)
		{
			ThrowInvalidAssemblyName();
		}
		if (span[2] != ushort.MaxValue)
		{
			if (span[3] != ushort.MaxValue)
			{
				return new Version(span[0], span[1], span[2], span[3]);
			}
			return new Version(span[0], span[1], span[2]);
		}
		return new Version(span[0], span[1]);
	}

	private static string ParseCulture(string attributeValue)
	{
		if (attributeValue.Equals("Neutral", StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		return attributeValue;
	}

	private byte[] ParsePKT(string attributeValue, bool isToken)
	{
		if (attributeValue.Equals("null", StringComparison.OrdinalIgnoreCase) || attributeValue == string.Empty)
		{
			return Array.Empty<byte>();
		}
		if (isToken && attributeValue.Length != 16)
		{
			ThrowInvalidAssemblyName();
		}
		byte[] array = new byte[attributeValue.Length / 2];
		int num = 0;
		for (int i = 0; i < array.Length; i++)
		{
			char c = attributeValue[num++];
			char c2 = attributeValue[num++];
			array[i] = (byte)((ParseHexNybble(c) << 4) | ParseHexNybble(c2));
		}
		return array;
	}

	private ProcessorArchitecture ParseProcessorArchitecture(string attributeValue)
	{
		if (attributeValue.Equals("msil", StringComparison.OrdinalIgnoreCase))
		{
			return ProcessorArchitecture.MSIL;
		}
		if (attributeValue.Equals("x86", StringComparison.OrdinalIgnoreCase))
		{
			return ProcessorArchitecture.X86;
		}
		if (attributeValue.Equals("ia64", StringComparison.OrdinalIgnoreCase))
		{
			return ProcessorArchitecture.IA64;
		}
		if (attributeValue.Equals("amd64", StringComparison.OrdinalIgnoreCase))
		{
			return ProcessorArchitecture.Amd64;
		}
		if (attributeValue.Equals("arm", StringComparison.OrdinalIgnoreCase))
		{
			return ProcessorArchitecture.Arm;
		}
		ThrowInvalidAssemblyName();
		return ProcessorArchitecture.None;
	}

	private byte ParseHexNybble(char c)
	{
		int num = HexConverter.FromChar(c);
		if (num == 255)
		{
			ThrowInvalidAssemblyName();
		}
		return (byte)num;
	}

	private Token GetNextToken()
	{
		string tokenString;
		return GetNextToken(out tokenString);
	}

	private static bool IsWhiteSpace(char ch)
	{
		switch (ch)
		{
		case '\t':
		case '\n':
		case '\r':
		case ' ':
			return true;
		default:
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private char GetNextChar()
	{
		char c;
		if (_index < _input.Length)
		{
			c = _input[_index++];
			if (c == '\0')
			{
				ThrowInvalidAssemblyName();
			}
		}
		else
		{
			c = '\0';
		}
		return c;
	}

	private Token GetNextToken(out string tokenString)
	{
		tokenString = string.Empty;
		char nextChar;
		do
		{
			nextChar = GetNextChar();
			switch (nextChar)
			{
			case ',':
				return Token.Comma;
			case '=':
				return Token.Equals;
			case '\0':
				return Token.End;
			}
		}
		while (IsWhiteSpace(nextChar));
		Span<char> initialBuffer = stackalloc char[64];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		char c = '\0';
		if (nextChar == '\'' || nextChar == '"')
		{
			c = nextChar;
			nextChar = GetNextChar();
		}
		while (true)
		{
			if (nextChar == '\0')
			{
				if (c != 0)
				{
					ThrowInvalidAssemblyName();
				}
				break;
			}
			if (c != 0 && nextChar == c)
			{
				break;
			}
			if (c == '\0' && (nextChar == ',' || nextChar == '='))
			{
				_index--;
				break;
			}
			if (c == '\0' && (nextChar == '\'' || nextChar == '"'))
			{
				ThrowInvalidAssemblyName();
			}
			if (nextChar == '\\')
			{
				nextChar = GetNextChar();
				switch (nextChar)
				{
				case '"':
				case '\'':
				case ',':
				case '=':
				case '\\':
					valueStringBuilder.Append(nextChar);
					break;
				case 't':
					valueStringBuilder.Append('\t');
					break;
				case 'r':
					valueStringBuilder.Append('\r');
					break;
				case 'n':
					valueStringBuilder.Append('\n');
					break;
				default:
					ThrowInvalidAssemblyName();
					break;
				}
			}
			else
			{
				valueStringBuilder.Append(nextChar);
			}
			nextChar = GetNextChar();
		}
		if (c == '\0')
		{
			while (valueStringBuilder.Length > 0 && IsWhiteSpace(valueStringBuilder[valueStringBuilder.Length - 1]))
			{
				valueStringBuilder.Length--;
			}
		}
		tokenString = valueStringBuilder.ToString();
		return Token.String;
	}

	[DoesNotReturn]
	private void ThrowInvalidAssemblyName()
	{
		throw new FileLoadException(SR.InvalidAssemblyName, _input.ToString());
	}
}
