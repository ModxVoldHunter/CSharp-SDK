using System.Collections;
using System.Collections.Generic;

namespace System.Text.RegularExpressions.Symbolic;

internal sealed class DoublyLinkedList<T> : IEnumerable<T>, IEnumerable
{
	private sealed class Node
	{
		public Node Next;

		public Node Prev;

		public readonly T Value;

		public Node(T elem)
		{
			Value = elem;
		}

		public Node(T elem, Node prev, Node next)
		{
			Value = elem;
			Prev = prev;
			Next = next;
		}
	}

	private Node _first;

	private Node _last;

	private int _size;

	public int Count => _size;

	internal T FirstElement => _first.Value;

	public void AddLast(DoublyLinkedList<T> other)
	{
		if (other._first == null)
		{
			other._size = -1;
		}
		else if (_first == null)
		{
			_first = other._first;
			_last = other._last;
			_size = other._size;
			other._size = -1;
		}
		else
		{
			_last.Next = other._first;
			other._first.Prev = _last;
			_last = other._last;
			_size += other._size;
			other._size = -1;
		}
	}

	public void AddLast(T elem)
	{
		if (_last == null)
		{
			_first = new Node(elem);
			_last = _first;
			_size = 1;
		}
		else
		{
			_last = (_last.Next = new Node(elem, _last, null));
			_size++;
		}
	}

	public void AddFirst(T elem)
	{
		if (_first == null)
		{
			_first = new Node(elem);
			_last = _first;
			_size = 1;
		}
		else
		{
			_first.Prev = new Node(elem, null, _first);
			_first = _first.Prev;
			_size++;
		}
	}

	public IEnumerator<T> GetEnumerator()
	{
		for (Node current = _last; current != null; current = current.Prev)
		{
			yield return current.Value;
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
