using System.Reflection;
using System.Threading;

namespace System.Runtime.InteropServices;

internal sealed class ComEventsMethod
{
	public sealed class DelegateWrapper
	{
		private bool _once;

		private int _expectedParamsCount;

		private Type[] _cachedTargetTypes;

		public Delegate Delegate { get; set; }

		public bool WrapArgs { get; }

		public DelegateWrapper(Delegate d, bool wrapArgs)
		{
			Delegate = d;
			WrapArgs = wrapArgs;
		}

		public object Invoke(object[] args)
		{
			if ((object)Delegate == null)
			{
				return null;
			}
			if (!_once)
			{
				PreProcessSignature();
				_once = true;
			}
			if (_cachedTargetTypes != null && _expectedParamsCount == args.Length)
			{
				for (int i = 0; i < _expectedParamsCount; i++)
				{
					Type type = _cachedTargetTypes[i];
					if ((object)type != null)
					{
						args[i] = Enum.ToObject(type, args[i]);
					}
				}
			}
			return Delegate.DynamicInvoke((!WrapArgs) ? args : new object[1] { args });
		}

		private void PreProcessSignature()
		{
			ParameterInfo[] parameters = Delegate.Method.GetParameters();
			_expectedParamsCount = parameters.Length;
			Type[] array = null;
			for (int i = 0; i < _expectedParamsCount; i++)
			{
				ParameterInfo parameterInfo = parameters[i];
				if (parameterInfo.ParameterType.IsByRef && parameterInfo.ParameterType.HasElementType && parameterInfo.ParameterType.GetElementType().IsEnum)
				{
					if (array == null)
					{
						array = new Type[_expectedParamsCount];
					}
					array[i] = parameterInfo.ParameterType.GetElementType();
				}
			}
			if (array != null)
			{
				_cachedTargetTypes = array;
			}
		}
	}

	private DelegateWrapper[] _delegateWrappers = Array.Empty<DelegateWrapper>();

	private readonly int _dispid;

	private ComEventsMethod _next;

	public bool Empty => _delegateWrappers.Length == 0;

	public ComEventsMethod(int dispid)
	{
		_dispid = dispid;
	}

	public static ComEventsMethod Find(ComEventsMethod methods, int dispid)
	{
		while (methods != null && methods._dispid != dispid)
		{
			methods = methods._next;
		}
		return methods;
	}

	public static ComEventsMethod Add(ComEventsMethod methods, ComEventsMethod method)
	{
		method._next = methods;
		return method;
	}

	public static ComEventsMethod Remove(ComEventsMethod methods, ComEventsMethod method)
	{
		if (methods == method)
		{
			return methods._next;
		}
		ComEventsMethod comEventsMethod = methods;
		while (comEventsMethod != null && comEventsMethod._next != method)
		{
			comEventsMethod = comEventsMethod._next;
		}
		if (comEventsMethod != null)
		{
			comEventsMethod._next = method._next;
		}
		return methods;
	}

	public void AddDelegate(Delegate d, bool wrapArgs = false)
	{
		DelegateWrapper[] delegateWrappers;
		DelegateWrapper[] array;
		do
		{
			delegateWrappers = _delegateWrappers;
			array = new DelegateWrapper[delegateWrappers.Length + 1];
			delegateWrappers.CopyTo(array, 0);
			array[^1] = new DelegateWrapper(d, wrapArgs);
		}
		while (!PublishNewWrappers(array, delegateWrappers));
	}

	public void RemoveDelegate(Delegate d, bool wrapArgs = false)
	{
		DelegateWrapper[] delegateWrappers;
		DelegateWrapper[] array;
		do
		{
			delegateWrappers = _delegateWrappers;
			int num = -1;
			for (int i = 0; i < delegateWrappers.Length; i++)
			{
				DelegateWrapper delegateWrapper = delegateWrappers[i];
				if (delegateWrapper.Delegate == d && delegateWrapper.WrapArgs == wrapArgs)
				{
					num = i;
					break;
				}
			}
			if (num < 0)
			{
				break;
			}
			array = new DelegateWrapper[delegateWrappers.Length - 1];
			delegateWrappers.AsSpan(0, num).CopyTo(array);
			delegateWrappers.AsSpan(num + 1).CopyTo(array.AsSpan(num));
		}
		while (!PublishNewWrappers(array, delegateWrappers));
	}

	public object Invoke(object[] args)
	{
		object result = null;
		DelegateWrapper[] delegateWrappers = _delegateWrappers;
		foreach (DelegateWrapper delegateWrapper in delegateWrappers)
		{
			result = delegateWrapper.Invoke(args);
		}
		return result;
	}

	private bool PublishNewWrappers(DelegateWrapper[] newWrappers, DelegateWrapper[] currentMaybe)
	{
		return Interlocked.CompareExchange(ref _delegateWrappers, newWrappers, currentMaybe) == currentMaybe;
	}
}
