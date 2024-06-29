namespace System.Text.Json;

internal static class Utf8JsonWriterCache
{
	private sealed class ThreadLocalState
	{
		public readonly PooledByteBufferWriter BufferWriter;

		public readonly Utf8JsonWriter Writer;

		public int RentedWriters;

		public ThreadLocalState()
		{
			BufferWriter = PooledByteBufferWriter.CreateEmptyInstanceForCaching();
			Writer = Utf8JsonWriter.CreateEmptyInstanceForCaching();
		}
	}

	[ThreadStatic]
	private static ThreadLocalState t_threadLocalState;

	public static Utf8JsonWriter RentWriterAndBuffer(JsonSerializerOptions options, out PooledByteBufferWriter bufferWriter)
	{
		ThreadLocalState threadLocalState = t_threadLocalState ?? (t_threadLocalState = new ThreadLocalState());
		Utf8JsonWriter utf8JsonWriter;
		if (threadLocalState.RentedWriters++ == 0)
		{
			bufferWriter = threadLocalState.BufferWriter;
			utf8JsonWriter = threadLocalState.Writer;
			bufferWriter.InitializeEmptyInstance(options.DefaultBufferSize);
			utf8JsonWriter.Reset(bufferWriter, options.GetWriterOptions());
		}
		else
		{
			bufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize);
			utf8JsonWriter = new Utf8JsonWriter(bufferWriter, options.GetWriterOptions());
		}
		return utf8JsonWriter;
	}

	public static Utf8JsonWriter RentWriter(JsonSerializerOptions options, PooledByteBufferWriter bufferWriter)
	{
		ThreadLocalState threadLocalState = t_threadLocalState ?? (t_threadLocalState = new ThreadLocalState());
		Utf8JsonWriter utf8JsonWriter;
		if (threadLocalState.RentedWriters++ == 0)
		{
			utf8JsonWriter = threadLocalState.Writer;
			utf8JsonWriter.Reset(bufferWriter, options.GetWriterOptions());
		}
		else
		{
			utf8JsonWriter = new Utf8JsonWriter(bufferWriter, options.GetWriterOptions());
		}
		return utf8JsonWriter;
	}

	public static void ReturnWriterAndBuffer(Utf8JsonWriter writer, PooledByteBufferWriter bufferWriter)
	{
		ThreadLocalState threadLocalState = t_threadLocalState;
		writer.ResetAllStateForCacheReuse();
		bufferWriter.ClearAndReturnBuffers();
		threadLocalState.RentedWriters--;
	}

	public static void ReturnWriter(Utf8JsonWriter writer)
	{
		ThreadLocalState threadLocalState = t_threadLocalState;
		writer.ResetAllStateForCacheReuse();
		threadLocalState.RentedWriters--;
	}
}
