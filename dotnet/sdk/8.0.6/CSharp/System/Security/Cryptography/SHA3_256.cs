using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography;

public abstract class SHA3_256 : HashAlgorithm
{
	private sealed class Implementation : SHA3_256
	{
		private readonly HashProvider _hashProvider;

		public Implementation()
		{
			_hashProvider = HashProviderDispenser.CreateHashProvider("SHA3-256");
			HashSizeValue = _hashProvider.HashSizeInBytes * 8;
		}

		protected sealed override void HashCore(byte[] array, int ibStart, int cbSize)
		{
			_hashProvider.AppendHashData(array, ibStart, cbSize);
		}

		protected sealed override void HashCore(ReadOnlySpan<byte> source)
		{
			_hashProvider.AppendHashData(source);
		}

		protected sealed override byte[] HashFinal()
		{
			return _hashProvider.FinalizeHashAndReset();
		}

		protected sealed override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
		{
			return _hashProvider.TryFinalizeHashAndReset(destination, out bytesWritten);
		}

		public sealed override void Initialize()
		{
			_hashProvider.Reset();
		}

		protected sealed override void Dispose(bool disposing)
		{
			_hashProvider.Dispose(disposing);
			base.Dispose(disposing);
		}
	}

	public const int HashSizeInBits = 256;

	public const int HashSizeInBytes = 32;

	public static bool IsSupported { get; } = HashProviderDispenser.HashSupported("SHA3-256");


	protected SHA3_256()
	{
		HashSizeValue = 256;
	}

	public new static SHA3_256 Create()
	{
		CheckSha3Support();
		return new Implementation();
	}

	public static byte[] HashData(byte[] source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return HashData(new ReadOnlySpan<byte>(source));
	}

	public static byte[] HashData(ReadOnlySpan<byte> source)
	{
		byte[] array = new byte[32];
		int num = HashData(source, array.AsSpan());
		return array;
	}

	public static int HashData(ReadOnlySpan<byte> source, Span<byte> destination)
	{
		if (!TryHashData(source, destination, out var bytesWritten))
		{
			throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
		}
		return bytesWritten;
	}

	public static bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
	{
		CheckSha3Support();
		if (destination.Length < 32)
		{
			bytesWritten = 0;
			return false;
		}
		bytesWritten = HashProviderDispenser.OneShotHashProvider.HashData("SHA3-256", source, destination);
		return true;
	}

	public static int HashData(Stream source, Span<byte> destination)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		if (destination.Length < 32)
		{
			throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
		}
		if (!source.CanRead)
		{
			throw new ArgumentException(System.SR.Argument_StreamNotReadable, "source");
		}
		CheckSha3Support();
		return LiteHashProvider.HashStream("SHA3-256", source, destination);
	}

	public static byte[] HashData(Stream source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		if (!source.CanRead)
		{
			throw new ArgumentException(System.SR.Argument_StreamNotReadable, "source");
		}
		CheckSha3Support();
		return LiteHashProvider.HashStream("SHA3-256", 32, source);
	}

	public static ValueTask<byte[]> HashDataAsync(Stream source, CancellationToken cancellationToken = default(CancellationToken))
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		if (!source.CanRead)
		{
			throw new ArgumentException(System.SR.Argument_StreamNotReadable, "source");
		}
		CheckSha3Support();
		return LiteHashProvider.HashStreamAsync("SHA3-256", source, cancellationToken);
	}

	public static ValueTask<int> HashDataAsync(Stream source, Memory<byte> destination, CancellationToken cancellationToken = default(CancellationToken))
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		if (destination.Length < 32)
		{
			throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
		}
		if (!source.CanRead)
		{
			throw new ArgumentException(System.SR.Argument_StreamNotReadable, "source");
		}
		CheckSha3Support();
		return LiteHashProvider.HashStreamAsync("SHA3-256", source, destination, cancellationToken);
	}

	private static void CheckSha3Support()
	{
		if (!IsSupported)
		{
			throw new PlatformNotSupportedException();
		}
	}
}
