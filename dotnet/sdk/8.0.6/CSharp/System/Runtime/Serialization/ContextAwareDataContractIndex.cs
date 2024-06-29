using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Runtime.Serialization.DataContracts;

namespace System.Runtime.Serialization;

internal sealed class ContextAwareDataContractIndex
{
	private (DataContract strong, WeakReference<DataContract> weak)[] _contracts;

	private ConditionalWeakTable<Type, DataContract> _keepAlive;

	public int Length => _contracts.Length;

	public ContextAwareDataContractIndex(int size)
	{
		_contracts = new(DataContract, WeakReference<DataContract>)[size];
		_keepAlive = new ConditionalWeakTable<Type, DataContract>();
	}

	public DataContract GetItem(int index)
	{
		DataContract dataContract = _contracts[index].strong;
		if (dataContract == null)
		{
			WeakReference<DataContract> item = _contracts[index].weak;
			if (item == null || !item.TryGetTarget(out var target))
			{
				return null;
			}
			dataContract = target;
		}
		return dataContract;
	}

	public void SetItem(int index, DataContract dataContract)
	{
		AssemblyLoadContext loadContext = AssemblyLoadContext.GetLoadContext(dataContract.UnderlyingType.Assembly);
		if (loadContext == null || !loadContext.IsCollectible)
		{
			_contracts[index].strong = dataContract;
			return;
		}
		_contracts[index].weak = new WeakReference<DataContract>(dataContract);
		_keepAlive.Add(dataContract.UnderlyingType, dataContract);
	}

	public void Resize(int newSize)
	{
		Array.Resize(ref _contracts, newSize);
	}
}
