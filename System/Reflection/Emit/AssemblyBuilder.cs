using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace System.Reflection.Emit;

public abstract class AssemblyBuilder : Assembly
{
	private sealed class ForceAllowDynamicCodeScope : IDisposable
	{
		private readonly bool _previous;

		public ForceAllowDynamicCodeScope()
		{
			_previous = t_allowDynamicCode;
			t_allowDynamicCode = true;
		}

		public void Dispose()
		{
			t_allowDynamicCode = _previous;
		}
	}

	[ThreadStatic]
	private static bool t_allowDynamicCode;

	[Obsolete("Assembly.CodeBase and Assembly.EscapedCodeBase are only included for .NET Framework compatibility. Use Assembly.Location instead.", DiagnosticId = "SYSLIB0012", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public override string? CodeBase
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
		}
	}

	public override string Location => string.Empty;

	public override MethodInfo? EntryPoint => null;

	public override bool IsDynamic => true;

	[RequiresDynamicCode("Defining a dynamic assembly requires dynamic code.")]
	public static AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access)
	{
		return DefineDynamicAssembly(name, access, null, Assembly.GetCallingAssembly());
	}

	[RequiresDynamicCode("Defining a dynamic assembly requires dynamic code.")]
	public static AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, IEnumerable<CustomAttributeBuilder>? assemblyAttributes)
	{
		return DefineDynamicAssembly(name, access, assemblyAttributes, Assembly.GetCallingAssembly());
	}

	private static RuntimeAssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, IEnumerable<CustomAttributeBuilder> assemblyAttributes, Assembly callingAssembly)
	{
		ArgumentNullException.ThrowIfNull(name, "name");
		if (access != AssemblyBuilderAccess.Run && access != AssemblyBuilderAccess.RunAndCollect)
		{
			throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)access), "access");
		}
		if (callingAssembly == null)
		{
			throw new InvalidOperationException();
		}
		EnsureDynamicCodeSupported();
		AssemblyLoadContext assemblyLoadContext = AssemblyLoadContext.CurrentContextualReflectionContext ?? AssemblyLoadContext.GetLoadContext(callingAssembly);
		if (assemblyLoadContext == null)
		{
			throw new InvalidOperationException();
		}
		return new RuntimeAssemblyBuilder(name, access, assemblyLoadContext, assemblyAttributes);
	}

	public ModuleBuilder DefineDynamicModule(string name)
	{
		ArgumentException.ThrowIfNullOrEmpty(name, "name");
		return DefineDynamicModuleCore(name);
	}

	protected abstract ModuleBuilder DefineDynamicModuleCore(string name);

	public ModuleBuilder? GetDynamicModule(string name)
	{
		ArgumentException.ThrowIfNullOrEmpty(name, "name");
		return GetDynamicModuleCore(name);
	}

	protected abstract ModuleBuilder? GetDynamicModuleCore(string name);

	public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
	{
		ArgumentNullException.ThrowIfNull(con, "con");
		ArgumentNullException.ThrowIfNull(binaryAttribute, "binaryAttribute");
		SetCustomAttributeCore(con, binaryAttribute);
	}

	protected abstract void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute);

	public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
	{
		ArgumentNullException.ThrowIfNull(customBuilder, "customBuilder");
		SetCustomAttributeCore(customBuilder.Ctor, customBuilder.Data);
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type[] GetExportedTypes()
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public override FileStream GetFile(string name)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public override FileStream[] GetFiles(bool getResourceModules)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	public override ManifestResourceInfo? GetManifestResourceInfo(string resourceName)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	public override string[] GetManifestResourceNames()
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	public override Stream? GetManifestResourceStream(string name)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	public override Stream? GetManifestResourceStream(Type type, string name)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	internal static void EnsureDynamicCodeSupported()
	{
		if (!RuntimeFeature.IsDynamicCodeSupported && !t_allowDynamicCode)
		{
			ThrowDynamicCodeNotSupported();
		}
	}

	internal static IDisposable ForceAllowDynamicCode()
	{
		return new ForceAllowDynamicCodeScope();
	}

	private static void ThrowDynamicCodeNotSupported()
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_ReflectionEmit);
	}
}
