using System.Buffers.Binary;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.DataContracts;

public abstract class DataContract
{
	internal class DataContractCriticalHelper
	{
		private static readonly ConcurrentDictionary<nint, int> s_typeToIDCache = new ConcurrentDictionary<nint, int>();

		private static readonly ContextAwareDataContractIndex s_dataContractCache = new ContextAwareDataContractIndex(32);

		private static int s_dataContractID;

		private static readonly ContextAwareDictionary<Type, DataContract> s_typeToBuiltInContract = new ContextAwareDictionary<Type, DataContract>();

		private static Dictionary<XmlQualifiedName, DataContract> s_nameToBuiltInContract;

		private static Dictionary<string, DataContract> s_typeNameToBuiltInContract;

		private static readonly Hashtable s_namespaces = new Hashtable();

		private static Dictionary<string, XmlDictionaryString> s_clrTypeStrings;

		private static XmlDictionary s_clrTypeStringsDictionary;

		private static readonly object s_cacheLock = new object();

		private static readonly object s_createDataContractLock = new object();

		private static readonly object s_initBuiltInContractsLock = new object();

		private static readonly object s_namespacesLock = new object();

		private static readonly object s_clrTypeStringsLock = new object();

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
		private readonly Type _underlyingType;

		private Type _originalUnderlyingType;

		private bool _isValueType;

		private GenericInfo _genericInfo;

		private XmlQualifiedName _xmlName;

		private XmlDictionaryString _name;

		private XmlDictionaryString _ns;

		private MethodInfo _parseMethod;

		private bool _parseMethodSet;

		private Type _typeForInitialization;

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
		internal Type UnderlyingType => _underlyingType;

		internal Type OriginalUnderlyingType => _originalUnderlyingType ?? (_originalUnderlyingType = GetDataContractOriginalType(_underlyingType));

		internal virtual bool IsBuiltInDataContract => false;

		internal Type TypeForInitialization => _typeForInitialization;

		internal bool IsReference { get; set; }

		internal bool IsValueType
		{
			get
			{
				return _isValueType;
			}
			set
			{
				_isValueType = value;
			}
		}

		internal XmlQualifiedName XmlName
		{
			get
			{
				return _xmlName;
			}
			set
			{
				_xmlName = value;
			}
		}

		internal GenericInfo GenericInfo
		{
			get
			{
				return _genericInfo;
			}
			set
			{
				_genericInfo = value;
			}
		}

		internal virtual Dictionary<XmlQualifiedName, DataContract> KnownDataContracts
		{
			[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
			[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
			get
			{
				return null;
			}
			set
			{
			}
		}

		internal virtual bool IsISerializable
		{
			get
			{
				return false;
			}
			set
			{
				ThrowInvalidDataContractException(System.SR.RequiresClassDataContractToSetIsISerializable);
			}
		}

		internal XmlDictionaryString Name
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
			}
		}

		internal XmlDictionaryString Namespace
		{
			get
			{
				return _ns;
			}
			set
			{
				_ns = value;
			}
		}

		internal virtual bool HasRoot { get; set; } = true;


		internal virtual XmlDictionaryString TopLevelElementName
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
			}
		}

		internal virtual XmlDictionaryString TopLevelElementNamespace
		{
			get
			{
				return _ns;
			}
			set
			{
				_ns = value;
			}
		}

		internal virtual bool CanContainReferences => true;

		internal virtual bool IsPrimitive => false;

		internal MethodInfo ParseMethod
		{
			get
			{
				if (!_parseMethodSet)
				{
					MethodInfo method = UnderlyingType.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, new Type[1] { typeof(string) });
					if (method != null && method.ReturnType == UnderlyingType)
					{
						_parseMethod = method;
					}
					_parseMethodSet = true;
				}
				return _parseMethod;
			}
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal static DataContract GetDataContractSkipValidation(int id, RuntimeTypeHandle typeHandle, Type type)
		{
			DataContract item = s_dataContractCache.GetItem(id);
			if (item == null)
			{
				return CreateDataContract(id, typeHandle, type);
			}
			return item.GetValidContract(verifyConstructor: true);
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal static DataContract GetGetOnlyCollectionDataContractSkipValidation(int id, RuntimeTypeHandle typeHandle, Type type)
		{
			return s_dataContractCache.GetItem(id) ?? CreateGetOnlyCollectionDataContract(id, typeHandle, type);
		}

		internal static DataContract GetDataContractForInitialization(int id)
		{
			DataContract item = s_dataContractCache.GetItem(id);
			if (item == null)
			{
				throw new SerializationException(System.SR.DataContractCacheOverflow);
			}
			return item;
		}

		internal static int GetIdForInitialization(ClassDataContract classContract)
		{
			int id = DataContract.GetId(classContract.TypeForInitialization.TypeHandle);
			if (id < s_dataContractCache.Length && ContractMatches(classContract, s_dataContractCache.GetItem(id)))
			{
				return id;
			}
			int num = s_dataContractID;
			for (int i = 0; i < num; i++)
			{
				if (ContractMatches(classContract, s_dataContractCache.GetItem(id)))
				{
					return i;
				}
			}
			throw new SerializationException(System.SR.DataContractCacheOverflow);
		}

		private static bool ContractMatches(DataContract contract, DataContract cachedContract)
		{
			if (cachedContract != null)
			{
				return cachedContract.UnderlyingType == contract.UnderlyingType;
			}
			return false;
		}

		internal static int GetId(RuntimeTypeHandle typeHandle)
		{
			typeHandle = GetDataContractAdapterTypeHandle(typeHandle);
			if (s_typeToIDCache.TryGetValue(typeHandle.Value, out var value))
			{
				return value;
			}
			try
			{
				lock (s_cacheLock)
				{
					return s_typeToIDCache.GetOrAdd(typeHandle.Value, delegate
					{
						int num = s_dataContractID++;
						if (num >= s_dataContractCache.Length)
						{
							int num2 = ((num < 1073741823) ? (num * 2) : int.MaxValue);
							if (num2 <= num)
							{
								throw new SerializationException(System.SR.DataContractCacheOverflow);
							}
							s_dataContractCache.Resize(num2);
						}
						return num;
					});
				}
			}
			catch (Exception ex) when (!ExceptionUtility.IsFatal(ex))
			{
				throw new Exception(ex.Message, ex);
			}
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private static DataContract CreateDataContract(int id, RuntimeTypeHandle typeHandle, Type type)
		{
			DataContract dataContract = s_dataContractCache.GetItem(id);
			if (dataContract == null)
			{
				lock (s_createDataContractLock)
				{
					dataContract = s_dataContractCache.GetItem(id);
					if (dataContract == null)
					{
						if ((object)type == null)
						{
							type = Type.GetTypeFromHandle(typeHandle);
						}
						dataContract = CreateDataContract(type);
						AssignDataContractToId(dataContract, id);
					}
				}
			}
			return dataContract;
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private static DataContract CreateDataContract(Type type)
		{
			type = UnwrapNullableType(type);
			Type type2 = type;
			type = GetDataContractAdapterType(type);
			DataContract dataContract = GetBuiltInDataContract(type);
			if (dataContract == null)
			{
				if (type.IsArray)
				{
					dataContract = new CollectionDataContract(type);
				}
				else if (type.IsEnum)
				{
					dataContract = new EnumDataContract(type);
				}
				else if (type.IsGenericParameter)
				{
					dataContract = new GenericParameterDataContract(type);
				}
				else if (Globals.TypeOfIXmlSerializable.IsAssignableFrom(type))
				{
					dataContract = new XmlDataContract(type);
				}
				else
				{
					if (type.IsPointer)
					{
						type = Globals.TypeOfReflectionPointer;
					}
					if (!CollectionDataContract.TryCreate(type, out dataContract))
					{
						if (!type.IsSerializable && !type.IsDefined(Globals.TypeOfDataContractAttribute, inherit: false) && !ClassDataContract.IsNonAttributedTypeValidForSerialization(type))
						{
							ThrowInvalidDataContractException(System.SR.Format(System.SR.TypeNotSerializable, type), type);
						}
						dataContract = new ClassDataContract(type);
						if (type != type2)
						{
							ClassDataContract classDataContract = new ClassDataContract(type2);
							if (dataContract.XmlName != classDataContract.XmlName)
							{
								dataContract.XmlName = classDataContract.XmlName;
							}
						}
					}
				}
			}
			return dataContract;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void AssignDataContractToId(DataContract dataContract, int id)
		{
			lock (s_cacheLock)
			{
				s_dataContractCache.SetItem(id, dataContract);
			}
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private static DataContract CreateGetOnlyCollectionDataContract(int id, RuntimeTypeHandle typeHandle, Type type)
		{
			DataContract dataContract = null;
			lock (s_createDataContractLock)
			{
				dataContract = s_dataContractCache.GetItem(id);
				if (dataContract == null)
				{
					if ((object)type == null)
					{
						type = Type.GetTypeFromHandle(typeHandle);
					}
					type = UnwrapNullableType(type);
					type = GetDataContractAdapterType(type);
					if (!CollectionDataContract.TryCreateGetOnlyCollectionDataContract(type, out dataContract))
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.TypeNotSerializable, type), type);
					}
					AssignDataContractToId(dataContract, id);
				}
			}
			return dataContract;
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal static Type GetDataContractAdapterType(Type type)
		{
			if (type == Globals.TypeOfDateTimeOffset)
			{
				return Globals.TypeOfDateTimeOffsetAdapter;
			}
			if (type == Globals.TypeOfMemoryStream)
			{
				return Globals.TypeOfMemoryStreamAdapter;
			}
			return type;
		}

		internal static Type GetDataContractOriginalType(Type type)
		{
			if (type == Globals.TypeOfDateTimeOffsetAdapter)
			{
				return Globals.TypeOfDateTimeOffset;
			}
			if (type == Globals.TypeOfMemoryStreamAdapter)
			{
				return Globals.TypeOfMemoryStream;
			}
			return type;
		}

		private static RuntimeTypeHandle GetDataContractAdapterTypeHandle(RuntimeTypeHandle typeHandle)
		{
			if (Globals.TypeOfDateTimeOffset.TypeHandle.Equals(typeHandle))
			{
				return Globals.TypeOfDateTimeOffsetAdapter.TypeHandle;
			}
			if (Globals.TypeOfMemoryStream.TypeHandle.Equals(typeHandle))
			{
				return Globals.TypeOfMemoryStreamAdapter.TypeHandle;
			}
			return typeHandle;
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal static DataContract GetBuiltInDataContract(Type type)
		{
			if (type.IsInterface && !CollectionDataContract.IsCollectionInterface(type))
			{
				type = Globals.TypeOfObject;
			}
			return s_typeToBuiltInContract.GetOrAdd(type, delegate(Type key)
			{
				TryCreateBuiltInDataContract(key, out var dataContract);
				return dataContract;
			});
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal static DataContract GetBuiltInDataContract(string name, string ns)
		{
			lock (s_initBuiltInContractsLock)
			{
				if (s_nameToBuiltInContract == null)
				{
					s_nameToBuiltInContract = new Dictionary<XmlQualifiedName, DataContract>();
				}
				XmlQualifiedName key = new XmlQualifiedName(name, ns);
				if (!s_nameToBuiltInContract.TryGetValue(key, out var value) && TryCreateBuiltInDataContract(name, ns, out value))
				{
					s_nameToBuiltInContract.Add(key, value);
				}
				return value;
			}
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal static DataContract GetBuiltInDataContract(string typeName)
		{
			if (!typeName.StartsWith("System.", StringComparison.Ordinal))
			{
				return null;
			}
			lock (s_initBuiltInContractsLock)
			{
				if (s_typeNameToBuiltInContract == null)
				{
					s_typeNameToBuiltInContract = new Dictionary<string, DataContract>();
				}
				if (!s_typeNameToBuiltInContract.TryGetValue(typeName, out var value))
				{
					Type type = typeName.AsSpan(7) switch
					{
						"Char" => typeof(char), 
						"Boolean" => typeof(bool), 
						"SByte" => typeof(sbyte), 
						"Byte" => typeof(byte), 
						"Int16" => typeof(short), 
						"UInt16" => typeof(ushort), 
						"Int32" => typeof(int), 
						"UInt32" => typeof(uint), 
						"Int64" => typeof(long), 
						"UInt64" => typeof(ulong), 
						"Single" => typeof(float), 
						"Double" => typeof(double), 
						"Decimal" => typeof(decimal), 
						"DateTime" => typeof(DateTime), 
						"String" => typeof(string), 
						"Byte[]" => typeof(byte[]), 
						"Object" => typeof(object), 
						"TimeSpan" => typeof(TimeSpan), 
						"Guid" => typeof(Guid), 
						"Uri" => typeof(Uri), 
						"Xml.XmlQualifiedName" => typeof(XmlQualifiedName), 
						"Enum" => typeof(Enum), 
						"ValueType" => typeof(ValueType), 
						"Array" => typeof(Array), 
						"Xml.XmlElement" => typeof(XmlElement), 
						"Xml.XmlNode[]" => typeof(XmlNode[]), 
						_ => null, 
					};
					if (type != null)
					{
						TryCreateBuiltInDataContract(type, out value);
					}
					s_typeNameToBuiltInContract.Add(typeName, value);
				}
				return value;
			}
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal static bool TryCreateBuiltInDataContract(Type type, [NotNullWhen(true)] out DataContract dataContract)
		{
			if (type.IsEnum)
			{
				dataContract = null;
				return false;
			}
			dataContract = null;
			switch (Type.GetTypeCode(type))
			{
			case TypeCode.Boolean:
				dataContract = new BooleanDataContract();
				break;
			case TypeCode.Byte:
				dataContract = new UnsignedByteDataContract();
				break;
			case TypeCode.Char:
				dataContract = new CharDataContract();
				break;
			case TypeCode.DateTime:
				dataContract = new DateTimeDataContract();
				break;
			case TypeCode.Decimal:
				dataContract = new DecimalDataContract();
				break;
			case TypeCode.Double:
				dataContract = new DoubleDataContract();
				break;
			case TypeCode.Int16:
				dataContract = new ShortDataContract();
				break;
			case TypeCode.Int32:
				dataContract = new IntDataContract();
				break;
			case TypeCode.Int64:
				dataContract = new LongDataContract();
				break;
			case TypeCode.SByte:
				dataContract = new SignedByteDataContract();
				break;
			case TypeCode.Single:
				dataContract = new FloatDataContract();
				break;
			case TypeCode.String:
				dataContract = new StringDataContract();
				break;
			case TypeCode.UInt16:
				dataContract = new UnsignedShortDataContract();
				break;
			case TypeCode.UInt32:
				dataContract = new UnsignedIntDataContract();
				break;
			case TypeCode.UInt64:
				dataContract = new UnsignedLongDataContract();
				break;
			default:
				if (type == typeof(byte[]))
				{
					dataContract = new ByteArrayDataContract();
				}
				else if (type == typeof(object))
				{
					dataContract = new ObjectDataContract();
				}
				else if (type == typeof(Uri))
				{
					dataContract = new UriDataContract();
				}
				else if (type == typeof(XmlQualifiedName))
				{
					dataContract = new QNameDataContract();
				}
				else if (type == typeof(TimeSpan))
				{
					dataContract = new TimeSpanDataContract();
				}
				else if (type == typeof(Guid))
				{
					dataContract = new GuidDataContract();
				}
				else if (type == typeof(Enum) || type == typeof(ValueType))
				{
					dataContract = new SpecialTypeDataContract(type, DictionaryGlobals.ObjectLocalName, DictionaryGlobals.SchemaNamespace);
				}
				else if (type == typeof(Array))
				{
					dataContract = new CollectionDataContract(type);
				}
				else if (type == typeof(XmlElement) || type == typeof(XmlNode[]))
				{
					dataContract = new XmlDataContract(type);
				}
				break;
			}
			return dataContract != null;
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal static bool TryCreateBuiltInDataContract(string name, string ns, [NotNullWhen(true)] out DataContract dataContract)
		{
			dataContract = null;
			if (ns == DictionaryGlobals.SchemaNamespace.Value)
			{
				if (DictionaryGlobals.BooleanLocalName.Value == name)
				{
					dataContract = new BooleanDataContract();
				}
				else if (DictionaryGlobals.SignedByteLocalName.Value == name)
				{
					dataContract = new SignedByteDataContract();
				}
				else if (DictionaryGlobals.UnsignedByteLocalName.Value == name)
				{
					dataContract = new UnsignedByteDataContract();
				}
				else if (DictionaryGlobals.ShortLocalName.Value == name)
				{
					dataContract = new ShortDataContract();
				}
				else if (DictionaryGlobals.UnsignedShortLocalName.Value == name)
				{
					dataContract = new UnsignedShortDataContract();
				}
				else if (DictionaryGlobals.IntLocalName.Value == name)
				{
					dataContract = new IntDataContract();
				}
				else if (DictionaryGlobals.UnsignedIntLocalName.Value == name)
				{
					dataContract = new UnsignedIntDataContract();
				}
				else if (DictionaryGlobals.LongLocalName.Value == name)
				{
					dataContract = new LongDataContract();
				}
				else if (DictionaryGlobals.integerLocalName.Value == name)
				{
					dataContract = new IntegerDataContract();
				}
				else if (DictionaryGlobals.positiveIntegerLocalName.Value == name)
				{
					dataContract = new PositiveIntegerDataContract();
				}
				else if (DictionaryGlobals.negativeIntegerLocalName.Value == name)
				{
					dataContract = new NegativeIntegerDataContract();
				}
				else if (DictionaryGlobals.nonPositiveIntegerLocalName.Value == name)
				{
					dataContract = new NonPositiveIntegerDataContract();
				}
				else if (DictionaryGlobals.nonNegativeIntegerLocalName.Value == name)
				{
					dataContract = new NonNegativeIntegerDataContract();
				}
				else if (DictionaryGlobals.UnsignedLongLocalName.Value == name)
				{
					dataContract = new UnsignedLongDataContract();
				}
				else if (DictionaryGlobals.FloatLocalName.Value == name)
				{
					dataContract = new FloatDataContract();
				}
				else if (DictionaryGlobals.DoubleLocalName.Value == name)
				{
					dataContract = new DoubleDataContract();
				}
				else if (DictionaryGlobals.DecimalLocalName.Value == name)
				{
					dataContract = new DecimalDataContract();
				}
				else if (DictionaryGlobals.DateTimeLocalName.Value == name)
				{
					dataContract = new DateTimeDataContract();
				}
				else if (DictionaryGlobals.StringLocalName.Value == name)
				{
					dataContract = new StringDataContract();
				}
				else if (DictionaryGlobals.timeLocalName.Value == name)
				{
					dataContract = new TimeDataContract();
				}
				else if (DictionaryGlobals.dateLocalName.Value == name)
				{
					dataContract = new DateDataContract();
				}
				else if (DictionaryGlobals.hexBinaryLocalName.Value == name)
				{
					dataContract = new HexBinaryDataContract();
				}
				else if (DictionaryGlobals.gYearMonthLocalName.Value == name)
				{
					dataContract = new GYearMonthDataContract();
				}
				else if (DictionaryGlobals.gYearLocalName.Value == name)
				{
					dataContract = new GYearDataContract();
				}
				else if (DictionaryGlobals.gMonthDayLocalName.Value == name)
				{
					dataContract = new GMonthDayDataContract();
				}
				else if (DictionaryGlobals.gDayLocalName.Value == name)
				{
					dataContract = new GDayDataContract();
				}
				else if (DictionaryGlobals.gMonthLocalName.Value == name)
				{
					dataContract = new GMonthDataContract();
				}
				else if (DictionaryGlobals.normalizedStringLocalName.Value == name)
				{
					dataContract = new NormalizedStringDataContract();
				}
				else if (DictionaryGlobals.tokenLocalName.Value == name)
				{
					dataContract = new TokenDataContract();
				}
				else if (DictionaryGlobals.languageLocalName.Value == name)
				{
					dataContract = new LanguageDataContract();
				}
				else if (DictionaryGlobals.NameLocalName.Value == name)
				{
					dataContract = new NameDataContract();
				}
				else if (DictionaryGlobals.NCNameLocalName.Value == name)
				{
					dataContract = new NCNameDataContract();
				}
				else if (DictionaryGlobals.XSDIDLocalName.Value == name)
				{
					dataContract = new IDDataContract();
				}
				else if (DictionaryGlobals.IDREFLocalName.Value == name)
				{
					dataContract = new IDREFDataContract();
				}
				else if (DictionaryGlobals.IDREFSLocalName.Value == name)
				{
					dataContract = new IDREFSDataContract();
				}
				else if (DictionaryGlobals.ENTITYLocalName.Value == name)
				{
					dataContract = new ENTITYDataContract();
				}
				else if (DictionaryGlobals.ENTITIESLocalName.Value == name)
				{
					dataContract = new ENTITIESDataContract();
				}
				else if (DictionaryGlobals.NMTOKENLocalName.Value == name)
				{
					dataContract = new NMTOKENDataContract();
				}
				else if (DictionaryGlobals.NMTOKENSLocalName.Value == name)
				{
					dataContract = new NMTOKENDataContract();
				}
				else if (DictionaryGlobals.ByteArrayLocalName.Value == name)
				{
					dataContract = new ByteArrayDataContract();
				}
				else if (DictionaryGlobals.ObjectLocalName.Value == name)
				{
					dataContract = new ObjectDataContract();
				}
				else if (DictionaryGlobals.TimeSpanLocalName.Value == name)
				{
					dataContract = new XsDurationDataContract();
				}
				else if (DictionaryGlobals.UriLocalName.Value == name)
				{
					dataContract = new UriDataContract();
				}
				else if (DictionaryGlobals.QNameLocalName.Value == name)
				{
					dataContract = new QNameDataContract();
				}
			}
			else if (ns == DictionaryGlobals.SerializationNamespace.Value)
			{
				if (DictionaryGlobals.TimeSpanLocalName.Value == name)
				{
					dataContract = new TimeSpanDataContract();
				}
				else if (DictionaryGlobals.GuidLocalName.Value == name)
				{
					dataContract = new GuidDataContract();
				}
				else if (DictionaryGlobals.CharLocalName.Value == name)
				{
					dataContract = new CharDataContract();
				}
				else if ("ArrayOfanyType" == name)
				{
					dataContract = new CollectionDataContract(typeof(Array));
				}
			}
			else if (ns == DictionaryGlobals.AsmxTypesNamespace.Value)
			{
				if (DictionaryGlobals.CharLocalName.Value == name)
				{
					dataContract = new AsmxCharDataContract();
				}
				else if (DictionaryGlobals.GuidLocalName.Value == name)
				{
					dataContract = new AsmxGuidDataContract();
				}
			}
			else if (ns == "http://schemas.datacontract.org/2004/07/System.Xml")
			{
				if (name == "XmlElement")
				{
					dataContract = new XmlDataContract(typeof(XmlElement));
				}
				else if (name == "ArrayOfXmlNode")
				{
					dataContract = new XmlDataContract(typeof(XmlNode[]));
				}
			}
			return dataContract != null;
		}

		internal static string GetNamespace(string key)
		{
			object obj = s_namespaces[key];
			if (obj != null)
			{
				return (string)obj;
			}
			try
			{
				lock (s_namespacesLock)
				{
					obj = s_namespaces[key];
					if (obj != null)
					{
						return (string)obj;
					}
					s_namespaces.Add(key, key);
					return key;
				}
			}
			catch (Exception ex) when (!ExceptionUtility.IsFatal(ex))
			{
				throw new Exception(ex.Message, ex);
			}
		}

		internal static XmlDictionaryString GetClrTypeString(string key)
		{
			lock (s_clrTypeStringsLock)
			{
				if (s_clrTypeStrings == null)
				{
					s_clrTypeStringsDictionary = new XmlDictionary();
					s_clrTypeStrings = new Dictionary<string, XmlDictionaryString>();
					try
					{
						s_clrTypeStrings.Add(Globals.TypeOfInt.Assembly.FullName, s_clrTypeStringsDictionary.Add("0"));
					}
					catch (Exception ex) when (!ExceptionUtility.IsFatal(ex))
					{
						throw new Exception(ex.Message, ex);
					}
				}
				if (s_clrTypeStrings.TryGetValue(key, out var value))
				{
					return value;
				}
				value = s_clrTypeStringsDictionary.Add(key);
				try
				{
					s_clrTypeStrings.Add(key, value);
				}
				catch (Exception ex2) when (!ExceptionUtility.IsFatal(ex2))
				{
					throw new Exception(ex2.Message, ex2);
				}
				return value;
			}
		}

		[DoesNotReturn]
		internal static void ThrowInvalidDataContractException(string message, Type type)
		{
			if (type != null)
			{
				RuntimeTypeHandle dataContractAdapterTypeHandle = GetDataContractAdapterTypeHandle(type.TypeHandle);
				s_typeToIDCache.TryRemove(dataContractAdapterTypeHandle.Value, out var _);
			}
			throw new InvalidDataContractException(message);
		}

		internal DataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
		{
			_underlyingType = type;
			SetTypeForInitialization(type);
			_isValueType = type.IsValueType;
		}

		[MemberNotNull("_typeForInitialization")]
		private void SetTypeForInitialization(Type classType)
		{
			_typeForInitialization = classType;
		}

		internal void SetDataContractName(XmlQualifiedName xmlName)
		{
			XmlDictionary xmlDictionary = new XmlDictionary(2);
			Name = xmlDictionary.Add(xmlName.Name);
			Namespace = xmlDictionary.Add(xmlName.Namespace);
			XmlName = xmlName;
		}

		internal void SetDataContractName(XmlDictionaryString name, XmlDictionaryString ns)
		{
			Name = name;
			Namespace = ns;
			XmlName = CreateQualifiedName(name.Value, ns.Value);
		}

		[DoesNotReturn]
		internal void ThrowInvalidDataContractException(string message)
		{
			ThrowInvalidDataContractException(message, UnderlyingType);
		}
	}

	internal const string SerializerTrimmerWarning = "Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.";

	internal const string SerializerAOTWarning = "Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.";

	internal const DynamicallyAccessedMemberTypes DataContractPreserveMemberTypes = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties;

	private readonly XmlDictionaryString _name;

	private readonly XmlDictionaryString _ns;

	private readonly DataContractCriticalHelper _helper;

	public virtual string? ContractType => null;

	internal MethodInfo? ParseMethod => _helper.ParseMethod;

	internal DataContractCriticalHelper Helper => _helper;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
	public virtual Type UnderlyingType => _helper.UnderlyingType;

	public virtual Type OriginalUnderlyingType => _helper.OriginalUnderlyingType;

	public virtual bool IsBuiltInDataContract => _helper.IsBuiltInDataContract;

	internal Type TypeForInitialization => _helper.TypeForInitialization;

	public virtual bool IsValueType
	{
		get
		{
			return _helper.IsValueType;
		}
		internal set
		{
			_helper.IsValueType = value;
		}
	}

	public virtual bool IsReference
	{
		get
		{
			return _helper.IsReference;
		}
		internal set
		{
			_helper.IsReference = value;
		}
	}

	public virtual XmlQualifiedName XmlName
	{
		get
		{
			return _helper.XmlName;
		}
		internal set
		{
			_helper.XmlName = value;
		}
	}

	public virtual DataContract? BaseContract
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			return null;
		}
	}

	internal GenericInfo? GenericInfo
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			return _helper.GenericInfo;
		}
		set
		{
			_helper.GenericInfo = value;
		}
	}

	public virtual Dictionary<XmlQualifiedName, DataContract>? KnownDataContracts
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

	public virtual bool IsISerializable
	{
		get
		{
			return _helper.IsISerializable;
		}
		internal set
		{
			_helper.IsISerializable = value;
		}
	}

	internal XmlDictionaryString Name => _name;

	internal virtual XmlDictionaryString Namespace => _ns;

	internal virtual bool HasRoot
	{
		get
		{
			return _helper.HasRoot;
		}
		set
		{
			_helper.HasRoot = value;
		}
	}

	public virtual XmlDictionaryString? TopLevelElementName
	{
		get
		{
			return _helper.TopLevelElementName;
		}
		internal set
		{
			_helper.TopLevelElementName = value;
		}
	}

	public virtual XmlDictionaryString? TopLevelElementNamespace
	{
		get
		{
			return _helper.TopLevelElementNamespace;
		}
		internal set
		{
			_helper.TopLevelElementNamespace = value;
		}
	}

	internal virtual bool CanContainReferences => true;

	internal virtual bool IsPrimitive => false;

	public virtual ReadOnlyCollection<DataMember> DataMembers => ReadOnlyCollection<DataMember>.Empty;

	internal DataContract(DataContractCriticalHelper helper)
	{
		_helper = helper;
		_name = helper.Name;
		_ns = helper.Namespace;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static DataContract GetDataContract(Type type)
	{
		return GetDataContract(type.TypeHandle);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static DataContract GetDataContract(RuntimeTypeHandle typeHandle)
	{
		int id = GetId(typeHandle);
		DataContract dataContractSkipValidation = GetDataContractSkipValidation(id, typeHandle, null);
		return dataContractSkipValidation.GetValidContract();
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static DataContract GetDataContract(int id, RuntimeTypeHandle typeHandle)
	{
		DataContract dataContractSkipValidation = GetDataContractSkipValidation(id, typeHandle, null);
		return dataContractSkipValidation.GetValidContract();
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static DataContract GetDataContractSkipValidation(int id, RuntimeTypeHandle typeHandle, Type type)
	{
		return DataContractCriticalHelper.GetDataContractSkipValidation(id, typeHandle, type);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static DataContract GetGetOnlyCollectionDataContract(int id, RuntimeTypeHandle typeHandle, Type type)
	{
		DataContract getOnlyCollectionDataContractSkipValidation = GetGetOnlyCollectionDataContractSkipValidation(id, typeHandle, type);
		getOnlyCollectionDataContractSkipValidation = getOnlyCollectionDataContractSkipValidation.GetValidContract();
		if (getOnlyCollectionDataContractSkipValidation is ClassDataContract)
		{
			throw new SerializationException(System.SR.Format(System.SR.ErrorDeserializing, System.SR.Format(System.SR.ErrorTypeInfo, GetClrTypeFullName(getOnlyCollectionDataContractSkipValidation.UnderlyingType)), System.SR.Format(System.SR.NoSetMethodForProperty, string.Empty, string.Empty)));
		}
		return getOnlyCollectionDataContractSkipValidation;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static DataContract GetGetOnlyCollectionDataContractSkipValidation(int id, RuntimeTypeHandle typeHandle, Type type)
	{
		return DataContractCriticalHelper.GetGetOnlyCollectionDataContractSkipValidation(id, typeHandle, type);
	}

	internal static DataContract GetDataContractForInitialization(int id)
	{
		return DataContractCriticalHelper.GetDataContractForInitialization(id);
	}

	internal static int GetIdForInitialization(ClassDataContract classContract)
	{
		return DataContractCriticalHelper.GetIdForInitialization(classContract);
	}

	internal static int GetId(RuntimeTypeHandle typeHandle)
	{
		return DataContractCriticalHelper.GetId(typeHandle);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static DataContract GetBuiltInDataContract(Type type)
	{
		return DataContractCriticalHelper.GetBuiltInDataContract(type);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public static DataContract? GetBuiltInDataContract(string name, string ns)
	{
		return DataContractCriticalHelper.GetBuiltInDataContract(name, ns);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static DataContract GetBuiltInDataContract(string typeName)
	{
		return DataContractCriticalHelper.GetBuiltInDataContract(typeName);
	}

	internal static string GetNamespace(string key)
	{
		return DataContractCriticalHelper.GetNamespace(key);
	}

	internal static XmlDictionaryString GetClrTypeString(string key)
	{
		return DataContractCriticalHelper.GetClrTypeString(key);
	}

	[DoesNotReturn]
	internal static void ThrowInvalidDataContractException(string message, Type type)
	{
		DataContractCriticalHelper.ThrowInvalidDataContractException(message, type);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
	{
		throw new InvalidDataContractException(System.SR.Format(System.SR.UnexpectedContractType, GetClrTypeFullName(GetType()), GetClrTypeFullName(UnderlyingType)));
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual object ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context)
	{
		throw new InvalidDataContractException(System.SR.Format(System.SR.UnexpectedContractType, GetClrTypeFullName(GetType()), GetClrTypeFullName(UnderlyingType)));
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
	{
		throw new InvalidDataContractException(System.SR.Format(System.SR.UnexpectedContractType, GetClrTypeFullName(GetType()), GetClrTypeFullName(UnderlyingType)));
	}

	internal virtual object ReadXmlElement(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context)
	{
		throw new InvalidDataContractException(System.SR.Format(System.SR.UnexpectedContractType, GetClrTypeFullName(GetType()), GetClrTypeFullName(UnderlyingType)));
	}

	public virtual bool IsDictionaryLike([NotNullWhen(true)] out string? keyName, [NotNullWhen(true)] out string? valueName, [NotNullWhen(true)] out string? itemName)
	{
		keyName = (valueName = (itemName = null));
		return false;
	}

	internal virtual void WriteRootElement(XmlWriterDelegator writer, XmlDictionaryString name, XmlDictionaryString ns)
	{
		if (ns == DictionaryGlobals.SerializationNamespace && !IsPrimitive)
		{
			writer.WriteStartElement("z", name, ns);
		}
		else
		{
			writer.WriteStartElement(name, ns);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual DataContract BindGenericParameters(DataContract[] paramContracts, Dictionary<DataContract, DataContract> boundContracts = null)
	{
		return this;
	}

	internal virtual DataContract GetValidContract(bool verifyConstructor = false)
	{
		return this;
	}

	internal virtual bool IsValidContract()
	{
		return true;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static bool IsTypeSerializable(Type type)
	{
		return IsTypeSerializable(type, null);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static bool IsTypeSerializable(Type type, HashSet<Type> previousCollectionTypes)
	{
		if (type.IsSerializable || type.IsEnum || type.IsDefined(Globals.TypeOfDataContractAttribute, inherit: false) || type.IsInterface || type.IsPointer || type == Globals.TypeOfDBNull || Globals.TypeOfIXmlSerializable.IsAssignableFrom(type))
		{
			return true;
		}
		if (CollectionDataContract.IsCollection(type, out var itemType))
		{
			if (previousCollectionTypes == null)
			{
				previousCollectionTypes = new HashSet<Type>();
			}
			ValidatePreviousCollectionTypes(type, itemType, previousCollectionTypes);
			if (IsTypeSerializable(itemType, previousCollectionTypes))
			{
				return true;
			}
		}
		if (GetBuiltInDataContract(type) == null)
		{
			return ClassDataContract.IsNonAttributedTypeValidForSerialization(type);
		}
		return true;
	}

	private static void ValidatePreviousCollectionTypes(Type collectionType, Type itemType, HashSet<Type> previousCollectionTypes)
	{
		previousCollectionTypes.Add(collectionType);
		while (itemType.IsArray)
		{
			itemType = itemType.GetElementType();
		}
		List<Type> list = new List<Type>();
		Queue<Type> queue = new Queue<Type>();
		queue.Enqueue(itemType);
		list.Add(itemType);
		while (queue.Count > 0)
		{
			itemType = queue.Dequeue();
			if (previousCollectionTypes.Contains(itemType))
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.RecursiveCollectionType, GetClrTypeFullName(itemType)));
			}
			if (!itemType.IsGenericType)
			{
				continue;
			}
			Type[] genericArguments = itemType.GetGenericArguments();
			foreach (Type item in genericArguments)
			{
				if (!list.Contains(item))
				{
					queue.Enqueue(item);
					list.Add(item);
				}
			}
		}
	}

	internal static Type UnwrapRedundantNullableType(Type type)
	{
		Type result = type;
		while (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
		{
			result = type;
			type = type.GetGenericArguments()[0];
		}
		return result;
	}

	internal static Type UnwrapNullableType(Type type)
	{
		while (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
		{
			type = type.GetGenericArguments()[0];
		}
		return type;
	}

	private static bool IsAsciiLocalName(string localName)
	{
		if (localName.Length == 0)
		{
			return false;
		}
		if (!char.IsAsciiLetter(localName[0]))
		{
			return false;
		}
		for (int i = 1; i < localName.Length; i++)
		{
			char c = localName[i];
			if (!char.IsAsciiLetterOrDigit(c))
			{
				return false;
			}
		}
		return true;
	}

	internal static string EncodeLocalName(string localName)
	{
		if (IsAsciiLocalName(localName))
		{
			return localName;
		}
		if (IsValidNCName(localName))
		{
			return localName;
		}
		return XmlConvert.EncodeLocalName(localName);
	}

	internal static bool IsValidNCName(string name)
	{
		try
		{
			XmlConvert.VerifyNCName(name);
			return true;
		}
		catch (XmlException)
		{
			return false;
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public static XmlQualifiedName GetXmlName(Type type)
	{
		bool hasDataContract;
		return GetXmlName(type, out hasDataContract);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static XmlQualifiedName GetXmlName(Type type, out bool hasDataContract)
	{
		return GetXmlName(type, new HashSet<Type>(), out hasDataContract);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static XmlQualifiedName GetXmlName(Type type, HashSet<Type> previousCollectionTypes, out bool hasDataContract)
	{
		type = UnwrapRedundantNullableType(type);
		DataContractAttribute dataContractAttribute;
		if (TryGetBuiltInXmlAndArrayTypeXmlName(type, previousCollectionTypes, out var xmlName))
		{
			hasDataContract = false;
		}
		else if (TryGetDCAttribute(type, out dataContractAttribute))
		{
			xmlName = GetDCTypeXmlName(type, dataContractAttribute);
			hasDataContract = true;
		}
		else
		{
			xmlName = GetNonDCTypeXmlName(type, previousCollectionTypes);
			hasDataContract = false;
		}
		return xmlName;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static XmlQualifiedName GetDCTypeXmlName(Type type, DataContractAttribute dataContractAttribute)
	{
		string text;
		if (dataContractAttribute.IsNameSetExplicitly)
		{
			text = dataContractAttribute.Name;
			if (string.IsNullOrEmpty(text))
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.InvalidDataContractName, GetClrTypeFullName(type)));
			}
			if (type.IsGenericType && !type.IsGenericTypeDefinition)
			{
				text = ExpandGenericParameters(text, type);
			}
			text = EncodeLocalName(text);
		}
		else
		{
			text = GetDefaultXmlLocalName(type);
		}
		string text2;
		if (dataContractAttribute.IsNamespaceSetExplicitly)
		{
			text2 = dataContractAttribute.Namespace;
			if (text2 == null)
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.InvalidDataContractNamespace, GetClrTypeFullName(type)));
			}
			CheckExplicitDataContractNamespaceUri(text2, type);
		}
		else
		{
			text2 = GetDefaultDataContractNamespace(type);
		}
		return CreateQualifiedName(text, text2);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static XmlQualifiedName GetNonDCTypeXmlName(Type type, HashSet<Type> previousCollectionTypes)
	{
		if (CollectionDataContract.IsCollection(type, out var itemType))
		{
			ValidatePreviousCollectionTypes(type, itemType, previousCollectionTypes);
			CollectionDataContractAttribute collectionContractAttribute;
			return GetCollectionXmlName(type, itemType, previousCollectionTypes, out collectionContractAttribute);
		}
		string defaultXmlLocalName = GetDefaultXmlLocalName(type);
		string ns = ((!ClassDataContract.IsNonAttributedTypeValidForSerialization(type)) ? GetDefaultXmlNamespace(type) : GetDefaultDataContractNamespace(type));
		return CreateQualifiedName(defaultXmlLocalName, ns);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static bool TryGetBuiltInXmlAndArrayTypeXmlName(Type type, HashSet<Type> previousCollectionTypes, [NotNullWhen(true)] out XmlQualifiedName xmlName)
	{
		xmlName = null;
		DataContract builtInDataContract = GetBuiltInDataContract(type);
		if (builtInDataContract != null)
		{
			xmlName = builtInDataContract.XmlName;
		}
		else if (Globals.TypeOfIXmlSerializable.IsAssignableFrom(type))
		{
			SchemaExporter.GetXmlTypeInfo(type, out var xmlName2, out var _, out var _);
			xmlName = xmlName2;
		}
		else if (type.IsArray)
		{
			Type elementType = type.GetElementType();
			ValidatePreviousCollectionTypes(type, elementType, previousCollectionTypes);
			xmlName = GetCollectionXmlName(type, elementType, previousCollectionTypes, out var _);
		}
		return xmlName != null;
	}

	internal static bool TryGetDCAttribute(Type type, [NotNullWhen(true)] out DataContractAttribute dataContractAttribute)
	{
		dataContractAttribute = null;
		object[] array = type.GetCustomAttributes(Globals.TypeOfDataContractAttribute, inherit: false).ToArray();
		if (array != null && array.Length != 0)
		{
			dataContractAttribute = (DataContractAttribute)array[0];
		}
		return dataContractAttribute != null;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static XmlQualifiedName GetCollectionXmlName(Type type, Type itemType, out CollectionDataContractAttribute collectionContractAttribute)
	{
		return GetCollectionXmlName(type, itemType, new HashSet<Type>(), out collectionContractAttribute);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static XmlQualifiedName GetCollectionXmlName(Type type, Type itemType, HashSet<Type> previousCollectionTypes, out CollectionDataContractAttribute collectionContractAttribute)
	{
		object[] array = type.GetCustomAttributes(Globals.TypeOfCollectionDataContractAttribute, inherit: false).ToArray();
		string text;
		string text2;
		if (array != null && array.Length != 0)
		{
			collectionContractAttribute = (CollectionDataContractAttribute)array[0];
			if (collectionContractAttribute.IsNameSetExplicitly)
			{
				text = collectionContractAttribute.Name;
				if (string.IsNullOrEmpty(text))
				{
					throw new InvalidDataContractException(System.SR.Format(System.SR.InvalidCollectionContractName, GetClrTypeFullName(type)));
				}
				if (type.IsGenericType && !type.IsGenericTypeDefinition)
				{
					text = ExpandGenericParameters(text, type);
				}
				text = EncodeLocalName(text);
			}
			else
			{
				text = GetDefaultXmlLocalName(type);
			}
			if (collectionContractAttribute.IsNamespaceSetExplicitly)
			{
				text2 = collectionContractAttribute.Namespace;
				if (text2 == null)
				{
					throw new InvalidDataContractException(System.SR.Format(System.SR.InvalidCollectionContractNamespace, GetClrTypeFullName(type)));
				}
				CheckExplicitDataContractNamespaceUri(text2, type);
			}
			else
			{
				text2 = GetDefaultDataContractNamespace(type);
			}
		}
		else
		{
			collectionContractAttribute = null;
			string text3 = "ArrayOf" + GetArrayPrefix(ref itemType);
			bool hasDataContract;
			XmlQualifiedName xmlName = GetXmlName(itemType, previousCollectionTypes, out hasDataContract);
			text = text3 + xmlName.Name;
			text2 = GetCollectionNamespace(xmlName.Namespace);
		}
		return CreateQualifiedName(text, text2);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static string GetArrayPrefix(ref Type itemType)
	{
		string text = string.Empty;
		while (itemType.IsArray && GetBuiltInDataContract(itemType) == null)
		{
			text += "ArrayOf";
			itemType = itemType.GetElementType();
		}
		return text;
	}

	internal static string GetCollectionNamespace(string elementNs)
	{
		if (!IsBuiltInNamespace(elementNs))
		{
			return elementNs;
		}
		return "http://schemas.microsoft.com/2003/10/Serialization/Arrays";
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual XmlQualifiedName GetArrayTypeName(bool isNullable)
	{
		XmlQualifiedName xmlQualifiedName;
		if (IsValueType && isNullable)
		{
			GenericInfo genericInfo = new GenericInfo(GetXmlName(Globals.TypeOfNullable), Globals.TypeOfNullable.FullName);
			genericInfo.Add(new GenericInfo(XmlName, null));
			genericInfo.AddToLevel(0, 1);
			xmlQualifiedName = genericInfo.GetExpandedXmlName();
		}
		else
		{
			xmlQualifiedName = XmlName;
		}
		string collectionNamespace = GetCollectionNamespace(xmlQualifiedName.Namespace);
		string name = "ArrayOf" + xmlQualifiedName.Name;
		return new XmlQualifiedName(name, collectionNamespace);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static XmlQualifiedName GetDefaultXmlName(Type type)
	{
		return CreateQualifiedName(GetDefaultXmlLocalName(type), GetDefaultXmlNamespace(type));
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static string GetDefaultXmlLocalName(Type type)
	{
		if (type.IsGenericParameter)
		{
			return "{" + type.GenericParameterPosition + "}";
		}
		string text = null;
		if (type.IsArray)
		{
			text = GetArrayPrefix(ref type);
		}
		string text2;
		if (type.DeclaringType == null)
		{
			text2 = type.Name;
		}
		else
		{
			int num = ((type.Namespace != null) ? type.Namespace.Length : 0);
			if (num > 0)
			{
				num++;
			}
			text2 = GetClrTypeFullName(type).Substring(num).Replace('+', '.');
		}
		if (text != null)
		{
			text2 = text + text2;
		}
		if (type.IsGenericType)
		{
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			bool flag = true;
			int num2 = text2.IndexOf('[');
			if (num2 >= 0)
			{
				text2 = text2.Substring(0, num2);
			}
			IList<int> dataContractNameForGenericName = GetDataContractNameForGenericName(text2, stringBuilder);
			bool isGenericTypeDefinition = type.IsGenericTypeDefinition;
			Type[] genericArguments = type.GetGenericArguments();
			for (int i = 0; i < genericArguments.Length; i++)
			{
				Type type2 = genericArguments[i];
				if (isGenericTypeDefinition)
				{
					stringBuilder.Append('{').Append(i).Append('}');
					continue;
				}
				XmlQualifiedName xmlName = GetXmlName(type2);
				stringBuilder.Append(xmlName.Name);
				stringBuilder2.Append(' ').Append(xmlName.Namespace);
				if (flag)
				{
					flag = IsBuiltInNamespace(xmlName.Namespace);
				}
			}
			if (isGenericTypeDefinition)
			{
				stringBuilder.Append("{#}");
			}
			else if (dataContractNameForGenericName.Count > 1 || !flag)
			{
				foreach (int item in dataContractNameForGenericName)
				{
					stringBuilder2.Insert(0, item.ToString(CultureInfo.InvariantCulture)).Insert(0, " ");
				}
				stringBuilder.Append(GetNamespacesDigest(stringBuilder2.ToString()));
			}
			text2 = stringBuilder.ToString();
		}
		return EncodeLocalName(text2);
	}

	private static string GetDefaultDataContractNamespace(Type type)
	{
		string clrNs = type.Namespace ?? string.Empty;
		object[] nsAttributes = type.Module.GetCustomAttributes(typeof(ContractNamespaceAttribute)).ToArray();
		string globalDataContractNamespace = GetGlobalDataContractNamespace(clrNs, nsAttributes);
		if (globalDataContractNamespace == null)
		{
			nsAttributes = type.Assembly.GetCustomAttributes(typeof(ContractNamespaceAttribute)).ToArray();
			globalDataContractNamespace = GetGlobalDataContractNamespace(clrNs, nsAttributes);
		}
		string text = globalDataContractNamespace;
		if (text == null)
		{
			text = GetDefaultXmlNamespace(type);
		}
		else
		{
			CheckExplicitDataContractNamespaceUri(text, type);
		}
		return text;
	}

	internal static List<int> GetDataContractNameForGenericName(string typeName, StringBuilder localName)
	{
		List<int> list = new List<int>();
		int num = 0;
		while (true)
		{
			int num2 = typeName.IndexOf('`', num);
			if (num2 < 0)
			{
				localName?.Append(typeName.AsSpan(num));
				list.Add(0);
				break;
			}
			if (localName != null)
			{
				ReadOnlySpan<char> value = typeName.AsSpan(num, num2 - num);
				localName.Append(value);
			}
			while ((num = typeName.IndexOf('.', num + 1, num2 - num - 1)) >= 0)
			{
				list.Add(0);
			}
			num = typeName.IndexOf('.', num2);
			if (num < 0)
			{
				list.Add(int.Parse(typeName.AsSpan(num2 + 1), CultureInfo.InvariantCulture));
				break;
			}
			list.Add(int.Parse(typeName.AsSpan(num2 + 1, num - num2 - 1), CultureInfo.InvariantCulture));
		}
		localName?.Append("Of");
		return list;
	}

	internal static bool IsBuiltInNamespace(string ns)
	{
		if (!(ns == "http://www.w3.org/2001/XMLSchema"))
		{
			return ns == "http://schemas.microsoft.com/2003/10/Serialization/";
		}
		return true;
	}

	internal static string GetDefaultXmlNamespace(Type type)
	{
		if (type.IsGenericParameter)
		{
			return "{ns}";
		}
		return GetDefaultXmlNamespace(type.Namespace);
	}

	internal static XmlQualifiedName CreateQualifiedName(string localName, string ns)
	{
		return new XmlQualifiedName(localName, GetNamespace(ns));
	}

	internal static string GetDefaultXmlNamespace(string clrNs)
	{
		return new Uri(Globals.DataContractXsdBaseNamespaceUri, clrNs ?? string.Empty).AbsoluteUri;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static void GetDefaultXmlName(string fullTypeName, out string localName, out string ns)
	{
		CodeTypeReference typeReference = new CodeTypeReference(fullTypeName);
		GetDefaultName(typeReference, out localName, out ns);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static void GetDefaultName(CodeTypeReference typeReference, out string localName, out string ns)
	{
		string baseType = typeReference.BaseType;
		DataContract builtInDataContract = GetBuiltInDataContract(baseType);
		if (builtInDataContract != null)
		{
			localName = builtInDataContract.XmlName.Name;
			ns = builtInDataContract.XmlName.Namespace;
			return;
		}
		GetClrNameAndNamespace(baseType, out localName, out ns);
		if (typeReference.TypeArguments.Count > 0)
		{
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			bool flag = true;
			List<int> dataContractNameForGenericName = GetDataContractNameForGenericName(localName, stringBuilder);
			foreach (CodeTypeReference typeArgument in typeReference.TypeArguments)
			{
				GetDefaultName(typeArgument, out var localName2, out var ns2);
				stringBuilder.Append(localName2);
				stringBuilder2.Append(' ').Append(ns2);
				if (flag)
				{
					flag = IsBuiltInNamespace(ns2);
				}
			}
			if (dataContractNameForGenericName.Count > 1 || !flag)
			{
				foreach (int item in dataContractNameForGenericName)
				{
					stringBuilder2.Insert(0, item.ToString(CultureInfo.InvariantCulture)).Insert(0, ' ');
				}
				stringBuilder.Append(GetNamespacesDigest(stringBuilder2.ToString()));
			}
			localName = stringBuilder.ToString();
		}
		localName = EncodeLocalName(localName);
		ns = GetDefaultXmlNamespace(ns);
	}

	private static void CheckExplicitDataContractNamespaceUri(string dataContractNs, Type type)
	{
		if (dataContractNs.Length > 0)
		{
			string text = dataContractNs.Trim();
			if (text.Length == 0 || text.Contains("##", StringComparison.Ordinal))
			{
				ThrowInvalidDataContractException(System.SR.Format(System.SR.DataContractNamespaceIsNotValid, dataContractNs), type);
			}
			dataContractNs = text;
		}
		if (Uri.TryCreate(dataContractNs, UriKind.RelativeOrAbsolute, out Uri result))
		{
			Span<char> span = stackalloc char["http://schemas.microsoft.com/2003/10/Serialization/".Length];
			if (result.TryFormat(span, out var charsWritten) && charsWritten == "http://schemas.microsoft.com/2003/10/Serialization/".Length && span.SequenceEqual("http://schemas.microsoft.com/2003/10/Serialization/"))
			{
				ThrowInvalidDataContractException(System.SR.Format(System.SR.DataContractNamespaceReserved, "http://schemas.microsoft.com/2003/10/Serialization/"), type);
			}
		}
		else
		{
			ThrowInvalidDataContractException(System.SR.Format(System.SR.DataContractNamespaceIsNotValid, dataContractNs), type);
		}
	}

	internal static string GetClrTypeFullName(Type type)
	{
		if (type.IsGenericTypeDefinition || !type.ContainsGenericParameters)
		{
			return type.FullName;
		}
		return type.Namespace + "." + type.Name;
	}

	internal static void GetClrNameAndNamespace(string fullTypeName, out string localName, out string ns)
	{
		int num = fullTypeName.LastIndexOf('.');
		if (num < 0)
		{
			ns = string.Empty;
			localName = fullTypeName.Replace('+', '.');
		}
		else
		{
			ns = fullTypeName.Substring(0, num);
			localName = fullTypeName.Substring(num + 1).Replace('+', '.');
		}
		int num2 = localName.IndexOf('[');
		if (num2 >= 0)
		{
			localName = localName.Substring(0, num2);
		}
	}

	internal static string GetDataContractNamespaceFromUri(string uriString)
	{
		if (!uriString.StartsWith("http://schemas.datacontract.org/2004/07/", StringComparison.Ordinal))
		{
			return uriString;
		}
		return uriString.Substring("http://schemas.datacontract.org/2004/07/".Length);
	}

	private static string GetGlobalDataContractNamespace(string clrNs, object[] nsAttributes)
	{
		string text = null;
		for (int i = 0; i < nsAttributes.Length; i++)
		{
			ContractNamespaceAttribute contractNamespaceAttribute = (ContractNamespaceAttribute)nsAttributes[i];
			string text2 = contractNamespaceAttribute.ClrNamespace ?? string.Empty;
			if (text2 == clrNs)
			{
				if (contractNamespaceAttribute.ContractNamespace == null)
				{
					throw new InvalidDataContractException(System.SR.Format(System.SR.InvalidGlobalDataContractNamespace, clrNs));
				}
				if (text != null)
				{
					throw new InvalidDataContractException(System.SR.Format(System.SR.DataContractNamespaceAlreadySet, text, contractNamespaceAttribute.ContractNamespace, clrNs));
				}
				text = contractNamespaceAttribute.ContractNamespace;
			}
		}
		return text;
	}

	private static string GetNamespacesDigest(string namespaces)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(namespaces);
		byte[] inArray = ComputeHash(bytes);
		char[] array = new char[24];
		int num = Convert.ToBase64CharArray(inArray, 0, 6, array, 0);
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < num; i++)
		{
			char c = array[i];
			switch (c)
			{
			case '/':
				stringBuilder.Append("_S");
				break;
			case '+':
				stringBuilder.Append("_P");
				break;
			default:
				stringBuilder.Append(c);
				break;
			case '=':
				break;
			}
		}
		return stringBuilder.ToString();
	}

	private static byte[] ComputeHash(byte[] namespaces)
	{
		int[] array = new int[16]
		{
			7, 12, 17, 22, 5, 9, 14, 20, 4, 11,
			16, 23, 6, 10, 15, 21
		};
		uint[] array2 = new uint[64]
		{
			3614090360u, 3905402710u, 606105819u, 3250441966u, 4118548399u, 1200080426u, 2821735955u, 4249261313u, 1770035416u, 2336552879u,
			4294925233u, 2304563134u, 1804603682u, 4254626195u, 2792965006u, 1236535329u, 4129170786u, 3225465664u, 643717713u, 3921069994u,
			3593408605u, 38016083u, 3634488961u, 3889429448u, 568446438u, 3275163606u, 4107603335u, 1163531501u, 2850285829u, 4243563512u,
			1735328473u, 2368359562u, 4294588738u, 2272392833u, 1839030562u, 4259657740u, 2763975236u, 1272893353u, 4139469664u, 3200236656u,
			681279174u, 3936430074u, 3572445317u, 76029189u, 3654602809u, 3873151461u, 530742520u, 3299628645u, 4096336452u, 1126891415u,
			2878612391u, 4237533241u, 1700485571u, 2399980690u, 4293915773u, 2240044497u, 1873313359u, 4264355552u, 2734768916u, 1309151649u,
			4149444226u, 3174756917u, 718787259u, 3951481745u
		};
		int num = (namespaces.Length + 8) / 64 + 1;
		uint num2 = 1732584193u;
		uint num3 = 4023233417u;
		uint num4 = 2562383102u;
		uint num5 = 271733878u;
		for (int i = 0; i < num; i++)
		{
			byte[] array3 = namespaces;
			int num6 = i * 64;
			if (num6 + 64 > namespaces.Length)
			{
				array3 = new byte[64];
				for (int j = num6; j < namespaces.Length; j++)
				{
					array3[j - num6] = namespaces[j];
				}
				if (num6 <= namespaces.Length)
				{
					array3[namespaces.Length - num6] = 128;
				}
				if (i == num - 1)
				{
					array3[56] = (byte)(namespaces.Length << 3);
					array3[57] = (byte)(namespaces.Length >> 5);
					array3[58] = (byte)(namespaces.Length >> 13);
					array3[59] = (byte)(namespaces.Length >> 21);
				}
				num6 = 0;
			}
			uint num7 = num2;
			uint num8 = num3;
			uint num9 = num4;
			uint num10 = num5;
			for (int k = 0; k < 64; k++)
			{
				uint num11;
				int num12;
				if (k < 16)
				{
					num11 = (num8 & num9) | (~num8 & num10);
					num12 = k;
				}
				else if (k < 32)
				{
					num11 = (num8 & num10) | (num9 & ~num10);
					num12 = 5 * k + 1;
				}
				else if (k < 48)
				{
					num11 = num8 ^ num9 ^ num10;
					num12 = 3 * k + 5;
				}
				else
				{
					num11 = num9 ^ (num8 | ~num10);
					num12 = 7 * k;
				}
				num12 = (num12 & 0xF) * 4 + num6;
				uint num13 = num10;
				num10 = num9;
				num9 = num8;
				num8 = num7 + num11 + array2[k] + BinaryPrimitives.ReadUInt32LittleEndian(array3.AsSpan(num12));
				num8 = (num8 << array[(k & 3) | ((k >> 2) & -4)]) | (num8 >> 32 - array[(k & 3) | ((k >> 2) & -4)]);
				num8 += num9;
				num7 = num13;
			}
			num2 += num7;
			num3 += num8;
			if (i < num - 1)
			{
				num4 += num9;
				num5 += num10;
			}
		}
		return new byte[6]
		{
			(byte)num2,
			(byte)(num2 >> 8),
			(byte)(num2 >> 16),
			(byte)(num2 >> 24),
			(byte)num3,
			(byte)(num3 >> 8)
		};
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static string ExpandGenericParameters(string format, Type type)
	{
		GenericNameProvider genericNameProvider = new GenericNameProvider(type);
		return ExpandGenericParameters(format, genericNameProvider);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static string ExpandGenericParameters(string format, IGenericNameProvider genericNameProvider)
	{
		string text = null;
		StringBuilder stringBuilder = new StringBuilder();
		IList<int> nestedParameterCounts = genericNameProvider.GetNestedParameterCounts();
		for (int i = 0; i < format.Length; i++)
		{
			char c = format[i];
			if (c == '{')
			{
				i++;
				int num = i;
				for (; i < format.Length && format[i] != '}'; i++)
				{
				}
				if (i == format.Length)
				{
					throw new InvalidDataContractException(System.SR.Format(System.SR.GenericNameBraceMismatch, format, genericNameProvider.GetGenericTypeName()));
				}
				if (format[num] == '#' && i == num + 1)
				{
					if (nestedParameterCounts.Count <= 1 && genericNameProvider.ParametersFromBuiltInNamespaces)
					{
						continue;
					}
					if (text == null)
					{
						StringBuilder stringBuilder2 = new StringBuilder(genericNameProvider.GetNamespaces());
						foreach (int item in nestedParameterCounts)
						{
							stringBuilder2.Insert(0, item.ToString(CultureInfo.InvariantCulture)).Insert(0, " ");
						}
						text = GetNamespacesDigest(stringBuilder2.ToString());
					}
					stringBuilder.Append(text);
				}
				else
				{
					if (!int.TryParse(format.AsSpan(num, i - num), out var result) || result < 0 || result >= genericNameProvider.GetParameterCount())
					{
						throw new InvalidDataContractException(System.SR.Format(System.SR.GenericParameterNotValid, format.Substring(num, i - num), genericNameProvider.GetGenericTypeName(), genericNameProvider.GetParameterCount() - 1));
					}
					stringBuilder.Append(genericNameProvider.GetParameterName(result));
				}
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}

	internal static bool IsTypeNullable(Type type)
	{
		if (type.IsValueType)
		{
			if (type.IsGenericType)
			{
				return type.GetGenericTypeDefinition() == Globals.TypeOfNullable;
			}
			return false;
		}
		return true;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static Dictionary<XmlQualifiedName, DataContract> ImportKnownTypeAttributes(Type type)
	{
		Dictionary<XmlQualifiedName, DataContract> knownDataContracts = null;
		Dictionary<Type, Type> typesChecked = new Dictionary<Type, Type>();
		ImportKnownTypeAttributes(type, typesChecked, ref knownDataContracts);
		return knownDataContracts;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static void ImportKnownTypeAttributes(Type type, Dictionary<Type, Type> typesChecked, ref Dictionary<XmlQualifiedName, DataContract> knownDataContracts)
	{
		while (type != null && IsTypeSerializable(type) && !typesChecked.ContainsKey(type))
		{
			typesChecked.Add(type, type);
			object[] array = type.GetCustomAttributes(Globals.TypeOfKnownTypeAttribute, inherit: false).ToArray();
			if (array != null)
			{
				bool flag = false;
				bool flag2 = false;
				for (int i = 0; i < array.Length; i++)
				{
					KnownTypeAttribute knownTypeAttribute = (KnownTypeAttribute)array[i];
					if (knownTypeAttribute.Type != null)
					{
						if (flag)
						{
							ThrowInvalidDataContractException(System.SR.Format(System.SR.KnownTypeAttributeOneScheme, GetClrTypeFullName(type)), type);
						}
						CheckAndAdd(knownTypeAttribute.Type, typesChecked, ref knownDataContracts);
						flag2 = true;
						continue;
					}
					if (flag || flag2)
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.KnownTypeAttributeOneScheme, GetClrTypeFullName(type)), type);
					}
					string methodName = knownTypeAttribute.MethodName;
					if (methodName == null)
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.KnownTypeAttributeNoData, GetClrTypeFullName(type)), type);
					}
					if (methodName.Length == 0)
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.KnownTypeAttributeEmptyString, GetClrTypeFullName(type)), type);
					}
					MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
					if (method == null)
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.KnownTypeAttributeUnknownMethod, methodName, GetClrTypeFullName(type)), type);
					}
					if (!Globals.TypeOfTypeEnumerable.IsAssignableFrom(method.ReturnType))
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.KnownTypeAttributeReturnType, GetClrTypeFullName(type), methodName), type);
					}
					object obj = method.Invoke(null, Array.Empty<object>());
					if (obj == null)
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.KnownTypeAttributeMethodNull, GetClrTypeFullName(type)), type);
					}
					foreach (Type item in (IEnumerable<Type>)obj)
					{
						if (item == null)
						{
							ThrowInvalidDataContractException(System.SR.Format(System.SR.KnownTypeAttributeValidMethodTypes, GetClrTypeFullName(type)), type);
						}
						CheckAndAdd(item, typesChecked, ref knownDataContracts);
					}
					flag = true;
				}
			}
			AppContext.TryGetSwitch("Switch.System.Runtime.Serialization.DataContracts.Auto_Import_KVP", out var isEnabled);
			if (isEnabled)
			{
				try
				{
					if (GetDataContract(type) is CollectionDataContract { IsDictionary: not false } collectionDataContract && collectionDataContract.ItemType.GetGenericTypeDefinition() == Globals.TypeOfKeyValue)
					{
						DataContract dataContract = GetDataContract(Globals.TypeOfKeyValuePair.MakeGenericType(collectionDataContract.ItemType.GetGenericArguments()));
						if (knownDataContracts == null)
						{
							knownDataContracts = new Dictionary<XmlQualifiedName, DataContract>();
						}
						knownDataContracts.TryAdd(dataContract.XmlName, dataContract);
					}
				}
				catch (InvalidDataContractException)
				{
				}
			}
			type = type.BaseType;
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static void CheckAndAdd(Type type, Dictionary<Type, Type> typesChecked, [NotNullIfNotNull("nameToDataContractTable")] ref Dictionary<XmlQualifiedName, DataContract> nameToDataContractTable)
	{
		type = UnwrapNullableType(type);
		DataContract dataContract = GetDataContract(type);
		DataContract value;
		if (nameToDataContractTable == null)
		{
			nameToDataContractTable = new Dictionary<XmlQualifiedName, DataContract>();
		}
		else if (nameToDataContractTable.TryGetValue(dataContract.XmlName, out value))
		{
			if (DataContractCriticalHelper.GetDataContractAdapterType(value.UnderlyingType) != DataContractCriticalHelper.GetDataContractAdapterType(type))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.DupContractInKnownTypes, type, value.UnderlyingType, dataContract.XmlName.Namespace, dataContract.XmlName.Name));
			}
			return;
		}
		nameToDataContractTable.Add(dataContract.XmlName, dataContract);
		ImportKnownTypeAttributes(type, typesChecked, ref nameToDataContractTable);
	}

	public sealed override bool Equals(object? obj)
	{
		if (this == obj)
		{
			return true;
		}
		return Equals(obj, new HashSet<DataContractPairKey>());
	}

	internal virtual bool Equals(object other, HashSet<DataContractPairKey> checkedContracts)
	{
		if (other is DataContract dataContract)
		{
			if (XmlName.Name == dataContract.XmlName.Name && XmlName.Namespace == dataContract.XmlName.Namespace)
			{
				return IsReference == dataContract.IsReference;
			}
			return false;
		}
		return false;
	}

	internal bool IsEqualOrChecked(object other, HashSet<DataContractPairKey> checkedContracts)
	{
		if (other == null)
		{
			return false;
		}
		if (this == other)
		{
			return true;
		}
		if (checkedContracts != null)
		{
			DataContractPairKey item = new DataContractPairKey(this, other);
			if (checkedContracts.Contains(item))
			{
				return true;
			}
			checkedContracts.Add(item);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	internal static bool IsTypeVisible(Type t)
	{
		if (t.IsGenericParameter)
		{
			return true;
		}
		if (!IsTypeAndDeclaringTypeVisible(t))
		{
			return false;
		}
		Type[] genericArguments = t.GetGenericArguments();
		foreach (Type t2 in genericArguments)
		{
			if (!IsTypeVisible(t2))
			{
				return false;
			}
		}
		return true;
	}

	internal static bool IsTypeAndDeclaringTypeVisible(Type t)
	{
		if (t.HasElementType)
		{
			return IsTypeVisible(t.GetElementType());
		}
		if (!t.IsNested)
		{
			if (!t.IsPublic)
			{
				return IsTypeVisibleInSerializationModule(t);
			}
			return true;
		}
		if (t.IsNestedPublic || IsTypeVisibleInSerializationModule(t))
		{
			return IsTypeVisible(t.DeclaringType);
		}
		return false;
	}

	internal static bool ConstructorRequiresMemberAccess(ConstructorInfo ctor)
	{
		if (ctor != null && !ctor.IsPublic)
		{
			return !IsMemberVisibleInSerializationModule(ctor);
		}
		return false;
	}

	internal static bool MethodRequiresMemberAccess(MethodInfo method)
	{
		if (method != null && !method.IsPublic)
		{
			return !IsMemberVisibleInSerializationModule(method);
		}
		return false;
	}

	internal static bool FieldRequiresMemberAccess(FieldInfo field)
	{
		if (field != null && !field.IsPublic)
		{
			return !IsMemberVisibleInSerializationModule(field);
		}
		return false;
	}

	private static bool IsTypeVisibleInSerializationModule(Type type)
	{
		if (type.Module.Equals(typeof(DataContract).Module) || IsAssemblyFriendOfSerialization(type.Assembly))
		{
			return !type.IsNestedPrivate;
		}
		return false;
	}

	private static bool IsMemberVisibleInSerializationModule(MemberInfo member)
	{
		if (!IsTypeVisibleInSerializationModule(member.DeclaringType))
		{
			return false;
		}
		if (member is MethodInfo methodInfo)
		{
			if (!methodInfo.IsAssembly)
			{
				return methodInfo.IsFamilyOrAssembly;
			}
			return true;
		}
		if (member is FieldInfo fieldInfo)
		{
			if (fieldInfo.IsAssembly || fieldInfo.IsFamilyOrAssembly)
			{
				return IsTypeVisible(fieldInfo.FieldType);
			}
			return false;
		}
		if (member is ConstructorInfo constructorInfo)
		{
			if (!constructorInfo.IsAssembly)
			{
				return constructorInfo.IsFamilyOrAssembly;
			}
			return true;
		}
		return false;
	}

	internal static bool IsAssemblyFriendOfSerialization(Assembly assembly)
	{
		InternalsVisibleToAttribute[] array = (InternalsVisibleToAttribute[])assembly.GetCustomAttributes(typeof(InternalsVisibleToAttribute));
		InternalsVisibleToAttribute[] array2 = array;
		foreach (InternalsVisibleToAttribute internalsVisibleToAttribute in array2)
		{
			string assemblyName = internalsVisibleToAttribute.AssemblyName;
			if (assemblyName.Trim().Equals("System.Runtime.Serialization") || Globals.FullSRSInternalsVisibleRegex().IsMatch(assemblyName))
			{
				return true;
			}
		}
		return false;
	}

	internal static string SanitizeTypeName(string typeName)
	{
		return typeName.Replace('.', '_');
	}
}
