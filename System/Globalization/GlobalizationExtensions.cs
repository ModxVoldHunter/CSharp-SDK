namespace System.Globalization;

public static class GlobalizationExtensions
{
	public static StringComparer GetStringComparer(this CompareInfo compareInfo, CompareOptions options)
	{
		ArgumentNullException.ThrowIfNull(compareInfo, "compareInfo");
		return options switch
		{
			CompareOptions.Ordinal => StringComparer.Ordinal, 
			CompareOptions.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase, 
			_ => new CultureAwareComparer(compareInfo, options), 
		};
	}
}
