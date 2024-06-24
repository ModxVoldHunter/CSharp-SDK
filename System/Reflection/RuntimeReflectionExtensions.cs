using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection;

public static class RuntimeReflectionExtensions
{
	public static IEnumerable<FieldInfo> GetRuntimeFields([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] this Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}

	public static IEnumerable<MethodInfo> GetRuntimeMethods([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] this Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}

	public static IEnumerable<PropertyInfo> GetRuntimeProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] this Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}

	public static IEnumerable<EventInfo> GetRuntimeEvents([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)] this Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetEvents(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}

	public static FieldInfo? GetRuntimeField([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] this Type type, string name)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetField(name);
	}

	public static MethodInfo? GetRuntimeMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] this Type type, string name, Type[] parameters)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetMethod(name, parameters);
	}

	public static PropertyInfo? GetRuntimeProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] this Type type, string name)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetProperty(name);
	}

	public static EventInfo? GetRuntimeEvent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)] this Type type, string name)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetEvent(name);
	}

	public static MethodInfo? GetRuntimeBaseDefinition(this MethodInfo method)
	{
		ArgumentNullException.ThrowIfNull(method, "method");
		return method.GetBaseDefinition();
	}

	public static InterfaceMapping GetRuntimeInterfaceMap(this TypeInfo typeInfo, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
	{
		ArgumentNullException.ThrowIfNull(typeInfo, "typeInfo");
		return typeInfo.GetInterfaceMap(interfaceType);
	}

	public static MethodInfo GetMethodInfo(this Delegate del)
	{
		ArgumentNullException.ThrowIfNull(del, "del");
		return del.Method;
	}
}
