using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling;

[CLSCompliant(false)]
public class StrategyBasedComWrappers : ComWrappers
{
	internal static StrategyBasedComWrappers DefaultMarshallingInstance { get; } = new StrategyBasedComWrappers();


	public static IIUnknownInterfaceDetailsStrategy DefaultIUnknownInterfaceDetailsStrategy { get; } = System.Runtime.InteropServices.Marshalling.DefaultIUnknownInterfaceDetailsStrategy.Instance;


	public static IIUnknownStrategy DefaultIUnknownStrategy { get; } = System.Runtime.InteropServices.Marshalling.FreeThreadedStrategy.Instance;


	protected static IIUnknownCacheStrategy CreateDefaultCacheStrategy()
	{
		return new System.Runtime.InteropServices.Marshalling.DefaultCaching();
	}

	protected virtual IIUnknownInterfaceDetailsStrategy GetOrCreateInterfaceDetailsStrategy()
	{
		if (OperatingSystem.IsWindows() && RuntimeFeature.IsDynamicCodeSupported && ComObject.BuiltInComSupported && ComObject.ComImportInteropEnabled)
		{
			return GetInteropStrategy();
		}
		return DefaultIUnknownInterfaceDetailsStrategy;
		[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "The usage is guarded, but the analyzer and the trimmer don't understand it.")]
		[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "The opt-in feature is documented to not work in trimming scenarios.")]
		static IIUnknownInterfaceDetailsStrategy GetInteropStrategy()
		{
			return System.Runtime.InteropServices.Marshalling.ComImportInteropInterfaceDetailsStrategy.Instance;
		}
	}

	protected virtual IIUnknownStrategy GetOrCreateIUnknownStrategy()
	{
		return DefaultIUnknownStrategy;
	}

	protected virtual IIUnknownCacheStrategy CreateCacheStrategy()
	{
		return CreateDefaultCacheStrategy();
	}

	protected unsafe sealed override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
	{
		IComExposedDetails comExposedTypeDetails = GetOrCreateInterfaceDetailsStrategy().GetComExposedTypeDetails(obj.GetType().TypeHandle);
		if (comExposedTypeDetails != null)
		{
			return comExposedTypeDetails.GetComInterfaceEntries(out count);
		}
		count = 0;
		return null;
	}

	protected unsafe sealed override object CreateObject(nint externalComObject, CreateObjectFlags flags)
	{
		if (flags.HasFlag(CreateObjectFlags.TrackerObject) || flags.HasFlag(CreateObjectFlags.Aggregation))
		{
			throw new NotSupportedException();
		}
		return new ComObject(GetOrCreateInterfaceDetailsStrategy(), GetOrCreateIUnknownStrategy(), CreateCacheStrategy(), (void*)externalComObject)
		{
			UniqueInstance = flags.HasFlag(CreateObjectFlags.UniqueInstance)
		};
	}

	protected sealed override void ReleaseObjects(IEnumerable objects)
	{
		throw new NotImplementedException();
	}
}
