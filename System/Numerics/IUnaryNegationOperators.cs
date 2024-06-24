namespace System.Numerics;

public interface IUnaryNegationOperators<TSelf, TResult> where TSelf : IUnaryNegationOperators<TSelf, TResult>?
{
	static abstract TResult operator -(TSelf value);

	static virtual TResult operator checked -(TSelf value)
	{
		return -value;
	}
}
