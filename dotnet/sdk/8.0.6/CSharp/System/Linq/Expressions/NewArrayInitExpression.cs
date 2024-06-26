using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Expressions;

[RequiresDynamicCode("Creating arrays at runtime requires dynamic code generation.")]
internal sealed class NewArrayInitExpression : NewArrayExpression
{
	public sealed override ExpressionType NodeType => ExpressionType.NewArrayInit;

	internal NewArrayInitExpression(Type type, ReadOnlyCollection<Expression> expressions)
		: base(type, expressions)
	{
	}
}
