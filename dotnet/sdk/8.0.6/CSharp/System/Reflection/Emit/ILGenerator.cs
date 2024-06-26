using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit;

public abstract class ILGenerator
{
	private static readonly Type[] s_parameterTypes = new Type[1] { typeof(string) };

	public abstract int ILOffset { get; }

	public abstract void Emit(OpCode opcode);

	public abstract void Emit(OpCode opcode, byte arg);

	public abstract void Emit(OpCode opcode, short arg);

	public abstract void Emit(OpCode opcode, long arg);

	public abstract void Emit(OpCode opcode, float arg);

	public abstract void Emit(OpCode opcode, double arg);

	public abstract void Emit(OpCode opcode, int arg);

	public abstract void Emit(OpCode opcode, MethodInfo meth);

	public abstract void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, Type[]? optionalParameterTypes);

	public abstract void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type? returnType, Type[]? parameterTypes);

	public abstract void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes);

	public abstract void Emit(OpCode opcode, SignatureHelper signature);

	public abstract void Emit(OpCode opcode, ConstructorInfo con);

	public abstract void Emit(OpCode opcode, Type cls);

	public abstract void Emit(OpCode opcode, Label label);

	public abstract void Emit(OpCode opcode, Label[] labels);

	public abstract void Emit(OpCode opcode, FieldInfo field);

	public abstract void Emit(OpCode opcode, string str);

	public abstract void Emit(OpCode opcode, LocalBuilder local);

	public abstract Label BeginExceptionBlock();

	public abstract void EndExceptionBlock();

	public abstract void BeginExceptFilterBlock();

	public abstract void BeginCatchBlock(Type? exceptionType);

	public abstract void BeginFaultBlock();

	public abstract void BeginFinallyBlock();

	public abstract Label DefineLabel();

	public abstract void MarkLabel(Label loc);

	public virtual void ThrowException([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type excType)
	{
		ArgumentNullException.ThrowIfNull(excType, "excType");
		if (!excType.IsSubclassOf(typeof(Exception)) && excType != typeof(Exception))
		{
			throw new ArgumentException(SR.Argument_NotExceptionType, "excType");
		}
		ConstructorInfo constructor = excType.GetConstructor(Type.EmptyTypes);
		if (constructor == null)
		{
			throw new ArgumentException(SR.Arg_NoDefCTorWithoutTypeName, "excType");
		}
		Emit(OpCodes.Newobj, constructor);
		Emit(OpCodes.Throw);
	}

	public virtual void EmitWriteLine(string value)
	{
		Emit(OpCodes.Ldstr, value);
		Type type = Type.GetType("System.Console, System.Console", throwOnError: true);
		MethodInfo method = type.GetMethod("WriteLine", s_parameterTypes);
		Emit(OpCodes.Call, method);
	}

	public virtual void EmitWriteLine(LocalBuilder localBuilder)
	{
		Type type = Type.GetType("System.Console, System.Console", throwOnError: true);
		MethodInfo method = type.GetMethod("get_Out");
		Emit(OpCodes.Call, method);
		Emit(OpCodes.Ldloc, localBuilder);
		Type[] array = new Type[1];
		Type localType = localBuilder.LocalType;
		if (localType is TypeBuilder || localType is EnumBuilder)
		{
			throw new ArgumentException(SR.NotSupported_OutputStreamUsingTypeBuilder);
		}
		array[0] = localType;
		MethodInfo method2 = typeof(TextWriter).GetMethod("WriteLine", array);
		if (method2 == null)
		{
			throw new ArgumentException(SR.Argument_EmitWriteLineType, "localBuilder");
		}
		Emit(OpCodes.Callvirt, method2);
	}

	public virtual void EmitWriteLine(FieldInfo fld)
	{
		ArgumentNullException.ThrowIfNull(fld, "fld");
		Type type = Type.GetType("System.Console, System.Console", throwOnError: true);
		MethodInfo method = type.GetMethod("get_Out");
		Emit(OpCodes.Call, method);
		if ((fld.Attributes & FieldAttributes.Static) != 0)
		{
			Emit(OpCodes.Ldsfld, fld);
		}
		else
		{
			Emit(OpCodes.Ldarg_0);
			Emit(OpCodes.Ldfld, fld);
		}
		Type[] array = new Type[1];
		Type fieldType = fld.FieldType;
		if (fieldType is TypeBuilder || fieldType is EnumBuilder)
		{
			throw new NotSupportedException(SR.NotSupported_OutputStreamUsingTypeBuilder);
		}
		array[0] = fieldType;
		MethodInfo method2 = typeof(TextWriter).GetMethod("WriteLine", array);
		if (method2 == null)
		{
			throw new ArgumentException(SR.Argument_EmitWriteLineType, "fld");
		}
		Emit(OpCodes.Callvirt, method2);
	}

	public virtual LocalBuilder DeclareLocal(Type localType)
	{
		return DeclareLocal(localType, pinned: false);
	}

	public abstract LocalBuilder DeclareLocal(Type localType, bool pinned);

	public abstract void UsingNamespace(string usingNamespace);

	public abstract void BeginScope();

	public abstract void EndScope();

	[CLSCompliant(false)]
	public void Emit(OpCode opcode, sbyte arg)
	{
		Emit(opcode, (byte)arg);
	}
}
