using System.Reflection;

namespace System.Runtime.Serialization;

internal static class SerializationInfoExtensions
{
	private static readonly Action<SerializationInfo, string, object, Type> s_updateValue = typeof(SerializationInfo).GetMethod("UpdateValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).CreateDelegate<Action<SerializationInfo, string, object, Type>>();

	public static void UpdateValue(this SerializationInfo si, string name, object value, Type type)
	{
		s_updateValue(si, name, value, type);
	}
}
