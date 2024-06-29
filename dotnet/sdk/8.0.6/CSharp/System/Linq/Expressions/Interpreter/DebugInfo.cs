using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Interpreter;

internal sealed class DebugInfo
{
	private sealed class DebugInfoComparer : IComparer<DebugInfo>
	{
		int IComparer<DebugInfo>.Compare(DebugInfo d1, DebugInfo d2)
		{
			if (d1.Index > d2.Index)
			{
				return 1;
			}
			if (d1.Index == d2.Index)
			{
				return 0;
			}
			return -1;
		}
	}

	public int StartLine;

	public int EndLine;

	public int Index;

	public string FileName;

	public bool IsClear;

	private static readonly DebugInfoComparer s_debugComparer = new DebugInfoComparer();

	public static DebugInfo GetMatchingDebugInfo(DebugInfo[] debugInfos, int index)
	{
		DebugInfo value = new DebugInfo
		{
			Index = index
		};
		int num = Array.BinarySearch(debugInfos, value, s_debugComparer);
		if (num < 0)
		{
			num = ~num;
			if (num == 0)
			{
				return null;
			}
			num--;
		}
		return debugInfos[num];
	}

	public override string ToString()
	{
		DefaultInterpolatedStringHandler handler;
		IFormatProvider invariantCulture;
		if (IsClear)
		{
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider = invariantCulture;
			handler = new DefaultInterpolatedStringHandler(7, 1, invariantCulture);
			handler.AppendFormatted(Index);
			handler.AppendLiteral(": clear");
			return string.Create(provider, ref handler);
		}
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		handler = new DefaultInterpolatedStringHandler(8, 4, invariantCulture);
		handler.AppendFormatted(Index);
		handler.AppendLiteral(": [");
		handler.AppendFormatted(StartLine);
		handler.AppendLiteral("-");
		handler.AppendFormatted(EndLine);
		handler.AppendLiteral("] '");
		handler.AppendFormatted(FileName);
		handler.AppendLiteral("'");
		return string.Create(provider2, ref handler);
	}
}
