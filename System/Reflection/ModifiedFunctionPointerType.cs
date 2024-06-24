using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Reflection;

internal sealed class ModifiedFunctionPointerType : ModifiedType
{
	private Type[] _parameterTypes;

	private Type _returnType;

	internal ModifiedFunctionPointerType(Type unmodifiedType, TypeSignature typeSignature)
		: base(unmodifiedType, typeSignature)
	{
	}

	public override Type GetFunctionPointerReturnType()
	{
		return _returnType ?? Initialize();
		Type Initialize()
		{
			Interlocked.CompareExchange(ref _returnType, GetTypeParameter(base.UnmodifiedType.GetFunctionPointerReturnType(), 0), null);
			return _returnType;
		}
	}

	public override Type[] GetFunctionPointerParameterTypes()
	{
		return (Type[])(_parameterTypes ?? Initialize()).Clone();
		Type[] Initialize()
		{
			Type[] functionPointerParameterTypes = base.UnmodifiedType.GetFunctionPointerParameterTypes();
			for (int i = 0; i < functionPointerParameterTypes.Length; i++)
			{
				functionPointerParameterTypes[i] = GetTypeParameter(functionPointerParameterTypes[i], i + 1);
			}
			Interlocked.CompareExchange(ref _parameterTypes, functionPointerParameterTypes, null);
			return _parameterTypes;
		}
	}

	public override Type[] GetFunctionPointerCallingConventions()
	{
		ArrayBuilder<Type> arrayBuilder = default(ArrayBuilder<Type>);
		switch (GetCallingConventionFromFunctionPointer())
		{
		case SignatureCallingConvention.Cdecl:
			arrayBuilder.Add(typeof(CallConvCdecl));
			break;
		case SignatureCallingConvention.StdCall:
			arrayBuilder.Add(typeof(CallConvStdcall));
			break;
		case SignatureCallingConvention.ThisCall:
			arrayBuilder.Add(typeof(CallConvThiscall));
			break;
		case SignatureCallingConvention.FastCall:
			arrayBuilder.Add(typeof(CallConvFastcall));
			break;
		case SignatureCallingConvention.Unmanaged:
		{
			Type[] optionalCustomModifiers = GetFunctionPointerReturnType().GetOptionalCustomModifiers();
			foreach (Type type in optionalCustomModifiers)
			{
				if (type.FullName.StartsWith("System.Runtime.CompilerServices.CallConv", StringComparison.Ordinal))
				{
					arrayBuilder.Add(type);
				}
			}
			break;
		}
		}
		if (arrayBuilder.Count != 0)
		{
			return arrayBuilder.ToArray();
		}
		return Type.EmptyTypes;
	}
}
