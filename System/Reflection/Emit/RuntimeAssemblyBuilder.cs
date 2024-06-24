using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace System.Reflection.Emit;

internal sealed class RuntimeAssemblyBuilder : AssemblyBuilder
{
	internal readonly AssemblyBuilderAccess _access;

	private readonly RuntimeAssembly _internalAssembly;

	private readonly RuntimeModuleBuilder _manifestModuleBuilder;

	private bool _isManifestModuleUsedAsDefinedModule;

	private static readonly object s_assemblyBuilderLock = new object();

	internal object SyncRoot => InternalAssembly.SyncRoot;

	internal RuntimeAssembly InternalAssembly => _internalAssembly;

	public override string FullName => InternalAssembly.FullName;

	public override Module ManifestModule => _manifestModuleBuilder.InternalModule;

	public override bool ReflectionOnly => InternalAssembly.ReflectionOnly;

	public override long HostContext => InternalAssembly.HostContext;

	public override bool IsCollectible => InternalAssembly.IsCollectible;

	internal RuntimeAssemblyBuilder(AssemblyName name, AssemblyBuilderAccess access, AssemblyLoadContext assemblyLoadContext, IEnumerable<CustomAttributeBuilder> assemblyAttributes)
	{
		_access = access;
		_internalAssembly = CreateDynamicAssembly(assemblyLoadContext, name, access);
		_manifestModuleBuilder = new RuntimeModuleBuilder(this, (RuntimeModule)InternalAssembly.ManifestModule);
		if (assemblyAttributes == null)
		{
			return;
		}
		foreach (CustomAttributeBuilder assemblyAttribute in assemblyAttributes)
		{
			SetCustomAttribute(assemblyAttribute);
		}
	}

	[DllImport("QCall", EntryPoint = "AppDomain_CreateDynamicAssembly", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "AppDomain_CreateDynamicAssembly")]
	private unsafe static extern void CreateDynamicAssembly(ObjectHandleOnStack assemblyLoadContext, NativeAssemblyNameParts* pAssemblyName, AssemblyHashAlgorithm hashAlgId, AssemblyBuilderAccess access, ObjectHandleOnStack retAssembly);

	private unsafe static RuntimeAssembly CreateDynamicAssembly(AssemblyLoadContext assemblyLoadContext, AssemblyName name, AssemblyBuilderAccess access)
	{
		//The blocks IL_0038, IL_003c, IL_003e, IL_0057, IL_008b, IL_008e, IL_0091, IL_00c2 are reachable both inside and outside the pinned region starting at IL_0033. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		RuntimeAssembly o = null;
		byte[] publicKey = name.GetPublicKey();
		fixed (char* pName = name.Name)
		{
			string? cultureName = name.CultureName;
			char* intPtr;
			byte[] array;
			NativeAssemblyNameParts nativeAssemblyNameParts;
			ref NativeAssemblyNameParts reference;
			int cbPublicKeyOrToken;
			if (cultureName == null)
			{
				char* pCultureName;
				intPtr = (pCultureName = null);
				array = publicKey;
				fixed (byte* ptr = array)
				{
					byte* pPublicKeyOrToken = ptr;
					nativeAssemblyNameParts = default(NativeAssemblyNameParts);
					nativeAssemblyNameParts._flags = name.RawFlags;
					nativeAssemblyNameParts._pName = pName;
					nativeAssemblyNameParts._pCultureName = pCultureName;
					nativeAssemblyNameParts._pPublicKeyOrToken = pPublicKeyOrToken;
					reference = ref nativeAssemblyNameParts;
					cbPublicKeyOrToken = ((publicKey != null) ? publicKey.Length : 0);
					reference._cbPublicKeyOrToken = cbPublicKeyOrToken;
					nativeAssemblyNameParts.SetVersion(name.Version, 0);
					CreateDynamicAssembly(ObjectHandleOnStack.Create(ref assemblyLoadContext), &nativeAssemblyNameParts, name.HashAlgorithm, access, ObjectHandleOnStack.Create(ref o));
				}
			}
			else
			{
				fixed (char* ptr2 = &cultureName.GetPinnableReference())
				{
					char* pCultureName;
					intPtr = (pCultureName = ptr2);
					array = publicKey;
					fixed (byte* ptr = array)
					{
						byte* pPublicKeyOrToken = ptr;
						nativeAssemblyNameParts = default(NativeAssemblyNameParts);
						nativeAssemblyNameParts._flags = name.RawFlags;
						nativeAssemblyNameParts._pName = pName;
						nativeAssemblyNameParts._pCultureName = pCultureName;
						nativeAssemblyNameParts._pPublicKeyOrToken = pPublicKeyOrToken;
						reference = ref nativeAssemblyNameParts;
						cbPublicKeyOrToken = (reference._cbPublicKeyOrToken = ((publicKey != null) ? publicKey.Length : 0));
						nativeAssemblyNameParts.SetVersion(name.Version, 0);
						CreateDynamicAssembly(ObjectHandleOnStack.Create(ref assemblyLoadContext), &nativeAssemblyNameParts, name.HashAlgorithm, access, ObjectHandleOnStack.Create(ref o));
					}
				}
			}
		}
		return o;
	}

	internal static RuntimeAssemblyBuilder InternalDefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, AssemblyLoadContext assemblyLoadContext, IEnumerable<CustomAttributeBuilder> assemblyAttributes)
	{
		lock (s_assemblyBuilderLock)
		{
			return new RuntimeAssemblyBuilder(name, access, assemblyLoadContext, assemblyAttributes);
		}
	}

	protected override ModuleBuilder DefineDynamicModuleCore(string name)
	{
		if (name[0] == '\0')
		{
			throw new ArgumentException(SR.Argument_InvalidName, "name");
		}
		lock (SyncRoot)
		{
			if (_isManifestModuleUsedAsDefinedModule)
			{
				throw new InvalidOperationException(SR.InvalidOperation_NoMultiModuleAssembly);
			}
			_isManifestModuleUsedAsDefinedModule = true;
			return _manifestModuleBuilder;
		}
	}

	internal void CheckTypeNameConflict(string strTypeName, TypeBuilder enclosingType)
	{
		_manifestModuleBuilder.CheckTypeNameConflict(strTypeName, enclosingType);
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return InternalAssembly.GetCustomAttributes(inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return InternalAssembly.GetCustomAttributes(attributeType, inherit);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		return InternalAssembly.IsDefined(attributeType, inherit);
	}

	public override IList<CustomAttributeData> GetCustomAttributesData()
	{
		return InternalAssembly.GetCustomAttributesData();
	}

	public override AssemblyName GetName(bool copiedName)
	{
		return InternalAssembly.GetName(copiedName);
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type GetType(string name, bool throwOnError, bool ignoreCase)
	{
		return InternalAssembly.GetType(name, throwOnError, ignoreCase);
	}

	public override Module GetModule(string name)
	{
		return InternalAssembly.GetModule(name);
	}

	[RequiresUnreferencedCode("Assembly references might be removed")]
	public override AssemblyName[] GetReferencedAssemblies()
	{
		return InternalAssembly.GetReferencedAssemblies();
	}

	public override Module[] GetModules(bool getResourceModules)
	{
		return InternalAssembly.GetModules(getResourceModules);
	}

	public override Module[] GetLoadedModules(bool getResourceModules)
	{
		return InternalAssembly.GetLoadedModules(getResourceModules);
	}

	public override Assembly GetSatelliteAssembly(CultureInfo culture)
	{
		return InternalAssembly.GetSatelliteAssembly(culture, null);
	}

	public override Assembly GetSatelliteAssembly(CultureInfo culture, Version version)
	{
		return InternalAssembly.GetSatelliteAssembly(culture, version);
	}

	protected override ModuleBuilder GetDynamicModuleCore(string name)
	{
		if (_isManifestModuleUsedAsDefinedModule && "RefEmit_InMemoryManifestModule" == name)
		{
			return _manifestModuleBuilder;
		}
		return null;
	}

	protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
	{
		lock (SyncRoot)
		{
			RuntimeTypeBuilder.DefineCustomAttribute(_manifestModuleBuilder, 536870913, _manifestModuleBuilder.GetMethodMetadataToken(con), binaryAttribute);
		}
	}
}
