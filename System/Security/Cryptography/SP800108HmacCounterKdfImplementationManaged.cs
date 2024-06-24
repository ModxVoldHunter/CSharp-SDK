using System.Buffers.Binary;
using System.Threading;

namespace System.Security.Cryptography;

internal sealed class SP800108HmacCounterKdfImplementationManaged : SP800108HmacCounterKdfImplementationBase
{
	private byte[] _key;

	private int _keyReferenceCount = 1;

	private int _disposed;

	private readonly HashAlgorithmName _hashAlgorithm;

	internal override void DeriveBytes(ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, Span<byte> destination)
	{
		byte[] array = IncrementAndAcquireKey();
		try
		{
			DeriveBytesOneShot(array, _hashAlgorithm, label, context, destination);
		}
		finally
		{
			ReleaseKey();
		}
	}

	internal override void DeriveBytes(ReadOnlySpan<char> label, ReadOnlySpan<char> context, Span<byte> destination)
	{
		byte[] array = IncrementAndAcquireKey();
		try
		{
			DeriveBytesOneShot(array, _hashAlgorithm, label, context, destination);
		}
		finally
		{
			ReleaseKey();
		}
	}

	public override void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) == 0)
		{
			ReleaseKey();
		}
	}

	private byte[] IncrementAndAcquireKey()
	{
		int num;
		int value;
		do
		{
			num = Volatile.Read(ref _keyReferenceCount);
			if (num == 0)
			{
				throw new ObjectDisposedException("SP800108HmacCounterKdfImplementationManaged");
			}
			value = checked(num + 1);
		}
		while (Interlocked.CompareExchange(ref _keyReferenceCount, value, num) != num);
		return _key;
	}

	public void ReleaseKey()
	{
		if (Interlocked.Decrement(ref _keyReferenceCount) == 0)
		{
			ZeroKey();
		}
	}

	private void ZeroKey()
	{
		CryptographicOperations.ZeroMemory(_key);
		_key = null;
	}

	public SP800108HmacCounterKdfImplementationManaged(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm)
	{
		_key = GC.AllocateArray<byte>(key.Length, pinned: true);
		key.CopyTo(_key);
		_hashAlgorithm = hashAlgorithm;
	}

	internal static void DeriveBytesOneShot(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, Span<byte> destination)
	{
		if (destination.Length == 0)
		{
			return;
		}
		checked
		{
			using IncrementalHash incrementalHash = IncrementalHash.CreateHMAC(hashAlgorithm, key);
			Span<byte> span = stackalloc byte[4];
			Span<byte> span2 = stackalloc byte[4];
			Span<byte> span3 = stackalloc byte[1] { 0 };
			ReadOnlySpan<byte> data = span3;
			Span<byte> destination2 = stackalloc byte[64];
			int length = 0;
			BinaryPrimitives.WriteUInt32BigEndian(span2, (uint)destination.Length * 8);
			uint num = 1u;
			while (!destination.IsEmpty)
			{
				BinaryPrimitives.WriteUInt32BigEndian(span, num);
				incrementalHash.AppendData(span);
				incrementalHash.AppendData(label);
				incrementalHash.AppendData(data);
				incrementalHash.AppendData(context);
				incrementalHash.AppendData(span2);
				if (destination.Length >= incrementalHash.HashLengthInBytes)
				{
					int hashAndReset = incrementalHash.GetHashAndReset(destination);
					destination = destination.Slice(hashAndReset);
				}
				else
				{
					length = incrementalHash.GetHashAndReset(destination2);
					destination2.Slice(0, destination.Length).CopyTo(destination);
					destination = default(Span<byte>);
				}
				num++;
			}
			CryptographicOperations.ZeroMemory(destination2.Slice(0, length));
		}
	}

	internal static void DeriveBytesOneShot(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm, ReadOnlySpan<char> label, ReadOnlySpan<char> context, Span<byte> destination)
	{
		if (destination.Length == 0)
		{
			return;
		}
		Span<byte> stackBuffer = stackalloc byte[256];
		using Utf8DataEncoding utf8DataEncoding = new Utf8DataEncoding(label, stackBuffer);
		stackBuffer = stackalloc byte[256];
		using Utf8DataEncoding utf8DataEncoding2 = new Utf8DataEncoding(context, stackBuffer);
		DeriveBytesOneShot(key, hashAlgorithm, utf8DataEncoding.Utf8Bytes, utf8DataEncoding2.Utf8Bytes, destination);
	}
}
