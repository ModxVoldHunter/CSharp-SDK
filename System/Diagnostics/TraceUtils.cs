using System.Collections.Specialized;

namespace System.Diagnostics;

internal static class TraceUtils
{
	internal static void VerifyAttributes(StringDictionary attributes, string[] supportedAttributes, object parent)
	{
		ArgumentNullException.ThrowIfNull(attributes, "attributes");
		foreach (string key in attributes.Keys)
		{
			bool flag = false;
			if (supportedAttributes != null)
			{
				for (int i = 0; i < supportedAttributes.Length; i++)
				{
					if (supportedAttributes[i] == key)
					{
						flag = true;
					}
				}
			}
			if (!flag)
			{
				throw new ArgumentException(System.SR.Format(System.SR.AttributeNotSupported, key, parent.GetType().FullName));
			}
		}
	}
}
