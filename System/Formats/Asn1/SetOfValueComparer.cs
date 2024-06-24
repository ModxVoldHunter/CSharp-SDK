using System.Collections.Generic;

namespace System.Formats.Asn1;

internal sealed class SetOfValueComparer : IComparer<ReadOnlyMemory<byte>>
{
	internal static SetOfValueComparer Instance { get; } = new SetOfValueComparer();


	public int Compare(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
	{
		return Compare(x.Span, y.Span);
	}

	internal static int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
	{
		int num = Math.Min(x.Length, y.Length);
		int num2 = x.CommonPrefixLength(y);
		if (num2 != num)
		{
			return x[num2] - y[num2];
		}
		return x.Length - y.Length;
	}
}
