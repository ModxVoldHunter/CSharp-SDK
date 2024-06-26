using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Sockets;

internal static class SocketPal
{
	public static readonly int MaximumAddressSize = UnixDomainSocketEndPoint.MaxAddressSize;

	private static void MicrosecondsToTimeValue(long microseconds, ref global::Interop.Winsock.TimeValue socketTime)
	{
		socketTime.Seconds = (int)(microseconds / 1000000);
		socketTime.Microseconds = (int)(microseconds % 1000000);
	}

	public static SocketError GetLastSocketError()
	{
		return (SocketError)Marshal.GetLastPInvokeError();
	}

	public static SocketError CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, out SafeSocketHandle socket)
	{
		global::Interop.Winsock.EnsureInitialized();
		socket = new SafeSocketHandle();
		Marshal.InitHandle(socket, global::Interop.Winsock.WSASocketW(addressFamily, socketType, protocolType, IntPtr.Zero, 0u, global::Interop.Winsock.SocketConstructorFlags.WSA_FLAG_OVERLAPPED | global::Interop.Winsock.SocketConstructorFlags.WSA_FLAG_NO_HANDLE_INHERIT));
		if (socket.IsInvalid)
		{
			SocketError lastSocketError = GetLastSocketError();
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(null, $"WSASocketW failed with error {lastSocketError}", "CreateSocket");
			}
			socket.Dispose();
			return lastSocketError;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(null, socket, "CreateSocket");
		}
		return SocketError.Success;
	}

	public unsafe static SocketError CreateSocket(SocketInformation socketInformation, out SafeSocketHandle socket, ref AddressFamily addressFamily, ref SocketType socketType, ref ProtocolType protocolType)
	{
		if (socketInformation.ProtocolInformation == null || socketInformation.ProtocolInformation.Length < sizeof(global::Interop.Winsock.WSAPROTOCOL_INFOW))
		{
			throw new ArgumentException(System.SR.net_sockets_invalid_socketinformation, "socketInformation");
		}
		global::Interop.Winsock.EnsureInitialized();
		fixed (byte* ptr = socketInformation.ProtocolInformation)
		{
			socket = new SafeSocketHandle();
			Marshal.InitHandle(socket, global::Interop.Winsock.WSASocketW(AddressFamily.Unknown, SocketType.Unknown, ProtocolType.Unknown, (nint)ptr, 0u, global::Interop.Winsock.SocketConstructorFlags.WSA_FLAG_OVERLAPPED | global::Interop.Winsock.SocketConstructorFlags.WSA_FLAG_NO_HANDLE_INHERIT));
			if (socket.IsInvalid)
			{
				SocketError lastSocketError = GetLastSocketError();
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(null, $"WSASocketW failed with error {lastSocketError}", "CreateSocket");
				}
				socket.Dispose();
				return lastSocketError;
			}
			if (!global::Interop.Kernel32.SetHandleInformation(socket, global::Interop.Kernel32.HandleFlags.HANDLE_FLAG_INHERIT, global::Interop.Kernel32.HandleFlags.None))
			{
				SocketError lastSocketError2 = GetLastSocketError();
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(null, $"SetHandleInformation failed with error {lastSocketError2}", "CreateSocket");
				}
				socket.Dispose();
				return lastSocketError2;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, socket, "CreateSocket");
			}
			global::Interop.Winsock.WSAPROTOCOL_INFOW* ptr2 = (global::Interop.Winsock.WSAPROTOCOL_INFOW*)ptr;
			addressFamily = ptr2->iAddressFamily;
			socketType = ptr2->iSocketType;
			protocolType = ptr2->iProtocol;
			return SocketError.Success;
		}
	}

	public static SocketError SetBlocking(SafeSocketHandle handle, bool shouldBlock, out bool willBlock)
	{
		int argp = ((!shouldBlock) ? (-1) : 0);
		SocketError socketError = global::Interop.Winsock.ioctlsocket(handle, -2147195266, ref argp);
		if (socketError == SocketError.SocketError)
		{
			socketError = GetLastSocketError();
		}
		willBlock = argp == 0;
		return socketError;
	}

	public unsafe static SocketError GetSockName(SafeSocketHandle handle, byte* buffer, int* nameLen)
	{
		SocketError socketError = global::Interop.Winsock.getsockname(handle, buffer, nameLen);
		if (socketError != SocketError.SocketError)
		{
			return SocketError.Success;
		}
		return GetLastSocketError();
	}

	public static SocketError GetAvailable(SafeSocketHandle handle, out int available)
	{
		int argp = 0;
		SocketError socketError = global::Interop.Winsock.ioctlsocket(handle, 1074030207, ref argp);
		available = argp;
		if (socketError != SocketError.SocketError)
		{
			return SocketError.Success;
		}
		return GetLastSocketError();
	}

	public unsafe static SocketError GetPeerName(SafeSocketHandle handle, Span<byte> buffer, ref int nameLen)
	{
		fixed (byte* socketAddress = buffer)
		{
			SocketError socketError = global::Interop.Winsock.getpeername(handle, socketAddress, ref nameLen);
			if (socketError != SocketError.SocketError)
			{
				return SocketError.Success;
			}
			return GetLastSocketError();
		}
	}

	public static SocketError Bind(SafeSocketHandle handle, ProtocolType _, ReadOnlySpan<byte> buffer)
	{
		SocketError socketError = global::Interop.Winsock.bind(handle, buffer);
		if (socketError != SocketError.SocketError)
		{
			return SocketError.Success;
		}
		return GetLastSocketError();
	}

	public static SocketError Listen(SafeSocketHandle handle, int backlog)
	{
		SocketError socketError = global::Interop.Winsock.listen(handle, backlog);
		if (socketError != SocketError.SocketError)
		{
			return SocketError.Success;
		}
		return GetLastSocketError();
	}

	public static SocketError Accept(SafeSocketHandle listenSocket, Memory<byte> socketAddress, out int socketAddressSize, out SafeSocketHandle socket)
	{
		socket = new SafeSocketHandle();
		socketAddressSize = socketAddress.Length;
		Marshal.InitHandle(socket, global::Interop.Winsock.accept(listenSocket, socketAddress.Span, ref socketAddressSize));
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(null, socket, "Accept");
		}
		if (!socket.IsInvalid)
		{
			return SocketError.Success;
		}
		return GetLastSocketError();
	}

	public static SocketError Connect(SafeSocketHandle handle, Memory<byte> peerAddress)
	{
		SocketError socketError = global::Interop.Winsock.WSAConnect(handle, peerAddress.Span, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
		if (socketError != SocketError.SocketError)
		{
			return SocketError.Success;
		}
		return GetLastSocketError();
	}

	public unsafe static SocketError Send(SafeSocketHandle handle, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out int bytesTransferred)
	{
		int count = buffers.Count;
		bool flag = count <= 16;
		WSABuffer[] array = null;
		GCHandle[] array2 = null;
		Span<WSABuffer> buffers2;
		Span<GCHandle> span;
		if (flag)
		{
			buffers2 = stackalloc WSABuffer[16];
			span = stackalloc GCHandle[16];
		}
		else
		{
			buffers2 = (array = ArrayPool<WSABuffer>.Shared.Rent(count));
			span = (array2 = ArrayPool<GCHandle>.Shared.Rent(count));
		}
		span = span.Slice(0, count);
		span.Clear();
		try
		{
			for (int i = 0; i < count; i++)
			{
				ArraySegment<byte> segment = buffers[i];
				RangeValidationHelpers.ValidateSegment(segment);
				span[i] = GCHandle.Alloc(segment.Array, GCHandleType.Pinned);
				buffers2[i].Length = segment.Count;
				buffers2[i].Pointer = Marshal.UnsafeAddrOfPinnedArrayElement(segment.Array, segment.Offset);
			}
			SocketError socketError = global::Interop.Winsock.WSASend(handle, buffers2, count, out bytesTransferred, socketFlags, null, IntPtr.Zero);
			if (socketError == SocketError.SocketError)
			{
				socketError = GetLastSocketError();
			}
			return socketError;
		}
		finally
		{
			for (int j = 0; j < count; j++)
			{
				if (span[j].IsAllocated)
				{
					span[j].Free();
				}
			}
			if (!flag)
			{
				ArrayPool<WSABuffer>.Shared.Return(array);
				ArrayPool<GCHandle>.Shared.Return(array2);
			}
		}
	}

	public static SocketError Send(SafeSocketHandle handle, byte[] buffer, int offset, int size, SocketFlags socketFlags, out int bytesTransferred)
	{
		return Send(handle, new ReadOnlySpan<byte>(buffer, offset, size), socketFlags, out bytesTransferred);
	}

	public unsafe static SocketError Send(SafeSocketHandle handle, ReadOnlySpan<byte> buffer, SocketFlags socketFlags, out int bytesTransferred)
	{
		int num;
		fixed (byte* pinnedBuffer = &MemoryMarshal.GetReference(buffer))
		{
			num = global::Interop.Winsock.send(handle, pinnedBuffer, buffer.Length, socketFlags);
		}
		if (num == -1)
		{
			bytesTransferred = 0;
			return GetLastSocketError();
		}
		bytesTransferred = num;
		return SocketError.Success;
	}

	public unsafe static SocketError SendFile(SafeSocketHandle handle, SafeFileHandle fileHandle, ReadOnlySpan<byte> preBuffer, ReadOnlySpan<byte> postBuffer, TransmitFileOptions flags)
	{
		fixed (byte* pinnedPreBuffer = preBuffer)
		{
			fixed (byte* pinnedPostBuffer = postBuffer)
			{
				if (!TransmitFileHelper(handle, fileHandle, null, (nint)pinnedPreBuffer, preBuffer.Length, (nint)pinnedPostBuffer, postBuffer.Length, flags))
				{
					return GetLastSocketError();
				}
				return SocketError.Success;
			}
		}
	}

	public static SocketError SendTo(SafeSocketHandle handle, byte[] buffer, int offset, int size, SocketFlags socketFlags, ReadOnlyMemory<byte> peerAddress, out int bytesTransferred)
	{
		return SendTo(handle, buffer.AsSpan(offset, size), socketFlags, peerAddress, out bytesTransferred);
	}

	public unsafe static SocketError SendTo(SafeSocketHandle handle, ReadOnlySpan<byte> buffer, SocketFlags socketFlags, ReadOnlyMemory<byte> peerAddress, out int bytesTransferred)
	{
		int num;
		fixed (byte* pinnedBuffer = &MemoryMarshal.GetReference(buffer))
		{
			num = global::Interop.Winsock.sendto(handle, pinnedBuffer, buffer.Length, socketFlags, peerAddress.Span, peerAddress.Length);
		}
		if (num == -1)
		{
			bytesTransferred = 0;
			return GetLastSocketError();
		}
		bytesTransferred = num;
		return SocketError.Success;
	}

	public unsafe static SocketError Receive(SafeSocketHandle handle, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out int bytesTransferred)
	{
		int count = buffers.Count;
		bool flag = count <= 16;
		WSABuffer[] array = null;
		GCHandle[] array2 = null;
		Span<WSABuffer> buffers2;
		Span<GCHandle> span;
		if (flag)
		{
			buffers2 = stackalloc WSABuffer[16];
			span = stackalloc GCHandle[16];
		}
		else
		{
			buffers2 = (array = ArrayPool<WSABuffer>.Shared.Rent(count));
			span = (array2 = ArrayPool<GCHandle>.Shared.Rent(count));
		}
		span = span.Slice(0, count);
		span.Clear();
		try
		{
			for (int i = 0; i < count; i++)
			{
				ArraySegment<byte> segment = buffers[i];
				RangeValidationHelpers.ValidateSegment(segment);
				span[i] = GCHandle.Alloc(segment.Array, GCHandleType.Pinned);
				buffers2[i].Length = segment.Count;
				buffers2[i].Pointer = Marshal.UnsafeAddrOfPinnedArrayElement(segment.Array, segment.Offset);
			}
			SocketError socketError = global::Interop.Winsock.WSARecv(handle, buffers2, count, out bytesTransferred, ref socketFlags, null, IntPtr.Zero);
			if (socketError == SocketError.SocketError)
			{
				socketError = GetLastSocketError();
			}
			return socketError;
		}
		finally
		{
			for (int j = 0; j < count; j++)
			{
				if (span[j].IsAllocated)
				{
					span[j].Free();
				}
			}
			if (!flag)
			{
				ArrayPool<WSABuffer>.Shared.Return(array);
				ArrayPool<GCHandle>.Shared.Return(array2);
			}
		}
	}

	public static SocketError Receive(SafeSocketHandle handle, byte[] buffer, int offset, int size, SocketFlags socketFlags, out int bytesTransferred)
	{
		return Receive(handle, new Span<byte>(buffer, offset, size), socketFlags, out bytesTransferred);
	}

	public unsafe static SocketError Receive(SafeSocketHandle handle, Span<byte> buffer, SocketFlags socketFlags, out int bytesTransferred)
	{
		int num;
		fixed (byte* pinnedBuffer = &MemoryMarshal.GetReference(buffer))
		{
			num = global::Interop.Winsock.recv(handle, pinnedBuffer, buffer.Length, socketFlags);
		}
		if (num == -1)
		{
			bytesTransferred = 0;
			return GetLastSocketError();
		}
		bytesTransferred = num;
		return SocketError.Success;
	}

	public unsafe static IPPacketInformation GetIPPacketInformation(global::Interop.Winsock.ControlData* controlBuffer)
	{
		IPAddress address = ((controlBuffer->length == UIntPtr.Zero) ? IPAddress.None : new IPAddress(controlBuffer->address));
		return new IPPacketInformation(address, (int)controlBuffer->index);
	}

	public unsafe static IPPacketInformation GetIPPacketInformation(global::Interop.Winsock.ControlDataIPv6* controlBuffer)
	{
		if (controlBuffer->length == (nuint)sizeof(global::Interop.Winsock.ControlData))
		{
			return GetIPPacketInformation((global::Interop.Winsock.ControlData*)controlBuffer);
		}
		IPAddress address = ((controlBuffer->length != UIntPtr.Zero) ? new IPAddress(new ReadOnlySpan<byte>(controlBuffer->address, 16)) : IPAddress.IPv6None);
		return new IPPacketInformation(address, (int)controlBuffer->index);
	}

	public static SocketError ReceiveMessageFrom(Socket socket, SafeSocketHandle handle, byte[] buffer, int offset, int size, ref SocketFlags socketFlags, SocketAddress socketAddress, out SocketAddress receiveAddress, out IPPacketInformation ipPacketInformation, out int bytesTransferred)
	{
		return ReceiveMessageFrom(socket, handle, new Span<byte>(buffer, offset, size), ref socketFlags, socketAddress, out receiveAddress, out ipPacketInformation, out bytesTransferred);
	}

	public unsafe static SocketError ReceiveMessageFrom(Socket socket, SafeSocketHandle handle, Span<byte> buffer, ref SocketFlags socketFlags, SocketAddress socketAddress, out SocketAddress receiveAddress, out IPPacketInformation ipPacketInformation, out int bytesTransferred)
	{
		Socket.GetIPProtocolInformation(socket.AddressFamily, socketAddress, out var isIPv, out var isIPv2);
		bytesTransferred = 0;
		receiveAddress = socketAddress;
		ipPacketInformation = default(IPPacketInformation);
		fixed (byte* pointer = &MemoryMarshal.GetReference(buffer))
		{
			fixed (byte* socketAddress2 = &MemoryMarshal.GetReference(socketAddress.Buffer.Span))
			{
				Unsafe.SkipInit(out global::Interop.Winsock.WSAMsg wSAMsg);
				wSAMsg.socketAddress = (nint)socketAddress2;
				wSAMsg.addressLength = (uint)socketAddress.Size;
				wSAMsg.flags = socketFlags;
				Unsafe.SkipInit(out WSABuffer wSABuffer);
				wSABuffer.Length = buffer.Length;
				wSABuffer.Pointer = (nint)pointer;
				wSAMsg.buffers = (nint)(&wSABuffer);
				wSAMsg.count = 1u;
				if (isIPv)
				{
					Unsafe.SkipInit(out global::Interop.Winsock.ControlData controlData);
					wSAMsg.controlBuffer.Pointer = (nint)(&controlData);
					wSAMsg.controlBuffer.Length = sizeof(global::Interop.Winsock.ControlData);
					if (socket.WSARecvMsgBlocking(handle, (nint)(&wSAMsg), out bytesTransferred) == SocketError.SocketError)
					{
						return GetLastSocketError();
					}
					ipPacketInformation = GetIPPacketInformation(&controlData);
				}
				else if (isIPv2)
				{
					Unsafe.SkipInit(out global::Interop.Winsock.ControlDataIPv6 controlDataIPv);
					wSAMsg.controlBuffer.Pointer = (nint)(&controlDataIPv);
					wSAMsg.controlBuffer.Length = sizeof(global::Interop.Winsock.ControlDataIPv6);
					if (socket.WSARecvMsgBlocking(handle, (nint)(&wSAMsg), out bytesTransferred) == SocketError.SocketError)
					{
						return GetLastSocketError();
					}
					ipPacketInformation = GetIPPacketInformation(&controlDataIPv);
				}
				else
				{
					wSAMsg.controlBuffer.Pointer = IntPtr.Zero;
					wSAMsg.controlBuffer.Length = 0;
					if (socket.WSARecvMsgBlocking(handle, (nint)(&wSAMsg), out bytesTransferred) == SocketError.SocketError)
					{
						return GetLastSocketError();
					}
				}
				socketFlags = wSAMsg.flags;
			}
		}
		return SocketError.Success;
	}

	public static SocketError ReceiveFrom(SafeSocketHandle handle, byte[] buffer, int offset, int size, SocketFlags _, Memory<byte> socketAddress, out int addressLength, out int bytesTransferred)
	{
		return ReceiveFrom(handle, buffer.AsSpan(offset, size), SocketFlags.None, socketAddress, out addressLength, out bytesTransferred);
	}

	public static SocketError ReceiveFrom(SafeSocketHandle handle, Span<byte> buffer, SocketFlags socketFlags, Memory<byte> socketAddress, out int addressLength, out int bytesTransferred)
	{
		addressLength = socketAddress.Length;
		int num = global::Interop.Winsock.recvfrom(handle, buffer, buffer.Length, socketFlags, socketAddress.Span, ref addressLength);
		if (num == -1)
		{
			bytesTransferred = 0;
			return GetLastSocketError();
		}
		bytesTransferred = num;
		return SocketError.Success;
	}

	public static SocketError WindowsIoctl(SafeSocketHandle handle, int ioControlCode, byte[] optionInValue, byte[] optionOutValue, out int optionLength)
	{
		if (ioControlCode == -2147195266)
		{
			throw new InvalidOperationException(System.SR.net_sockets_useblocking);
		}
		SocketError socketError = global::Interop.Winsock.WSAIoctl_Blocking(handle, ioControlCode, optionInValue, (optionInValue != null) ? optionInValue.Length : 0, optionOutValue, (optionOutValue != null) ? optionOutValue.Length : 0, out optionLength, IntPtr.Zero, IntPtr.Zero);
		if (socketError != SocketError.SocketError)
		{
			return SocketError.Success;
		}
		return GetLastSocketError();
	}

	public static SocketError SetSockOpt(SafeSocketHandle handle, SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
	{
		SocketError socketError = ((optionLevel != SocketOptionLevel.Tcp || (optionName != SocketOptionName.TypeOfService && optionName != SocketOptionName.BlockSource) || !IOControlKeepAlive.IsNeeded) ? global::Interop.Winsock.setsockopt(handle, optionLevel, optionName, ref optionValue, 4) : IOControlKeepAlive.Set(handle, optionName, optionValue));
		if (socketError != SocketError.SocketError)
		{
			return SocketError.Success;
		}
		return GetLastSocketError();
	}

	public unsafe static SocketError SetSockOpt(SafeSocketHandle handle, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
	{
		if (optionLevel == SocketOptionLevel.Tcp && (optionName == SocketOptionName.TypeOfService || optionName == SocketOptionName.BlockSource) && IOControlKeepAlive.IsNeeded)
		{
			return IOControlKeepAlive.Set(handle, optionName, optionValue);
		}
		fixed (byte* optionValue2 = optionValue)
		{
			SocketError socketError = global::Interop.Winsock.setsockopt(handle, optionLevel, optionName, optionValue2, (optionValue != null) ? optionValue.Length : 0);
			if (socketError != SocketError.SocketError)
			{
				return SocketError.Success;
			}
			return GetLastSocketError();
		}
	}

	public unsafe static SocketError SetRawSockOpt(SafeSocketHandle handle, int optionLevel, int optionName, ReadOnlySpan<byte> optionValue)
	{
		fixed (byte* optionValue2 = optionValue)
		{
			SocketError socketError = global::Interop.Winsock.setsockopt(handle, (SocketOptionLevel)optionLevel, (SocketOptionName)optionName, optionValue2, optionValue.Length);
			if (socketError != SocketError.SocketError)
			{
				return SocketError.Success;
			}
			return GetLastSocketError();
		}
	}

	public static void SetReceivingDualModeIPv4PacketInformation(Socket socket)
	{
		socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, optionValue: true);
	}

	public static SocketError SetMulticastOption(SafeSocketHandle handle, SocketOptionName optionName, MulticastOption optionValue)
	{
		global::Interop.Winsock.IPMulticastRequest mreq = default(global::Interop.Winsock.IPMulticastRequest);
		mreq.MulticastAddress = (int)optionValue.Group.Address;
		if (optionValue.LocalAddress != null)
		{
			mreq.InterfaceAddress = (int)optionValue.LocalAddress.Address;
		}
		else
		{
			int interfaceAddress = IPAddress.HostToNetworkOrder(optionValue.InterfaceIndex);
			mreq.InterfaceAddress = interfaceAddress;
		}
		SocketError socketError = global::Interop.Winsock.setsockopt(handle, SocketOptionLevel.IP, optionName, ref mreq, global::Interop.Winsock.IPMulticastRequest.Size);
		if (socketError != SocketError.SocketError)
		{
			return SocketError.Success;
		}
		return GetLastSocketError();
	}

	public static SocketError SetIPv6MulticastOption(SafeSocketHandle handle, SocketOptionName optionName, IPv6MulticastOption optionValue)
	{
		global::Interop.Winsock.IPv6MulticastRequest mreq = default(global::Interop.Winsock.IPv6MulticastRequest);
		mreq.MulticastAddress = optionValue.Group.GetAddressBytes();
		mreq.InterfaceIndex = (int)optionValue.InterfaceIndex;
		SocketError socketError = global::Interop.Winsock.setsockopt(handle, SocketOptionLevel.IPv6, optionName, in mreq, global::Interop.Winsock.IPv6MulticastRequest.Size);
		if (socketError != SocketError.SocketError)
		{
			return SocketError.Success;
		}
		return GetLastSocketError();
	}

	public static SocketError SetLingerOption(SafeSocketHandle handle, LingerOption optionValue)
	{
		global::Interop.Winsock.Linger linger = default(global::Interop.Winsock.Linger);
		linger.OnOff = (optionValue.Enabled ? ((ushort)1) : ((ushort)0));
		linger.Time = (ushort)optionValue.LingerTime;
		SocketError socketError = global::Interop.Winsock.setsockopt(handle, SocketOptionLevel.Socket, SocketOptionName.Linger, ref linger, 4);
		if (socketError != SocketError.SocketError)
		{
			return SocketError.Success;
		}
		return GetLastSocketError();
	}

	public static void SetIPProtectionLevel(Socket socket, SocketOptionLevel optionLevel, int protectionLevel)
	{
		socket.SetSocketOption(optionLevel, SocketOptionName.IPProtectionLevel, protectionLevel);
	}

	public unsafe static SocketError GetSockOpt(SafeSocketHandle handle, SocketOptionLevel optionLevel, SocketOptionName optionName, out int optionValue)
	{
		if (optionLevel == SocketOptionLevel.Tcp && (optionName == SocketOptionName.TypeOfService || optionName == SocketOptionName.BlockSource) && IOControlKeepAlive.IsNeeded)
		{
			optionValue = IOControlKeepAlive.Get(handle, optionName);
			return SocketError.Success;
		}
		int optionLength = 4;
		int num = 0;
		SocketError socketError = global::Interop.Winsock.getsockopt(handle, optionLevel, optionName, (byte*)(&num), ref optionLength);
		optionValue = num;
		if (socketError != SocketError.SocketError)
		{
			return SocketError.Success;
		}
		return GetLastSocketError();
	}

	public unsafe static SocketError GetSockOpt(SafeSocketHandle handle, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue, ref int optionLength)
	{
		if (optionLevel == SocketOptionLevel.Tcp && (optionName == SocketOptionName.TypeOfService || optionName == SocketOptionName.BlockSource) && IOControlKeepAlive.IsNeeded)
		{
			return IOControlKeepAlive.Get(handle, optionName, optionValue, ref optionLength);
		}
		fixed (byte* optionValue2 = optionValue)
		{
			SocketError socketError = global::Interop.Winsock.getsockopt(handle, optionLevel, optionName, optionValue2, ref optionLength);
			if (socketError != SocketError.SocketError)
			{
				return SocketError.Success;
			}
			return GetLastSocketError();
		}
	}

	public unsafe static SocketError GetRawSockOpt(SafeSocketHandle handle, int optionLevel, int optionName, Span<byte> optionValue, ref int optionLength)
	{
		fixed (byte* optionValue2 = optionValue)
		{
			SocketError socketError = global::Interop.Winsock.getsockopt(handle, (SocketOptionLevel)optionLevel, (SocketOptionName)optionName, optionValue2, ref optionLength);
			if (socketError != SocketError.SocketError)
			{
				return SocketError.Success;
			}
			return GetLastSocketError();
		}
	}

	public static SocketError GetMulticastOption(SafeSocketHandle handle, SocketOptionName optionName, out MulticastOption optionValue)
	{
		global::Interop.Winsock.IPMulticastRequest optionValue2 = default(global::Interop.Winsock.IPMulticastRequest);
		int optionLength = global::Interop.Winsock.IPMulticastRequest.Size;
		SocketError socketError = global::Interop.Winsock.getsockopt(handle, SocketOptionLevel.IP, optionName, out optionValue2, ref optionLength);
		if (socketError == SocketError.SocketError)
		{
			optionValue = null;
			return GetLastSocketError();
		}
		IPAddress group = new IPAddress(optionValue2.MulticastAddress);
		IPAddress mcint = new IPAddress(optionValue2.InterfaceAddress);
		optionValue = new MulticastOption(group, mcint);
		return SocketError.Success;
	}

	public static SocketError GetIPv6MulticastOption(SafeSocketHandle handle, SocketOptionName optionName, out IPv6MulticastOption optionValue)
	{
		int optionLength = global::Interop.Winsock.IPv6MulticastRequest.Size;
		global::Interop.Winsock.IPv6MulticastRequest optionValue2;
		SocketError socketError = global::Interop.Winsock.getsockopt(handle, SocketOptionLevel.IP, optionName, out optionValue2, ref optionLength);
		if (socketError == SocketError.SocketError)
		{
			optionValue = null;
			return GetLastSocketError();
		}
		optionValue = new IPv6MulticastOption(new IPAddress(optionValue2.MulticastAddress), optionValue2.InterfaceIndex);
		return SocketError.Success;
	}

	public static SocketError GetLingerOption(SafeSocketHandle handle, out LingerOption optionValue)
	{
		int optionLength = 4;
		global::Interop.Winsock.Linger optionValue2;
		SocketError socketError = global::Interop.Winsock.getsockopt(handle, SocketOptionLevel.Socket, SocketOptionName.Linger, out optionValue2, ref optionLength);
		if (socketError == SocketError.SocketError)
		{
			optionValue = null;
			return GetLastSocketError();
		}
		optionValue = new LingerOption(optionValue2.OnOff != 0, optionValue2.Time);
		return SocketError.Success;
	}

	public unsafe static SocketError Poll(SafeSocketHandle handle, int microseconds, SelectMode mode, out bool status)
	{
		bool success = false;
		try
		{
			handle.DangerousAddRef(ref success);
			nint num = handle.DangerousGetHandle();
			nint* num2 = stackalloc nint[2];
			*num2 = 1;
			num2[1] = num;
			nint* ptr = num2;
			global::Interop.Winsock.TimeValue socketTime = default(global::Interop.Winsock.TimeValue);
			int num3;
			if (microseconds != -1)
			{
				MicrosecondsToTimeValue((uint)microseconds, ref socketTime);
				num3 = global::Interop.Winsock.select(0, (mode == SelectMode.SelectRead) ? ptr : null, (mode == SelectMode.SelectWrite) ? ptr : null, (mode == SelectMode.SelectError) ? ptr : null, ref socketTime);
			}
			else
			{
				num3 = global::Interop.Winsock.select(0, (mode == SelectMode.SelectRead) ? ptr : null, (mode == SelectMode.SelectWrite) ? ptr : null, (mode == SelectMode.SelectError) ? ptr : null, IntPtr.Zero);
			}
			if (num3 == -1)
			{
				status = false;
				return GetLastSocketError();
			}
			status = (int)(*ptr) != 0 && ptr[1] == num;
			return SocketError.Success;
		}
		finally
		{
			if (success)
			{
				handle.DangerousRelease();
			}
		}
	}

	public unsafe static SocketError Select(IList checkRead, IList checkWrite, IList checkError, int microseconds)
	{
		nint[] lease2 = null;
		nint[] lease3 = null;
		nint[] lease4 = null;
		int refsAdded = 0;
		try
		{
			Span<nint> span2;
			Span<nint> span3 = ((!ShouldStackAlloc(checkRead, ref lease2, out span2)) ? span2 : stackalloc nint[64]);
			Span<nint> span4 = span3;
			Socket.SocketListToFileDescriptorSet(checkRead, span4, ref refsAdded);
			Span<nint> span5 = ((!ShouldStackAlloc(checkWrite, ref lease3, out span2)) ? span2 : stackalloc nint[64]);
			Span<nint> span6 = span5;
			Socket.SocketListToFileDescriptorSet(checkWrite, span6, ref refsAdded);
			Span<nint> span7 = ((!ShouldStackAlloc(checkError, ref lease4, out span2)) ? span2 : stackalloc nint[64]);
			Span<nint> span8 = span7;
			Socket.SocketListToFileDescriptorSet(checkError, span8, ref refsAdded);
			int num;
			fixed (nint* readfds = &MemoryMarshal.GetReference(span4))
			{
				fixed (nint* writefds = &MemoryMarshal.GetReference(span6))
				{
					fixed (nint* exceptfds = &MemoryMarshal.GetReference(span8))
					{
						if (microseconds != -1)
						{
							global::Interop.Winsock.TimeValue socketTime = default(global::Interop.Winsock.TimeValue);
							MicrosecondsToTimeValue((uint)microseconds, ref socketTime);
							num = global::Interop.Winsock.select(0, readfds, writefds, exceptfds, ref socketTime);
						}
						else
						{
							num = global::Interop.Winsock.select(0, readfds, writefds, exceptfds, IntPtr.Zero);
						}
					}
				}
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, $"Interop.Winsock.select returns socketCount:{num}", "Select");
			}
			if (num == -1)
			{
				return GetLastSocketError();
			}
			Socket.SelectFileDescriptor(checkRead, span4, ref refsAdded);
			Socket.SelectFileDescriptor(checkWrite, span6, ref refsAdded);
			Socket.SelectFileDescriptor(checkError, span8, ref refsAdded);
			return SocketError.Success;
		}
		finally
		{
			if (lease2 != null)
			{
				ArrayPool<nint>.Shared.Return(lease2);
			}
			if (lease3 != null)
			{
				ArrayPool<nint>.Shared.Return(lease3);
			}
			if (lease4 != null)
			{
				ArrayPool<nint>.Shared.Return(lease4);
			}
			Socket.SocketListDangerousReleaseRefs(checkRead, ref refsAdded);
			Socket.SocketListDangerousReleaseRefs(checkWrite, ref refsAdded);
			Socket.SocketListDangerousReleaseRefs(checkError, ref refsAdded);
		}
		static bool ShouldStackAlloc(IList list, scoped ref nint[] lease, out Span<nint> span)
		{
			int count;
			if (list == null || (count = list.Count) == 0)
			{
				span = default(Span<nint>);
				return false;
			}
			if (count >= 64)
			{
				span = (lease = ArrayPool<nint>.Shared.Rent(count + 1));
				return false;
			}
			span = default(Span<nint>);
			return true;
		}
	}

	public static SocketError Shutdown(SafeSocketHandle handle, bool isConnected, bool isDisconnected, SocketShutdown how)
	{
		SocketError socketError = global::Interop.Winsock.shutdown(handle, (int)how);
		if (socketError != SocketError.SocketError)
		{
			handle.TrackShutdown(how);
			return SocketError.Success;
		}
		return GetLastSocketError();
	}

	private unsafe static bool TransmitFileHelper(SafeHandle socket, SafeHandle fileHandle, NativeOverlapped* overlapped, nint pinnedPreBuffer, int preBufferLength, nint pinnedPostBuffer, int postBufferLength, TransmitFileOptions flags)
	{
		bool flag = false;
		global::Interop.Mswsock.TransmitFileBuffers transmitFileBuffers = default(global::Interop.Mswsock.TransmitFileBuffers);
		if (preBufferLength > 0)
		{
			flag = true;
			transmitFileBuffers.Head = pinnedPreBuffer;
			transmitFileBuffers.HeadLength = preBufferLength;
		}
		if (postBufferLength > 0)
		{
			flag = true;
			transmitFileBuffers.Tail = pinnedPostBuffer;
			transmitFileBuffers.TailLength = postBufferLength;
		}
		bool success = false;
		nint fileHandle2 = IntPtr.Zero;
		try
		{
			if (fileHandle != null)
			{
				fileHandle.DangerousAddRef(ref success);
				fileHandle2 = fileHandle.DangerousGetHandle();
			}
			return global::Interop.Mswsock.TransmitFile(socket, fileHandle2, 0, 0, overlapped, flag ? (&transmitFileBuffers) : null, flags);
		}
		finally
		{
			if (success)
			{
				fileHandle.DangerousRelease();
			}
		}
	}

	internal static SocketError Disconnect(Socket socket, SafeSocketHandle handle, bool reuseSocket)
	{
		SocketError result = SocketError.Success;
		if (!socket.DisconnectExBlocking(handle, reuseSocket ? 2 : 0, 0))
		{
			result = GetLastSocketError();
		}
		return result;
	}

	internal unsafe static SocketError DuplicateSocket(SafeSocketHandle handle, int targetProcessId, out SocketInformation socketInformation)
	{
		socketInformation = new SocketInformation
		{
			ProtocolInformation = new byte[sizeof(global::Interop.Winsock.WSAPROTOCOL_INFOW)]
		};
		fixed (byte* ptr = socketInformation.ProtocolInformation)
		{
			global::Interop.Winsock.WSAPROTOCOL_INFOW* lpProtocolInfo = (global::Interop.Winsock.WSAPROTOCOL_INFOW*)ptr;
			if (global::Interop.Winsock.WSADuplicateSocket(handle, (uint)targetProcessId, lpProtocolInfo) != 0)
			{
				return GetLastSocketError();
			}
			return SocketError.Success;
		}
	}

	internal unsafe static bool HasNonBlockingConnectCompleted(SafeSocketHandle handle, out bool success)
	{
		bool success2 = false;
		try
		{
			handle.DangerousAddRef(ref success2);
			nint num = handle.DangerousGetHandle();
			nint* num2 = stackalloc nint[2];
			*num2 = 1;
			num2[1] = num;
			nint* ptr = num2;
			nint* num3 = stackalloc nint[2];
			*num3 = 1;
			num3[1] = num;
			nint* ptr2 = num3;
			global::Interop.Winsock.TimeValue socketTime = default(global::Interop.Winsock.TimeValue);
			MicrosecondsToTimeValue(0L, ref socketTime);
			int num4 = global::Interop.Winsock.select(0, null, ptr, ptr2, ref socketTime);
			if (num4 == -1)
			{
				throw new SocketException((int)GetLastSocketError());
			}
			if ((int)(*ptr2) != 0 && ptr2[1] == num)
			{
				success = false;
				return true;
			}
			if ((int)(*ptr) != 0 && ptr[1] == num)
			{
				success = true;
				return true;
			}
			success = false;
			return false;
		}
		finally
		{
			if (success2)
			{
				handle.DangerousRelease();
			}
		}
	}
}
