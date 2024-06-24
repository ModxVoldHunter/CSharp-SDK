using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization.DataContracts;

namespace System.Runtime.Serialization;

internal sealed class CodeGenerator
{
	private static MethodInfo s_getTypeFromHandle;

	private static MethodInfo s_objectEquals;

	private static MethodInfo s_arraySetValue;

	private static MethodInfo s_objectToString;

	private static MethodInfo s_stringFormat;

	private Type _delegateType;

	private static Module s_serializationModule;

	private DynamicMethod _dynamicMethod;

	private ILGenerator _ilGen;

	private List<ArgBuilder> _argList;

	private Stack<object> _blockStack;

	private Label _methodEndLabel;

	private LocalBuilder _stringFormatArray;

	private static readonly MethodInfo s_stringLength = typeof(string).GetProperty("Length").GetMethod;

	private static MethodInfo GetTypeFromHandle
	{
		get
		{
			if (s_getTypeFromHandle == null)
			{
				s_getTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");
			}
			return s_getTypeFromHandle;
		}
	}

	private static MethodInfo ObjectEquals
	{
		get
		{
			if (s_objectEquals == null)
			{
				s_objectEquals = typeof(object).GetMethod("Equals", BindingFlags.Static | BindingFlags.Public);
			}
			return s_objectEquals;
		}
	}

	private static MethodInfo ArraySetValue
	{
		get
		{
			if (s_arraySetValue == null)
			{
				s_arraySetValue = typeof(Array).GetMethod("SetValue", new Type[2]
				{
					typeof(object),
					typeof(int)
				});
			}
			return s_arraySetValue;
		}
	}

	private static MethodInfo ObjectToString
	{
		get
		{
			if (s_objectToString == null)
			{
				s_objectToString = typeof(object).GetMethod("ToString", Type.EmptyTypes);
			}
			return s_objectToString;
		}
	}

	private static MethodInfo StringFormat
	{
		get
		{
			if (s_stringFormat == null)
			{
				s_stringFormat = typeof(string).GetMethod("Format", new Type[2]
				{
					typeof(string),
					typeof(object[])
				});
			}
			return s_stringFormat;
		}
	}

	private static Module SerializationModule => s_serializationModule ?? (s_serializationModule = typeof(CodeGenerator).Module);

	internal MethodInfo CurrentMethod => _dynamicMethod;

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "The trimmer will never remove the Invoke method from delegates.")]
	internal static MethodInfo GetInvokeMethod(Type delegateType)
	{
		return delegateType.GetMethod("Invoke");
	}

	internal CodeGenerator()
	{
	}

	internal void BeginMethod(DynamicMethod dynamicMethod, Type delegateType, Type[] argTypes)
	{
		_dynamicMethod = dynamicMethod;
		_ilGen = _dynamicMethod.GetILGenerator();
		_delegateType = delegateType;
		InitILGeneration(argTypes);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	internal void BeginMethod(string methodName, Type delegateType, bool allowPrivateMemberAccess)
	{
		MethodInfo invokeMethod = GetInvokeMethod(delegateType);
		ParameterInfo[] parameters = invokeMethod.GetParameters();
		Type[] array = new Type[parameters.Length];
		for (int i = 0; i < parameters.Length; i++)
		{
			array[i] = parameters[i].ParameterType;
		}
		BeginMethod(invokeMethod.ReturnType, methodName, array, allowPrivateMemberAccess);
		_delegateType = delegateType;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	private void BeginMethod(Type returnType, string methodName, Type[] argTypes, bool allowPrivateMemberAccess)
	{
		_dynamicMethod = new DynamicMethod(methodName, returnType, argTypes, SerializationModule, allowPrivateMemberAccess);
		_ilGen = _dynamicMethod.GetILGenerator();
		InitILGeneration(argTypes);
	}

	private void InitILGeneration(Type[] argTypes)
	{
		_methodEndLabel = _ilGen.DefineLabel();
		_blockStack = new Stack<object>();
		_argList = new List<ArgBuilder>();
		for (int i = 0; i < argTypes.Length; i++)
		{
			_argList.Add(new ArgBuilder(i, argTypes[i]));
		}
	}

	internal Delegate EndMethod()
	{
		MarkLabel(_methodEndLabel);
		Ret();
		Delegate result = _dynamicMethod.CreateDelegate(_delegateType);
		_dynamicMethod = null;
		_delegateType = null;
		_ilGen = null;
		_blockStack = null;
		_argList = null;
		return result;
	}

	internal ArgBuilder GetArg(int index)
	{
		return _argList[index];
	}

	internal static Type GetVariableType(object var)
	{
		if (var is ArgBuilder argBuilder)
		{
			return argBuilder.ArgType;
		}
		if (var is LocalBuilder localBuilder)
		{
			return localBuilder.LocalType;
		}
		return var.GetType();
	}

	internal LocalBuilder DeclareLocal(Type type, object initialValue)
	{
		LocalBuilder localBuilder = DeclareLocal(type);
		Load(initialValue);
		Store(localBuilder);
		return localBuilder;
	}

	internal LocalBuilder DeclareLocal(Type type)
	{
		return DeclareLocal(type, isPinned: false);
	}

	internal LocalBuilder DeclareLocal(Type type, bool isPinned)
	{
		return _ilGen.DeclareLocal(type, isPinned);
	}

	internal void Set(LocalBuilder local, object value)
	{
		Load(value);
		Store(local);
	}

	internal object For(LocalBuilder local, object start, object end)
	{
		ForState forState = new ForState(local, DefineLabel(), DefineLabel(), end);
		if (forState.Index != null)
		{
			Load(start);
			Stloc(forState.Index);
			Br(forState.TestLabel);
		}
		MarkLabel(forState.BeginLabel);
		_blockStack.Push(forState);
		return forState;
	}

	internal void EndFor()
	{
		object obj = _blockStack.Pop();
		ForState forState = obj as ForState;
		if (forState == null)
		{
			ThrowMismatchException(obj);
		}
		if (forState.Index != null)
		{
			Ldloc(forState.Index);
			Ldc(1);
			Add();
			Stloc(forState.Index);
			MarkLabel(forState.TestLabel);
			Ldloc(forState.Index);
			Load(forState.End);
			if (GetVariableType(forState.End).IsArray)
			{
				Ldlen();
			}
			Blt(forState.BeginLabel);
		}
		else
		{
			Br(forState.BeginLabel);
		}
		if (forState.RequiresEndLabel)
		{
			MarkLabel(forState.EndLabel);
		}
	}

	internal void Break(object forState)
	{
		InternalBreakFor(forState, OpCodes.Br);
	}

	internal void IfFalseBreak(object forState)
	{
		InternalBreakFor(forState, OpCodes.Brfalse);
	}

	internal void InternalBreakFor(object userForState, OpCode branchInstruction)
	{
		foreach (object item in _blockStack)
		{
			if (item == userForState && item is ForState forState)
			{
				if (!forState.RequiresEndLabel)
				{
					forState.EndLabel = DefineLabel();
					forState.RequiresEndLabel = true;
				}
				_ilGen.Emit(branchInstruction, forState.EndLabel);
				break;
			}
		}
	}

	internal void ForEach(LocalBuilder local, Type elementType, LocalBuilder enumerator, MethodInfo getCurrentMethod)
	{
		ForState forState = new ForState(local, DefineLabel(), DefineLabel(), enumerator);
		Br(forState.TestLabel);
		MarkLabel(forState.BeginLabel);
		Call(enumerator, getCurrentMethod);
		ConvertValue(elementType, GetVariableType(local));
		Stloc(local);
		_blockStack.Push(forState);
	}

	internal void EndForEach(MethodInfo moveNextMethod)
	{
		object obj = _blockStack.Pop();
		ForState forState = obj as ForState;
		if (forState == null)
		{
			ThrowMismatchException(obj);
		}
		MarkLabel(forState.TestLabel);
		object end = forState.End;
		Call(end, moveNextMethod);
		Brtrue(forState.BeginLabel);
		if (forState.RequiresEndLabel)
		{
			MarkLabel(forState.EndLabel);
		}
	}

	internal void IfNotDefaultValue(object value)
	{
		Type variableType = GetVariableType(value);
		TypeCode typeCode = Type.GetTypeCode(variableType);
		if ((typeCode == TypeCode.Object && variableType.IsValueType) || typeCode == TypeCode.DateTime || typeCode == TypeCode.Decimal)
		{
			LoadDefaultValue(variableType);
			ConvertValue(variableType, Globals.TypeOfObject);
			Load(value);
			ConvertValue(variableType, Globals.TypeOfObject);
			Call(ObjectEquals);
			IfNot();
		}
		else
		{
			LoadDefaultValue(variableType);
			Load(value);
			If(Cmp.NotEqualTo);
		}
	}

	internal void If()
	{
		InternalIf(negate: false);
	}

	internal void IfNot()
	{
		InternalIf(negate: true);
	}

	private static OpCode GetBranchCode(Cmp cmp)
	{
		return cmp switch
		{
			Cmp.LessThan => OpCodes.Bge, 
			Cmp.EqualTo => OpCodes.Bne_Un, 
			Cmp.LessThanOrEqualTo => OpCodes.Bgt, 
			Cmp.GreaterThan => OpCodes.Ble, 
			Cmp.NotEqualTo => OpCodes.Beq, 
			_ => OpCodes.Blt, 
		};
	}

	internal void If(Cmp cmpOp)
	{
		IfState ifState = new IfState();
		ifState.EndIf = DefineLabel();
		ifState.ElseBegin = DefineLabel();
		_ilGen.Emit(GetBranchCode(cmpOp), ifState.ElseBegin);
		_blockStack.Push(ifState);
	}

	internal void If(object value1, Cmp cmpOp, object value2)
	{
		Load(value1);
		Load(value2);
		If(cmpOp);
	}

	internal void Else()
	{
		IfState ifState = PopIfState();
		Br(ifState.EndIf);
		MarkLabel(ifState.ElseBegin);
		ifState.ElseBegin = ifState.EndIf;
		_blockStack.Push(ifState);
	}

	internal void ElseIf(object value1, Cmp cmpOp, object value2)
	{
		IfState ifState = (IfState)_blockStack.Pop();
		Br(ifState.EndIf);
		MarkLabel(ifState.ElseBegin);
		Load(value1);
		Load(value2);
		ifState.ElseBegin = DefineLabel();
		_ilGen.Emit(GetBranchCode(cmpOp), ifState.ElseBegin);
		_blockStack.Push(ifState);
	}

	internal void EndIf()
	{
		IfState ifState = PopIfState();
		if (!ifState.ElseBegin.Equals(ifState.EndIf))
		{
			MarkLabel(ifState.ElseBegin);
		}
		MarkLabel(ifState.EndIf);
	}

	internal static void VerifyParameterCount(MethodInfo methodInfo, int expectedCount)
	{
		if (methodInfo.GetParameters().Length != expectedCount)
		{
			throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ParameterCountMismatch, methodInfo.Name, methodInfo.GetParameters().Length, expectedCount));
		}
	}

	internal void Call(object thisObj, MethodInfo methodInfo)
	{
		VerifyParameterCount(methodInfo, 0);
		LoadThis(thisObj, methodInfo);
		Call(methodInfo);
	}

	internal void Call(object thisObj, MethodInfo methodInfo, object param1)
	{
		VerifyParameterCount(methodInfo, 1);
		LoadThis(thisObj, methodInfo);
		LoadParam(param1, 1, methodInfo);
		Call(methodInfo);
	}

	internal void Call(object thisObj, MethodInfo methodInfo, object param1, object param2)
	{
		VerifyParameterCount(methodInfo, 2);
		LoadThis(thisObj, methodInfo);
		LoadParam(param1, 1, methodInfo);
		LoadParam(param2, 2, methodInfo);
		Call(methodInfo);
	}

	internal void Call(object thisObj, MethodInfo methodInfo, object param1, object param2, object param3)
	{
		VerifyParameterCount(methodInfo, 3);
		LoadThis(thisObj, methodInfo);
		LoadParam(param1, 1, methodInfo);
		LoadParam(param2, 2, methodInfo);
		LoadParam(param3, 3, methodInfo);
		Call(methodInfo);
	}

	internal void Call(object thisObj, MethodInfo methodInfo, object param1, object param2, object param3, object param4)
	{
		VerifyParameterCount(methodInfo, 4);
		LoadThis(thisObj, methodInfo);
		LoadParam(param1, 1, methodInfo);
		LoadParam(param2, 2, methodInfo);
		LoadParam(param3, 3, methodInfo);
		LoadParam(param4, 4, methodInfo);
		Call(methodInfo);
	}

	internal void Call(object thisObj, MethodInfo methodInfo, object param1, object param2, object param3, object param4, object param5)
	{
		VerifyParameterCount(methodInfo, 5);
		LoadThis(thisObj, methodInfo);
		LoadParam(param1, 1, methodInfo);
		LoadParam(param2, 2, methodInfo);
		LoadParam(param3, 3, methodInfo);
		LoadParam(param4, 4, methodInfo);
		LoadParam(param5, 5, methodInfo);
		Call(methodInfo);
	}

	internal void Call(object thisObj, MethodInfo methodInfo, object param1, object param2, object param3, object param4, object param5, object param6)
	{
		VerifyParameterCount(methodInfo, 6);
		LoadThis(thisObj, methodInfo);
		LoadParam(param1, 1, methodInfo);
		LoadParam(param2, 2, methodInfo);
		LoadParam(param3, 3, methodInfo);
		LoadParam(param4, 4, methodInfo);
		LoadParam(param5, 5, methodInfo);
		LoadParam(param6, 6, methodInfo);
		Call(methodInfo);
	}

	internal void Call(MethodInfo methodInfo)
	{
		if (methodInfo.IsVirtual && !methodInfo.DeclaringType.IsValueType)
		{
			_ilGen.Emit(OpCodes.Callvirt, methodInfo);
		}
		else if (methodInfo.IsStatic)
		{
			_ilGen.Emit(OpCodes.Call, methodInfo);
		}
		else
		{
			_ilGen.Emit(OpCodes.Call, methodInfo);
		}
	}

	internal void Call(ConstructorInfo ctor)
	{
		_ilGen.Emit(OpCodes.Call, ctor);
	}

	internal void New(ConstructorInfo constructorInfo)
	{
		_ilGen.Emit(OpCodes.Newobj, constructorInfo);
	}

	internal void InitObj(Type valueType)
	{
		_ilGen.Emit(OpCodes.Initobj, valueType);
	}

	internal void NewArray(Type elementType, object len)
	{
		Load(len);
		_ilGen.Emit(OpCodes.Newarr, elementType);
	}

	internal void LoadArrayElement(object obj, object arrayIndex)
	{
		Type elementType = GetVariableType(obj).GetElementType();
		Load(obj);
		Load(arrayIndex);
		if (IsStruct(elementType))
		{
			Ldelema(elementType);
			Ldobj(elementType);
		}
		else
		{
			Ldelem(elementType);
		}
	}

	internal void StoreArrayElement(object obj, object arrayIndex, object value)
	{
		Type variableType = GetVariableType(obj);
		if (variableType == Globals.TypeOfArray)
		{
			Call(obj, ArraySetValue, value, arrayIndex);
			return;
		}
		Type elementType = variableType.GetElementType();
		Load(obj);
		Load(arrayIndex);
		if (IsStruct(elementType))
		{
			Ldelema(elementType);
		}
		Load(value);
		ConvertValue(GetVariableType(value), elementType);
		if (IsStruct(elementType))
		{
			Stobj(elementType);
		}
		else
		{
			Stelem(elementType);
		}
	}

	private static bool IsStruct(Type objType)
	{
		if (objType.IsValueType)
		{
			return !objType.IsPrimitive;
		}
		return false;
	}

	internal Type LoadMember(MemberInfo memberInfo)
	{
		Type result;
		if (memberInfo is FieldInfo fieldInfo)
		{
			result = fieldInfo.FieldType;
			if (fieldInfo.IsStatic)
			{
				_ilGen.Emit(OpCodes.Ldsfld, fieldInfo);
			}
			else
			{
				_ilGen.Emit(OpCodes.Ldfld, fieldInfo);
			}
		}
		else if (memberInfo is PropertyInfo propertyInfo)
		{
			result = propertyInfo.PropertyType;
			MethodInfo getMethod = propertyInfo.GetMethod;
			if (getMethod == null)
			{
				throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.NoGetMethodForProperty, propertyInfo.DeclaringType, propertyInfo));
			}
			Call(getMethod);
		}
		else
		{
			if (!(memberInfo is MethodInfo methodInfo))
			{
				throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.CannotLoadMemberType, "Unknown", memberInfo.DeclaringType, memberInfo.Name));
			}
			result = methodInfo.ReturnType;
			Call(methodInfo);
		}
		return result;
	}

	internal void StoreMember(MemberInfo memberInfo)
	{
		if (memberInfo is FieldInfo fieldInfo)
		{
			if (fieldInfo.IsStatic)
			{
				_ilGen.Emit(OpCodes.Stsfld, fieldInfo);
			}
			else
			{
				_ilGen.Emit(OpCodes.Stfld, fieldInfo);
			}
		}
		else if (memberInfo is PropertyInfo propertyInfo)
		{
			MethodInfo setMethod = propertyInfo.SetMethod;
			if (setMethod == null)
			{
				throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.NoSetMethodForProperty, propertyInfo.DeclaringType, propertyInfo));
			}
			Call(setMethod);
		}
		else
		{
			if (!(memberInfo is MethodInfo methodInfo))
			{
				throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.CannotLoadMemberType, "Unknown"));
			}
			Call(methodInfo);
		}
	}

	internal void LoadDefaultValue(Type type)
	{
		if (type.IsValueType)
		{
			switch (Type.GetTypeCode(type))
			{
			case TypeCode.Boolean:
				Ldc(boolVar: false);
				return;
			case TypeCode.Char:
			case TypeCode.SByte:
			case TypeCode.Byte:
			case TypeCode.Int16:
			case TypeCode.UInt16:
			case TypeCode.Int32:
			case TypeCode.UInt32:
				Ldc(0);
				return;
			case TypeCode.Int64:
			case TypeCode.UInt64:
				Ldc(0L);
				return;
			case TypeCode.Single:
				Ldc(0f);
				return;
			case TypeCode.Double:
				Ldc(0.0);
				return;
			}
			LocalBuilder obj = DeclareLocal(type);
			LoadAddress(obj);
			InitObj(type);
			Load(obj);
		}
		else
		{
			Load(null);
		}
	}

	internal void Load(object obj)
	{
		if (obj == null)
		{
			_ilGen.Emit(OpCodes.Ldnull);
		}
		else if (obj is ArgBuilder arg)
		{
			Ldarg(arg);
		}
		else if (obj is LocalBuilder localBuilder)
		{
			Ldloc(localBuilder);
		}
		else
		{
			Ldc(obj);
		}
	}

	internal void Store(object var)
	{
		if (var is ArgBuilder arg)
		{
			Starg(arg);
			return;
		}
		if (var is LocalBuilder local)
		{
			Stloc(local);
			return;
		}
		throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.CanOnlyStoreIntoArgOrLocGot0, DataContract.GetClrTypeFullName(var.GetType())));
	}

	internal void Dec(object var)
	{
		Load(var);
		Load(1);
		Subtract();
		Store(var);
	}

	internal void LoadAddress(object obj)
	{
		if (obj is ArgBuilder argBuilder)
		{
			LdargAddress(argBuilder);
		}
		else if (obj is LocalBuilder localBuilder)
		{
			LdlocAddress(localBuilder);
		}
		else
		{
			Load(obj);
		}
	}

	internal void ConvertAddress(Type source, Type target)
	{
		InternalConvert(source, target, isAddress: true);
	}

	internal void ConvertValue(Type source, Type target)
	{
		InternalConvert(source, target, isAddress: false);
	}

	internal void Castclass(Type target)
	{
		_ilGen.Emit(OpCodes.Castclass, target);
	}

	internal void Box(Type type)
	{
		_ilGen.Emit(OpCodes.Box, type);
	}

	internal void Unbox(Type type)
	{
		_ilGen.Emit(OpCodes.Unbox, type);
	}

	private static OpCode GetLdindOpCode(TypeCode typeCode)
	{
		return typeCode switch
		{
			TypeCode.Boolean => OpCodes.Ldind_I1, 
			TypeCode.Char => OpCodes.Ldind_I2, 
			TypeCode.SByte => OpCodes.Ldind_I1, 
			TypeCode.Byte => OpCodes.Ldind_U1, 
			TypeCode.Int16 => OpCodes.Ldind_I2, 
			TypeCode.UInt16 => OpCodes.Ldind_U2, 
			TypeCode.Int32 => OpCodes.Ldind_I4, 
			TypeCode.UInt32 => OpCodes.Ldind_U4, 
			TypeCode.Int64 => OpCodes.Ldind_I8, 
			TypeCode.UInt64 => OpCodes.Ldind_I8, 
			TypeCode.Single => OpCodes.Ldind_R4, 
			TypeCode.Double => OpCodes.Ldind_R8, 
			TypeCode.String => OpCodes.Ldind_Ref, 
			_ => OpCodes.Nop, 
		};
	}

	internal void Ldobj(Type type)
	{
		OpCode ldindOpCode = GetLdindOpCode(Type.GetTypeCode(type));
		if (!ldindOpCode.Equals(OpCodes.Nop))
		{
			_ilGen.Emit(ldindOpCode);
		}
		else
		{
			_ilGen.Emit(OpCodes.Ldobj, type);
		}
	}

	internal void Stobj(Type type)
	{
		_ilGen.Emit(OpCodes.Stobj, type);
	}

	internal void Ceq()
	{
		_ilGen.Emit(OpCodes.Ceq);
	}

	internal void Throw()
	{
		_ilGen.Emit(OpCodes.Throw);
	}

	internal void Ldtoken(Type t)
	{
		_ilGen.Emit(OpCodes.Ldtoken, t);
	}

	internal void Ldc(object o)
	{
		Type type = o.GetType();
		if (o is Type t)
		{
			Ldtoken(t);
			Call(GetTypeFromHandle);
			return;
		}
		if (type.IsEnum)
		{
			Ldc(Convert.ChangeType(o, Enum.GetUnderlyingType(type), null));
			return;
		}
		switch (Type.GetTypeCode(type))
		{
		case TypeCode.Boolean:
			Ldc((bool)o);
			break;
		case TypeCode.Char:
			throw new NotSupportedException(System.SR.CharIsInvalidPrimitive);
		case TypeCode.SByte:
		case TypeCode.Byte:
		case TypeCode.Int16:
		case TypeCode.UInt16:
			Ldc(Convert.ToInt32(o, CultureInfo.InvariantCulture));
			break;
		case TypeCode.Int32:
			Ldc((int)o);
			break;
		case TypeCode.UInt32:
			Ldc((int)(uint)o);
			break;
		case TypeCode.UInt64:
			Ldc((long)(ulong)o);
			break;
		case TypeCode.Int64:
			Ldc((long)o);
			break;
		case TypeCode.Single:
			Ldc((float)o);
			break;
		case TypeCode.Double:
			Ldc((double)o);
			break;
		case TypeCode.String:
			Ldstr((string)o);
			break;
		default:
			throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.UnknownConstantType, DataContract.GetClrTypeFullName(type)));
		}
	}

	internal void Ldc(bool boolVar)
	{
		if (boolVar)
		{
			_ilGen.Emit(OpCodes.Ldc_I4_1);
		}
		else
		{
			_ilGen.Emit(OpCodes.Ldc_I4_0);
		}
	}

	internal void Ldc(int intVar)
	{
		_ilGen.Emit(OpCodes.Ldc_I4, intVar);
	}

	internal void Ldc(long l)
	{
		_ilGen.Emit(OpCodes.Ldc_I8, l);
	}

	internal void Ldc(float f)
	{
		_ilGen.Emit(OpCodes.Ldc_R4, f);
	}

	internal void Ldc(double d)
	{
		_ilGen.Emit(OpCodes.Ldc_R8, d);
	}

	internal void Ldstr(string strVar)
	{
		_ilGen.Emit(OpCodes.Ldstr, strVar);
	}

	internal void LdlocAddress(LocalBuilder localBuilder)
	{
		if (localBuilder.LocalType.IsValueType)
		{
			Ldloca(localBuilder);
		}
		else
		{
			Ldloc(localBuilder);
		}
	}

	internal void Ldloc(LocalBuilder localBuilder)
	{
		_ilGen.Emit(OpCodes.Ldloc, localBuilder);
	}

	internal void Stloc(LocalBuilder local)
	{
		_ilGen.Emit(OpCodes.Stloc, local);
	}

	internal void Ldloca(LocalBuilder localBuilder)
	{
		_ilGen.Emit(OpCodes.Ldloca, localBuilder);
	}

	internal void LdargAddress(ArgBuilder argBuilder)
	{
		if (argBuilder.ArgType.IsValueType)
		{
			Ldarga(argBuilder);
		}
		else
		{
			Ldarg(argBuilder);
		}
	}

	internal void Ldarg(ArgBuilder arg)
	{
		Ldarg(arg.Index);
	}

	internal void Starg(ArgBuilder arg)
	{
		Starg(arg.Index);
	}

	internal void Ldarg(int slot)
	{
		_ilGen.Emit(OpCodes.Ldarg, slot);
	}

	internal void Starg(int slot)
	{
		_ilGen.Emit(OpCodes.Starg, slot);
	}

	internal void Ldarga(ArgBuilder argBuilder)
	{
		Ldarga(argBuilder.Index);
	}

	internal void Ldarga(int slot)
	{
		_ilGen.Emit(OpCodes.Ldarga, slot);
	}

	internal void Ldlen()
	{
		_ilGen.Emit(OpCodes.Ldlen);
		_ilGen.Emit(OpCodes.Conv_I4);
	}

	private static OpCode GetLdelemOpCode(TypeCode typeCode)
	{
		switch (typeCode)
		{
		case TypeCode.Object:
		case TypeCode.DBNull:
			return OpCodes.Ldelem_Ref;
		case TypeCode.Boolean:
			return OpCodes.Ldelem_I1;
		case TypeCode.Char:
			return OpCodes.Ldelem_I2;
		case TypeCode.SByte:
			return OpCodes.Ldelem_I1;
		case TypeCode.Byte:
			return OpCodes.Ldelem_U1;
		case TypeCode.Int16:
			return OpCodes.Ldelem_I2;
		case TypeCode.UInt16:
			return OpCodes.Ldelem_U2;
		case TypeCode.Int32:
			return OpCodes.Ldelem_I4;
		case TypeCode.UInt32:
			return OpCodes.Ldelem_U4;
		case TypeCode.Int64:
			return OpCodes.Ldelem_I8;
		case TypeCode.UInt64:
			return OpCodes.Ldelem_I8;
		case TypeCode.Single:
			return OpCodes.Ldelem_R4;
		case TypeCode.Double:
			return OpCodes.Ldelem_R8;
		case TypeCode.String:
			return OpCodes.Ldelem_Ref;
		default:
			return OpCodes.Nop;
		}
	}

	internal void Ldelem(Type arrayElementType)
	{
		if (arrayElementType.IsEnum)
		{
			Ldelem(Enum.GetUnderlyingType(arrayElementType));
			return;
		}
		OpCode ldelemOpCode = GetLdelemOpCode(Type.GetTypeCode(arrayElementType));
		if (ldelemOpCode.Equals(OpCodes.Nop))
		{
			throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ArrayTypeIsNotSupported_GeneratingCode, DataContract.GetClrTypeFullName(arrayElementType)));
		}
		_ilGen.Emit(ldelemOpCode);
	}

	internal void Ldelema(Type arrayElementType)
	{
		OpCode ldelema = OpCodes.Ldelema;
		_ilGen.Emit(ldelema, arrayElementType);
	}

	private static OpCode GetStelemOpCode(TypeCode typeCode)
	{
		switch (typeCode)
		{
		case TypeCode.Object:
		case TypeCode.DBNull:
			return OpCodes.Stelem_Ref;
		case TypeCode.Boolean:
			return OpCodes.Stelem_I1;
		case TypeCode.Char:
			return OpCodes.Stelem_I2;
		case TypeCode.SByte:
			return OpCodes.Stelem_I1;
		case TypeCode.Byte:
			return OpCodes.Stelem_I1;
		case TypeCode.Int16:
			return OpCodes.Stelem_I2;
		case TypeCode.UInt16:
			return OpCodes.Stelem_I2;
		case TypeCode.Int32:
			return OpCodes.Stelem_I4;
		case TypeCode.UInt32:
			return OpCodes.Stelem_I4;
		case TypeCode.Int64:
			return OpCodes.Stelem_I8;
		case TypeCode.UInt64:
			return OpCodes.Stelem_I8;
		case TypeCode.Single:
			return OpCodes.Stelem_R4;
		case TypeCode.Double:
			return OpCodes.Stelem_R8;
		case TypeCode.String:
			return OpCodes.Stelem_Ref;
		default:
			return OpCodes.Nop;
		}
	}

	internal void Stelem(Type arrayElementType)
	{
		if (arrayElementType.IsEnum)
		{
			Stelem(Enum.GetUnderlyingType(arrayElementType));
			return;
		}
		OpCode stelemOpCode = GetStelemOpCode(Type.GetTypeCode(arrayElementType));
		if (stelemOpCode.Equals(OpCodes.Nop))
		{
			throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ArrayTypeIsNotSupported_GeneratingCode, DataContract.GetClrTypeFullName(arrayElementType)));
		}
		_ilGen.Emit(stelemOpCode);
	}

	internal Label DefineLabel()
	{
		return _ilGen.DefineLabel();
	}

	internal void MarkLabel(Label label)
	{
		_ilGen.MarkLabel(label);
	}

	internal void Add()
	{
		_ilGen.Emit(OpCodes.Add);
	}

	internal void Subtract()
	{
		_ilGen.Emit(OpCodes.Sub);
	}

	internal void And()
	{
		_ilGen.Emit(OpCodes.And);
	}

	internal void Or()
	{
		_ilGen.Emit(OpCodes.Or);
	}

	internal void Not()
	{
		_ilGen.Emit(OpCodes.Not);
	}

	internal void Ret()
	{
		_ilGen.Emit(OpCodes.Ret);
	}

	internal void Br(Label label)
	{
		_ilGen.Emit(OpCodes.Br, label);
	}

	internal void Blt(Label label)
	{
		_ilGen.Emit(OpCodes.Blt, label);
	}

	internal void Brfalse(Label label)
	{
		_ilGen.Emit(OpCodes.Brfalse, label);
	}

	internal void Brtrue(Label label)
	{
		_ilGen.Emit(OpCodes.Brtrue, label);
	}

	internal void Pop()
	{
		_ilGen.Emit(OpCodes.Pop);
	}

	internal void Dup()
	{
		_ilGen.Emit(OpCodes.Dup);
	}

	private void LoadThis(object thisObj, MethodInfo methodInfo)
	{
		if (thisObj != null && !methodInfo.IsStatic)
		{
			LoadAddress(thisObj);
			ConvertAddress(GetVariableType(thisObj), methodInfo.DeclaringType);
		}
	}

	private void LoadParam(object arg, int oneBasedArgIndex, MethodInfo methodInfo)
	{
		Load(arg);
		if (arg != null)
		{
			ConvertValue(GetVariableType(arg), methodInfo.GetParameters()[oneBasedArgIndex - 1].ParameterType);
		}
	}

	private void InternalIf(bool negate)
	{
		IfState ifState = new IfState();
		ifState.EndIf = DefineLabel();
		ifState.ElseBegin = DefineLabel();
		if (negate)
		{
			Brtrue(ifState.ElseBegin);
		}
		else
		{
			Brfalse(ifState.ElseBegin);
		}
		_blockStack.Push(ifState);
	}

	private static OpCode GetConvOpCode(TypeCode typeCode)
	{
		return typeCode switch
		{
			TypeCode.Boolean => OpCodes.Conv_I1, 
			TypeCode.Char => OpCodes.Conv_I2, 
			TypeCode.SByte => OpCodes.Conv_I1, 
			TypeCode.Byte => OpCodes.Conv_U1, 
			TypeCode.Int16 => OpCodes.Conv_I2, 
			TypeCode.UInt16 => OpCodes.Conv_U2, 
			TypeCode.Int32 => OpCodes.Conv_I4, 
			TypeCode.UInt32 => OpCodes.Conv_U4, 
			TypeCode.Int64 => OpCodes.Conv_I8, 
			TypeCode.UInt64 => OpCodes.Conv_I8, 
			TypeCode.Single => OpCodes.Conv_R4, 
			TypeCode.Double => OpCodes.Conv_R8, 
			_ => OpCodes.Nop, 
		};
	}

	private void InternalConvert(Type source, Type target, bool isAddress)
	{
		if (target == source)
		{
			return;
		}
		if (target.IsValueType)
		{
			if (source.IsValueType)
			{
				OpCode convOpCode = GetConvOpCode(Type.GetTypeCode(target));
				if (convOpCode.Equals(OpCodes.Nop))
				{
					throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.NoConversionPossibleTo, DataContract.GetClrTypeFullName(target)));
				}
				_ilGen.Emit(convOpCode);
				return;
			}
			if (!source.IsAssignableFrom(target))
			{
				throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.IsNotAssignableFrom, DataContract.GetClrTypeFullName(target), DataContract.GetClrTypeFullName(source)));
			}
			Unbox(target);
			if (!isAddress)
			{
				Ldobj(target);
			}
		}
		else if (target.IsAssignableFrom(source))
		{
			if (source.IsValueType)
			{
				if (isAddress)
				{
					Ldobj(source);
				}
				Box(source);
			}
		}
		else if (source.IsAssignableFrom(target))
		{
			Castclass(target);
		}
		else
		{
			if (!target.IsInterface && !source.IsInterface)
			{
				throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.IsNotAssignableFrom, DataContract.GetClrTypeFullName(target), DataContract.GetClrTypeFullName(source)));
			}
			Castclass(target);
		}
	}

	private IfState PopIfState()
	{
		object obj = _blockStack.Pop();
		IfState ifState = obj as IfState;
		if (ifState == null)
		{
			ThrowMismatchException(obj);
		}
		return ifState;
	}

	[DoesNotReturn]
	private static void ThrowMismatchException(object expected)
	{
		throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ExpectingEnd, expected.ToString()));
	}

	internal Label[] Switch(int labelCount)
	{
		SwitchState switchState = new SwitchState(DefineLabel(), DefineLabel());
		Label[] array = new Label[labelCount];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = DefineLabel();
		}
		_ilGen.Emit(OpCodes.Switch, array);
		Br(switchState.DefaultLabel);
		_blockStack.Push(switchState);
		return array;
	}

	internal void Case(Label caseLabel1)
	{
		MarkLabel(caseLabel1);
	}

	internal void EndCase()
	{
		object obj = _blockStack.Peek();
		SwitchState switchState = obj as SwitchState;
		if (switchState == null)
		{
			ThrowMismatchException(obj);
		}
		Br(switchState.EndOfSwitchLabel);
	}

	internal void EndSwitch()
	{
		object obj = _blockStack.Pop();
		SwitchState switchState = obj as SwitchState;
		if (switchState == null)
		{
			ThrowMismatchException(obj);
		}
		if (!switchState.DefaultDefined)
		{
			MarkLabel(switchState.DefaultLabel);
		}
		MarkLabel(switchState.EndOfSwitchLabel);
	}

	internal void ElseIfIsEmptyString(LocalBuilder strLocal)
	{
		IfState ifState = (IfState)_blockStack.Pop();
		Br(ifState.EndIf);
		MarkLabel(ifState.ElseBegin);
		Load(strLocal);
		Call(s_stringLength);
		Load(0);
		ifState.ElseBegin = DefineLabel();
		_ilGen.Emit(GetBranchCode(Cmp.EqualTo), ifState.ElseBegin);
		_blockStack.Push(ifState);
	}

	internal void IfNotIsEmptyString(LocalBuilder strLocal)
	{
		Load(strLocal);
		Call(s_stringLength);
		Load(0);
		If(Cmp.NotEqualTo);
	}

	internal void BeginWhileCondition()
	{
		Label label = DefineLabel();
		MarkLabel(label);
		_blockStack.Push(label);
	}

	internal void BeginWhileBody(Cmp cmpOp)
	{
		Label label = (Label)_blockStack.Pop();
		If(cmpOp);
		_blockStack.Push(label);
	}

	internal void EndWhile()
	{
		Label label = (Label)_blockStack.Pop();
		Br(label);
		EndIf();
	}

	internal void CallStringFormat(string msg, params object[] values)
	{
		NewArray(typeof(object), values.Length);
		if (_stringFormatArray == null)
		{
			_stringFormatArray = DeclareLocal(typeof(object[]));
		}
		Stloc(_stringFormatArray);
		for (int i = 0; i < values.Length; i++)
		{
			StoreArrayElement(_stringFormatArray, i, values[i]);
		}
		Load(msg);
		Load(_stringFormatArray);
		Call(StringFormat);
	}

	internal void ToString(Type type)
	{
		if (type != Globals.TypeOfString)
		{
			if (type.IsValueType)
			{
				Box(type);
			}
			Call(ObjectToString);
		}
	}
}
