using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

internal struct LiteXof : ILiteHash, IDisposable
{
	private readonly nuint _algorithm;

	private SafeBCryptHashHandle _hashHandle;

	public readonly int HashSizeInBytes
	{
		get
		{
			throw new CryptographicException();
		}
	}

	internal LiteXof(string algorithm)
	{
		_hashHandle = null;
		nuint algorithm2;
		if (!(algorithm == "CSHAKE128"))
		{
			if (!(algorithm == "CSHAKE256"))
			{
				throw FailThrow(algorithm);
			}
			algorithm2 = 1057u;
		}
		else
		{
			algorithm2 = 1041u;
		}
		_algorithm = algorithm2;
		Reset();
		static Exception FailThrow(string algorithm)
		{
			return new CryptographicException();
		}
	}

	public readonly void Append(ReadOnlySpan<byte> data)
	{
		if (!data.IsEmpty)
		{
			global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptHashData(_hashHandle, data, data.Length, 0);
			if (nTSTATUS != 0)
			{
				throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
			}
		}
	}

	public unsafe readonly int Finalize(Span<byte> destination)
	{
		fixed (byte* pbOutput = &Helpers.GetNonNullPinnableReference(destination))
		{
			global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptFinishHash(_hashHandle, pbOutput, destination.Length, 0);
			if (nTSTATUS != 0)
			{
				throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
			}
			return destination.Length;
		}
	}

	[MemberNotNull("_hashHandle")]
	public void Reset()
	{
		_hashHandle?.Dispose();
		SafeBCryptHashHandle phHash;
		global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptCreateHash(_algorithm, out phHash, IntPtr.Zero, 0, ReadOnlySpan<byte>.Empty, 0, global::Interop.BCrypt.BCryptCreateHashFlags.None);
		if (nTSTATUS != 0)
		{
			phHash.Dispose();
			throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
		}
		_hashHandle = phHash;
	}

	public unsafe readonly void Current(Span<byte> destination)
	{
		using SafeBCryptHashHandle hHash = global::Interop.BCrypt.BCryptDuplicateHash(_hashHandle);
		fixed (byte* pbOutput = &Helpers.GetNonNullPinnableReference(destination))
		{
			global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptFinishHash(hHash, pbOutput, destination.Length, 0);
			if (nTSTATUS != 0)
			{
				throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
			}
		}
	}

	public readonly void Dispose()
	{
		_hashHandle.Dispose();
	}
}
