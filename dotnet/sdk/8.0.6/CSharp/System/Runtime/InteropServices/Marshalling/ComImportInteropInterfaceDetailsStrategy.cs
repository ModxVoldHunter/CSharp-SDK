using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling;

[RequiresDynamicCode("Enabling interop between source-generated and built-in COM is not supported when trimming is enabled.")]
[RequiresUnreferencedCode("Enabling interop between source-generated and built-in COM requires dynamic code generation.")]
internal sealed class ComImportInteropInterfaceDetailsStrategy : IIUnknownInterfaceDetailsStrategy
{
	private sealed class ComImportDetails : IIUnknownDerivedDetails
	{
		public Guid Iid { get; }

		public Type Implementation { get; }

		public unsafe void** ManagedVirtualMethodTable => null;

		public ComImportDetails(Guid iid, Type implementation)
		{
			Iid = iid;
			Implementation = implementation;
			base._002Ector();
		}
	}

	internal interface IComImportAdapter
	{
		internal static readonly MethodInfo GetRuntimeCallableWrapperMethod = typeof(IComImportAdapter).GetMethod("GetRuntimeCallableWrapper");

		object GetRuntimeCallableWrapper();
	}

	public static readonly IIUnknownInterfaceDetailsStrategy Instance = new System.Runtime.InteropServices.Marshalling.ComImportInteropInterfaceDetailsStrategy();

	private readonly ConditionalWeakTable<Type, Type> _forwarderInterfaceCache = new ConditionalWeakTable<Type, Type>();

	private static readonly ConstructorInfo s_attributeBaseClassCtor = typeof(Attribute).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];

	private static readonly ConstructorInfo s_attributeUsageCtor = typeof(AttributeUsageAttribute).GetConstructor(new Type[1] { typeof(AttributeTargets) });

	private static readonly PropertyInfo s_attributeUsageAllowMultipleProperty = typeof(AttributeUsageAttribute).GetProperty("AllowMultiple");

	public IComExposedDetails GetComExposedTypeDetails(RuntimeTypeHandle type)
	{
		return System.Runtime.InteropServices.Marshalling.DefaultIUnknownInterfaceDetailsStrategy.Instance.GetComExposedTypeDetails(type);
	}

	public IIUnknownDerivedDetails GetIUnknownDerivedDetails(RuntimeTypeHandle type)
	{
		Type typeFromHandle = Type.GetTypeFromHandle(type);
		if (!typeFromHandle.IsImport)
		{
			return System.Runtime.InteropServices.Marshalling.DefaultIUnknownInterfaceDetailsStrategy.Instance.GetIUnknownDerivedDetails(type);
		}
		Type value = _forwarderInterfaceCache.GetValue(typeFromHandle, delegate(Type runtimeType)
		{
			AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ComImportForwarder"), (!runtimeType.IsCollectible) ? AssemblyBuilderAccess.Run : AssemblyBuilderAccess.RunAndCollect);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("ComImportForwarder");
			ConstructorInfo ignoresAccessChecksToAttributeConstructor = GetIgnoresAccessChecksToAttributeConstructor(moduleBuilder);
			assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(ignoresAccessChecksToAttributeConstructor, new object[1] { typeof(IComImportAdapter).Assembly.GetName().Name }));
			TypeBuilder typeBuilder = moduleBuilder.DefineType("InterfaceForwarder", TypeAttributes.ClassSemanticsMask | TypeAttributes.Abstract, null, runtimeType.GetInterfaces());
			typeBuilder.AddInterfaceImplementation(runtimeType);
			typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(DynamicInterfaceCastableImplementationAttribute).GetConstructor(Array.Empty<Type>()), Array.Empty<object>()));
			Type[] interfaces = typeBuilder.GetInterfaces();
			foreach (Type type2 in interfaces)
			{
				assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(ignoresAccessChecksToAttributeConstructor, new object[1] { type2.Assembly.GetName().Name }));
				MethodInfo[] methods = type2.GetMethods();
				foreach (MethodInfo methodInfo in methods)
				{
					Type[] optionalCustomModifiers = methodInfo.ReturnParameter.GetOptionalCustomModifiers();
					Type[] requiredCustomModifiers = methodInfo.ReturnParameter.GetRequiredCustomModifiers();
					ParameterInfo[] parameters = methodInfo.GetParameters();
					Type[] array = new Type[parameters.Length];
					Type[][] array2 = new Type[parameters.Length][];
					Type[][] array3 = new Type[parameters.Length][];
					for (int k = 0; k < parameters.Length; k++)
					{
						array[k] = parameters[k].ParameterType;
						array2[k] = parameters[k].GetOptionalCustomModifiers();
						array3[k] = parameters[k].GetRequiredCustomModifiers();
					}
					MethodBuilder methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig, CallingConventions.HasThis, methodInfo.ReturnType, requiredCustomModifiers, optionalCustomModifiers, array, array3, array2);
					ILGenerator iLGenerator = methodBuilder.GetILGenerator();
					iLGenerator.Emit(OpCodes.Ldarg_0);
					iLGenerator.Emit(OpCodes.Castclass, typeof(IComImportAdapter));
					iLGenerator.Emit(OpCodes.Callvirt, IComImportAdapter.GetRuntimeCallableWrapperMethod);
					iLGenerator.Emit(OpCodes.Castclass, type2);
					for (int l = 0; l < parameters.Length; l++)
					{
						iLGenerator.Emit(OpCodes.Ldarg, l + 1);
					}
					iLGenerator.Emit(OpCodes.Callvirt, methodInfo);
					iLGenerator.Emit(OpCodes.Ret);
					typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
				}
			}
			return typeBuilder.CreateType();
		});
		return new ComImportDetails(typeFromHandle.GUID, value);
	}

	private static ConstructorInfo GetIgnoresAccessChecksToAttributeConstructor(ModuleBuilder moduleBuilder)
	{
		Type type = EmitIgnoresAccessChecksToAttribute(moduleBuilder);
		return type.GetConstructor(new Type[1] { typeof(string) });
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	private static Type EmitIgnoresAccessChecksToAttribute(ModuleBuilder moduleBuilder)
	{
		TypeBuilder typeBuilder = moduleBuilder.DefineType("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute", TypeAttributes.NotPublic, typeof(Attribute));
		CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(s_attributeUsageCtor, new object[1] { AttributeTargets.Assembly }, new PropertyInfo[1] { s_attributeUsageAllowMultipleProperty }, new object[1] { true });
		typeBuilder.SetCustomAttribute(customAttribute);
		ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard, new Type[1] { typeof(string) });
		constructorBuilder.DefineParameter(1, ParameterAttributes.None, "assemblyName");
		ILGenerator iLGenerator = constructorBuilder.GetILGenerator();
		iLGenerator.Emit(OpCodes.Ldarg_0);
		iLGenerator.Emit(OpCodes.Call, s_attributeBaseClassCtor);
		iLGenerator.Emit(OpCodes.Ret);
		return typeBuilder.CreateType();
	}
}
