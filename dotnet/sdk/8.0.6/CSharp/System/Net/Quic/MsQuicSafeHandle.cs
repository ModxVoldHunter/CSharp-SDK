using System.Runtime.InteropServices;
using Microsoft.Quic;

namespace System.Net.Quic;

internal class MsQuicSafeHandle : SafeHandle
{
	private static readonly string[] s_typeName = new string[5] { " reg", "cnfg", "list", "conn", "strm" };

	private unsafe readonly delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void> _releaseAction;

	private string _traceId;

	private readonly SafeHandleType _type;

	public override bool IsInvalid => handle == IntPtr.Zero;

	public unsafe QUIC_HANDLE* QuicHandle => (QUIC_HANDLE*)DangerousGetHandle();

	public unsafe MsQuicSafeHandle(QUIC_HANDLE* handle, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void> releaseAction, SafeHandleType safeHandleType)
		: base((nint)handle, ownsHandle: true)
	{
		_releaseAction = releaseAction;
		_type = safeHandleType;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"{this} MsQuicSafeHandle created", ".ctor");
		}
	}

	public unsafe MsQuicSafeHandle(QUIC_HANDLE* handle, SafeHandleType safeHandleType)
		: this(handle, safeHandleType switch
		{
			SafeHandleType.Registration => MsQuicApi.Api.ApiTable->RegistrationClose, 
			SafeHandleType.Configuration => MsQuicApi.Api.ApiTable->ConfigurationClose, 
			SafeHandleType.Listener => MsQuicApi.Api.ApiTable->ListenerClose, 
			SafeHandleType.Connection => MsQuicApi.Api.ApiTable->ConnectionClose, 
			SafeHandleType.Stream => MsQuicApi.Api.ApiTable->StreamClose, 
			_ => throw new ArgumentException($"Unexpected value: {safeHandleType}", "safeHandleType"), 
		}, safeHandleType)
	{
	}

	protected unsafe override bool ReleaseHandle()
	{
		QUIC_HANDLE* quicHandle = QuicHandle;
		SetHandle(IntPtr.Zero);
		_releaseAction(quicHandle);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"{this} MsQuicSafeHandle released", "ReleaseHandle");
		}
		return true;
	}

	public override string ToString()
	{
		return _traceId ?? (_traceId = $"[{s_typeName[(int)_type]}][0x{DangerousGetHandle():X11}]");
	}
}
