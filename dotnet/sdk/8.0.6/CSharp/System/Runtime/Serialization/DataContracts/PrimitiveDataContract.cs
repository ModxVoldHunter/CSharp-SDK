using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml;

namespace System.Runtime.Serialization.DataContracts;

internal abstract class PrimitiveDataContract : DataContract
{
	private sealed class PrimitiveDataContractCriticalHelper : DataContractCriticalHelper
	{
		private MethodInfo _xmlFormatWriterMethod;

		private MethodInfo _xmlFormatContentWriterMethod;

		private MethodInfo _xmlFormatReaderMethod;

		internal MethodInfo XmlFormatWriterMethod
		{
			get
			{
				return _xmlFormatWriterMethod;
			}
			set
			{
				_xmlFormatWriterMethod = value;
			}
		}

		internal MethodInfo XmlFormatContentWriterMethod
		{
			get
			{
				return _xmlFormatContentWriterMethod;
			}
			set
			{
				_xmlFormatContentWriterMethod = value;
			}
		}

		internal MethodInfo XmlFormatReaderMethod
		{
			get
			{
				return _xmlFormatReaderMethod;
			}
			set
			{
				_xmlFormatReaderMethod = value;
			}
		}

		internal PrimitiveDataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, XmlDictionaryString name, XmlDictionaryString ns)
			: base(type)
		{
			SetDataContractName(name, ns);
		}
	}

	internal const string ContractTypeString = "PrimitiveDataContract";

	internal static readonly PrimitiveDataContract NullContract = new NullPrimitiveDataContract();

	private readonly PrimitiveDataContractCriticalHelper _helper;

	public override string ContractType => "PrimitiveDataContract";

	internal abstract string WriteMethodName { get; }

	internal abstract string ReadMethodName { get; }

	public override XmlDictionaryString TopLevelElementNamespace
	{
		get
		{
			return DictionaryGlobals.SerializationNamespace;
		}
		internal set
		{
		}
	}

	internal override bool CanContainReferences => false;

	internal override bool IsPrimitive => true;

	public override bool IsBuiltInDataContract => true;

	internal MethodInfo XmlFormatWriterMethod
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (_helper.XmlFormatWriterMethod == null)
			{
				if (UnderlyingType.IsValueType)
				{
					_helper.XmlFormatWriterMethod = typeof(XmlWriterDelegator).GetMethod(WriteMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[3]
					{
						UnderlyingType,
						typeof(XmlDictionaryString),
						typeof(XmlDictionaryString)
					});
				}
				else
				{
					_helper.XmlFormatWriterMethod = typeof(XmlObjectSerializerWriteContext).GetMethod(WriteMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[4]
					{
						typeof(XmlWriterDelegator),
						UnderlyingType,
						typeof(XmlDictionaryString),
						typeof(XmlDictionaryString)
					});
				}
			}
			return _helper.XmlFormatWriterMethod;
		}
	}

	internal MethodInfo XmlFormatContentWriterMethod
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (_helper.XmlFormatContentWriterMethod == null)
			{
				if (UnderlyingType.IsValueType)
				{
					_helper.XmlFormatContentWriterMethod = typeof(XmlWriterDelegator).GetMethod(WriteMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { UnderlyingType });
				}
				else
				{
					_helper.XmlFormatContentWriterMethod = typeof(XmlObjectSerializerWriteContext).GetMethod(WriteMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
					{
						typeof(XmlWriterDelegator),
						UnderlyingType
					});
				}
			}
			return _helper.XmlFormatContentWriterMethod;
		}
	}

	internal MethodInfo XmlFormatReaderMethod
	{
		get
		{
			PrimitiveDataContractCriticalHelper helper = _helper;
			return helper.XmlFormatReaderMethod ?? (helper.XmlFormatReaderMethod = typeof(XmlReaderDelegator).GetMethod(ReadMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
		}
	}

	protected PrimitiveDataContract([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, XmlDictionaryString name, XmlDictionaryString ns)
		: base(new PrimitiveDataContractCriticalHelper(type, name, ns))
	{
		_helper = base.Helper as PrimitiveDataContractCriticalHelper;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static PrimitiveDataContract GetPrimitiveDataContract(Type type)
	{
		return DataContract.GetBuiltInDataContract(type) as PrimitiveDataContract;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static PrimitiveDataContract GetPrimitiveDataContract(string name, string ns)
	{
		return DataContract.GetBuiltInDataContract(name, ns) as PrimitiveDataContract;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
	{
		xmlWriter.WriteAnyType(obj);
	}

	protected static object HandleReadValue(object obj, XmlObjectSerializerReadContext context)
	{
		context.AddNewObject(obj);
		return obj;
	}

	protected static bool TryReadNullAtTopLevel(XmlReaderDelegator reader)
	{
		Attributes attributes = new Attributes();
		attributes.Read(reader);
		if (attributes.Ref != Globals.NewObjectId)
		{
			throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.CannotDeserializeRefAtTopLevel, attributes.Ref));
		}
		if (attributes.XsiNil)
		{
			reader.Skip();
			return true;
		}
		return false;
	}

	internal override bool Equals(object other, HashSet<DataContractPairKey> checkedContracts)
	{
		if (other is PrimitiveDataContract)
		{
			Type type = GetType();
			Type type2 = other.GetType();
			if (!type.Equals(type2) && !type.IsSubclassOf(type2))
			{
				return type2.IsSubclassOf(type);
			}
			return true;
		}
		return false;
	}
}
