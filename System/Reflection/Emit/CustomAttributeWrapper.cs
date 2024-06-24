namespace System.Reflection.Emit;

internal readonly struct CustomAttributeWrapper
{
	private readonly ConstructorInfo _constructorInfo;

	private readonly byte[] _binaryAttribute;

	public ConstructorInfo Ctor => _constructorInfo;

	public byte[] Data => _binaryAttribute;

	public CustomAttributeWrapper(ConstructorInfo constructorInfo, ReadOnlySpan<byte> binaryAttribute)
	{
		_constructorInfo = constructorInfo;
		_binaryAttribute = binaryAttribute.ToArray();
	}
}
