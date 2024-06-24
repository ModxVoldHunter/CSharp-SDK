namespace System.Runtime.InteropServices.Marshalling;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
public sealed class MarshalUsingAttribute : Attribute
{
	public const string ReturnsCountValue = "return-value";

	public Type? NativeType { get; }

	public string CountElementName { get; set; }

	public int ConstantElementCount { get; set; }

	public int ElementIndirectionDepth { get; set; }

	public MarshalUsingAttribute()
	{
		CountElementName = string.Empty;
	}

	public MarshalUsingAttribute(Type nativeType)
		: this()
	{
		NativeType = nativeType;
	}
}
