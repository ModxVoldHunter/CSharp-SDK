namespace System.Reflection.Emit;

public abstract class PropertyBuilder : PropertyInfo
{
	public void AddOtherMethod(MethodBuilder mdBuilder)
	{
		AddOtherMethodCore(mdBuilder);
	}

	protected abstract void AddOtherMethodCore(MethodBuilder mdBuilder);

	public void SetConstant(object? defaultValue)
	{
		SetConstantCore(defaultValue);
	}

	protected abstract void SetConstantCore(object? defaultValue);

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

	public void SetGetMethod(MethodBuilder mdBuilder)
	{
		SetGetMethodCore(mdBuilder);
	}

	protected abstract void SetGetMethodCore(MethodBuilder mdBuilder);

	public void SetSetMethod(MethodBuilder mdBuilder)
	{
		SetSetMethodCore(mdBuilder);
	}

	protected abstract void SetSetMethodCore(MethodBuilder mdBuilder);
}
