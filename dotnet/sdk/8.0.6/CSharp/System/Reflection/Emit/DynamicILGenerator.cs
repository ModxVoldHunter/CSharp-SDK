using System.Runtime.InteropServices;

namespace System.Reflection.Emit;

internal sealed class DynamicILGenerator : RuntimeILGenerator
{
	internal DynamicScope m_scope;

	private readonly int m_methodSigToken;

	internal DynamicILGenerator(DynamicMethod method, byte[] methodSignature, int size)
		: base(method, size)
	{
		m_scope = new DynamicScope();
		m_methodSigToken = m_scope.GetTokenFor(methodSignature);
	}

	internal void GetCallableMethod(RuntimeModule module, DynamicMethod dm)
	{
		dm._methodHandle = ModuleHandle.GetDynamicMethod(dm, module, m_methodBuilder.Name, (byte[])m_scope[m_methodSigToken], new DynamicResolver(this));
	}

	public override LocalBuilder DeclareLocal(Type localType, bool pinned)
	{
		ArgumentNullException.ThrowIfNull(localType, "localType");
		RuntimeType runtimeType = localType as RuntimeType;
		if (runtimeType == null)
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType);
		}
		LocalBuilder result = new LocalBuilder(m_localCount, localType, m_methodBuilder, pinned);
		m_localSignature.AddArgument(localType, pinned);
		m_localCount++;
		return result;
	}

	public override void Emit(OpCode opcode, MethodInfo meth)
	{
		ArgumentNullException.ThrowIfNull(meth, "meth");
		int num = 0;
		DynamicMethod dynamicMethod = meth as DynamicMethod;
		int value;
		if (dynamicMethod == null)
		{
			RuntimeMethodInfo runtimeMethodInfo = meth as RuntimeMethodInfo;
			if (runtimeMethodInfo == null)
			{
				throw new ArgumentException(SR.Argument_MustBeRuntimeMethodInfo, "meth");
			}
			RuntimeType runtimeType = runtimeMethodInfo.GetRuntimeType();
			value = ((!(runtimeType != null) || (!runtimeType.IsGenericType && !runtimeType.IsArray)) ? GetTokenFor(runtimeMethodInfo) : GetTokenFor(runtimeMethodInfo, runtimeType));
		}
		else
		{
			if (opcode.Equals(OpCodes.Ldtoken) || opcode.Equals(OpCodes.Ldftn) || opcode.Equals(OpCodes.Ldvirtftn))
			{
				throw new ArgumentException(SR.Argument_InvalidOpCodeOnDynamicMethod);
			}
			value = GetTokenFor(dynamicMethod);
		}
		EnsureCapacity(7);
		InternalEmit(opcode);
		if (opcode.StackBehaviourPush == StackBehaviour.Varpush && meth.ReturnType != typeof(void))
		{
			num++;
		}
		if (opcode.StackBehaviourPop == StackBehaviour.Varpop)
		{
			num -= meth.GetParametersNoCopy().Length;
		}
		if (!meth.IsStatic && !opcode.Equals(OpCodes.Newobj) && !opcode.Equals(OpCodes.Ldtoken) && !opcode.Equals(OpCodes.Ldftn))
		{
			num--;
		}
		UpdateStackSize(opcode, num);
		PutInteger4(value);
	}

	public override void Emit(OpCode opcode, ConstructorInfo con)
	{
		ArgumentNullException.ThrowIfNull(con, "con");
		RuntimeConstructorInfo runtimeConstructorInfo = con as RuntimeConstructorInfo;
		if (runtimeConstructorInfo == null)
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeMethodInfo, "con");
		}
		RuntimeType runtimeType = runtimeConstructorInfo.GetRuntimeType();
		int value = ((!(runtimeType != null) || (!runtimeType.IsGenericType && !runtimeType.IsArray)) ? GetTokenFor(runtimeConstructorInfo) : GetTokenFor(runtimeConstructorInfo, runtimeType));
		EnsureCapacity(7);
		InternalEmit(opcode);
		UpdateStackSize(opcode, 1);
		PutInteger4(value);
	}

	public override void Emit(OpCode opcode, Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		RuntimeType runtimeType = type as RuntimeType;
		if (runtimeType == null)
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType);
		}
		int tokenFor = GetTokenFor(runtimeType);
		EnsureCapacity(7);
		InternalEmit(opcode);
		PutInteger4(tokenFor);
	}

	public override void Emit(OpCode opcode, FieldInfo field)
	{
		ArgumentNullException.ThrowIfNull(field, "field");
		RuntimeFieldInfo runtimeFieldInfo = field as RuntimeFieldInfo;
		if (runtimeFieldInfo == null)
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeFieldInfo, "field");
		}
		int value = ((!(field.DeclaringType == null)) ? GetTokenFor(runtimeFieldInfo, runtimeFieldInfo.GetRuntimeType()) : GetTokenFor(runtimeFieldInfo));
		EnsureCapacity(7);
		InternalEmit(opcode);
		PutInteger4(value);
	}

	public override void Emit(OpCode opcode, string str)
	{
		ArgumentNullException.ThrowIfNull(str, "str");
		int tokenForString = GetTokenForString(str);
		EnsureCapacity(7);
		InternalEmit(opcode);
		PutInteger4(tokenForString);
	}

	public override void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[] optionalParameterTypes)
	{
		int num = 0;
		if (optionalParameterTypes != null && (callingConvention & CallingConventions.VarArgs) == 0)
		{
			throw new InvalidOperationException(SR.InvalidOperation_NotAVarArgCallingConvention);
		}
		SignatureHelper methodSigHelper = GetMethodSigHelper(callingConvention, returnType, parameterTypes, null, null, optionalParameterTypes);
		EnsureCapacity(7);
		Emit(OpCodes.Calli);
		if (returnType != typeof(void))
		{
			num++;
		}
		if (parameterTypes != null)
		{
			num -= parameterTypes.Length;
		}
		if (optionalParameterTypes != null)
		{
			num -= optionalParameterTypes.Length;
		}
		if ((callingConvention & CallingConventions.HasThis) == CallingConventions.HasThis)
		{
			num--;
		}
		num--;
		UpdateStackSize(OpCodes.Calli, num);
		int tokenForSig = GetTokenForSig(methodSigHelper.GetSignature(appendEndOfSig: true));
		PutInteger4(tokenForSig);
	}

	public override void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type returnType, Type[] parameterTypes)
	{
		int num = 0;
		int num2 = 0;
		if (parameterTypes != null)
		{
			num2 = parameterTypes.Length;
		}
		SignatureHelper methodSigHelper = GetMethodSigHelper(unmanagedCallConv, returnType, parameterTypes);
		if (returnType != typeof(void))
		{
			num++;
		}
		if (parameterTypes != null)
		{
			num -= num2;
		}
		num--;
		UpdateStackSize(OpCodes.Calli, num);
		EnsureCapacity(7);
		Emit(OpCodes.Calli);
		int tokenForSig = GetTokenForSig(methodSigHelper.GetSignature(appendEndOfSig: true));
		PutInteger4(tokenForSig);
	}

	public override void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[] optionalParameterTypes)
	{
		ArgumentNullException.ThrowIfNull(methodInfo, "methodInfo");
		if (!opcode.Equals(OpCodes.Call) && !opcode.Equals(OpCodes.Callvirt) && !opcode.Equals(OpCodes.Newobj))
		{
			throw new ArgumentException(SR.Argument_NotMethodCallOpcode, "opcode");
		}
		if (methodInfo.ContainsGenericParameters)
		{
			throw new ArgumentException(SR.Argument_GenericsInvalid, "methodInfo");
		}
		if (methodInfo.DeclaringType != null && methodInfo.DeclaringType.ContainsGenericParameters)
		{
			throw new ArgumentException(SR.Argument_GenericsInvalid, "methodInfo");
		}
		int num = 0;
		int memberRefToken = GetMemberRefToken(methodInfo, optionalParameterTypes);
		EnsureCapacity(7);
		InternalEmit(opcode);
		if (methodInfo.ReturnType != typeof(void))
		{
			num++;
		}
		num -= methodInfo.GetParameterTypes().Length;
		if (!(methodInfo is SymbolMethod) && !methodInfo.IsStatic && !opcode.Equals(OpCodes.Newobj))
		{
			num--;
		}
		if (optionalParameterTypes != null)
		{
			num -= optionalParameterTypes.Length;
		}
		UpdateStackSize(opcode, num);
		PutInteger4(memberRefToken);
	}

	public override void Emit(OpCode opcode, SignatureHelper signature)
	{
		ArgumentNullException.ThrowIfNull(signature, "signature");
		int num = 0;
		EnsureCapacity(7);
		InternalEmit(opcode);
		if (opcode.StackBehaviourPop == StackBehaviour.Varpop)
		{
			num -= signature.ArgumentCount;
			num--;
			UpdateStackSize(opcode, num);
		}
		int tokenForSig = GetTokenForSig(signature.GetSignature(appendEndOfSig: true));
		PutInteger4(tokenForSig);
	}

	public override void BeginExceptFilterBlock()
	{
		if (base.CurrExcStackCount == 0)
		{
			throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
		}
		__ExceptionInfo _ExceptionInfo = base.CurrExcStack[base.CurrExcStackCount - 1];
		Label endLabel = _ExceptionInfo.GetEndLabel();
		Emit(OpCodes.Leave, endLabel);
		UpdateStackSize(OpCodes.Nop, 1);
		_ExceptionInfo.MarkFilterAddr(ILOffset);
	}

	public override void BeginCatchBlock(Type exceptionType)
	{
		if (base.CurrExcStackCount == 0)
		{
			throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
		}
		__ExceptionInfo _ExceptionInfo = base.CurrExcStack[base.CurrExcStackCount - 1];
		RuntimeType runtimeType = exceptionType as RuntimeType;
		if (_ExceptionInfo.GetCurrentState() == 1)
		{
			if (exceptionType != null)
			{
				throw new ArgumentException(SR.Argument_ShouldNotSpecifyExceptionType);
			}
			Emit(OpCodes.Endfilter);
			_ExceptionInfo.MarkCatchAddr(ILOffset, null);
			return;
		}
		ArgumentNullException.ThrowIfNull(exceptionType, "exceptionType");
		if (runtimeType == null)
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType);
		}
		Label endLabel = _ExceptionInfo.GetEndLabel();
		Emit(OpCodes.Leave, endLabel);
		UpdateStackSize(OpCodes.Nop, 1);
		_ExceptionInfo.MarkCatchAddr(ILOffset, exceptionType);
		_ExceptionInfo.m_filterAddr[_ExceptionInfo.m_currentCatch - 1] = GetTokenFor(runtimeType);
	}

	public override void UsingNamespace(string ns)
	{
		throw new NotSupportedException(SR.InvalidOperation_NotAllowedInDynamicMethod);
	}

	public override void BeginScope()
	{
		throw new NotSupportedException(SR.InvalidOperation_NotAllowedInDynamicMethod);
	}

	public override void EndScope()
	{
		throw new NotSupportedException(SR.InvalidOperation_NotAllowedInDynamicMethod);
	}

	private int GetMemberRefToken(MethodInfo methodInfo, Type[] optionalParameterTypes)
	{
		if (optionalParameterTypes != null && (methodInfo.CallingConvention & CallingConventions.VarArgs) == 0)
		{
			throw new InvalidOperationException(SR.InvalidOperation_NotAVarArgCallingConvention);
		}
		RuntimeMethodInfo runtimeMethodInfo = methodInfo as RuntimeMethodInfo;
		DynamicMethod dynamicMethod = methodInfo as DynamicMethod;
		if (runtimeMethodInfo == null && dynamicMethod == null)
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeMethodInfo, "methodInfo");
		}
		ParameterInfo[] parametersNoCopy = methodInfo.GetParametersNoCopy();
		Type[] array;
		Type[][] array2;
		Type[][] array3;
		if (parametersNoCopy != null && parametersNoCopy.Length != 0)
		{
			array = new Type[parametersNoCopy.Length];
			array2 = new Type[array.Length][];
			array3 = new Type[array.Length][];
			for (int i = 0; i < parametersNoCopy.Length; i++)
			{
				array[i] = parametersNoCopy[i].ParameterType;
				array2[i] = parametersNoCopy[i].GetRequiredCustomModifiers();
				array3[i] = parametersNoCopy[i].GetOptionalCustomModifiers();
			}
		}
		else
		{
			array = null;
			array2 = null;
			array3 = null;
		}
		SignatureHelper methodSigHelper = GetMethodSigHelper(methodInfo.CallingConvention, RuntimeMethodBuilder.GetMethodBaseReturnType(methodInfo), array, array2, array3, optionalParameterTypes);
		if (runtimeMethodInfo != null)
		{
			return GetTokenForVarArgMethod(runtimeMethodInfo, methodSigHelper);
		}
		return GetTokenForVarArgMethod(dynamicMethod, methodSigHelper);
	}

	private SignatureHelper GetMethodSigHelper(CallingConvention unmanagedCallConv, Type returnType, Type[] parameterTypes)
	{
		SignatureHelper methodSigHelper = SignatureHelper.GetMethodSigHelper(null, unmanagedCallConv, returnType);
		AddParameters(methodSigHelper, parameterTypes, null, null);
		return methodSigHelper;
	}

	private SignatureHelper GetMethodSigHelper(CallingConventions call, Type returnType, Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers, Type[] optionalParameterTypes)
	{
		SignatureHelper methodSigHelper = SignatureHelper.GetMethodSigHelper(call, returnType);
		AddParameters(methodSigHelper, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
		if (optionalParameterTypes != null && optionalParameterTypes.Length != 0)
		{
			methodSigHelper.AddSentinel();
			methodSigHelper.AddArguments(optionalParameterTypes, null, null);
		}
		return methodSigHelper;
	}

	private void AddParameters(SignatureHelper sigHelp, Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers)
	{
		if (requiredCustomModifiers != null && (parameterTypes == null || requiredCustomModifiers.Length != parameterTypes.Length))
		{
			throw new ArgumentException(SR.Format(SR.Argument_MismatchedArrays, "requiredCustomModifiers", "parameterTypes"));
		}
		if (optionalCustomModifiers != null && (parameterTypes == null || optionalCustomModifiers.Length != parameterTypes.Length))
		{
			throw new ArgumentException(SR.Format(SR.Argument_MismatchedArrays, "optionalCustomModifiers", "parameterTypes"));
		}
		if (parameterTypes != null)
		{
			for (int i = 0; i < parameterTypes.Length; i++)
			{
				sigHelp.AddDynamicArgument(m_scope, parameterTypes[i], (requiredCustomModifiers != null) ? requiredCustomModifiers[i] : null, (optionalCustomModifiers != null) ? optionalCustomModifiers[i] : null);
			}
		}
	}

	internal override void RecordTokenFixup()
	{
	}

	private int GetTokenFor(RuntimeType rtType)
	{
		return m_scope.GetTokenFor(rtType.TypeHandle);
	}

	private int GetTokenFor(RuntimeFieldInfo runtimeField)
	{
		return m_scope.GetTokenFor(runtimeField.FieldHandle);
	}

	private int GetTokenFor(RuntimeFieldInfo runtimeField, RuntimeType rtType)
	{
		return m_scope.GetTokenFor(runtimeField.FieldHandle, rtType.TypeHandle);
	}

	private int GetTokenFor(RuntimeConstructorInfo rtMeth)
	{
		return m_scope.GetTokenFor(rtMeth.MethodHandle);
	}

	private int GetTokenFor(RuntimeConstructorInfo rtMeth, RuntimeType rtType)
	{
		return m_scope.GetTokenFor(rtMeth.MethodHandle, rtType.TypeHandle);
	}

	private int GetTokenFor(RuntimeMethodInfo rtMeth)
	{
		return m_scope.GetTokenFor(rtMeth.MethodHandle);
	}

	private int GetTokenFor(RuntimeMethodInfo rtMeth, RuntimeType rtType)
	{
		return m_scope.GetTokenFor(rtMeth.MethodHandle, rtType.TypeHandle);
	}

	private int GetTokenFor(DynamicMethod dm)
	{
		return m_scope.GetTokenFor(dm);
	}

	private int GetTokenForVarArgMethod(RuntimeMethodInfo rtMeth, SignatureHelper sig)
	{
		VarArgMethod varArgMethod = new VarArgMethod(rtMeth, sig);
		return m_scope.GetTokenFor(varArgMethod);
	}

	private int GetTokenForVarArgMethod(DynamicMethod dm, SignatureHelper sig)
	{
		VarArgMethod varArgMethod = new VarArgMethod(dm, sig);
		return m_scope.GetTokenFor(varArgMethod);
	}

	private int GetTokenForString(string s)
	{
		return m_scope.GetTokenFor(s);
	}

	private int GetTokenForSig(byte[] sig)
	{
		return m_scope.GetTokenFor(sig);
	}
}
