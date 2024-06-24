using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Linq;

internal sealed class OrderedImplicitlyStableEnumerable<TElement> : OrderedEnumerable<TElement>
{
	private readonly bool _descending;

	public override TElement[] ToArray()
	{
		TElement[] array = _source.ToArray();
		Sort(array, _descending);
		return array;
	}

	public override List<TElement> ToList()
	{
		List<TElement> list = _source.ToList();
		Sort(CollectionsMarshal.AsSpan(list), _descending);
		return list;
	}

	public OrderedImplicitlyStableEnumerable(IEnumerable<TElement> source, bool descending)
		: base(source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		_descending = descending;
	}

	internal override CachingComparer<TElement> GetComparer(CachingComparer<TElement> childComparer)
	{
		if (childComparer != null)
		{
			return new CachingComparerWithChild<TElement, TElement>(EnumerableSorter<TElement>.IdentityFunc, Comparer<TElement>.Default, _descending, childComparer);
		}
		return new CachingComparer<TElement, TElement>(EnumerableSorter<TElement>.IdentityFunc, Comparer<TElement>.Default, _descending);
	}

	internal override EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement> next)
	{
		return new EnumerableSorter<TElement, TElement>(EnumerableSorter<TElement>.IdentityFunc, Comparer<TElement>.Default, _descending, next);
	}

	public override IEnumerator<TElement> GetEnumerator()
	{
		Buffer<TElement> buffer = new Buffer<TElement>(_source);
		if (buffer._count > 0)
		{
			Sort(buffer._items.AsSpan(0, buffer._count), _descending);
			for (int i = 0; i < buffer._count; i++)
			{
				yield return buffer._items[i];
			}
		}
	}

	private static void Sort(Span<TElement> span, bool descending)
	{
		if (descending)
		{
			span.Sort((TElement a, TElement b) => Comparer<TElement>.Default.Compare(b, a));
		}
		else
		{
			span.Sort();
		}
	}
}
