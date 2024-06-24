namespace System.Numerics;

public interface IDecrementOperators<TSelf> where TSelf : IDecrementOperators<TSelf>?
{
	static abstract TSelf operator --(TSelf value);

	static virtual TSelf operator checked --(TSelf value)
	{
		return --value;
	}
}
