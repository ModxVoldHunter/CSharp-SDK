namespace System.Transactions.Configuration;

internal static class AppSettings
{
	private static volatile bool s_settingsInitialized;

	private static readonly object s_appSettingsLock = new object();

	private static bool s_includeDistributedTxIdInExceptionMessage;

	internal static bool IncludeDistributedTxIdInExceptionMessage
	{
		get
		{
			EnsureSettingsLoaded();
			return s_includeDistributedTxIdInExceptionMessage;
		}
	}

	private static void EnsureSettingsLoaded()
	{
		if (s_settingsInitialized)
		{
			return;
		}
		lock (s_appSettingsLock)
		{
			if (!s_settingsInitialized)
			{
				s_includeDistributedTxIdInExceptionMessage = false;
				s_settingsInitialized = true;
			}
		}
	}
}
