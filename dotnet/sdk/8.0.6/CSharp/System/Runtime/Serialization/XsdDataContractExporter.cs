using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization.DataContracts;
using System.Xml;
using System.Xml.Schema;

namespace System.Runtime.Serialization;

public class XsdDataContractExporter
{
	private ExportOptions _options;

	private XmlSchemaSet _schemas;

	private DataContractSet _dataContractSet;

	public ExportOptions? Options
	{
		get
		{
			return _options;
		}
		set
		{
			_options = value;
		}
	}

	public XmlSchemaSet Schemas
	{
		get
		{
			XmlSchemaSet schemaSet = GetSchemaSet();
			SchemaImporter.CompileSchemaSet(schemaSet);
			return schemaSet;
		}
	}

	private DataContractSet DataContractSet => _dataContractSet ?? (_dataContractSet = new DataContractSet(Options?.DataContractSurrogate, null, null));

	public XsdDataContractExporter()
	{
	}

	public XsdDataContractExporter(XmlSchemaSet? schemas)
	{
		_schemas = schemas;
	}

	private XmlSchemaSet GetSchemaSet()
	{
		if (_schemas == null)
		{
			_schemas = new XmlSchemaSet();
			_schemas.XmlResolver = null;
		}
		return _schemas;
	}

	private static void EnsureTypeNotGeneric(Type type)
	{
		if (type.ContainsGenericParameters)
		{
			throw new InvalidDataContractException(System.SR.Format(System.SR.GenericTypeNotExportable, type));
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void Export(ICollection<Assembly> assemblies)
	{
		ArgumentNullException.ThrowIfNull(assemblies, "assemblies");
		DataContractSet dataContractSet = ((_dataContractSet == null) ? null : new DataContractSet(_dataContractSet));
		try
		{
			foreach (Assembly assembly in assemblies)
			{
				if (assembly == null)
				{
					throw new ArgumentException(System.SR.Format(System.SR.CannotExportNullAssembly, "assemblies"));
				}
				Type[] types = assembly.GetTypes();
				for (int i = 0; i < types.Length; i++)
				{
					CheckAndAddType(types[i]);
				}
			}
			Export();
		}
		catch (Exception exception) when (!ExceptionUtility.IsFatal(exception))
		{
			_dataContractSet = dataContractSet;
			throw;
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void Export(ICollection<Type> types)
	{
		ArgumentNullException.ThrowIfNull(types, "types");
		DataContractSet dataContractSet = ((_dataContractSet == null) ? null : new DataContractSet(_dataContractSet));
		try
		{
			foreach (Type type in types)
			{
				if (type == null)
				{
					throw new ArgumentException(System.SR.Format(System.SR.CannotExportNullType, "types"));
				}
				AddType(type);
			}
			Export();
		}
		catch (Exception exception) when (!ExceptionUtility.IsFatal(exception))
		{
			_dataContractSet = dataContractSet;
			throw;
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void Export(Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		DataContractSet dataContractSet = ((_dataContractSet == null) ? null : new DataContractSet(_dataContractSet));
		try
		{
			AddType(type);
			Export();
		}
		catch (Exception exception) when (!ExceptionUtility.IsFatal(exception))
		{
			_dataContractSet = dataContractSet;
			throw;
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public XmlQualifiedName GetSchemaTypeName(Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		type = GetSurrogatedType(type);
		DataContract dataContract = DataContract.GetDataContract(type);
		EnsureTypeNotGeneric(dataContract.UnderlyingType);
		if (dataContract is XmlDataContract { IsAnonymous: not false })
		{
			return XmlQualifiedName.Empty;
		}
		return dataContract.XmlName;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public XmlSchemaType? GetSchemaType(Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		type = GetSurrogatedType(type);
		DataContract dataContract = DataContract.GetDataContract(type);
		EnsureTypeNotGeneric(dataContract.UnderlyingType);
		if (dataContract is XmlDataContract { IsAnonymous: not false } xmlDataContract)
		{
			return xmlDataContract.XsdType;
		}
		return null;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public XmlQualifiedName? GetRootElementName(Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		type = GetSurrogatedType(type);
		DataContract dataContract = DataContract.GetDataContract(type);
		EnsureTypeNotGeneric(dataContract.UnderlyingType);
		if (!(dataContract is XmlDataContract { HasRoot: false }))
		{
			return new XmlQualifiedName(dataContract.TopLevelElementName.Value, dataContract.TopLevelElementNamespace.Value);
		}
		return null;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private Type GetSurrogatedType(Type type)
	{
		ISerializationSurrogateProvider serializationSurrogateProvider = Options?.DataContractSurrogate;
		if (serializationSurrogateProvider != null)
		{
			type = DataContractSurrogateCaller.GetDataContractType(serializationSurrogateProvider, type);
		}
		return type;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void CheckAndAddType(Type type)
	{
		type = GetSurrogatedType(type);
		if (!type.ContainsGenericParameters && DataContract.IsTypeSerializable(type))
		{
			AddType(type);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void AddType(Type type)
	{
		DataContractSet.Add(type);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void Export()
	{
		AddKnownTypes();
		SchemaExporter schemaExporter = new SchemaExporter(GetSchemaSet(), DataContractSet);
		schemaExporter.Export();
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void AddKnownTypes()
	{
		if (Options == null)
		{
			return;
		}
		Collection<Type> knownTypes = Options.KnownTypes;
		if (knownTypes == null)
		{
			return;
		}
		for (int i = 0; i < knownTypes.Count; i++)
		{
			Type type = knownTypes[i];
			if (type == null)
			{
				throw new ArgumentException(System.SR.CannotExportNullKnownType);
			}
			AddType(type);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public bool CanExport(ICollection<Assembly> assemblies)
	{
		ArgumentNullException.ThrowIfNull(assemblies, "assemblies");
		DataContractSet dataContractSet = ((_dataContractSet == null) ? null : new DataContractSet(_dataContractSet));
		try
		{
			foreach (Assembly assembly in assemblies)
			{
				if (assembly == null)
				{
					throw new ArgumentException(System.SR.Format(System.SR.CannotExportNullAssembly, "assemblies"));
				}
				Type[] types = assembly.GetTypes();
				for (int i = 0; i < types.Length; i++)
				{
					CheckAndAddType(types[i]);
				}
			}
			AddKnownTypes();
			return true;
		}
		catch (InvalidDataContractException)
		{
			_dataContractSet = dataContractSet;
			return false;
		}
		catch (Exception exception) when (!ExceptionUtility.IsFatal(exception))
		{
			_dataContractSet = dataContractSet;
			throw;
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public bool CanExport(ICollection<Type> types)
	{
		ArgumentNullException.ThrowIfNull(types, "types");
		DataContractSet dataContractSet = ((_dataContractSet == null) ? null : new DataContractSet(_dataContractSet));
		try
		{
			foreach (Type type in types)
			{
				if (type == null)
				{
					throw new ArgumentException(System.SR.Format(System.SR.CannotExportNullType, "types"));
				}
				AddType(type);
			}
			AddKnownTypes();
			return true;
		}
		catch (InvalidDataContractException)
		{
			_dataContractSet = dataContractSet;
			return false;
		}
		catch (Exception exception) when (!ExceptionUtility.IsFatal(exception))
		{
			_dataContractSet = dataContractSet;
			throw;
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public bool CanExport(Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		DataContractSet dataContractSet = ((_dataContractSet == null) ? null : new DataContractSet(_dataContractSet));
		try
		{
			AddType(type);
			AddKnownTypes();
			return true;
		}
		catch (InvalidDataContractException)
		{
			_dataContractSet = dataContractSet;
			return false;
		}
		catch (Exception exception) when (!ExceptionUtility.IsFatal(exception))
		{
			_dataContractSet = dataContractSet;
			throw;
		}
	}
}
