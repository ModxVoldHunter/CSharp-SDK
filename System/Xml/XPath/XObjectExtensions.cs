using System.Xml.Linq;

namespace System.Xml.XPath;

internal static class XObjectExtensions
{
	public static XContainer GetParent(this XObject obj)
	{
		XContainer xContainer = (XContainer)(((object)obj.Parent) ?? ((object)obj.Document));
		if (xContainer == obj)
		{
			return null;
		}
		return xContainer;
	}
}
