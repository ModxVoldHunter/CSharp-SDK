using System.Threading;

namespace System.Net.Sockets;

internal unsafe delegate bool TransmitPacketsDelegate(SafeSocketHandle socketHandle, nint packetArray, int elementCount, int sendSize, NativeOverlapped* overlapped, TransmitFileOptions flags);
