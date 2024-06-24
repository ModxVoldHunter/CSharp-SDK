using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Linq;

[RequiresUnreferencedCode("Enumerating in-memory collections as IQueryable can require unreferenced code because expressions referencing IQueryable extension methods can get rebound to IEnumerable extension methods. The IEnumerable extension methods could be trimmed causing the application to fail at runtime.")]
[RequiresDynamicCode("Requires MakeGenericType")]
internal sealed class EnumerableRewriter : ExpressionVisitor
{
	private Dictionary<LabelTarget, LabelTarget> _targetCache;

	private Dictionary<Type, Type> _equivalentTypeCache;

	private static ILookup<string, MethodInfo> s_seqMethods;

	protected override Expression VisitMethodCall(MethodCallExpression m)
	{
		Expression expression = Visit(m.Object);
		ReadOnlyCollection<Expression> readOnlyCollection = Visit(m.Arguments);
		if (expression != m.Object || readOnlyCollection != m.Arguments)
		{
			MethodInfo method = m.Method;
			Type[] typeArgs = (method.IsGenericMethod ? method.GetGenericArguments() : null);
			if ((method.IsStatic || method.DeclaringType.IsAssignableFrom(expression.Type)) && ArgsMatch(method, readOnlyCollection, typeArgs))
			{
				return Expression.Call(expression, method, readOnlyCollection);
			}
			if (method.DeclaringType == typeof(Queryable))
			{
				MethodInfo methodInfo = FindEnumerableMethodForQueryable(method.Name, readOnlyCollection, typeArgs);
				readOnlyCollection = FixupQuotedArgs(methodInfo, readOnlyCollection);
				return Expression.Call(expression, methodInfo, readOnlyCollection);
			}
			MethodInfo methodInfo2 = FindMethod(method.DeclaringType, method.Name, readOnlyCollection, typeArgs);
			readOnlyCollection = FixupQuotedArgs(methodInfo2, readOnlyCollection);
			return Expression.Call(expression, methodInfo2, readOnlyCollection);
		}
		return m;
	}

	private static ReadOnlyCollection<Expression> FixupQuotedArgs(MethodInfo mi, ReadOnlyCollection<Expression> argList)
	{
		ParameterInfo[] parameters = mi.GetParameters();
		if (parameters.Length != 0)
		{
			List<Expression> list = null;
			int i = 0;
			for (int num = parameters.Length; i < num; i++)
			{
				Expression expression = argList[i];
				ParameterInfo parameterInfo = parameters[i];
				expression = FixupQuotedExpression(parameterInfo.ParameterType, expression);
				if (list == null && expression != argList[i])
				{
					list = new List<Expression>(argList.Count);
					for (int j = 0; j < i; j++)
					{
						list.Add(argList[j]);
					}
				}
				list?.Add(expression);
			}
			if (list != null)
			{
				argList = list.AsReadOnly();
			}
		}
		return argList;
	}

	private static Expression FixupQuotedExpression(Type type, Expression expression)
	{
		Expression expression2 = expression;
		while (true)
		{
			if (type.IsAssignableFrom(expression2.Type))
			{
				return expression2;
			}
			if (expression2.NodeType != ExpressionType.Quote)
			{
				break;
			}
			expression2 = ((UnaryExpression)expression2).Operand;
		}
		if (!type.IsAssignableFrom(expression2.Type) && type.IsArray && expression2.NodeType == ExpressionType.NewArrayInit)
		{
			Type c = StripExpression(expression2.Type);
			if (type.IsAssignableFrom(c))
			{
				Type elementType = type.GetElementType();
				NewArrayExpression newArrayExpression = (NewArrayExpression)expression2;
				List<Expression> list = new List<Expression>(newArrayExpression.Expressions.Count);
				int i = 0;
				for (int count = newArrayExpression.Expressions.Count; i < count; i++)
				{
					list.Add(FixupQuotedExpression(elementType, newArrayExpression.Expressions[i]));
				}
				expression = Expression.NewArrayInit(elementType, list);
			}
		}
		return expression;
	}

	protected override Expression VisitLambda<T>(Expression<T> node)
	{
		return node;
	}

	private static Type GetPublicType(Type t)
	{
		if (t.IsGenericType && t.GetGenericTypeDefinition().GetInterfaces().Contains<Type>(typeof(IGrouping<, >)))
		{
			return typeof(IGrouping<, >).MakeGenericType(t.GetGenericArguments());
		}
		if (!t.IsNestedPrivate)
		{
			return t;
		}
		Type[] interfaces = t.GetInterfaces();
		foreach (Type type in interfaces)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
			{
				return type;
			}
		}
		if (typeof(IEnumerable).IsAssignableFrom(t))
		{
			return typeof(IEnumerable);
		}
		return t;
	}

	private Type GetEquivalentType(Type type)
	{
		if (_equivalentTypeCache == null)
		{
			_equivalentTypeCache = new Dictionary<Type, Type>
			{
				{
					typeof(IQueryable),
					typeof(IEnumerable)
				},
				{
					typeof(IEnumerable),
					typeof(IEnumerable)
				}
			};
		}
		if (!_equivalentTypeCache.TryGetValue(type, out var value))
		{
			Type publicType = GetPublicType(type);
			if (publicType.IsInterface && publicType.IsGenericType)
			{
				Type genericTypeDefinition = publicType.GetGenericTypeDefinition();
				if (genericTypeDefinition == typeof(IOrderedEnumerable<>))
				{
					value = publicType;
				}
				else if (genericTypeDefinition == typeof(IOrderedQueryable<>))
				{
					value = typeof(IOrderedEnumerable<>).MakeGenericType(publicType.GenericTypeArguments[0]);
				}
				else if (genericTypeDefinition == typeof(IEnumerable<>))
				{
					value = publicType;
				}
				else if (genericTypeDefinition == typeof(IQueryable<>))
				{
					value = typeof(IEnumerable<>).MakeGenericType(publicType.GenericTypeArguments[0]);
				}
			}
			if (value == null)
			{
				Type[] interfaces = publicType.GetInterfaces();
				var source = (from i in interfaces
					where i.IsGenericType && i.GenericTypeArguments.Length == 1
					select new
					{
						Info = i,
						GenType = i.GetGenericTypeDefinition()
					}).ToArray();
				Type type2 = (from i in source
					where i.GenType == typeof(IOrderedQueryable<>) || i.GenType == typeof(IOrderedEnumerable<>)
					select i.Info.GenericTypeArguments[0]).Distinct().SingleOrDefault();
				if (type2 != null)
				{
					value = typeof(IOrderedEnumerable<>).MakeGenericType(type2);
				}
				else
				{
					type2 = (from i in source
						where i.GenType == typeof(IQueryable<>) || i.GenType == typeof(IEnumerable<>)
						select i.Info.GenericTypeArguments[0]).Distinct().Single();
					value = typeof(IEnumerable<>).MakeGenericType(type2);
				}
			}
			_equivalentTypeCache.Add(type, value);
		}
		return value;
	}

	protected override Expression VisitConstant(ConstantExpression c)
	{
		if (c.Value is EnumerableQuery enumerableQuery)
		{
			if (enumerableQuery.Enumerable != null)
			{
				Type publicType = GetPublicType(enumerableQuery.Enumerable.GetType());
				return Expression.Constant(enumerableQuery.Enumerable, publicType);
			}
			Expression expression = enumerableQuery.Expression;
			if (expression != c)
			{
				return Visit(expression);
			}
		}
		return c;
	}

	private static MethodInfo FindEnumerableMethodForQueryable(string name, ReadOnlyCollection<Expression> args, params Type[] typeArgs)
	{
		if (s_seqMethods == null)
		{
			s_seqMethods = GetEnumerableStaticMethods(typeof(Enumerable)).ToLookup((MethodInfo m) => m.Name);
		}
		MethodInfo[] array = s_seqMethods[name].Where((MethodInfo m) => ArgsMatch(m, args, typeArgs)).Select(ApplyTypeArgs).ToArray();
		if (array.Length > 1)
		{
			return DisambiguateMatches(array);
		}
		return array[0];
		[RequiresDynamicCode("Calls System.Reflection.MethodInfo.MakeGenericMethod(params Type[])")]
		MethodInfo ApplyTypeArgs(MethodInfo methodInfo)
		{
			if (typeArgs != null)
			{
				return methodInfo.MakeGenericMethod(typeArgs);
			}
			return methodInfo;
		}
		static bool AreAssignableFromStrict(ParameterInfo[] left, ParameterInfo[] right)
		{
			bool flag = true;
			bool flag2 = true;
			for (int i = 0; i < left.Length; i++)
			{
				Type parameterType = left[i].ParameterType;
				Type parameterType2 = right[i].ParameterType;
				flag = flag && parameterType == parameterType2;
				flag2 = flag2 && parameterType.IsAssignableFrom(parameterType2);
			}
			return !flag && flag2;
		}
		static MethodInfo DisambiguateMatches(MethodInfo[] matchingMethods)
		{
			ParameterInfo[][] array2 = matchingMethods.Select((MethodInfo m) => m.GetParameters()).ToArray();
			for (int j = 0; j < matchingMethods.Length; j++)
			{
				bool flag3 = true;
				for (int k = 0; k < matchingMethods.Length; k++)
				{
					if (j != k && AreAssignableFromStrict(array2[j], array2[k]))
					{
						flag3 = false;
						break;
					}
				}
				if (flag3)
				{
					return matchingMethods[j];
				}
			}
			throw new Exception();
		}
		static MethodInfo[] GetEnumerableStaticMethods(Type type)
		{
			return type.GetMethods(BindingFlags.Static | BindingFlags.Public);
		}
	}

	[RequiresUnreferencedCode("Enumerating in-memory collections as IQueryable can require unreferenced code because expressions referencing IQueryable extension methods can get rebound to IEnumerable extension methods. The IEnumerable extension methods could be trimmed causing the application to fail at runtime.")]
	[RequiresDynamicCode("Calls System.Reflection.MethodInfo.MakeGenericMethod(params Type[])")]
	private static MethodInfo FindMethod(Type type, string name, ReadOnlyCollection<Expression> args, Type[] typeArgs)
	{
		using (IEnumerator<MethodInfo> enumerator = (from m in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
			where m.Name == name
			select m).GetEnumerator())
		{
			if (!enumerator.MoveNext())
			{
				throw Error.NoMethodOnType(name, type);
			}
			do
			{
				MethodInfo current = enumerator.Current;
				if (ArgsMatch(current, args, typeArgs))
				{
					return (typeArgs != null) ? current.MakeGenericMethod(typeArgs) : current;
				}
			}
			while (enumerator.MoveNext());
		}
		throw Error.NoMethodOnTypeMatchingArguments(name, type);
	}

	private static bool ArgsMatch(MethodInfo m, ReadOnlyCollection<Expression> args, Type[] typeArgs)
	{
		ParameterInfo[] array = m.GetParameters();
		if (array.Length != args.Count)
		{
			return false;
		}
		if (!m.IsGenericMethod && typeArgs != null && typeArgs.Length != 0)
		{
			return false;
		}
		if (!m.IsGenericMethodDefinition && m.IsGenericMethod && m.ContainsGenericParameters)
		{
			m = m.GetGenericMethodDefinition();
		}
		if (m.IsGenericMethodDefinition)
		{
			if (typeArgs == null || typeArgs.Length == 0)
			{
				return false;
			}
			if (m.GetGenericArguments().Length != typeArgs.Length)
			{
				return false;
			}
			array = GetConstrutedGenericParameters(m, typeArgs);
		}
		int i = 0;
		for (int count = args.Count; i < count; i++)
		{
			Type type = array[i].ParameterType;
			if (type == null)
			{
				return false;
			}
			if (type.IsByRef)
			{
				type = type.GetElementType();
			}
			Expression expression = args[i];
			if (!type.IsAssignableFrom(expression.Type))
			{
				if (expression.NodeType == ExpressionType.Quote)
				{
					expression = ((UnaryExpression)expression).Operand;
				}
				if (!type.IsAssignableFrom(expression.Type) && !type.IsAssignableFrom(StripExpression(expression.Type)))
				{
					return false;
				}
			}
		}
		return true;
		[RequiresDynamicCode("Calls System.Reflection.MethodInfo.MakeGenericMethod(params Type[])")]
		static ParameterInfo[] GetConstrutedGenericParameters(MethodInfo method, Type[] genericTypes)
		{
			return method.MakeGenericMethod(genericTypes).GetParameters();
		}
	}

	[RequiresDynamicCode("Calls System.Type.MakeArrayType()")]
	private static Type StripExpression(Type type)
	{
		bool isArray = type.IsArray;
		Type type2 = (isArray ? type.GetElementType() : type);
		Type type3 = TypeHelper.FindGenericType(typeof(Expression<>), type2);
		if (type3 != null)
		{
			type2 = type3.GetGenericArguments()[0];
		}
		if (isArray)
		{
			int arrayRank = type.GetArrayRank();
			if (arrayRank != 1)
			{
				return type2.MakeArrayType(arrayRank);
			}
			return type2.MakeArrayType();
		}
		return type;
	}

	protected override Expression VisitConditional(ConditionalExpression c)
	{
		Type type = c.Type;
		if (!typeof(IQueryable).IsAssignableFrom(type))
		{
			return base.VisitConditional(c);
		}
		Expression test = Visit(c.Test);
		Expression expression = Visit(c.IfTrue);
		Expression expression2 = Visit(c.IfFalse);
		Type type2 = expression.Type;
		Type type3 = expression2.Type;
		if (type2.IsAssignableFrom(type3))
		{
			return Expression.Condition(test, expression, expression2, type2);
		}
		if (type3.IsAssignableFrom(type2))
		{
			return Expression.Condition(test, expression, expression2, type3);
		}
		return Expression.Condition(test, expression, expression2, GetEquivalentType(type));
	}

	protected override Expression VisitBlock(BlockExpression node)
	{
		Type type = node.Type;
		if (!typeof(IQueryable).IsAssignableFrom(type))
		{
			return base.VisitBlock(node);
		}
		ReadOnlyCollection<Expression> expressions = Visit(node.Expressions);
		ReadOnlyCollection<ParameterExpression> variables = VisitAndConvert(node.Variables, "EnumerableRewriter.VisitBlock");
		if (type == node.Expressions.Last().Type)
		{
			return Expression.Block(variables, expressions);
		}
		return Expression.Block(GetEquivalentType(type), variables, expressions);
	}

	protected override Expression VisitGoto(GotoExpression node)
	{
		Type type = node.Value.Type;
		if (!typeof(IQueryable).IsAssignableFrom(type))
		{
			return base.VisitGoto(node);
		}
		LabelTarget target = VisitLabelTarget(node.Target);
		Expression expression = Visit(node.Value);
		return Expression.MakeGoto(node.Kind, target, expression, GetEquivalentType(typeof(EnumerableQuery).IsAssignableFrom(type) ? expression.Type : type));
	}

	protected override LabelTarget VisitLabelTarget(LabelTarget node)
	{
		LabelTarget value;
		if (_targetCache == null)
		{
			_targetCache = new Dictionary<LabelTarget, LabelTarget>();
		}
		else if (_targetCache.TryGetValue(node, out value))
		{
			return value;
		}
		Type type = node.Type;
		value = (typeof(IQueryable).IsAssignableFrom(type) ? Expression.Label(GetEquivalentType(type), node.Name) : base.VisitLabelTarget(node));
		_targetCache.Add(node, value);
		return value;
	}
}
