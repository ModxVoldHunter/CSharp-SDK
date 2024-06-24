namespace System.Reflection.Emit;

public abstract class ParameterBuilder
{
	public virtual int Attributes
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public bool IsIn => (Attributes & 1) != 0;

	public bool IsOptional => (Attributes & 0x10) != 0;

	public bool IsOut => (Attributes & 2) != 0;

	public virtual string? Name
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual int Position
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual void SetConstant(object? defaultValue)
	{
		throw new NotImplementedException();
	}

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
}
