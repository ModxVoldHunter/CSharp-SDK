using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Threading;

public sealed class CompressedStack : ISerializable
{
	private CompressedStack()
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	public static CompressedStack Capture()
	{
		return GetCompressedStack();
	}

	public CompressedStack CreateCopy()
	{
		return this;
	}

	public static CompressedStack GetCompressedStack()
	{
		return new CompressedStack();
	}

	public static void Run(CompressedStack compressedStack, ContextCallback callback, object? state)
	{
		ArgumentNullException.ThrowIfNull(compressedStack, "compressedStack");
		callback(state);
	}
}
