using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace System.Reflection.Emit;

internal sealed class AssemblyBuilderImpl : AssemblyBuilder
{
	private readonly AssemblyName _assemblyName;

	private readonly Assembly _coreAssembly;

	private readonly MetadataBuilder _metadataBuilder;

	private ModuleBuilderImpl _module;

	private bool _previouslySaved;

	internal List<CustomAttributeWrapper> _customAttributes;

	internal AssemblyBuilderImpl(AssemblyName name, Assembly coreAssembly, IEnumerable<CustomAttributeBuilder> assemblyAttributes)
	{
		ArgumentNullException.ThrowIfNull(name, "name");
		name = (AssemblyName)name.Clone();
		ArgumentException.ThrowIfNullOrEmpty(name.Name, "AssemblyName.Name");
		_assemblyName = name;
		_coreAssembly = coreAssembly;
		_metadataBuilder = new MetadataBuilder();
		if (assemblyAttributes == null)
		{
			return;
		}
		foreach (CustomAttributeBuilder assemblyAttribute in assemblyAttributes)
		{
			SetCustomAttribute(assemblyAttribute);
		}
	}

	internal static AssemblyBuilderImpl DefinePersistedAssembly(AssemblyName name, Assembly coreAssembly, IEnumerable<CustomAttributeBuilder> assemblyAttributes)
	{
		return new AssemblyBuilderImpl(name, coreAssembly, assemblyAttributes);
	}

	private void WritePEImage(Stream peStream, BlobBuilder ilBuilder)
	{
		PEHeaderBuilder header = new PEHeaderBuilder(Machine.Unknown, 8192, 512, 4194304uL, 48, 0, 4, 0, 0, 0, 4, 0, Subsystem.WindowsCui, DllCharacteristics.DynamicBase | DllCharacteristics.NxCompatible | DllCharacteristics.NoSeh | DllCharacteristics.TerminalServerAware, Characteristics.Dll, 1048576uL, 4096uL, 1048576uL, 4096uL);
		ManagedPEBuilder managedPEBuilder = new ManagedPEBuilder(header, new MetadataRootBuilder(_metadataBuilder), ilBuilder);
		BlobBuilder blobBuilder = new BlobBuilder();
		managedPEBuilder.Serialize(blobBuilder);
		blobBuilder.WriteContentTo(peStream);
	}

	internal void Save(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream, "stream");
		if (_module == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_AModuleRequired);
		}
		if (_previouslySaved)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_CannotSaveMultipleTimes);
		}
		MetadataBuilder metadataBuilder = _metadataBuilder;
		StringHandle orAddString = _metadataBuilder.GetOrAddString(_assemblyName.Name);
		Version? version = _assemblyName.Version ?? new Version(0, 0, 0, 0);
		StringHandle culture = ((_assemblyName.CultureName == null) ? default(StringHandle) : _metadataBuilder.GetOrAddString(_assemblyName.CultureName));
		byte[] publicKey = _assemblyName.GetPublicKey();
		AssemblyDefinitionHandle assemblyDefinitionHandle = metadataBuilder.AddAssembly(orAddString, version, culture, (publicKey != null) ? _metadataBuilder.GetOrAddBlob(publicKey) : default(BlobHandle), AddContentType((AssemblyFlags)_assemblyName.Flags, _assemblyName.ContentType), (AssemblyHashAlgorithm)_assemblyName.HashAlgorithm);
		_module.WriteCustomAttributes(_customAttributes, assemblyDefinitionHandle);
		_module.AppendMetadata();
		BlobBuilder ilBuilder = new BlobBuilder();
		WritePEImage(stream, ilBuilder);
		_previouslySaved = true;
	}

	private static AssemblyFlags AddContentType(AssemblyFlags flags, AssemblyContentType contentType)
	{
		return (AssemblyFlags)(((int)contentType << 9) | (int)flags);
	}

	internal void Save(string assemblyFileName)
	{
		ArgumentNullException.ThrowIfNull(assemblyFileName, "assemblyFileName");
		using FileStream stream = new FileStream(assemblyFileName, FileMode.Create, FileAccess.Write);
		Save(stream);
	}

	protected override ModuleBuilder DefineDynamicModuleCore(string name)
	{
		if (_module != null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_NoMultiModuleAssembly);
		}
		_module = new ModuleBuilderImpl(name, _coreAssembly, _metadataBuilder);
		return _module;
	}

	protected override ModuleBuilder GetDynamicModuleCore(string name)
	{
		if (_module != null && _module.ScopeName.Equals(name))
		{
			return _module;
		}
		return null;
	}

	protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
	{
		if (_customAttributes == null)
		{
			_customAttributes = new List<CustomAttributeWrapper>();
		}
		_customAttributes.Add(new CustomAttributeWrapper(con, binaryAttribute));
	}
}
