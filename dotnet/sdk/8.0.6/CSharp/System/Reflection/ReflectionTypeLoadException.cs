using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace System.Reflection;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class ReflectionTypeLoadException : SystemException
{
	public Type?[] Types { get; }

	public Exception?[] LoaderExceptions { get; }

	public override string Message => CreateString(isMessage: true);

	public ReflectionTypeLoadException(Type?[]? classes, Exception?[]? exceptions)
		: this(classes, exceptions, null)
	{
	}

	public ReflectionTypeLoadException(Type?[]? classes, Exception?[]? exceptions, string? message)
		: base(message)
	{
		Types = classes ?? Type.EmptyTypes;
		LoaderExceptions = exceptions ?? Array.Empty<Exception>();
		base.HResult = -2146232830;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	private ReflectionTypeLoadException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		Types = Type.EmptyTypes;
		LoaderExceptions = ((Exception[])info.GetValue("Exceptions", typeof(Exception[]))) ?? Array.Empty<Exception>();
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("Types", null, typeof(Type[]));
		info.AddValue("Exceptions", LoaderExceptions, typeof(Exception[]));
	}

	public override string ToString()
	{
		return CreateString(isMessage: false);
	}

	private string CreateString(bool isMessage)
	{
		string text = (isMessage ? base.Message : base.ToString());
		Exception[] loaderExceptions = LoaderExceptions;
		if (loaderExceptions.Length == 0)
		{
			return text;
		}
		StringBuilder stringBuilder = new StringBuilder(text);
		Exception[] array = loaderExceptions;
		foreach (Exception ex in array)
		{
			if (ex != null)
			{
				stringBuilder.AppendLine().Append(isMessage ? ex.Message : ex.ToString());
			}
		}
		return stringBuilder.ToString();
	}
}
