using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Expressions.Interpreter;

internal readonly struct LocalDefinition : IEquatable<LocalDefinition>
{
	public int Index { get; }

	public ParameterExpression Parameter { get; }

	internal LocalDefinition(int localIndex, ParameterExpression parameter)
	{
		Index = localIndex;
		Parameter = parameter;
	}

	public override bool Equals([NotNullWhen(true)] object obj)
	{
		if (obj is LocalDefinition other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(LocalDefinition other)
	{
		if (other.Index == Index)
		{
			return other.Parameter == Parameter;
		}
		return false;
	}

	public override int GetHashCode()
	{
		if (Parameter != null)
		{
			return Parameter.GetHashCode() ^ Index.GetHashCode();
		}
		return 0;
	}
}
