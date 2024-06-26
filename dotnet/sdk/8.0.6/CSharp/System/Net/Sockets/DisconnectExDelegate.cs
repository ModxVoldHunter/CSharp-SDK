using System.Threading;

namespace System.Net.Sockets;

internal unsafe delegate bool DisconnectExDelegate(SafeSocketHandle socketHandle, NativeOverlapped* overlapped, int flags, int reserved);
