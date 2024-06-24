namespace System.Collections.Generic;

internal interface IInternalStringEqualityComparer : IEqualityComparer<string>
{
	IEqualityComparer<string> GetUnderlyingEqualityComparer();

	internal static IEqualityComparer<string> GetUnderlyingEqualityComparer(IEqualityComparer<string> outerComparer)
	{
		if (outerComparer is IInternalStringEqualityComparer internalStringEqualityComparer)
		{
			return internalStringEqualityComparer.GetUnderlyingEqualityComparer();
		}
		return outerComparer;
	}
}
