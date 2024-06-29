using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace System.Reflection;

internal sealed class RuntimeConstructorInfo : ConstructorInfo, IRuntimeMethodInfo
{
	private readonly RuntimeType m_declaringType;

	private readonly RuntimeType.RuntimeTypeCache m_reflectedTypeCache;

	private string m_toString;

	private ParameterInfo[] m_parameters;

	private object _empty1;

	private object _empty2;

	private object _empty3;

	private readonly nint m_handle;

	private readonly MethodAttributes m_methodAttributes;

	private readonly BindingFlags m_bindingFlags;

	private Signature m_signature;

	private MethodBaseInvoker m_invoker;

	internal InvocationFlags InvocationFlags
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return Invoker._invocationFlags;
		}
	}

	private MethodBaseInvoker Invoker
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (m_invoker == null)
			{
				m_invoker = new MethodBaseInvoker(this);
			}
			return m_invoker;
		}
	}

	RuntimeMethodHandleInternal IRuntimeMethodInfo.Value => new RuntimeMethodHandleInternal(m_handle);

	internal Signature Signature
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return m_signature ?? LazyCreateSignature();
			[MethodImpl(MethodImplOptions.NoInlining)]
			Signature LazyCreateSignature()
			{
				Signature signature = new Signature(this, m_declaringType);
				Volatile.Write(ref m_signature, signature);
				return signature;
			}
		}
	}

	private RuntimeType ReflectedTypeInternal => m_reflectedTypeCache.GetRuntimeType();

	internal BindingFlags BindingFlags => m_bindingFlags;

	public override string Name => RuntimeMethodHandle.GetName(this);

	public override MemberTypes MemberType => MemberTypes.Constructor;

	public override Type DeclaringType
	{
		get
		{
			if (!m_reflectedTypeCache.IsGlobal)
			{
				return m_declaringType;
			}
			return null;
		}
	}

	public override Type ReflectedType
	{
		get
		{
			if (!m_reflectedTypeCache.IsGlobal)
			{
				return ReflectedTypeInternal;
			}
			return null;
		}
	}

	public override int MetadataToken => RuntimeMethodHandle.GetMethodDef(this);

	public override Module Module => GetRuntimeModule();

	public override RuntimeMethodHandle MethodHandle => new RuntimeMethodHandle(this);

	public override MethodAttributes Attributes => m_methodAttributes;

	public override CallingConventions CallingConvention => Signature.CallingConvention;

	internal RuntimeType[] ArgumentTypes => Signature.Arguments;

	public override bool IsSecurityCritical => true;

	public override bool IsSecuritySafeCritical => false;

	public override bool IsSecurityTransparent => false;

	public override bool ContainsGenericParameters
	{
		get
		{
			if (DeclaringType != null)
			{
				return DeclaringType.ContainsGenericParameters;
			}
			return false;
		}
	}

	internal RuntimeConstructorInfo(RuntimeMethodHandleInternal handle, RuntimeType declaringType, RuntimeType.RuntimeTypeCache reflectedTypeCache, MethodAttributes methodAttributes, BindingFlags bindingFlags)
	{
		m_bindingFlags = bindingFlags;
		m_reflectedTypeCache = reflectedTypeCache;
		m_declaringType = declaringType;
		m_handle = handle.Value;
		m_methodAttributes = methodAttributes;
	}

	internal override bool CacheEquals(object o)
	{
		if (o is RuntimeConstructorInfo runtimeConstructorInfo && runtimeConstructorInfo.m_handle == m_handle)
		{
			return (object)m_declaringType == runtimeConstructorInfo.m_declaringType;
		}
		return false;
	}

	public override string ToString()
	{
		if (m_toString == null)
		{
			ValueStringBuilder sbParamList = new ValueStringBuilder(100);
			sbParamList.Append("Void ");
			sbParamList.Append(Name);
			sbParamList.Append('(');
			MethodBase.AppendParameters(ref sbParamList, GetParameterTypes(), CallingConvention);
			sbParamList.Append(')');
			m_toString = sbParamList.ToString();
		}
		return m_toString;
	}

	public override bool Equals(object obj)
	{
		if (this != obj)
		{
			if (MetadataUpdater.IsSupported)
			{
				return CacheEquals(obj);
			}
			return false;
		}
		return true;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(((IntPtr)m_handle).GetHashCode(), ((IntPtr)m_declaringType.GetUnderlyingNativeHandle()).GetHashCode());
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		ArgumentNullException.ThrowIfNull(attributeType, "attributeType");
		if (!(attributeType.UnderlyingSystemType is RuntimeType caType))
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.GetCustomAttributes(this, caType);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		ArgumentNullException.ThrowIfNull(attributeType, "attributeType");
		if (!(attributeType.UnderlyingSystemType is RuntimeType caType))
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.IsDefined(this, caType);
	}

	public override IList<CustomAttributeData> GetCustomAttributesData()
	{
		return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
	}

	public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
	{
		return HasSameMetadataDefinitionAsCore<RuntimeConstructorInfo>(other);
	}

	internal RuntimeType GetRuntimeType()
	{
		return m_declaringType;
	}

	internal RuntimeModule GetRuntimeModule()
	{
		return RuntimeTypeHandle.GetModule(m_declaringType);
	}

	internal override Type GetReturnType()
	{
		return Signature.ReturnType;
	}

	internal override ParameterInfo[] GetParametersNoCopy()
	{
		return m_parameters ?? (m_parameters = RuntimeParameterInfo.GetParameters(this, this, Signature));
	}

	public override ParameterInfo[] GetParameters()
	{
		ParameterInfo[] parametersNoCopy = GetParametersNoCopy();
		if (parametersNoCopy.Length == 0)
		{
			return parametersNoCopy;
		}
		ParameterInfo[] array = new ParameterInfo[parametersNoCopy.Length];
		Array.Copy(parametersNoCopy, array, parametersNoCopy.Length);
		return array;
	}

	public override MethodImplAttributes GetMethodImplementationFlags()
	{
		return RuntimeMethodHandle.GetImplAttributes(this);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2059:RunClassConstructor", Justification = "This ConstructorInfo instance represents the static constructor itself, so if this object was created, the static constructor exists.")]
	private void InvokeClassConstructor()
	{
		Type declaringType = DeclaringType;
		if (declaringType != null)
		{
			RuntimeHelpers.RunClassConstructor(declaringType.TypeHandle);
		}
		else
		{
			RuntimeHelpers.RunModuleConstructor(Module.ModuleHandle);
		}
	}

	[RequiresUnreferencedCode("Trimming may change method bodies. For example it can change some instructions, remove branches or local variables.")]
	public override MethodBody GetMethodBody()
	{
		RuntimeMethodBody methodBody = RuntimeMethodHandle.GetMethodBody(this, ReflectedTypeInternal);
		if (methodBody != null)
		{
			methodBody._methodBase = this;
		}
		return methodBody;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal InvocationFlags ComputeAndUpdateInvocationFlags()
	{
		InvocationFlags invocationFlags = InvocationFlags.IsConstructor;
		Type declaringType = DeclaringType;
		if (declaringType == typeof(void) || (declaringType != null && declaringType.ContainsGenericParameters) || (CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
		{
			invocationFlags |= InvocationFlags.NoInvoke;
		}
		else if (base.IsStatic)
		{
			invocationFlags |= InvocationFlags.RunClassConstructor | InvocationFlags.NoConstructorInvoke;
		}
		else if (declaringType != null && declaringType.IsAbstract)
		{
			invocationFlags |= InvocationFlags.NoConstructorInvoke;
		}
		else
		{
			if (declaringType != null && declaringType.IsByRefLike)
			{
				invocationFlags |= InvocationFlags.ContainsStackPointers;
			}
			if (typeof(Delegate).IsAssignableFrom(DeclaringType))
			{
				invocationFlags |= InvocationFlags.IsDelegateConstructor;
			}
		}
		return invocationFlags | InvocationFlags.Initialized;
	}

	internal static void CheckCanCreateInstance(Type declaringType, bool isVarArg)
	{
		ArgumentNullException.ThrowIfNull(declaringType, "declaringType");
		if (declaringType.IsInterface)
		{
			throw new MemberAccessException(SR.Format(SR.Acc_CreateInterfaceEx, declaringType));
		}
		if (declaringType.IsAbstract)
		{
			throw new MemberAccessException(SR.Format(SR.Acc_CreateAbstEx, declaringType));
		}
		if (declaringType.GetRootElementType() == typeof(ArgIterator))
		{
			throw new NotSupportedException();
		}
		if (isVarArg)
		{
			throw new NotSupportedException();
		}
		if (declaringType.ContainsGenericParameters)
		{
			throw new MemberAccessException(SR.Format(SR.Acc_CreateGenericEx, declaringType));
		}
		if (declaringType == typeof(void))
		{
			throw new MemberAccessException(SR.Access_Void);
		}
	}

	[DoesNotReturn]
	internal void ThrowNoInvokeException()
	{
		CheckCanCreateInstance(DeclaringType, (CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs);
		if ((Attributes & MethodAttributes.Static) == MethodAttributes.Static)
		{
			throw new MemberAccessException(SR.Acc_NotClassInit);
		}
		throw new TargetException();
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		if ((InvocationFlags & InvocationFlags.NoInvoke) != 0)
		{
			ThrowNoInvokeException();
		}
		if (!base.IsStatic)
		{
			MethodInvokerCommon.ValidateInvokeTarget(obj, this);
		}
		int num = ((parameters != null) ? parameters.Length : 0);
		if (ArgumentTypes.Length != num)
		{
			MethodBaseInvoker.ThrowTargetParameterCountException();
		}
		if ((InvocationFlags & InvocationFlags.RunClassConstructor) != 0)
		{
			InvokeClassConstructor();
			return null;
		}
		if (num != 0)
		{
			return Invoker.InvokeConstructorWithoutAlloc(obj, invokeAttr, binder, parameters, culture);
		}
		return Invoker.InvokeConstructorWithoutAlloc(obj, (invokeAttr & BindingFlags.DoNotWrapExceptions) == 0);
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		if ((InvocationFlags & (InvocationFlags.NoInvoke | InvocationFlags.NoConstructorInvoke | InvocationFlags.ContainsStackPointers)) != 0)
		{
			ThrowNoInvokeException();
		}
		int num = ((parameters != null) ? parameters.Length : 0);
		if (ArgumentTypes.Length != num)
		{
			MethodBaseInvoker.ThrowTargetParameterCountException();
		}
		switch (num)
		{
		case 0:
			return Invoker.InvokeWithNoArgs(null, invokeAttr);
		case 1:
			return Invoker.InvokeWithOneArg(null, invokeAttr, binder, parameters, culture);
		case 2:
		case 3:
		case 4:
			return Invoker.InvokeWithFewArgs(null, invokeAttr, binder, parameters, culture);
		default:
			return Invoker.InvokeWithManyArgs(null, invokeAttr, binder, parameters, culture);
		}
	}
}
