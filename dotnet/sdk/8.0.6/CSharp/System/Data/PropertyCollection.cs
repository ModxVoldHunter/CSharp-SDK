using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class PropertyCollection : Hashtable, ICloneable
{
	public PropertyCollection()
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected PropertyCollection(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public override object Clone()
	{
		PropertyCollection propertyCollection = new PropertyCollection();
		IDictionaryEnumerator dictionaryEnumerator = GetEnumerator();
		try
		{
			while (dictionaryEnumerator.MoveNext())
			{
				DictionaryEntry dictionaryEntry = (DictionaryEntry)dictionaryEnumerator.Current;
				propertyCollection.Add(dictionaryEntry.Key, dictionaryEntry.Value);
			}
			return propertyCollection;
		}
		finally
		{
			IDisposable disposable = dictionaryEnumerator as IDisposable;
			if (disposable != null)
			{
				disposable.Dispose();
			}
		}
	}
}
