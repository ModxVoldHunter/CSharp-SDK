using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization.DataContracts;

internal interface IGenericNameProvider
{
	bool ParametersFromBuiltInNamespaces
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get;
	}

	int GetParameterCount();

	IList<int> GetNestedParameterCounts();

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	string GetParameterName(int paramIndex);

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	string GetNamespaces();

	string GetGenericTypeName();
}
