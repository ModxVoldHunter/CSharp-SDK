namespace System.Net.Http.Headers;

internal sealed class UnvalidatedObjectCollection<T> : ObjectCollection<T> where T : class
{
	public override void Validate(T item)
	{
		ArgumentNullException.ThrowIfNull(item, "item");
	}
}
