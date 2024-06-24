using System.ComponentModel;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript;

[SupportedOSPlatform("browser")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class JSMarshalerType
{
	public static JSMarshalerType Void
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public static JSMarshalerType Discard
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public static JSMarshalerType Boolean
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public static JSMarshalerType Byte
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public static JSMarshalerType Char
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public static JSMarshalerType Int16
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public static JSMarshalerType Int32
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public static JSMarshalerType Int52
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public static JSMarshalerType BigInt64
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public static JSMarshalerType Double
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public static JSMarshalerType Single
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public static JSMarshalerType IntPtr
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public static JSMarshalerType JSObject
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public static JSMarshalerType Object
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public static JSMarshalerType String
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public static JSMarshalerType Exception
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public static JSMarshalerType DateTime
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public static JSMarshalerType DateTimeOffset
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	private JSMarshalerType()
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public static JSMarshalerType Nullable(JSMarshalerType primitive)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public static JSMarshalerType Task()
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public static JSMarshalerType Task(JSMarshalerType result)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public static JSMarshalerType Array(JSMarshalerType element)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public static JSMarshalerType ArraySegment(JSMarshalerType element)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public static JSMarshalerType Span(JSMarshalerType element)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public static JSMarshalerType Action()
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public static JSMarshalerType Action(JSMarshalerType arg1)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public static JSMarshalerType Action(JSMarshalerType arg1, JSMarshalerType arg2)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public static JSMarshalerType Action(JSMarshalerType arg1, JSMarshalerType arg2, JSMarshalerType arg3)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public static JSMarshalerType Function(JSMarshalerType result)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public static JSMarshalerType Function(JSMarshalerType arg1, JSMarshalerType result)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public static JSMarshalerType Function(JSMarshalerType arg1, JSMarshalerType arg2, JSMarshalerType result)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public static JSMarshalerType Function(JSMarshalerType arg1, JSMarshalerType arg2, JSMarshalerType arg3, JSMarshalerType result)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}
}
