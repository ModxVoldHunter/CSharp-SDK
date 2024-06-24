using System.Diagnostics.CodeAnalysis;

namespace System.Reflection;

public abstract class DispatchProxy
{
	protected abstract object? Invoke(MethodInfo? targetMethod, object?[]? args);

	[RequiresDynamicCode("Creating a proxy instance requires generating code at runtime")]
	public static T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TProxy>() where TProxy : DispatchProxy
	{
		return (T)DispatchProxyGenerator.CreateProxyInstance(typeof(TProxy), typeof(T), "T", "TProxy");
	}

	[RequiresDynamicCode("Creating a proxy instance requires generating code at runtime")]
	public static object Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type proxyType)
	{
		ArgumentNullException.ThrowIfNull(interfaceType, "interfaceType");
		ArgumentNullException.ThrowIfNull(proxyType, "proxyType");
		if (!proxyType.IsAssignableTo(typeof(DispatchProxy)))
		{
			throw new ArgumentException(System.SR.Format(System.SR.ProxyType_Must_Be_Derived_From_DispatchProxy, proxyType.Name), "proxyType");
		}
		return DispatchProxyGenerator.CreateProxyInstance(proxyType, interfaceType, "interfaceType", "proxyType");
	}
}
