using System.Runtime.CompilerServices;

namespace System.Reflection.Emit;

internal sealed class RuntimeEventBuilder : EventBuilder
{
	private readonly string m_name;

	private readonly int m_evToken;

	private readonly RuntimeModuleBuilder m_module;

	private readonly EventAttributes m_attributes;

	private readonly RuntimeTypeBuilder m_type;

	internal RuntimeEventBuilder(RuntimeModuleBuilder mod, string name, EventAttributes attr, RuntimeTypeBuilder type, int evToken)
	{
		m_name = name;
		m_module = mod;
		m_attributes = attr;
		m_evToken = evToken;
		m_type = type;
	}

	private void SetMethodSemantics(MethodBuilder mdBuilder, MethodSemanticsAttributes semantics)
	{
		ArgumentNullException.ThrowIfNull(mdBuilder, "mdBuilder");
		m_type.ThrowIfCreated();
		RuntimeModuleBuilder module = m_module;
		RuntimeTypeBuilder.DefineMethodSemantics(new QCallModule(ref module), m_evToken, semantics, mdBuilder.MetadataToken);
	}

	protected override void SetAddOnMethodCore(MethodBuilder mdBuilder)
	{
		SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.AddOn);
	}

	protected override void SetRemoveOnMethodCore(MethodBuilder mdBuilder)
	{
		SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.RemoveOn);
	}

	protected override void SetRaiseMethodCore(MethodBuilder mdBuilder)
	{
		SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Fire);
	}

	protected override void AddOtherMethodCore(MethodBuilder mdBuilder)
	{
		SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Other);
	}

	protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
	{
		m_type.ThrowIfCreated();
		RuntimeTypeBuilder.DefineCustomAttribute(m_module, m_evToken, m_module.GetMethodMetadataToken(con), binaryAttribute);
	}
}
