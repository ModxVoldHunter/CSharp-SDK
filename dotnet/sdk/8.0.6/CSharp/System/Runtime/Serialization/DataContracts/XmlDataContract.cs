using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Runtime.Serialization.DataContracts;

public sealed class XmlDataContract : DataContract
{
	private sealed class XmlDataContractCriticalHelper : DataContractCriticalHelper
	{
		private Dictionary<XmlQualifiedName, DataContract> _knownDataContracts;

		private bool _isKnownTypeAttributeChecked;

		private XmlDictionaryString _topLevelElementName;

		private XmlDictionaryString _topLevelElementNamespace;

		private bool _isTopLevelElementNullable;

		private bool _isTypeDefinedOnImport;

		private CreateXmlSerializableDelegate _createXmlSerializable;

		private XmlSchemaType _xsdType;

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

		internal XmlSchemaType XsdType
		{
			get
			{
				return _xsdType;
			}
			set
			{
				_xsdType = value;
			}
		}

		internal bool IsAnonymous => _xsdType != null;

		internal override XmlDictionaryString TopLevelElementName
		{
			get
			{
				return _topLevelElementName;
			}
			set
			{
				_topLevelElementName = value;
			}
		}

		internal override XmlDictionaryString TopLevelElementNamespace
		{
			get
			{
				return _topLevelElementNamespace;
			}
			set
			{
				_topLevelElementNamespace = value;
			}
		}

		internal bool IsTopLevelElementNullable
		{
			get
			{
				return _isTopLevelElementNullable;
			}
			set
			{
				_isTopLevelElementNullable = value;
			}
		}

		internal bool IsTypeDefinedOnImport
		{
			get
			{
				return _isTypeDefinedOnImport;
			}
			set
			{
				_isTypeDefinedOnImport = value;
			}
		}

		internal CreateXmlSerializableDelegate CreateXmlSerializableDelegate
		{
			get
			{
				return _createXmlSerializable;
			}
			set
			{
				_createXmlSerializable = value;
			}
		}

		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal XmlDataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
			: base(type)
		{
			if (type.IsDefined(Globals.TypeOfDataContractAttribute, inherit: false))
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.IXmlSerializableCannotHaveDataContract, DataContract.GetClrTypeFullName(type)));
			}
			if (type.IsDefined(Globals.TypeOfCollectionDataContractAttribute, inherit: false))
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.IXmlSerializableCannotHaveCollectionDataContract, DataContract.GetClrTypeFullName(type)));
			}
			SchemaExporter.GetXmlTypeInfo(type, out var xmlName, out var xsdType, out var hasRoot);
			base.XmlName = xmlName;
			XsdType = xsdType;
			HasRoot = hasRoot;
			XmlDictionary xmlDictionary = new XmlDictionary();
			base.Name = xmlDictionary.Add(base.XmlName.Name);
			base.Namespace = xmlDictionary.Add(base.XmlName.Namespace);
			object[] array = base.UnderlyingType?.GetCustomAttributes(Globals.TypeOfXmlRootAttribute, inherit: false).ToArray();
			if (array == null || array.Length == 0)
			{
				if (hasRoot)
				{
					_topLevelElementName = base.Name;
					_topLevelElementNamespace = ((base.XmlName.Namespace == "http://www.w3.org/2001/XMLSchema") ? DictionaryGlobals.EmptyString : base.Namespace);
					_isTopLevelElementNullable = true;
				}
				return;
			}
			if (hasRoot)
			{
				XmlRootAttribute xmlRootAttribute = (XmlRootAttribute)array[0];
				_isTopLevelElementNullable = xmlRootAttribute.IsNullable;
				string elementName = xmlRootAttribute.ElementName;
				_topLevelElementName = (string.IsNullOrEmpty(elementName) ? base.Name : xmlDictionary.Add(DataContract.EncodeLocalName(elementName)));
				string @namespace = xmlRootAttribute.Namespace;
				_topLevelElementNamespace = (string.IsNullOrEmpty(@namespace) ? DictionaryGlobals.EmptyString : xmlDictionary.Add(@namespace));
				return;
			}
			throw new InvalidDataContractException(System.SR.Format(System.SR.IsAnyCannotHaveXmlRoot, DataContract.GetClrTypeFullName(base.UnderlyingType)));
		}
	}

	internal const string ContractTypeString = "XmlDataContract";

	private readonly XmlDataContractCriticalHelper _helper;

	public override string? ContractType => "XmlDataContract";

	public override Dictionary<XmlQualifiedName, DataContract>? KnownDataContracts
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

	public XmlSchemaType? XsdType
	{
		get
		{
			return _helper.XsdType;
		}
		internal set
		{
			_helper.XsdType = value;
		}
	}

	public bool IsAnonymous => _helper.IsAnonymous;

	public new bool IsValueType
	{
		get
		{
			return _helper.IsValueType;
		}
		set
		{
			_helper.IsValueType = value;
		}
	}

	public new bool HasRoot
	{
		get
		{
			return _helper.HasRoot;
		}
		internal set
		{
			_helper.HasRoot = value;
		}
	}

	public override XmlDictionaryString? TopLevelElementName
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

	public override XmlDictionaryString? TopLevelElementNamespace
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

	public bool IsTopLevelElementNullable
	{
		get
		{
			return _helper.IsTopLevelElementNullable;
		}
		internal set
		{
			_helper.IsTopLevelElementNullable = value;
		}
	}

	public bool IsTypeDefinedOnImport
	{
		get
		{
			return _helper.IsTypeDefinedOnImport;
		}
		set
		{
			_helper.IsTypeDefinedOnImport = value;
		}
	}

	internal CreateXmlSerializableDelegate CreateXmlSerializableDelegate
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (DataContractSerializer.Option == SerializationOption.CodeGenOnly || DataContractSerializer.Option == SerializationOption.ReflectionAsBackup)
			{
				if (_helper.CreateXmlSerializableDelegate == null)
				{
					lock (this)
					{
						if (_helper.CreateXmlSerializableDelegate == null)
						{
							CreateXmlSerializableDelegate createXmlSerializableDelegate = GenerateCreateXmlSerializableDelegate();
							Interlocked.MemoryBarrier();
							_helper.CreateXmlSerializableDelegate = createXmlSerializableDelegate;
						}
					}
				}
				return _helper.CreateXmlSerializableDelegate;
			}
			return () => ReflectionCreateXmlSerializable(UnderlyingType);
		}
	}

	internal override bool CanContainReferences => false;

	public override bool IsBuiltInDataContract
	{
		get
		{
			if (!(UnderlyingType == Globals.TypeOfXmlElement))
			{
				return UnderlyingType == Globals.TypeOfXmlNodeArray;
			}
			return true;
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal XmlDataContract(Type type)
		: base(new XmlDataContractCriticalHelper(type))
	{
		_helper = base.Helper as XmlDataContractCriticalHelper;
	}

	private ConstructorInfo GetConstructor()
	{
		if (UnderlyingType.IsValueType)
		{
			return null;
		}
		ConstructorInfo constructor = UnderlyingType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		if (constructor == null)
		{
			throw new InvalidDataContractException(System.SR.Format(System.SR.IXmlSerializableMustHaveDefaultConstructor, DataContract.GetClrTypeFullName(UnderlyingType)));
		}
		return constructor;
	}

	internal void SetTopLevelElementName(XmlQualifiedName elementName)
	{
		if (elementName != null)
		{
			XmlDictionary xmlDictionary = new XmlDictionary();
			TopLevelElementName = xmlDictionary.Add(elementName.Name);
			TopLevelElementNamespace = xmlDictionary.Add(elementName.Namespace);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal CreateXmlSerializableDelegate GenerateCreateXmlSerializableDelegate()
	{
		Type underlyingType = UnderlyingType;
		CodeGenerator codeGenerator = new CodeGenerator();
		bool flag = RequiresMemberAccessForCreate(null) && !(underlyingType.FullName == "System.Xml.Linq.XElement");
		try
		{
			codeGenerator.BeginMethod("Create" + DataContract.GetClrTypeFullName(underlyingType), typeof(CreateXmlSerializableDelegate), flag);
		}
		catch (SecurityException securityException)
		{
			if (!flag)
			{
				throw;
			}
			RequiresMemberAccessForCreate(securityException);
		}
		if (underlyingType.IsValueType)
		{
			LocalBuilder localBuilder = codeGenerator.DeclareLocal(underlyingType);
			codeGenerator.Ldloca(localBuilder);
			codeGenerator.InitObj(underlyingType);
			codeGenerator.Ldloc(localBuilder);
		}
		else
		{
			ConstructorInfo constructorInfo = GetConstructor();
			if (!constructorInfo.IsPublic && underlyingType.FullName == "System.Xml.Linq.XElement")
			{
				Type type = underlyingType.Assembly.GetType("System.Xml.Linq.XName");
				if (type != null)
				{
					MethodInfo method = type.GetMethod("op_Implicit", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(string) });
					ConstructorInfo constructor = underlyingType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { type });
					if (method != null && constructor != null)
					{
						codeGenerator.Ldstr("default");
						codeGenerator.Call(method);
						constructorInfo = constructor;
					}
				}
			}
			codeGenerator.New(constructorInfo);
		}
		codeGenerator.ConvertValue(UnderlyingType, Globals.TypeOfIXmlSerializable);
		codeGenerator.Ret();
		return (CreateXmlSerializableDelegate)codeGenerator.EndMethod();
	}

	private bool RequiresMemberAccessForCreate(SecurityException securityException)
	{
		if (!DataContract.IsTypeVisible(UnderlyingType))
		{
			if (securityException != null)
			{
				throw new SecurityException(System.SR.Format(System.SR.PartialTrustIXmlSerializableTypeNotPublic, DataContract.GetClrTypeFullName(UnderlyingType)), securityException);
			}
			return true;
		}
		if (DataContract.ConstructorRequiresMemberAccess(GetConstructor()))
		{
			if (securityException != null)
			{
				throw new SecurityException(System.SR.Format(System.SR.PartialTrustIXmlSerialzableNoPublicConstructor, DataContract.GetClrTypeFullName(UnderlyingType)), securityException);
			}
			return true;
		}
		return false;
	}

	internal IXmlSerializable ReflectionCreateXmlSerializable(Type type)
	{
		if (type.IsValueType)
		{
			throw new NotImplementedException("ReflectionCreateXmlSerializable - value type");
		}
		object obj;
		if (type == typeof(XElement))
		{
			obj = new XElement("default");
		}
		else
		{
			ConstructorInfo constructor = GetConstructor();
			obj = constructor.Invoke(Array.Empty<object>());
		}
		return (IXmlSerializable)obj;
	}

	internal override bool Equals(object other, HashSet<DataContractPairKey> checkedContracts)
	{
		if (IsEqualOrChecked(other, checkedContracts))
		{
			return true;
		}
		if (other is XmlDataContract xmlDataContract)
		{
			if (HasRoot != xmlDataContract.HasRoot)
			{
				return false;
			}
			if (IsAnonymous)
			{
				return xmlDataContract.IsAnonymous;
			}
			if (XmlName.Name == xmlDataContract.XmlName.Name)
			{
				return XmlName.Namespace == xmlDataContract.XmlName.Namespace;
			}
			return false;
		}
		return false;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
	{
		if (context == null)
		{
			XmlObjectSerializerWriteContext.WriteRootIXmlSerializable(xmlWriter, obj);
		}
		else
		{
			context.WriteIXmlSerializable(xmlWriter, obj);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override object ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context)
	{
		object obj;
		if (context == null)
		{
			obj = XmlObjectSerializerReadContext.ReadRootIXmlSerializable(xmlReader, this, isMemberType: true);
		}
		else
		{
			obj = context.ReadIXmlSerializable(xmlReader, this, isMemberType: true);
			context.AddNewObject(obj);
		}
		xmlReader.ReadEndElement();
		return obj;
	}
}
