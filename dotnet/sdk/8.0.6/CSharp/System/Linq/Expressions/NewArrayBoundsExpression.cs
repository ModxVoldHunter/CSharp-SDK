using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Expressions;

[RequiresDynamicCode("Creating arrays at runtime requires dynamic code generation.")]
internal sealed class NewArrayBoundsExpression : NewArrayExpression
{
	public sealed override ExpressionType NodeType => ExpressionType.NewArrayBounds;

	internal NewArrayBoundsExpression(Type type, ReadOnlyCollection<Expression> expressions)
		: base(type, expressions)
	{
	}
}
