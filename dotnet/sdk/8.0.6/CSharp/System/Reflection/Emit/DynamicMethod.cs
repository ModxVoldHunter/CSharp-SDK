using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading;

namespace System.Reflection.Emit;

public sealed class DynamicMethod : MethodInfo
{
	private RuntimeType[] _parameterTypes;

	internal IRuntimeMethodInfo _methodHandle;

	private RuntimeType _returnType;

	private DynamicILGenerator _ilGenerator;

	private DynamicILInfo _dynamicILInfo;

	private bool _initLocals;

	private Module _module;

	internal bool _skipVisibility;

	internal RuntimeType _typeOwner;

	private MethodBaseInvoker _invoker;

	private Signature _signature;

	private string _name;

	private MethodAttributes _attributes;

	private CallingConventions _callingConvention;

	private RuntimeParameterInfo[] _parameters;

	internal DynamicResolver _resolver;

	internal bool _restrictedSkipVisibility;

	private static volatile Module s_anonymouslyHostedDynamicMethodsModule;

	private static readonly object s_anonymouslyHostedDynamicMethodsModuleLock = new object();

	private MethodBaseInvoker Invoker
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return _invoker ?? (_invoker = new MethodBaseInvoker(this, Signature));
		}
	}

	internal Signature Signature
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return _signature ?? LazyCreateSignature();
			[MethodImpl(MethodImplOptions.NoInlining)]
			Signature LazyCreateSignature()
			{
				Signature signature = new Signature(_methodHandle, _parameterTypes, _returnType, CallingConvention);
				Volatile.Write(ref _signature, signature);
				return signature;
			}
		}
	}

	public override string Name => _name;

	public override Type? DeclaringType => null;

	public override Type? ReflectedType => null;

	public override Module Module => _module;

	public override RuntimeMethodHandle MethodHandle
	{
		get
		{
			throw new InvalidOperationException(SR.InvalidOperation_NotAllowedInDynamicMethod);
		}
	}

	public override MethodAttributes Attributes => _attributes;

	public override CallingConventions CallingConvention => _callingConvention;

	public override bool IsSecurityCritical => true;

	public override bool IsSecuritySafeCritical => false;

	public override bool IsSecurityTransparent => false;

	public override Type ReturnType => _returnType;

	public override ParameterInfo ReturnParameter => new RuntimeParameterInfo(this, null, _returnType, -1);

	public override ICustomAttributeProvider ReturnTypeCustomAttributes => new EmptyCAHolder();

	public bool InitLocals
	{
		get
		{
			return _initLocals;
		}
		set
		{
			_initLocals = value;
		}
	}

	public sealed override Delegate CreateDelegate(Type delegateType)
	{
		return CreateDelegate(delegateType, null);
	}

	public sealed override Delegate CreateDelegate(Type delegateType, object? target)
	{
		if (_restrictedSkipVisibility)
		{
			GetMethodDescriptor();
			IRuntimeMethodInfo methodHandle = _methodHandle;
			RuntimeHelpers.CompileMethod(methodHandle?.Value ?? RuntimeMethodHandleInternal.EmptyHandle);
			GC.KeepAlive(methodHandle);
		}
		MulticastDelegate multicastDelegate = (MulticastDelegate)Delegate.CreateDelegateNoSecurityCheck(delegateType, target, GetMethodDescriptor());
		multicastDelegate.StoreDynamicMethod(this);
		return multicastDelegate;
	}

	internal RuntimeMethodHandle GetMethodDescriptor()
	{
		if (_methodHandle == null)
		{
			lock (this)
			{
				if (_methodHandle == null)
				{
					if (_dynamicILInfo != null)
					{
						_dynamicILInfo.GetCallableMethod((RuntimeModule)_module, this);
					}
					else
					{
						if (_ilGenerator == null || _ilGenerator.ILOffset == 0)
						{
							throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadEmptyMethodBody, Name));
						}
						_ilGenerator.GetCallableMethod((RuntimeModule)_module, this);
					}
				}
			}
		}
		return new RuntimeMethodHandle(_methodHandle);
	}

	public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
	{
		if ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
		{
			throw new NotSupportedException(SR.NotSupported_CallToVarArg);
		}
		GetMethodDescriptor();
		int num = ((parameters != null) ? parameters.Length : 0);
		if (Signature.Arguments.Length != num)
		{
			throw new TargetParameterCountException(SR.Arg_ParmCnt);
		}
		object result;
		switch (num)
		{
		case 0:
			result = Invoker.InvokeWithNoArgs(obj, invokeAttr);
			break;
		case 1:
			result = Invoker.InvokeWithOneArg(obj, invokeAttr, binder, parameters, culture);
			break;
		case 2:
		case 3:
		case 4:
			result = Invoker.InvokeWithFewArgs(obj, invokeAttr, binder, parameters, culture);
			break;
		default:
			result = Invoker.InvokeWithManyArgs(obj, invokeAttr, binder, parameters, culture);
			break;
		}
		GC.KeepAlive(this);
		return result;
	}

	public DynamicILInfo GetDynamicILInfo()
	{
		if (_dynamicILInfo == null)
		{
			CallingConventions callingConvention = CallingConvention;
			Type returnType = ReturnType;
			Type[] parameterTypes = _parameterTypes;
			byte[] signature = SignatureHelper.GetMethodSigHelper(null, callingConvention, returnType, null, null, parameterTypes, null, null).GetSignature(appendEndOfSig: true);
			_dynamicILInfo = new DynamicILInfo(this, signature);
		}
		return _dynamicILInfo;
	}

	public ILGenerator GetILGenerator(int streamSize)
	{
		if (_ilGenerator == null)
		{
			CallingConventions callingConvention = CallingConvention;
			Type returnType = ReturnType;
			Type[] parameterTypes = _parameterTypes;
			byte[] signature = SignatureHelper.GetMethodSigHelper(null, callingConvention, returnType, null, null, parameterTypes, null, null).GetSignature(appendEndOfSig: true);
			_ilGenerator = new DynamicILGenerator(this, signature, streamSize);
		}
		return _ilGenerator;
	}

	[RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
	public DynamicMethod(string name, Type? returnType, Type[]? parameterTypes)
	{
		Init(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, null, null, skipVisibility: false, transparentMethod: true);
	}

	[RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
	public DynamicMethod(string name, Type? returnType, Type[]? parameterTypes, bool restrictedSkipVisibility)
	{
		Init(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, null, null, restrictedSkipVisibility, transparentMethod: true);
	}

	[RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
	public DynamicMethod(string name, Type? returnType, Type[]? parameterTypes, Module m)
	{
		ArgumentNullException.ThrowIfNull(m, "m");
		Init(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, null, m, skipVisibility: false, transparentMethod: false);
	}

	[RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
	public DynamicMethod(string name, Type? returnType, Type[]? parameterTypes, Module m, bool skipVisibility)
	{
		ArgumentNullException.ThrowIfNull(m, "m");
		Init(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, null, m, skipVisibility, transparentMethod: false);
	}

	[RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
	public DynamicMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, Module m, bool skipVisibility)
	{
		ArgumentNullException.ThrowIfNull(m, "m");
		Init(name, attributes, callingConvention, returnType, parameterTypes, null, m, skipVisibility, transparentMethod: false);
	}

	[RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
	public DynamicMethod(string name, Type? returnType, Type[]? parameterTypes, Type owner)
	{
		ArgumentNullException.ThrowIfNull(owner, "owner");
		Init(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, owner, null, skipVisibility: false, transparentMethod: false);
	}

	[RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
	public DynamicMethod(string name, Type? returnType, Type[]? parameterTypes, Type owner, bool skipVisibility)
	{
		ArgumentNullException.ThrowIfNull(owner, "owner");
		Init(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, owner, null, skipVisibility, transparentMethod: false);
	}

	[RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
	public DynamicMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, Type owner, bool skipVisibility)
	{
		ArgumentNullException.ThrowIfNull(owner, "owner");
		Init(name, attributes, callingConvention, returnType, parameterTypes, owner, null, skipVisibility, transparentMethod: false);
	}

	private static Module GetDynamicMethodsModule()
	{
		if (s_anonymouslyHostedDynamicMethodsModule != null)
		{
			return s_anonymouslyHostedDynamicMethodsModule;
		}
		AssemblyBuilder.EnsureDynamicCodeSupported();
		lock (s_anonymouslyHostedDynamicMethodsModuleLock)
		{
			if (s_anonymouslyHostedDynamicMethodsModule != null)
			{
				return s_anonymouslyHostedDynamicMethodsModule;
			}
			AssemblyName name = new AssemblyName("Anonymously Hosted DynamicMethods Assembly");
			RuntimeAssemblyBuilder runtimeAssemblyBuilder = RuntimeAssemblyBuilder.InternalDefineDynamicAssembly(name, AssemblyBuilderAccess.Run, AssemblyLoadContext.Default, null);
			s_anonymouslyHostedDynamicMethodsModule = runtimeAssemblyBuilder.ManifestModule;
		}
		return s_anonymouslyHostedDynamicMethodsModule;
	}

	[MemberNotNull("_parameterTypes")]
	[MemberNotNull("_returnType")]
	[MemberNotNull("_module")]
	[MemberNotNull("_name")]
	private void Init(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] signature, Type owner, Module m, bool skipVisibility, bool transparentMethod)
	{
		ArgumentNullException.ThrowIfNull(name, "name");
		AssemblyBuilder.EnsureDynamicCodeSupported();
		if (attributes != (MethodAttributes.Public | MethodAttributes.Static) || callingConvention != CallingConventions.Standard)
		{
			throw new NotSupportedException(SR.NotSupported_DynamicMethodFlags);
		}
		if (signature != null)
		{
			_parameterTypes = new RuntimeType[signature.Length];
			for (int i = 0; i < signature.Length; i++)
			{
				if (signature[i] == null)
				{
					throw new ArgumentException(SR.Arg_InvalidTypeInSignature);
				}
				_parameterTypes[i] = signature[i].UnderlyingSystemType as RuntimeType;
				if (_parameterTypes[i] == null || _parameterTypes[i] == typeof(void))
				{
					throw new ArgumentException(SR.Arg_InvalidTypeInSignature);
				}
			}
		}
		else
		{
			_parameterTypes = Array.Empty<RuntimeType>();
		}
		_returnType = (((object)returnType == null) ? ((RuntimeType)typeof(void)) : ((returnType.UnderlyingSystemType as RuntimeType) ?? throw new NotSupportedException(SR.Arg_InvalidTypeInRetType)));
		if (transparentMethod)
		{
			_module = GetDynamicMethodsModule();
			_restrictedSkipVisibility = skipVisibility;
		}
		else
		{
			if (m != null)
			{
				_module = RuntimeModuleBuilder.GetRuntimeModuleFromModule(m);
			}
			else if (owner?.UnderlyingSystemType is RuntimeType runtimeType)
			{
				if (runtimeType.HasElementType || runtimeType.ContainsGenericParameters || runtimeType.IsGenericParameter || runtimeType.IsInterface)
				{
					throw new ArgumentException(SR.Argument_InvalidTypeForDynamicMethod);
				}
				_typeOwner = runtimeType;
				_module = runtimeType.GetRuntimeModule();
			}
			else
			{
				_module = null;
			}
			_skipVisibility = skipVisibility;
		}
		_ilGenerator = null;
		_initLocals = true;
		_methodHandle = null;
		_name = name;
		_attributes = attributes;
		_callingConvention = callingConvention;
	}

	public override string ToString()
	{
		ValueStringBuilder sbParamList = new ValueStringBuilder(100);
		sbParamList.Append(ReturnType.FormatTypeName());
		sbParamList.Append(' ');
		sbParamList.Append(Name);
		sbParamList.Append('(');
		MethodBase.AppendParameters(ref sbParamList, GetParameterTypes(), CallingConvention);
		sbParamList.Append(')');
		return sbParamList.ToString();
	}

	public override MethodInfo GetBaseDefinition()
	{
		return this;
	}

	public override ParameterInfo[] GetParameters()
	{
		ParameterInfo[] array = LoadParameters();
		ParameterInfo[] array2 = array;
		ParameterInfo[] array3 = new ParameterInfo[array2.Length];
		Array.Copy(array2, array3, array2.Length);
		return array3;
	}

	internal override ParameterInfo[] GetParametersNoCopy()
	{
		return LoadParameters();
	}

	public override MethodImplAttributes GetMethodImplementationFlags()
	{
		return MethodImplAttributes.NoInlining;
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		ArgumentNullException.ThrowIfNull(attributeType, "attributeType");
		if (!(attributeType.UnderlyingSystemType is RuntimeType caType))
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		bool flag = attributeType.IsAssignableFrom(typeof(MethodImplAttribute));
		object[] array = CustomAttribute.CreateAttributeArrayHelper(caType, flag ? 1 : 0);
		if (flag)
		{
			array[0] = new MethodImplAttribute((MethodImplOptions)GetMethodImplementationFlags());
		}
		return array;
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return new object[1]
		{
			new MethodImplAttribute((MethodImplOptions)GetMethodImplementationFlags())
		};
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		ArgumentNullException.ThrowIfNull(attributeType, "attributeType");
		return attributeType.IsAssignableFrom(typeof(MethodImplAttribute));
	}

	public ParameterBuilder? DefineParameter(int position, ParameterAttributes attributes, string? parameterName)
	{
		if (position < 0 || position > _parameterTypes.Length)
		{
			throw new ArgumentOutOfRangeException(SR.ArgumentOutOfRange_ParamSequence);
		}
		position--;
		if (position >= 0)
		{
			RuntimeParameterInfo[] array = LoadParameters();
			array[position].SetName(parameterName);
			array[position].SetAttributes(attributes);
		}
		return null;
	}

	public ILGenerator GetILGenerator()
	{
		return GetILGenerator(64);
	}

	private RuntimeParameterInfo[] LoadParameters()
	{
		if (_parameters == null)
		{
			Type[] parameterTypes = _parameterTypes;
			Type[] array = parameterTypes;
			RuntimeParameterInfo[] array2 = new RuntimeParameterInfo[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = new RuntimeParameterInfo(this, null, array[i], i);
			}
			if (_parameters == null)
			{
				_parameters = array2;
			}
		}
		return _parameters;
	}
}
