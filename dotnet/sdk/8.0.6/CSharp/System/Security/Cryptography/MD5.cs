using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography;

public abstract class MD5 : HashAlgorithm
{
	private sealed class Implementation : MD5
	{
		private readonly HashProvider _hashProvider;

		public Implementation()
		{
			_hashProvider = HashProviderDispenser.CreateHashProvider("MD5");
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

	public const int HashSizeInBits = 128;

	public const int HashSizeInBytes = 16;

	protected MD5()
	{
		HashSizeValue = 128;
	}

	[UnsupportedOSPlatform("browser")]
	public new static MD5 Create()
	{
		return new Implementation();
	}

	[Obsolete("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId = "SYSLIB0045", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public new static MD5? Create(string algName)
	{
		return (MD5)CryptoConfig.CreateFromName(algName);
	}

	[UnsupportedOSPlatform("browser")]
	public static byte[] HashData(byte[] source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return HashData(new ReadOnlySpan<byte>(source));
	}

	[UnsupportedOSPlatform("browser")]
	public static byte[] HashData(ReadOnlySpan<byte> source)
	{
		byte[] array = GC.AllocateUninitializedArray<byte>(16);
		int num = HashData(source, array.AsSpan());
		return array;
	}

	[UnsupportedOSPlatform("browser")]
	public static int HashData(ReadOnlySpan<byte> source, Span<byte> destination)
	{
		if (!TryHashData(source, destination, out var bytesWritten))
		{
			throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
		}
		return bytesWritten;
	}

	[UnsupportedOSPlatform("browser")]
	public static bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length < 16)
		{
			bytesWritten = 0;
			return false;
		}
		bytesWritten = HashProviderDispenser.OneShotHashProvider.HashData("MD5", source, destination);
		return true;
	}

	[UnsupportedOSPlatform("browser")]
	public static int HashData(Stream source, Span<byte> destination)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		if (destination.Length < 16)
		{
			throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
		}
		if (!source.CanRead)
		{
			throw new ArgumentException(System.SR.Argument_StreamNotReadable, "source");
		}
		return LiteHashProvider.HashStream("MD5", source, destination);
	}

	[UnsupportedOSPlatform("browser")]
	public static byte[] HashData(Stream source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		if (!source.CanRead)
		{
			throw new ArgumentException(System.SR.Argument_StreamNotReadable, "source");
		}
		return LiteHashProvider.HashStream("MD5", 16, source);
	}

	[UnsupportedOSPlatform("browser")]
	public static ValueTask<byte[]> HashDataAsync(Stream source, CancellationToken cancellationToken = default(CancellationToken))
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		if (!source.CanRead)
		{
			throw new ArgumentException(System.SR.Argument_StreamNotReadable, "source");
		}
		return LiteHashProvider.HashStreamAsync("MD5", source, cancellationToken);
	}

	[UnsupportedOSPlatform("browser")]
	public static ValueTask<int> HashDataAsync(Stream source, Memory<byte> destination, CancellationToken cancellationToken = default(CancellationToken))
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		if (destination.Length < 16)
		{
			throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
		}
		if (!source.CanRead)
		{
			throw new ArgumentException(System.SR.Argument_StreamNotReadable, "source");
		}
		return LiteHashProvider.HashStreamAsync("MD5", source, destination, cancellationToken);
	}
}
