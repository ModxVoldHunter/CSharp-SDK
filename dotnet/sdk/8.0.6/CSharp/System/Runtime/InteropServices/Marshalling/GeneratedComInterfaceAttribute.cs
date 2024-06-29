namespace System.Runtime.InteropServices.Marshalling;

[AttributeUsage(AttributeTargets.Interface)]
public class GeneratedComInterfaceAttribute : Attribute
{
	public ComInterfaceOptions Options { get; set; } = ComInterfaceOptions.ManagedObjectWrapper | ComInterfaceOptions.ComObjectWrapper;


	public StringMarshalling StringMarshalling { get; set; }

	public Type? StringMarshallingCustomType { get; set; }
}
