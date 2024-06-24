namespace System.Reflection.Emit;

public abstract class EventBuilder
{
	public void AddOtherMethod(MethodBuilder mdBuilder)
	{
		AddOtherMethodCore(mdBuilder);
	}

	protected abstract void AddOtherMethodCore(MethodBuilder mdBuilder);

	public void SetAddOnMethod(MethodBuilder mdBuilder)
	{
		SetAddOnMethodCore(mdBuilder);
	}

	protected abstract void SetAddOnMethodCore(MethodBuilder mdBuilder);

	public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
	{
		ArgumentNullException.ThrowIfNull(con, "con");
		ArgumentNullException.ThrowIfNull(binaryAttribute, "binaryAttribute");
		SetCustomAttributeCore(con, binaryAttribute);
	}

	protected abstract void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute);

	public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
	{
		ArgumentNullException.ThrowIfNull(customBuilder, "customBuilder");
		SetCustomAttributeCore(customBuilder.Ctor, customBuilder.Data);
	}

	public void SetRaiseMethod(MethodBuilder mdBuilder)
	{
		SetRaiseMethodCore(mdBuilder);
	}

	protected abstract void SetRaiseMethodCore(MethodBuilder mdBuilder);

	public void SetRemoveOnMethod(MethodBuilder mdBuilder)
	{
		SetRemoveOnMethodCore(mdBuilder);
	}

	protected abstract void SetRemoveOnMethodCore(MethodBuilder mdBuilder);
}
