using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics;

public static class StackFrameExtensions
{
	public static bool HasNativeImage(this StackFrame stackFrame)
	{
		return stackFrame.GetNativeImageBase() != IntPtr.Zero;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "StackFrame.GetMethod is used to establish if method is available.")]
	public static bool HasMethod(this StackFrame stackFrame)
	{
		return stackFrame.GetMethod() != null;
	}

	public static bool HasILOffset(this StackFrame stackFrame)
	{
		return stackFrame.GetILOffset() != -1;
	}

	public static bool HasSource(this StackFrame stackFrame)
	{
		return stackFrame.GetFileName() != null;
	}

	public static nint GetNativeIP(this StackFrame stackFrame)
	{
		return IntPtr.Zero;
	}

	public static nint GetNativeImageBase(this StackFrame stackFrame)
	{
		return IntPtr.Zero;
	}
}
