using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.Metadata;
using System.Text;

namespace System.Reflection;

internal sealed class RuntimePropertyInfo : PropertyInfo
{
	private readonly int m_token;

	private string m_name;

	private unsafe readonly void* m_utf8name;

	private readonly PropertyAttributes m_flags;

	private readonly RuntimeType.RuntimeTypeCache m_reflectedTypeCache;

	private readonly RuntimeMethodInfo m_getterMethod;

	private readonly RuntimeMethodInfo m_setterMethod;

	private readonly MethodInfo[] m_otherMethod;

	private readonly RuntimeType m_declaringType;

	private readonly BindingFlags m_bindingFlags;

	private Signature m_signature;

	private ParameterInfo[] m_parameters;

	internal unsafe Signature Signature
	{
		get
		{
			if (m_signature == null)
			{
				GetRuntimeModule().MetadataImport.GetPropertyProps(m_token, out var _, out var _, out var signature);
				m_signature = new Signature(((IntPtr)signature.Signature).ToPointer(), signature.Length, m_declaringType);
			}
			return m_signature;
		}
	}

	internal BindingFlags BindingFlags => m_bindingFlags;

	public override MemberTypes MemberType => MemberTypes.Property;

	public unsafe override string Name => m_name ?? (m_name = new MdUtf8String(m_utf8name).ToString());

	public override Type DeclaringType => m_declaringType;

	public override Type ReflectedType => ReflectedTypeInternal;

	private RuntimeType ReflectedTypeInternal => m_reflectedTypeCache.GetRuntimeType();

	public override int MetadataToken => m_token;

	public override Module Module => GetRuntimeModule();

	public override bool IsCollectible => m_declaringType.IsCollectible;

	public override Type PropertyType => Signature.ReturnType;

	public override PropertyAttributes Attributes => m_flags;

	public override bool CanRead => m_getterMethod != null;

	public override bool CanWrite => m_setterMethod != null;

	internal unsafe RuntimePropertyInfo(int tkProperty, RuntimeType declaredType, RuntimeType.RuntimeTypeCache reflectedTypeCache, out bool isPrivate)
	{
		MetadataImport metadataImport = declaredType.GetRuntimeModule().MetadataImport;
		m_token = tkProperty;
		m_reflectedTypeCache = reflectedTypeCache;
		m_declaringType = declaredType;
		metadataImport.GetPropertyProps(tkProperty, out m_utf8name, out m_flags, out var _);
		Associates.AssignAssociates(metadataImport, tkProperty, declaredType, reflectedTypeCache.GetRuntimeType(), out var _, out var _, out var _, out m_getterMethod, out m_setterMethod, out m_otherMethod, out isPrivate, out m_bindingFlags);
	}

	internal override bool CacheEquals(object o)
	{
		if (o is RuntimePropertyInfo runtimePropertyInfo && runtimePropertyInfo.m_token == m_token)
		{
			return (object)m_declaringType == runtimePropertyInfo.m_declaringType;
		}
		return false;
	}

	internal bool EqualsSig(RuntimePropertyInfo target)
	{
		return Signature.CompareSig(Signature, target.Signature);
	}

	public override string ToString()
	{
		ValueStringBuilder sbParamList = new ValueStringBuilder(100);
		sbParamList.Append(PropertyType.FormatTypeName());
		sbParamList.Append(' ');
		sbParamList.Append(Name);
		RuntimeType[] arguments = Signature.Arguments;
		if (arguments.Length != 0)
		{
			sbParamList.Append(" [");
			Type[] parameterTypes = arguments;
			MethodBase.AppendParameters(ref sbParamList, parameterTypes, Signature.CallingConvention);
			sbParamList.Append(']');
		}
		return sbParamList.ToString();
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		ArgumentNullException.ThrowIfNull(attributeType, "attributeType");
		if (!(attributeType.UnderlyingSystemType is RuntimeType caType))
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.GetCustomAttributes(this, caType);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		ArgumentNullException.ThrowIfNull(attributeType, "attributeType");
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

	public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
	{
		return HasSameMetadataDefinitionAsCore<RuntimePropertyInfo>(other);
	}

	internal RuntimeModule GetRuntimeModule()
	{
		return m_declaringType.GetRuntimeModule();
	}

	public override bool Equals(object obj)
	{
		if (this != obj)
		{
			if (MetadataUpdater.IsSupported && obj is RuntimePropertyInfo runtimePropertyInfo && runtimePropertyInfo.m_token == m_token && (object)runtimePropertyInfo.m_declaringType == m_declaringType)
			{
				return (object)runtimePropertyInfo.m_reflectedTypeCache.GetRuntimeType() == m_reflectedTypeCache.GetRuntimeType();
			}
			return false;
		}
		return true;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(m_token.GetHashCode(), ((IntPtr)m_declaringType.GetUnderlyingNativeHandle()).GetHashCode());
	}

	public override Type[] GetRequiredCustomModifiers()
	{
		return Signature.GetCustomModifiers(0, required: true);
	}

	public override Type[] GetOptionalCustomModifiers()
	{
		return Signature.GetCustomModifiers(0, required: false);
	}

	public override Type GetModifiedPropertyType()
	{
		return ModifiedType.Create(PropertyType, Signature);
	}

	internal object GetConstantValue(bool raw)
	{
		object value = MdConstant.GetValue(GetRuntimeModule().MetadataImport, m_token, PropertyType.TypeHandle, raw);
		if (value == DBNull.Value)
		{
			throw new InvalidOperationException(SR.Arg_EnumLitValueNotFound);
		}
		return value;
	}

	public override object GetConstantValue()
	{
		return GetConstantValue(raw: false);
	}

	public override object GetRawConstantValue()
	{
		return GetConstantValue(raw: true);
	}

	public override MethodInfo[] GetAccessors(bool nonPublic)
	{
		List<MethodInfo> list = new List<MethodInfo>();
		if (Associates.IncludeAccessor(m_getterMethod, nonPublic))
		{
			list.Add(m_getterMethod);
		}
		if (Associates.IncludeAccessor(m_setterMethod, nonPublic))
		{
			list.Add(m_setterMethod);
		}
		if (m_otherMethod != null)
		{
			for (int i = 0; i < m_otherMethod.Length; i++)
			{
				if (Associates.IncludeAccessor(m_otherMethod[i], nonPublic))
				{
					list.Add(m_otherMethod[i]);
				}
			}
		}
		return list.ToArray();
	}

	public override RuntimeMethodInfo GetGetMethod(bool nonPublic)
	{
		if (!Associates.IncludeAccessor(m_getterMethod, nonPublic))
		{
			return null;
		}
		return m_getterMethod;
	}

	public override RuntimeMethodInfo GetSetMethod(bool nonPublic)
	{
		if (!Associates.IncludeAccessor(m_setterMethod, nonPublic))
		{
			return null;
		}
		return m_setterMethod;
	}

	public override ParameterInfo[] GetIndexParameters()
	{
		ParameterInfo[] indexParametersNoCopy = GetIndexParametersNoCopy();
		int num = indexParametersNoCopy.Length;
		if (num == 0)
		{
			return indexParametersNoCopy;
		}
		ParameterInfo[] array = new ParameterInfo[num];
		Array.Copy(indexParametersNoCopy, array, num);
		return array;
	}

	internal ParameterInfo[] GetIndexParametersNoCopy()
	{
		if (m_parameters == null)
		{
			int num = 0;
			ParameterInfo[] array = null;
			RuntimeMethodInfo getMethod = GetGetMethod(nonPublic: true);
			if (getMethod != null)
			{
				array = getMethod.GetParametersNoCopy();
				num = array.Length;
			}
			else
			{
				getMethod = GetSetMethod(nonPublic: true);
				if (getMethod != null)
				{
					array = getMethod.GetParametersNoCopy();
					num = array.Length - 1;
				}
			}
			ParameterInfo[] array2 = ((num != 0) ? new ParameterInfo[num] : Array.Empty<ParameterInfo>());
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i] = new RuntimeParameterInfo((RuntimeParameterInfo)array[i], this);
			}
			m_parameters = array2;
		}
		return m_parameters;
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	public override object GetValue(object obj, object[] index)
	{
		return GetValue(obj, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, index, null);
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
	{
		RuntimeMethodInfo getMethod = GetGetMethod(nonPublic: true);
		if (getMethod == null)
		{
			throw new ArgumentException(SR.Arg_GetMethNotFnd);
		}
		return getMethod.Invoke(obj, invokeAttr, binder, index, null);
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	public override void SetValue(object obj, object value, object[] index)
	{
		SetValue(obj, value, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, index, null);
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
	{
		RuntimeMethodInfo setMethod = GetSetMethod(nonPublic: true);
		if (setMethod == null)
		{
			throw new ArgumentException(SR.Arg_SetMethNotFnd);
		}
		if (index == null)
		{
			setMethod.InvokePropertySetter(obj, invokeAttr, binder, value, culture);
			return;
		}
		object[] array = new object[index.Length + 1];
		for (int i = 0; i < index.Length; i++)
		{
			array[i] = index[i];
		}
		array[index.Length] = value;
		setMethod.Invoke(obj, invokeAttr, binder, array, culture);
	}
}
