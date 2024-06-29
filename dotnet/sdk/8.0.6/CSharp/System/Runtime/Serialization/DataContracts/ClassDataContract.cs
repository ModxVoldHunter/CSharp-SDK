using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Xml;

namespace System.Runtime.Serialization.DataContracts;

internal sealed class ClassDataContract : DataContract
{
	private sealed class ClassDataContractCriticalHelper : DataContractCriticalHelper
	{
		internal readonly struct Member
		{
			internal readonly DataMember _member;

			internal readonly string _ns;

			internal readonly int _baseTypeIndex;

			internal Member(DataMember member, string ns, int baseTypeIndex)
			{
				_member = member;
				_ns = ns;
				_baseTypeIndex = baseTypeIndex;
			}
		}

		internal sealed class DataMemberConflictComparer : IComparer<Member>
		{
			internal static DataMemberConflictComparer Singleton = new DataMemberConflictComparer();

			public int Compare(Member x, Member y)
			{
				int num = string.CompareOrdinal(x._ns, y._ns);
				if (num != 0)
				{
					return num;
				}
				int num2 = string.CompareOrdinal(x._member.Name, y._member.Name);
				if (num2 != 0)
				{
					return num2;
				}
				return x._baseTypeIndex - y._baseTypeIndex;
			}
		}

		private static Type[] s_serInfoCtorArgs;

		private ClassDataContract _baseContract;

		private List<DataMember> _members;

		private MethodInfo _onSerializing;

		private MethodInfo _onSerialized;

		private MethodInfo _onDeserializing;

		private MethodInfo _onDeserialized;

		private MethodInfo _extensionDataSetMethod;

		private Dictionary<XmlQualifiedName, DataContract> _knownDataContracts;

		private string _serializationExceptionMessage;

		private bool _isKnownTypeAttributeChecked;

		private bool _isMethodChecked;

		private bool _isNonAttributedType;

		private bool _hasDataContract;

		private readonly bool _hasExtensionData;

		internal XmlDictionaryString[] ContractNamespaces;

		internal XmlDictionaryString[] MemberNames;

		internal XmlDictionaryString[] MemberNamespaces;

		internal ClassDataContract BaseClassContract
		{
			get
			{
				return _baseContract;
			}
			set
			{
				_baseContract = value;
				if (_baseContract != null && base.IsValueType)
				{
					ThrowInvalidDataContractException(System.SR.Format(System.SR.ValueTypeCannotHaveBaseType, base.XmlName.Name, base.XmlName.Namespace, _baseContract.XmlName.Name, _baseContract.XmlName.Namespace));
				}
			}
		}

		internal List<DataMember> Members
		{
			get
			{
				return _members;
			}
			set
			{
				_members = value;
			}
		}

		internal MethodInfo OnSerializing
		{
			get
			{
				EnsureMethodsImported();
				return _onSerializing;
			}
		}

		internal MethodInfo OnSerialized
		{
			get
			{
				EnsureMethodsImported();
				return _onSerialized;
			}
		}

		internal MethodInfo OnDeserializing
		{
			get
			{
				EnsureMethodsImported();
				return _onDeserializing;
			}
		}

		internal MethodInfo OnDeserialized
		{
			get
			{
				EnsureMethodsImported();
				return _onDeserialized;
			}
		}

		internal MethodInfo ExtensionDataSetMethod
		{
			get
			{
				EnsureMethodsImported();
				return _extensionDataSetMethod;
			}
		}

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

		internal string SerializationExceptionMessage => _serializationExceptionMessage;

		internal string DeserializationExceptionMessage
		{
			get
			{
				if (_serializationExceptionMessage != null)
				{
					return System.SR.Format(System.SR.ReadOnlyClassDeserialization, _serializationExceptionMessage);
				}
				return null;
			}
		}

		internal override bool IsISerializable { get; set; }

		internal bool HasDataContract => _hasDataContract;

		internal bool HasExtensionData => _hasExtensionData;

		internal bool IsNonAttributedType => _isNonAttributedType;

		internal XmlFormatClassWriterDelegate XmlFormatWriterDelegate { get; set; }

		internal XmlFormatClassReaderDelegate XmlFormatReaderDelegate { get; set; }

		internal XmlDictionaryString[] ChildElementNamespaces { get; set; }

		private static Type[] SerInfoCtorArgs
		{
			get
			{
				object obj = s_serInfoCtorArgs;
				if (obj == null)
				{
					obj = new Type[2]
					{
						typeof(SerializationInfo),
						typeof(StreamingContext)
					};
					s_serInfoCtorArgs = (Type[])obj;
				}
				return (Type[])obj;
			}
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal ClassDataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
			: base(type)
		{
			XmlQualifiedName xmlNameAndSetHasDataContract = GetXmlNameAndSetHasDataContract(type);
			if (type == Globals.TypeOfDBNull)
			{
				base.XmlName = xmlNameAndSetHasDataContract;
				_members = new List<DataMember>();
				XmlDictionary xmlDictionary = new XmlDictionary(2);
				base.Name = xmlDictionary.Add(base.XmlName.Name);
				base.Namespace = xmlDictionary.Add(base.XmlName.Namespace);
				ContractNamespaces = (MemberNames = (MemberNamespaces = Array.Empty<XmlDictionaryString>()));
				EnsureMethodsImported();
				return;
			}
			Type type2 = type.BaseType;
			IsISerializable = Globals.TypeOfISerializable.IsAssignableFrom(type);
			SetIsNonAttributedType(type);
			if (IsISerializable)
			{
				if (HasDataContract)
				{
					throw new InvalidDataContractException(System.SR.Format(System.SR.ISerializableCannotHaveDataContract, DataContract.GetClrTypeFullName(type)));
				}
				if (type2 != null && (!type2.IsSerializable || !Globals.TypeOfISerializable.IsAssignableFrom(type2)))
				{
					type2 = null;
				}
			}
			base.IsValueType = type.IsValueType;
			if (type2 != null && type2 != Globals.TypeOfObject && type2 != Globals.TypeOfValueType && type2 != Globals.TypeOfUri)
			{
				DataContract dataContract = DataContract.GetDataContract(type2);
				if (dataContract is CollectionDataContract collectionDataContract)
				{
					BaseClassContract = collectionDataContract.SharedTypeContract as ClassDataContract;
				}
				else
				{
					BaseClassContract = dataContract as ClassDataContract;
				}
				if (BaseClassContract != null && BaseClassContract.IsNonAttributedType && !_isNonAttributedType)
				{
					throw new InvalidDataContractException(System.SR.Format(System.SR.AttributedTypesCannotInheritFromNonAttributedSerializableTypes, DataContract.GetClrTypeFullName(type), DataContract.GetClrTypeFullName(type2)));
				}
			}
			else
			{
				BaseClassContract = null;
			}
			_hasExtensionData = Globals.TypeOfIExtensibleDataObject.IsAssignableFrom(type);
			if (_hasExtensionData && !HasDataContract && !IsNonAttributedType)
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.OnlyDataContractTypesCanHaveExtensionData, DataContract.GetClrTypeFullName(type)));
			}
			if (IsISerializable)
			{
				SetDataContractName(xmlNameAndSetHasDataContract);
			}
			else
			{
				base.XmlName = xmlNameAndSetHasDataContract;
				ImportDataMembers();
				XmlDictionary xmlDictionary2 = new XmlDictionary(2 + Members.Count);
				base.Name = xmlDictionary2.Add(base.XmlName.Name);
				base.Namespace = xmlDictionary2.Add(base.XmlName.Namespace);
				int num = 0;
				int num2 = 0;
				if (BaseClassContract == null)
				{
					MemberNames = new XmlDictionaryString[Members.Count];
					MemberNamespaces = new XmlDictionaryString[Members.Count];
					ContractNamespaces = new XmlDictionaryString[1];
				}
				else
				{
					if (BaseClassContract.IsReadOnlyContract)
					{
						_serializationExceptionMessage = BaseClassContract.SerializationExceptionMessage;
					}
					num = BaseClassContract.MemberNames.Length;
					MemberNames = new XmlDictionaryString[Members.Count + num];
					Array.Copy(BaseClassContract.MemberNames, MemberNames, num);
					MemberNamespaces = new XmlDictionaryString[Members.Count + num];
					Array.Copy(BaseClassContract.MemberNamespaces, MemberNamespaces, num);
					num2 = BaseClassContract.ContractNamespaces.Length;
					ContractNamespaces = new XmlDictionaryString[1 + num2];
					Array.Copy(BaseClassContract.ContractNamespaces, ContractNamespaces, num2);
				}
				ContractNamespaces[num2] = base.Namespace;
				for (int i = 0; i < Members.Count; i++)
				{
					MemberNames[i + num] = xmlDictionary2.Add(Members[i].Name);
					MemberNamespaces[i + num] = base.Namespace;
				}
			}
			EnsureMethodsImported();
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal ClassDataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, XmlDictionaryString ns, string[] memberNames)
			: base(type)
		{
			base.XmlName = new XmlQualifiedName(GetXmlNameAndSetHasDataContract(type).Name, ns.Value);
			ImportDataMembers();
			XmlDictionary xmlDictionary = new XmlDictionary(1 + Members.Count);
			base.Name = xmlDictionary.Add(base.XmlName.Name);
			base.Namespace = ns;
			ContractNamespaces = new XmlDictionaryString[1] { base.Namespace };
			MemberNames = new XmlDictionaryString[Members.Count];
			MemberNamespaces = new XmlDictionaryString[Members.Count];
			for (int i = 0; i < Members.Count; i++)
			{
				Members[i].Name = memberNames[i];
				MemberNames[i] = xmlDictionary.Add(Members[i].Name);
				MemberNamespaces[i] = base.Namespace;
			}
			EnsureMethodsImported();
		}

		private void EnsureIsReferenceImported(Type type)
		{
			bool flag = false;
			DataContractAttribute dataContractAttribute;
			bool flag2 = DataContract.TryGetDCAttribute(type, out dataContractAttribute);
			if (BaseClassContract != null)
			{
				if (flag2 && dataContractAttribute.IsReferenceSetExplicitly)
				{
					bool isReference = BaseClassContract.IsReference;
					if ((isReference && !dataContractAttribute.IsReference) || (!isReference && dataContractAttribute.IsReference))
					{
						DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.InconsistentIsReference, DataContract.GetClrTypeFullName(type), dataContractAttribute.IsReference, DataContract.GetClrTypeFullName(BaseClassContract.UnderlyingType), BaseClassContract.IsReference), type);
					}
					else
					{
						flag = dataContractAttribute.IsReference;
					}
				}
				else
				{
					flag = BaseClassContract.IsReference;
				}
			}
			else if (flag2 && dataContractAttribute.IsReference)
			{
				flag = dataContractAttribute.IsReference;
			}
			if (flag && type.IsValueType)
			{
				DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.ValueTypeCannotHaveIsReference, DataContract.GetClrTypeFullName(type), true, false), type);
			}
			else
			{
				base.IsReference = flag;
			}
		}

		[MemberNotNull("_members")]
		[MemberNotNull("Members")]
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void ImportDataMembers()
		{
			Type underlyingType = base.UnderlyingType;
			EnsureIsReferenceImported(underlyingType);
			List<DataMember> list = new List<DataMember>();
			Dictionary<string, DataMember> memberNamesTable = new Dictionary<string, DataMember>();
			MemberInfo[] array = ((!_isNonAttributedType) ? underlyingType.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : underlyingType.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public));
			foreach (MemberInfo memberInfo in array)
			{
				if (HasDataContract)
				{
					object[] array2 = memberInfo.GetCustomAttributes(typeof(DataMemberAttribute), inherit: false).ToArray();
					if (array2 == null || array2.Length == 0)
					{
						continue;
					}
					if (array2.Length > 1)
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.TooManyDataMembers, DataContract.GetClrTypeFullName(memberInfo.DeclaringType), memberInfo.Name));
					}
					DataMember dataMember = new DataMember(memberInfo);
					if (memberInfo is PropertyInfo propertyInfo)
					{
						MethodInfo getMethod = propertyInfo.GetMethod;
						if (getMethod != null && IsMethodOverriding(getMethod))
						{
							continue;
						}
						MethodInfo setMethod = propertyInfo.SetMethod;
						if (setMethod != null && IsMethodOverriding(setMethod))
						{
							continue;
						}
						if (getMethod == null)
						{
							ThrowInvalidDataContractException(System.SR.Format(System.SR.NoGetMethodForProperty, propertyInfo.DeclaringType, propertyInfo.Name));
						}
						if (setMethod == null && !SetIfGetOnlyCollection(dataMember, skipIfReadOnlyContract: false))
						{
							_serializationExceptionMessage = System.SR.Format(System.SR.NoSetMethodForProperty, propertyInfo.DeclaringType, propertyInfo.Name);
						}
						if (getMethod.GetParameters().Length != 0)
						{
							ThrowInvalidDataContractException(System.SR.Format(System.SR.IndexedPropertyCannotBeSerialized, propertyInfo.DeclaringType, propertyInfo.Name));
						}
					}
					else if (!(memberInfo is FieldInfo))
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.InvalidMember, DataContract.GetClrTypeFullName(underlyingType), memberInfo.Name));
					}
					DataMemberAttribute dataMemberAttribute = (DataMemberAttribute)array2[0];
					if (dataMemberAttribute.IsNameSetExplicitly)
					{
						if (string.IsNullOrEmpty(dataMemberAttribute.Name))
						{
							ThrowInvalidDataContractException(System.SR.Format(System.SR.InvalidDataMemberName, memberInfo.Name, DataContract.GetClrTypeFullName(underlyingType)));
						}
						dataMember.Name = dataMemberAttribute.Name;
					}
					else
					{
						dataMember.Name = memberInfo.Name;
					}
					dataMember.Name = DataContract.EncodeLocalName(dataMember.Name);
					dataMember.IsNullable = DataContract.IsTypeNullable(dataMember.MemberType);
					dataMember.IsRequired = dataMemberAttribute.IsRequired;
					if (dataMemberAttribute.IsRequired && base.IsReference)
					{
						DataContractCriticalHelper.ThrowInvalidDataContractException(System.SR.Format(System.SR.IsRequiredDataMemberOnIsReferenceDataContractType, DataContract.GetClrTypeFullName(memberInfo.DeclaringType), memberInfo.Name, true), underlyingType);
					}
					dataMember.EmitDefaultValue = dataMemberAttribute.EmitDefaultValue;
					dataMember.Order = dataMemberAttribute.Order;
					CheckAndAddMember(list, dataMember, memberNamesTable);
					continue;
				}
				if (_isNonAttributedType)
				{
					FieldInfo fieldInfo = memberInfo as FieldInfo;
					PropertyInfo propertyInfo2 = memberInfo as PropertyInfo;
					if ((fieldInfo == null && propertyInfo2 == null) || (fieldInfo != null && fieldInfo.IsInitOnly))
					{
						continue;
					}
					object[] array3 = memberInfo.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), inherit: false).ToArray();
					if (array3 != null && array3.Length != 0)
					{
						if (array3.Length <= 1)
						{
							continue;
						}
						ThrowInvalidDataContractException(System.SR.Format(System.SR.TooManyIgnoreDataMemberAttributes, DataContract.GetClrTypeFullName(memberInfo.DeclaringType), memberInfo.Name));
					}
					DataMember dataMember2 = new DataMember(memberInfo);
					if (propertyInfo2 != null)
					{
						MethodInfo getMethod2 = propertyInfo2.GetGetMethod();
						if (getMethod2 == null || IsMethodOverriding(getMethod2) || getMethod2.GetParameters().Length != 0)
						{
							continue;
						}
						MethodInfo setMethod2 = propertyInfo2.SetMethod;
						if (setMethod2 == null)
						{
							if (!SetIfGetOnlyCollection(dataMember2, skipIfReadOnlyContract: true))
							{
								continue;
							}
						}
						else if (!setMethod2.IsPublic || IsMethodOverriding(setMethod2))
						{
							continue;
						}
						if (_hasExtensionData && dataMember2.MemberType == Globals.TypeOfExtensionDataObject && memberInfo.Name == "ExtensionData")
						{
							continue;
						}
					}
					dataMember2.Name = DataContract.EncodeLocalName(memberInfo.Name);
					dataMember2.IsNullable = DataContract.IsTypeNullable(dataMember2.MemberType);
					CheckAndAddMember(list, dataMember2, memberNamesTable);
					continue;
				}
				FieldInfo fieldInfo2 = memberInfo as FieldInfo;
				if (!(fieldInfo2 != null) || fieldInfo2.IsNotSerialized)
				{
					continue;
				}
				DataMember dataMember3 = new DataMember(memberInfo);
				dataMember3.Name = DataContract.EncodeLocalName(memberInfo.Name);
				object[] customAttributes = fieldInfo2.GetCustomAttributes(Globals.TypeOfOptionalFieldAttribute, inherit: false);
				if (customAttributes == null || customAttributes.Length == 0)
				{
					if (base.IsReference)
					{
						DataContractCriticalHelper.ThrowInvalidDataContractException(System.SR.Format(System.SR.NonOptionalFieldMemberOnIsReferenceSerializableType, DataContract.GetClrTypeFullName(memberInfo.DeclaringType), memberInfo.Name, true), underlyingType);
					}
					dataMember3.IsRequired = true;
				}
				dataMember3.IsNullable = DataContract.IsTypeNullable(dataMember3.MemberType);
				CheckAndAddMember(list, dataMember3, memberNamesTable);
			}
			if (list.Count > 1)
			{
				list.Sort(DataMemberComparer.Singleton);
			}
			SetIfMembersHaveConflict(list);
			Interlocked.MemoryBarrier();
			_members = list;
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private static bool SetIfGetOnlyCollection(DataMember memberContract, bool skipIfReadOnlyContract)
		{
			if (CollectionDataContract.IsCollection(memberContract.MemberType, constructorRequired: false, skipIfReadOnlyContract) && !memberContract.MemberType.IsValueType)
			{
				memberContract.IsGetOnlyCollection = true;
				return true;
			}
			return false;
		}

		private void SetIfMembersHaveConflict(List<DataMember> members)
		{
			if (BaseClassContract == null)
			{
				return;
			}
			int num = 0;
			List<Member> list = new List<Member>();
			foreach (DataMember member in members)
			{
				list.Add(new Member(member, base.XmlName.Namespace, num));
			}
			for (ClassDataContract baseClassContract = BaseClassContract; baseClassContract != null; baseClassContract = baseClassContract.BaseClassContract)
			{
				num++;
				foreach (DataMember member2 in baseClassContract.Members)
				{
					list.Add(new Member(member2, baseClassContract.XmlName.Namespace, num));
				}
			}
			IComparer<Member> singleton = DataMemberConflictComparer.Singleton;
			list.Sort(singleton);
			int num2;
			for (num2 = 0; num2 < list.Count - 1; num2++)
			{
				int num3 = num2;
				int i = num2;
				bool flag = false;
				for (; i < list.Count - 1 && list[i]._member.Name == list[i + 1]._member.Name && list[i]._ns == list[i + 1]._ns; i++)
				{
					list[i]._member.ConflictingMember = list[i + 1]._member;
					if (!flag)
					{
						flag = list[i + 1]._member.HasConflictingNameAndType || list[i]._member.MemberType != list[i + 1]._member.MemberType;
					}
				}
				if (flag)
				{
					for (int j = num3; j <= i; j++)
					{
						list[j]._member.HasConflictingNameAndType = true;
					}
				}
				num2 = i + 1;
			}
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private XmlQualifiedName GetXmlNameAndSetHasDataContract(Type type)
		{
			return DataContract.GetXmlName(type, out _hasDataContract);
		}

		private void SetIsNonAttributedType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] Type type)
		{
			_isNonAttributedType = !type.IsSerializable && !_hasDataContract && IsNonAttributedTypeValidForSerialization(type);
		}

		private static bool IsMethodOverriding(MethodInfo method)
		{
			if (method.IsVirtual)
			{
				return (method.Attributes & MethodAttributes.VtableLayoutMask) == 0;
			}
			return false;
		}

		internal void EnsureMethodsImported()
		{
			if (_isMethodChecked || !(base.UnderlyingType != null))
			{
				return;
			}
			lock (this)
			{
				if (_isMethodChecked)
				{
					return;
				}
				Type underlyingType = base.UnderlyingType;
				MethodInfo[] methods = underlyingType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (MethodInfo methodInfo in methods)
				{
					Type prevAttributeType = null;
					ParameterInfo[] parameters = methodInfo.GetParameters();
					if (HasExtensionData && IsValidExtensionDataSetMethod(methodInfo, parameters))
					{
						if (methodInfo.Name == "System.Runtime.Serialization.IExtensibleDataObject.set_ExtensionData" || !methodInfo.IsPublic)
						{
							_extensionDataSetMethod = XmlFormatGeneratorStatics.ExtensionDataSetExplicitMethodInfo;
						}
						else
						{
							_extensionDataSetMethod = methodInfo;
						}
					}
					if (IsValidCallback(methodInfo, parameters, Globals.TypeOfOnSerializingAttribute, _onSerializing, ref prevAttributeType))
					{
						_onSerializing = methodInfo;
					}
					if (IsValidCallback(methodInfo, parameters, Globals.TypeOfOnSerializedAttribute, _onSerialized, ref prevAttributeType))
					{
						_onSerialized = methodInfo;
					}
					if (IsValidCallback(methodInfo, parameters, Globals.TypeOfOnDeserializingAttribute, _onDeserializing, ref prevAttributeType))
					{
						_onDeserializing = methodInfo;
					}
					if (IsValidCallback(methodInfo, parameters, Globals.TypeOfOnDeserializedAttribute, _onDeserialized, ref prevAttributeType))
					{
						_onDeserialized = methodInfo;
					}
				}
				Interlocked.MemoryBarrier();
				_isMethodChecked = true;
			}
		}

		private bool IsValidExtensionDataSetMethod(MethodInfo method, ParameterInfo[] parameters)
		{
			if (method.Name == "System.Runtime.Serialization.IExtensibleDataObject.set_ExtensionData" || method.Name == "set_ExtensionData")
			{
				if (_extensionDataSetMethod != null)
				{
					ThrowInvalidDataContractException(System.SR.Format(System.SR.DuplicateExtensionDataSetMethod, method, _extensionDataSetMethod, DataContract.GetClrTypeFullName(method.DeclaringType)));
				}
				if (method.ReturnType != Globals.TypeOfVoid)
				{
					DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.ExtensionDataSetMustReturnVoid, DataContract.GetClrTypeFullName(method.DeclaringType), method), method.DeclaringType);
				}
				if (parameters == null || parameters.Length != 1 || parameters[0].ParameterType != Globals.TypeOfExtensionDataObject)
				{
					DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.ExtensionDataSetParameterInvalid, DataContract.GetClrTypeFullName(method.DeclaringType), method, Globals.TypeOfExtensionDataObject), method.DeclaringType);
				}
				return true;
			}
			return false;
		}

		private static bool IsValidCallback(MethodInfo method, ParameterInfo[] parameters, Type attributeType, MethodInfo currentCallback, ref Type prevAttributeType)
		{
			if (method.IsDefined(attributeType, inherit: false))
			{
				if (currentCallback != null)
				{
					DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.DuplicateCallback, method, currentCallback, DataContract.GetClrTypeFullName(method.DeclaringType), attributeType), method.DeclaringType);
				}
				else if (prevAttributeType != null)
				{
					DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.DuplicateAttribute, prevAttributeType, attributeType, DataContract.GetClrTypeFullName(method.DeclaringType), method), method.DeclaringType);
				}
				else if (method.IsVirtual)
				{
					DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.CallbacksCannotBeVirtualMethods, method, DataContract.GetClrTypeFullName(method.DeclaringType), attributeType), method.DeclaringType);
				}
				else
				{
					if (method.ReturnType != Globals.TypeOfVoid)
					{
						DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.CallbackMustReturnVoid, DataContract.GetClrTypeFullName(method.DeclaringType), method), method.DeclaringType);
					}
					if (parameters == null || parameters.Length != 1 || parameters[0].ParameterType != Globals.TypeOfStreamingContext)
					{
						DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.CallbackParameterInvalid, DataContract.GetClrTypeFullName(method.DeclaringType), method, Globals.TypeOfStreamingContext), method.DeclaringType);
					}
					prevAttributeType = attributeType;
				}
				return true;
			}
			return false;
		}

		internal ConstructorInfo GetISerializableConstructor()
		{
			if (!IsISerializable)
			{
				return null;
			}
			ConstructorInfo constructor = base.UnderlyingType.GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, SerInfoCtorArgs);
			if (constructor == null)
			{
				throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.SerializationInfo_ConstructorNotFound, DataContract.GetClrTypeFullName(base.UnderlyingType)));
			}
			return constructor;
		}

		internal ConstructorInfo GetNonAttributedTypeConstructor()
		{
			if (!IsNonAttributedType)
			{
				return null;
			}
			Type underlyingType = base.UnderlyingType;
			if (underlyingType.IsValueType)
			{
				return null;
			}
			ConstructorInfo constructor = underlyingType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			if (constructor == null)
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.NonAttributedSerializableTypesMustHaveDefaultConstructor, DataContract.GetClrTypeFullName(underlyingType)));
			}
			return constructor;
		}
	}

	internal sealed class DataMemberComparer : IComparer<DataMember>
	{
		internal static DataMemberComparer Singleton = new DataMemberComparer();

		public int Compare(DataMember x, DataMember y)
		{
			if (x == null && y == null)
			{
				return 0;
			}
			if (x == null || y == null)
			{
				return -1;
			}
			int num = (int)(x.Order - y.Order);
			if (num != 0)
			{
				return num;
			}
			return string.CompareOrdinal(x.Name, y.Name);
		}
	}

	internal const string ContractTypeString = "ClassDataContract";

	public XmlDictionaryString[] ContractNamespaces;

	public XmlDictionaryString[] MemberNames;

	internal XmlDictionaryString[] MemberNamespaces;

	private XmlDictionaryString[] _childElementNamespaces;

	private ClassDataContractCriticalHelper _helper;

	private ConstructorInfo _nonAttributedTypeConstructor;

	private Func<object> _makeNewInstance;

	public override string ContractType => "ClassDataContract";

	public override DataContract BaseContract
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			return BaseClassContract;
		}
	}

	internal ClassDataContract BaseClassContract
	{
		get
		{
			return _helper.BaseClassContract;
		}
		set
		{
			_helper.BaseClassContract = value;
		}
	}

	internal List<DataMember> Members
	{
		get
		{
			return _helper.Members;
		}
		set
		{
			_helper.Members = value;
		}
	}

	public override ReadOnlyCollection<DataMember> DataMembers
	{
		get
		{
			if (Members != null)
			{
				return Members.AsReadOnly();
			}
			return ReadOnlyCollection<DataMember>.Empty;
		}
	}

	internal XmlDictionaryString[] ChildElementNamespaces
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (_childElementNamespaces == null)
			{
				lock (this)
				{
					if (_childElementNamespaces == null)
					{
						if (_helper.ChildElementNamespaces == null)
						{
							XmlDictionaryString[] childElementNamespaces = CreateChildElementNamespaces();
							Interlocked.MemoryBarrier();
							_helper.ChildElementNamespaces = childElementNamespaces;
						}
						_childElementNamespaces = _helper.ChildElementNamespaces;
					}
				}
			}
			return _childElementNamespaces;
		}
	}

	internal MethodInfo OnSerializing => _helper.OnSerializing;

	internal MethodInfo OnSerialized => _helper.OnSerialized;

	internal MethodInfo OnDeserializing => _helper.OnDeserializing;

	internal MethodInfo OnDeserialized => _helper.OnDeserialized;

	internal MethodInfo ExtensionDataSetMethod => _helper.ExtensionDataSetMethod;

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

	public override bool IsISerializable
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

	internal bool IsNonAttributedType => _helper.IsNonAttributedType;

	internal bool HasExtensionData => _helper.HasExtensionData;

	internal string SerializationExceptionMessage => _helper.SerializationExceptionMessage;

	internal string DeserializationExceptionMessage => _helper.DeserializationExceptionMessage;

	internal bool IsReadOnlyContract => DeserializationExceptionMessage != null;

	[UnconditionalSuppressMessage("AOT Analysis", "IL3050:RequiresDynamicCodeAttribute", Justification = "Fields cannot be annotated, annotating the use instead")]
	private Func<object> MakeNewInstance => _makeNewInstance ?? (_makeNewInstance = FastInvokerBuilder.GetMakeNewInstanceFunc(UnderlyingType));

	internal XmlFormatClassWriterDelegate XmlFormatWriterDelegate
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
						XmlFormatClassWriterDelegate xmlFormatWriterDelegate = CreateXmlFormatWriterDelegate();
						Interlocked.MemoryBarrier();
						_helper.XmlFormatWriterDelegate = xmlFormatWriterDelegate;
					}
				}
			}
			return _helper.XmlFormatWriterDelegate;
		}
	}

	internal XmlFormatClassReaderDelegate XmlFormatReaderDelegate
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
							DataContract.ThrowInvalidDataContractException(DeserializationExceptionMessage, null);
						}
						XmlFormatClassReaderDelegate xmlFormatReaderDelegate = CreateXmlFormatReaderDelegate();
						Interlocked.MemoryBarrier();
						_helper.XmlFormatReaderDelegate = xmlFormatReaderDelegate;
					}
				}
			}
			return _helper.XmlFormatReaderDelegate;
		}
	}

	internal Type ObjectType
	{
		get
		{
			Type type = UnderlyingType;
			if (type.IsValueType && !IsNonAttributedType)
			{
				type = Globals.TypeOfValueType;
			}
			return type;
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal ClassDataContract(Type type)
		: base(new ClassDataContractCriticalHelper(type))
	{
		InitClassDataContract();
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private ClassDataContract(Type type, XmlDictionaryString ns, string[] memberNames)
		: base(new ClassDataContractCriticalHelper(type, ns, memberNames))
	{
		InitClassDataContract();
	}

	[MemberNotNull("_helper")]
	private void InitClassDataContract()
	{
		_helper = base.Helper as ClassDataContractCriticalHelper;
		ContractNamespaces = _helper.ContractNamespaces;
		MemberNames = _helper.MemberNames;
		MemberNamespaces = _helper.MemberNamespaces;
	}

	internal ConstructorInfo GetISerializableConstructor()
	{
		return _helper.GetISerializableConstructor();
	}

	internal ConstructorInfo GetNonAttributedTypeConstructor()
	{
		if (_nonAttributedTypeConstructor == null)
		{
			_nonAttributedTypeConstructor = _helper.GetNonAttributedTypeConstructor();
		}
		return _nonAttributedTypeConstructor;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	internal bool CreateNewInstanceViaDefaultConstructor([NotNullWhen(true)] out object obj)
	{
		ConstructorInfo nonAttributedTypeConstructor = GetNonAttributedTypeConstructor();
		if (nonAttributedTypeConstructor == null || UnderlyingType == Globals.TypeOfSchemaDefinedType)
		{
			obj = null;
			return false;
		}
		if (nonAttributedTypeConstructor.IsPublic)
		{
			obj = MakeNewInstance();
		}
		else
		{
			obj = nonAttributedTypeConstructor.Invoke(Array.Empty<object>());
		}
		return true;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private XmlFormatClassWriterDelegate CreateXmlFormatWriterDelegate()
	{
		return new XmlFormatWriterGenerator().GenerateClassWriter(this);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private XmlFormatClassReaderDelegate CreateXmlFormatReaderDelegate()
	{
		return new XmlFormatReaderGenerator().GenerateClassReader(this);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static ClassDataContract CreateClassDataContractForKeyValue(Type type, XmlDictionaryString ns, string[] memberNames)
	{
		return new ClassDataContract(type, ns, memberNames);
	}

	internal static void CheckAndAddMember(List<DataMember> members, DataMember memberContract, Dictionary<string, DataMember> memberNamesTable)
	{
		if (memberNamesTable.TryGetValue(memberContract.Name, out var value))
		{
			Type declaringType = memberContract.MemberInfo.DeclaringType;
			DataContract.ThrowInvalidDataContractException(System.SR.Format(declaringType.IsEnum ? System.SR.DupEnumMemberValue : System.SR.DupMemberName, value.MemberInfo.Name, memberContract.MemberInfo.Name, DataContract.GetClrTypeFullName(declaringType), memberContract.Name), declaringType);
		}
		memberNamesTable.Add(memberContract.Name, memberContract);
		members.Add(memberContract);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static XmlDictionaryString GetChildNamespaceToDeclare(DataContract dataContract, Type childType, XmlDictionary dictionary)
	{
		childType = DataContract.UnwrapNullableType(childType);
		if (!childType.IsEnum && !Globals.TypeOfIXmlSerializable.IsAssignableFrom(childType) && DataContract.GetBuiltInDataContract(childType) == null && childType != Globals.TypeOfDBNull)
		{
			string @namespace = DataContract.GetXmlName(childType).Namespace;
			if (@namespace.Length > 0 && @namespace != dataContract.Namespace.Value)
			{
				return dictionary.Add(@namespace);
			}
		}
		return null;
	}

	private static bool IsArraySegment(Type t)
	{
		if (t.IsGenericType)
		{
			return t.GetGenericTypeDefinition() == typeof(ArraySegment<>);
		}
		return false;
	}

	internal static bool IsNonAttributedTypeValidForSerialization([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] Type type)
	{
		if (type.IsArray)
		{
			return false;
		}
		if (type.IsEnum)
		{
			return false;
		}
		if (type.IsGenericParameter)
		{
			return false;
		}
		if (Globals.TypeOfIXmlSerializable.IsAssignableFrom(type))
		{
			return false;
		}
		if (type.IsPointer)
		{
			return false;
		}
		if (type.IsDefined(Globals.TypeOfCollectionDataContractAttribute, inherit: false))
		{
			return false;
		}
		if (!IsArraySegment(type))
		{
			Type[] interfaces = type.GetInterfaces();
			foreach (Type type2 in interfaces)
			{
				if (CollectionDataContract.IsCollectionInterface(type2))
				{
					return false;
				}
			}
		}
		if (type.IsSerializable)
		{
			return false;
		}
		if (Globals.TypeOfISerializable.IsAssignableFrom(type))
		{
			return false;
		}
		if (type.IsDefined(Globals.TypeOfDataContractAttribute, inherit: false))
		{
			return false;
		}
		if (type == Globals.TypeOfExtensionDataObject)
		{
			return false;
		}
		if (type.IsValueType)
		{
			return type.IsVisible;
		}
		if (type.IsVisible)
		{
			return type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes) != null;
		}
		return false;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private XmlDictionaryString[] CreateChildElementNamespaces()
	{
		if (Members == null)
		{
			return null;
		}
		XmlDictionaryString[] array = null;
		if (BaseClassContract != null)
		{
			array = BaseClassContract.ChildElementNamespaces;
		}
		int num = ((array != null) ? array.Length : 0);
		XmlDictionaryString[] array2 = new XmlDictionaryString[Members.Count + num];
		if (num > 0)
		{
			Array.Copy(array, array2, array.Length);
		}
		XmlDictionary dictionary = new XmlDictionary();
		for (int i = 0; i < Members.Count; i++)
		{
			array2[i + num] = GetChildNamespaceToDeclare(this, Members[i].MemberType, dictionary);
		}
		return array2;
	}

	private void EnsureMethodsImported()
	{
		_helper.EnsureMethodsImported();
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
	{
		XmlFormatWriterDelegate(xmlWriter, obj, context, this);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override object ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context)
	{
		xmlReader.Read();
		object result = XmlFormatReaderDelegate(xmlReader, context, MemberNames, MemberNamespaces);
		xmlReader.ReadEndElement();
		return result;
	}

	internal bool RequiresMemberAccessForRead(SecurityException securityException)
	{
		EnsureMethodsImported();
		if (!DataContract.IsTypeVisible(UnderlyingType))
		{
			if (securityException != null)
			{
				throw new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractTypeNotPublic, DataContract.GetClrTypeFullName(UnderlyingType)), securityException);
			}
			return true;
		}
		if (BaseClassContract != null && BaseClassContract.RequiresMemberAccessForRead(securityException))
		{
			return true;
		}
		if (DataContract.ConstructorRequiresMemberAccess(GetISerializableConstructor()))
		{
			if (securityException != null)
			{
				throw new SecurityException(System.SR.Format(System.SR.PartialTrustIXmlSerialzableNoPublicConstructor, DataContract.GetClrTypeFullName(UnderlyingType)), securityException);
			}
			return true;
		}
		if (DataContract.ConstructorRequiresMemberAccess(GetNonAttributedTypeConstructor()))
		{
			if (securityException != null)
			{
				throw new SecurityException(System.SR.Format(System.SR.PartialTrustNonAttributedSerializableTypeNoPublicConstructor, DataContract.GetClrTypeFullName(UnderlyingType)), securityException);
			}
			return true;
		}
		if (DataContract.MethodRequiresMemberAccess(OnDeserializing))
		{
			if (securityException != null)
			{
				throw new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractOnDeserializingNotPublic, DataContract.GetClrTypeFullName(UnderlyingType), OnDeserializing.Name), securityException);
			}
			return true;
		}
		if (DataContract.MethodRequiresMemberAccess(OnDeserialized))
		{
			if (securityException != null)
			{
				throw new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractOnDeserializedNotPublic, DataContract.GetClrTypeFullName(UnderlyingType), OnDeserialized.Name), securityException);
			}
			return true;
		}
		if (Members != null)
		{
			for (int i = 0; i < Members.Count; i++)
			{
				if (!Members[i].RequiresMemberAccessForSet())
				{
					continue;
				}
				if (securityException != null)
				{
					if (Members[i].MemberInfo is FieldInfo)
					{
						throw new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractFieldSetNotPublic, DataContract.GetClrTypeFullName(UnderlyingType), Members[i].MemberInfo.Name), securityException);
					}
					throw new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractPropertySetNotPublic, DataContract.GetClrTypeFullName(UnderlyingType), Members[i].MemberInfo.Name), securityException);
				}
				return true;
			}
		}
		return false;
	}

	internal bool RequiresMemberAccessForWrite(SecurityException securityException)
	{
		EnsureMethodsImported();
		if (!DataContract.IsTypeVisible(UnderlyingType))
		{
			if (securityException != null)
			{
				throw new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractTypeNotPublic, DataContract.GetClrTypeFullName(UnderlyingType)), securityException);
			}
			return true;
		}
		if (BaseClassContract != null && BaseClassContract.RequiresMemberAccessForWrite(securityException))
		{
			return true;
		}
		if (DataContract.MethodRequiresMemberAccess(OnSerializing))
		{
			if (securityException != null)
			{
				throw new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractOnSerializingNotPublic, DataContract.GetClrTypeFullName(UnderlyingType), OnSerializing.Name), securityException);
			}
			return true;
		}
		if (DataContract.MethodRequiresMemberAccess(OnSerialized))
		{
			if (securityException != null)
			{
				throw new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractOnSerializedNotPublic, DataContract.GetClrTypeFullName(UnderlyingType), OnSerialized.Name), securityException);
			}
			return true;
		}
		if (Members != null)
		{
			for (int i = 0; i < Members.Count; i++)
			{
				if (!Members[i].RequiresMemberAccessForGet())
				{
					continue;
				}
				if (securityException != null)
				{
					if (Members[i].MemberInfo is FieldInfo)
					{
						throw new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractFieldGetNotPublic, DataContract.GetClrTypeFullName(UnderlyingType), Members[i].MemberInfo.Name), securityException);
					}
					throw new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractPropertyGetNotPublic, DataContract.GetClrTypeFullName(UnderlyingType), Members[i].MemberInfo.Name), securityException);
				}
				return true;
			}
		}
		return false;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override DataContract BindGenericParameters(DataContract[] paramContracts, Dictionary<DataContract, DataContract> boundContracts = null)
	{
		Type underlyingType = UnderlyingType;
		if (!underlyingType.IsGenericType || !underlyingType.ContainsGenericParameters)
		{
			return this;
		}
		lock (this)
		{
			if (boundContracts != null && boundContracts.TryGetValue(this, out var value))
			{
				return value;
			}
			XmlQualifiedName xmlName;
			object[] array;
			Type type;
			if (underlyingType.IsGenericTypeDefinition)
			{
				xmlName = XmlName;
				array = paramContracts;
				Type[] array2 = new Type[paramContracts.Length];
				for (int i = 0; i < paramContracts.Length; i++)
				{
					array2[i] = paramContracts[i].UnderlyingType;
				}
				type = underlyingType.MakeGenericType(array2);
			}
			else
			{
				xmlName = DataContract.GetXmlName(underlyingType.GetGenericTypeDefinition());
				Type[] genericArguments = underlyingType.GetGenericArguments();
				array = new object[genericArguments.Length];
				for (int j = 0; j < genericArguments.Length; j++)
				{
					Type type2 = genericArguments[j];
					if (type2.IsGenericParameter)
					{
						array[j] = paramContracts[type2.GenericParameterPosition];
						genericArguments[j] = paramContracts[type2.GenericParameterPosition].UnderlyingType;
					}
					else
					{
						array[j] = type2;
					}
				}
				type = underlyingType.MakeGenericType(genericArguments);
			}
			ClassDataContract classDataContract = new ClassDataContract(type);
			if (boundContracts == null)
			{
				boundContracts = new Dictionary<DataContract, DataContract>();
			}
			boundContracts.Add(this, classDataContract);
			classDataContract.XmlName = DataContract.CreateQualifiedName(DataContract.ExpandGenericParameters(XmlConvert.DecodeName(xmlName.Name), new GenericNameProvider(DataContract.GetClrTypeFullName(UnderlyingType), array)), xmlName.Namespace);
			if (BaseClassContract != null)
			{
				classDataContract.BaseClassContract = (ClassDataContract)BaseClassContract.BindGenericParameters(paramContracts, boundContracts);
			}
			classDataContract.IsISerializable = IsISerializable;
			classDataContract.IsValueType = IsValueType;
			classDataContract.IsReference = IsReference;
			if (Members != null)
			{
				classDataContract.Members = new List<DataMember>(Members.Count);
				foreach (DataMember member in Members)
				{
					classDataContract.Members.Add(member.BindGenericParameters(paramContracts, boundContracts));
				}
			}
			return classDataContract;
		}
	}

	[UnconditionalSuppressMessage("AOT Analysis", "IL3050:RequiresDynamicCode", Justification = "All ctor's required to create an instance of this type are marked with RequiresDynamicCode.")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "All ctor's required to create an instance of this type are marked with RequiresUnreferencedCode.")]
	internal override bool Equals(object other, HashSet<DataContractPairKey> checkedContracts)
	{
		if (IsEqualOrChecked(other, checkedContracts))
		{
			return true;
		}
		if (base.Equals(other, checkedContracts) && other is ClassDataContract classDataContract)
		{
			if (IsISerializable)
			{
				if (!classDataContract.IsISerializable)
				{
					return false;
				}
			}
			else
			{
				if (classDataContract.IsISerializable)
				{
					return false;
				}
				if (Members == null)
				{
					if (classDataContract.Members != null && !IsEveryDataMemberOptional(classDataContract.Members))
					{
						return false;
					}
				}
				else if (classDataContract.Members == null)
				{
					if (!IsEveryDataMemberOptional(Members))
					{
						return false;
					}
				}
				else
				{
					Dictionary<string, DataMember> dictionary = new Dictionary<string, DataMember>(Members.Count);
					List<DataMember> list = new List<DataMember>();
					for (int i = 0; i < Members.Count; i++)
					{
						dictionary.Add(Members[i].Name, Members[i]);
					}
					for (int j = 0; j < classDataContract.Members.Count; j++)
					{
						if (dictionary.TryGetValue(classDataContract.Members[j].Name, out var value))
						{
							if (!value.Equals(classDataContract.Members[j], checkedContracts))
							{
								return false;
							}
							dictionary.Remove(value.Name);
						}
						else
						{
							list.Add(classDataContract.Members[j]);
						}
					}
					if (!IsEveryDataMemberOptional(dictionary.Values))
					{
						return false;
					}
					if (!IsEveryDataMemberOptional(list))
					{
						return false;
					}
				}
			}
			if (BaseClassContract == null)
			{
				return classDataContract.BaseClassContract == null;
			}
			if (classDataContract.BaseClassContract == null)
			{
				return false;
			}
			return BaseClassContract.Equals(classDataContract.BaseClassContract, checkedContracts);
		}
		return false;
	}

	private static bool IsEveryDataMemberOptional(IEnumerable<DataMember> dataMembers)
	{
		return !dataMembers.Any((DataMember dm) => dm.IsRequired);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}
