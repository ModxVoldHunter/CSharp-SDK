namespace System.Net.Http;

internal enum Http3SettingType : long
{
	QPackMaxTableCapacity = 1L,
	ReservedHttp2EnablePush = 2L,
	ReservedHttp2MaxConcurrentStreams = 3L,
	ReservedHttp2InitialWindowSize = 4L,
	ReservedHttp2MaxFrameSize = 5L,
	MaxHeaderListSize = 6L,
	QPackBlockedStreams = 7L,
	EnableWebTransport = 727725890L,
	H3Datagram = 16765559L
}
