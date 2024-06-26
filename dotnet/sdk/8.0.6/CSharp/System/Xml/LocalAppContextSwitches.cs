using System.Runtime.CompilerServices;

namespace System.Xml;

internal static class LocalAppContextSwitches
{
	private static int s_dontThrowOnInvalidSurrogatePairs;

	private static int s_ignoreEmptyKeySequences;

	private static int s_ignoreKindInUtcTimeSerialization;

	private static int s_limitXPathComplexity;

	private static int s_allowDefaultResolver;

	private static int s_isNetworkingEnabledByDefault;

	public static bool DontThrowOnInvalidSurrogatePairs
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return System.LocalAppContextSwitches.GetCachedSwitchValue("Switch.System.Xml.DontThrowOnInvalidSurrogatePairs", ref s_dontThrowOnInvalidSurrogatePairs);
		}
	}

	public static bool IgnoreEmptyKeySequences
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return System.LocalAppContextSwitches.GetCachedSwitchValue("Switch.System.Xml.IgnoreEmptyKeySequences", ref s_ignoreEmptyKeySequences);
		}
	}

	public static bool IgnoreKindInUtcTimeSerialization
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return System.LocalAppContextSwitches.GetCachedSwitchValue("Switch.System.Xml.IgnoreKindInUtcTimeSerialization", ref s_ignoreKindInUtcTimeSerialization);
		}
	}

	public static bool LimitXPathComplexity
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return System.LocalAppContextSwitches.GetCachedSwitchValue("Switch.System.Xml.LimitXPathComplexity", ref s_limitXPathComplexity);
		}
	}

	public static bool AllowDefaultResolver
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return System.LocalAppContextSwitches.GetCachedSwitchValue("Switch.System.Xml.AllowDefaultResolver", ref s_allowDefaultResolver);
		}
	}

	public static bool IsNetworkingEnabledByDefault
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return System.LocalAppContextSwitches.GetCachedSwitchValue("System.Xml.XmlResolver.IsNetworkingEnabledByDefault", ref s_isNetworkingEnabledByDefault);
		}
	}
}
