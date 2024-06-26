namespace System.Runtime.Serialization.DataContracts;

internal sealed class DataContractPairKey
{
	private readonly object _object1;

	private readonly object _object2;

	internal DataContractPairKey(object object1, object object2)
	{
		_object1 = object1;
		_object2 = object2;
	}

	public override bool Equals(object other)
	{
		if (!(other is DataContractPairKey dataContractPairKey))
		{
			return false;
		}
		if (dataContractPairKey._object1 != _object1 || dataContractPairKey._object2 != _object2)
		{
			if (dataContractPairKey._object1 == _object2)
			{
				return dataContractPairKey._object2 == _object1;
			}
			return false;
		}
		return true;
	}

	public override int GetHashCode()
	{
		return _object1.GetHashCode() ^ _object2.GetHashCode();
	}
}
