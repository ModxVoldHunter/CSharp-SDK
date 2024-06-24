using System.Diagnostics.CodeAnalysis;

namespace System.Threading;

public sealed class AsyncLocal<T> : IAsyncLocal
{
	private readonly Action<AsyncLocalValueChangedArgs<T>> _valueChangedHandler;

	public T Value
	{
		[return: MaybeNull]
		get
		{
			object localValue = ExecutionContext.GetLocalValue(this);
			if (typeof(T).IsValueType && localValue == null)
			{
				return default(T);
			}
			return (T)localValue;
		}
		set
		{
			ExecutionContext.SetLocalValue(this, value, _valueChangedHandler != null);
		}
	}

	public AsyncLocal()
	{
	}

	public AsyncLocal(Action<AsyncLocalValueChangedArgs<T>>? valueChangedHandler)
	{
		_valueChangedHandler = valueChangedHandler;
	}

	void IAsyncLocal.OnValueChanged(object previousValueObj, object currentValueObj, bool contextChanged)
	{
		T previousValue = ((previousValueObj == null) ? default(T) : ((T)previousValueObj));
		T currentValue = ((currentValueObj == null) ? default(T) : ((T)currentValueObj));
		_valueChangedHandler(new AsyncLocalValueChangedArgs<T>(previousValue, currentValue, contextChanged));
	}
}
