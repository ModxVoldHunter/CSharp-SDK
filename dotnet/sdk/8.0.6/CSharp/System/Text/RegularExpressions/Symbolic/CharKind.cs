namespace System.Text.RegularExpressions.Symbolic;

internal static class CharKind
{
	internal static uint Prev(uint context)
	{
		return context & 7u;
	}

	internal static uint Next(uint context)
	{
		return context >> 3;
	}

	internal static uint Context(uint prevKind, uint nextKind)
	{
		return (nextKind << 3) | prevKind;
	}
}
