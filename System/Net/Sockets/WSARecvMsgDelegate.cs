using System.Threading;

namespace System.Net.Sockets;

internal unsafe delegate SocketError WSARecvMsgDelegate(SafeSocketHandle socketHandle, nint msg, out int bytesTransferred, NativeOverlapped* overlapped, nint completionRoutine);
