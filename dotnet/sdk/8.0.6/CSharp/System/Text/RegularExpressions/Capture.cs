namespace System.Text.RegularExpressions;

public class Capture
{
	public int Index { get; private protected set; }

	public int Length { get; private protected set; }

	internal string? Text { get; set; }

	public string Value
	{
		get
		{
			string text = Text;
			if (text == null)
			{
				return string.Empty;
			}
			return text.Substring(Index, Length);
		}
	}

	public ReadOnlySpan<char> ValueSpan => Text?.AsSpan(Index, Length) ?? ReadOnlySpan<char>.Empty;

	internal Capture(string text, int index, int length)
	{
		Text = text;
		Index = index;
		Length = length;
	}

	public override string ToString()
	{
		return Value;
	}

	internal ReadOnlyMemory<char> GetLeftSubstring()
	{
		return Text?.AsMemory(0, Index) ?? ReadOnlyMemory<char>.Empty;
	}

	internal ReadOnlyMemory<char> GetRightSubstring()
	{
		return Text?.AsMemory(Index + Length, Text.Length - Index - Length) ?? ReadOnlyMemory<char>.Empty;
	}
}
