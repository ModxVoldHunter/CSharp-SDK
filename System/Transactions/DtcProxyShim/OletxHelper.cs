using System.Runtime.InteropServices;
using System.Threading;

namespace System.Transactions.DtcProxyShim;

internal static class OletxHelper
{
	internal static int S_OK = 0;

	internal static int E_FAIL = -2147467259;

	internal static int XACT_S_READONLY = 315394;

	internal static int XACT_S_SINGLEPHASE = 315401;

	internal static int XACT_E_ABORTED = -2147168231;

	internal static int XACT_E_NOTRANSACTION = -2147168242;

	internal static int XACT_E_CONNECTION_DOWN = -2147168228;

	internal static int XACT_E_REENLISTTIMEOUT = -2147168226;

	internal static int XACT_E_RECOVERYALREADYDONE = -2147167996;

	internal static int XACT_E_TMNOTAVAILABLE = -2147168229;

	internal static int XACT_E_INDOUBT = -2147168234;

	internal static int XACT_E_ALREADYINPROGRESS = -2147168232;

	internal static int XACT_E_TOOMANY_ENLISTMENTS = -2147167999;

	internal static int XACT_E_PROTOCOL = -2147167995;

	internal static int XACT_E_FIRST = -2147168256;

	internal static int XACT_E_LAST = -2147168215;

	internal static int XACT_E_NOTSUPPORTED = -2147168241;

	internal static int XACT_E_NETWORK_TX_DISABLED = -2147168220;

	internal static void Retry(Action action)
	{
		int num = 100;
		while (true)
		{
			try
			{
				action();
				break;
			}
			catch (COMException ex) when (ex.ErrorCode == XACT_E_ALREADYINPROGRESS)
			{
				if (--num == 0)
				{
					throw;
				}
				Thread.Sleep(50);
			}
		}
	}
}
