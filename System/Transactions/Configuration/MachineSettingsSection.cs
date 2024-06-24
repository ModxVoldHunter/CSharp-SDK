namespace System.Transactions.Configuration;

internal sealed class MachineSettingsSection
{
	private static readonly MachineSettingsSection s_section = new MachineSettingsSection();

	private static readonly TimeSpan s_maxTimeout = TimeSpan.Parse("00:10:00");

	public static TimeSpan MaxTimeout => s_maxTimeout;
}
