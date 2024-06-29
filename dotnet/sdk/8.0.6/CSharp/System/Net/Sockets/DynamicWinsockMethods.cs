using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets;

internal sealed class DynamicWinsockMethods
{
	private readonly struct SocketDelegateHelper
	{
		private readonly nint _target;

		public SocketDelegateHelper(nint target)
		{
			_target = target;
		}

		internal unsafe bool AcceptEx(SafeSocketHandle listenSocketHandle, SafeSocketHandle acceptSocketHandle, nint buffer, int len, int localAddressLength, int remoteAddressLength, out int bytesReceived, NativeOverlapped* overlapped)
		{
			nint num = 0;
			nint num2 = 0;
			bytesReceived = 0;
			int num3 = 0;
			bool success = false;
			bool success2 = false;
			try
			{
				listenSocketHandle.DangerousAddRef(ref success);
				num = listenSocketHandle.DangerousGetHandle();
				acceptSocketHandle.DangerousAddRef(ref success2);
				num2 = acceptSocketHandle.DangerousGetHandle();
				fixed (int* ptr = &bytesReceived)
				{
					num3 = ((delegate* unmanaged<nint, nint, nint, int, int, int, int*, NativeOverlapped*, int>)_target)(num, num2, buffer, len, localAddressLength, remoteAddressLength, ptr, overlapped);
				}
				Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
				return num3 != 0;
			}
			finally
			{
				if (success)
				{
					listenSocketHandle.DangerousRelease();
				}
				if (success2)
				{
					acceptSocketHandle.DangerousRelease();
				}
			}
		}

		internal unsafe void GetAcceptExSockaddrs(nint buffer, int receiveDataLength, int localAddressLength, int remoteAddressLength, out nint localSocketAddress, out int localSocketAddressLength, out nint remoteSocketAddress, out int remoteSocketAddressLength)
		{
			localSocketAddress = 0;
			localSocketAddressLength = 0;
			remoteSocketAddress = 0;
			remoteSocketAddressLength = 0;
			fixed (nint* ptr = &localSocketAddress)
			{
				fixed (int* ptr2 = &localSocketAddressLength)
				{
					fixed (nint* ptr3 = &remoteSocketAddress)
					{
						fixed (int* ptr4 = &remoteSocketAddressLength)
						{
							((delegate* unmanaged<nint, int, int, int, nint*, int*, nint*, int*, void>)_target)(buffer, receiveDataLength, localAddressLength, remoteAddressLength, ptr, ptr2, ptr3, ptr4);
						}
					}
				}
			}
			Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		}

		internal unsafe bool ConnectEx(SafeSocketHandle socketHandle, ReadOnlySpan<byte> socketAddress, nint buffer, int dataLength, out int bytesSent, NativeOverlapped* overlapped)
		{
			nint num = 0;
			bytesSent = 0;
			int num2 = 0;
			bool success = false;
			try
			{
				socketHandle.DangerousAddRef(ref success);
				num = socketHandle.DangerousGetHandle();
				fixed (int* ptr3 = &bytesSent)
				{
					fixed (byte* ptr = &MemoryMarshal.GetReference(socketAddress))
					{
						void* ptr2 = ptr;
						num2 = ((delegate* unmanaged<nint, void*, int, nint, int, int*, NativeOverlapped*, int>)_target)(num, ptr2, socketAddress.Length, buffer, dataLength, ptr3, overlapped);
					}
				}
				Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
				return num2 != 0;
			}
			finally
			{
				if (success)
				{
					socketHandle.DangerousRelease();
				}
			}
		}

		internal unsafe bool DisconnectEx(SafeSocketHandle socketHandle, NativeOverlapped* overlapped, int flags, int reserved)
		{
			bool success = false;
			try
			{
				socketHandle.DangerousAddRef(ref success);
				nint num = socketHandle.DangerousGetHandle();
				int num2 = ((delegate* unmanaged<nint, NativeOverlapped*, int, int, int>)_target)(num, overlapped, flags, reserved);
				Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
				return num2 != 0;
			}
			finally
			{
				if (success)
				{
					socketHandle.DangerousRelease();
				}
			}
		}

		internal unsafe SocketError WSARecvMsg(SafeSocketHandle socketHandle, nint msg, out int bytesTransferred, NativeOverlapped* overlapped, nint completionRoutine)
		{
			nint num = 0;
			bytesTransferred = 0;
			bool success = false;
			try
			{
				socketHandle.DangerousAddRef(ref success);
				num = socketHandle.DangerousGetHandle();
				SocketError result;
				fixed (int* ptr = &bytesTransferred)
				{
					result = ((delegate* unmanaged<nint, nint, int*, NativeOverlapped*, nint, SocketError>)_target)(num, msg, ptr, overlapped, completionRoutine);
				}
				Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
				return result;
			}
			finally
			{
				if (success)
				{
					socketHandle.DangerousRelease();
				}
			}
		}

		internal unsafe bool TransmitPackets(SafeSocketHandle socketHandle, nint packetArray, int elementCount, int sendSize, NativeOverlapped* overlapped, TransmitFileOptions flags)
		{
			bool success = false;
			try
			{
				socketHandle.DangerousAddRef(ref success);
				nint num = socketHandle.DangerousGetHandle();
				int num2 = ((delegate* unmanaged<nint, nint, int, int, NativeOverlapped*, TransmitFileOptions, int>)_target)(num, packetArray, elementCount, sendSize, overlapped, flags);
				Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
				return num2 != 0;
			}
			finally
			{
				if (success)
				{
					socketHandle.DangerousRelease();
				}
			}
		}
	}

	private static readonly List<DynamicWinsockMethods> s_methodTable = new List<DynamicWinsockMethods>();

	private readonly AddressFamily _addressFamily;

	private readonly SocketType _socketType;

	private readonly ProtocolType _protocolType;

	private AcceptExDelegate _acceptEx;

	private GetAcceptExSockaddrsDelegate _getAcceptExSockaddrs;

	private ConnectExDelegate _connectEx;

	private TransmitPacketsDelegate _transmitPackets;

	private DisconnectExDelegate _disconnectEx;

	private WSARecvMsgDelegate _recvMsg;

	public static DynamicWinsockMethods GetMethods(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
	{
		lock (s_methodTable)
		{
			DynamicWinsockMethods dynamicWinsockMethods;
			for (int i = 0; i < s_methodTable.Count; i++)
			{
				dynamicWinsockMethods = s_methodTable[i];
				if (dynamicWinsockMethods._addressFamily == addressFamily && dynamicWinsockMethods._socketType == socketType && dynamicWinsockMethods._protocolType == protocolType)
				{
					return dynamicWinsockMethods;
				}
			}
			dynamicWinsockMethods = new DynamicWinsockMethods(addressFamily, socketType, protocolType);
			s_methodTable.Add(dynamicWinsockMethods);
			return dynamicWinsockMethods;
		}
	}

	private DynamicWinsockMethods(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
	{
		_addressFamily = addressFamily;
		_socketType = socketType;
		_protocolType = protocolType;
	}

	private unsafe static T CreateDelegate<T>(Func<nint, T> functionPointerWrapper, [NotNull] ref T cache, SafeSocketHandle socketHandle, string guidString) where T : Delegate
	{
		Guid guid = new Guid(guidString);
		if (global::Interop.Winsock.WSAIoctl(socketHandle, -939524090, ref guid, sizeof(Guid), out var funcPtr, sizeof(nint), out var _, IntPtr.Zero, IntPtr.Zero) != 0)
		{
			throw new SocketException();
		}
		Interlocked.CompareExchange(ref cache, functionPointerWrapper(funcPtr), null);
		return cache;
	}

	internal unsafe AcceptExDelegate GetAcceptExDelegate(SafeSocketHandle socketHandle)
	{
		return _acceptEx ?? CreateDelegate<AcceptExDelegate>((nint ptr) => new SocketDelegateHelper(ptr).AcceptEx, ref _acceptEx, socketHandle, "b5367df1cbac11cf95ca00805f48a192");
	}

	internal GetAcceptExSockaddrsDelegate GetGetAcceptExSockaddrsDelegate(SafeSocketHandle socketHandle)
	{
		return _getAcceptExSockaddrs ?? CreateDelegate<GetAcceptExSockaddrsDelegate>((nint ptr) => new SocketDelegateHelper(ptr).GetAcceptExSockaddrs, ref _getAcceptExSockaddrs, socketHandle, "b5367df2cbac11cf95ca00805f48a192");
	}

	internal unsafe ConnectExDelegate GetConnectExDelegate(SafeSocketHandle socketHandle)
	{
		return _connectEx ?? CreateDelegate<ConnectExDelegate>((nint ptr) => new SocketDelegateHelper(ptr).ConnectEx, ref _connectEx, socketHandle, "25a207b9ddf346608ee976e58c74063e");
	}

	internal unsafe DisconnectExDelegate GetDisconnectExDelegate(SafeSocketHandle socketHandle)
	{
		return _disconnectEx ?? CreateDelegate<DisconnectExDelegate>((nint ptr) => new SocketDelegateHelper(ptr).DisconnectEx, ref _disconnectEx, socketHandle, "7fda2e118630436fa031f536a6eec157");
	}

	internal unsafe WSARecvMsgDelegate GetWSARecvMsgDelegate(SafeSocketHandle socketHandle)
	{
		return _recvMsg ?? CreateDelegate<WSARecvMsgDelegate>((nint ptr) => new SocketDelegateHelper(ptr).WSARecvMsg, ref _recvMsg, socketHandle, "f689d7c86f1f436b8a53e54fe351c322");
	}

	internal unsafe TransmitPacketsDelegate GetTransmitPacketsDelegate(SafeSocketHandle socketHandle)
	{
		return _transmitPackets ?? CreateDelegate<TransmitPacketsDelegate>((nint ptr) => new SocketDelegateHelper(ptr).TransmitPackets, ref _transmitPackets, socketHandle, "d9689da01f9011d3997100c04f68c876");
	}
}
