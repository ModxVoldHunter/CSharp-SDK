using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Quic;
using Microsoft.Win32;

namespace System.Net.Quic;

internal sealed class MsQuicApi
{
	private static readonly Version s_minWindowsVersion;

	private static readonly Version s_minMsQuicVersion;

	private unsafe static readonly delegate* unmanaged[Cdecl]<uint, QUIC_API_TABLE**, int> MsQuicOpenVersion;

	private unsafe static readonly delegate* unmanaged[Cdecl]<QUIC_API_TABLE*, void> MsQuicClose;

	private static readonly Lazy<MsQuicApi> _api;

	public MsQuicSafeHandle Registration { get; }

	public unsafe QUIC_API_TABLE* ApiTable { get; }

	internal static MsQuicApi Api => _api.Value;

	internal static bool IsQuicSupported { get; }

	internal static string MsQuicLibraryVersion { get; }

	internal static string NotSupportedReason { get; }

	internal static bool UsesSChannelBackend { get; }

	internal static bool Tls13ServerMayBeDisabled { get; }

	internal static bool Tls13ClientMayBeDisabled { get; }

	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(MsQuicSafeHandle))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(MsQuicContextSafeHandle))]
	private unsafe MsQuicApi(QUIC_API_TABLE* apiTable)
	{
		ApiTable = apiTable;
		fixed (byte* appName = "System.Net.Quic"u8)
		{
			QUIC_REGISTRATION_CONFIG qUIC_REGISTRATION_CONFIG = new QUIC_REGISTRATION_CONFIG
			{
				AppName = (sbyte*)appName,
				ExecutionProfile = QUIC_EXECUTION_PROFILE.LOW_LATENCY
			};
			Unsafe.SkipInit(out QUIC_HANDLE* handle);
			ThrowHelper.ThrowIfMsQuicError(ApiTable->RegistrationOpen(&qUIC_REGISTRATION_CONFIG, &handle), "RegistrationOpen failed");
			Registration = new MsQuicSafeHandle(handle, apiTable->RegistrationClose, SafeHandleType.Registration);
		}
	}

	unsafe static MsQuicApi()
	{
		s_minWindowsVersion = new Version(10, 0, 20145, 1000);
		s_minMsQuicVersion = new Version(2, 2, 2);
		_api = new Lazy<MsQuicApi>(AllocateMsQuicApi);
		MsQuicLibraryVersion = "unknown";
		bool flag = false;
		if (!Socket.OSSupportsIPv6)
		{
			NotSupportedReason = "OS does not support dual mode sockets.";
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, NotSupportedReason, ".cctor");
			}
			return;
		}
		if (!((!OperatingSystem.IsWindows()) ? (NativeLibrary.TryLoad($"{"msquic.dll"}.{s_minMsQuicVersion.Major}", typeof(MsQuicApi).Assembly, null, out var handle) || NativeLibrary.TryLoad("msquic.dll", typeof(MsQuicApi).Assembly, null, out handle)) : NativeLibrary.TryLoad("msquic.dll", typeof(MsQuicApi).Assembly, DllImportSearchPath.AssemblyDirectory, out handle)))
		{
			NotSupportedReason = $"Unable to load MsQuic library version '{s_minMsQuicVersion.Major}'.";
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, NotSupportedReason, ".cctor");
			}
			return;
		}
		MsQuicOpenVersion = (delegate* unmanaged[Cdecl]<uint, QUIC_API_TABLE**, int>)NativeLibrary.GetExport(handle, "MsQuicOpenVersion");
		MsQuicClose = (delegate* unmanaged[Cdecl]<QUIC_API_TABLE*, void>)NativeLibrary.GetExport(handle, "MsQuicClose");
		if (!TryOpenMsQuic(out var apiTable, out var openStatus))
		{
			NotSupportedReason = $"MsQuicOpenVersion for version {s_minMsQuicVersion.Major} returned {openStatus} status code.";
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, NotSupportedReason, ".cctor");
			}
			return;
		}
		try
		{
			uint num = 16u;
			uint* ptr = stackalloc uint[4];
			int num2 = apiTable->GetParam(null, 16777220u, &num, ptr);
			if (MsQuic.StatusFailed(num2))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(null, FormattableStringFactory.Create("Cannot retrieve {0} from MsQuic library: '{1}'.", "QUIC_PARAM_GLOBAL_LIBRARY_VERSION", num2), ".cctor");
				}
				return;
			}
			Version version = new Version((int)(*ptr), (int)ptr[1], (int)ptr[2], (int)ptr[3]);
			num = 64u;
			sbyte* ptr2 = stackalloc sbyte[64];
			num2 = apiTable->GetParam(null, 16777224u, &num, ptr2);
			if (MsQuic.StatusFailed(num2))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(null, FormattableStringFactory.Create("Cannot retrieve {0} from MsQuic library: '{1}'.", "QUIC_PARAM_GLOBAL_LIBRARY_GIT_HASH", num2), ".cctor");
				}
				return;
			}
			string value = Marshal.PtrToStringUTF8((nint)ptr2);
			MsQuicLibraryVersion = $"{"msquic.dll"} {version} ({value})";
			if (version < s_minMsQuicVersion)
			{
				NotSupportedReason = $"Incompatible MsQuic library version '{version}', expecting higher than '{s_minMsQuicVersion}'.";
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(null, NotSupportedReason, ".cctor");
				}
				return;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, $"Loaded MsQuic library '{MsQuicLibraryVersion}'.", ".cctor");
			}
			QUIC_TLS_PROVIDER qUIC_TLS_PROVIDER = ((!OperatingSystem.IsWindows()) ? QUIC_TLS_PROVIDER.OPENSSL : QUIC_TLS_PROVIDER.SCHANNEL);
			num = 4u;
			apiTable->GetParam(null, 16777226u, &num, &qUIC_TLS_PROVIDER);
			UsesSChannelBackend = qUIC_TLS_PROVIDER == QUIC_TLS_PROVIDER.SCHANNEL;
			if (UsesSChannelBackend)
			{
				if (!IsWindowsVersionSupported())
				{
					NotSupportedReason = $"Current Windows version ({Environment.OSVersion}) is not supported by QUIC. Minimal supported version is {s_minWindowsVersion}.";
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(null, NotSupportedReason, ".cctor");
					}
					return;
				}
				Tls13ServerMayBeDisabled = IsTls13Disabled(isServer: true);
				Tls13ClientMayBeDisabled = IsTls13Disabled(isServer: false);
			}
			IsQuicSupported = true;
		}
		finally
		{
			MsQuicClose(apiTable);
		}
	}

	private unsafe static MsQuicApi AllocateMsQuicApi()
	{
		if (!TryOpenMsQuic(out var apiTable, out var openStatus))
		{
			throw ThrowHelper.GetExceptionForMsQuicStatus(openStatus);
		}
		return new MsQuicApi(apiTable);
	}

	private unsafe static bool TryOpenMsQuic(out QUIC_API_TABLE* apiTable, out int openStatus)
	{
		QUIC_API_TABLE* ptr = null;
		openStatus = MsQuicOpenVersion((uint)s_minMsQuicVersion.Major, &ptr);
		if (MsQuic.StatusFailed(openStatus))
		{
			apiTable = null;
			return false;
		}
		apiTable = ptr;
		return true;
	}

	private static bool IsWindowsVersionSupported()
	{
		return OperatingSystem.IsWindowsVersionAtLeast(s_minWindowsVersion.Major, s_minWindowsVersion.Minor, s_minWindowsVersion.Build, s_minWindowsVersion.Revision);
	}

	private static bool IsTls13Disabled(bool isServer)
	{
		string name = (isServer ? "SYSTEM\\CurrentControlSet\\Control\\SecurityProviders\\SCHANNEL\\Protocols\\TLS 1.3\\Server" : "SYSTEM\\CurrentControlSet\\Control\\SecurityProviders\\SCHANNEL\\Protocols\\TLS 1.3\\Client");
		using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(name);
		if (registryKey == null)
		{
			return false;
		}
		object value = registryKey.GetValue("Enabled");
		if (value is int && (int)value == 0)
		{
			return true;
		}
		if (registryKey.GetValue("DisabledByDefault") is int num && num == 1)
		{
			return true;
		}
		return false;
	}

	public unsafe void SetCallbackHandler(MsQuicSafeHandle handle, void* callback, void* context)
	{
		bool success = false;
		try
		{
			handle.DangerousAddRef(ref success);
			ApiTable->SetCallbackHandler(handle.QuicHandle, callback, context);
		}
		finally
		{
			if (success)
			{
				handle.DangerousRelease();
			}
		}
	}

	public unsafe int SetParam(MsQuicSafeHandle handle, uint param, uint bufferLength, void* buffer)
	{
		bool success = false;
		try
		{
			handle.DangerousAddRef(ref success);
			return ApiTable->SetParam(handle.QuicHandle, param, bufferLength, buffer);
		}
		finally
		{
			if (success)
			{
				handle.DangerousRelease();
			}
		}
	}

	public unsafe int GetParam(MsQuicSafeHandle handle, uint param, uint* bufferLength, void* buffer)
	{
		bool success = false;
		try
		{
			handle.DangerousAddRef(ref success);
			return ApiTable->GetParam(handle.QuicHandle, param, bufferLength, buffer);
		}
		finally
		{
			if (success)
			{
				handle.DangerousRelease();
			}
		}
	}

	public unsafe int ConfigurationOpen(MsQuicSafeHandle registration, QUIC_BUFFER* alpnBuffers, uint alpnBuffersCount, QUIC_SETTINGS* settings, uint settingsSize, void* context, QUIC_HANDLE** configuration)
	{
		bool success = false;
		try
		{
			registration.DangerousAddRef(ref success);
			return ApiTable->ConfigurationOpen(registration.QuicHandle, alpnBuffers, alpnBuffersCount, settings, settingsSize, context, configuration);
		}
		finally
		{
			if (success)
			{
				registration.DangerousRelease();
			}
		}
	}

	public unsafe int ConfigurationLoadCredential(MsQuicSafeHandle configuration, QUIC_CREDENTIAL_CONFIG* config)
	{
		bool success = false;
		try
		{
			configuration.DangerousAddRef(ref success);
			return ApiTable->ConfigurationLoadCredential(configuration.QuicHandle, config);
		}
		finally
		{
			if (success)
			{
				configuration.DangerousRelease();
			}
		}
	}

	public unsafe int ListenerOpen(MsQuicSafeHandle registration, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_LISTENER_EVENT*, int> callback, void* context, QUIC_HANDLE** listener)
	{
		bool success = false;
		try
		{
			registration.DangerousAddRef(ref success);
			return ApiTable->ListenerOpen(registration.QuicHandle, callback, context, listener);
		}
		finally
		{
			if (success)
			{
				registration.DangerousRelease();
			}
		}
	}

	public unsafe int ListenerStart(MsQuicSafeHandle listener, QUIC_BUFFER* alpnBuffers, uint alpnBuffersCount, QuicAddr* localAddress)
	{
		bool success = false;
		try
		{
			listener.DangerousAddRef(ref success);
			return ApiTable->ListenerStart(listener.QuicHandle, alpnBuffers, alpnBuffersCount, localAddress);
		}
		finally
		{
			if (success)
			{
				listener.DangerousRelease();
			}
		}
	}

	public unsafe void ListenerStop(MsQuicSafeHandle listener)
	{
		bool success = false;
		try
		{
			listener.DangerousAddRef(ref success);
			ApiTable->ListenerStop(listener.QuicHandle);
		}
		finally
		{
			if (success)
			{
				listener.DangerousRelease();
			}
		}
	}

	public unsafe int ConnectionOpen(MsQuicSafeHandle registration, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_CONNECTION_EVENT*, int> callback, void* context, QUIC_HANDLE** connection)
	{
		bool success = false;
		try
		{
			registration.DangerousAddRef(ref success);
			return ApiTable->ConnectionOpen(registration.QuicHandle, callback, context, connection);
		}
		finally
		{
			if (success)
			{
				registration.DangerousRelease();
			}
		}
	}

	public unsafe void ConnectionShutdown(MsQuicSafeHandle connection, QUIC_CONNECTION_SHUTDOWN_FLAGS flags, ulong code)
	{
		bool success = false;
		try
		{
			connection.DangerousAddRef(ref success);
			ApiTable->ConnectionShutdown(connection.QuicHandle, flags, code);
		}
		finally
		{
			if (success)
			{
				connection.DangerousRelease();
			}
		}
	}

	public unsafe int ConnectionStart(MsQuicSafeHandle connection, MsQuicSafeHandle configuration, ushort family, sbyte* serverName, ushort serverPort)
	{
		bool success = false;
		bool success2 = false;
		try
		{
			connection.DangerousAddRef(ref success);
			configuration.DangerousAddRef(ref success2);
			return ApiTable->ConnectionStart(connection.QuicHandle, configuration.QuicHandle, family, serverName, serverPort);
		}
		finally
		{
			if (success)
			{
				connection.DangerousRelease();
			}
			if (success2)
			{
				configuration.DangerousRelease();
			}
		}
	}

	public unsafe int ConnectionSetConfiguration(MsQuicSafeHandle connection, MsQuicSafeHandle configuration)
	{
		bool success = false;
		bool success2 = false;
		try
		{
			connection.DangerousAddRef(ref success);
			configuration.DangerousAddRef(ref success2);
			return ApiTable->ConnectionSetConfiguration(connection.QuicHandle, configuration.QuicHandle);
		}
		finally
		{
			if (success)
			{
				connection.DangerousRelease();
			}
			if (success2)
			{
				configuration.DangerousRelease();
			}
		}
	}

	public unsafe int StreamOpen(MsQuicSafeHandle connection, QUIC_STREAM_OPEN_FLAGS flags, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_STREAM_EVENT*, int> callback, void* context, QUIC_HANDLE** stream)
	{
		bool success = false;
		try
		{
			connection.DangerousAddRef(ref success);
			return ApiTable->StreamOpen(connection.QuicHandle, flags, callback, context, stream);
		}
		finally
		{
			if (success)
			{
				connection.DangerousRelease();
			}
		}
	}

	public unsafe int StreamStart(MsQuicSafeHandle stream, QUIC_STREAM_START_FLAGS flags)
	{
		bool success = false;
		try
		{
			stream.DangerousAddRef(ref success);
			return ApiTable->StreamStart(stream.QuicHandle, flags);
		}
		finally
		{
			if (success)
			{
				stream.DangerousRelease();
			}
		}
	}

	public unsafe int StreamShutdown(MsQuicSafeHandle stream, QUIC_STREAM_SHUTDOWN_FLAGS flags, ulong code)
	{
		bool success = false;
		try
		{
			stream.DangerousAddRef(ref success);
			return ApiTable->StreamShutdown(stream.QuicHandle, flags, code);
		}
		finally
		{
			if (success)
			{
				stream.DangerousRelease();
			}
		}
	}

	public unsafe int StreamSend(MsQuicSafeHandle stream, QUIC_BUFFER* buffers, uint buffersCount, QUIC_SEND_FLAGS flags, void* context)
	{
		bool success = false;
		try
		{
			stream.DangerousAddRef(ref success);
			return ApiTable->StreamSend(stream.QuicHandle, buffers, buffersCount, flags, context);
		}
		finally
		{
			if (success)
			{
				stream.DangerousRelease();
			}
		}
	}

	public unsafe int StreamReceiveSetEnabled(MsQuicSafeHandle stream, byte enabled)
	{
		bool success = false;
		try
		{
			stream.DangerousAddRef(ref success);
			return ApiTable->StreamReceiveSetEnabled(stream.QuicHandle, enabled);
		}
		finally
		{
			if (success)
			{
				stream.DangerousRelease();
			}
		}
	}
}
