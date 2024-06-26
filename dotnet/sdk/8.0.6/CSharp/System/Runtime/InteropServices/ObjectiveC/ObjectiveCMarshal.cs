using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.ObjectiveC;

[SupportedOSPlatform("macos")]
[CLSCompliant(false)]
public static class ObjectiveCMarshal
{
	public unsafe delegate delegate* unmanaged<nint, void> UnhandledExceptionPropagationHandler(Exception exception, RuntimeMethodHandle lastMethod, out nint context);

	public enum MessageSendFunction
	{
		MsgSend,
		MsgSendFpret,
		MsgSendStret,
		MsgSendSuper,
		MsgSendSuperStret
	}

	public unsafe static void Initialize(delegate* unmanaged<void> beginEndCallback, delegate* unmanaged<nint, int> isReferencedCallback, delegate* unmanaged<nint, void> trackedObjectEnteredFinalization, UnhandledExceptionPropagationHandler unhandledExceptionPropagationHandler)
	{
		throw new PlatformNotSupportedException();
	}

	public static GCHandle CreateReferenceTrackingHandle(object obj, out Span<nint> taggedMemory)
	{
		throw new PlatformNotSupportedException();
	}

	public static void SetMessageSendCallback(MessageSendFunction msgSendFunction, nint func)
	{
		throw new PlatformNotSupportedException();
	}

	public static void SetMessageSendPendingException(Exception? exception)
	{
		throw new PlatformNotSupportedException();
	}
}
