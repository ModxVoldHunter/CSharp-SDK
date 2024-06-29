namespace System;

public class GenericUriParser : UriParser
{
	public GenericUriParser(GenericUriParserOptions options)
		: base(MapGenericParserOptions(options))
	{
	}

	private static System.UriSyntaxFlags MapGenericParserOptions(GenericUriParserOptions options)
	{
		System.UriSyntaxFlags uriSyntaxFlags = System.UriSyntaxFlags.AllowAnInternetHost | System.UriSyntaxFlags.MustHaveAuthority | System.UriSyntaxFlags.MayHaveUserInfo | System.UriSyntaxFlags.MayHavePort | System.UriSyntaxFlags.MayHavePath | System.UriSyntaxFlags.MayHaveQuery | System.UriSyntaxFlags.MayHaveFragment | System.UriSyntaxFlags.AllowUncHost | System.UriSyntaxFlags.PathIsRooted | System.UriSyntaxFlags.ConvertPathSlashes | System.UriSyntaxFlags.CompressPath | System.UriSyntaxFlags.CanonicalizeAsFilePath | System.UriSyntaxFlags.UnEscapeDotsAndSlashes;
		if ((options & GenericUriParserOptions.GenericAuthority) != 0)
		{
			uriSyntaxFlags &= ~(System.UriSyntaxFlags.AllowAnInternetHost | System.UriSyntaxFlags.MayHaveUserInfo | System.UriSyntaxFlags.MayHavePort | System.UriSyntaxFlags.AllowUncHost);
			uriSyntaxFlags |= System.UriSyntaxFlags.AllowAnyOtherHost;
		}
		if ((options & GenericUriParserOptions.AllowEmptyAuthority) != 0)
		{
			uriSyntaxFlags |= System.UriSyntaxFlags.AllowEmptyHost;
		}
		if ((options & GenericUriParserOptions.NoUserInfo) != 0)
		{
			uriSyntaxFlags &= ~System.UriSyntaxFlags.MayHaveUserInfo;
		}
		if ((options & GenericUriParserOptions.NoPort) != 0)
		{
			uriSyntaxFlags &= ~System.UriSyntaxFlags.MayHavePort;
		}
		if ((options & GenericUriParserOptions.NoQuery) != 0)
		{
			uriSyntaxFlags &= ~System.UriSyntaxFlags.MayHaveQuery;
		}
		if ((options & GenericUriParserOptions.NoFragment) != 0)
		{
			uriSyntaxFlags &= ~System.UriSyntaxFlags.MayHaveFragment;
		}
		if ((options & GenericUriParserOptions.DontConvertPathBackslashes) != 0)
		{
			uriSyntaxFlags &= ~System.UriSyntaxFlags.ConvertPathSlashes;
		}
		if ((options & GenericUriParserOptions.DontCompressPath) != 0)
		{
			uriSyntaxFlags &= ~(System.UriSyntaxFlags.CompressPath | System.UriSyntaxFlags.CanonicalizeAsFilePath);
		}
		if ((options & GenericUriParserOptions.DontUnescapePathDotsAndSlashes) != 0)
		{
			uriSyntaxFlags &= ~System.UriSyntaxFlags.UnEscapeDotsAndSlashes;
		}
		if ((options & GenericUriParserOptions.Idn) != 0)
		{
			uriSyntaxFlags |= System.UriSyntaxFlags.AllowIdn;
		}
		if ((options & GenericUriParserOptions.IriParsing) != 0)
		{
			uriSyntaxFlags |= System.UriSyntaxFlags.AllowIriParsing;
		}
		return uriSyntaxFlags;
	}
}
