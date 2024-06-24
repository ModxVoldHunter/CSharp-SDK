namespace System.IO.Compression;

internal static class BrotliUtils
{
	internal static int GetQualityFromCompressionLevel(CompressionLevel compressionLevel)
	{
		return compressionLevel switch
		{
			CompressionLevel.NoCompression => 0, 
			CompressionLevel.Fastest => 1, 
			CompressionLevel.Optimal => 4, 
			CompressionLevel.SmallestSize => 11, 
			_ => throw new ArgumentException(System.SR.ArgumentOutOfRange_Enum, "compressionLevel"), 
		};
	}
}
