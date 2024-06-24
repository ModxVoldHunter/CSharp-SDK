using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic.Utils;

namespace System.Linq.Expressions;

internal sealed class Block3 : BlockExpression
{
	private object _arg0;

	private readonly Expression _arg1;

	private readonly Expression _arg2;

	internal override int ExpressionCount => 3;

	internal Block3(Expression arg0, Expression arg1, Expression arg2)
	{
		_arg0 = arg0;
		_arg1 = arg1;
		_arg2 = arg2;
	}

	internal override bool SameExpressions(ICollection<Expression> expressions)
	{
		if (expressions.Count == 3)
		{
			if (_arg0 is ReadOnlyCollection<Expression> current)
			{
				return ExpressionUtils.SameElements(expressions, current);
			}
			using IEnumerator<Expression> enumerator = expressions.GetEnumerator();
			enumerator.MoveNext();
			if (enumerator.Current == _arg0)
			{
				enumerator.MoveNext();
				if (enumerator.Current == _arg1)
				{
					enumerator.MoveNext();
					return enumerator.Current == _arg2;
				}
			}
		}
		return false;
	}

	internal override Expression GetExpression(int index)
	{
		return index switch
		{
			0 => ExpressionUtils.ReturnObject<Expression>(_arg0), 
			1 => _arg1, 
			2 => _arg2, 
			_ => throw Error.ArgumentOutOfRange("index"), 
		};
	}

	internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
	{
		return BlockExpression.ReturnReadOnlyExpressions(this, ref _arg0);
	}

	internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
	{
		return new Block3(args[0], args[1], args[2]);
	}
}
