using System.Collections.Generic;

namespace System.ComponentModel.Design.Serialization;

public sealed class ContextStack
{
	private List<object> _contextStack;

	public object? Current
	{
		get
		{
			if (_contextStack != null && _contextStack.Count > 0)
			{
				return _contextStack[_contextStack.Count - 1];
			}
			return null;
		}
	}

	public object? this[int level]
	{
		get
		{
			ArgumentOutOfRangeException.ThrowIfNegative(level, "level");
			if (_contextStack != null && level < _contextStack.Count)
			{
				return _contextStack[_contextStack.Count - 1 - level];
			}
			return null;
		}
	}

	public object? this[Type type]
	{
		get
		{
			ArgumentNullException.ThrowIfNull(type, "type");
			if (_contextStack != null)
			{
				int num = _contextStack.Count;
				while (num > 0)
				{
					object obj = _contextStack[--num];
					if (type.IsInstanceOfType(obj))
					{
						return obj;
					}
				}
			}
			return null;
		}
	}

	public void Append(object context)
	{
		ArgumentNullException.ThrowIfNull(context, "context");
		if (_contextStack == null)
		{
			_contextStack = new List<object>();
		}
		_contextStack.Insert(0, context);
	}

	public object? Pop()
	{
		object result = null;
		if (_contextStack != null && _contextStack.Count > 0)
		{
			int index = _contextStack.Count - 1;
			result = _contextStack[index];
			_contextStack.RemoveAt(index);
		}
		return result;
	}

	public void Push(object context)
	{
		ArgumentNullException.ThrowIfNull(context, "context");
		if (_contextStack == null)
		{
			_contextStack = new List<object>();
		}
		_contextStack.Add(context);
	}
}
