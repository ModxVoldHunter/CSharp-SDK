namespace System.Net.Http.Headers;

internal struct HeaderEntry
{
	public HeaderDescriptor Key;

	public object Value;

	public HeaderEntry(HeaderDescriptor key, object value)
	{
		Key = key;
		Value = value;
	}
}
