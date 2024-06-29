using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization.DataContracts;

internal sealed class GenericParameterDataContract : DataContract
{
	private sealed class GenericParameterDataContractCriticalHelper : DataContractCriticalHelper
	{
		private readonly int _parameterPosition;

		internal int ParameterPosition => _parameterPosition;

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal GenericParameterDataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
			: base(type)
		{
			SetDataContractName(DataContract.GetXmlName(type));
			_parameterPosition = type.GenericParameterPosition;
		}
	}

	private readonly GenericParameterDataContractCriticalHelper _helper;

	internal int ParameterPosition => _helper.ParameterPosition;

	public override bool IsBuiltInDataContract => true;

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal GenericParameterDataContract(Type type)
		: base(new GenericParameterDataContractCriticalHelper(type))
	{
		_helper = base.Helper as GenericParameterDataContractCriticalHelper;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override DataContract BindGenericParameters(DataContract[] paramContracts, Dictionary<DataContract, DataContract> boundContracts = null)
	{
		return paramContracts[ParameterPosition];
	}
}
