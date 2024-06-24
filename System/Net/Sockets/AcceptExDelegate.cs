using System.Threading;

namespace System.Net.Sockets;

internal unsafe delegate bool AcceptExDelegate(SafeSocketHandle listenSocketHandle, SafeSocketHandle acceptSocketHandle, nint buffer, int len, int localAddressLength, int remoteAddressLength, out int bytesReceived, NativeOverlapped* overlapped);
