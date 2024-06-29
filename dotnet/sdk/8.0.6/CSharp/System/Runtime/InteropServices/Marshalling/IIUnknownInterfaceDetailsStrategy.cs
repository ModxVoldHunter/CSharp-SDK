namespace System.Runtime.InteropServices.Marshalling;

[CLSCompliant(false)]
public interface IIUnknownInterfaceDetailsStrategy
{
	IIUnknownDerivedDetails? GetIUnknownDerivedDetails(RuntimeTypeHandle type);

	IComExposedDetails? GetComExposedTypeDetails(RuntimeTypeHandle type);
}
