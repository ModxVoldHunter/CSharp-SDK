using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic.Utils;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Compiler;

internal sealed class HoistedLocals
{
	internal readonly HoistedLocals Parent;

	internal readonly ReadOnlyDictionary<Expression, int> Indexes;

	internal readonly ReadOnlyCollection<ParameterExpression> Variables;

	internal readonly ParameterExpression SelfVariable;

	internal ParameterExpression ParentVariable => Parent?.SelfVariable;

	internal HoistedLocals(HoistedLocals parent, ReadOnlyCollection<ParameterExpression> vars)
	{
		if (parent != null)
		{
			vars = vars.AddFirst(parent.SelfVariable);
		}
		if (vars.Count != 0)
		{
			Dictionary<Expression, int> dictionary = new Dictionary<Expression, int>(vars.Count);
			for (int i = 0; i < vars.Count; i++)
			{
				dictionary.Add(vars[i], i);
			}
			Indexes = new ReadOnlyDictionary<Expression, int>(dictionary);
		}
		else
		{
			Indexes = ReadOnlyDictionary<Expression, int>.Empty;
		}
		Parent = parent;
		Variables = vars;
		SelfVariable = Expression.Variable(typeof(object[]), null);
	}

	internal static object[] GetParent(object[] locals)
	{
		return ((StrongBox<object[]>)locals[0]).Value;
	}
}
