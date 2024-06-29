using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace System.Linq;

public static class Queryable
{
	[RequiresUnreferencedCode("Enumerating in-memory collections as IQueryable can require unreferenced code because expressions referencing IQueryable extension methods can get rebound to IEnumerable extension methods. The IEnumerable extension methods could be trimmed causing the application to fail at runtime.")]
	[RequiresDynamicCode("Enumerating in-memory collections as IQueryable can require creating new generic types or methods, which requires creating code at runtime. This may not work when AOT compiling.")]
	public static IQueryable<TElement> AsQueryable<TElement>(this IEnumerable<TElement> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return (source as IQueryable<TElement>) ?? new EnumerableQuery<TElement>(source);
	}

	[RequiresUnreferencedCode("Enumerating in-memory collections as IQueryable can require unreferenced code because expressions referencing IQueryable extension methods can get rebound to IEnumerable extension methods. The IEnumerable extension methods could be trimmed causing the application to fail at runtime.")]
	[RequiresDynamicCode("Enumerating in-memory collections as IQueryable can require creating new generic types or methods, which requires creating code at runtime. This may not work when AOT compiling.")]
	public static IQueryable AsQueryable(this IEnumerable source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		if (source is IQueryable result)
		{
			return result;
		}
		Type type = TypeHelper.FindGenericType(typeof(IEnumerable<>), source.GetType());
		if (type == null)
		{
			throw Error.ArgumentNotIEnumerableGeneric("source");
		}
		return EnumerableQuery.Create(type.GenericTypeArguments[0], source);
	}

	[DynamicDependency("Where`1", typeof(Enumerable))]
	public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, IQueryable<TSource>>(Where).Method, source.Expression, Expression.Quote(predicate)));
	}

	[DynamicDependency("Where`1", typeof(Enumerable))]
	public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, int, bool>>, IQueryable<TSource>>(Where).Method, source.Expression, Expression.Quote(predicate)));
	}

	[DynamicDependency("OfType`1", typeof(Enumerable))]
	public static IQueryable<TResult> OfType<TResult>(this IQueryable source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.CreateQuery<TResult>(Expression.Call(null, new Func<IQueryable, IQueryable<TResult>>(OfType<TResult>).Method, source.Expression));
	}

	[DynamicDependency("Cast`1", typeof(Enumerable))]
	public static IQueryable<TResult> Cast<TResult>(this IQueryable source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.CreateQuery<TResult>(Expression.Call(null, new Func<IQueryable, IQueryable<TResult>>(Cast<TResult>).Method, source.Expression));
	}

	[DynamicDependency("Select`2", typeof(Enumerable))]
	public static IQueryable<TResult> Select<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.CreateQuery<TResult>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TResult>>, IQueryable<TResult>>(Select).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Select`2", typeof(Enumerable))]
	public static IQueryable<TResult> Select<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, int, TResult>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.CreateQuery<TResult>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, int, TResult>>, IQueryable<TResult>>(Select).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("SelectMany`2", typeof(Enumerable))]
	public static IQueryable<TResult> SelectMany<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, IEnumerable<TResult>>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.CreateQuery<TResult>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, IEnumerable<TResult>>>, IQueryable<TResult>>(SelectMany).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("SelectMany`2", typeof(Enumerable))]
	public static IQueryable<TResult> SelectMany<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, int, IEnumerable<TResult>>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.CreateQuery<TResult>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, int, IEnumerable<TResult>>>, IQueryable<TResult>>(SelectMany).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("SelectMany`3", typeof(Enumerable))]
	public static IQueryable<TResult> SelectMany<TSource, TCollection, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, int, IEnumerable<TCollection>>> collectionSelector, Expression<Func<TSource, TCollection, TResult>> resultSelector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(collectionSelector, "collectionSelector");
		ArgumentNullException.ThrowIfNull(resultSelector, "resultSelector");
		return source.Provider.CreateQuery<TResult>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, int, IEnumerable<TCollection>>>, Expression<Func<TSource, TCollection, TResult>>, IQueryable<TResult>>(SelectMany).Method, source.Expression, Expression.Quote(collectionSelector), Expression.Quote(resultSelector)));
	}

	[DynamicDependency("SelectMany`3", typeof(Enumerable))]
	public static IQueryable<TResult> SelectMany<TSource, TCollection, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, IEnumerable<TCollection>>> collectionSelector, Expression<Func<TSource, TCollection, TResult>> resultSelector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(collectionSelector, "collectionSelector");
		ArgumentNullException.ThrowIfNull(resultSelector, "resultSelector");
		return source.Provider.CreateQuery<TResult>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, IEnumerable<TCollection>>>, Expression<Func<TSource, TCollection, TResult>>, IQueryable<TResult>>(SelectMany).Method, source.Expression, Expression.Quote(collectionSelector), Expression.Quote(resultSelector)));
	}

	private static Expression GetSourceExpression<TSource>(IEnumerable<TSource> source)
	{
		if (!(source is IQueryable<TSource> queryable))
		{
			return Expression.Constant(source, typeof(IEnumerable<TSource>));
		}
		return queryable.Expression;
	}

	[DynamicDependency("Join`4", typeof(Enumerable))]
	public static IQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector)
	{
		ArgumentNullException.ThrowIfNull(outer, "outer");
		ArgumentNullException.ThrowIfNull(inner, "inner");
		ArgumentNullException.ThrowIfNull(outerKeySelector, "outerKeySelector");
		ArgumentNullException.ThrowIfNull(innerKeySelector, "innerKeySelector");
		ArgumentNullException.ThrowIfNull(resultSelector, "resultSelector");
		return outer.Provider.CreateQuery<TResult>(Expression.Call(null, new Func<IQueryable<TOuter>, IEnumerable<TInner>, Expression<Func<TOuter, TKey>>, Expression<Func<TInner, TKey>>, Expression<Func<TOuter, TInner, TResult>>, IQueryable<TResult>>(Join).Method, outer.Expression, GetSourceExpression(inner), Expression.Quote(outerKeySelector), Expression.Quote(innerKeySelector), Expression.Quote(resultSelector)));
	}

	[DynamicDependency("Join`4", typeof(Enumerable))]
	public static IQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector, IEqualityComparer<TKey>? comparer)
	{
		ArgumentNullException.ThrowIfNull(outer, "outer");
		ArgumentNullException.ThrowIfNull(inner, "inner");
		ArgumentNullException.ThrowIfNull(outerKeySelector, "outerKeySelector");
		ArgumentNullException.ThrowIfNull(innerKeySelector, "innerKeySelector");
		ArgumentNullException.ThrowIfNull(resultSelector, "resultSelector");
		return outer.Provider.CreateQuery<TResult>(Expression.Call(null, new Func<IQueryable<TOuter>, IEnumerable<TInner>, Expression<Func<TOuter, TKey>>, Expression<Func<TInner, TKey>>, Expression<Func<TOuter, TInner, TResult>>, IEqualityComparer<TKey>, IQueryable<TResult>>(Join).Method, outer.Expression, GetSourceExpression(inner), Expression.Quote(outerKeySelector), Expression.Quote(innerKeySelector), Expression.Quote(resultSelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
	}

	[DynamicDependency("GroupJoin`4", typeof(Enumerable))]
	public static IQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, IEnumerable<TInner>, TResult>> resultSelector)
	{
		ArgumentNullException.ThrowIfNull(outer, "outer");
		ArgumentNullException.ThrowIfNull(inner, "inner");
		ArgumentNullException.ThrowIfNull(outerKeySelector, "outerKeySelector");
		ArgumentNullException.ThrowIfNull(innerKeySelector, "innerKeySelector");
		ArgumentNullException.ThrowIfNull(resultSelector, "resultSelector");
		return outer.Provider.CreateQuery<TResult>(Expression.Call(null, new Func<IQueryable<TOuter>, IEnumerable<TInner>, Expression<Func<TOuter, TKey>>, Expression<Func<TInner, TKey>>, Expression<Func<TOuter, IEnumerable<TInner>, TResult>>, IQueryable<TResult>>(GroupJoin).Method, outer.Expression, GetSourceExpression(inner), Expression.Quote(outerKeySelector), Expression.Quote(innerKeySelector), Expression.Quote(resultSelector)));
	}

	[DynamicDependency("GroupJoin`4", typeof(Enumerable))]
	public static IQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, IEnumerable<TInner>, TResult>> resultSelector, IEqualityComparer<TKey>? comparer)
	{
		ArgumentNullException.ThrowIfNull(outer, "outer");
		ArgumentNullException.ThrowIfNull(inner, "inner");
		ArgumentNullException.ThrowIfNull(outerKeySelector, "outerKeySelector");
		ArgumentNullException.ThrowIfNull(innerKeySelector, "innerKeySelector");
		ArgumentNullException.ThrowIfNull(resultSelector, "resultSelector");
		return outer.Provider.CreateQuery<TResult>(Expression.Call(null, new Func<IQueryable<TOuter>, IEnumerable<TInner>, Expression<Func<TOuter, TKey>>, Expression<Func<TInner, TKey>>, Expression<Func<TOuter, IEnumerable<TInner>, TResult>>, IEqualityComparer<TKey>, IQueryable<TResult>>(GroupJoin).Method, outer.Expression, GetSourceExpression(inner), Expression.Quote(outerKeySelector), Expression.Quote(innerKeySelector), Expression.Quote(resultSelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
	}

	[DynamicDependency("Order`1", typeof(Enumerable))]
	public static IOrderedQueryable<T> Order<T>(this IQueryable<T> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(Expression.Call(null, new Func<IQueryable<T>, IOrderedQueryable<T>>(Order).Method, source.Expression));
	}

	[DynamicDependency("Order`1", typeof(Enumerable))]
	public static IOrderedQueryable<T> Order<T>(this IQueryable<T> source, IComparer<T> comparer)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(Expression.Call(null, new Func<IQueryable<T>, IComparer<T>, IOrderedQueryable<T>>(Order).Method, source.Expression, Expression.Constant(comparer, typeof(IComparer<T>))));
	}

	[DynamicDependency("OrderBy`2", typeof(Enumerable))]
	public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IOrderedQueryable<TSource>>(OrderBy).Method, source.Expression, Expression.Quote(keySelector)));
	}

	[DynamicDependency("OrderBy`2", typeof(Enumerable))]
	public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IComparer<TKey>, IOrderedQueryable<TSource>>(OrderBy).Method, source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IComparer<TKey>))));
	}

	[DynamicDependency("OrderDescending`1", typeof(Enumerable))]
	public static IOrderedQueryable<T> OrderDescending<T>(this IQueryable<T> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(Expression.Call(null, new Func<IQueryable<T>, IOrderedQueryable<T>>(OrderDescending).Method, source.Expression));
	}

	[DynamicDependency("OrderDescending`1", typeof(Enumerable))]
	public static IOrderedQueryable<T> OrderDescending<T>(this IQueryable<T> source, IComparer<T> comparer)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(Expression.Call(null, new Func<IQueryable<T>, IComparer<T>, IOrderedQueryable<T>>(OrderDescending).Method, source.Expression, Expression.Constant(comparer, typeof(IComparer<T>))));
	}

	[DynamicDependency("OrderByDescending`2", typeof(Enumerable))]
	public static IOrderedQueryable<TSource> OrderByDescending<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IOrderedQueryable<TSource>>(OrderByDescending).Method, source.Expression, Expression.Quote(keySelector)));
	}

	[DynamicDependency("OrderByDescending`2", typeof(Enumerable))]
	public static IOrderedQueryable<TSource> OrderByDescending<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IComparer<TKey>, IOrderedQueryable<TSource>>(OrderByDescending).Method, source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IComparer<TKey>))));
	}

	[DynamicDependency("ThenBy`2", typeof(Enumerable))]
	public static IOrderedQueryable<TSource> ThenBy<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IOrderedQueryable<TSource>, Expression<Func<TSource, TKey>>, IOrderedQueryable<TSource>>(ThenBy).Method, source.Expression, Expression.Quote(keySelector)));
	}

	[DynamicDependency("ThenBy`2", typeof(Enumerable))]
	public static IOrderedQueryable<TSource> ThenBy<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IOrderedQueryable<TSource>, Expression<Func<TSource, TKey>>, IComparer<TKey>, IOrderedQueryable<TSource>>(ThenBy).Method, source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IComparer<TKey>))));
	}

	[DynamicDependency("ThenByDescending`2", typeof(Enumerable))]
	public static IOrderedQueryable<TSource> ThenByDescending<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IOrderedQueryable<TSource>, Expression<Func<TSource, TKey>>, IOrderedQueryable<TSource>>(ThenByDescending).Method, source.Expression, Expression.Quote(keySelector)));
	}

	[DynamicDependency("ThenByDescending`2", typeof(Enumerable))]
	public static IOrderedQueryable<TSource> ThenByDescending<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IOrderedQueryable<TSource>, Expression<Func<TSource, TKey>>, IComparer<TKey>, IOrderedQueryable<TSource>>(ThenByDescending).Method, source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IComparer<TKey>))));
	}

	[DynamicDependency("Take`1", typeof(Enumerable))]
	public static IQueryable<TSource> Take<TSource>(this IQueryable<TSource> source, int count)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, int, IQueryable<TSource>>(Take).Method, source.Expression, Expression.Constant(count)));
	}

	[DynamicDependency("Take`1", typeof(Enumerable))]
	public static IQueryable<TSource> Take<TSource>(this IQueryable<TSource> source, Range range)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Range, IQueryable<TSource>>(Take).Method, source.Expression, Expression.Constant(range)));
	}

	[DynamicDependency("TakeWhile`1", typeof(Enumerable))]
	public static IQueryable<TSource> TakeWhile<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, IQueryable<TSource>>(TakeWhile).Method, source.Expression, Expression.Quote(predicate)));
	}

	[DynamicDependency("TakeWhile`1", typeof(Enumerable))]
	public static IQueryable<TSource> TakeWhile<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, int, bool>>, IQueryable<TSource>>(TakeWhile).Method, source.Expression, Expression.Quote(predicate)));
	}

	[DynamicDependency("Skip`1", typeof(Enumerable))]
	public static IQueryable<TSource> Skip<TSource>(this IQueryable<TSource> source, int count)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, int, IQueryable<TSource>>(Skip).Method, source.Expression, Expression.Constant(count)));
	}

	[DynamicDependency("SkipWhile`1", typeof(Enumerable))]
	public static IQueryable<TSource> SkipWhile<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, IQueryable<TSource>>(SkipWhile).Method, source.Expression, Expression.Quote(predicate)));
	}

	[DynamicDependency("SkipWhile`1", typeof(Enumerable))]
	public static IQueryable<TSource> SkipWhile<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, int, bool>>, IQueryable<TSource>>(SkipWhile).Method, source.Expression, Expression.Quote(predicate)));
	}

	[DynamicDependency("GroupBy`2", typeof(Enumerable))]
	public static IQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return source.Provider.CreateQuery<IGrouping<TKey, TSource>>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IQueryable<IGrouping<TKey, TSource>>>(GroupBy).Method, source.Expression, Expression.Quote(keySelector)));
	}

	[DynamicDependency("GroupBy`3", typeof(Enumerable))]
	public static IQueryable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		ArgumentNullException.ThrowIfNull(elementSelector, "elementSelector");
		return source.Provider.CreateQuery<IGrouping<TKey, TElement>>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, Expression<Func<TSource, TElement>>, IQueryable<IGrouping<TKey, TElement>>>(GroupBy).Method, source.Expression, Expression.Quote(keySelector), Expression.Quote(elementSelector)));
	}

	[DynamicDependency("GroupBy`2", typeof(Enumerable))]
	public static IQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IEqualityComparer<TKey>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return source.Provider.CreateQuery<IGrouping<TKey, TSource>>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IEqualityComparer<TKey>, IQueryable<IGrouping<TKey, TSource>>>(GroupBy).Method, source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
	}

	[DynamicDependency("GroupBy`3", typeof(Enumerable))]
	public static IQueryable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector, IEqualityComparer<TKey>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		ArgumentNullException.ThrowIfNull(elementSelector, "elementSelector");
		return source.Provider.CreateQuery<IGrouping<TKey, TElement>>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, Expression<Func<TSource, TElement>>, IEqualityComparer<TKey>, IQueryable<IGrouping<TKey, TElement>>>(GroupBy).Method, source.Expression, Expression.Quote(keySelector), Expression.Quote(elementSelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
	}

	[DynamicDependency("GroupBy`4", typeof(Enumerable))]
	public static IQueryable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector, Expression<Func<TKey, IEnumerable<TElement>, TResult>> resultSelector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		ArgumentNullException.ThrowIfNull(elementSelector, "elementSelector");
		ArgumentNullException.ThrowIfNull(resultSelector, "resultSelector");
		return source.Provider.CreateQuery<TResult>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, Expression<Func<TSource, TElement>>, Expression<Func<TKey, IEnumerable<TElement>, TResult>>, IQueryable<TResult>>(GroupBy).Method, source.Expression, Expression.Quote(keySelector), Expression.Quote(elementSelector), Expression.Quote(resultSelector)));
	}

	[DynamicDependency("GroupBy`3", typeof(Enumerable))]
	public static IQueryable<TResult> GroupBy<TSource, TKey, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TKey, IEnumerable<TSource>, TResult>> resultSelector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		ArgumentNullException.ThrowIfNull(resultSelector, "resultSelector");
		return source.Provider.CreateQuery<TResult>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, Expression<Func<TKey, IEnumerable<TSource>, TResult>>, IQueryable<TResult>>(GroupBy).Method, source.Expression, Expression.Quote(keySelector), Expression.Quote(resultSelector)));
	}

	[DynamicDependency("GroupBy`3", typeof(Enumerable))]
	public static IQueryable<TResult> GroupBy<TSource, TKey, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TKey, IEnumerable<TSource>, TResult>> resultSelector, IEqualityComparer<TKey>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		ArgumentNullException.ThrowIfNull(resultSelector, "resultSelector");
		return source.Provider.CreateQuery<TResult>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, Expression<Func<TKey, IEnumerable<TSource>, TResult>>, IEqualityComparer<TKey>, IQueryable<TResult>>(GroupBy).Method, source.Expression, Expression.Quote(keySelector), Expression.Quote(resultSelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
	}

	[DynamicDependency("GroupBy`4", typeof(Enumerable))]
	public static IQueryable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector, Expression<Func<TKey, IEnumerable<TElement>, TResult>> resultSelector, IEqualityComparer<TKey>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		ArgumentNullException.ThrowIfNull(elementSelector, "elementSelector");
		ArgumentNullException.ThrowIfNull(resultSelector, "resultSelector");
		return source.Provider.CreateQuery<TResult>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, Expression<Func<TSource, TElement>>, Expression<Func<TKey, IEnumerable<TElement>, TResult>>, IEqualityComparer<TKey>, IQueryable<TResult>>(GroupBy).Method, source.Expression, Expression.Quote(keySelector), Expression.Quote(elementSelector), Expression.Quote(resultSelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
	}

	[DynamicDependency("Distinct`1", typeof(Enumerable))]
	public static IQueryable<TSource> Distinct<TSource>(this IQueryable<TSource> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IQueryable<TSource>>(Distinct).Method, source.Expression));
	}

	[DynamicDependency("Distinct`1", typeof(Enumerable))]
	public static IQueryable<TSource> Distinct<TSource>(this IQueryable<TSource> source, IEqualityComparer<TSource>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IEqualityComparer<TSource>, IQueryable<TSource>>(Distinct).Method, source.Expression, Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))));
	}

	[DynamicDependency("DistinctBy`2", typeof(Enumerable))]
	public static IQueryable<TSource> DistinctBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IQueryable<TSource>>(DistinctBy).Method, source.Expression, Expression.Quote(keySelector)));
	}

	[DynamicDependency("DistinctBy`2", typeof(Enumerable))]
	public static IQueryable<TSource> DistinctBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IEqualityComparer<TKey>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IEqualityComparer<TKey>, IQueryable<TSource>>(DistinctBy).Method, source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
	}

	[DynamicDependency("Chunk`1", typeof(Enumerable))]
	public static IQueryable<TSource[]> Chunk<TSource>(this IQueryable<TSource> source, int size)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.CreateQuery<TSource[]>(Expression.Call(null, new Func<IQueryable<TSource>, int, IQueryable<TSource[]>>(Chunk).Method, source.Expression, Expression.Constant(size)));
	}

	[DynamicDependency("Concat`1", typeof(Enumerable))]
	public static IQueryable<TSource> Concat<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
	{
		ArgumentNullException.ThrowIfNull(source1, "source1");
		ArgumentNullException.ThrowIfNull(source2, "source2");
		return source1.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IEnumerable<TSource>, IQueryable<TSource>>(Concat).Method, source1.Expression, GetSourceExpression(source2)));
	}

	[DynamicDependency("Zip`2", typeof(Enumerable))]
	public static IQueryable<(TFirst First, TSecond Second)> Zip<TFirst, TSecond>(this IQueryable<TFirst> source1, IEnumerable<TSecond> source2)
	{
		ArgumentNullException.ThrowIfNull(source1, "source1");
		ArgumentNullException.ThrowIfNull(source2, "source2");
		return source1.Provider.CreateQuery<(TFirst, TSecond)>(Expression.Call(null, new Func<IQueryable<TFirst>, IEnumerable<TSecond>, IQueryable<(TFirst, TSecond)>>(Zip).Method, source1.Expression, GetSourceExpression(source2)));
	}

	[DynamicDependency("Zip`3", typeof(Enumerable))]
	public static IQueryable<TResult> Zip<TFirst, TSecond, TResult>(this IQueryable<TFirst> source1, IEnumerable<TSecond> source2, Expression<Func<TFirst, TSecond, TResult>> resultSelector)
	{
		ArgumentNullException.ThrowIfNull(source1, "source1");
		ArgumentNullException.ThrowIfNull(source2, "source2");
		ArgumentNullException.ThrowIfNull(resultSelector, "resultSelector");
		return source1.Provider.CreateQuery<TResult>(Expression.Call(null, new Func<IQueryable<TFirst>, IEnumerable<TSecond>, Expression<Func<TFirst, TSecond, TResult>>, IQueryable<TResult>>(Zip).Method, source1.Expression, GetSourceExpression(source2), Expression.Quote(resultSelector)));
	}

	[DynamicDependency("Zip`3", typeof(Enumerable))]
	public static IQueryable<(TFirst First, TSecond Second, TThird Third)> Zip<TFirst, TSecond, TThird>(this IQueryable<TFirst> source1, IEnumerable<TSecond> source2, IEnumerable<TThird> source3)
	{
		ArgumentNullException.ThrowIfNull(source1, "source1");
		ArgumentNullException.ThrowIfNull(source2, "source2");
		ArgumentNullException.ThrowIfNull(source3, "source3");
		return source1.Provider.CreateQuery<(TFirst, TSecond, TThird)>(Expression.Call(null, new Func<IQueryable<TFirst>, IEnumerable<TSecond>, IEnumerable<TThird>, IQueryable<(TFirst, TSecond, TThird)>>(Zip).Method, source1.Expression, GetSourceExpression(source2), GetSourceExpression(source3)));
	}

	[DynamicDependency("Union`1", typeof(Enumerable))]
	public static IQueryable<TSource> Union<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
	{
		ArgumentNullException.ThrowIfNull(source1, "source1");
		ArgumentNullException.ThrowIfNull(source2, "source2");
		return source1.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IEnumerable<TSource>, IQueryable<TSource>>(Union).Method, source1.Expression, GetSourceExpression(source2)));
	}

	[DynamicDependency("Union`1", typeof(Enumerable))]
	public static IQueryable<TSource> Union<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source1, "source1");
		ArgumentNullException.ThrowIfNull(source2, "source2");
		return source1.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IEnumerable<TSource>, IEqualityComparer<TSource>, IQueryable<TSource>>(Union).Method, source1.Expression, GetSourceExpression(source2), Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))));
	}

	[DynamicDependency("UnionBy`2", typeof(Enumerable))]
	public static IQueryable<TSource> UnionBy<TSource, TKey>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, Expression<Func<TSource, TKey>> keySelector)
	{
		ArgumentNullException.ThrowIfNull(source1, "source1");
		ArgumentNullException.ThrowIfNull(source2, "source2");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return source1.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IEnumerable<TSource>, Expression<Func<TSource, TKey>>, IQueryable<TSource>>(UnionBy).Method, source1.Expression, GetSourceExpression(source2), Expression.Quote(keySelector)));
	}

	[DynamicDependency("UnionBy`2", typeof(Enumerable))]
	public static IQueryable<TSource> UnionBy<TSource, TKey>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, Expression<Func<TSource, TKey>> keySelector, IEqualityComparer<TKey>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source1, "source1");
		ArgumentNullException.ThrowIfNull(source2, "source2");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return source1.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IEnumerable<TSource>, Expression<Func<TSource, TKey>>, IEqualityComparer<TKey>, IQueryable<TSource>>(UnionBy).Method, source1.Expression, GetSourceExpression(source2), Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
	}

	[DynamicDependency("Intersect`1", typeof(Enumerable))]
	public static IQueryable<TSource> Intersect<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
	{
		ArgumentNullException.ThrowIfNull(source1, "source1");
		ArgumentNullException.ThrowIfNull(source2, "source2");
		return source1.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IEnumerable<TSource>, IQueryable<TSource>>(Intersect).Method, source1.Expression, GetSourceExpression(source2)));
	}

	[DynamicDependency("Intersect`1", typeof(Enumerable))]
	public static IQueryable<TSource> Intersect<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source1, "source1");
		ArgumentNullException.ThrowIfNull(source2, "source2");
		return source1.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IEnumerable<TSource>, IEqualityComparer<TSource>, IQueryable<TSource>>(Intersect).Method, source1.Expression, GetSourceExpression(source2), Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))));
	}

	[DynamicDependency("IntersectBy`2", typeof(Enumerable))]
	public static IQueryable<TSource> IntersectBy<TSource, TKey>(this IQueryable<TSource> source1, IEnumerable<TKey> source2, Expression<Func<TSource, TKey>> keySelector)
	{
		ArgumentNullException.ThrowIfNull(source1, "source1");
		ArgumentNullException.ThrowIfNull(source2, "source2");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return source1.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IEnumerable<TKey>, Expression<Func<TSource, TKey>>, IQueryable<TSource>>(IntersectBy).Method, source1.Expression, GetSourceExpression(source2), Expression.Quote(keySelector)));
	}

	[DynamicDependency("IntersectBy`2", typeof(Enumerable))]
	public static IQueryable<TSource> IntersectBy<TSource, TKey>(this IQueryable<TSource> source1, IEnumerable<TKey> source2, Expression<Func<TSource, TKey>> keySelector, IEqualityComparer<TKey>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source1, "source1");
		ArgumentNullException.ThrowIfNull(source2, "source2");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return source1.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IEnumerable<TKey>, Expression<Func<TSource, TKey>>, IEqualityComparer<TKey>, IQueryable<TSource>>(IntersectBy).Method, source1.Expression, GetSourceExpression(source2), Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
	}

	[DynamicDependency("Except`1", typeof(Enumerable))]
	public static IQueryable<TSource> Except<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
	{
		ArgumentNullException.ThrowIfNull(source1, "source1");
		ArgumentNullException.ThrowIfNull(source2, "source2");
		return source1.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IEnumerable<TSource>, IQueryable<TSource>>(Except).Method, source1.Expression, GetSourceExpression(source2)));
	}

	[DynamicDependency("Except`1", typeof(Enumerable))]
	public static IQueryable<TSource> Except<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source1, "source1");
		ArgumentNullException.ThrowIfNull(source2, "source2");
		return source1.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IEnumerable<TSource>, IEqualityComparer<TSource>, IQueryable<TSource>>(Except).Method, source1.Expression, GetSourceExpression(source2), Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))));
	}

	[DynamicDependency("ExceptBy`2", typeof(Enumerable))]
	public static IQueryable<TSource> ExceptBy<TSource, TKey>(this IQueryable<TSource> source1, IEnumerable<TKey> source2, Expression<Func<TSource, TKey>> keySelector)
	{
		ArgumentNullException.ThrowIfNull(source1, "source1");
		ArgumentNullException.ThrowIfNull(source2, "source2");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return source1.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IEnumerable<TKey>, Expression<Func<TSource, TKey>>, IQueryable<TSource>>(ExceptBy).Method, source1.Expression, GetSourceExpression(source2), Expression.Quote(keySelector)));
	}

	[DynamicDependency("ExceptBy`2", typeof(Enumerable))]
	public static IQueryable<TSource> ExceptBy<TSource, TKey>(this IQueryable<TSource> source1, IEnumerable<TKey> source2, Expression<Func<TSource, TKey>> keySelector, IEqualityComparer<TKey>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source1, "source1");
		ArgumentNullException.ThrowIfNull(source2, "source2");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return source1.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IEnumerable<TKey>, Expression<Func<TSource, TKey>>, IEqualityComparer<TKey>, IQueryable<TSource>>(ExceptBy).Method, source1.Expression, GetSourceExpression(source2), Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
	}

	[DynamicDependency("First`1", typeof(Enumerable))]
	public static TSource First<TSource>(this IQueryable<TSource> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, TSource>(First).Method, source.Expression));
	}

	[DynamicDependency("First`1", typeof(Enumerable))]
	public static TSource First<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, TSource>(First).Method, source.Expression, Expression.Quote(predicate)));
	}

	[DynamicDependency("FirstOrDefault`1", typeof(Enumerable))]
	public static TSource? FirstOrDefault<TSource>(this IQueryable<TSource> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, TSource>(FirstOrDefault).Method, source.Expression));
	}

	[DynamicDependency("FirstOrDefault`1", typeof(Enumerable))]
	public static TSource FirstOrDefault<TSource>(this IQueryable<TSource> source, TSource defaultValue)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, TSource, TSource>(FirstOrDefault).Method, source.Expression, Expression.Constant(defaultValue, typeof(TSource))));
	}

	[DynamicDependency("FirstOrDefault`1", typeof(Enumerable))]
	public static TSource? FirstOrDefault<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, TSource>(FirstOrDefault).Method, source.Expression, Expression.Quote(predicate)));
	}

	[DynamicDependency("FirstOrDefault`1", typeof(Enumerable))]
	public static TSource FirstOrDefault<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, TSource defaultValue)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, TSource, TSource>(FirstOrDefault).Method, source.Expression, Expression.Quote(predicate), Expression.Constant(defaultValue, typeof(TSource))));
	}

	[DynamicDependency("Last`1", typeof(Enumerable))]
	public static TSource Last<TSource>(this IQueryable<TSource> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, TSource>(Last).Method, source.Expression));
	}

	[DynamicDependency("Last`1", typeof(Enumerable))]
	public static TSource Last<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, TSource>(Last).Method, source.Expression, Expression.Quote(predicate)));
	}

	[DynamicDependency("LastOrDefault`1", typeof(Enumerable))]
	public static TSource? LastOrDefault<TSource>(this IQueryable<TSource> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, TSource>(LastOrDefault).Method, source.Expression));
	}

	[DynamicDependency("LastOrDefault`1", typeof(Enumerable))]
	public static TSource LastOrDefault<TSource>(this IQueryable<TSource> source, TSource defaultValue)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, TSource, TSource>(LastOrDefault).Method, source.Expression, Expression.Constant(defaultValue, typeof(TSource))));
	}

	[DynamicDependency("LastOrDefault`1", typeof(Enumerable))]
	public static TSource? LastOrDefault<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, TSource>(LastOrDefault).Method, source.Expression, Expression.Quote(predicate)));
	}

	[DynamicDependency("LastOrDefault`1", typeof(Enumerable))]
	public static TSource LastOrDefault<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, TSource defaultValue)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, TSource, TSource>(LastOrDefault).Method, source.Expression, Expression.Quote(predicate), Expression.Constant(defaultValue, typeof(TSource))));
	}

	[DynamicDependency("Single`1", typeof(Enumerable))]
	public static TSource Single<TSource>(this IQueryable<TSource> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, TSource>(Single).Method, source.Expression));
	}

	[DynamicDependency("Single`1", typeof(Enumerable))]
	public static TSource Single<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, TSource>(Single).Method, source.Expression, Expression.Quote(predicate)));
	}

	[DynamicDependency("SingleOrDefault`1", typeof(Enumerable))]
	public static TSource? SingleOrDefault<TSource>(this IQueryable<TSource> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, TSource>(SingleOrDefault).Method, source.Expression));
	}

	[DynamicDependency("SingleOrDefault`1", typeof(Enumerable))]
	public static TSource SingleOrDefault<TSource>(this IQueryable<TSource> source, TSource defaultValue)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, TSource, TSource>(SingleOrDefault).Method, source.Expression, Expression.Constant(defaultValue, typeof(TSource))));
	}

	[DynamicDependency("SingleOrDefault`1", typeof(Enumerable))]
	public static TSource? SingleOrDefault<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, TSource>(SingleOrDefault).Method, source.Expression, Expression.Quote(predicate)));
	}

	[DynamicDependency("SingleOrDefault`1", typeof(Enumerable))]
	public static TSource SingleOrDefault<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, TSource defaultValue)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, TSource, TSource>(SingleOrDefault).Method, source.Expression, Expression.Quote(predicate), Expression.Constant(defaultValue, typeof(TSource))));
	}

	[DynamicDependency("ElementAt`1", typeof(Enumerable))]
	public static TSource ElementAt<TSource>(this IQueryable<TSource> source, int index)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		if (index < 0)
		{
			throw Error.ArgumentOutOfRange("index");
		}
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, int, TSource>(ElementAt).Method, source.Expression, Expression.Constant(index)));
	}

	[DynamicDependency("ElementAt`1", typeof(Enumerable))]
	public static TSource ElementAt<TSource>(this IQueryable<TSource> source, Index index)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		if (index.IsFromEnd && index.Value == 0)
		{
			throw Error.ArgumentOutOfRange("index");
		}
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Index, TSource>(ElementAt).Method, source.Expression, Expression.Constant(index)));
	}

	[DynamicDependency("ElementAtOrDefault`1", typeof(Enumerable))]
	public static TSource? ElementAtOrDefault<TSource>(this IQueryable<TSource> source, int index)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, int, TSource>(ElementAtOrDefault).Method, source.Expression, Expression.Constant(index)));
	}

	[DynamicDependency("ElementAtOrDefault`1", typeof(Enumerable))]
	public static TSource? ElementAtOrDefault<TSource>(this IQueryable<TSource> source, Index index)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Index, TSource>(ElementAtOrDefault).Method, source.Expression, Expression.Constant(index)));
	}

	[DynamicDependency("DefaultIfEmpty`1", typeof(Enumerable))]
	public static IQueryable<TSource?> DefaultIfEmpty<TSource>(this IQueryable<TSource> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IQueryable<TSource>>(DefaultIfEmpty).Method, source.Expression));
	}

	[DynamicDependency("DefaultIfEmpty`1", typeof(Enumerable))]
	public static IQueryable<TSource> DefaultIfEmpty<TSource>(this IQueryable<TSource> source, TSource defaultValue)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, TSource, IQueryable<TSource>>(DefaultIfEmpty).Method, source.Expression, Expression.Constant(defaultValue, typeof(TSource))));
	}

	[DynamicDependency("Contains`1", typeof(Enumerable))]
	public static bool Contains<TSource>(this IQueryable<TSource> source, TSource item)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<bool>(Expression.Call(null, new Func<IQueryable<TSource>, TSource, bool>(Contains).Method, source.Expression, Expression.Constant(item, typeof(TSource))));
	}

	[DynamicDependency("Contains`1", typeof(Enumerable))]
	public static bool Contains<TSource>(this IQueryable<TSource> source, TSource item, IEqualityComparer<TSource>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<bool>(Expression.Call(null, new Func<IQueryable<TSource>, TSource, IEqualityComparer<TSource>, bool>(Contains).Method, source.Expression, Expression.Constant(item, typeof(TSource)), Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))));
	}

	[DynamicDependency("Reverse`1", typeof(Enumerable))]
	public static IQueryable<TSource> Reverse<TSource>(this IQueryable<TSource> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IQueryable<TSource>>(Reverse).Method, source.Expression));
	}

	[DynamicDependency("SequenceEqual`1", typeof(Enumerable))]
	public static bool SequenceEqual<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
	{
		ArgumentNullException.ThrowIfNull(source1, "source1");
		ArgumentNullException.ThrowIfNull(source2, "source2");
		return source1.Provider.Execute<bool>(Expression.Call(null, new Func<IQueryable<TSource>, IEnumerable<TSource>, bool>(SequenceEqual).Method, source1.Expression, GetSourceExpression(source2)));
	}

	[DynamicDependency("SequenceEqual`1", typeof(Enumerable))]
	public static bool SequenceEqual<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source1, "source1");
		ArgumentNullException.ThrowIfNull(source2, "source2");
		return source1.Provider.Execute<bool>(Expression.Call(null, new Func<IQueryable<TSource>, IEnumerable<TSource>, IEqualityComparer<TSource>, bool>(SequenceEqual).Method, source1.Expression, GetSourceExpression(source2), Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))));
	}

	[DynamicDependency("Any`1", typeof(Enumerable))]
	public static bool Any<TSource>(this IQueryable<TSource> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<bool>(Expression.Call(null, new Func<IQueryable<TSource>, bool>(Any).Method, source.Expression));
	}

	[DynamicDependency("Any`1", typeof(Enumerable))]
	public static bool Any<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.Execute<bool>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, bool>(Any).Method, source.Expression, Expression.Quote(predicate)));
	}

	[DynamicDependency("All`1", typeof(Enumerable))]
	public static bool All<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.Execute<bool>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, bool>(All).Method, source.Expression, Expression.Quote(predicate)));
	}

	[DynamicDependency("Count`1", typeof(Enumerable))]
	public static int Count<TSource>(this IQueryable<TSource> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<int>(Expression.Call(null, new Func<IQueryable<TSource>, int>(Count).Method, source.Expression));
	}

	[DynamicDependency("Count`1", typeof(Enumerable))]
	public static int Count<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.Execute<int>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, int>(Count).Method, source.Expression, Expression.Quote(predicate)));
	}

	[DynamicDependency("LongCount`1", typeof(Enumerable))]
	public static long LongCount<TSource>(this IQueryable<TSource> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<long>(Expression.Call(null, new Func<IQueryable<TSource>, long>(LongCount).Method, source.Expression));
	}

	[DynamicDependency("LongCount`1", typeof(Enumerable))]
	public static long LongCount<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		return source.Provider.Execute<long>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, long>(LongCount).Method, source.Expression, Expression.Quote(predicate)));
	}

	[DynamicDependency("Min`1", typeof(Enumerable))]
	public static TSource? Min<TSource>(this IQueryable<TSource> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, TSource>(Min).Method, source.Expression));
	}

	[DynamicDependency("Min`1", typeof(Enumerable))]
	public static TSource? Min<TSource>(this IQueryable<TSource> source, IComparer<TSource>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IComparer<TSource>, TSource>(Min).Method, source.Expression, Expression.Constant(comparer, typeof(IComparer<TSource>))));
	}

	[DynamicDependency("Min`2", typeof(Enumerable))]
	public static TResult? Min<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<TResult>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TResult>>, TResult>(Min).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("MinBy`2", typeof(Enumerable))]
	public static TSource? MinBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, TSource>(MinBy).Method, source.Expression, Expression.Quote(keySelector)));
	}

	[DynamicDependency("MinBy`2", typeof(Enumerable))]
	public static TSource? MinBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TSource>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IComparer<TSource>, TSource>(MinBy).Method, source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IComparer<TSource>))));
	}

	[DynamicDependency("Max`1", typeof(Enumerable))]
	public static TSource? Max<TSource>(this IQueryable<TSource> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, TSource>(Max).Method, source.Expression));
	}

	[DynamicDependency("Max`1", typeof(Enumerable))]
	public static TSource? Max<TSource>(this IQueryable<TSource> source, IComparer<TSource>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, IComparer<TSource>, TSource>(Max).Method, source.Expression, Expression.Constant(comparer, typeof(IComparer<TSource>))));
	}

	[DynamicDependency("Max`2", typeof(Enumerable))]
	public static TResult? Max<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<TResult>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TResult>>, TResult>(Max).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("MaxBy`2", typeof(Enumerable))]
	public static TSource? MaxBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, TSource>(MaxBy).Method, source.Expression, Expression.Quote(keySelector)));
	}

	[DynamicDependency("MaxBy`2", typeof(Enumerable))]
	public static TSource? MaxBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TSource>? comparer)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IComparer<TSource>, TSource>(MaxBy).Method, source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IComparer<TSource>))));
	}

	[DynamicDependency("Sum", typeof(Enumerable))]
	public static int Sum(this IQueryable<int> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<int>(Expression.Call(null, new Func<IQueryable<int>, int>(Sum).Method, source.Expression));
	}

	[DynamicDependency("Sum", typeof(Enumerable))]
	public static int? Sum(this IQueryable<int?> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<int?>(Expression.Call(null, new Func<IQueryable<int?>, int?>(Sum).Method, source.Expression));
	}

	[DynamicDependency("Sum", typeof(Enumerable))]
	public static long Sum(this IQueryable<long> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<long>(Expression.Call(null, new Func<IQueryable<long>, long>(Sum).Method, source.Expression));
	}

	[DynamicDependency("Sum", typeof(Enumerable))]
	public static long? Sum(this IQueryable<long?> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<long?>(Expression.Call(null, new Func<IQueryable<long?>, long?>(Sum).Method, source.Expression));
	}

	[DynamicDependency("Sum", typeof(Enumerable))]
	public static float Sum(this IQueryable<float> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<float>(Expression.Call(null, new Func<IQueryable<float>, float>(Sum).Method, source.Expression));
	}

	[DynamicDependency("Sum", typeof(Enumerable))]
	public static float? Sum(this IQueryable<float?> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<float?>(Expression.Call(null, new Func<IQueryable<float?>, float?>(Sum).Method, source.Expression));
	}

	[DynamicDependency("Sum", typeof(Enumerable))]
	public static double Sum(this IQueryable<double> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<double>(Expression.Call(null, new Func<IQueryable<double>, double>(Sum).Method, source.Expression));
	}

	[DynamicDependency("Sum", typeof(Enumerable))]
	public static double? Sum(this IQueryable<double?> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<double?>(Expression.Call(null, new Func<IQueryable<double?>, double?>(Sum).Method, source.Expression));
	}

	[DynamicDependency("Sum", typeof(Enumerable))]
	public static decimal Sum(this IQueryable<decimal> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<decimal>(Expression.Call(null, new Func<IQueryable<decimal>, decimal>(Sum).Method, source.Expression));
	}

	[DynamicDependency("Sum", typeof(Enumerable))]
	public static decimal? Sum(this IQueryable<decimal?> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<decimal?>(Expression.Call(null, new Func<IQueryable<decimal?>, decimal?>(Sum).Method, source.Expression));
	}

	[DynamicDependency("Sum`1", typeof(Enumerable))]
	public static int Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<int>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, int>>, int>(Sum).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Sum`1", typeof(Enumerable))]
	public static int? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<int?>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, int?>>, int?>(Sum).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Sum`1", typeof(Enumerable))]
	public static long Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<long>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, long>>, long>(Sum).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Sum`1", typeof(Enumerable))]
	public static long? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<long?>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, long?>>, long?>(Sum).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Sum`1", typeof(Enumerable))]
	public static float Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<float>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, float>>, float>(Sum).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Sum`1", typeof(Enumerable))]
	public static float? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<float?>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, float?>>, float?>(Sum).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Sum`1", typeof(Enumerable))]
	public static double Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<double>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, double>>, double>(Sum).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Sum`1", typeof(Enumerable))]
	public static double? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<double?>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, double?>>, double?>(Sum).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Sum`1", typeof(Enumerable))]
	public static decimal Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<decimal>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, decimal>>, decimal>(Sum).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Sum`1", typeof(Enumerable))]
	public static decimal? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<decimal?>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, decimal?>>, decimal?>(Sum).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Average", typeof(Enumerable))]
	public static double Average(this IQueryable<int> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<double>(Expression.Call(null, new Func<IQueryable<int>, double>(Average).Method, source.Expression));
	}

	[DynamicDependency("Average", typeof(Enumerable))]
	public static double? Average(this IQueryable<int?> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<double?>(Expression.Call(null, new Func<IQueryable<int?>, double?>(Average).Method, source.Expression));
	}

	[DynamicDependency("Average", typeof(Enumerable))]
	public static double Average(this IQueryable<long> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<double>(Expression.Call(null, new Func<IQueryable<long>, double>(Average).Method, source.Expression));
	}

	[DynamicDependency("Average", typeof(Enumerable))]
	public static double? Average(this IQueryable<long?> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<double?>(Expression.Call(null, new Func<IQueryable<long?>, double?>(Average).Method, source.Expression));
	}

	[DynamicDependency("Average", typeof(Enumerable))]
	public static float Average(this IQueryable<float> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<float>(Expression.Call(null, new Func<IQueryable<float>, float>(Average).Method, source.Expression));
	}

	[DynamicDependency("Average", typeof(Enumerable))]
	public static float? Average(this IQueryable<float?> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<float?>(Expression.Call(null, new Func<IQueryable<float?>, float?>(Average).Method, source.Expression));
	}

	[DynamicDependency("Average", typeof(Enumerable))]
	public static double Average(this IQueryable<double> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<double>(Expression.Call(null, new Func<IQueryable<double>, double>(Average).Method, source.Expression));
	}

	[DynamicDependency("Average", typeof(Enumerable))]
	public static double? Average(this IQueryable<double?> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<double?>(Expression.Call(null, new Func<IQueryable<double?>, double?>(Average).Method, source.Expression));
	}

	[DynamicDependency("Average", typeof(Enumerable))]
	public static decimal Average(this IQueryable<decimal> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<decimal>(Expression.Call(null, new Func<IQueryable<decimal>, decimal>(Average).Method, source.Expression));
	}

	[DynamicDependency("Average", typeof(Enumerable))]
	public static decimal? Average(this IQueryable<decimal?> source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.Execute<decimal?>(Expression.Call(null, new Func<IQueryable<decimal?>, decimal?>(Average).Method, source.Expression));
	}

	[DynamicDependency("Average`1", typeof(Enumerable))]
	public static double Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<double>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, int>>, double>(Average).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Average`1", typeof(Enumerable))]
	public static double? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<double?>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, int?>>, double?>(Average).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Average`1", typeof(Enumerable))]
	public static float Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<float>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, float>>, float>(Average).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Average`1", typeof(Enumerable))]
	public static float? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<float?>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, float?>>, float?>(Average).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Average`1", typeof(Enumerable))]
	public static double Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<double>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, long>>, double>(Average).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Average`1", typeof(Enumerable))]
	public static double? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<double?>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, long?>>, double?>(Average).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Average`1", typeof(Enumerable))]
	public static double Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<double>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, double>>, double>(Average).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Average`1", typeof(Enumerable))]
	public static double? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<double?>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, double?>>, double?>(Average).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Average`1", typeof(Enumerable))]
	public static decimal Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<decimal>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, decimal>>, decimal>(Average).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Average`1", typeof(Enumerable))]
	public static decimal? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<decimal?>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, decimal?>>, decimal?>(Average).Method, source.Expression, Expression.Quote(selector)));
	}

	[DynamicDependency("Aggregate`1", typeof(Enumerable))]
	public static TSource Aggregate<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, TSource, TSource>> func)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(func, "func");
		return source.Provider.Execute<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TSource, TSource>>, TSource>(Aggregate).Method, source.Expression, Expression.Quote(func)));
	}

	[DynamicDependency("Aggregate`2", typeof(Enumerable))]
	public static TAccumulate Aggregate<TSource, TAccumulate>(this IQueryable<TSource> source, TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(func, "func");
		return source.Provider.Execute<TAccumulate>(Expression.Call(null, new Func<IQueryable<TSource>, TAccumulate, Expression<Func<TAccumulate, TSource, TAccumulate>>, TAccumulate>(Aggregate).Method, source.Expression, Expression.Constant(seed), Expression.Quote(func)));
	}

	[DynamicDependency("Aggregate`3", typeof(Enumerable))]
	public static TResult Aggregate<TSource, TAccumulate, TResult>(this IQueryable<TSource> source, TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func, Expression<Func<TAccumulate, TResult>> selector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(func, "func");
		ArgumentNullException.ThrowIfNull(selector, "selector");
		return source.Provider.Execute<TResult>(Expression.Call(null, new Func<IQueryable<TSource>, TAccumulate, Expression<Func<TAccumulate, TSource, TAccumulate>>, Expression<Func<TAccumulate, TResult>>, TResult>(Aggregate).Method, source.Expression, Expression.Constant(seed), Expression.Quote(func), Expression.Quote(selector)));
	}

	[DynamicDependency("SkipLast`1", typeof(Enumerable))]
	public static IQueryable<TSource> SkipLast<TSource>(this IQueryable<TSource> source, int count)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, int, IQueryable<TSource>>(SkipLast).Method, source.Expression, Expression.Constant(count)));
	}

	[DynamicDependency("TakeLast`1", typeof(Enumerable))]
	public static IQueryable<TSource> TakeLast<TSource>(this IQueryable<TSource> source, int count)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, int, IQueryable<TSource>>(TakeLast).Method, source.Expression, Expression.Constant(count)));
	}

	[DynamicDependency("Append`1", typeof(Enumerable))]
	public static IQueryable<TSource> Append<TSource>(this IQueryable<TSource> source, TSource element)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, TSource, IQueryable<TSource>>(Append).Method, source.Expression, Expression.Constant(element)));
	}

	[DynamicDependency("Prepend`1", typeof(Enumerable))]
	public static IQueryable<TSource> Prepend<TSource>(this IQueryable<TSource> source, TSource element)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Provider.CreateQuery<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, TSource, IQueryable<TSource>>(Prepend).Method, source.Expression, Expression.Constant(element)));
	}
}
