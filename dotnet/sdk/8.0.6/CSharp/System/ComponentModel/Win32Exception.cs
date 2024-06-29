using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace System.ComponentModel;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class Win32Exception : ExternalException
{
	public int NativeErrorCode { get; }

	public Win32Exception()
		: this(Marshal.GetLastPInvokeError())
	{
	}

	public Win32Exception(int error)
		: this(error, Marshal.GetPInvokeErrorMessage(error))
	{
	}

	public Win32Exception(int error, string? message)
		: base(message)
	{
		NativeErrorCode = error;
	}

	public Win32Exception(string? message)
		: this(Marshal.GetLastPInvokeError(), message)
	{
	}

	public Win32Exception(string? message, Exception? innerException)
		: base(message, innerException)
	{
		NativeErrorCode = Marshal.GetLastPInvokeError();
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected Win32Exception(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		NativeErrorCode = info.GetInt32("NativeErrorCode");
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("NativeErrorCode", NativeErrorCode);
	}

	public override string ToString()
	{
		if (NativeErrorCode == 0 || NativeErrorCode == base.HResult)
		{
			return base.ToString();
		}
		string message = Message;
		string value = GetType().ToString();
		StringBuilder stringBuilder = new StringBuilder(value);
		string value2 = ((NativeErrorCode < 0) ? $"0x{NativeErrorCode:X8}" : NativeErrorCode.ToString(CultureInfo.InvariantCulture));
		if (base.HResult == -2147467259)
		{
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder2);
			handler.AppendLiteral(" (");
			handler.AppendFormatted(value2);
			handler.AppendLiteral(")");
			stringBuilder3.Append(ref handler);
		}
		else
		{
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder2);
			handler.AppendLiteral(" (");
			handler.AppendFormatted(base.HResult, "X8");
			handler.AppendLiteral(", ");
			handler.AppendFormatted(value2);
			handler.AppendLiteral(")");
			stringBuilder4.Append(ref handler);
		}
		if (!string.IsNullOrEmpty(message))
		{
			stringBuilder.Append(": ");
			stringBuilder.Append(message);
		}
		Exception innerException = base.InnerException;
		if (innerException != null)
		{
			stringBuilder.Append(" ---> ");
			stringBuilder.Append(innerException.ToString());
		}
		string stackTrace = StackTrace;
		if (stackTrace != null)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(stackTrace);
		}
		return stringBuilder.ToString();
	}
}
