using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Emit;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection;

internal sealed class MethodBaseInvoker
{
	private readonly Signature _signature;

	private InvokerEmitUtil.InvokeFunc_ObjSpanArgs _invokeFunc_ObjSpanArgs;

	private InvokerEmitUtil.InvokeFunc_RefArgs _invokeFunc_RefArgs;

	private MethodBase.InvokerStrategy _strategy;

	internal readonly InvocationFlags _invocationFlags;

	private readonly MethodBase.InvokerArgFlags[] _invokerArgFlags;

	private readonly RuntimeType[] _argTypes;

	private readonly MethodBase _method;

	private readonly int _argCount;

	private readonly bool _needsByRefStrategy;

	internal unsafe MethodBaseInvoker(RuntimeMethodInfo method)
		: this(method, method.Signature.Arguments)
	{
		_signature = method.Signature;
		_invocationFlags = method.ComputeAndUpdateInvocationFlags();
		_invokeFunc_RefArgs = InterpretedInvoke_Method;
	}

	internal unsafe MethodBaseInvoker(RuntimeConstructorInfo constructor)
		: this(constructor, constructor.Signature.Arguments)
	{
		_signature = constructor.Signature;
		_invocationFlags = constructor.ComputeAndUpdateInvocationFlags();
		_invokeFunc_RefArgs = InterpretedInvoke_Constructor;
	}

	internal unsafe MethodBaseInvoker(DynamicMethod method, Signature signature)
		: this(method, signature.Arguments)
	{
		_signature = signature;
		_invokeFunc_RefArgs = InterpretedInvoke_Method;
	}

	private unsafe object InterpretedInvoke_Constructor(object obj, nint* args)
	{
		return RuntimeMethodHandle.InvokeMethod(obj, (void**)args, _signature, obj == null);
	}

	private unsafe object InterpretedInvoke_Method(object obj, nint* args)
	{
		return RuntimeMethodHandle.InvokeMethod(obj, (void**)args, _signature, isConstructor: false);
	}

	private MethodBaseInvoker(MethodBase method, RuntimeType[] argumentTypes)
	{
		_method = method;
		_argTypes = argumentTypes;
		_argCount = _argTypes.Length;
		MethodInvokerCommon.Initialize(argumentTypes, out _strategy, out _invokerArgFlags, out _needsByRefStrategy);
	}

	[DoesNotReturn]
	internal static void ThrowTargetParameterCountException()
	{
		throw new TargetParameterCountException(SR.Arg_ParmCnt);
	}

	internal unsafe object InvokeWithNoArgs(object obj, BindingFlags invokeAttr)
	{
		if ((_strategy & MethodBase.InvokerStrategy.StrategyDetermined_RefArgs) == 0)
		{
			MethodInvokerCommon.DetermineStrategy_RefArgs(ref _strategy, ref _invokeFunc_RefArgs, _method, backwardsCompat: true);
		}
		try
		{
			return _invokeFunc_RefArgs(obj, null);
		}
		catch (Exception inner) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
		{
			throw new TargetInvocationException(inner);
		}
	}

	internal object InvokeWithOneArg(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		object reference = parameters[0];
		ReadOnlySpan<object> parameters2 = new ReadOnlySpan<object>(ref reference);
		object reference2 = null;
		Span<object> span = new Span<object>(ref reference2);
		bool reference3 = false;
		Span<bool> shouldCopyBack = new Span<bool>(ref reference3);
		if ((_strategy & MethodBase.InvokerStrategy.StrategyDetermined_ObjSpanArgs) == 0)
		{
			MethodInvokerCommon.DetermineStrategy_ObjSpanArgs(ref _strategy, ref _invokeFunc_ObjSpanArgs, _method, _needsByRefStrategy, backwardsCompat: true);
		}
		CheckArguments(parameters2, span, shouldCopyBack, binder, culture, invokeAttr);
		object result;
		if (_invokeFunc_ObjSpanArgs != null)
		{
			try
			{
				result = _invokeFunc_ObjSpanArgs(obj, span);
			}
			catch (Exception inner) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
			{
				throw new TargetInvocationException(inner);
			}
		}
		else
		{
			result = InvokeDirectByRefWithFewArgs(obj, span, invokeAttr);
		}
		CopyBack(parameters, span, shouldCopyBack);
		return result;
	}

	internal object InvokeWithFewArgs(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		MethodBase.StackAllocatedArgumentsWithCopyBack stackAllocatedArgumentsWithCopyBack = default(MethodBase.StackAllocatedArgumentsWithCopyBack);
		Span<object> span = stackAllocatedArgumentsWithCopyBack._args.AsSpan(_argCount);
		Span<bool> shouldCopyBack = stackAllocatedArgumentsWithCopyBack._shouldCopyBack.AsSpan(_argCount);
		if ((_strategy & MethodBase.InvokerStrategy.StrategyDetermined_ObjSpanArgs) == 0)
		{
			MethodInvokerCommon.DetermineStrategy_ObjSpanArgs(ref _strategy, ref _invokeFunc_ObjSpanArgs, _method, _needsByRefStrategy, backwardsCompat: true);
		}
		CheckArguments(parameters, span, shouldCopyBack, binder, culture, invokeAttr);
		object result;
		if (_invokeFunc_ObjSpanArgs != null)
		{
			try
			{
				result = _invokeFunc_ObjSpanArgs(obj, span);
			}
			catch (Exception inner) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
			{
				throw new TargetInvocationException(inner);
			}
		}
		else
		{
			result = InvokeDirectByRefWithFewArgs(obj, span, invokeAttr);
		}
		CopyBack(parameters, span, shouldCopyBack);
		return result;
	}

	internal unsafe object InvokeDirectByRefWithFewArgs(object obj, Span<object> copyOfArgs, BindingFlags invokeAttr)
	{
		if ((_strategy & MethodBase.InvokerStrategy.StrategyDetermined_RefArgs) == 0)
		{
			MethodInvokerCommon.DetermineStrategy_RefArgs(ref _strategy, ref _invokeFunc_RefArgs, _method, backwardsCompat: true);
		}
		MethodBase.StackAllocatedByRefs stackAllocatedByRefs = default(MethodBase.StackAllocatedByRefs);
		nint* ptr = (nint*)(&stackAllocatedByRefs);
		for (int i = 0; i < _argCount; i++)
		{
			Unsafe.Write((byte*)ptr + (nint)i * (nint)8, ((_invokerArgFlags[i] & MethodBase.InvokerArgFlags.IsValueType) != 0) ? ByReference.Create(ref copyOfArgs[i].GetRawData()) : ByReference.Create(ref copyOfArgs[i]));
		}
		try
		{
			return _invokeFunc_RefArgs(obj, ptr);
		}
		catch (Exception inner) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
		{
			throw new TargetInvocationException(inner);
		}
	}

	internal unsafe object InvokeWithManyArgs(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		if ((_strategy & MethodBase.InvokerStrategy.StrategyDetermined_ObjSpanArgs) == 0)
		{
			MethodInvokerCommon.DetermineStrategy_ObjSpanArgs(ref _strategy, ref _invokeFunc_ObjSpanArgs, _method, _needsByRefStrategy, backwardsCompat: true);
		}
		GCFrameRegistration gCFrameRegistration;
		object result;
		if (_invokeFunc_ObjSpanArgs != null)
		{
			nint* ptr = (nint*)stackalloc byte[(int)checked(unchecked((nuint)(uint)(_argCount * 2)) * (nuint)8u)];
			NativeMemory.Clear(ptr, (nuint)((nint)_argCount * (nint)8 * 2));
			Span<object> span = new Span<object>(ref Unsafe.AsRef<object>(ptr), _argCount);
			gCFrameRegistration = new GCFrameRegistration((void**)ptr, (uint)_argCount, areByRefs: false);
			Span<bool> shouldCopyBack = new Span<bool>((byte*)ptr + (nint)_argCount * (nint)8, _argCount);
			try
			{
				GCFrameRegistration.RegisterForGCReporting(&gCFrameRegistration);
				CheckArguments(parameters, span, shouldCopyBack, binder, culture, invokeAttr);
				try
				{
					result = _invokeFunc_ObjSpanArgs(obj, span);
				}
				catch (Exception inner) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
				{
					throw new TargetInvocationException(inner);
				}
				CopyBack(parameters, span, shouldCopyBack);
			}
			finally
			{
				GCFrameRegistration.UnregisterForGCReporting(&gCFrameRegistration);
			}
		}
		else
		{
			if ((_strategy & MethodBase.InvokerStrategy.StrategyDetermined_RefArgs) == 0)
			{
				MethodInvokerCommon.DetermineStrategy_RefArgs(ref _strategy, ref _invokeFunc_RefArgs, _method, backwardsCompat: true);
			}
			nint* ptr2 = (nint*)stackalloc byte[(int)checked(unchecked((nuint)(uint)(3 * _argCount)) * (nuint)8u)];
			NativeMemory.Clear(ptr2, (nuint)(3 * _argCount) * (nuint)8u);
			Span<object> span = new Span<object>(ref Unsafe.AsRef<object>(ptr2), _argCount);
			gCFrameRegistration = new GCFrameRegistration((void**)ptr2, (uint)_argCount, areByRefs: false);
			nint* ptr3 = (nint*)((byte*)ptr2 + (nint)_argCount * (nint)8);
			GCFrameRegistration gCFrameRegistration2 = new GCFrameRegistration((void**)ptr3, (uint)_argCount);
			Span<bool> shouldCopyBack = new Span<bool>((byte*)ptr2 + (nint)(_argCount * 2) * (nint)8, _argCount);
			try
			{
				GCFrameRegistration.RegisterForGCReporting(&gCFrameRegistration);
				GCFrameRegistration.RegisterForGCReporting(&gCFrameRegistration2);
				CheckArguments(parameters, span, shouldCopyBack, binder, culture, invokeAttr);
				for (int i = 0; i < _argCount; i++)
				{
					Unsafe.Write((byte*)ptr3 + (nint)i * (nint)8, ((_invokerArgFlags[i] & MethodBase.InvokerArgFlags.IsValueType) != 0) ? ByReference.Create(ref Unsafe.AsRef<object>((byte*)ptr2 + (nint)i * (nint)8).GetRawData()) : ByReference.Create(ref Unsafe.AsRef<object>((byte*)ptr2 + (nint)i * (nint)8)));
				}
				try
				{
					result = _invokeFunc_RefArgs(obj, ptr3);
				}
				catch (Exception inner2) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
				{
					throw new TargetInvocationException(inner2);
				}
				CopyBack(parameters, span, shouldCopyBack);
			}
			finally
			{
				GCFrameRegistration.UnregisterForGCReporting(&gCFrameRegistration2);
				GCFrameRegistration.UnregisterForGCReporting(&gCFrameRegistration);
			}
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void InvokePropertySetter(object obj, BindingFlags invokeAttr, Binder binder, object parameter, CultureInfo culture)
	{
		object reference = null;
		Span<object> span = new Span<object>(ref reference, 1);
		bool reference2 = false;
		Span<bool> shouldCopyBack = new Span<bool>(ref reference2, 1);
		CheckArguments(new ReadOnlySpan<object>(ref parameter), span, shouldCopyBack, binder, culture, invokeAttr);
		if (_invokeFunc_ObjSpanArgs != null)
		{
			try
			{
				_invokeFunc_ObjSpanArgs(obj, span);
				return;
			}
			catch (Exception inner) when ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
			{
				throw new TargetInvocationException(inner);
			}
		}
		if ((_strategy & MethodBase.InvokerStrategy.StrategyDetermined_ObjSpanArgs) == 0)
		{
			MethodInvokerCommon.DetermineStrategy_ObjSpanArgs(ref _strategy, ref _invokeFunc_ObjSpanArgs, _method, _needsByRefStrategy, backwardsCompat: true);
		}
		InvokeDirectByRefWithFewArgs(obj, span, invokeAttr);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void CopyBack(object[] dest, Span<object> copyOfParameters, Span<bool> shouldCopyBack)
	{
		for (int i = 0; i < dest.Length; i++)
		{
			if (shouldCopyBack[i])
			{
				if ((_invokerArgFlags[i] & MethodBase.InvokerArgFlags.IsNullableOfT) != 0)
				{
					dest[i] = RuntimeMethodHandle.ReboxFromNullable(copyOfParameters[i]);
				}
				else
				{
					dest[i] = copyOfParameters[i];
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void CheckArguments(ReadOnlySpan<object> parameters, Span<object> copyOfParameters, Span<bool> shouldCopyBack, Binder binder, CultureInfo culture, BindingFlags invokeAttr)
	{
		for (int i = 0; i < parameters.Length; i++)
		{
			object arg = parameters[i];
			RuntimeType runtimeType = _argTypes[i];
			if (arg == Type.Missing)
			{
				arg = MethodBase.HandleTypeMissing(_method.GetParametersNoCopy()[i], runtimeType);
				shouldCopyBack[i] = true;
			}
			if (arg == null)
			{
				if ((_invokerArgFlags[i] & MethodBase.InvokerArgFlags.IsValueType_ByRef_Or_Pointer) != 0)
				{
					shouldCopyBack[i] = runtimeType.CheckValue(ref arg, binder, culture, invokeAttr);
				}
			}
			else if ((object)arg.GetType() != runtimeType)
			{
				if (TryByRefFastPath(runtimeType, ref arg))
				{
					shouldCopyBack[i] = true;
				}
				else
				{
					shouldCopyBack[i] = runtimeType.CheckValue(ref arg, binder, culture, invokeAttr);
				}
			}
			copyOfParameters[i] = arg;
		}
	}

	private static bool TryByRefFastPath(RuntimeType type, ref object arg)
	{
		if (RuntimeType.TryGetByRefElementType(type, out var elementType) && (object)elementType == arg.GetType())
		{
			if (elementType.IsValueType)
			{
				arg = RuntimeType.AllocateValueType(elementType, arg);
			}
			return true;
		}
		return false;
	}

	internal unsafe object InvokeConstructorWithoutAlloc(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		bool flag = (invokeAttr & BindingFlags.DoNotWrapExceptions) == 0;
		int argCount = _argCount;
		Span<bool> shouldCopyBack = stackalloc bool[argCount];
		nint* ptr = (nint*)stackalloc byte[(int)checked(unchecked((nuint)(uint)(2 * argCount)) * (nuint)8u)];
		NativeMemory.Clear(ptr, (nuint)(2 * argCount) * (nuint)8u);
		Span<object> copyOfParameters = new Span<object>(ref Unsafe.AsRef<object>(ptr), argCount);
		GCFrameRegistration gCFrameRegistration = new GCFrameRegistration((void**)ptr, (uint)argCount, areByRefs: false);
		nint* ptr2 = (nint*)((byte*)ptr + (nint)argCount * (nint)8);
		GCFrameRegistration gCFrameRegistration2 = new GCFrameRegistration((void**)ptr2, (uint)argCount);
		try
		{
			GCFrameRegistration.RegisterForGCReporting(&gCFrameRegistration);
			GCFrameRegistration.RegisterForGCReporting(&gCFrameRegistration2);
			CheckArguments(parameters, copyOfParameters, shouldCopyBack, binder, culture, invokeAttr);
			for (int i = 0; i < argCount; i++)
			{
				Unsafe.Write((byte*)ptr2 + (nint)i * (nint)8, ((_invokerArgFlags[i] & MethodBase.InvokerArgFlags.IsValueType) != 0) ? ByReference.Create(ref Unsafe.AsRef<object>((byte*)ptr + (nint)i * (nint)8).GetRawData()) : ByReference.Create(ref Unsafe.AsRef<object>((byte*)ptr + (nint)i * (nint)8)));
			}
			object result;
			try
			{
				result = InterpretedInvoke_Constructor(obj, ptr2);
			}
			catch (Exception inner) when (flag)
			{
				throw new TargetInvocationException(inner);
			}
			CopyBack(parameters, copyOfParameters, shouldCopyBack);
			return result;
		}
		finally
		{
			GCFrameRegistration.UnregisterForGCReporting(&gCFrameRegistration2);
			GCFrameRegistration.UnregisterForGCReporting(&gCFrameRegistration);
		}
	}

	internal unsafe object InvokeConstructorWithoutAlloc(object obj, bool wrapInTargetInvocationException)
	{
		try
		{
			return InterpretedInvoke_Constructor(obj, null);
		}
		catch (Exception inner) when (wrapInTargetInvocationException)
		{
			throw new TargetInvocationException(inner);
		}
	}
}
