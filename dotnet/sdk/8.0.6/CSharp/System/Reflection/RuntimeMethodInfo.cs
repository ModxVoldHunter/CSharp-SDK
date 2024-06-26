using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading;

namespace System.Reflection;

internal sealed class RuntimeMethodInfo : MethodInfo, IRuntimeMethodInfo
{
	private readonly nint m_handle;

	private readonly RuntimeType.RuntimeTypeCache m_reflectedTypeCache;

	private string m_name;

	private string m_toString;

	private ParameterInfo[] m_parameters;

	private ParameterInfo m_returnParameter;

	private readonly BindingFlags m_bindingFlags;

	private readonly MethodAttributes m_methodAttributes;

	private Signature m_signature;

	private readonly RuntimeType m_declaringType;

	private readonly object m_keepalive;

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

	private RuntimeType ReflectedTypeInternal => m_reflectedTypeCache.GetRuntimeType();

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

	internal BindingFlags BindingFlags => m_bindingFlags;

	internal sealed override int GenericParameterCount => RuntimeMethodHandle.GetGenericParameterCount(this);

	public override string Name => m_name ?? (m_name = RuntimeMethodHandle.GetName(this));

	public override Type DeclaringType
	{
		get
		{
			if (m_reflectedTypeCache.IsGlobal)
			{
				return null;
			}
			return m_declaringType;
		}
	}

	public override Type ReflectedType
	{
		get
		{
			if (m_reflectedTypeCache.IsGlobal)
			{
				return null;
			}
			return m_reflectedTypeCache.GetRuntimeType();
		}
	}

	public override MemberTypes MemberType => MemberTypes.Method;

	public override int MetadataToken => RuntimeMethodHandle.GetMethodDef(this);

	public override Module Module => GetRuntimeModule();

	public override bool IsSecurityCritical => true;

	public override bool IsSecuritySafeCritical => false;

	public override bool IsSecurityTransparent => false;

	public override RuntimeMethodHandle MethodHandle => new RuntimeMethodHandle(this);

	public override MethodAttributes Attributes => m_methodAttributes;

	public override CallingConventions CallingConvention => Signature.CallingConvention;

	internal RuntimeType[] ArgumentTypes => Signature.Arguments;

	public override Type ReturnType => Signature.ReturnType;

	public override ICustomAttributeProvider ReturnTypeCustomAttributes => ReturnParameter;

	public override ParameterInfo ReturnParameter => FetchReturnParameter();

	public override bool IsCollectible => RuntimeMethodHandle.GetIsCollectible(new RuntimeMethodHandleInternal(m_handle)) != Interop.BOOL.FALSE;

	public override bool IsGenericMethod => RuntimeMethodHandle.HasMethodInstantiation(this);

	public override bool IsGenericMethodDefinition => RuntimeMethodHandle.IsGenericMethodDefinition(this);

	public override bool ContainsGenericParameters
	{
		get
		{
			if (DeclaringType != null && DeclaringType.ContainsGenericParameters)
			{
				return true;
			}
			if (!IsGenericMethod)
			{
				return false;
			}
			Type[] genericArguments = GetGenericArguments();
			for (int i = 0; i < genericArguments.Length; i++)
			{
				if (genericArguments[i].ContainsGenericParameters)
				{
					return true;
				}
			}
			return false;
		}
	}

	internal RuntimeMethodInfo(RuntimeMethodHandleInternal handle, RuntimeType declaringType, RuntimeType.RuntimeTypeCache reflectedTypeCache, MethodAttributes methodAttributes, BindingFlags bindingFlags, object keepalive)
	{
		m_bindingFlags = bindingFlags;
		m_declaringType = declaringType;
		m_keepalive = keepalive;
		m_handle = handle.Value;
		m_reflectedTypeCache = reflectedTypeCache;
		m_methodAttributes = methodAttributes;
	}

	private ParameterInfo[] FetchNonReturnParameters()
	{
		return m_parameters ?? (m_parameters = RuntimeParameterInfo.GetParameters(this, this, Signature));
	}

	private ParameterInfo FetchReturnParameter()
	{
		return m_returnParameter ?? (m_returnParameter = RuntimeParameterInfo.GetReturnParameter(this, this, Signature));
	}

	internal override bool CacheEquals(object o)
	{
		if (o is RuntimeMethodInfo runtimeMethodInfo)
		{
			return runtimeMethodInfo.m_handle == m_handle;
		}
		return false;
	}

	internal RuntimeMethodInfo GetParentDefinition()
	{
		if (!base.IsVirtual || m_declaringType.IsInterface)
		{
			return null;
		}
		RuntimeType runtimeType = (RuntimeType)m_declaringType.BaseType;
		if (runtimeType == null)
		{
			return null;
		}
		int slot = RuntimeMethodHandle.GetSlot(this);
		if (RuntimeTypeHandle.GetNumVirtuals(runtimeType) <= slot)
		{
			return null;
		}
		return (RuntimeMethodInfo)RuntimeType.GetMethodBase(runtimeType, RuntimeTypeHandle.GetMethodAt(runtimeType, slot));
	}

	internal RuntimeType GetDeclaringTypeInternal()
	{
		return m_declaringType;
	}

	public override string ToString()
	{
		if (m_toString == null)
		{
			ValueStringBuilder sbParamList = new ValueStringBuilder(100);
			sbParamList.Append(ReturnType.FormatTypeName());
			sbParamList.Append(' ');
			sbParamList.Append(Name);
			if (IsGenericMethod)
			{
				sbParamList.Append(RuntimeMethodHandle.ConstructInstantiation(this, TypeNameFormatFlags.FormatBasic));
			}
			sbParamList.Append('(');
			MethodBase.AppendParameters(ref sbParamList, GetParameterTypes(), CallingConvention);
			sbParamList.Append(')');
			m_toString = sbParamList.ToString();
		}
		return m_toString;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(((IntPtr)m_handle).GetHashCode(), ((IntPtr)m_declaringType.GetUnderlyingNativeHandle()).GetHashCode());
	}

	public override bool Equals(object obj)
	{
		if (obj is RuntimeMethodInfo runtimeMethodInfo && m_handle == runtimeMethodInfo.m_handle && (object)m_declaringType == runtimeMethodInfo.m_declaringType)
		{
			return (object)m_reflectedTypeCache.GetRuntimeType() == runtimeMethodInfo.m_reflectedTypeCache.GetRuntimeType();
		}
		return false;
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType, inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		ArgumentNullException.ThrowIfNull(attributeType, "attributeType");
		if (!(attributeType.UnderlyingSystemType is RuntimeType caType))
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.GetCustomAttributes(this, caType, inherit);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		ArgumentNullException.ThrowIfNull(attributeType, "attributeType");
		if (!(attributeType.UnderlyingSystemType is RuntimeType caType))
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.IsDefined(this, caType, inherit);
	}

	public override IList<CustomAttributeData> GetCustomAttributesData()
	{
		return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
	}

	public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
	{
		return HasSameMetadataDefinitionAsCore<RuntimeMethodInfo>(other);
	}

	internal RuntimeType GetRuntimeType()
	{
		return m_declaringType;
	}

	internal RuntimeModule GetRuntimeModule()
	{
		return m_declaringType.GetRuntimeModule();
	}

	internal override ParameterInfo[] GetParametersNoCopy()
	{
		return FetchNonReturnParameters();
	}

	public override ParameterInfo[] GetParameters()
	{
		ParameterInfo[] array = FetchNonReturnParameters();
		if (array.Length == 0)
		{
			return array;
		}
		ParameterInfo[] array2 = new ParameterInfo[array.Length];
		Array.Copy(array, array2, array.Length);
		return array2;
	}

	public override MethodImplAttributes GetMethodImplementationFlags()
	{
		return RuntimeMethodHandle.GetImplAttributes(this);
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

	[DebuggerStepThrough]
	[DebuggerHidden]
	internal void InvokePropertySetter(object obj, BindingFlags invokeAttr, Binder binder, object parameter, CultureInfo culture)
	{
		if ((InvocationFlags & (InvocationFlags.NoInvoke | InvocationFlags.ContainsStackPointers)) != 0)
		{
			ThrowNoInvokeException();
		}
		if (!base.IsStatic)
		{
			MethodInvokerCommon.ValidateInvokeTarget(obj, this);
		}
		Signature signature = Signature;
		if (signature.Arguments.Length != 1)
		{
			throw new TargetParameterCountException(SR.Arg_ParmCnt);
		}
		Invoker.InvokePropertySetter(obj, invokeAttr, binder, parameter, culture);
	}

	public override MethodInfo GetBaseDefinition()
	{
		if (!base.IsVirtual || base.IsStatic || m_declaringType == null || m_declaringType.IsInterface)
		{
			return this;
		}
		int slot = RuntimeMethodHandle.GetSlot(this);
		RuntimeType runtimeType = (RuntimeType)DeclaringType;
		RuntimeType reflectedType = runtimeType;
		RuntimeMethodHandleInternal methodHandle = default(RuntimeMethodHandleInternal);
		do
		{
			int numVirtuals = RuntimeTypeHandle.GetNumVirtuals(runtimeType);
			if (numVirtuals <= slot)
			{
				break;
			}
			methodHandle = RuntimeTypeHandle.GetMethodAt(runtimeType, slot);
			reflectedType = runtimeType;
			runtimeType = (RuntimeType)runtimeType.BaseType;
		}
		while (runtimeType != null);
		return (MethodInfo)RuntimeType.GetMethodBase(reflectedType, methodHandle);
	}

	public override Delegate CreateDelegate(Type delegateType)
	{
		return CreateDelegateInternal(delegateType, null, (DelegateBindingFlags)68);
	}

	public override Delegate CreateDelegate(Type delegateType, object target)
	{
		return CreateDelegateInternal(delegateType, target, DelegateBindingFlags.RelaxedSignature);
	}

	private Delegate CreateDelegateInternal(Type delegateType, object firstArgument, DelegateBindingFlags bindingFlags)
	{
		ArgumentNullException.ThrowIfNull(delegateType, "delegateType");
		RuntimeType runtimeType = delegateType as RuntimeType;
		if (runtimeType == null)
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType, "delegateType");
		}
		if (!runtimeType.IsDelegate())
		{
			throw new ArgumentException(SR.Arg_MustBeDelegate, "delegateType");
		}
		Delegate @delegate = Delegate.CreateDelegateInternal(runtimeType, this, firstArgument, bindingFlags);
		if ((object)@delegate == null)
		{
			throw new ArgumentException(SR.Arg_DlgtTargMeth);
		}
		return @delegate;
	}

	[RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
	public override MethodInfo MakeGenericMethod(params Type[] methodInstantiation)
	{
		ArgumentNullException.ThrowIfNull(methodInstantiation, "methodInstantiation");
		RuntimeType[] array = new RuntimeType[methodInstantiation.Length];
		if (!IsGenericMethodDefinition)
		{
			throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericMethodDefinition, this));
		}
		for (int i = 0; i < methodInstantiation.Length; i++)
		{
			Type type = methodInstantiation[i];
			ArgumentNullException.ThrowIfNull(type);
			RuntimeType runtimeType = type as RuntimeType;
			if (runtimeType == null)
			{
				Type[] array2 = new Type[methodInstantiation.Length];
				for (int j = 0; j < methodInstantiation.Length; j++)
				{
					array2[j] = methodInstantiation[j];
				}
				methodInstantiation = array2;
				return MethodBuilderInstantiation.MakeGenericMethod(this, methodInstantiation);
			}
			array[i] = runtimeType;
		}
		RuntimeType[] genericArgumentsInternal = GetGenericArgumentsInternal();
		RuntimeType.SanityCheckGenericArguments(array, genericArgumentsInternal);
		try
		{
			return RuntimeType.GetMethodBase(ReflectedTypeInternal, RuntimeMethodHandle.GetStubIfNeeded(new RuntimeMethodHandleInternal(m_handle), m_declaringType, array)) as MethodInfo;
		}
		catch (VerificationException e)
		{
			RuntimeType.ValidateGenericArguments(this, array, e);
			throw;
		}
	}

	internal RuntimeType[] GetGenericArgumentsInternal()
	{
		return RuntimeMethodHandle.GetMethodInstantiationInternal(this);
	}

	public override Type[] GetGenericArguments()
	{
		return RuntimeMethodHandle.GetMethodInstantiationPublic(this) ?? Type.EmptyTypes;
	}

	public override MethodInfo GetGenericMethodDefinition()
	{
		if (!IsGenericMethod)
		{
			throw new InvalidOperationException();
		}
		return RuntimeType.GetMethodBase(m_declaringType, RuntimeMethodHandle.StripMethodInstantiation(this)) as MethodInfo;
	}

	internal static MethodBase InternalGetCurrentMethod(ref StackCrawlMark stackMark)
	{
		IRuntimeMethodInfo currentMethod = RuntimeMethodHandle.GetCurrentMethod(ref stackMark);
		if (currentMethod == null)
		{
			return null;
		}
		return RuntimeType.GetMethodBase(currentMethod);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal InvocationFlags ComputeAndUpdateInvocationFlags()
	{
		InvocationFlags invocationFlags = InvocationFlags.Unknown;
		Type declaringType = DeclaringType;
		if (ContainsGenericParameters || IsDisallowedByRefType(ReturnType) || (CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
		{
			invocationFlags = InvocationFlags.NoInvoke;
		}
		else
		{
			if (declaringType != null)
			{
				if (declaringType.ContainsGenericParameters)
				{
					invocationFlags = InvocationFlags.NoInvoke;
				}
				else if (declaringType.IsByRefLike)
				{
					invocationFlags |= InvocationFlags.ContainsStackPointers;
				}
			}
			if (ReturnType.IsByRefLike)
			{
				invocationFlags |= InvocationFlags.ContainsStackPointers;
			}
		}
		return invocationFlags | InvocationFlags.Initialized;
		static bool IsDisallowedByRefType(Type type)
		{
			if (!type.IsByRef)
			{
				return false;
			}
			Type elementType = type.GetElementType();
			if (!elementType.IsByRefLike)
			{
				return elementType == typeof(void);
			}
			return true;
		}
	}

	[DoesNotReturn]
	internal void ThrowNoInvokeException()
	{
		if ((InvocationFlags & InvocationFlags.ContainsStackPointers) != 0)
		{
			throw new NotSupportedException();
		}
		if ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
		{
			throw new NotSupportedException();
		}
		if (DeclaringType.ContainsGenericParameters || ContainsGenericParameters)
		{
			throw new InvalidOperationException(SR.Arg_UnboundGenParam);
		}
		if (base.IsAbstract)
		{
			throw new MemberAccessException();
		}
		if (ReturnType.IsByRef)
		{
			Type elementType = ReturnType.GetElementType();
			if (elementType.IsByRefLike)
			{
				throw new NotSupportedException(SR.NotSupported_ByRefToByRefLikeReturn);
			}
			if (elementType == typeof(void))
			{
				throw new NotSupportedException(SR.NotSupported_ByRefToVoidReturn);
			}
		}
		throw new TargetException();
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		if ((InvocationFlags & (InvocationFlags.NoInvoke | InvocationFlags.ContainsStackPointers)) != 0)
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
		switch (num)
		{
		case 0:
			return Invoker.InvokeWithNoArgs(obj, invokeAttr);
		case 1:
			return Invoker.InvokeWithOneArg(obj, invokeAttr, binder, parameters, culture);
		case 2:
		case 3:
		case 4:
			return Invoker.InvokeWithFewArgs(obj, invokeAttr, binder, parameters, culture);
		default:
			return Invoker.InvokeWithManyArgs(obj, invokeAttr, binder, parameters, culture);
		}
	}
}
