using System.Runtime.CompilerServices;

namespace System.Drawing;

internal static class KnownColorTable
{
	public static ReadOnlySpan<uint> ColorValueTable => RuntimeHelpers.CreateSpan<uint>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	public static ReadOnlySpan<byte> ColorKindTable => new byte[176]
	{
		2, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 0, 0,
		0, 0, 0, 0, 0, 1
	};

	internal static Color ArgbToKnownColor(uint argb)
	{
		ReadOnlySpan<uint> colorValueTable = ColorValueTable;
		for (int i = 1; i < colorValueTable.Length; i++)
		{
			if (ColorKindTable[i] == 1 && colorValueTable[i] == argb)
			{
				return Color.FromKnownColor((KnownColor)i);
			}
		}
		return Color.FromArgb((int)argb);
	}

	public static uint KnownColorToArgb(KnownColor color)
	{
		if (ColorKindTable[(int)color] != 0)
		{
			return ColorValueTable[(int)color];
		}
		return GetSystemColorArgb(color);
	}

	public static uint GetSystemColorArgb(KnownColor color)
	{
		return ColorTranslator.COLORREFToARGB(global::Interop.User32.GetSysColor((byte)ColorValueTable[(int)color]));
	}
}
