namespace System.Collections;

public abstract class ReadOnlyCollectionBase : ICollection, IEnumerable
{
	private ArrayList _list;

	protected ArrayList InnerList => _list ?? (_list = new ArrayList());

	public virtual int Count => InnerList.Count;

	bool ICollection.IsSynchronized => InnerList.IsSynchronized;

	object ICollection.SyncRoot => InnerList.SyncRoot;

	void ICollection.CopyTo(Array array, int index)
	{
		InnerList.CopyTo(array, index);
	}

	public virtual IEnumerator GetEnumerator()
	{
		return InnerList.GetEnumerator();
	}
}
