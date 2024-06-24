using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.IO;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class FileLoadException : IOException
{
	public override string Message => _message ?? (_message = FormatFileLoadExceptionMessage(FileName, base.HResult));

	public string? FileName { get; }

	public string? FusionLog { get; }

	private FileLoadException(string fileName, int hResult)
		: base(null)
	{
		base.HResult = hResult;
		FileName = fileName;
		_message = FormatFileLoadExceptionMessage(FileName, base.HResult);
	}

	internal static string FormatFileLoadExceptionMessage(string fileName, int hResult)
	{
		string s = null;
		GetFileLoadExceptionMessage(hResult, new StringHandleOnStack(ref s));
		string s2 = null;
		if (hResult == -2147024703)
		{
			s2 = SR.Arg_BadImageFormatException;
		}
		else
		{
			GetMessageForHR(hResult, new StringHandleOnStack(ref s2));
		}
		return string.Format(s, fileName, s2);
	}

	[DllImport("QCall", ExactSpelling = true)]
	[LibraryImport("QCall")]
	private static extern void GetFileLoadExceptionMessage(int hResult, StringHandleOnStack retString);

	[DllImport("QCall", EntryPoint = "FileLoadException_GetMessageForHR", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "FileLoadException_GetMessageForHR")]
	private static extern void GetMessageForHR(int hresult, StringHandleOnStack retString);

	public FileLoadException()
		: base(SR.IO_FileLoad)
	{
		base.HResult = -2146232799;
	}

	public FileLoadException(string? message)
		: base(message)
	{
		base.HResult = -2146232799;
	}

	public FileLoadException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146232799;
	}

	public FileLoadException(string? message, string? fileName)
		: base(message)
	{
		base.HResult = -2146232799;
		FileName = fileName;
	}

	public FileLoadException(string? message, string? fileName, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146232799;
		FileName = fileName;
	}

	public override string ToString()
	{
		string text = GetType().ToString() + ": " + Message;
		if (!string.IsNullOrEmpty(FileName))
		{
			text = text + "\r\n" + SR.Format(SR.IO_FileName_Name, FileName);
		}
		if (base.InnerException != null)
		{
			text = text + "\r\n ---> " + base.InnerException.ToString();
		}
		if (StackTrace != null)
		{
			text = text + "\r\n" + StackTrace;
		}
		if (FusionLog != null)
		{
			if (text == null)
			{
				text = " ";
			}
			text = text + "\r\n\r\n" + FusionLog;
		}
		return text;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected FileLoadException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		FileName = info.GetString("FileLoad_FileName");
		FusionLog = info.GetString("FileLoad_FusionLog");
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("FileLoad_FileName", FileName, typeof(string));
		info.AddValue("FileLoad_FusionLog", FusionLog, typeof(string));
	}
}
