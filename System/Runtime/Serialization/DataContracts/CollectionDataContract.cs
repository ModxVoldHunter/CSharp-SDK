using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Xml;

namespace System.Runtime.Serialization.DataContracts;

internal sealed class CollectionDataContract : DataContract
{
	private sealed class CollectionDataContractCriticalHelper : DataContractCriticalHelper
	{
		private delegate void IncrementCollectionCountDelegate(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context);

		private delegate IEnumerator CreateGenericDictionaryEnumeratorDelegate(IEnumerator enumerator);

		private static Type[] s_knownInterfaces;

		private Type _itemType;

		private bool _isItemTypeNullable;

		private CollectionKind _kind;

		private readonly MethodInfo _getEnumeratorMethod;

		private readonly MethodInfo _addMethod;

		private readonly ConstructorInfo _constructor;

		private readonly string _serializationExceptionMessage;

		private readonly string _deserializationExceptionMessage;

		private DataContract _itemContract;

		private DataContract _sharedTypeContract;

		private Dictionary<XmlQualifiedName, DataContract> _knownDataContracts;

		private bool _isKnownTypeAttributeChecked;

		private string _itemName;

		private bool _itemNameSetExplicit;

		private XmlDictionaryString _collectionItemName;

		private string _keyName;

		private string _valueName;

		private XmlDictionaryString _childElementNamespace;

		private readonly string _invalidCollectionInSharedContractMessage;

		private XmlFormatCollectionReaderDelegate _xmlFormatReaderDelegate;

		private XmlFormatGetOnlyCollectionReaderDelegate _xmlFormatGetOnlyCollectionReaderDelegate;

		private XmlFormatCollectionWriterDelegate _xmlFormatWriterDelegate;

		private bool _isConstructorCheckRequired;

		private IncrementCollectionCountDelegate _incrementCollectionCountDelegate;

		private static MethodInfo s_buildIncrementCollectionCountDelegateMethod;

		private CreateGenericDictionaryEnumeratorDelegate _createGenericDictionaryEnumeratorDelegate;

		private static MethodInfo s_buildCreateGenericDictionaryEnumerator;

		internal static Type[] KnownInterfaces
		{
			get
			{
				object obj = s_knownInterfaces;
				if (obj == null)
				{
					obj = new Type[8]
					{
						Globals.TypeOfIDictionaryGeneric,
						Globals.TypeOfIDictionary,
						Globals.TypeOfIListGeneric,
						Globals.TypeOfICollectionGeneric,
						Globals.TypeOfIList,
						Globals.TypeOfIEnumerableGeneric,
						Globals.TypeOfICollection,
						Globals.TypeOfIEnumerable
					};
					s_knownInterfaces = (Type[])obj;
				}
				return (Type[])obj;
			}
		}

		internal CollectionKind Kind => _kind;

		internal Type ItemType => _itemType;

		internal DataContract ItemContract
		{
			[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
			[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
			get
			{
				if (_itemContract == null)
				{
					if (IsDictionary)
					{
						if (KeyName == ValueName)
						{
							DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.DupKeyValueName, DataContract.GetClrTypeFullName(base.UnderlyingType), KeyName), base.UnderlyingType);
						}
						_itemContract = ClassDataContract.CreateClassDataContractForKeyValue(ItemType, base.Namespace, new string[2] { KeyName, ValueName });
						DataContract.GetDataContract(ItemType);
					}
					else
					{
						_itemContract = DataContract.GetDataContract(ItemType);
					}
				}
				return _itemContract;
			}
			set
			{
				_itemContract = value;
			}
		}

		internal DataContract SharedTypeContract
		{
			get
			{
				return _sharedTypeContract;
			}
			set
			{
				_sharedTypeContract = value;
			}
		}

		internal string ItemName
		{
			get
			{
				return _itemName;
			}
			set
			{
				_itemName = value;
			}
		}

		internal bool IsConstructorCheckRequired
		{
			get
			{
				return _isConstructorCheckRequired;
			}
			set
			{
				_isConstructorCheckRequired = value;
			}
		}

		internal XmlDictionaryString CollectionItemName => _collectionItemName;

		internal string KeyName
		{
			get
			{
				return _keyName;
			}
			set
			{
				_keyName = value;
			}
		}

		internal string ValueName
		{
			get
			{
				return _valueName;
			}
			set
			{
				_valueName = value;
			}
		}

		internal bool IsDictionary => KeyName != null;

		internal string SerializationExceptionMessage => _serializationExceptionMessage;

		internal string DeserializationExceptionMessage => _deserializationExceptionMessage;

		internal XmlDictionaryString ChildElementNamespace
		{
			get
			{
				return _childElementNamespace;
			}
			set
			{
				_childElementNamespace = value;
			}
		}

		internal bool IsItemTypeNullable
		{
			get
			{
				return _isItemTypeNullable;
			}
			set
			{
				_isItemTypeNullable = value;
			}
		}

		internal MethodInfo GetEnumeratorMethod => _getEnumeratorMethod;

		internal MethodInfo AddMethod => _addMethod;

		internal ConstructorInfo Constructor => _constructor;

		internal override Dictionary<XmlQualifiedName, DataContract> KnownDataContracts
		{
			[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
			[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
			get
			{
				if (!_isKnownTypeAttributeChecked && base.UnderlyingType != null)
				{
					lock (this)
					{
						if (!_isKnownTypeAttributeChecked)
						{
							_knownDataContracts = DataContract.ImportKnownTypeAttributes(base.UnderlyingType);
							Interlocked.MemoryBarrier();
							_isKnownTypeAttributeChecked = true;
						}
						if (_knownDataContracts == null)
						{
							_knownDataContracts = new Dictionary<XmlQualifiedName, DataContract>();
						}
					}
				}
				return _knownDataContracts;
			}
			set
			{
				_knownDataContracts = value;
			}
		}

		internal string InvalidCollectionInSharedContractMessage => _invalidCollectionInSharedContractMessage;

		internal bool ItemNameSetExplicit => _itemNameSetExplicit;

		internal XmlFormatCollectionWriterDelegate XmlFormatWriterDelegate
		{
			get
			{
				return _xmlFormatWriterDelegate;
			}
			set
			{
				_xmlFormatWriterDelegate = value;
			}
		}

		internal XmlFormatCollectionReaderDelegate XmlFormatReaderDelegate
		{
			get
			{
				return _xmlFormatReaderDelegate;
			}
			set
			{
				_xmlFormatReaderDelegate = value;
			}
		}

		internal XmlFormatGetOnlyCollectionReaderDelegate XmlFormatGetOnlyCollectionReaderDelegate
		{
			get
			{
				return _xmlFormatGetOnlyCollectionReaderDelegate;
			}
			set
			{
				_xmlFormatGetOnlyCollectionReaderDelegate = value;
			}
		}

		private static MethodInfo BuildIncrementCollectionCountDelegateMethod => s_buildIncrementCollectionCountDelegateMethod ?? (s_buildIncrementCollectionCountDelegateMethod = typeof(CollectionDataContractCriticalHelper).GetMethod("BuildIncrementCollectionCountDelegate", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));

		private static MethodInfo GetBuildCreateGenericDictionaryEnumeratorMethodInfo => s_buildCreateGenericDictionaryEnumerator ?? (s_buildCreateGenericDictionaryEnumerator = typeof(CollectionDataContractCriticalHelper).GetMethod("BuildCreateGenericDictionaryEnumerator", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void Init(CollectionKind kind, Type itemType, CollectionDataContractAttribute collectionContractAttribute)
		{
			_kind = kind;
			if (itemType != null)
			{
				_itemType = itemType;
				_isItemTypeNullable = DataContract.IsTypeNullable(itemType);
				bool flag = kind == CollectionKind.Dictionary || kind == CollectionKind.GenericDictionary;
				string text = null;
				string text2 = null;
				string text3 = null;
				if (collectionContractAttribute != null)
				{
					if (collectionContractAttribute.IsItemNameSetExplicitly)
					{
						if (string.IsNullOrEmpty(collectionContractAttribute.ItemName))
						{
							throw new InvalidDataContractException(System.SR.Format(System.SR.InvalidCollectionContractItemName, DataContract.GetClrTypeFullName(base.UnderlyingType)));
						}
						text = DataContract.EncodeLocalName(collectionContractAttribute.ItemName);
						_itemNameSetExplicit = true;
					}
					if (collectionContractAttribute.IsKeyNameSetExplicitly)
					{
						if (string.IsNullOrEmpty(collectionContractAttribute.KeyName))
						{
							throw new InvalidDataContractException(System.SR.Format(System.SR.InvalidCollectionContractKeyName, DataContract.GetClrTypeFullName(base.UnderlyingType)));
						}
						if (!flag)
						{
							throw new InvalidDataContractException(System.SR.Format(System.SR.InvalidCollectionContractKeyNoDictionary, DataContract.GetClrTypeFullName(base.UnderlyingType), collectionContractAttribute.KeyName));
						}
						text2 = DataContract.EncodeLocalName(collectionContractAttribute.KeyName);
					}
					if (collectionContractAttribute.IsValueNameSetExplicitly)
					{
						if (string.IsNullOrEmpty(collectionContractAttribute.ValueName))
						{
							throw new InvalidDataContractException(System.SR.Format(System.SR.InvalidCollectionContractValueName, DataContract.GetClrTypeFullName(base.UnderlyingType)));
						}
						if (!flag)
						{
							throw new InvalidDataContractException(System.SR.Format(System.SR.InvalidCollectionContractValueNoDictionary, DataContract.GetClrTypeFullName(base.UnderlyingType), collectionContractAttribute.ValueName));
						}
						text3 = DataContract.EncodeLocalName(collectionContractAttribute.ValueName);
					}
				}
				XmlDictionary xmlDictionary = (flag ? new XmlDictionary(5) : new XmlDictionary(3));
				base.Name = xmlDictionary.Add(base.XmlName.Name);
				base.Namespace = xmlDictionary.Add(base.XmlName.Namespace);
				_itemName = text ?? DataContract.GetXmlName(DataContract.UnwrapNullableType(itemType)).Name;
				_collectionItemName = xmlDictionary.Add(_itemName);
				if (flag)
				{
					_keyName = text2 ?? "Key";
					_valueName = text3 ?? "Value";
				}
			}
			if (collectionContractAttribute != null)
			{
				base.IsReference = collectionContractAttribute.IsReference;
			}
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal CollectionDataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
			: base(type)
		{
			if (type == Globals.TypeOfArray)
			{
				type = Globals.TypeOfObjectArray;
			}
			if (type.GetArrayRank() > 1)
			{
				throw new NotSupportedException(System.SR.SupportForMultidimensionalArraysNotPresent);
			}
			base.XmlName = DataContract.GetXmlName(type);
			Init(CollectionKind.Array, type.GetElementType(), null);
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal CollectionDataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, CollectionKind kind)
			: base(type)
		{
			base.XmlName = DataContract.GetXmlName(type);
			Init(kind, type.GetElementType(), null);
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal CollectionDataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, DataContract itemContract)
			: base(type)
		{
			if (type.GetArrayRank() > 1)
			{
				throw new NotSupportedException(System.SR.SupportForMultidimensionalArraysNotPresent);
			}
			base.XmlName = DataContract.CreateQualifiedName("ArrayOf" + itemContract.XmlName.Name, itemContract.XmlName.Namespace);
			_itemContract = itemContract;
			Init(CollectionKind.Array, type.GetElementType(), null);
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal CollectionDataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, string serializationExceptionMessage, string deserializationExceptionMessage)
			: base(type)
		{
			if (getEnumeratorMethod == null)
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.CollectionMustHaveGetEnumeratorMethod, DataContract.GetClrTypeFullName(type)));
			}
			if (itemType == null)
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.CollectionMustHaveItemType, DataContract.GetClrTypeFullName(type)));
			}
			base.XmlName = DataContract.GetCollectionXmlName(type, itemType, out var collectionContractAttribute);
			Init(kind, itemType, collectionContractAttribute);
			_getEnumeratorMethod = getEnumeratorMethod;
			_serializationExceptionMessage = serializationExceptionMessage;
			_deserializationExceptionMessage = deserializationExceptionMessage;
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal CollectionDataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo addMethod, ConstructorInfo constructor)
			: this(type, kind, itemType, getEnumeratorMethod, (string)null, (string)null)
		{
			if (addMethod == null && !type.IsInterface)
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.CollectionMustHaveAddMethod, DataContract.GetClrTypeFullName(type)));
			}
			_addMethod = addMethod;
			_constructor = constructor;
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal CollectionDataContractCriticalHelper(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo addMethod, ConstructorInfo constructor, bool isConstructorCheckRequired)
			: this(type, kind, itemType, getEnumeratorMethod, addMethod, constructor)
		{
			_isConstructorCheckRequired = isConstructorCheckRequired;
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal CollectionDataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, string invalidCollectionInSharedContractMessage)
			: base(type)
		{
			Init(CollectionKind.Collection, null, null);
			_invalidCollectionInSharedContractMessage = invalidCollectionInSharedContractMessage;
		}

		private static void DummyIncrementCollectionCount(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
		{
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		internal void IncrementCollectionCount(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
		{
			if (_incrementCollectionCountDelegate == null)
			{
				switch (Kind)
				{
				case CollectionKind.Dictionary:
				case CollectionKind.List:
				case CollectionKind.Collection:
					_incrementCollectionCountDelegate = delegate(XmlWriterDelegator x, object o, XmlObjectSerializerWriteContext c)
					{
						c.IncrementCollectionCount(x, (ICollection)o);
					};
					break;
				case CollectionKind.GenericList:
				case CollectionKind.GenericCollection:
				{
					MethodInfo methodInfo = GetBuildIncrementCollectionCountGenericDelegate(ItemType);
					_incrementCollectionCountDelegate = (IncrementCollectionCountDelegate)methodInfo.Invoke(null, Array.Empty<object>());
					break;
				}
				case CollectionKind.GenericDictionary:
				{
					MethodInfo methodInfo2 = GetBuildIncrementCollectionCountGenericDelegate(typeof(KeyValuePair<, >).MakeGenericType(ItemType.GetGenericArguments()));
					_incrementCollectionCountDelegate = (IncrementCollectionCountDelegate)methodInfo2.Invoke(null, Array.Empty<object>());
					break;
				}
				default:
					_incrementCollectionCountDelegate = DummyIncrementCollectionCount;
					break;
				}
			}
			_incrementCollectionCountDelegate(xmlWriter, obj, context);
			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod", Justification = "The call to MakeGenericMethod is safe due to the fact that CollectionDataContractCriticalHelper.BuildIncrementCollectionCountDelegate<T> is not annotated.")]
			static MethodInfo GetBuildIncrementCollectionCountGenericDelegate(Type type)
			{
				return BuildIncrementCollectionCountDelegateMethod.MakeGenericMethod(type);
			}
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod", Justification = "The call to MakeGenericMethod is safe due to the fact that CollectionDataContractCriticalHelper.BuildIncrementCollectionCountDelegate<T> is not annotated.")]
		private static MethodInfo GetBuildIncrementCollectionCountGenericDelegate(Type type)
		{
			return BuildIncrementCollectionCountDelegateMethod.MakeGenericMethod(type);
		}

		private static IncrementCollectionCountDelegate BuildIncrementCollectionCountDelegate<T>()
		{
			return delegate(XmlWriterDelegator xmlwriter, object obj, XmlObjectSerializerWriteContext context)
			{
				context.IncrementCollectionCountGeneric(xmlwriter, (ICollection<T>)obj);
			};
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		internal IEnumerator GetEnumeratorForCollection(object obj)
		{
			IEnumerator enumerator = ((IEnumerable)obj).GetEnumerator();
			if (Kind == CollectionKind.GenericDictionary)
			{
				if (_createGenericDictionaryEnumeratorDelegate == null)
				{
					Type[] genericArguments = ItemType.GetGenericArguments();
					MethodInfo methodInfo = GetBuildCreateGenericDictionaryEnumeratorGenericMethod(genericArguments);
					_createGenericDictionaryEnumeratorDelegate = (CreateGenericDictionaryEnumeratorDelegate)methodInfo.Invoke(null, Array.Empty<object>());
				}
				enumerator = _createGenericDictionaryEnumeratorDelegate(enumerator);
			}
			else if (Kind == CollectionKind.Dictionary)
			{
				enumerator = new DictionaryEnumerator(((IDictionary)obj).GetEnumerator());
			}
			return enumerator;
			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod", Justification = "The call to MakeGenericMethod is safe due to the fact that CollectionDataContractCriticalHelper.BuildCreateGenericDictionaryEnumerator<K,V> is not annotated.")]
			static MethodInfo GetBuildCreateGenericDictionaryEnumeratorGenericMethod(Type[] keyValueTypes)
			{
				return GetBuildCreateGenericDictionaryEnumeratorMethodInfo.MakeGenericMethod(keyValueTypes[0], keyValueTypes[1]);
			}
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal Type GetCollectionElementType()
		{
			Type type;
			if (Kind != CollectionKind.GenericDictionary)
			{
				type = ((Kind != CollectionKind.Dictionary) ? GetEnumeratorMethod.ReturnType : Globals.TypeOfDictionaryEnumerator);
			}
			else
			{
				Type[] genericArguments = ItemType.GetGenericArguments();
				type = Globals.TypeOfGenericDictionaryEnumerator.MakeGenericType(genericArguments);
			}
			MethodInfo methodInfo = type.GetMethod("get_Current", BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes);
			if (methodInfo == null)
			{
				if (type.IsInterface)
				{
					methodInfo = XmlFormatGeneratorStatics.GetCurrentMethod;
				}
				else
				{
					Type interfaceType = Globals.TypeOfIEnumerator;
					if (Kind == CollectionKind.GenericDictionary || Kind == CollectionKind.GenericCollection || Kind == CollectionKind.GenericEnumerable)
					{
						Type[] interfaces = type.GetInterfaces();
						Type[] array = interfaces;
						foreach (Type type2 in array)
						{
							if (type2.IsGenericType && type2.GetGenericTypeDefinition() == Globals.TypeOfIEnumeratorGeneric && type2.GetGenericArguments()[0] == ItemType)
							{
								interfaceType = type2;
								break;
							}
						}
					}
					methodInfo = GetTargetMethodWithName("get_Current", type, interfaceType);
				}
			}
			return methodInfo.ReturnType;
		}

		private static CreateGenericDictionaryEnumeratorDelegate BuildCreateGenericDictionaryEnumerator<K, V>()
		{
			return (IEnumerator enumerator) => new GenericDictionaryEnumerator<K, V>((IEnumerator<KeyValuePair<K, V>>)enumerator);
		}
	}

	internal sealed class DictionaryEnumerator : IEnumerator<KeyValue<object, object>>, IEnumerator, IDisposable
	{
		private readonly IDictionaryEnumerator _enumerator;

		public KeyValue<object, object> Current => new KeyValue<object, object>(_enumerator.Key, _enumerator.Value);

		object IEnumerator.Current => Current;

		public DictionaryEnumerator(IDictionaryEnumerator enumerator)
		{
			_enumerator = enumerator;
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}

		public bool MoveNext()
		{
			return _enumerator.MoveNext();
		}

		public void Reset()
		{
			_enumerator.Reset();
		}
	}

	internal sealed class GenericDictionaryEnumerator<K, V> : IEnumerator<KeyValue<K, V>>, IEnumerator, IDisposable
	{
		private readonly IEnumerator<KeyValuePair<K, V>> _enumerator;

		public KeyValue<K, V> Current
		{
			get
			{
				KeyValuePair<K, V> current = _enumerator.Current;
				return new KeyValue<K, V>(current.Key, current.Value);
			}
		}

		object IEnumerator.Current => Current;

		public GenericDictionaryEnumerator(IEnumerator<KeyValuePair<K, V>> enumerator)
		{
			_enumerator = enumerator;
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}

		public bool MoveNext()
		{
			return _enumerator.MoveNext();
		}

		public void Reset()
		{
			_enumerator.Reset();
		}
	}

	internal const string ContractTypeString = "CollectionDataContract";

	private XmlDictionaryString _collectionItemName;

	private XmlDictionaryString _childElementNamespace;

	private DataContract _itemContract;

	private CollectionDataContractCriticalHelper _helper;

	public override string ContractType => "CollectionDataContract";

	private static Type[] KnownInterfaces => CollectionDataContractCriticalHelper.KnownInterfaces;

	internal CollectionKind Kind => _helper.Kind;

	internal Type ItemType => _helper.ItemType;

	internal DataContract ItemContract
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			return _itemContract ?? _helper.ItemContract;
		}
		set
		{
			_itemContract = value;
			_helper.ItemContract = value;
		}
	}

	internal DataContract SharedTypeContract => _helper.SharedTypeContract;

	internal string ItemName
	{
		get
		{
			return _helper.ItemName;
		}
		set
		{
			_helper.ItemName = value;
		}
	}

	internal XmlDictionaryString CollectionItemName => _collectionItemName;

	internal string KeyName
	{
		get
		{
			return _helper.KeyName;
		}
		set
		{
			_helper.KeyName = value;
		}
	}

	internal string ValueName
	{
		get
		{
			return _helper.ValueName;
		}
		set
		{
			_helper.ValueName = value;
		}
	}

	public override DataContract BaseContract
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			return ItemContract;
		}
	}

	internal bool IsDictionary => KeyName != null;

	internal XmlDictionaryString ChildElementNamespace
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (_childElementNamespace == null)
			{
				lock (this)
				{
					if (_childElementNamespace == null)
					{
						if (_helper.ChildElementNamespace == null && !IsDictionary)
						{
							XmlDictionaryString childNamespaceToDeclare = ClassDataContract.GetChildNamespaceToDeclare(this, ItemType, new XmlDictionary());
							Interlocked.MemoryBarrier();
							_helper.ChildElementNamespace = childNamespaceToDeclare;
						}
						_childElementNamespace = _helper.ChildElementNamespace;
					}
				}
			}
			return _childElementNamespace;
		}
	}

	internal bool IsItemTypeNullable
	{
		get
		{
			return _helper.IsItemTypeNullable;
		}
		set
		{
			_helper.IsItemTypeNullable = value;
		}
	}

	internal bool IsConstructorCheckRequired
	{
		get
		{
			return _helper.IsConstructorCheckRequired;
		}
		set
		{
			_helper.IsConstructorCheckRequired = value;
		}
	}

	internal MethodInfo GetEnumeratorMethod => _helper.GetEnumeratorMethod;

	internal MethodInfo AddMethod => _helper.AddMethod;

	internal ConstructorInfo Constructor => _helper.Constructor;

	public override Dictionary<XmlQualifiedName, DataContract> KnownDataContracts
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			return _helper.KnownDataContracts;
		}
		internal set
		{
			_helper.KnownDataContracts = value;
		}
	}

	internal string InvalidCollectionInSharedContractMessage => _helper.InvalidCollectionInSharedContractMessage;

	internal string SerializationExceptionMessage => _helper.SerializationExceptionMessage;

	internal string DeserializationExceptionMessage => _helper.DeserializationExceptionMessage;

	internal bool IsReadOnlyContract => DeserializationExceptionMessage != null;

	private bool ItemNameSetExplicit => _helper.ItemNameSetExplicit;

	internal XmlFormatCollectionWriterDelegate XmlFormatWriterDelegate
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (_helper.XmlFormatWriterDelegate == null)
			{
				lock (this)
				{
					if (_helper.XmlFormatWriterDelegate == null)
					{
						XmlFormatCollectionWriterDelegate xmlFormatWriterDelegate = CreateXmlFormatWriterDelegate();
						Interlocked.MemoryBarrier();
						_helper.XmlFormatWriterDelegate = xmlFormatWriterDelegate;
					}
				}
			}
			return _helper.XmlFormatWriterDelegate;
		}
	}

	internal XmlFormatCollectionReaderDelegate XmlFormatReaderDelegate
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (_helper.XmlFormatReaderDelegate == null)
			{
				lock (this)
				{
					if (_helper.XmlFormatReaderDelegate == null)
					{
						if (IsReadOnlyContract)
						{
							DataContract.ThrowInvalidDataContractException(_helper.DeserializationExceptionMessage, null);
						}
						XmlFormatCollectionReaderDelegate xmlFormatReaderDelegate = CreateXmlFormatReaderDelegate();
						Interlocked.MemoryBarrier();
						_helper.XmlFormatReaderDelegate = xmlFormatReaderDelegate;
					}
				}
			}
			return _helper.XmlFormatReaderDelegate;
		}
	}

	internal XmlFormatGetOnlyCollectionReaderDelegate XmlFormatGetOnlyCollectionReaderDelegate
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (_helper.XmlFormatGetOnlyCollectionReaderDelegate == null)
			{
				lock (this)
				{
					if (_helper.XmlFormatGetOnlyCollectionReaderDelegate == null)
					{
						if (UnderlyingType.IsInterface && (Kind == CollectionKind.Enumerable || Kind == CollectionKind.Collection || Kind == CollectionKind.GenericEnumerable))
						{
							throw new InvalidDataContractException(System.SR.Format(System.SR.GetOnlyCollectionMustHaveAddMethod, DataContract.GetClrTypeFullName(UnderlyingType)));
						}
						if (IsReadOnlyContract)
						{
							DataContract.ThrowInvalidDataContractException(_helper.DeserializationExceptionMessage, null);
						}
						if (Kind != CollectionKind.Array && AddMethod == null)
						{
							throw new InvalidDataContractException(System.SR.Format(System.SR.GetOnlyCollectionMustHaveAddMethod, DataContract.GetClrTypeFullName(UnderlyingType)));
						}
						XmlFormatGetOnlyCollectionReaderDelegate xmlFormatGetOnlyCollectionReaderDelegate = CreateXmlFormatGetOnlyCollectionReaderDelegate();
						Interlocked.MemoryBarrier();
						_helper.XmlFormatGetOnlyCollectionReaderDelegate = xmlFormatGetOnlyCollectionReaderDelegate;
					}
				}
			}
			return _helper.XmlFormatGetOnlyCollectionReaderDelegate;
		}
		set
		{
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal CollectionDataContract(Type type)
		: base(new CollectionDataContractCriticalHelper(type))
	{
		InitCollectionDataContract(this);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal CollectionDataContract(Type type, DataContract itemContract)
		: base(new CollectionDataContractCriticalHelper(type, itemContract))
	{
		InitCollectionDataContract(this);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal CollectionDataContract(Type type, CollectionKind kind)
		: base(new CollectionDataContractCriticalHelper(type, kind))
	{
		InitCollectionDataContract(this);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private CollectionDataContract(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, string serializationExceptionMessage, string deserializationExceptionMessage)
		: base(new CollectionDataContractCriticalHelper(type, kind, itemType, getEnumeratorMethod, serializationExceptionMessage, deserializationExceptionMessage))
	{
		InitCollectionDataContract(GetSharedTypeContract(type));
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private CollectionDataContract(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo addMethod, ConstructorInfo constructor)
		: base(new CollectionDataContractCriticalHelper(type, kind, itemType, getEnumeratorMethod, addMethod, constructor))
	{
		InitCollectionDataContract(GetSharedTypeContract(type));
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private CollectionDataContract(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo addMethod, ConstructorInfo constructor, bool isConstructorCheckRequired)
		: base(new CollectionDataContractCriticalHelper(type, kind, itemType, getEnumeratorMethod, addMethod, constructor, isConstructorCheckRequired))
	{
		InitCollectionDataContract(GetSharedTypeContract(type));
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private CollectionDataContract(Type type, string invalidCollectionInSharedContractMessage)
		: base(new CollectionDataContractCriticalHelper(type, invalidCollectionInSharedContractMessage))
	{
		InitCollectionDataContract(GetSharedTypeContract(type));
	}

	[MemberNotNull("_helper")]
	[MemberNotNull("_collectionItemName")]
	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void InitCollectionDataContract(DataContract sharedTypeContract)
	{
		_helper = base.Helper as CollectionDataContractCriticalHelper;
		_collectionItemName = _helper.CollectionItemName;
		if (_helper.Kind == CollectionKind.Dictionary || _helper.Kind == CollectionKind.GenericDictionary)
		{
			_itemContract = _helper.ItemContract;
		}
		_helper.SharedTypeContract = sharedTypeContract;
	}

	public override bool IsDictionaryLike([NotNullWhen(true)] out string keyName, [NotNullWhen(true)] out string valueName, [NotNullWhen(true)] out string itemName)
	{
		keyName = KeyName;
		valueName = ValueName;
		itemName = ItemName;
		return IsDictionary;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private XmlFormatCollectionWriterDelegate CreateXmlFormatWriterDelegate()
	{
		return new XmlFormatWriterGenerator().GenerateCollectionWriter(this);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private XmlFormatCollectionReaderDelegate CreateXmlFormatReaderDelegate()
	{
		return new XmlFormatReaderGenerator().GenerateCollectionReader(this);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private XmlFormatGetOnlyCollectionReaderDelegate CreateXmlFormatGetOnlyCollectionReaderDelegate()
	{
		return new XmlFormatReaderGenerator().GenerateGetOnlyCollectionReader(this);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	internal void IncrementCollectionCount(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
	{
		_helper.IncrementCollectionCount(xmlWriter, obj, context);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	internal IEnumerator GetEnumeratorForCollection(object obj)
	{
		return _helper.GetEnumeratorForCollection(obj);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal Type GetCollectionElementType()
	{
		return _helper.GetCollectionElementType();
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private DataContract GetSharedTypeContract(Type type)
	{
		if (type.IsDefined(Globals.TypeOfCollectionDataContractAttribute, inherit: false))
		{
			return this;
		}
		if (type.IsSerializable || type.IsDefined(Globals.TypeOfDataContractAttribute, inherit: false))
		{
			return new ClassDataContract(type);
		}
		return null;
	}

	internal static bool IsCollectionInterface(Type type)
	{
		if (type.IsGenericType)
		{
			type = type.GetGenericTypeDefinition();
		}
		return ((ICollection<Type>)KnownInterfaces).Contains(type);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static bool IsCollection(Type type)
	{
		Type itemType;
		return IsCollection(type, out itemType);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static bool IsCollection(Type type, [NotNullWhen(true)] out Type itemType)
	{
		return IsCollectionHelper(type, out itemType, constructorRequired: true);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static bool IsCollection(Type type, bool constructorRequired, bool skipIfReadOnlyContract)
	{
		Type itemType;
		return IsCollectionHelper(type, out itemType, constructorRequired, skipIfReadOnlyContract);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static bool IsCollectionHelper(Type type, [NotNullWhen(true)] out Type itemType, bool constructorRequired, bool skipIfReadOnlyContract = false)
	{
		if (type.IsArray && DataContract.GetBuiltInDataContract(type) == null)
		{
			itemType = type.GetElementType();
			return true;
		}
		DataContract dataContract;
		return IsCollectionOrTryCreate(type, tryCreate: false, out dataContract, out itemType, constructorRequired, skipIfReadOnlyContract);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static bool TryCreate(Type type, [NotNullWhen(true)] out DataContract dataContract)
	{
		Type itemType;
		return IsCollectionOrTryCreate(type, tryCreate: true, out dataContract, out itemType, constructorRequired: true);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static bool TryCreateGetOnlyCollectionDataContract(Type type, [NotNullWhen(true)] out DataContract dataContract)
	{
		if (type.IsArray)
		{
			dataContract = new CollectionDataContract(type);
			return true;
		}
		Type itemType;
		return IsCollectionOrTryCreate(type, tryCreate: true, out dataContract, out itemType, constructorRequired: false);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:GetMethod", Justification = "The DynamicallyAccessedMembers declarations will ensure the interface methods will be preserved.")]
	internal static MethodInfo GetTargetMethodWithName(string name, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type interfaceType)
	{
		return (from it in type.GetInterfaces()
			where it.Equals(interfaceType)
			select it).FirstOrDefault()?.GetMethod(name);
	}

	private static bool IsArraySegment(Type t)
	{
		if (t.IsGenericType)
		{
			return t.GetGenericTypeDefinition() == typeof(ArraySegment<>);
		}
		return false;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static bool IsCollectionOrTryCreate(Type type, bool tryCreate, out DataContract dataContract, out Type itemType, bool constructorRequired, bool skipIfReadOnlyContract = false)
	{
		dataContract = null;
		itemType = Globals.TypeOfObject;
		if (DataContract.GetBuiltInDataContract(type) != null)
		{
			return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract: false, createContractWithException: false, System.SR.CollectionTypeCannotBeBuiltIn, null, ref dataContract);
		}
		bool hasCollectionDataContract = IsCollectionDataContract(type);
		bool flag = false;
		string serializationExceptionMessage = null;
		string deserializationExceptionMessage = null;
		Type baseType = type.BaseType;
		bool flag2 = baseType != null && baseType != Globals.TypeOfObject && baseType != Globals.TypeOfValueType && baseType != Globals.TypeOfUri && IsCollection(baseType) && !type.IsSerializable;
		if (type.IsDefined(Globals.TypeOfDataContractAttribute, inherit: false))
		{
			return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, flag2, System.SR.CollectionTypeCannotHaveDataContract, null, ref dataContract);
		}
		if (Globals.TypeOfIXmlSerializable.IsAssignableFrom(type) || IsArraySegment(type))
		{
			return false;
		}
		if (!Globals.TypeOfIEnumerable.IsAssignableFrom(type))
		{
			return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, flag2, System.SR.CollectionTypeIsNotIEnumerable, null, ref dataContract);
		}
		MethodInfo method;
		MethodInfo addMethod;
		if (type.IsInterface)
		{
			Type type2 = (type.IsGenericType ? type.GetGenericTypeDefinition() : type);
			Type[] knownInterfaces = KnownInterfaces;
			for (int i = 0; i < knownInterfaces.Length; i++)
			{
				if (!(knownInterfaces[i] == type2))
				{
					continue;
				}
				addMethod = null;
				if (type.IsGenericType)
				{
					Type[] genericArguments = type.GetGenericArguments();
					if (type2 == Globals.TypeOfIDictionaryGeneric)
					{
						itemType = Globals.TypeOfKeyValue.MakeGenericType(genericArguments);
						addMethod = type.GetMethod("Add");
						method = Globals.TypeOfIEnumerableGeneric.MakeGenericType(Globals.TypeOfKeyValuePair.MakeGenericType(genericArguments)).GetMethod("GetEnumerator");
					}
					else
					{
						itemType = genericArguments[0];
						if (type2 == Globals.TypeOfICollectionGeneric || type2 == Globals.TypeOfIListGeneric)
						{
							addMethod = Globals.TypeOfICollectionGeneric.MakeGenericType(itemType).GetMethod("Add");
						}
						method = Globals.TypeOfIEnumerableGeneric.MakeGenericType(itemType).GetMethod("GetEnumerator");
					}
				}
				else
				{
					if (type2 == Globals.TypeOfIDictionary)
					{
						itemType = typeof(KeyValue<object, object>);
						addMethod = type.GetMethod("Add");
					}
					else
					{
						itemType = Globals.TypeOfObject;
						if (type2 == Globals.TypeOfIList)
						{
							addMethod = type.GetMethod("Add");
						}
					}
					method = typeof(IEnumerable).GetMethod("GetEnumerator");
				}
				if (tryCreate)
				{
					dataContract = new CollectionDataContract(type, (CollectionKind)(i + 1), itemType, method, addMethod, null);
				}
				return true;
			}
		}
		ConstructorInfo constructorInfo = null;
		if (!type.IsValueType)
		{
			constructorInfo = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			if (constructorInfo == null && constructorRequired)
			{
				if (type.IsSerializable)
				{
					return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, flag2, System.SR.CollectionTypeDoesNotHaveDefaultCtor, null, ref dataContract);
				}
				flag = true;
				GetReadOnlyCollectionExceptionMessages(type, hasCollectionDataContract, System.SR.CollectionTypeDoesNotHaveDefaultCtor, null, out serializationExceptionMessage, out deserializationExceptionMessage);
			}
		}
		Type type3 = null;
		CollectionKind collectionKind = CollectionKind.None;
		bool flag3 = false;
		Type[] interfaces = type.GetInterfaces();
		Type[] array = interfaces;
		foreach (Type type4 in array)
		{
			Type type5 = (type4.IsGenericType ? type4.GetGenericTypeDefinition() : type4);
			Type[] knownInterfaces2 = KnownInterfaces;
			for (int k = 0; k < knownInterfaces2.Length; k++)
			{
				if (knownInterfaces2[k] == type5)
				{
					CollectionKind collectionKind2 = (CollectionKind)(k + 1);
					if (collectionKind == CollectionKind.None || (int)collectionKind2 < (int)collectionKind)
					{
						collectionKind = collectionKind2;
						type3 = type4;
						flag3 = false;
					}
					else if ((collectionKind & collectionKind2) == collectionKind2)
					{
						flag3 = true;
					}
					break;
				}
			}
		}
		switch (collectionKind)
		{
		case CollectionKind.None:
			return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, flag2, System.SR.CollectionTypeIsNotIEnumerable, null, ref dataContract);
		case CollectionKind.GenericEnumerable:
		case CollectionKind.Collection:
		case CollectionKind.Enumerable:
			if (flag3)
			{
				type3 = Globals.TypeOfIEnumerable;
			}
			itemType = (type3.IsGenericType ? type3.GetGenericArguments()[0] : Globals.TypeOfObject);
			GetCollectionMethods(type, type3, new Type[1] { itemType }, addMethodOnInterface: false, out method, out addMethod);
			if (addMethod == null)
			{
				if (type.IsSerializable || skipIfReadOnlyContract)
				{
					return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, flag2 && !skipIfReadOnlyContract, System.SR.CollectionTypeDoesNotHaveAddMethod, DataContract.GetClrTypeFullName(itemType), ref dataContract);
				}
				flag = true;
				GetReadOnlyCollectionExceptionMessages(type, hasCollectionDataContract, System.SR.CollectionTypeDoesNotHaveAddMethod, DataContract.GetClrTypeFullName(itemType), out serializationExceptionMessage, out deserializationExceptionMessage);
			}
			if (tryCreate)
			{
				dataContract = (flag ? new CollectionDataContract(type, collectionKind, itemType, method, serializationExceptionMessage, deserializationExceptionMessage) : new CollectionDataContract(type, collectionKind, itemType, method, addMethod, constructorInfo, !constructorRequired));
			}
			break;
		default:
		{
			if (flag3)
			{
				return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, flag2, System.SR.CollectionTypeHasMultipleDefinitionsOfInterface, KnownInterfaces[(uint)(collectionKind - 1)].Name, ref dataContract);
			}
			Type[] array2 = null;
			switch (collectionKind)
			{
			case CollectionKind.GenericDictionary:
			{
				array2 = type3.GetGenericArguments();
				bool flag4 = type3.IsGenericTypeDefinition || (array2[0].IsGenericParameter && array2[1].IsGenericParameter);
				itemType = (flag4 ? Globals.TypeOfKeyValue : Globals.TypeOfKeyValue.MakeGenericType(array2));
				break;
			}
			case CollectionKind.Dictionary:
				array2 = new Type[2]
				{
					Globals.TypeOfObject,
					Globals.TypeOfObject
				};
				itemType = Globals.TypeOfKeyValue.MakeGenericType(array2);
				break;
			case CollectionKind.GenericList:
			case CollectionKind.GenericCollection:
				array2 = type3.GetGenericArguments();
				itemType = array2[0];
				break;
			case CollectionKind.List:
				itemType = Globals.TypeOfObject;
				array2 = new Type[1] { itemType };
				break;
			}
			if (tryCreate)
			{
				GetCollectionMethods(type, type3, array2, addMethodOnInterface: true, out method, out addMethod);
				dataContract = (flag ? new CollectionDataContract(type, collectionKind, itemType, method, serializationExceptionMessage, deserializationExceptionMessage) : new CollectionDataContract(type, collectionKind, itemType, method, addMethod, constructorInfo, !constructorRequired));
			}
			break;
		}
		}
		return !(flag && skipIfReadOnlyContract);
	}

	internal static bool IsCollectionDataContract(Type type)
	{
		return type.IsDefined(Globals.TypeOfCollectionDataContractAttribute, inherit: false);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static bool HandleIfInvalidCollection(Type type, bool tryCreate, bool hasCollectionDataContract, bool createContractWithException, string message, string param, ref DataContract dataContract)
	{
		if (hasCollectionDataContract)
		{
			if (tryCreate)
			{
				throw new InvalidDataContractException(GetInvalidCollectionMessage(message, System.SR.Format(System.SR.InvalidCollectionDataContract, DataContract.GetClrTypeFullName(type)), param));
			}
			return true;
		}
		if (createContractWithException)
		{
			if (tryCreate)
			{
				dataContract = new CollectionDataContract(type, GetInvalidCollectionMessage(message, System.SR.Format(System.SR.InvalidCollectionType, DataContract.GetClrTypeFullName(type)), param));
			}
			return true;
		}
		return false;
	}

	private static void GetReadOnlyCollectionExceptionMessages(Type type, bool hasCollectionDataContract, string message, string param, out string serializationExceptionMessage, out string deserializationExceptionMessage)
	{
		serializationExceptionMessage = GetInvalidCollectionMessage(message, System.SR.Format(hasCollectionDataContract ? System.SR.InvalidCollectionDataContract : System.SR.InvalidCollectionType, DataContract.GetClrTypeFullName(type)), param);
		deserializationExceptionMessage = GetInvalidCollectionMessage(message, System.SR.Format(System.SR.ReadOnlyCollectionDeserialization, DataContract.GetClrTypeFullName(type)), param);
	}

	private static string GetInvalidCollectionMessage(string message, string nestedMessage, string param)
	{
		if (param != null)
		{
			return System.SR.Format(message, nestedMessage, param);
		}
		return System.SR.Format(message, nestedMessage);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:GetMethod", Justification = "The DynamicallyAccessedMembers declarations will ensure the interface methods will be preserved.")]
	private static void FindCollectionMethodsOnInterface([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type interfaceType, ref MethodInfo addMethod, ref MethodInfo getEnumeratorMethod)
	{
		Type type2 = (from it in type.GetInterfaces()
			where it.Equals(interfaceType)
			select it).FirstOrDefault();
		if (type2 != null)
		{
			addMethod = type2.GetMethod("Add") ?? addMethod;
			getEnumeratorMethod = type2.GetMethod("GetEnumerator") ?? getEnumeratorMethod;
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static void GetCollectionMethods([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type interfaceType, Type[] addMethodTypeArray, bool addMethodOnInterface, out MethodInfo getEnumeratorMethod, out MethodInfo addMethod)
	{
		addMethod = (getEnumeratorMethod = null);
		if (addMethodOnInterface)
		{
			addMethod = type.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, addMethodTypeArray);
			if (addMethod == null || addMethod.GetParameters()[0].ParameterType != addMethodTypeArray[0])
			{
				FindCollectionMethodsOnInterface(type, interfaceType, ref addMethod, ref getEnumeratorMethod);
				if (addMethod == null)
				{
					Type[] interfaces = interfaceType.GetInterfaces();
					Array.Sort(interfaces, (Type x, Type y) => string.Compare(x.FullName, y.FullName));
					Type[] array = interfaces;
					foreach (Type type2 in array)
					{
						if (IsKnownInterface(type2))
						{
							FindCollectionMethodsOnInterface(type, type2, ref addMethod, ref getEnumeratorMethod);
							if (addMethod == null)
							{
								break;
							}
						}
					}
				}
			}
		}
		else
		{
			addMethod = type.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, addMethodTypeArray);
		}
		if (!(getEnumeratorMethod == null))
		{
			return;
		}
		getEnumeratorMethod = type.GetMethod("GetEnumerator", BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes);
		if (getEnumeratorMethod == null || !Globals.TypeOfIEnumerator.IsAssignableFrom(getEnumeratorMethod.ReturnType))
		{
			Type ienumerableInterface = (from t in interfaceType.GetInterfaces()
				where t.FullName.StartsWith("System.Collections.Generic.IEnumerable")
				select t).FirstOrDefault() ?? Globals.TypeOfIEnumerable;
			getEnumeratorMethod = GetIEnumerableGetEnumeratorMethod(type, ienumerableInterface);
		}
	}

	private static MethodInfo GetIEnumerableGetEnumeratorMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type ienumerableInterface)
	{
		return GetTargetMethodWithName("GetEnumerator", type, ienumerableInterface);
	}

	private static bool IsKnownInterface(Type type)
	{
		Type type2 = (type.IsGenericType ? type.GetGenericTypeDefinition() : type);
		Type[] knownInterfaces = KnownInterfaces;
		foreach (Type type3 in knownInterfaces)
		{
			if (type2 == type3)
			{
				return true;
			}
		}
		return false;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override DataContract BindGenericParameters(DataContract[] paramContracts, Dictionary<DataContract, DataContract> boundContracts = null)
	{
		if (boundContracts != null && boundContracts.TryGetValue(this, out var value))
		{
			return value;
		}
		Type underlyingType = UnderlyingType;
		Type[] genericArguments = underlyingType.GetGenericArguments();
		for (int i = 0; i < genericArguments.Length; i++)
		{
			if (genericArguments[i].IsGenericParameter)
			{
				genericArguments[i] = paramContracts[genericArguments[i].GenericParameterPosition].UnderlyingType;
			}
		}
		Type type = underlyingType.MakeGenericType(genericArguments);
		CollectionDataContract collectionDataContract = new CollectionDataContract(type);
		if (boundContracts == null)
		{
			boundContracts = new Dictionary<DataContract, DataContract>();
		}
		boundContracts.Add(this, collectionDataContract);
		collectionDataContract.ItemContract = ItemContract.BindGenericParameters(paramContracts, boundContracts);
		collectionDataContract.IsItemTypeNullable = !collectionDataContract.ItemContract.IsValueType;
		collectionDataContract.ItemName = (ItemNameSetExplicit ? ItemName : collectionDataContract.ItemContract.XmlName.Name);
		collectionDataContract.KeyName = KeyName;
		collectionDataContract.ValueName = ValueName;
		collectionDataContract.XmlName = DataContract.CreateQualifiedName(DataContract.ExpandGenericParameters(XmlConvert.DecodeName(XmlName.Name), new GenericNameProvider(DataContract.GetClrTypeFullName(UnderlyingType), paramContracts)), IsCollectionDataContract(UnderlyingType) ? XmlName.Namespace : DataContract.GetCollectionNamespace(collectionDataContract.ItemContract.XmlName.Namespace));
		return collectionDataContract;
	}

	internal override DataContract GetValidContract(bool verifyConstructor = false)
	{
		if (verifyConstructor && IsConstructorCheckRequired)
		{
			CheckConstructor();
			return this;
		}
		if (InvalidCollectionInSharedContractMessage != null)
		{
			throw new InvalidDataContractException(InvalidCollectionInSharedContractMessage);
		}
		return this;
	}

	private void CheckConstructor()
	{
		if (Constructor == null)
		{
			throw new InvalidDataContractException(System.SR.Format(System.SR.CollectionTypeDoesNotHaveDefaultCtor, DataContract.GetClrTypeFullName(UnderlyingType)));
		}
		IsConstructorCheckRequired = false;
	}

	internal override bool IsValidContract()
	{
		return InvalidCollectionInSharedContractMessage == null;
	}

	internal bool RequiresMemberAccessForRead(SecurityException securityException)
	{
		if (!DataContract.IsTypeVisible(UnderlyingType))
		{
			if (securityException != null)
			{
				throw new SecurityException(System.SR.Format(System.SR.PartialTrustCollectionContractTypeNotPublic, DataContract.GetClrTypeFullName(UnderlyingType)), securityException);
			}
			return true;
		}
		if (ItemType != null && !DataContract.IsTypeVisible(ItemType))
		{
			if (securityException != null)
			{
				throw new SecurityException(System.SR.Format(System.SR.PartialTrustCollectionContractTypeNotPublic, DataContract.GetClrTypeFullName(ItemType)), securityException);
			}
			return true;
		}
		if (DataContract.ConstructorRequiresMemberAccess(Constructor))
		{
			if (securityException != null)
			{
				throw new SecurityException(System.SR.Format(System.SR.PartialTrustCollectionContractNoPublicConstructor, DataContract.GetClrTypeFullName(UnderlyingType)), securityException);
			}
			return true;
		}
		if (DataContract.MethodRequiresMemberAccess(AddMethod))
		{
			if (securityException != null)
			{
				throw new SecurityException(System.SR.Format(System.SR.PartialTrustCollectionContractAddMethodNotPublic, DataContract.GetClrTypeFullName(UnderlyingType), AddMethod.Name), securityException);
			}
			return true;
		}
		return false;
	}

	internal bool RequiresMemberAccessForWrite(SecurityException securityException)
	{
		if (!DataContract.IsTypeVisible(UnderlyingType))
		{
			if (securityException != null)
			{
				throw new SecurityException(System.SR.Format(System.SR.PartialTrustCollectionContractTypeNotPublic, DataContract.GetClrTypeFullName(UnderlyingType)), securityException);
			}
			return true;
		}
		if (ItemType != null && !DataContract.IsTypeVisible(ItemType))
		{
			if (securityException != null)
			{
				throw new SecurityException(System.SR.Format(System.SR.PartialTrustCollectionContractTypeNotPublic, DataContract.GetClrTypeFullName(ItemType)), securityException);
			}
			return true;
		}
		return false;
	}

	[UnconditionalSuppressMessage("AOT Analysis", "IL3050:RequiresDynamicCode", Justification = "All ctor's required to create an instance of this type are marked with RequiresDynamicCode.")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "All ctor's required to create an instance of this type are marked with RequiresUnreferencedCode.")]
	internal override bool Equals(object other, HashSet<DataContractPairKey> checkedContracts)
	{
		if (IsEqualOrChecked(other, checkedContracts))
		{
			return true;
		}
		if (base.Equals(other, checkedContracts) && other is CollectionDataContract collectionDataContract)
		{
			bool flag = ItemContract != null && !ItemContract.IsValueType;
			bool flag2 = collectionDataContract.ItemContract != null && !collectionDataContract.ItemContract.IsValueType;
			if (ItemName == collectionDataContract.ItemName && (IsItemTypeNullable || flag) == (collectionDataContract.IsItemTypeNullable || flag2) && ItemContract != null)
			{
				return ItemContract.Equals(collectionDataContract.ItemContract, checkedContracts);
			}
			return false;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
	{
		context.IsGetOnlyCollection = false;
		XmlFormatWriterDelegate(xmlWriter, obj, context, this);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override object ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context)
	{
		xmlReader.Read();
		object result = null;
		if (context.IsGetOnlyCollection)
		{
			context.IsGetOnlyCollection = false;
			XmlFormatGetOnlyCollectionReaderDelegate(xmlReader, context, CollectionItemName, Namespace, this);
		}
		else
		{
			result = XmlFormatReaderDelegate(xmlReader, context, CollectionItemName, Namespace, this);
		}
		xmlReader.ReadEndElement();
		return result;
	}
}
