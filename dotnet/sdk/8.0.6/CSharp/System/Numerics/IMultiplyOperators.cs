namespace System.Numerics;

public interface IMultiplyOperators<TSelf, TOther, TResult> where TSelf : IMultiplyOperators<TSelf, TOther, TResult>?
{
	static abstract TResult operator *(TSelf left, TOther right);

	static virtual TResult operator checked *(TSelf left, TOther right)
	{
		return left * right;
	}
}
