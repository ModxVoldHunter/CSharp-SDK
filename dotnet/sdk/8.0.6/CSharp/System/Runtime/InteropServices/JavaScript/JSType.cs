using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript;

[SupportedOSPlatform("browser")]
public abstract class JSType
{
	public sealed class Void : JSType
	{
		internal Void()
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public sealed class Discard : JSType
	{
		internal Discard()
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public sealed class Boolean : JSType
	{
		internal Boolean()
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public sealed class Number : JSType
	{
		internal Number()
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public sealed class BigInt : JSType
	{
		internal BigInt()
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public sealed class Date : JSType
	{
		internal Date()
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public sealed class String : JSType
	{
		internal String()
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public sealed class Object : JSType
	{
		internal Object()
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public sealed class Error : JSType
	{
		internal Error()
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public sealed class MemoryView : JSType
	{
		internal MemoryView()
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public sealed class Array<T> : JSType where T : JSType
	{
		internal Array()
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public sealed class Promise<T> : JSType where T : JSType
	{
		internal Promise()
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public sealed class Function : JSType
	{
		internal Function()
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public sealed class Function<T> : JSType where T : JSType
	{
		internal Function()
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public sealed class Function<T1, T2> : JSType where T1 : JSType where T2 : JSType
	{
		internal Function()
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public sealed class Function<T1, T2, T3> : JSType where T1 : JSType where T2 : JSType where T3 : JSType
	{
		internal Function()
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public sealed class Function<T1, T2, T3, T4> : JSType where T1 : JSType where T2 : JSType where T3 : JSType where T4 : JSType
	{
		internal Function()
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public sealed class Any : JSType
	{
		internal Any()
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	internal JSType()
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}
}
