using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Emit;

public abstract class EnumBuilder : TypeInfo
{
	public FieldBuilder UnderlyingField => UnderlyingFieldCore;

	protected abstract FieldBuilder UnderlyingFieldCore { get; }

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public Type CreateType()
	{
		return CreateTypeInfoCore();
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public TypeInfo CreateTypeInfo()
	{
		return CreateTypeInfoCore();
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	protected abstract TypeInfo CreateTypeInfoCore();

	public FieldBuilder DefineLiteral(string literalName, object? literalValue)
	{
		return DefineLiteralCore(literalName, literalValue);
	}

	protected abstract FieldBuilder DefineLiteralCore(string literalName, object? literalValue);

	public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
	{
		SetCustomAttributeCore(con, binaryAttribute);
	}

	protected abstract void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute);

	public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
	{
		SetCustomAttributeCore(customBuilder.Ctor, customBuilder.Data);
	}

	public override Type MakePointerType()
	{
		return SymbolType.FormCompoundType("*", this, 0);
	}

	public override Type MakeByRefType()
	{
		return SymbolType.FormCompoundType("&", this, 0);
	}

	[RequiresDynamicCode("The code for an array of the specified type might not be available.")]
	public override Type MakeArrayType()
	{
		return SymbolType.FormCompoundType("[]", this, 0);
	}

	[RequiresDynamicCode("The code for an array of the specified type might not be available.")]
	public override Type MakeArrayType(int rank)
	{
		string rankString = TypeInfo.GetRankString(rank);
		return SymbolType.FormCompoundType(rankString, this, 0);
	}
}
