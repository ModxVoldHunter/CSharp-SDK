namespace System.Xml.Xsl;

internal readonly struct StringPair
{
	public string Left { get; }

	public string Right { get; }

	public StringPair(string left, string right)
	{
		Left = left;
		Right = right;
	}
}
