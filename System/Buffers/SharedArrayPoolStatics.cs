namespace System.Buffers;

internal static class SharedArrayPoolStatics
{
	internal static readonly int s_partitionCount = GetPartitionCount();

	internal static readonly int s_maxArraysPerPartition = GetMaxArraysPerPartition();

	private static int GetPartitionCount()
	{
		int result;
		int val = ((TryGetInt32EnvironmentVariable("DOTNET_SYSTEM_BUFFERS_SHAREDARRAYPOOL_MAXPARTITIONCOUNT", out result) && result > 0) ? result : int.MaxValue);
		return Math.Min(val, Environment.ProcessorCount);
	}

	private static int GetMaxArraysPerPartition()
	{
		if (!TryGetInt32EnvironmentVariable("DOTNET_SYSTEM_BUFFERS_SHAREDARRAYPOOL_MAXARRAYSPERPARTITION", out var result) || result <= 0)
		{
			return 32;
		}
		return result;
	}

	private static bool TryGetInt32EnvironmentVariable(string variable, out int result)
	{
		string environmentVariableCore_NoArrayPool = Environment.GetEnvironmentVariableCore_NoArrayPool(variable);
		if (environmentVariableCore_NoArrayPool != null)
		{
			int length = environmentVariableCore_NoArrayPool.Length;
			if (length > 0 && length <= 32)
			{
				ReadOnlySpan<char> readOnlySpan = environmentVariableCore_NoArrayPool.AsSpan().Trim(' ');
				if (!readOnlySpan.IsEmpty && readOnlySpan.Length <= 10)
				{
					long num = 0L;
					ReadOnlySpan<char> readOnlySpan2 = readOnlySpan;
					int num2 = 0;
					while (true)
					{
						if (num2 < readOnlySpan2.Length)
						{
							char c = readOnlySpan2[num2];
							uint num3 = (uint)(c - 48);
							if (num3 > 9)
							{
								break;
							}
							num = num * 10 + num3;
							num2++;
							continue;
						}
						if (num < 0 || num > int.MaxValue)
						{
							break;
						}
						result = (int)num;
						return true;
					}
				}
			}
		}
		result = 0;
		return false;
	}
}
