using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization.DataContracts;

namespace System.Runtime.Serialization;

internal static class DataContractSurrogateCaller
{
	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static Type GetDataContractType(ISerializationSurrogateProvider surrogateProvider, Type type)
	{
		if (DataContract.GetBuiltInDataContract(type) != null)
		{
			return type;
		}
		return surrogateProvider.GetSurrogateType(type) ?? type;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	[return: NotNullIfNotNull("obj")]
	internal static object GetObjectToSerialize(ISerializationSurrogateProvider surrogateProvider, object obj, Type objType, Type membertype)
	{
		if (obj == null)
		{
			return null;
		}
		if (DataContract.GetBuiltInDataContract(objType) != null)
		{
			return obj;
		}
		return surrogateProvider.GetObjectToSerialize(obj, membertype);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	[return: NotNullIfNotNull("obj")]
	internal static object GetDeserializedObject(ISerializationSurrogateProvider surrogateProvider, object obj, Type objType, Type memberType)
	{
		if (obj == null)
		{
			return null;
		}
		if (DataContract.GetBuiltInDataContract(objType) != null)
		{
			return obj;
		}
		return surrogateProvider.GetDeserializedObject(obj, memberType);
	}

	internal static object GetCustomDataToExport(ISerializationSurrogateProvider2 surrogateProvider, MemberInfo memberInfo, Type dataContractType)
	{
		return surrogateProvider.GetCustomDataToExport(memberInfo, dataContractType);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static object GetCustomDataToExport(ISerializationSurrogateProvider2 surrogateProvider, Type clrType, Type dataContractType)
	{
		if (DataContract.GetBuiltInDataContract(clrType) != null)
		{
			return null;
		}
		return surrogateProvider.GetCustomDataToExport(clrType, dataContractType);
	}

	internal static void GetKnownCustomDataTypes(ISerializationSurrogateProvider2 surrogateProvider, Collection<Type> customDataTypes)
	{
		surrogateProvider.GetKnownCustomDataTypes(customDataTypes);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static Type GetReferencedTypeOnImport(ISerializationSurrogateProvider2 surrogateProvider, string typeName, string typeNamespace, object customData)
	{
		if (DataContract.GetBuiltInDataContract(typeName, typeNamespace) != null)
		{
			return null;
		}
		return surrogateProvider.GetReferencedTypeOnImport(typeName, typeNamespace, customData);
	}
}
