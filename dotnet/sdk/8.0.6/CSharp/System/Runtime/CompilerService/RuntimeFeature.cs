namespace System.Runtime.CompilerServices;

public static class RuntimeFeature
{
	public const string PortablePdb = "PortablePdb";

	public const string DefaultImplementationsOfInterfaces = "DefaultImplementationsOfInterfaces";

	public const string UnmanagedSignatureCallingConvention = "UnmanagedSignatureCallingConvention";

	public const string CovariantReturnsOfClasses = "CovariantReturnsOfClasses";

	public const string ByRefFields = "ByRefFields";

	public const string VirtualStaticsInInterfaces = "VirtualStaticsInInterfaces";

	public const string NumericIntPtr = "NumericIntPtr";

	public static bool IsDynamicCodeSupported { get; } = !AppContext.TryGetSwitch("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", out var isEnabled) || isEnabled;


	public static bool IsDynamicCodeCompiled => IsDynamicCodeSupported;

	public static bool IsSupported(string feature)
	{
		switch (feature)
		{
		case "ByRefFields":
		case "PortablePdb":
		case "CovariantReturnsOfClasses":
		case "UnmanagedSignatureCallingConvention":
		case "DefaultImplementationsOfInterfaces":
		case "VirtualStaticsInInterfaces":
		case "NumericIntPtr":
			return true;
		case "IsDynamicCodeSupported":
			return IsDynamicCodeSupported;
		case "IsDynamicCodeCompiled":
			return IsDynamicCodeCompiled;
		default:
			return false;
		}
	}
}
