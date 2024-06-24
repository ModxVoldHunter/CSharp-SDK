using System.ComponentModel;

namespace System.Reflection;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class IntrospectionExtensions
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static TypeInfo GetTypeInfo(this Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		if (type is IReflectableType reflectableType)
		{
			return reflectableType.GetTypeInfo();
		}
		return new TypeDelegator(type);
	}
}
