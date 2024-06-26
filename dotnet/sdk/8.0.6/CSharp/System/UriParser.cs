using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System;

public abstract class UriParser
{
	private sealed class BuiltInUriParser : UriParser
	{
		internal BuiltInUriParser(string lwrCaseScheme, int defaultPort, System.UriSyntaxFlags syntaxFlags)
			: base(syntaxFlags | System.UriSyntaxFlags.SimpleUserSyntax | System.UriSyntaxFlags.BuiltInSyntax)
		{
			_scheme = lwrCaseScheme;
			_port = defaultPort;
		}
	}

	internal static readonly UriParser HttpUri = new BuiltInUriParser("http", 80, System.UriSyntaxFlags.AllowAnInternetHost | System.UriSyntaxFlags.MustHaveAuthority | System.UriSyntaxFlags.MayHaveUserInfo | System.UriSyntaxFlags.MayHavePort | System.UriSyntaxFlags.MayHavePath | System.UriSyntaxFlags.MayHaveQuery | System.UriSyntaxFlags.MayHaveFragment | System.UriSyntaxFlags.AllowUncHost | System.UriSyntaxFlags.PathIsRooted | System.UriSyntaxFlags.ConvertPathSlashes | System.UriSyntaxFlags.CompressPath | System.UriSyntaxFlags.CanonicalizeAsFilePath | System.UriSyntaxFlags.AllowIdn | System.UriSyntaxFlags.AllowIriParsing);

	internal static readonly UriParser HttpsUri = new BuiltInUriParser("https", 443, HttpUri._flags);

	internal static readonly UriParser WsUri = new BuiltInUriParser("ws", 80, System.UriSyntaxFlags.AllowAnInternetHost | System.UriSyntaxFlags.MustHaveAuthority | System.UriSyntaxFlags.MayHaveUserInfo | System.UriSyntaxFlags.MayHavePort | System.UriSyntaxFlags.MayHavePath | System.UriSyntaxFlags.MayHaveQuery | System.UriSyntaxFlags.MayHaveFragment | System.UriSyntaxFlags.AllowUncHost | System.UriSyntaxFlags.PathIsRooted | System.UriSyntaxFlags.ConvertPathSlashes | System.UriSyntaxFlags.CompressPath | System.UriSyntaxFlags.CanonicalizeAsFilePath | System.UriSyntaxFlags.AllowIdn | System.UriSyntaxFlags.AllowIriParsing);

	internal static readonly UriParser WssUri = new BuiltInUriParser("wss", 443, System.UriSyntaxFlags.AllowAnInternetHost | System.UriSyntaxFlags.MustHaveAuthority | System.UriSyntaxFlags.MayHaveUserInfo | System.UriSyntaxFlags.MayHavePort | System.UriSyntaxFlags.MayHavePath | System.UriSyntaxFlags.MayHaveQuery | System.UriSyntaxFlags.MayHaveFragment | System.UriSyntaxFlags.AllowUncHost | System.UriSyntaxFlags.PathIsRooted | System.UriSyntaxFlags.ConvertPathSlashes | System.UriSyntaxFlags.CompressPath | System.UriSyntaxFlags.CanonicalizeAsFilePath | System.UriSyntaxFlags.AllowIdn | System.UriSyntaxFlags.AllowIriParsing);

	internal static readonly UriParser FtpUri = new BuiltInUriParser("ftp", 21, System.UriSyntaxFlags.AllowAnInternetHost | System.UriSyntaxFlags.MustHaveAuthority | System.UriSyntaxFlags.MayHaveUserInfo | System.UriSyntaxFlags.MayHavePort | System.UriSyntaxFlags.MayHavePath | System.UriSyntaxFlags.MayHaveFragment | System.UriSyntaxFlags.AllowUncHost | System.UriSyntaxFlags.PathIsRooted | System.UriSyntaxFlags.ConvertPathSlashes | System.UriSyntaxFlags.CompressPath | System.UriSyntaxFlags.CanonicalizeAsFilePath | System.UriSyntaxFlags.AllowIdn | System.UriSyntaxFlags.AllowIriParsing);

	internal static readonly UriParser FileUri = new BuiltInUriParser("file", -1, System.UriSyntaxFlags.AllowAnInternetHost | System.UriSyntaxFlags.MustHaveAuthority | System.UriSyntaxFlags.MayHavePath | System.UriSyntaxFlags.MayHaveQuery | System.UriSyntaxFlags.MayHaveFragment | System.UriSyntaxFlags.AllowEmptyHost | System.UriSyntaxFlags.AllowUncHost | System.UriSyntaxFlags.FileLikeUri | System.UriSyntaxFlags.AllowDOSPath | System.UriSyntaxFlags.PathIsRooted | System.UriSyntaxFlags.ConvertPathSlashes | System.UriSyntaxFlags.CompressPath | System.UriSyntaxFlags.CanonicalizeAsFilePath | System.UriSyntaxFlags.UnEscapeDotsAndSlashes | System.UriSyntaxFlags.AllowIdn | System.UriSyntaxFlags.AllowIriParsing);

	internal static readonly UriParser UnixFileUri = new BuiltInUriParser("file", -1, System.UriSyntaxFlags.AllowAnInternetHost | System.UriSyntaxFlags.MustHaveAuthority | System.UriSyntaxFlags.MayHavePath | System.UriSyntaxFlags.MayHaveQuery | System.UriSyntaxFlags.MayHaveFragment | System.UriSyntaxFlags.AllowEmptyHost | System.UriSyntaxFlags.AllowUncHost | System.UriSyntaxFlags.FileLikeUri | System.UriSyntaxFlags.AllowDOSPath | System.UriSyntaxFlags.PathIsRooted | System.UriSyntaxFlags.CompressPath | System.UriSyntaxFlags.CanonicalizeAsFilePath | System.UriSyntaxFlags.UnEscapeDotsAndSlashes | System.UriSyntaxFlags.AllowIdn | System.UriSyntaxFlags.AllowIriParsing);

	internal static readonly UriParser GopherUri = new BuiltInUriParser("gopher", 70, System.UriSyntaxFlags.AllowAnInternetHost | System.UriSyntaxFlags.MustHaveAuthority | System.UriSyntaxFlags.MayHaveUserInfo | System.UriSyntaxFlags.MayHavePort | System.UriSyntaxFlags.MayHavePath | System.UriSyntaxFlags.MayHaveFragment | System.UriSyntaxFlags.AllowUncHost | System.UriSyntaxFlags.PathIsRooted | System.UriSyntaxFlags.AllowIdn | System.UriSyntaxFlags.AllowIriParsing);

	internal static readonly UriParser NntpUri = new BuiltInUriParser("nntp", 119, System.UriSyntaxFlags.AllowAnInternetHost | System.UriSyntaxFlags.MustHaveAuthority | System.UriSyntaxFlags.MayHaveUserInfo | System.UriSyntaxFlags.MayHavePort | System.UriSyntaxFlags.MayHavePath | System.UriSyntaxFlags.MayHaveFragment | System.UriSyntaxFlags.AllowUncHost | System.UriSyntaxFlags.PathIsRooted | System.UriSyntaxFlags.AllowIdn | System.UriSyntaxFlags.AllowIriParsing);

	internal static readonly UriParser NewsUri = new BuiltInUriParser("news", -1, System.UriSyntaxFlags.MayHavePath | System.UriSyntaxFlags.MayHaveFragment | System.UriSyntaxFlags.AllowIriParsing);

	internal static readonly UriParser MailToUri = new BuiltInUriParser("mailto", 25, System.UriSyntaxFlags.AllowAnInternetHost | System.UriSyntaxFlags.MayHaveUserInfo | System.UriSyntaxFlags.MayHavePort | System.UriSyntaxFlags.MayHavePath | System.UriSyntaxFlags.MayHaveQuery | System.UriSyntaxFlags.MayHaveFragment | System.UriSyntaxFlags.AllowEmptyHost | System.UriSyntaxFlags.AllowUncHost | System.UriSyntaxFlags.MailToLikeUri | System.UriSyntaxFlags.AllowIdn | System.UriSyntaxFlags.AllowIriParsing);

	internal static readonly UriParser UuidUri = new BuiltInUriParser("uuid", -1, NewsUri._flags);

	internal static readonly UriParser TelnetUri = new BuiltInUriParser("telnet", 23, System.UriSyntaxFlags.AllowAnInternetHost | System.UriSyntaxFlags.MustHaveAuthority | System.UriSyntaxFlags.MayHaveUserInfo | System.UriSyntaxFlags.MayHavePort | System.UriSyntaxFlags.MayHavePath | System.UriSyntaxFlags.MayHaveFragment | System.UriSyntaxFlags.AllowUncHost | System.UriSyntaxFlags.PathIsRooted | System.UriSyntaxFlags.AllowIdn | System.UriSyntaxFlags.AllowIriParsing);

	internal static readonly UriParser LdapUri = new BuiltInUriParser("ldap", 389, System.UriSyntaxFlags.AllowAnInternetHost | System.UriSyntaxFlags.MustHaveAuthority | System.UriSyntaxFlags.MayHaveUserInfo | System.UriSyntaxFlags.MayHavePort | System.UriSyntaxFlags.MayHavePath | System.UriSyntaxFlags.MayHaveQuery | System.UriSyntaxFlags.MayHaveFragment | System.UriSyntaxFlags.AllowEmptyHost | System.UriSyntaxFlags.AllowUncHost | System.UriSyntaxFlags.PathIsRooted | System.UriSyntaxFlags.AllowIdn | System.UriSyntaxFlags.AllowIriParsing);

	internal static readonly UriParser NetTcpUri = new BuiltInUriParser("net.tcp", 808, System.UriSyntaxFlags.AllowAnInternetHost | System.UriSyntaxFlags.MustHaveAuthority | System.UriSyntaxFlags.MayHavePort | System.UriSyntaxFlags.MayHavePath | System.UriSyntaxFlags.MayHaveQuery | System.UriSyntaxFlags.MayHaveFragment | System.UriSyntaxFlags.PathIsRooted | System.UriSyntaxFlags.ConvertPathSlashes | System.UriSyntaxFlags.CompressPath | System.UriSyntaxFlags.CanonicalizeAsFilePath | System.UriSyntaxFlags.UnEscapeDotsAndSlashes | System.UriSyntaxFlags.AllowIdn | System.UriSyntaxFlags.AllowIriParsing);

	internal static readonly UriParser NetPipeUri = new BuiltInUriParser("net.pipe", -1, System.UriSyntaxFlags.AllowAnInternetHost | System.UriSyntaxFlags.MustHaveAuthority | System.UriSyntaxFlags.MayHavePath | System.UriSyntaxFlags.MayHaveQuery | System.UriSyntaxFlags.MayHaveFragment | System.UriSyntaxFlags.PathIsRooted | System.UriSyntaxFlags.ConvertPathSlashes | System.UriSyntaxFlags.CompressPath | System.UriSyntaxFlags.CanonicalizeAsFilePath | System.UriSyntaxFlags.UnEscapeDotsAndSlashes | System.UriSyntaxFlags.AllowIdn | System.UriSyntaxFlags.AllowIriParsing);

	internal static readonly UriParser VsMacrosUri = new BuiltInUriParser("vsmacros", -1, System.UriSyntaxFlags.AllowAnInternetHost | System.UriSyntaxFlags.MustHaveAuthority | System.UriSyntaxFlags.MayHavePath | System.UriSyntaxFlags.MayHaveFragment | System.UriSyntaxFlags.AllowEmptyHost | System.UriSyntaxFlags.AllowUncHost | System.UriSyntaxFlags.FileLikeUri | System.UriSyntaxFlags.AllowDOSPath | System.UriSyntaxFlags.ConvertPathSlashes | System.UriSyntaxFlags.CompressPath | System.UriSyntaxFlags.CanonicalizeAsFilePath | System.UriSyntaxFlags.UnEscapeDotsAndSlashes | System.UriSyntaxFlags.AllowIdn | System.UriSyntaxFlags.AllowIriParsing);

	private static readonly Hashtable s_table = new Hashtable(16)
	{
		{ HttpUri.SchemeName, HttpUri },
		{ HttpsUri.SchemeName, HttpsUri },
		{ WsUri.SchemeName, WsUri },
		{ WssUri.SchemeName, WssUri },
		{ FtpUri.SchemeName, FtpUri },
		{ FileUri.SchemeName, FileUri },
		{ GopherUri.SchemeName, GopherUri },
		{ NntpUri.SchemeName, NntpUri },
		{ NewsUri.SchemeName, NewsUri },
		{ MailToUri.SchemeName, MailToUri },
		{ UuidUri.SchemeName, UuidUri },
		{ TelnetUri.SchemeName, TelnetUri },
		{ LdapUri.SchemeName, LdapUri },
		{ NetTcpUri.SchemeName, NetTcpUri },
		{ NetPipeUri.SchemeName, NetPipeUri },
		{ VsMacrosUri.SchemeName, VsMacrosUri }
	};

	private static Hashtable s_tempTable = new Hashtable(25);

	private System.UriSyntaxFlags _flags;

	private int _port;

	private string _scheme;

	internal string SchemeName => _scheme;

	internal int DefaultPort => _port;

	internal System.UriSyntaxFlags Flags => _flags;

	internal bool IsSimple => InFact(System.UriSyntaxFlags.SimpleUserSyntax);

	protected UriParser()
		: this(System.UriSyntaxFlags.MayHavePath)
	{
	}

	protected virtual UriParser OnNewUri()
	{
		return this;
	}

	protected virtual void OnRegister(string schemeName, int defaultPort)
	{
	}

	protected virtual void InitializeAndValidate(Uri uri, out UriFormatException? parsingError)
	{
		if (uri._syntax == null)
		{
			throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
		}
		if (uri._syntax != this)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_uri_UserDrivenParsing, uri._syntax.GetType()));
		}
		ulong num = Interlocked.Or(ref Unsafe.As<Uri.Flags, ulong>(ref uri._flags), 4611686018427387904uL);
		if ((num & 0x4000000000000000L) != 0L)
		{
			throw new InvalidOperationException(System.SR.net_uri_InitializeCalledAlreadyOrTooLate);
		}
		parsingError = uri.ParseMinimal();
	}

	protected virtual string? Resolve(Uri baseUri, Uri? relativeUri, out UriFormatException? parsingError)
	{
		if (baseUri.UserDrivenParsing)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_uri_UserDrivenParsing, GetType()));
		}
		if (!baseUri.IsAbsoluteUri)
		{
			throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
		}
		string newUriString = null;
		bool userEscaped = false;
		parsingError = null;
		Uri uri = Uri.ResolveHelper(baseUri, relativeUri, ref newUriString, ref userEscaped);
		if (uri != null)
		{
			return uri.OriginalString;
		}
		return newUriString;
	}

	protected virtual bool IsBaseOf(Uri baseUri, Uri relativeUri)
	{
		return baseUri.IsBaseOfHelper(relativeUri);
	}

	protected virtual string GetComponents(Uri uri, UriComponents components, UriFormat format)
	{
		if (((uint)components & 0x80000000u) != 0 && components != UriComponents.SerializationInfoString)
		{
			throw new ArgumentOutOfRangeException("components", components, System.SR.net_uri_NotJustSerialization);
		}
		if (((uint)format & 0xFFFFFFFCu) != 0)
		{
			throw new ArgumentOutOfRangeException("format");
		}
		if (uri.UserDrivenParsing)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_uri_UserDrivenParsing, GetType()));
		}
		if (!uri.IsAbsoluteUri)
		{
			throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
		}
		if (uri.DisablePathAndQueryCanonicalization && (components & UriComponents.PathAndQuery) != 0)
		{
			throw new InvalidOperationException(System.SR.net_uri_GetComponentsCalledWhenCanonicalizationDisabled);
		}
		return uri.GetComponentsHelper(components, format);
	}

	protected virtual bool IsWellFormedOriginalString(Uri uri)
	{
		return uri.InternalIsWellFormedOriginalString();
	}

	public static void Register(UriParser uriParser, string schemeName, int defaultPort)
	{
		ArgumentNullException.ThrowIfNull(uriParser, "uriParser");
		ArgumentNullException.ThrowIfNull(schemeName, "schemeName");
		ArgumentOutOfRangeException.ThrowIfEqual(schemeName.Length, 1, "schemeName.Length");
		if (!Uri.CheckSchemeName(schemeName))
		{
			throw new ArgumentOutOfRangeException("schemeName");
		}
		if ((uint)defaultPort > 65535u && defaultPort != -1)
		{
			throw new ArgumentOutOfRangeException("defaultPort");
		}
		schemeName = schemeName.ToLowerInvariant();
		FetchSyntax(uriParser, schemeName, defaultPort);
	}

	public static bool IsKnownScheme(string schemeName)
	{
		ArgumentNullException.ThrowIfNull(schemeName, "schemeName");
		if (!Uri.CheckSchemeName(schemeName))
		{
			throw new ArgumentOutOfRangeException("schemeName");
		}
		return GetSyntax(schemeName.ToLowerInvariant())?.NotAny(System.UriSyntaxFlags.V1_UnknownUri) ?? false;
	}

	internal bool NotAny(System.UriSyntaxFlags flags)
	{
		return IsFullMatch(flags, System.UriSyntaxFlags.None);
	}

	internal bool InFact(System.UriSyntaxFlags flags)
	{
		return !IsFullMatch(flags, System.UriSyntaxFlags.None);
	}

	internal bool IsAllSet(System.UriSyntaxFlags flags)
	{
		return IsFullMatch(flags, flags);
	}

	private bool IsFullMatch(System.UriSyntaxFlags flags, System.UriSyntaxFlags expected)
	{
		return (_flags & flags) == expected;
	}

	internal UriParser(System.UriSyntaxFlags flags)
	{
		_flags = flags;
		_scheme = string.Empty;
	}

	private static void FetchSyntax(UriParser syntax, string lwrCaseSchemeName, int defaultPort)
	{
		if (syntax.SchemeName.Length != 0)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_uri_NeedFreshParser, syntax.SchemeName));
		}
		lock (s_table)
		{
			syntax._flags &= ~System.UriSyntaxFlags.V1_UnknownUri;
			UriParser uriParser = (UriParser)s_table[lwrCaseSchemeName];
			if (uriParser != null)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.net_uri_AlreadyRegistered, uriParser.SchemeName));
			}
			uriParser = (UriParser)s_tempTable[syntax.SchemeName];
			if (uriParser != null)
			{
				lwrCaseSchemeName = uriParser._scheme;
				s_tempTable.Remove(lwrCaseSchemeName);
			}
			syntax.OnRegister(lwrCaseSchemeName, defaultPort);
			syntax._scheme = lwrCaseSchemeName;
			syntax.CheckSetIsSimpleFlag();
			syntax._port = defaultPort;
			s_table[syntax.SchemeName] = syntax;
		}
	}

	internal static UriParser FindOrFetchAsUnknownV1Syntax(string lwrCaseScheme)
	{
		UriParser uriParser = (UriParser)s_table[lwrCaseScheme];
		if (uriParser != null)
		{
			return uriParser;
		}
		uriParser = (UriParser)s_tempTable[lwrCaseScheme];
		if (uriParser != null)
		{
			return uriParser;
		}
		lock (s_table)
		{
			if (s_tempTable.Count >= 512)
			{
				s_tempTable = new Hashtable(25);
			}
			uriParser = new BuiltInUriParser(lwrCaseScheme, -1, System.UriSyntaxFlags.AllowAnInternetHost | System.UriSyntaxFlags.OptionalAuthority | System.UriSyntaxFlags.MayHaveUserInfo | System.UriSyntaxFlags.MayHavePort | System.UriSyntaxFlags.MayHavePath | System.UriSyntaxFlags.MayHaveQuery | System.UriSyntaxFlags.MayHaveFragment | System.UriSyntaxFlags.AllowEmptyHost | System.UriSyntaxFlags.AllowUncHost | System.UriSyntaxFlags.V1_UnknownUri | System.UriSyntaxFlags.AllowDOSPath | System.UriSyntaxFlags.PathIsRooted | System.UriSyntaxFlags.ConvertPathSlashes | System.UriSyntaxFlags.CompressPath | System.UriSyntaxFlags.AllowIdn | System.UriSyntaxFlags.AllowIriParsing);
			s_tempTable[lwrCaseScheme] = uriParser;
			return uriParser;
		}
	}

	internal static UriParser GetSyntax(string lwrCaseScheme)
	{
		return (UriParser)(s_table[lwrCaseScheme] ?? s_tempTable[lwrCaseScheme]);
	}

	internal void CheckSetIsSimpleFlag()
	{
		Type type = GetType();
		if (type == typeof(GenericUriParser) || type == typeof(HttpStyleUriParser) || type == typeof(FtpStyleUriParser) || type == typeof(FileStyleUriParser) || type == typeof(NewsStyleUriParser) || type == typeof(GopherStyleUriParser) || type == typeof(NetPipeStyleUriParser) || type == typeof(NetTcpStyleUriParser) || type == typeof(LdapStyleUriParser))
		{
			_flags |= System.UriSyntaxFlags.SimpleUserSyntax;
		}
	}

	internal UriParser InternalOnNewUri()
	{
		UriParser uriParser = OnNewUri();
		if (this != uriParser)
		{
			uriParser._scheme = _scheme;
			uriParser._port = _port;
			uriParser._flags = _flags;
		}
		return uriParser;
	}

	internal void InternalValidate(Uri thisUri, out UriFormatException parsingError)
	{
		InitializeAndValidate(thisUri, out parsingError);
		Interlocked.Or(ref Unsafe.As<Uri.Flags, ulong>(ref thisUri._flags), 4611686018427387904uL);
	}

	internal string InternalResolve(Uri thisBaseUri, Uri uriLink, out UriFormatException parsingError)
	{
		return Resolve(thisBaseUri, uriLink, out parsingError);
	}

	internal bool InternalIsBaseOf(Uri thisBaseUri, Uri uriLink)
	{
		return IsBaseOf(thisBaseUri, uriLink);
	}

	internal string InternalGetComponents(Uri thisUri, UriComponents uriComponents, UriFormat uriFormat)
	{
		return GetComponents(thisUri, uriComponents, uriFormat);
	}

	internal bool InternalIsWellFormedOriginalString(Uri thisUri)
	{
		return IsWellFormedOriginalString(thisUri);
	}
}
