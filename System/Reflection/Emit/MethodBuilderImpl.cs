using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Metadata;

namespace System.Reflection.Emit;

internal sealed class MethodBuilderImpl : MethodBuilder
{
	private Type _returnType;

	private Type[] _parameterTypes;

	private readonly ModuleBuilderImpl _module;

	private readonly string _name;

	private readonly CallingConventions _callingConventions;

	private readonly TypeBuilderImpl _declaringType;

	private MethodAttributes _attributes;

	private MethodImplAttributes _methodImplFlags;

	private GenericTypeParameterBuilderImpl[] _typeParameters;

	internal DllImportData _dllImportData;

	internal List<CustomAttributeWrapper> _customAttributes;

	internal ParameterBuilderImpl[] _parameters;

	protected override bool InitLocalsCore
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public override string Name => _name;

	public override MethodAttributes Attributes => _attributes;

	public override CallingConventions CallingConvention => _callingConventions;

	public override TypeBuilder DeclaringType => _declaringType;

	public override Module Module => _module;

	public override bool ContainsGenericParameters
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public override bool IsGenericMethod => _typeParameters != null;

	public override bool IsGenericMethodDefinition => _typeParameters != null;

	public override bool IsSecurityCritical => true;

	public override bool IsSecuritySafeCritical => false;

	public override bool IsSecurityTransparent => false;

	public override int MetadataToken
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override RuntimeMethodHandle MethodHandle
	{
		get
		{
			throw new NotSupportedException(System.SR.NotSupported_DynamicModule);
		}
	}

	public override Type ReflectedType
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override ParameterInfo ReturnParameter
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override Type ReturnType => _returnType;

	public override ICustomAttributeProvider ReturnTypeCustomAttributes
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	internal MethodBuilderImpl(string name, MethodAttributes attributes, CallingConventions callingConventions, Type returnType, Type[] parameterTypes, ModuleBuilderImpl module, TypeBuilderImpl declaringType)
	{
		_module = module;
		_returnType = returnType ?? _module.GetTypeFromCoreAssembly(CoreTypeId.Void);
		_name = name;
		_attributes = attributes;
		_callingConventions = callingConventions;
		_declaringType = declaringType;
		if (parameterTypes != null)
		{
			_parameterTypes = new Type[parameterTypes.Length];
			_parameters = new ParameterBuilderImpl[parameterTypes.Length + 1];
			for (int i = 0; i < parameterTypes.Length; i++)
			{
				ArgumentNullException.ThrowIfNull(_parameterTypes[i] = parameterTypes[i], "parameterTypes");
			}
		}
		_methodImplFlags = MethodImplAttributes.IL;
	}

	internal BlobBuilder GetMethodSignatureBlob()
	{
		return MetadataSignatureHelper.MethodSignatureEncoder(_module, _parameterTypes, ReturnType, GetSignatureConvention(_callingConventions), GetGenericArguments().Length, !base.IsStatic);
	}

	internal static SignatureCallingConvention GetSignatureConvention(CallingConventions callingConventions)
	{
		SignatureCallingConvention signatureCallingConvention = SignatureCallingConvention.Default;
		if ((callingConventions & CallingConventions.HasThis) != 0 || (callingConventions & CallingConventions.ExplicitThis) != 0)
		{
			signatureCallingConvention |= SignatureCallingConvention.ThisCall;
		}
		if ((callingConventions & CallingConventions.VarArgs) != 0)
		{
			signatureCallingConvention |= SignatureCallingConvention.VarArgs;
		}
		return signatureCallingConvention;
	}

	protected override GenericTypeParameterBuilder[] DefineGenericParametersCore(params string[] names)
	{
		if (_typeParameters != null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_GenericParametersAlreadySet);
		}
		GenericTypeParameterBuilderImpl[] array = new GenericTypeParameterBuilderImpl[names.Length];
		for (int i = 0; i < names.Length; i++)
		{
			string name = names[i];
			ArgumentNullException.ThrowIfNull(names, "names");
			array[i] = new GenericTypeParameterBuilderImpl(name, i, this);
		}
		return _typeParameters = array;
	}

	protected override ParameterBuilder DefineParameterCore(int position, ParameterAttributes attributes, string strParamName)
	{
		if (position > 0 && (_parameterTypes == null || position > _parameterTypes.Length))
		{
			throw new ArgumentOutOfRangeException(System.SR.ArgumentOutOfRange_ParamSequence);
		}
		if (_parameters == null)
		{
			_parameters = new ParameterBuilderImpl[1];
		}
		attributes &= ~ParameterAttributes.ReservedMask;
		ParameterBuilderImpl parameterBuilderImpl = new ParameterBuilderImpl(this, position, attributes, strParamName);
		_parameters[position] = parameterBuilderImpl;
		return parameterBuilderImpl;
	}

	protected override ILGenerator GetILGeneratorCore(int size)
	{
		throw new NotImplementedException();
	}

	protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
	{
		switch (con.ReflectedType.FullName)
		{
		case "System.Runtime.CompilerServices.MethodImplAttribute":
		{
			int num = BinaryPrimitives.ReadUInt16LittleEndian(binaryAttribute.Slice(2));
			_methodImplFlags |= (MethodImplAttributes)num;
			return;
		}
		case "System.Runtime.InteropServices.DllImportAttribute":
		{
			_dllImportData = DllImportData.CreateDllImportData(CustomAttributeInfo.DecodeCustomAttribute(con, binaryAttribute), out var preserveSig);
			_attributes |= MethodAttributes.PinvokeImpl;
			if (preserveSig)
			{
				_methodImplFlags |= MethodImplAttributes.PreserveSig;
			}
			return;
		}
		case "System.Runtime.InteropServices.PreserveSigAttribute":
			_methodImplFlags |= MethodImplAttributes.PreserveSig;
			return;
		case "System.Runtime.CompilerServices.SpecialNameAttribute":
			_attributes |= MethodAttributes.SpecialName;
			return;
		case "System.Security.SuppressUnmanagedCodeSecurityAttribute":
			_attributes |= MethodAttributes.HasSecurity;
			break;
		}
		if (_customAttributes == null)
		{
			_customAttributes = new List<CustomAttributeWrapper>();
		}
		_customAttributes.Add(new CustomAttributeWrapper(con, binaryAttribute));
	}

	protected override void SetImplementationFlagsCore(MethodImplAttributes attributes)
	{
		_methodImplFlags = attributes;
	}

	protected override void SetSignatureCore(Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
	{
		if (returnType != null)
		{
			_returnType = returnType;
		}
		if (parameterTypes != null)
		{
			_parameterTypes = new Type[parameterTypes.Length];
			_parameters = new ParameterBuilderImpl[parameterTypes.Length + 1];
			for (int i = 0; i < parameterTypes.Length; i++)
			{
				ArgumentNullException.ThrowIfNull(_parameterTypes[i] = parameterTypes[i], "parameterTypes");
			}
		}
	}

	public override MethodInfo GetBaseDefinition()
	{
		return this;
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		throw new NotSupportedException(System.SR.NotSupported_DynamicModule);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		throw new NotSupportedException(System.SR.NotSupported_DynamicModule);
	}

	public override Type[] GetGenericArguments()
	{
		Type[] typeParameters = _typeParameters;
		return typeParameters ?? Type.EmptyTypes;
	}

	public override MethodInfo GetGenericMethodDefinition()
	{
		if (IsGenericMethod)
		{
			return this;
		}
		throw new InvalidOperationException();
	}

	public override int GetHashCode()
	{
		throw new NotImplementedException();
	}

	public override MethodImplAttributes GetMethodImplementationFlags()
	{
		return _methodImplFlags;
	}

	public override ParameterInfo[] GetParameters()
	{
		throw new NotImplementedException();
	}

	public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		throw new NotSupportedException(System.SR.NotSupported_DynamicModule);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		throw new NotSupportedException(System.SR.NotSupported_DynamicModule);
	}

	[RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
	[RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
	public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
	{
		throw new NotImplementedException();
	}
}
