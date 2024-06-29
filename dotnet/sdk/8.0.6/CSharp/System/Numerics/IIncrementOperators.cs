namespace System.Numerics;

public interface IIncrementOperators<TSelf> where TSelf : IIncrementOperators<TSelf>?
{
	static abstract TSelf operator ++(TSelf value);

	static virtual TSelf operator checked ++(TSelf value)
	{
		return ++value;
	}
}
