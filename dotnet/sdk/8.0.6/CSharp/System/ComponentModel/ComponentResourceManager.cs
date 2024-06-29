using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace System.ComponentModel;

public class ComponentResourceManager : ResourceManager
{
	private Dictionary<CultureInfo, SortedList<string, object>> _resourceSets;

	private CultureInfo _neutralResourcesCulture;

	private CultureInfo? NeutralResourcesCulture
	{
		get
		{
			if (_neutralResourcesCulture == null && MainAssembly != null)
			{
				_neutralResourcesCulture = ResourceManager.GetNeutralResourcesLanguage(MainAssembly);
			}
			return _neutralResourcesCulture;
		}
	}

	public ComponentResourceManager()
	{
	}

	public ComponentResourceManager(Type t)
		: base(t)
	{
	}

	[RequiresUnreferencedCode("The Type of value cannot be statically discovered.")]
	public void ApplyResources(object value, string objectName)
	{
		ApplyResources(value, objectName, null);
	}

	[RequiresUnreferencedCode("The Type of value cannot be statically discovered.")]
	public virtual void ApplyResources(object value, string objectName, CultureInfo? culture)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		ArgumentNullException.ThrowIfNull(objectName, "objectName");
		if (culture == null)
		{
			culture = CultureInfo.CurrentUICulture;
		}
		SortedList<string, object> sortedList;
		ResourceSet resourceSet;
		if (_resourceSets == null)
		{
			_resourceSets = new Dictionary<CultureInfo, SortedList<string, object>>();
			sortedList = FillResources(culture, out resourceSet);
			_resourceSets[culture] = sortedList;
		}
		else
		{
			sortedList = _resourceSets.GetValueOrDefault(culture, null);
			if (sortedList == null || sortedList.Comparer.Equals(StringComparer.OrdinalIgnoreCase) != IgnoreCase)
			{
				sortedList = FillResources(culture, out resourceSet);
				_resourceSets[culture] = sortedList;
			}
		}
		BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty;
		if (IgnoreCase)
		{
			bindingFlags |= BindingFlags.IgnoreCase;
		}
		bool flag = false;
		if (value is IComponent)
		{
			ISite site = ((IComponent)value).Site;
			if (site != null && site.DesignMode)
			{
				flag = true;
			}
		}
		foreach (KeyValuePair<string, object> item in sortedList)
		{
			string key = item.Key;
			if (IgnoreCase)
			{
				if (string.Compare(key, 0, objectName, 0, objectName.Length, StringComparison.OrdinalIgnoreCase) != 0)
				{
					continue;
				}
			}
			else if (string.CompareOrdinal(key, 0, objectName, 0, objectName.Length) != 0)
			{
				continue;
			}
			int length = objectName.Length;
			if (key.Length <= length || (key[length] != '.' && key[length] != '-'))
			{
				continue;
			}
			string name = key.Substring(length + 1);
			if (flag)
			{
				PropertyDescriptor propertyDescriptor = TypeDescriptor.GetProperties(value).Find(name, IgnoreCase);
				if (propertyDescriptor != null && !propertyDescriptor.IsReadOnly && (item.Value == null || propertyDescriptor.PropertyType.IsInstanceOfType(item.Value)))
				{
					propertyDescriptor.SetValue(value, item.Value);
				}
				continue;
			}
			PropertyInfo property;
			try
			{
				property = value.GetType().GetProperty(name, bindingFlags);
			}
			catch (AmbiguousMatchException)
			{
				Type type = value.GetType();
				do
				{
					property = type.GetProperty(name, bindingFlags | BindingFlags.DeclaredOnly);
					type = type.BaseType;
				}
				while (property == null && type != null && type != typeof(object));
			}
			if (property != null && property.CanWrite && (item.Value == null || property.PropertyType.IsInstanceOfType(item.Value)))
			{
				property.SetValue(value, item.Value, null);
			}
		}
	}

	private SortedList<string, object> FillResources(CultureInfo culture, out ResourceSet resourceSet)
	{
		ResourceSet resourceSet2 = null;
		SortedList<string, object> sortedList = ((!culture.Equals(CultureInfo.InvariantCulture) && !culture.Equals(NeutralResourcesCulture)) ? FillResources(culture.Parent, out resourceSet2) : ((!IgnoreCase) ? new SortedList<string, object>(StringComparer.Ordinal) : new SortedList<string, object>(StringComparer.OrdinalIgnoreCase)));
		resourceSet = GetResourceSet(culture, createIfNotExists: true, tryParents: true);
		if (resourceSet != null && resourceSet != resourceSet2)
		{
			foreach (DictionaryEntry item in resourceSet)
			{
				sortedList[(string)item.Key] = item.Value;
			}
		}
		return sortedList;
	}
}
