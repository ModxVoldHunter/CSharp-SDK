using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography;

public sealed class Shake128 : IDisposable
{
	private LiteXof _hashProvider;

	private bool _disposed;

	public static bool IsSupported { get; } = HashProviderDispenser.HashSupported("CSHAKE128");


	public Shake128()
	{
		CheckPlatformSupport();
		_hashProvider = LiteHashProvider.CreateXof("CSHAKE128");
	}

	public void AppendData(byte[] data)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		AppendData(new ReadOnlySpan<byte>(data));
	}

	public void AppendData(ReadOnlySpan<byte> data)
	{
		CheckDisposed();
		_hashProvider.Append(data);
	}

	public byte[] GetHashAndReset(int outputLength)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(outputLength, "outputLength");
		CheckDisposed();
		byte[] array = new byte[outputLength];
		_hashProvider.Finalize(array);
		_hashProvider.Reset();
		return array;
	}

	public void GetHashAndReset(Span<byte> destination)
	{
		CheckDisposed();
		_hashProvider.Finalize(destination);
		_hashProvider.Reset();
	}

	public byte[] GetCurrentHash(int outputLength)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(outputLength, "outputLength");
		CheckDisposed();
		byte[] array = new byte[outputLength];
		_hashProvider.Current(array);
		return array;
	}

	public void GetCurrentHash(Span<byte> destination)
	{
		CheckDisposed();
		_hashProvider.Current(destination);
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_hashProvider.Dispose();
			_disposed = true;
		}
	}

	public static byte[] HashData(byte[] source, int outputLength)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return HashData(new ReadOnlySpan<byte>(source), outputLength);
	}

	public static byte[] HashData(ReadOnlySpan<byte> source, int outputLength)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(outputLength, "outputLength");
		CheckPlatformSupport();
		byte[] array = new byte[outputLength];
		HashDataCore(source, array);
		return array;
	}

	public static void HashData(ReadOnlySpan<byte> source, Span<byte> destination)
	{
		CheckPlatformSupport();
		HashDataCore(source, destination);
	}

	public static byte[] HashData(Stream source, int outputLength)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentOutOfRangeException.ThrowIfNegative(outputLength, "outputLength");
		if (!source.CanRead)
		{
			throw new ArgumentException(System.SR.Argument_StreamNotReadable, "source");
		}
		CheckPlatformSupport();
		return LiteHashProvider.XofStream("CSHAKE128", outputLength, source);
	}

	public static void HashData(Stream source, Span<byte> destination)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		if (!source.CanRead)
		{
			throw new ArgumentException(System.SR.Argument_StreamNotReadable, "source");
		}
		CheckPlatformSupport();
		LiteHashProvider.XofStream("CSHAKE128", source, destination);
	}

	public static ValueTask HashDataAsync(Stream source, Memory<byte> destination, CancellationToken cancellationToken = default(CancellationToken))
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		if (!source.CanRead)
		{
			throw new ArgumentException(System.SR.Argument_StreamNotReadable, "source");
		}
		CheckPlatformSupport();
		return LiteHashProvider.XofStreamAsync("CSHAKE128", source, destination, cancellationToken);
	}

	public static ValueTask<byte[]> HashDataAsync(Stream source, int outputLength, CancellationToken cancellationToken = default(CancellationToken))
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentOutOfRangeException.ThrowIfNegative(outputLength, "outputLength");
		if (!source.CanRead)
		{
			throw new ArgumentException(System.SR.Argument_StreamNotReadable, "source");
		}
		CheckPlatformSupport();
		return LiteHashProvider.XofStreamAsync("CSHAKE128", outputLength, source, cancellationToken);
	}

	private static void HashDataCore(ReadOnlySpan<byte> source, Span<byte> destination)
	{
		HashProviderDispenser.OneShotHashProvider.HashDataXof("CSHAKE128", source, destination);
	}

	private static void CheckPlatformSupport()
	{
		if (!IsSupported)
		{
			throw new PlatformNotSupportedException();
		}
	}

	private void CheckDisposed()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}
}
