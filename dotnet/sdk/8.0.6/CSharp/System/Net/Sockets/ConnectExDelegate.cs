using System.Threading;

namespace System.Net.Sockets;

internal unsafe delegate bool ConnectExDelegate(SafeSocketHandle socketHandle, ReadOnlySpan<byte> socketAddress, nint buffer, int dataLength, out int bytesSent, NativeOverlapped* overlapped);
