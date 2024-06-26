using System.Linq.Expressions;
using System.Reflection;

namespace System.Xml.Serialization;

internal static class ReflectionXmlSerializationReaderHelper
{
	public delegate void SetMemberValueDelegate(object o, object val);

	public static SetMemberValueDelegate GetSetMemberValueDelegateWithType<TObj, TParam>(MemberInfo memberInfo)
	{
		if (typeof(TObj).IsValueType)
		{
			if (memberInfo is PropertyInfo @object)
			{
				return @object.SetValue;
			}
			if (memberInfo is FieldInfo object2)
			{
				return object2.SetValue;
			}
			throw new InvalidOperationException(System.SR.XmlInternalError);
		}
		Action<TObj, TParam> setTypedDelegate = null;
		if (memberInfo is PropertyInfo propertyInfo)
		{
			MethodInfo setMethod = propertyInfo.GetSetMethod(nonPublic: true);
			if (setMethod == null)
			{
				return propertyInfo.SetValue;
			}
			setTypedDelegate = (Action<TObj, TParam>)setMethod.CreateDelegate(typeof(Action<TObj, TParam>));
		}
		else if (memberInfo is FieldInfo field)
		{
			ParameterExpression parameterExpression = Expression.Parameter(typeof(TObj));
			ParameterExpression parameterExpression2 = Expression.Parameter(typeof(TParam));
			MemberExpression left = Expression.Field(parameterExpression, field);
			BinaryExpression body = Expression.Assign(left, parameterExpression2);
			setTypedDelegate = Expression.Lambda<Action<TObj, TParam>>(body, new ParameterExpression[2] { parameterExpression, parameterExpression2 }).Compile();
		}
		return delegate(object o, object p)
		{
			setTypedDelegate((TObj)o, (TParam)p);
		};
	}
}
