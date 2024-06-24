namespace System.Reflection.Emit;

public abstract class ConstructorBuilder : ConstructorInfo
{
	public bool InitLocals
	{
		get
		{
			return InitLocalsCore;
		}
		set
		{
			InitLocalsCore = value;
		}
	}

	protected abstract bool InitLocalsCore { get; set; }

	public ParameterBuilder DefineParameter(int iSequence, ParameterAttributes attributes, string strParamName)
	{
		return DefineParameterCore(iSequence, attributes, strParamName);
	}

	protected abstract ParameterBuilder DefineParameterCore(int iSequence, ParameterAttributes attributes, string strParamName);

	public ILGenerator GetILGenerator()
	{
		return GetILGeneratorCore(64);
	}

	public ILGenerator GetILGenerator(int streamSize)
	{
		return GetILGeneratorCore(streamSize);
	}

	protected abstract ILGenerator GetILGeneratorCore(int streamSize);

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

	public void SetImplementationFlags(MethodImplAttributes attributes)
	{
		SetImplementationFlagsCore(attributes);
	}

	protected abstract void SetImplementationFlagsCore(MethodImplAttributes attributes);
}
