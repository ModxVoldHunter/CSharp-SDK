using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Reflection;

internal sealed class RuntimeParameterInfo : ParameterInfo
{
	private readonly int m_tkParamDef;

	private readonly MetadataImport m_scope;

	private readonly Signature m_signature;

	private volatile bool m_nameIsCached;

	private readonly bool m_noMetadata;

	private bool m_noDefaultValue;

	private readonly MethodBase m_originalMember;

	public override Type ParameterType
	{
		get
		{
			if (ClassImpl == null)
			{
				RuntimeType classImpl = ((PositionImpl != -1) ? m_signature.Arguments[PositionImpl] : m_signature.ReturnType);
				ClassImpl = classImpl;
			}
			return ClassImpl;
		}
	}

	public override string Name
	{
		get
		{
			if (!m_nameIsCached)
			{
				if (!System.Reflection.MetadataToken.IsNullToken(m_tkParamDef))
				{
					string nameImpl = m_scope.GetName(m_tkParamDef).ToString();
					NameImpl = nameImpl;
				}
				m_nameIsCached = true;
			}
			return NameImpl;
		}
	}

	public override bool HasDefaultValue
	{
		get
		{
			if (m_noMetadata || m_noDefaultValue)
			{
				return false;
			}
			object defaultValue;
			return TryGetDefaultValueInternal(raw: false, out defaultValue);
		}
	}

	public override object DefaultValue => GetDefaultValue(raw: false);

	public override object RawDefaultValue => GetDefaultValue(raw: true);

	public override int MetadataToken => m_tkParamDef;

	internal static ParameterInfo[] GetParameters(IRuntimeMethodInfo method, MemberInfo member, Signature sig)
	{
		ParameterInfo returnParameter;
		return GetParameters(method, member, sig, out returnParameter, fetchReturnParameter: false);
	}

	internal static ParameterInfo GetReturnParameter(IRuntimeMethodInfo method, MemberInfo member, Signature sig)
	{
		GetParameters(method, member, sig, out var returnParameter, fetchReturnParameter: true);
		return returnParameter;
	}

	private static ParameterInfo[] GetParameters(IRuntimeMethodInfo methodHandle, MemberInfo member, Signature sig, out ParameterInfo returnParameter, bool fetchReturnParameter)
	{
		returnParameter = null;
		int num = sig.Arguments.Length;
		ParameterInfo[] array = (fetchReturnParameter ? null : ((num == 0) ? Array.Empty<ParameterInfo>() : new ParameterInfo[num]));
		int methodDef = RuntimeMethodHandle.GetMethodDef(methodHandle);
		int num2 = 0;
		if (!System.Reflection.MetadataToken.IsNullToken(methodDef))
		{
			MetadataImport metadataImport = RuntimeTypeHandle.GetMetadataImport(RuntimeMethodHandle.GetDeclaringType(methodHandle));
			metadataImport.EnumParams(methodDef, out var result);
			num2 = result.Length;
			if (num2 > num + 1)
			{
				throw new BadImageFormatException(SR.BadImageFormat_ParameterSignatureMismatch);
			}
			for (int i = 0; i < num2; i++)
			{
				int num3 = result[i];
				metadataImport.GetParamDefProps(num3, out var sequence, out var attributes);
				sequence--;
				if (fetchReturnParameter && sequence == -1)
				{
					if (returnParameter != null)
					{
						throw new BadImageFormatException(SR.BadImageFormat_ParameterSignatureMismatch);
					}
					returnParameter = new RuntimeParameterInfo(sig, metadataImport, num3, sequence, attributes, member);
				}
				else if (!fetchReturnParameter && sequence >= 0)
				{
					if (sequence >= num)
					{
						throw new BadImageFormatException(SR.BadImageFormat_ParameterSignatureMismatch);
					}
					array[sequence] = new RuntimeParameterInfo(sig, metadataImport, num3, sequence, attributes, member);
				}
			}
		}
		if (fetchReturnParameter)
		{
			if (returnParameter == null)
			{
				returnParameter = new RuntimeParameterInfo(sig, MetadataImport.EmptyImport, 0, -1, ParameterAttributes.None, member);
			}
		}
		else if (num2 < array.Length + 1)
		{
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j] == null)
				{
					array[j] = new RuntimeParameterInfo(sig, MetadataImport.EmptyImport, 0, j, ParameterAttributes.None, member);
				}
			}
		}
		return array;
	}

	internal void SetName(string name)
	{
		NameImpl = name;
	}

	internal void SetAttributes(ParameterAttributes attributes)
	{
		AttrsImpl = attributes;
	}

	internal RuntimeParameterInfo(RuntimeParameterInfo accessor, RuntimePropertyInfo property)
		: this(accessor, (MemberInfo)property)
	{
		m_signature = property.Signature;
	}

	private RuntimeParameterInfo(RuntimeParameterInfo accessor, MemberInfo member)
	{
		MemberImpl = member;
		m_originalMember = accessor.MemberImpl as MethodBase;
		NameImpl = accessor.Name;
		m_nameIsCached = true;
		ClassImpl = accessor.ParameterType;
		PositionImpl = accessor.Position;
		AttrsImpl = accessor.Attributes;
		m_tkParamDef = (System.Reflection.MetadataToken.IsNullToken(accessor.MetadataToken) ? 134217728 : accessor.MetadataToken);
		m_scope = accessor.m_scope;
	}

	private RuntimeParameterInfo(Signature signature, MetadataImport scope, int tkParamDef, int position, ParameterAttributes attributes, MemberInfo member)
	{
		PositionImpl = position;
		MemberImpl = member;
		m_signature = signature;
		m_tkParamDef = (System.Reflection.MetadataToken.IsNullToken(tkParamDef) ? 134217728 : tkParamDef);
		m_scope = scope;
		AttrsImpl = attributes;
		ClassImpl = null;
		NameImpl = null;
	}

	internal RuntimeParameterInfo(MethodInfo owner, string name, Type parameterType, int position)
	{
		MemberImpl = owner;
		NameImpl = name;
		m_nameIsCached = true;
		m_noMetadata = true;
		ClassImpl = parameterType;
		PositionImpl = position;
		AttrsImpl = ParameterAttributes.None;
		m_tkParamDef = 134217728;
		m_scope = MetadataImport.EmptyImport;
	}

	private object GetDefaultValue(bool raw)
	{
		if (m_noMetadata)
		{
			return null;
		}
		if (!TryGetDefaultValueInternal(raw, out var defaultValue) && base.IsOptional)
		{
			return Type.Missing;
		}
		return defaultValue;
	}

	private object GetDefaultValueFromCustomAttributeData()
	{
		foreach (CustomAttributeData customAttribute in CustomAttributeData.GetCustomAttributes(this))
		{
			Type attributeType = customAttribute.AttributeType;
			if (attributeType == typeof(DecimalConstantAttribute))
			{
				return GetRawDecimalConstant(customAttribute);
			}
			if (attributeType.IsSubclassOf(typeof(CustomConstantAttribute)))
			{
				if (attributeType == typeof(DateTimeConstantAttribute))
				{
					return GetRawDateTimeConstant(customAttribute);
				}
				return GetRawConstant(customAttribute);
			}
		}
		return DBNull.Value;
	}

	private object GetDefaultValueFromCustomAttributes()
	{
		object[] customAttributes = GetCustomAttributes(typeof(CustomConstantAttribute), inherit: false);
		if (customAttributes.Length != 0)
		{
			return ((CustomConstantAttribute)customAttributes[0]).Value;
		}
		customAttributes = GetCustomAttributes(typeof(DecimalConstantAttribute), inherit: false);
		if (customAttributes.Length != 0)
		{
			return ((DecimalConstantAttribute)customAttributes[0]).Value;
		}
		return DBNull.Value;
	}

	private bool TryGetDefaultValueInternal(bool raw, out object defaultValue)
	{
		if (m_noDefaultValue || System.Reflection.MetadataToken.IsNullToken(m_tkParamDef))
		{
			defaultValue = DBNull.Value;
			m_noDefaultValue = true;
			return false;
		}
		defaultValue = MdConstant.GetValue(m_scope, m_tkParamDef, ParameterType.TypeHandle, raw);
		if (defaultValue == DBNull.Value)
		{
			defaultValue = (raw ? GetDefaultValueFromCustomAttributeData() : GetDefaultValueFromCustomAttributes());
			if (defaultValue == DBNull.Value)
			{
				m_noDefaultValue = true;
				return false;
			}
		}
		return true;
	}

	private static decimal GetRawDecimalConstant(CustomAttributeData attr)
	{
		IList<CustomAttributeTypedArgument> constructorArguments = attr.ConstructorArguments;
		return new decimal(GetConstructorArgument(constructorArguments, 4), GetConstructorArgument(constructorArguments, 3), GetConstructorArgument(constructorArguments, 2), (byte)constructorArguments[1].Value != 0, (byte)constructorArguments[0].Value);
		static int GetConstructorArgument(IList<CustomAttributeTypedArgument> args, int index)
		{
			object value = args[index].Value;
			if (value is int)
			{
				return (int)value;
			}
			return (int)(uint)value;
		}
	}

	private static DateTime GetRawDateTimeConstant(CustomAttributeData attr)
	{
		return new DateTime((long)attr.ConstructorArguments[0].Value);
	}

	private static object GetRawConstant(CustomAttributeData attr)
	{
		foreach (CustomAttributeNamedArgument namedArgument in attr.NamedArguments)
		{
			if (namedArgument.MemberInfo.Name.Equals("Value"))
			{
				return namedArgument.TypedValue.Value;
			}
		}
		return DBNull.Value;
	}

	internal RuntimeModule GetRuntimeModule()
	{
		RuntimeMethodInfo runtimeMethodInfo = Member as RuntimeMethodInfo;
		RuntimeConstructorInfo runtimeConstructorInfo = Member as RuntimeConstructorInfo;
		RuntimePropertyInfo runtimePropertyInfo = Member as RuntimePropertyInfo;
		if (runtimeMethodInfo != null)
		{
			return runtimeMethodInfo.GetRuntimeModule();
		}
		if (runtimeConstructorInfo != null)
		{
			return runtimeConstructorInfo.GetRuntimeModule();
		}
		if (runtimePropertyInfo != null)
		{
			return runtimePropertyInfo.GetRuntimeModule();
		}
		return null;
	}

	public override Type[] GetRequiredCustomModifiers()
	{
		if (m_signature != null)
		{
			return m_signature.GetCustomModifiers(PositionImpl + 1, required: true);
		}
		return Type.EmptyTypes;
	}

	public override Type[] GetOptionalCustomModifiers()
	{
		if (m_signature != null)
		{
			return m_signature.GetCustomModifiers(PositionImpl + 1, required: false);
		}
		return Type.EmptyTypes;
	}

	public override Type GetModifiedParameterType()
	{
		return ModifiedType.Create(ParameterType, m_signature, PositionImpl + 1);
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		if (System.Reflection.MetadataToken.IsNullToken(m_tkParamDef))
		{
			return Array.Empty<object>();
		}
		return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		ArgumentNullException.ThrowIfNull(attributeType, "attributeType");
		if (!(attributeType.UnderlyingSystemType is RuntimeType caType))
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		if (System.Reflection.MetadataToken.IsNullToken(m_tkParamDef))
		{
			return CustomAttribute.CreateAttributeArrayHelper(caType, 0);
		}
		return CustomAttribute.GetCustomAttributes(this, caType);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		ArgumentNullException.ThrowIfNull(attributeType, "attributeType");
		if (System.Reflection.MetadataToken.IsNullToken(m_tkParamDef))
		{
			return false;
		}
		if (!(attributeType.UnderlyingSystemType is RuntimeType caType))
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.IsDefined(this, caType);
	}

	public override IList<CustomAttributeData> GetCustomAttributesData()
	{
		return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
	}
}
