using System.ComponentModel;
using System.Configuration.Assemblies;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace System.Reflection;

public sealed class AssemblyName : ICloneable, IDeserializationCallback, ISerializable
{
	private string _name;

	private byte[] _publicKey;

	private byte[] _publicKeyToken;

	private CultureInfo _cultureInfo;

	private string _codeBase;

	private Version _version;

	private AssemblyHashAlgorithm _hashAlgorithm;

	private AssemblyVersionCompatibility _versionCompatibility;

	private AssemblyNameFlags _flags;

	private static Func<string, AssemblyName> s_getAssemblyName;

	internal byte[]? RawPublicKey => _publicKey;

	internal byte[]? RawPublicKeyToken => _publicKeyToken;

	internal AssemblyNameFlags RawFlags
	{
		get
		{
			return _flags;
		}
		set
		{
			_flags = value;
		}
	}

	public string? Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
		}
	}

	public Version? Version
	{
		get
		{
			return _version;
		}
		set
		{
			_version = value;
		}
	}

	public CultureInfo? CultureInfo
	{
		get
		{
			return _cultureInfo;
		}
		set
		{
			_cultureInfo = value;
		}
	}

	public string? CultureName
	{
		get
		{
			return _cultureInfo?.Name;
		}
		set
		{
			_cultureInfo = ((value == null) ? null : new CultureInfo(value));
		}
	}

	[Obsolete("AssemblyName.CodeBase and AssemblyName.EscapedCodeBase are obsolete. Using them for loading an assembly is not supported.", DiagnosticId = "SYSLIB0044", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public string? CodeBase
	{
		[RequiresAssemblyFiles("The code will return an empty string for assemblies embedded in a single-file app")]
		get
		{
			return _codeBase;
		}
		set
		{
			_codeBase = value;
		}
	}

	[Obsolete("AssemblyName.CodeBase and AssemblyName.EscapedCodeBase are obsolete. Using them for loading an assembly is not supported.", DiagnosticId = "SYSLIB0044", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresAssemblyFiles("The code will return an empty string for assemblies embedded in a single-file app")]
	public string? EscapedCodeBase
	{
		get
		{
			if (_codeBase == null)
			{
				return null;
			}
			return EscapeCodeBase(_codeBase);
		}
	}

	[Obsolete("AssemblyName members HashAlgorithm, ProcessorArchitecture, and VersionCompatibility are obsolete and not supported.", DiagnosticId = "SYSLIB0037", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public ProcessorArchitecture ProcessorArchitecture
	{
		get
		{
			int num = (int)(_flags & (AssemblyNameFlags)112) >> 4;
			if (num > 5)
			{
				num = 0;
			}
			return (ProcessorArchitecture)num;
		}
		set
		{
			int num = (int)(value & (ProcessorArchitecture)7);
			if (num <= 5)
			{
				_flags = (AssemblyNameFlags)((long)_flags & 0xFFFFFF0FL);
				_flags |= (AssemblyNameFlags)(num << 4);
			}
		}
	}

	public AssemblyContentType ContentType
	{
		get
		{
			int num = (int)(_flags & (AssemblyNameFlags)3584) >> 9;
			if (num > 1)
			{
				num = 0;
			}
			return (AssemblyContentType)num;
		}
		set
		{
			int num = (int)(value & (AssemblyContentType)7);
			if (num <= 1)
			{
				_flags = (AssemblyNameFlags)((long)_flags & 0xFFFFF1FFL);
				_flags |= (AssemblyNameFlags)(num << 9);
			}
		}
	}

	public AssemblyNameFlags Flags
	{
		get
		{
			return _flags & (AssemblyNameFlags)(-3825);
		}
		set
		{
			_flags &= (AssemblyNameFlags)3824;
			_flags |= value & (AssemblyNameFlags)(-3825);
		}
	}

	[Obsolete("AssemblyName members HashAlgorithm, ProcessorArchitecture, and VersionCompatibility are obsolete and not supported.", DiagnosticId = "SYSLIB0037", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public AssemblyHashAlgorithm HashAlgorithm
	{
		get
		{
			return _hashAlgorithm;
		}
		set
		{
			_hashAlgorithm = value;
		}
	}

	[Obsolete("AssemblyName members HashAlgorithm, ProcessorArchitecture, and VersionCompatibility are obsolete and not supported.", DiagnosticId = "SYSLIB0037", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public AssemblyVersionCompatibility VersionCompatibility
	{
		get
		{
			return _versionCompatibility;
		}
		set
		{
			_versionCompatibility = value;
		}
	}

	[Obsolete("Strong name signing is not supported and throws PlatformNotSupportedException.", DiagnosticId = "SYSLIB0017", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public StrongNameKeyPair? KeyPair
	{
		get
		{
			throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);
		}
		set
		{
			throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);
		}
	}

	public string FullName
	{
		get
		{
			if (string.IsNullOrEmpty(Name))
			{
				return string.Empty;
			}
			byte[] pkt = _publicKeyToken ?? AssemblyNameHelpers.ComputePublicKeyToken(_publicKey);
			return AssemblyNameFormatter.ComputeDisplayName(Name, Version, CultureName, pkt, Flags, ContentType);
		}
	}

	internal unsafe AssemblyName(NativeAssemblyNameParts* pParts)
		: this()
	{
		if (pParts->_pName != null)
		{
			_name = new string(pParts->_pName);
		}
		if (pParts->_pCultureName != null)
		{
			_cultureInfo = new CultureInfo(new string(pParts->_pCultureName));
		}
		if (pParts->_pPublicKeyOrToken != null)
		{
			byte[] array = new ReadOnlySpan<byte>(pParts->_pPublicKeyOrToken, pParts->_cbPublicKeyOrToken).ToArray();
			if ((pParts->_flags & AssemblyNameFlags.PublicKey) != 0)
			{
				_publicKey = array;
			}
			else
			{
				_publicKeyToken = array;
			}
		}
		_version = pParts->GetVersion();
		_flags = pParts->_flags;
	}

	internal void SetProcArchIndex(PortableExecutableKinds pek, ImageFileMachine ifm)
	{
		ProcessorArchitecture = CalculateProcArch(pek, ifm, _flags);
	}

	private static ProcessorArchitecture CalculateProcArch(PortableExecutableKinds pek, ImageFileMachine ifm, AssemblyNameFlags aFlags)
	{
		if ((aFlags & (AssemblyNameFlags)240) == (AssemblyNameFlags)112)
		{
			return ProcessorArchitecture.None;
		}
		switch (ifm)
		{
		case ImageFileMachine.IA64:
			return ProcessorArchitecture.IA64;
		case ImageFileMachine.ARM:
			return ProcessorArchitecture.Arm;
		case ImageFileMachine.AMD64:
			return ProcessorArchitecture.Amd64;
		case ImageFileMachine.I386:
			if ((pek & PortableExecutableKinds.ILOnly) != 0 && (pek & PortableExecutableKinds.Required32Bit) == 0)
			{
				return ProcessorArchitecture.MSIL;
			}
			return ProcessorArchitecture.X86;
		default:
			return ProcessorArchitecture.None;
		}
	}

	private unsafe static void ParseAsAssemblySpec(char* pAssemblyName, void* pAssemblySpec)
	{
		//The blocks IL_003b, IL_0043, IL_0045, IL_005e, IL_0096, IL_0099, IL_00a1, IL_00c2 are reachable both inside and outside the pinned region starting at IL_0036. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		AssemblyNameParser.AssemblyNameParts assemblyNameParts = AssemblyNameParser.Parse(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(pAssemblyName));
		fixed (char* pName = assemblyNameParts._name)
		{
			string cultureName = assemblyNameParts._cultureName;
			char* intPtr;
			byte[] publicKeyOrToken;
			NativeAssemblyNameParts nativeAssemblyNameParts;
			ref NativeAssemblyNameParts reference;
			int cbPublicKeyOrToken;
			if (cultureName == null)
			{
				char* pCultureName;
				intPtr = (pCultureName = null);
				publicKeyOrToken = assemblyNameParts._publicKeyOrToken;
				fixed (byte* ptr = publicKeyOrToken)
				{
					byte* pPublicKeyOrToken = ptr;
					nativeAssemblyNameParts = default(NativeAssemblyNameParts);
					nativeAssemblyNameParts._flags = assemblyNameParts._flags;
					nativeAssemblyNameParts._pName = pName;
					nativeAssemblyNameParts._pCultureName = pCultureName;
					nativeAssemblyNameParts._pPublicKeyOrToken = pPublicKeyOrToken;
					reference = ref nativeAssemblyNameParts;
					cbPublicKeyOrToken = ((assemblyNameParts._publicKeyOrToken != null) ? assemblyNameParts._publicKeyOrToken.Length : 0);
					reference._cbPublicKeyOrToken = cbPublicKeyOrToken;
					nativeAssemblyNameParts.SetVersion(assemblyNameParts._version, ushort.MaxValue);
					InitializeAssemblySpec(&nativeAssemblyNameParts, pAssemblySpec);
				}
				return;
			}
			fixed (char* ptr2 = &cultureName.GetPinnableReference())
			{
				char* pCultureName;
				intPtr = (pCultureName = ptr2);
				publicKeyOrToken = assemblyNameParts._publicKeyOrToken;
				fixed (byte* ptr = publicKeyOrToken)
				{
					byte* pPublicKeyOrToken = ptr;
					nativeAssemblyNameParts = default(NativeAssemblyNameParts);
					nativeAssemblyNameParts._flags = assemblyNameParts._flags;
					nativeAssemblyNameParts._pName = pName;
					nativeAssemblyNameParts._pCultureName = pCultureName;
					nativeAssemblyNameParts._pPublicKeyOrToken = pPublicKeyOrToken;
					reference = ref nativeAssemblyNameParts;
					cbPublicKeyOrToken = (reference._cbPublicKeyOrToken = ((assemblyNameParts._publicKeyOrToken != null) ? assemblyNameParts._publicKeyOrToken.Length : 0));
					nativeAssemblyNameParts.SetVersion(assemblyNameParts._version, ushort.MaxValue);
					InitializeAssemblySpec(&nativeAssemblyNameParts, pAssemblySpec);
				}
			}
		}
	}

	[DllImport("QCall", EntryPoint = "AssemblyName_InitializeAssemblySpec", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "AssemblyName_InitializeAssemblySpec")]
	private unsafe static extern void InitializeAssemblySpec(NativeAssemblyNameParts* pAssemblyNameParts, void* pAssemblySpec);

	public AssemblyName(string assemblyName)
		: this()
	{
		ArgumentException.ThrowIfNullOrEmpty(assemblyName, "assemblyName");
		if (assemblyName[0] == '\0')
		{
			throw new ArgumentException(SR.Format_StringZeroLength);
		}
		AssemblyNameParser.AssemblyNameParts assemblyNameParts = AssemblyNameParser.Parse(assemblyName);
		_name = assemblyNameParts._name;
		_version = assemblyNameParts._version;
		_flags = assemblyNameParts._flags;
		if ((assemblyNameParts._flags & AssemblyNameFlags.PublicKey) != 0)
		{
			_publicKey = assemblyNameParts._publicKeyOrToken;
		}
		else
		{
			_publicKeyToken = assemblyNameParts._publicKeyOrToken;
		}
		if (assemblyNameParts._cultureName != null)
		{
			_cultureInfo = new CultureInfo(assemblyNameParts._cultureName);
		}
	}

	public AssemblyName()
	{
		_versionCompatibility = AssemblyVersionCompatibility.SameMachine;
	}

	public object Clone()
	{
		return new AssemblyName
		{
			_name = _name,
			_publicKey = (byte[])_publicKey?.Clone(),
			_publicKeyToken = (byte[])_publicKeyToken?.Clone(),
			_cultureInfo = _cultureInfo,
			_version = _version,
			_flags = _flags,
			_codeBase = _codeBase,
			_hashAlgorithm = _hashAlgorithm,
			_versionCompatibility = _versionCompatibility
		};
	}

	private static Func<string, AssemblyName> InitGetAssemblyName()
	{
		Type type = Type.GetType("System.Reflection.Metadata.MetadataReader, System.Reflection.Metadata", throwOnError: true);
		MethodInfo method = type.GetMethod("GetAssemblyName", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(string) }, null);
		if (method == null)
		{
			throw new MissingMethodException(type.FullName, "GetAssemblyName");
		}
		return s_getAssemblyName = method.CreateDelegate<Func<string, AssemblyName>>();
	}

	public static AssemblyName GetAssemblyName(string assemblyFile)
	{
		return (s_getAssemblyName ?? InitGetAssemblyName())(assemblyFile);
	}

	public byte[]? GetPublicKey()
	{
		return _publicKey;
	}

	public void SetPublicKey(byte[]? publicKey)
	{
		_publicKey = publicKey;
		if (publicKey == null)
		{
			_flags &= ~AssemblyNameFlags.PublicKey;
		}
		else
		{
			_flags |= AssemblyNameFlags.PublicKey;
		}
	}

	public byte[]? GetPublicKeyToken()
	{
		return _publicKeyToken ?? (_publicKeyToken = AssemblyNameHelpers.ComputePublicKeyToken(_publicKey));
	}

	public void SetPublicKeyToken(byte[]? publicKeyToken)
	{
		_publicKeyToken = publicKeyToken;
	}

	public override string ToString()
	{
		string fullName = FullName;
		if (fullName == null)
		{
			return base.ToString();
		}
		return fullName;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	public void OnDeserialization(object? sender)
	{
		throw new PlatformNotSupportedException();
	}

	public static bool ReferenceMatchesDefinition(AssemblyName? reference, AssemblyName? definition)
	{
		if (reference == definition)
		{
			return true;
		}
		ArgumentNullException.ThrowIfNull(reference, "reference");
		ArgumentNullException.ThrowIfNull(definition, "definition");
		string text = reference.Name ?? string.Empty;
		string value = definition.Name ?? string.Empty;
		return text.Equals(value, StringComparison.OrdinalIgnoreCase);
	}

	[RequiresAssemblyFiles("The code will return an empty string for assemblies embedded in a single-file app")]
	internal static string EscapeCodeBase(string codebase)
	{
		if (codebase == null)
		{
			return string.Empty;
		}
		int destPos = 0;
		char[] array = EscapeString(codebase, 0, codebase.Length, null, ref destPos, isUriString: true, '\uffff', '\uffff', '\uffff');
		if (array == null)
		{
			return codebase;
		}
		return new string(array, 0, destPos);
	}

	internal unsafe static char[] EscapeString(string input, int start, int end, char[] dest, ref int destPos, bool isUriString, char force1, char force2, char rsvd)
	{
		int i = start;
		int num = start;
		byte* ptr = stackalloc byte[160];
		fixed (char* ptr2 = input)
		{
			for (; i < end; i++)
			{
				char c = ptr2[i];
				if (c > '\u007f')
				{
					short num2 = (short)Math.Min(end - i, 39);
					short num3 = 1;
					while (num3 < num2 && ptr2[i + num3] > '\u007f')
					{
						num3++;
					}
					if (ptr2[i + num3 - 1] >= '\ud800' && ptr2[i + num3 - 1] <= '\udbff')
					{
						if (num3 == 1 || num3 == end - i)
						{
							throw new FormatException(SR.Arg_FormatException);
						}
						num3++;
					}
					dest = EnsureDestinationSize(ptr2, dest, i, (short)(num3 * 4 * 3), 480, ref destPos, num);
					short num4 = (short)Encoding.UTF8.GetBytes(ptr2 + i, num3, ptr, 160);
					if (num4 == 0)
					{
						throw new FormatException(SR.Arg_FormatException);
					}
					i += num3 - 1;
					for (num3 = 0; num3 < num4; num3++)
					{
						EscapeAsciiChar((char)ptr[num3], dest, ref destPos);
					}
					num = i + 1;
				}
				else if (c == '%' && rsvd == '%')
				{
					dest = EnsureDestinationSize(ptr2, dest, i, 3, 120, ref destPos, num);
					if (i + 2 < end && char.IsAsciiHexDigit(ptr2[i + 1]) && char.IsAsciiHexDigit(ptr2[i + 2]))
					{
						dest[destPos++] = '%';
						dest[destPos++] = ptr2[i + 1];
						dest[destPos++] = ptr2[i + 2];
						i += 2;
					}
					else
					{
						EscapeAsciiChar('%', dest, ref destPos);
					}
					num = i + 1;
				}
				else if (c == force1 || c == force2 || (c != rsvd && (isUriString ? (!IsReservedUnreservedOrHash(c)) : (!IsUnreserved(c)))))
				{
					dest = EnsureDestinationSize(ptr2, dest, i, 3, 120, ref destPos, num);
					EscapeAsciiChar(c, dest, ref destPos);
					num = i + 1;
				}
			}
			if (num != i && (num != start || dest != null))
			{
				dest = EnsureDestinationSize(ptr2, dest, i, 0, 0, ref destPos, num);
			}
		}
		return dest;
	}

	private unsafe static char[] EnsureDestinationSize(char* pStr, char[] dest, int currentInputPos, short charsToAdd, short minReallocateChars, ref int destPos, int prevInputPos)
	{
		if (dest == null || dest.Length < destPos + (currentInputPos - prevInputPos) + charsToAdd)
		{
			char[] array = new char[destPos + (currentInputPos - prevInputPos) + minReallocateChars];
			if (dest != null && destPos != 0)
			{
				Buffer.BlockCopy(dest, 0, array, 0, destPos << 1);
			}
			dest = array;
		}
		while (prevInputPos != currentInputPos)
		{
			dest[destPos++] = pStr[prevInputPos++];
		}
		return dest;
	}

	internal static void EscapeAsciiChar(char ch, char[] to, ref int pos)
	{
		to[pos++] = '%';
		to[pos++] = HexConverter.ToCharUpper((int)ch >> 4);
		to[pos++] = HexConverter.ToCharUpper(ch);
	}

	private static bool IsReservedUnreservedOrHash(char c)
	{
		if (IsUnreserved(c))
		{
			return true;
		}
		return ":/?#[]@!$&'()*+,;=".Contains(c);
	}

	internal static bool IsUnreserved(char c)
	{
		if (char.IsAsciiLetterOrDigit(c))
		{
			return true;
		}
		return "-._~".Contains(c);
	}
}
