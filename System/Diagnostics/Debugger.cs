using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Diagnostics;

public static class Debugger
{
	private sealed class CrossThreadDependencyNotification : ICustomDebuggerNotification
	{
	}

	public static readonly string? DefaultCategory;

	public static extern bool IsAttached
	{
		[MethodImpl(MethodImplOptions.InternalCall)]
		get;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Break()
	{
		BreakInternal();
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void BreakInternal();

	public static bool Launch()
	{
		if (!IsAttached)
		{
			return LaunchInternal();
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void NotifyOfCrossThreadDependencySlow()
	{
		CustomNotification(new CrossThreadDependencyNotification());
	}

	public static void NotifyOfCrossThreadDependency()
	{
		if (IsAttached)
		{
			NotifyOfCrossThreadDependencySlow();
		}
	}

	[LibraryImport("QCall", EntryPoint = "DebugDebugger_Launch")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static bool LaunchInternal()
	{
		int num = __PInvoke();
		return num != 0;
		[DllImport("QCall", EntryPoint = "DebugDebugger_Launch", ExactSpelling = true)]
		static extern int __PInvoke();
	}

	public static void Log(int level, string? category, string? message)
	{
		LogInternal(level, category, message);
	}

	[LibraryImport("QCall", EntryPoint = "DebugDebugger_Log", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static void LogInternal(int level, string category, string message)
	{
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(message))
		{
			void* _message_native = ptr;
			fixed (char* ptr2 = &Utf16StringMarshaller.GetPinnableReference(category))
			{
				void* _category_native = ptr2;
				__PInvoke(level, (ushort*)_category_native, (ushort*)_message_native);
			}
		}
		[DllImport("QCall", EntryPoint = "DebugDebugger_Log", ExactSpelling = true)]
		static extern unsafe void __PInvoke(int __level_native, ushort* __category_native, ushort* __message_native);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern bool IsLogging();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void CustomNotification(ICustomDebuggerNotification data);
}
