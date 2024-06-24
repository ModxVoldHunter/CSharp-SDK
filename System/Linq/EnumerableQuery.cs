using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace System.Linq;

public abstract class EnumerableQuery
{
	internal abstract Expression Expression { get; }

	internal abstract IEnumerable? Enumerable { get; }

	internal EnumerableQuery()
	{
	}

	[RequiresUnreferencedCode("Enumerating in-memory collections as IQueryable can require unreferenced code because expressions referencing IQueryable extension methods can get rebound to IEnumerable extension methods. The IEnumerable extension methods could be trimmed causing the application to fail at runtime.")]
	[RequiresDynamicCode("Enumerating in-memory collections as IQueryable can require creating new generic types or methods, which requires creating code at runtime. This may not work when AOT compiling.")]
	internal static IQueryable Create(Type elementType, IEnumerable sequence)
	{
		Type type = typeof(EnumerableQuery<>).MakeGenericType(elementType);
		return (IQueryable)Activator.CreateInstance(type, sequence);
	}

	[RequiresUnreferencedCode("Enumerating in-memory collections as IQueryable can require unreferenced code because expressions referencing IQueryable extension methods can get rebound to IEnumerable extension methods. The IEnumerable extension methods could be trimmed causing the application to fail at runtime.")]
	[RequiresDynamicCode("Enumerating in-memory collections as IQueryable can require creating new generic types or methods, which requires creating code at runtime. This may not work when AOT compiling.")]
	internal static IQueryable Create(Type elementType, Expression expression)
	{
		Type type = typeof(EnumerableQuery<>).MakeGenericType(elementType);
		return (IQueryable)Activator.CreateInstance(type, expression);
	}
}
[RequiresDynamicCode("Enumerating in-memory collections as IQueryable can require creating new generic types or methods, which requires creating code at runtime. This may not work when AOT compiling.")]
[RequiresUnreferencedCode("Enumerating in-memory collections as IQueryable can require unreferenced code because expressions referencing IQueryable extension methods can get rebound to IEnumerable extension methods. The IEnumerable extension methods could be trimmed causing the application to fail at runtime.")]
public class EnumerableQuery<T> : EnumerableQuery, IOrderedQueryable<T>, IEnumerable<T>, IEnumerable, IOrderedQueryable, IQueryable, IQueryable<T>, IQueryProvider
{
	private readonly Expression _expression;

	private IEnumerable<T> _enumerable;

	IQueryProvider IQueryable.Provider => this;

	internal override Expression Expression => _expression;

	internal override IEnumerable? Enumerable => _enumerable;

	Expression IQueryable.Expression => _expression;

	Type IQueryable.ElementType => typeof(T);

	public EnumerableQuery(IEnumerable<T> enumerable)
	{
		_enumerable = enumerable;
		_expression = System.Linq.Expressions.Expression.Constant(this);
	}

	public EnumerableQuery(Expression expression)
	{
		_expression = expression;
	}

	IQueryable IQueryProvider.CreateQuery(Expression expression)
	{
		ArgumentNullException.ThrowIfNull(expression, "expression");
		Type type = TypeHelper.FindGenericType(typeof(IQueryable<>), expression.Type);
		if (type == null)
		{
			throw Error.ArgumentNotValid("expression");
		}
		return EnumerableQuery.Create(type.GetGenericArguments()[0], expression);
	}

	IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
	{
		ArgumentNullException.ThrowIfNull(expression, "expression");
		if (!typeof(IQueryable<TElement>).IsAssignableFrom(expression.Type))
		{
			throw Error.ArgumentNotValid("expression");
		}
		return new EnumerableQuery<TElement>(expression);
	}

	object IQueryProvider.Execute(Expression expression)
	{
		ArgumentNullException.ThrowIfNull(expression, "expression");
		return EnumerableExecutor.Create(expression).ExecuteBoxed();
	}

	TElement IQueryProvider.Execute<TElement>(Expression expression)
	{
		ArgumentNullException.ThrowIfNull(expression, "expression");
		if (!typeof(TElement).IsAssignableFrom(expression.Type))
		{
			throw Error.ArgumentNotValid("expression");
		}
		return new EnumerableExecutor<TElement>(expression).Execute();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return GetEnumerator();
	}

	private IEnumerator<T> GetEnumerator()
	{
		if (_enumerable == null)
		{
			EnumerableRewriter enumerableRewriter = new EnumerableRewriter();
			Expression body = enumerableRewriter.Visit(_expression);
			Expression<Func<IEnumerable<T>>> expression = System.Linq.Expressions.Expression.Lambda<Func<IEnumerable<T>>>(body, (IEnumerable<ParameterExpression>?)null);
			IEnumerable<T> enumerable = expression.Compile()();
			if (enumerable == this)
			{
				throw Error.EnumeratingNullEnumerableExpression();
			}
			_enumerable = enumerable;
		}
		return _enumerable.GetEnumerator();
	}

	public override string? ToString()
	{
		if (_expression is ConstantExpression constantExpression && constantExpression.Value == this)
		{
			if (_enumerable != null)
			{
				return _enumerable.ToString();
			}
			return "null";
		}
		return _expression.ToString();
	}
}
