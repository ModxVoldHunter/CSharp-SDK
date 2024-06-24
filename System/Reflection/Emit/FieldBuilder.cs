namespace System.Reflection.Emit;

public abstract class FieldBuilder : FieldInfo
{
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

	public void SetOffset(int iOffset)
	{
		SetOffsetCore(iOffset);
	}

	protected abstract void SetOffsetCore(int iOffset);
}
