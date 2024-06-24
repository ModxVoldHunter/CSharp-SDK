using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Emit;

internal static class MetadataSignatureHelper
{
	internal static BlobBuilder FieldSignatureEncoder(Type fieldType, ModuleBuilderImpl module)
	{
		BlobBuilder blobBuilder = new BlobBuilder();
		WriteSignatureForType(new BlobEncoder(blobBuilder).Field().Type(), fieldType, module);
		return blobBuilder;
	}

	internal static BlobBuilder ConstructorSignatureEncoder(ParameterInfo[] parameters, ModuleBuilderImpl module)
	{
		BlobBuilder blobBuilder = new BlobBuilder();
		new BlobEncoder(blobBuilder).MethodSignature(SignatureCallingConvention.Default, 0, isInstanceMethod: true).Parameters((parameters != null) ? parameters.Length : 0, out var returnType, out var parameters2);
		returnType.Void();
		if (parameters != null)
		{
			Type[] array = Array.ConvertAll(parameters, (ParameterInfo parameter) => parameter.ParameterType);
			Type[] array2 = array;
			foreach (Type type in array2)
			{
				WriteSignatureForType(parameters2.AddParameter().Type(), type, module);
			}
		}
		return blobBuilder;
	}

	internal static BlobBuilder MethodSignatureEncoder(ModuleBuilderImpl module, Type[] parameters, Type returnType, SignatureCallingConvention convention, int genParamCount, bool isInstance)
	{
		BlobBuilder blobBuilder = new BlobBuilder();
		new BlobEncoder(blobBuilder).MethodSignature(convention, genParamCount, isInstance).Parameters((parameters != null) ? parameters.Length : 0, out var returnType2, out var parameters2);
		if (returnType != null && returnType != module.GetTypeFromCoreAssembly(CoreTypeId.Void))
		{
			WriteSignatureForType(returnType2.Type(), returnType, module);
		}
		else
		{
			returnType2.Void();
		}
		if (parameters != null)
		{
			foreach (Type type in parameters)
			{
				WriteSignatureForType(parameters2.AddParameter().Type(), type, module);
			}
		}
		return blobBuilder;
	}

	private static void WriteSignatureForType(SignatureTypeEncoder signature, Type type, ModuleBuilderImpl module)
	{
		if (type.IsArray)
		{
			Type elementType = type.GetElementType();
			int arrayRank = type.GetArrayRank();
			if (arrayRank == 1)
			{
				WriteSignatureForType(signature.SZArray(), elementType, module);
				return;
			}
			signature.Array(out var elementType2, out var arrayShape);
			WriteSimpleSignature(elementType2, elementType, module);
			arrayShape.Shape(type.GetArrayRank(), ImmutableArray.Create<int>(), ImmutableArray.Create(new int[arrayRank]));
		}
		else if (type.IsPointer)
		{
			WriteSignatureForType(signature.Pointer(), type.GetElementType(), module);
		}
		else if (type.IsByRef)
		{
			signature.Builder.WriteByte(16);
			WriteSignatureForType(signature, type.GetElementType(), module);
		}
		else if (type.IsGenericType)
		{
			Type[] genericArguments = type.GetGenericArguments();
			GenericTypeArgumentsEncoder genericTypeArgumentsEncoder = signature.GenericInstantiation(module.GetTypeHandle(type.GetGenericTypeDefinition()), genericArguments.Length, type.IsValueType);
			Type[] array = genericArguments;
			foreach (Type type2 in array)
			{
				if (type2.IsGenericMethodParameter)
				{
					genericTypeArgumentsEncoder.AddArgument().GenericMethodTypeParameter(type2.GenericParameterPosition);
				}
				else if (type2.IsGenericParameter)
				{
					genericTypeArgumentsEncoder.AddArgument().GenericTypeParameter(type2.GenericParameterPosition);
				}
				else
				{
					WriteSignatureForType(genericTypeArgumentsEncoder.AddArgument(), type2, module);
				}
			}
		}
		else if (type.IsGenericMethodParameter)
		{
			signature.GenericMethodTypeParameter(type.GenericParameterPosition);
		}
		else if (type.IsGenericParameter)
		{
			signature.GenericTypeParameter(type.GenericParameterPosition);
		}
		else
		{
			WriteSimpleSignature(signature, type, module);
		}
	}

	private static void WriteSimpleSignature(SignatureTypeEncoder signature, Type type, ModuleBuilderImpl module)
	{
		switch (module.GetTypeIdFromCoreTypes(type))
		{
		case CoreTypeId.Void:
			signature.Builder.WriteByte(1);
			break;
		case CoreTypeId.Boolean:
			signature.Boolean();
			break;
		case CoreTypeId.Byte:
			signature.Byte();
			break;
		case CoreTypeId.SByte:
			signature.SByte();
			break;
		case CoreTypeId.Char:
			signature.Char();
			break;
		case CoreTypeId.Int16:
			signature.Int16();
			break;
		case CoreTypeId.UInt16:
			signature.UInt16();
			break;
		case CoreTypeId.Int32:
			signature.Int32();
			break;
		case CoreTypeId.UInt32:
			signature.UInt32();
			break;
		case CoreTypeId.Int64:
			signature.Int64();
			break;
		case CoreTypeId.UInt64:
			signature.UInt64();
			break;
		case CoreTypeId.Single:
			signature.Single();
			break;
		case CoreTypeId.Double:
			signature.Double();
			break;
		case CoreTypeId.IntPtr:
			signature.IntPtr();
			break;
		case CoreTypeId.UIntPtr:
			signature.UIntPtr();
			break;
		case CoreTypeId.Object:
			signature.Object();
			break;
		case CoreTypeId.String:
			signature.String();
			break;
		case CoreTypeId.TypedReference:
			signature.TypedReference();
			break;
		default:
		{
			EntityHandle typeHandle = module.GetTypeHandle(type);
			signature.Type(typeHandle, type.IsValueType);
			break;
		}
		}
	}
}
