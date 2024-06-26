namespace System.Diagnostics.Tracing;

internal static class RuntimeEventSourceHelper
{
	private static long s_prevProcUserTime;

	private static long s_prevProcKernelTime;

	private static long s_prevSystemUserTime;

	private static long s_prevSystemKernelTime;

	internal static double GetCpuUsage()
	{
		double result = 0.0;
		if (Interop.Kernel32.GetProcessTimes(Interop.Kernel32.GetCurrentProcess(), out var _, out var exit, out var kernel, out var user) && Interop.Kernel32.GetSystemTimes(out exit, out var kernel2, out var user2))
		{
			long num = user - s_prevProcUserTime + (kernel - s_prevProcKernelTime);
			long num2 = user2 - s_prevSystemUserTime + (kernel2 - s_prevSystemKernelTime);
			if (s_prevSystemUserTime != 0L && s_prevSystemKernelTime != 0L && num2 != 0L)
			{
				result = (double)num * 100.0 / (double)num2;
			}
			s_prevProcUserTime = user;
			s_prevProcKernelTime = kernel;
			s_prevSystemUserTime = user2;
			s_prevSystemKernelTime = kernel2;
		}
		return result;
	}
}
