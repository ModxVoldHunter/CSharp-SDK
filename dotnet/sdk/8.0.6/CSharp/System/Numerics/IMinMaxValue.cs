namespace System.Numerics;

public interface IMinMaxValue<TSelf> where TSelf : IMinMaxValue<TSelf>?
{
	static abstract TSelf MinValue { get; }

	static abstract TSelf MaxValue { get; }
}
