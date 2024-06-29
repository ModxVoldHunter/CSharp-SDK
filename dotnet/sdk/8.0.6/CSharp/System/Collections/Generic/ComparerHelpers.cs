namespace System.Collections.Generic;

internal static class ComparerHelpers
{
	internal static object CreateDefaultComparer(Type type)
	{
		object obj = null;
		RuntimeType genericParameter = (RuntimeType)type;
		if (typeof(IComparable<>).MakeGenericType(type).IsAssignableFrom(type))
		{
			obj = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(GenericComparer<int>), genericParameter);
		}
		else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			RuntimeType genericParameter2 = (RuntimeType)type.GetGenericArguments()[0];
			obj = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(NullableComparer<int>), genericParameter2);
		}
		else if (type.IsEnum)
		{
			obj = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(EnumComparer<>), genericParameter);
		}
		return obj ?? RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(ObjectComparer<object>), genericParameter);
	}

	internal static object CreateDefaultEqualityComparer(Type type)
	{
		object obj = null;
		RuntimeType genericParameter = (RuntimeType)type;
		if (type == typeof(string))
		{
			return new GenericEqualityComparer<string>();
		}
		if (type.IsAssignableTo(typeof(IEquatable<>).MakeGenericType(type)))
		{
			obj = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(GenericEqualityComparer<string>), genericParameter);
		}
		else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			RuntimeType genericParameter2 = (RuntimeType)type.GetGenericArguments()[0];
			obj = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(NullableEqualityComparer<int>), genericParameter2);
		}
		else if (type.IsEnum)
		{
			obj = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(EnumEqualityComparer<>), genericParameter);
		}
		return obj ?? RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(ObjectEqualityComparer<object>), genericParameter);
	}
}
