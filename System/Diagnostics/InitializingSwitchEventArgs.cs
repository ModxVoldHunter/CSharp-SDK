namespace System.Diagnostics;

public sealed class InitializingSwitchEventArgs : EventArgs
{
	public Switch Switch { get; }

	public InitializingSwitchEventArgs(Switch @switch)
	{
		Switch = @switch;
	}
}
