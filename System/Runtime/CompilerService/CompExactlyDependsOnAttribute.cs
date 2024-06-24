namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
internal sealed class CompExactlyDependsOnAttribute : Attribute
{
	[CompilerGenerated]
	private readonly Type _003CIntrinsicsTypeUsedInHelperFunction_003Ek__BackingField;

	public CompExactlyDependsOnAttribute(Type intrinsicsTypeUsedInHelperFunction)
	{
		_003CIntrinsicsTypeUsedInHelperFunction_003Ek__BackingField = intrinsicsTypeUsedInHelperFunction;
	}
}
