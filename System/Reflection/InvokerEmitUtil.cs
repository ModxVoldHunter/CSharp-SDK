using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.StubHelpers;

namespace System.Reflection;

internal static class InvokerEmitUtil
{
	internal unsafe delegate object InvokeFunc_RefArgs(object obj, nint* refArguments);

	internal delegate object InvokeFunc_ObjSpanArgs(object obj, Span<object> arguments);

	internal delegate object InvokeFunc_Obj4Args(object obj, object arg1, object arg2, object arg3, object arg4);

	private static class ThrowHelper
	{
		public static void Throw_NullReference_InvokeNullRefReturned()
		{
			throw new NullReferenceException(SR.NullReference_InvokeNullRefReturned);
		}
	}

	private static class Methods
	{
		private static FieldInfo s_ByReferenceOfByte_Value;

		private static MethodInfo s_Span_get_Item;

		private static MethodInfo s_ThrowHelper_Throw_NullReference_InvokeNullRefReturned;

		private static MethodInfo s_Object_GetRawData;

		private static MethodInfo s_Pointer_Box;

		private static MethodInfo s_Type_GetTypeFromHandle;

		private static MethodInfo s_NextCallReturnAddress;

		public static FieldInfo ByReferenceOfByte_Value()
		{
			return s_ByReferenceOfByte_Value ?? (s_ByReferenceOfByte_Value = typeof(ByReference).GetField("Value"));
		}

		public static MethodInfo Span_get_Item()
		{
			return s_Span_get_Item ?? (s_Span_get_Item = typeof(Span<object>).GetProperty("Item").GetGetMethod());
		}

		public static MethodInfo ThrowHelper_Throw_NullReference_InvokeNullRefReturned()
		{
			return s_ThrowHelper_Throw_NullReference_InvokeNullRefReturned ?? (s_ThrowHelper_Throw_NullReference_InvokeNullRefReturned = typeof(ThrowHelper).GetMethod("Throw_NullReference_InvokeNullRefReturned"));
		}

		public static MethodInfo Object_GetRawData()
		{
			return s_Object_GetRawData ?? (s_Object_GetRawData = typeof(RuntimeHelpers).GetMethod("GetRawData", BindingFlags.Static | BindingFlags.NonPublic));
		}

		public unsafe static MethodInfo Pointer_Box()
		{
			return s_Pointer_Box ?? (s_Pointer_Box = typeof(Pointer).GetMethod("Box", new Type[2]
			{
				typeof(void*),
				typeof(Type)
			}));
		}

		public static MethodInfo Type_GetTypeFromHandle()
		{
			return s_Type_GetTypeFromHandle ?? (s_Type_GetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle", new Type[1] { typeof(RuntimeTypeHandle) }));
		}

		public static MethodInfo NextCallReturnAddress()
		{
			return s_NextCallReturnAddress ?? (s_NextCallReturnAddress = typeof(System.StubHelpers.StubHelpers).GetMethod("NextCallReturnAddress", BindingFlags.Static | BindingFlags.NonPublic));
		}
	}

	public static InvokeFunc_Obj4Args CreateInvokeDelegate_Obj4Args(MethodBase method, bool backwardsCompat)
	{
		bool flag = method is RuntimeConstructorInfo;
		bool flag2 = !flag && !method.IsStatic;
		Type[] parameterTypes = new Type[5]
		{
			typeof(object),
			typeof(object),
			typeof(object),
			typeof(object),
			typeof(object)
		};
		string text = ((method.DeclaringType != null) ? (method.DeclaringType.Name + ".") : string.Empty);
		DynamicMethod dynamicMethod = new DynamicMethod("InvokeStub_" + text + method.Name, typeof(object), parameterTypes, typeof(object).Module, skipVisibility: true);
		ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
		if (flag2)
		{
			iLGenerator.Emit(OpCodes.Ldarg_0);
			if (method.DeclaringType.IsValueType)
			{
				iLGenerator.Emit(OpCodes.Unbox, method.DeclaringType);
			}
		}
		ParameterInfo[] parametersNoCopy = method.GetParametersNoCopy();
		for (int i = 0; i < parametersNoCopy.Length; i++)
		{
			RuntimeType runtimeType = (RuntimeType)parametersNoCopy[i].ParameterType;
			switch (i)
			{
			case 0:
				iLGenerator.Emit(OpCodes.Ldarg_1);
				break;
			case 1:
				iLGenerator.Emit(OpCodes.Ldarg_2);
				break;
			case 2:
				iLGenerator.Emit(OpCodes.Ldarg_3);
				break;
			default:
				iLGenerator.Emit(OpCodes.Ldarg_S, i + 1);
				break;
			}
			if (runtimeType.IsPointer || runtimeType.IsFunctionPointer)
			{
				Unbox(iLGenerator, typeof(nint));
			}
			else if (runtimeType.IsValueType)
			{
				Unbox(iLGenerator, runtimeType);
			}
		}
		EmitCallAndReturnHandling(iLGenerator, method, flag, backwardsCompat);
		return (InvokeFunc_Obj4Args)dynamicMethod.CreateDelegate(typeof(InvokeFunc_Obj4Args), null);
	}

	public static InvokeFunc_ObjSpanArgs CreateInvokeDelegate_ObjSpanArgs(MethodBase method, bool backwardsCompat)
	{
		bool flag = method is RuntimeConstructorInfo;
		bool flag2 = !flag && !method.IsStatic;
		Type[] parameterTypes = new Type[2]
		{
			typeof(object),
			typeof(Span<object>)
		};
		string text = ((method.DeclaringType != null) ? (method.DeclaringType.Name + ".") : string.Empty);
		DynamicMethod dynamicMethod = new DynamicMethod("InvokeStub_" + text + method.Name, typeof(object), parameterTypes, typeof(object).Module, skipVisibility: true);
		ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
		if (flag2)
		{
			iLGenerator.Emit(OpCodes.Ldarg_0);
			if (method.DeclaringType.IsValueType)
			{
				iLGenerator.Emit(OpCodes.Unbox, method.DeclaringType);
			}
		}
		ParameterInfo[] parametersNoCopy = method.GetParametersNoCopy();
		for (int i = 0; i < parametersNoCopy.Length; i++)
		{
			RuntimeType runtimeType = (RuntimeType)parametersNoCopy[i].ParameterType;
			iLGenerator.Emit(OpCodes.Ldarga_S, 1);
			iLGenerator.Emit(OpCodes.Ldc_I4, i);
			iLGenerator.Emit(OpCodes.Call, Methods.Span_get_Item());
			iLGenerator.Emit(OpCodes.Ldind_Ref);
			if (runtimeType.IsPointer || runtimeType.IsFunctionPointer)
			{
				Unbox(iLGenerator, typeof(nint));
			}
			else if (runtimeType.IsValueType)
			{
				Unbox(iLGenerator, runtimeType);
			}
		}
		EmitCallAndReturnHandling(iLGenerator, method, flag, backwardsCompat);
		return (InvokeFunc_ObjSpanArgs)dynamicMethod.CreateDelegate(typeof(InvokeFunc_ObjSpanArgs), null);
	}

	public unsafe static InvokeFunc_RefArgs CreateInvokeDelegate_RefArgs(MethodBase method, bool backwardsCompat)
	{
		bool flag = method is RuntimeConstructorInfo;
		bool flag2 = !flag && !method.IsStatic;
		Type[] parameterTypes = new Type[3]
		{
			typeof(object),
			typeof(object),
			typeof(nint*)
		};
		string text = ((method.DeclaringType != null) ? (method.DeclaringType.Name + ".") : string.Empty);
		DynamicMethod dynamicMethod = new DynamicMethod("InvokeStub_" + text + method.Name, typeof(object), parameterTypes, typeof(object).Module, skipVisibility: true);
		ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
		if (flag2)
		{
			iLGenerator.Emit(OpCodes.Ldarg_1);
			if (method.DeclaringType.IsValueType)
			{
				iLGenerator.Emit(OpCodes.Unbox, method.DeclaringType);
			}
		}
		ParameterInfo[] parametersNoCopy = method.GetParametersNoCopy();
		for (int i = 0; i < parametersNoCopy.Length; i++)
		{
			iLGenerator.Emit(OpCodes.Ldarg_2);
			if (i != 0)
			{
				iLGenerator.Emit(OpCodes.Ldc_I4, i * 8);
				iLGenerator.Emit(OpCodes.Add);
			}
			iLGenerator.Emit(OpCodes.Ldfld, Methods.ByReferenceOfByte_Value());
			RuntimeType runtimeType = (RuntimeType)parametersNoCopy[i].ParameterType;
			if (!runtimeType.IsByRef)
			{
				iLGenerator.Emit(OpCodes.Ldobj, (runtimeType.IsPointer || runtimeType.IsFunctionPointer) ? typeof(nint) : runtimeType);
			}
		}
		EmitCallAndReturnHandling(iLGenerator, method, flag, backwardsCompat);
		return (InvokeFunc_RefArgs)dynamicMethod.CreateDelegate(typeof(InvokeFunc_RefArgs), null);
	}

	private static void Unbox(ILGenerator il, Type parameterType)
	{
		il.Emit(OpCodes.Call, Methods.Object_GetRawData());
		il.Emit(OpCodes.Ldobj, parameterType);
	}

	private static void EmitCallAndReturnHandling(ILGenerator il, MethodBase method, bool emitNew, bool backwardsCompat)
	{
		if (backwardsCompat && RuntimeFeature.IsDynamicCodeCompiled)
		{
			il.Emit(OpCodes.Call, Methods.NextCallReturnAddress());
			il.Emit(OpCodes.Pop);
		}
		if (emitNew)
		{
			il.Emit(OpCodes.Newobj, (ConstructorInfo)method);
		}
		else if (method.IsStatic || method.DeclaringType.IsValueType)
		{
			il.Emit(OpCodes.Call, (MethodInfo)method);
		}
		else
		{
			il.Emit(OpCodes.Callvirt, (MethodInfo)method);
		}
		if (emitNew)
		{
			Type declaringType = method.DeclaringType;
			if (declaringType.IsValueType)
			{
				il.Emit(OpCodes.Box, declaringType);
			}
		}
		else
		{
			RuntimeType runtimeType = ((!(method is RuntimeMethodInfo runtimeMethodInfo)) ? ((RuntimeType)((DynamicMethod)method).ReturnType) : ((RuntimeType)runtimeMethodInfo.ReturnType));
			if (runtimeType == typeof(void))
			{
				il.Emit(OpCodes.Ldnull);
			}
			else if (runtimeType.IsValueType)
			{
				il.Emit(OpCodes.Box, runtimeType);
			}
			else if (runtimeType.IsPointer)
			{
				il.Emit(OpCodes.Ldtoken, runtimeType);
				il.Emit(OpCodes.Call, Methods.Type_GetTypeFromHandle());
				il.Emit(OpCodes.Call, Methods.Pointer_Box());
			}
			else if (runtimeType.IsFunctionPointer)
			{
				il.Emit(OpCodes.Box, typeof(nint));
			}
			else if (runtimeType.IsByRef)
			{
				RuntimeType runtimeType2 = (RuntimeType)runtimeType.GetElementType();
				Label label = il.DefineLabel();
				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Brtrue_S, label);
				il.Emit(OpCodes.Call, Methods.ThrowHelper_Throw_NullReference_InvokeNullRefReturned());
				il.MarkLabel(label);
				if (runtimeType2.IsValueType)
				{
					il.Emit(OpCodes.Ldobj, runtimeType2);
					il.Emit(OpCodes.Box, runtimeType2);
				}
				else if (runtimeType2.IsPointer)
				{
					il.Emit(OpCodes.Ldind_Ref);
					il.Emit(OpCodes.Conv_U);
					il.Emit(OpCodes.Ldtoken, runtimeType2);
					il.Emit(OpCodes.Call, Methods.Type_GetTypeFromHandle());
					il.Emit(OpCodes.Call, Methods.Pointer_Box());
				}
				else if (runtimeType2.IsFunctionPointer)
				{
					il.Emit(OpCodes.Box, typeof(nint));
				}
				else
				{
					il.Emit(OpCodes.Ldobj, runtimeType2);
				}
			}
		}
		il.Emit(OpCodes.Ret);
	}
}
