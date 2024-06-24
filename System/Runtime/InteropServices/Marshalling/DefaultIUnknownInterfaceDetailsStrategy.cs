namespace System.Runtime.InteropServices.Marshalling;

internal sealed class DefaultIUnknownInterfaceDetailsStrategy : IIUnknownInterfaceDetailsStrategy
{
	public static readonly IIUnknownInterfaceDetailsStrategy Instance = new System.Runtime.InteropServices.Marshalling.DefaultIUnknownInterfaceDetailsStrategy();

	public IComExposedDetails GetComExposedTypeDetails(RuntimeTypeHandle type)
	{
		return IComExposedDetails.GetFromAttribute(type);
	}

	public IIUnknownDerivedDetails GetIUnknownDerivedDetails(RuntimeTypeHandle type)
	{
		return IIUnknownDerivedDetails.GetFromAttribute(type);
	}
}
