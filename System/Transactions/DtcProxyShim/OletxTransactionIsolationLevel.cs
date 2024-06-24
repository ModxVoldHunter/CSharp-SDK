namespace System.Transactions.DtcProxyShim;

internal enum OletxTransactionIsolationLevel
{
	ISOLATIONLEVEL_UNSPECIFIED = -1,
	ISOLATIONLEVEL_CHAOS = 16,
	ISOLATIONLEVEL_READUNCOMMITTED = 256,
	ISOLATIONLEVEL_BROWSE = 256,
	ISOLATIONLEVEL_CURSORSTABILITY = 4096,
	ISOLATIONLEVEL_READCOMMITTED = 4096,
	ISOLATIONLEVEL_REPEATABLEREAD = 65536,
	ISOLATIONLEVEL_SERIALIZABLE = 1048576,
	ISOLATIONLEVEL_ISOLATED = 1048576
}
