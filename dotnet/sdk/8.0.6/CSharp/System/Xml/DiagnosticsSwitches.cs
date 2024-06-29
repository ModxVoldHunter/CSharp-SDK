using System.Diagnostics;

namespace System.Xml;

internal static class DiagnosticsSwitches
{
	private static volatile BooleanSwitch s_nonRecursiveTypeLoading;

	public static BooleanSwitch NonRecursiveTypeLoading => s_nonRecursiveTypeLoading ?? (s_nonRecursiveTypeLoading = new BooleanSwitch("XmlSerialization.NonRecursiveTypeLoading", "Turn on non-recursive algorithm generating XmlMappings for CLR types."));
}
