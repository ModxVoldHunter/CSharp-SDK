namespace System.Net.Sockets;

public enum SocketError
{
	Success = 0,
	SocketError = -1,
	Interrupted = 10004,
	AccessDenied = 10013,
	Fault = 10014,
	InvalidArgument = 10022,
	TooManyOpenSockets = 10024,
	WouldBlock = 10035,
	InProgress = 10036,
	AlreadyInProgress = 10037,
	NotSocket = 10038,
	DestinationAddressRequired = 10039,
	MessageSize = 10040,
	ProtocolType = 10041,
	ProtocolOption = 10042,
	ProtocolNotSupported = 10043,
	SocketNotSupported = 10044,
	OperationNotSupported = 10045,
	ProtocolFamilyNotSupported = 10046,
	AddressFamilyNotSupported = 10047,
	AddressAlreadyInUse = 10048,
	AddressNotAvailable = 10049,
	NetworkDown = 10050,
	NetworkUnreachable = 10051,
	NetworkReset = 10052,
	ConnectionAborted = 10053,
	ConnectionReset = 10054,
	NoBufferSpaceAvailable = 10055,
	IsConnected = 10056,
	NotConnected = 10057,
	Shutdown = 10058,
	TimedOut = 10060,
	ConnectionRefused = 10061,
	HostDown = 10064,
	HostUnreachable = 10065,
	ProcessLimit = 10067,
	SystemNotReady = 10091,
	VersionNotSupported = 10092,
	NotInitialized = 10093,
	Disconnecting = 10101,
	TypeNotFound = 10109,
	HostNotFound = 11001,
	TryAgain = 11002,
	NoRecovery = 11003,
	NoData = 11004,
	IOPending = 997,
	OperationAborted = 995
}
