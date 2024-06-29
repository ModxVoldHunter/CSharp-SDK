using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Security.Principal;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class IdentityNotMappedException : SystemException
{
	private IdentityReferenceCollection _unmappedIdentities;

	public IdentityReferenceCollection UnmappedIdentities => _unmappedIdentities ?? (_unmappedIdentities = new IdentityReferenceCollection());

	public IdentityNotMappedException()
		: base(System.SR.IdentityReference_IdentityNotMapped)
	{
	}

	public IdentityNotMappedException(string? message)
		: base(message)
	{
	}

	public IdentityNotMappedException(string? message, Exception? inner)
		: base(message, inner)
	{
	}

	internal IdentityNotMappedException(string message, IdentityReferenceCollection unmappedIdentities)
		: this(message)
	{
		_unmappedIdentities = unmappedIdentities;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	private IdentityNotMappedException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		base.GetObjectData(serializationInfo, streamingContext);
	}
}
