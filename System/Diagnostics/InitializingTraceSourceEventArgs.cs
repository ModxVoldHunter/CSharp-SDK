namespace System.Diagnostics;

public sealed class InitializingTraceSourceEventArgs : EventArgs
{
	public TraceSource TraceSource { get; }

	public bool WasInitialized { get; set; }

	public InitializingTraceSourceEventArgs(TraceSource traceSource)
	{
		TraceSource = traceSource;
	}
}
