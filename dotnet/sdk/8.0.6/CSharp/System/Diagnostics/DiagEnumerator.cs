using System.Collections;
using System.Collections.Generic;

namespace System.Diagnostics;

internal struct DiagEnumerator<T> : IEnumerator<T>, IEnumerator, IDisposable
{
	private static readonly DiagNode<T> s_Empty = new DiagNode<T>(default(T));

	private DiagNode<T> _nextNode;

	private DiagNode<T> _currentNode;

	public T Current => _currentNode.Value;

	object IEnumerator.Current => Current;

	public DiagEnumerator(DiagNode<T> head)
	{
		_nextNode = head;
		_currentNode = s_Empty;
	}

	public bool MoveNext()
	{
		if (_nextNode == null)
		{
			_currentNode = s_Empty;
			return false;
		}
		_currentNode = _nextNode;
		_nextNode = _nextNode.Next;
		return true;
	}

	public void Reset()
	{
		throw new NotSupportedException();
	}

	public void Dispose()
	{
	}
}
