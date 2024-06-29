namespace System.Transactions.Configuration;

internal sealed class DefaultSettingsSection
{
	private static readonly DefaultSettingsSection s_section = new DefaultSettingsSection();

	private static readonly TimeSpan s_timeout = TimeSpan.Parse("00:01:00");

	public static string DistributedTransactionManagerName { get; } = "";


	public static TimeSpan Timeout => s_timeout;
}
