using System.Collections;
using System.Collections.Generic;

namespace System.Security.Cryptography;

public sealed class AsnEncodedDataCollection : ICollection, IEnumerable
{
	private readonly List<AsnEncodedData> _list;

	public AsnEncodedData this[int index] => _list[index];

	public int Count => _list.Count;

	public bool IsSynchronized => false;

	public object SyncRoot => this;

	public AsnEncodedDataCollection()
	{
		_list = new List<AsnEncodedData>();
	}

	public AsnEncodedDataCollection(AsnEncodedData asnEncodedData)
		: this()
	{
		_list.Add(asnEncodedData);
	}

	public int Add(AsnEncodedData asnEncodedData)
	{
		ArgumentNullException.ThrowIfNull(asnEncodedData, "asnEncodedData");
		int count = _list.Count;
		_list.Add(asnEncodedData);
		return count;
	}

	public void Remove(AsnEncodedData asnEncodedData)
	{
		ArgumentNullException.ThrowIfNull(asnEncodedData, "asnEncodedData");
		_list.Remove(asnEncodedData);
	}

	public AsnEncodedDataEnumerator GetEnumerator()
	{
		return new AsnEncodedDataEnumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	void ICollection.CopyTo(Array array, int index)
	{
		ArgumentNullException.ThrowIfNull(array, "array");
		if (array.Rank != 1)
		{
			throw new ArgumentException(System.SR.Arg_RankMultiDimNotSupported);
		}
		if (index < 0 || index >= array.Length)
		{
			throw new ArgumentOutOfRangeException("index", System.SR.ArgumentOutOfRange_IndexMustBeLess);
		}
		if (Count > array.Length - index)
		{
			throw new ArgumentException(System.SR.Argument_InvalidOffLen);
		}
		for (int i = 0; i < Count; i++)
		{
			array.SetValue(this[i], index);
			index++;
		}
	}

	public void CopyTo(AsnEncodedData[] array, int index)
	{
		ArgumentNullException.ThrowIfNull(array, "array");
		if (index < 0 || index >= array.Length)
		{
			throw new ArgumentOutOfRangeException("index", System.SR.ArgumentOutOfRange_IndexMustBeLess);
		}
		_list.CopyTo(array, index);
	}
}
