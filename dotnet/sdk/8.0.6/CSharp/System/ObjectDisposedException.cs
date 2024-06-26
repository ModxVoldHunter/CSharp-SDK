using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ObjectDisposedException : InvalidOperationException
{
	private readonly string _objectName;

	public override string Message
	{
		get
		{
			string objectName = ObjectName;
			if (string.IsNullOrEmpty(objectName))
			{
				return base.Message;
			}
			string text = SR.Format(SR.ObjectDisposed_ObjectName_Name, objectName);
			return base.Message + "\r\n" + text;
		}
	}

	public string ObjectName => _objectName ?? string.Empty;

	private ObjectDisposedException()
		: this(null, SR.ObjectDisposed_Generic)
	{
	}

	public ObjectDisposedException(string? objectName)
		: this(objectName, SR.ObjectDisposed_Generic)
	{
	}

	public ObjectDisposedException(string? objectName, string? message)
		: base(message)
	{
		base.HResult = -2146232798;
		_objectName = objectName;
	}

	public ObjectDisposedException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146232798;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected ObjectDisposedException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_objectName = info.GetString("ObjectName");
	}

	[StackTraceHidden]
	public static void ThrowIf([DoesNotReturnIf(true)] bool condition, object instance)
	{
		if (condition)
		{
			ThrowHelper.ThrowObjectDisposedException(instance);
		}
	}

	[StackTraceHidden]
	public static void ThrowIf([DoesNotReturnIf(true)] bool condition, Type type)
	{
		if (condition)
		{
			ThrowHelper.ThrowObjectDisposedException(type);
		}
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("ObjectName", ObjectName, typeof(string));
	}
}
