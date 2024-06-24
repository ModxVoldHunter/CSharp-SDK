using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace System.Reflection;

public abstract class MethodBase : MemberInfo
{
	[Flags]
	internal enum InvokerStrategy
	{
		HasBeenInvoked_ObjSpanArgs = 1,
		StrategyDetermined_ObjSpanArgs = 2,
		HasBeenInvoked_Obj4Args = 4,
		StrategyDetermined_Obj4Args = 8,
		HasBeenInvoked_RefArgs = 0x10,
		StrategyDetermined_RefArgs = 0x20
	}

	[Flags]
	internal enum InvokerArgFlags
	{
		IsValueType = 1,
		IsValueType_ByRef_Or_Pointer = 2,
		IsNullableOfT = 4
	}

	[InlineArray(4)]
	internal struct ArgumentData<T>
	{
		private T _arg0;

		[UnscopedRef]
		public Span<T> AsSpan(int length)
		{
			return new Span<T>(ref _arg0, length);
		}

		public void Set(int index, T value)
		{
			Unsafe.Add(ref _arg0, index) = value;
		}
	}

	internal ref struct StackAllocatedArguments
	{
		internal ArgumentData<object> _args;

		public StackAllocatedArguments(object obj1, object obj2, object obj3, object obj4)
		{
			_args = default(ArgumentData<object>);
			_args.Set(0, obj1);
			_args.Set(1, obj2);
			_args.Set(2, obj3);
			_args.Set(3, obj4);
		}
	}

	internal ref struct StackAllocatedArgumentsWithCopyBack
	{
		internal ArgumentData<object> _args;

		internal ArgumentData<bool> _shouldCopyBack;
	}

	[InlineArray(4)]
	internal ref struct StackAllocatedByRefs
	{
		internal ref byte _arg0;
	}

	public abstract MethodAttributes Attributes { get; }

	public virtual MethodImplAttributes MethodImplementationFlags => GetMethodImplementationFlags();

	public virtual CallingConventions CallingConvention => CallingConventions.Standard;

	public bool IsAbstract => (Attributes & MethodAttributes.Abstract) != 0;

	public bool IsConstructor
	{
		get
		{
			if (this is ConstructorInfo && !IsStatic)
			{
				return (Attributes & MethodAttributes.RTSpecialName) == MethodAttributes.RTSpecialName;
			}
			return false;
		}
	}

	public bool IsFinal => (Attributes & MethodAttributes.Final) != 0;

	public bool IsHideBySig => (Attributes & MethodAttributes.HideBySig) != 0;

	public bool IsSpecialName => (Attributes & MethodAttributes.SpecialName) != 0;

	public bool IsStatic => (Attributes & MethodAttributes.Static) != 0;

	public bool IsVirtual => (Attributes & MethodAttributes.Virtual) != 0;

	public bool IsAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly;

	public bool IsFamily => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family;

	public bool IsFamilyAndAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem;

	public bool IsFamilyOrAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem;

	public bool IsPrivate => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private;

	public bool IsPublic => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;

	public virtual bool IsConstructedGenericMethod
	{
		get
		{
			if (IsGenericMethod)
			{
				return !IsGenericMethodDefinition;
			}
			return false;
		}
	}

	public virtual bool IsGenericMethod => false;

	public virtual bool IsGenericMethodDefinition => false;

	public virtual bool ContainsGenericParameters => false;

	public abstract RuntimeMethodHandle MethodHandle { get; }

	public virtual bool IsSecurityCritical
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual bool IsSecuritySafeCritical
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual bool IsSecurityTransparent
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public static MethodBase? GetMethodFromHandle(RuntimeMethodHandle handle)
	{
		if (handle.IsNullHandle())
		{
			throw new ArgumentException(SR.Argument_InvalidHandle);
		}
		MethodBase methodBase = RuntimeType.GetMethodBase(handle.GetMethodInfo());
		Type type = methodBase?.DeclaringType;
		if (type != null && type.IsGenericType)
		{
			throw new ArgumentException(SR.Format(SR.Argument_MethodDeclaringTypeGeneric, methodBase, type.GetGenericTypeDefinition()));
		}
		return methodBase;
	}

	public static MethodBase? GetMethodFromHandle(RuntimeMethodHandle handle, RuntimeTypeHandle declaringType)
	{
		if (handle.IsNullHandle())
		{
			throw new ArgumentException(SR.Argument_InvalidHandle);
		}
		return RuntimeType.GetMethodBase(declaringType.GetRuntimeType(), handle.GetMethodInfo());
	}

	[RequiresUnreferencedCode("Metadata for the method might be incomplete or removed")]
	public static MethodBase? GetCurrentMethod()
	{
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
		return RuntimeMethodInfo.InternalGetCurrentMethod(ref stackMark);
	}

	private nint GetMethodDesc()
	{
		return MethodHandle.Value;
	}

	internal virtual ParameterInfo[] GetParametersNoCopy()
	{
		return GetParameters();
	}

	public abstract ParameterInfo[] GetParameters();

	public abstract MethodImplAttributes GetMethodImplementationFlags();

	[RequiresUnreferencedCode("Trimming may change method bodies. For example it can change some instructions, remove branches or local variables.")]
	public virtual MethodBody? GetMethodBody()
	{
		throw new InvalidOperationException();
	}

	public virtual Type[] GetGenericArguments()
	{
		throw new NotSupportedException(SR.NotSupported_SubclassOverride);
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public object? Invoke(object? obj, object?[]? parameters)
	{
		return Invoke(obj, BindingFlags.Default, null, parameters, null);
	}

	public abstract object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture);

	public override bool Equals(object? obj)
	{
		return base.Equals(obj);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(MethodBase? left, MethodBase? right)
	{
		if ((object)right == null)
		{
			return (object)left == null;
		}
		if ((object)left == right)
		{
			return true;
		}
		return left?.Equals(right) ?? false;
	}

	public static bool operator !=(MethodBase? left, MethodBase? right)
	{
		return !(left == right);
	}

	internal static void AppendParameters(ref ValueStringBuilder sbParamList, Type[] parameterTypes, CallingConventions callingConvention)
	{
		string s = "";
		foreach (Type type in parameterTypes)
		{
			sbParamList.Append(s);
			string text = type.FormatTypeName();
			if (type.IsByRef)
			{
				sbParamList.Append(text.AsSpan().TrimEnd('&'));
				sbParamList.Append(" ByRef");
			}
			else
			{
				sbParamList.Append(text);
			}
			s = ", ";
		}
		if ((callingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
		{
			sbParamList.Append(s);
			sbParamList.Append("...");
		}
	}

	internal virtual Type[] GetParameterTypes()
	{
		ParameterInfo[] parametersNoCopy = GetParametersNoCopy();
		if (parametersNoCopy.Length == 0)
		{
			return Type.EmptyTypes;
		}
		Type[] array = new Type[parametersNoCopy.Length];
		for (int i = 0; i < parametersNoCopy.Length; i++)
		{
			array[i] = parametersNoCopy[i].ParameterType;
		}
		return array;
	}

	internal static object HandleTypeMissing(ParameterInfo paramInfo, RuntimeType sigType)
	{
		if (paramInfo.DefaultValue == DBNull.Value)
		{
			throw new ArgumentException(SR.Arg_VarMissNull, "parameters");
		}
		object obj = paramInfo.DefaultValue;
		if (sigType.IsNullableOfT && obj != null)
		{
			Type type = sigType.GetGenericArguments()[0];
			if (type.IsEnum)
			{
				obj = Enum.ToObject(type, obj);
			}
		}
		return obj;
	}
}
