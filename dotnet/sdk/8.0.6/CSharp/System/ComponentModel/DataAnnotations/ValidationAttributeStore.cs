using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace System.ComponentModel.DataAnnotations;

internal sealed class ValidationAttributeStore
{
	private abstract class StoreItem
	{
		internal IEnumerable<ValidationAttribute> ValidationAttributes { get; }

		internal DisplayAttribute DisplayAttribute { get; }

		internal StoreItem(AttributeCollection attributes)
		{
			ValidationAttributes = attributes.OfType<ValidationAttribute>();
			DisplayAttribute = attributes.OfType<DisplayAttribute>().SingleOrDefault();
		}
	}

	private sealed class TypeStoreItem : StoreItem
	{
		private readonly object _syncRoot = new object();

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
		private readonly Type _type;

		private Dictionary<string, PropertyStoreItem> _propertyStoreItems;

		internal TypeStoreItem([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, AttributeCollection attributes)
			: base(attributes)
		{
			_type = type;
		}

		[RequiresUnreferencedCode("The Types of _type's properties cannot be statically discovered.")]
		internal PropertyStoreItem GetPropertyStoreItem(string propertyName)
		{
			if (!TryGetPropertyStoreItem(propertyName, out var item))
			{
				throw new ArgumentException(System.SR.Format(System.SR.AttributeStore_Unknown_Property, _type.Name, propertyName), "propertyName");
			}
			return item;
		}

		[RequiresUnreferencedCode("The Types of _type's properties cannot be statically discovered.")]
		internal bool TryGetPropertyStoreItem(string propertyName, [NotNullWhen(true)] out PropertyStoreItem item)
		{
			if (string.IsNullOrEmpty(propertyName))
			{
				throw new ArgumentNullException("propertyName");
			}
			if (_propertyStoreItems == null)
			{
				lock (_syncRoot)
				{
					if (_propertyStoreItems == null)
					{
						_propertyStoreItems = CreatePropertyStoreItems();
					}
				}
			}
			return _propertyStoreItems.TryGetValue(propertyName, out item);
		}

		[RequiresUnreferencedCode("The Types of _type's properties cannot be statically discovered.")]
		private Dictionary<string, PropertyStoreItem> CreatePropertyStoreItems()
		{
			Dictionary<string, PropertyStoreItem> dictionary = new Dictionary<string, PropertyStoreItem>();
			PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(_type);
			foreach (PropertyDescriptor item in properties)
			{
				PropertyStoreItem value = new PropertyStoreItem(item.PropertyType, GetExplicitAttributes(item));
				dictionary[item.Name] = value;
			}
			return dictionary;
		}

		[RequiresUnreferencedCode("The Type of propertyDescriptor.PropertyType cannot be statically discovered.")]
		private static AttributeCollection GetExplicitAttributes(PropertyDescriptor propertyDescriptor)
		{
			AttributeCollection attributes = propertyDescriptor.Attributes;
			List<Attribute> list = new List<Attribute>(attributes.Count);
			foreach (Attribute item in attributes)
			{
				list.Add(item);
			}
			AttributeCollection attributes2 = TypeDescriptor.GetAttributes(propertyDescriptor.PropertyType);
			bool flag = false;
			foreach (Attribute item2 in attributes2)
			{
				for (int num = list.Count - 1; num >= 0; num--)
				{
					if (item2 == list[num])
					{
						list.RemoveAt(num);
						flag = true;
					}
				}
			}
			if (!flag)
			{
				return attributes;
			}
			return new AttributeCollection(list.ToArray());
		}
	}

	private sealed class PropertyStoreItem : StoreItem
	{
		internal Type PropertyType { get; }

		internal PropertyStoreItem(Type propertyType, AttributeCollection attributes)
			: base(attributes)
		{
			PropertyType = propertyType;
		}
	}

	private readonly ConcurrentDictionary<Type, TypeStoreItem> _typeStoreItems = new ConcurrentDictionary<Type, TypeStoreItem>();

	internal static ValidationAttributeStore Instance { get; } = new ValidationAttributeStore();


	[RequiresUnreferencedCode("The Type of validationContext.ObjectType cannot be statically discovered.")]
	internal IEnumerable<ValidationAttribute> GetTypeValidationAttributes(ValidationContext validationContext)
	{
		EnsureValidationContext(validationContext);
		TypeStoreItem typeStoreItem = GetTypeStoreItem(validationContext.ObjectType);
		return typeStoreItem.ValidationAttributes;
	}

	[RequiresUnreferencedCode("The Type of validationContext.ObjectType cannot be statically discovered.")]
	internal DisplayAttribute GetTypeDisplayAttribute(ValidationContext validationContext)
	{
		EnsureValidationContext(validationContext);
		TypeStoreItem typeStoreItem = GetTypeStoreItem(validationContext.ObjectType);
		return typeStoreItem.DisplayAttribute;
	}

	[RequiresUnreferencedCode("The Type of validationContext.ObjectType cannot be statically discovered.")]
	internal IEnumerable<ValidationAttribute> GetPropertyValidationAttributes(ValidationContext validationContext)
	{
		EnsureValidationContext(validationContext);
		TypeStoreItem typeStoreItem = GetTypeStoreItem(validationContext.ObjectType);
		PropertyStoreItem propertyStoreItem = typeStoreItem.GetPropertyStoreItem(validationContext.MemberName);
		return propertyStoreItem.ValidationAttributes;
	}

	[RequiresUnreferencedCode("The Type of validationContext.ObjectType cannot be statically discovered.")]
	internal DisplayAttribute GetPropertyDisplayAttribute(ValidationContext validationContext)
	{
		EnsureValidationContext(validationContext);
		TypeStoreItem typeStoreItem = GetTypeStoreItem(validationContext.ObjectType);
		PropertyStoreItem propertyStoreItem = typeStoreItem.GetPropertyStoreItem(validationContext.MemberName);
		return propertyStoreItem.DisplayAttribute;
	}

	[RequiresUnreferencedCode("The Type of validationContext.ObjectType cannot be statically discovered.")]
	internal Type GetPropertyType(ValidationContext validationContext)
	{
		EnsureValidationContext(validationContext);
		TypeStoreItem typeStoreItem = GetTypeStoreItem(validationContext.ObjectType);
		PropertyStoreItem propertyStoreItem = typeStoreItem.GetPropertyStoreItem(validationContext.MemberName);
		return propertyStoreItem.PropertyType;
	}

	[RequiresUnreferencedCode("The Type of validationContext.ObjectType cannot be statically discovered.")]
	internal bool IsPropertyContext(ValidationContext validationContext)
	{
		EnsureValidationContext(validationContext);
		TypeStoreItem typeStoreItem = GetTypeStoreItem(validationContext.ObjectType);
		PropertyStoreItem item;
		return typeStoreItem.TryGetPropertyStoreItem(validationContext.MemberName, out item);
	}

	private TypeStoreItem GetTypeStoreItem([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
	{
		return _typeStoreItems.GetOrAdd(type, AddTypeStoreItem);
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern", Justification = "The parameter in the parent method has already been marked DynamicallyAccessedMemberTypes.All.")]
		static TypeStoreItem AddTypeStoreItem(Type type)
		{
			AttributeCollection attributes = TypeDescriptor.GetAttributes(type);
			return new TypeStoreItem(type, attributes);
		}
	}

	private static void EnsureValidationContext(ValidationContext validationContext)
	{
		ArgumentNullException.ThrowIfNull(validationContext, "validationContext");
	}
}
