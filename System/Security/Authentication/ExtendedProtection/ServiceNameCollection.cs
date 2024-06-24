using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Security.Authentication.ExtendedProtection;

public class ServiceNameCollection : ReadOnlyCollectionBase
{
	public ServiceNameCollection(ICollection items)
	{
		ArgumentNullException.ThrowIfNull(items, "items");
		AddIfNew(items, expectStrings: true);
	}

	private ServiceNameCollection(IList list, string serviceName)
		: this(list, 1)
	{
		AddIfNew(serviceName);
	}

	private ServiceNameCollection(IList list, IEnumerable serviceNames)
		: this(list, GetCountOrOne(serviceNames))
	{
		AddIfNew(serviceNames, expectStrings: false);
	}

	private ServiceNameCollection(IList list, int additionalCapacity)
	{
		foreach (string item in list)
		{
			base.InnerList.Add(item);
		}
	}

	public bool Contains(string? searchServiceName)
	{
		string b = NormalizeServiceName(searchServiceName);
		foreach (string inner in base.InnerList)
		{
			if (string.Equals(inner, b, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	public ServiceNameCollection Merge(string serviceName)
	{
		return new ServiceNameCollection(base.InnerList, serviceName);
	}

	public ServiceNameCollection Merge(IEnumerable serviceNames)
	{
		return new ServiceNameCollection(base.InnerList, serviceNames);
	}

	private void AddIfNew(IEnumerable serviceNames, bool expectStrings)
	{
		if (serviceNames is List<string> serviceNames2)
		{
			AddIfNew(serviceNames2);
			return;
		}
		if (serviceNames is ServiceNameCollection serviceNameCollection)
		{
			AddIfNew(serviceNameCollection.InnerList);
			return;
		}
		foreach (object serviceName in serviceNames)
		{
			AddIfNew(expectStrings ? ((string)serviceName) : (serviceName as string));
		}
	}

	private void AddIfNew(List<string> serviceNames)
	{
		foreach (string serviceName in serviceNames)
		{
			AddIfNew(serviceName);
		}
	}

	private void AddIfNew(IList serviceNames)
	{
		foreach (string serviceName in serviceNames)
		{
			AddIfNew(serviceName);
		}
	}

	private void AddIfNew(string serviceName)
	{
		ArgumentException.ThrowIfNullOrEmpty(serviceName, "serviceName");
		serviceName = NormalizeServiceName(serviceName);
		if (!Contains(serviceName))
		{
			base.InnerList.Add(serviceName);
		}
	}

	private static int GetCountOrOne(IEnumerable collection)
	{
		if (!(collection is ICollection<string> collection2))
		{
			return 1;
		}
		return collection2.Count;
	}

	[return: NotNullIfNotNull("inputServiceName")]
	private static string NormalizeServiceName(string inputServiceName)
	{
		if (string.IsNullOrWhiteSpace(inputServiceName))
		{
			return inputServiceName;
		}
		int num = inputServiceName.IndexOf('/');
		if (num < 0)
		{
			return inputServiceName;
		}
		ReadOnlySpan<char> readOnlySpan = inputServiceName.AsSpan(0, num + 1);
		string text = inputServiceName.Substring(num + 1);
		if (text.Length == 0)
		{
			return inputServiceName;
		}
		ReadOnlySpan<char> readOnlySpan2 = text;
		ReadOnlySpan<char> readOnlySpan3 = default(ReadOnlySpan<char>);
		ReadOnlySpan<char> readOnlySpan4 = default(ReadOnlySpan<char>);
		UriHostNameType uriHostNameType = Uri.CheckHostName(text);
		if (uriHostNameType == UriHostNameType.Unknown)
		{
			ReadOnlySpan<char> readOnlySpan5 = text;
			int num2 = text.IndexOf('/');
			if (num2 >= 0)
			{
				readOnlySpan5 = text.AsSpan(0, num2);
				readOnlySpan4 = text.AsSpan(num2);
				readOnlySpan2 = readOnlySpan5;
			}
			int num3 = readOnlySpan5.LastIndexOf(':');
			if (num3 >= 0)
			{
				readOnlySpan2 = readOnlySpan5.Slice(0, num3);
				readOnlySpan3 = readOnlySpan5.Slice(num3 + 1);
				if (!ushort.TryParse(readOnlySpan3, NumberStyles.Integer, CultureInfo.InvariantCulture, out var _))
				{
					return inputServiceName;
				}
				readOnlySpan3 = readOnlySpan5.Slice(num3);
			}
			uriHostNameType = Uri.CheckHostName((readOnlySpan2.Length == text.Length) ? text : readOnlySpan2.ToString());
		}
		if (uriHostNameType != UriHostNameType.Dns)
		{
			return inputServiceName;
		}
		if (!Uri.TryCreate("http://" + readOnlySpan2, UriKind.Absolute, out Uri result2))
		{
			return inputServiceName;
		}
		string components = result2.GetComponents(UriComponents.NormalizedHost, UriFormat.SafeUnescaped);
		string text2 = string.Concat(readOnlySpan, components, readOnlySpan3, readOnlySpan4);
		if (string.Equals(inputServiceName, text2, StringComparison.OrdinalIgnoreCase))
		{
			return inputServiceName;
		}
		return text2;
	}
}
