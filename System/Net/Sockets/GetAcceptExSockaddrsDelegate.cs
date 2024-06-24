namespace System.Net.Sockets;

internal delegate void GetAcceptExSockaddrsDelegate(nint buffer, int receiveDataLength, int localAddressLength, int remoteAddressLength, out nint localSocketAddress, out int localSocketAddressLength, out nint remoteSocketAddress, out int remoteSocketAddressLength);
