using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Security.AccessControl;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class PrivilegeNotHeldException : UnauthorizedAccessException
{
	private readonly string _privilegeName;

	public string? PrivilegeName => _privilegeName;

	public PrivilegeNotHeldException()
		: base(System.SR.PrivilegeNotHeld_Default)
	{
	}

	public PrivilegeNotHeldException(string? privilege)
		: base(System.SR.Format(System.SR.PrivilegeNotHeld_Named, privilege))
	{
		_privilegeName = privilege;
	}

	public PrivilegeNotHeldException(string? privilege, Exception? inner)
		: base(System.SR.Format(System.SR.PrivilegeNotHeld_Named, privilege), inner)
	{
		_privilegeName = privilege;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	private PrivilegeNotHeldException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_privilegeName = info.GetString("PrivilegeName");
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("PrivilegeName", _privilegeName, typeof(string));
	}
}
