using System.Collections.Generic;
using System.Linq.Expressions;

namespace System.Dynamic;

public class DynamicMetaObject
{
	public static readonly DynamicMetaObject[] EmptyMetaObjects = Array.Empty<DynamicMetaObject>();

	private static readonly object s_noValueSentinel = new object();

	private readonly object _value = s_noValueSentinel;

	public Expression Expression { get; }

	public BindingRestrictions Restrictions { get; }

	public object? Value
	{
		get
		{
			if (!HasValue)
			{
				return null;
			}
			return _value;
		}
	}

	public bool HasValue => _value != s_noValueSentinel;

	public Type? RuntimeType
	{
		get
		{
			if (HasValue)
			{
				Type type = Expression.Type;
				if (type.IsValueType)
				{
					return type;
				}
				return Value?.GetType();
			}
			return null;
		}
	}

	public Type LimitType => RuntimeType ?? Expression.Type;

	public DynamicMetaObject(Expression expression, BindingRestrictions restrictions)
	{
		ArgumentNullException.ThrowIfNull(expression, "expression");
		ArgumentNullException.ThrowIfNull(restrictions, "restrictions");
		Expression = expression;
		Restrictions = restrictions;
	}

	public DynamicMetaObject(Expression expression, BindingRestrictions restrictions, object value)
		: this(expression, restrictions)
	{
		_value = value;
	}

	public virtual DynamicMetaObject BindConvert(ConvertBinder binder)
	{
		ArgumentNullException.ThrowIfNull(binder, "binder");
		return binder.FallbackConvert(this);
	}

	public virtual DynamicMetaObject BindGetMember(GetMemberBinder binder)
	{
		ArgumentNullException.ThrowIfNull(binder, "binder");
		return binder.FallbackGetMember(this);
	}

	public virtual DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
	{
		ArgumentNullException.ThrowIfNull(binder, "binder");
		return binder.FallbackSetMember(this, value);
	}

	public virtual DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder)
	{
		ArgumentNullException.ThrowIfNull(binder, "binder");
		return binder.FallbackDeleteMember(this);
	}

	public virtual DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
	{
		ArgumentNullException.ThrowIfNull(binder, "binder");
		return binder.FallbackGetIndex(this, indexes);
	}

	public virtual DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
	{
		ArgumentNullException.ThrowIfNull(binder, "binder");
		return binder.FallbackSetIndex(this, indexes, value);
	}

	public virtual DynamicMetaObject BindDeleteIndex(DeleteIndexBinder binder, DynamicMetaObject[] indexes)
	{
		ArgumentNullException.ThrowIfNull(binder, "binder");
		return binder.FallbackDeleteIndex(this, indexes);
	}

	public virtual DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
	{
		ArgumentNullException.ThrowIfNull(binder, "binder");
		return binder.FallbackInvokeMember(this, args);
	}

	public virtual DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
	{
		ArgumentNullException.ThrowIfNull(binder, "binder");
		return binder.FallbackInvoke(this, args);
	}

	public virtual DynamicMetaObject BindCreateInstance(CreateInstanceBinder binder, DynamicMetaObject[] args)
	{
		ArgumentNullException.ThrowIfNull(binder, "binder");
		return binder.FallbackCreateInstance(this, args);
	}

	public virtual DynamicMetaObject BindUnaryOperation(UnaryOperationBinder binder)
	{
		ArgumentNullException.ThrowIfNull(binder, "binder");
		return binder.FallbackUnaryOperation(this);
	}

	public virtual DynamicMetaObject BindBinaryOperation(BinaryOperationBinder binder, DynamicMetaObject arg)
	{
		ArgumentNullException.ThrowIfNull(binder, "binder");
		return binder.FallbackBinaryOperation(this, arg);
	}

	public virtual IEnumerable<string> GetDynamicMemberNames()
	{
		return Array.Empty<string>();
	}

	internal static Expression[] GetExpressions(DynamicMetaObject[] objects)
	{
		ArgumentNullException.ThrowIfNull(objects, "objects");
		Expression[] array = new Expression[objects.Length];
		for (int i = 0; i < objects.Length; i++)
		{
			DynamicMetaObject dynamicMetaObject = objects[i];
			ArgumentNullException.ThrowIfNull(dynamicMetaObject, "objects");
			Expression expression = dynamicMetaObject.Expression;
			array[i] = expression;
		}
		return array;
	}

	public static DynamicMetaObject Create(object value, Expression expression)
	{
		ArgumentNullException.ThrowIfNull(expression, "expression");
		if (value is IDynamicMetaObjectProvider dynamicMetaObjectProvider)
		{
			DynamicMetaObject metaObject = dynamicMetaObjectProvider.GetMetaObject(expression);
			if (metaObject == null || !metaObject.HasValue || metaObject.Value == null || metaObject.Expression != expression)
			{
				throw Error.InvalidMetaObjectCreated(dynamicMetaObjectProvider.GetType());
			}
			return metaObject;
		}
		return new DynamicMetaObject(expression, BindingRestrictions.Empty, value);
	}
}
