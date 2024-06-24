using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates;

public sealed class X509SubjectKeyIdentifierExtension : X509Extension
{
	private byte[] _subjectKeyIdentifierBytes;

	private string _subjectKeyIdentifierString;

	private bool _decoded;

	public string? SubjectKeyIdentifier
	{
		get
		{
			if (!_decoded)
			{
				Decode(base.RawData);
			}
			return _subjectKeyIdentifierString;
		}
	}

	public ReadOnlyMemory<byte> SubjectKeyIdentifierBytes
	{
		get
		{
			if (_subjectKeyIdentifierBytes == null)
			{
				Decode(base.RawData);
			}
			return _subjectKeyIdentifierBytes;
		}
	}

	public X509SubjectKeyIdentifierExtension()
		: base(Oids.SubjectKeyIdentifierOid)
	{
		_decoded = true;
	}

	public X509SubjectKeyIdentifierExtension(AsnEncodedData encodedSubjectKeyIdentifier, bool critical)
		: base(Oids.SubjectKeyIdentifierOid, encodedSubjectKeyIdentifier.RawData, critical)
	{
	}

	public X509SubjectKeyIdentifierExtension(byte[] subjectKeyIdentifier, bool critical)
		: this((ReadOnlySpan<byte>)(subjectKeyIdentifier ?? throw new ArgumentNullException("subjectKeyIdentifier")), critical)
	{
	}

	public X509SubjectKeyIdentifierExtension(ReadOnlySpan<byte> subjectKeyIdentifier, bool critical)
		: base(Oids.SubjectKeyIdentifierOid, EncodeExtension(subjectKeyIdentifier), critical, skipCopy: true)
	{
	}

	public X509SubjectKeyIdentifierExtension(PublicKey key, bool critical)
		: this(key, X509SubjectKeyIdentifierHashAlgorithm.Sha1, critical)
	{
	}

	public X509SubjectKeyIdentifierExtension(PublicKey key, X509SubjectKeyIdentifierHashAlgorithm algorithm, bool critical)
		: base(Oids.SubjectKeyIdentifierOid, EncodeExtension(key, algorithm), critical, skipCopy: true)
	{
	}

	public X509SubjectKeyIdentifierExtension(string subjectKeyIdentifier, bool critical)
		: base(Oids.SubjectKeyIdentifierOid, EncodeExtension(subjectKeyIdentifier), critical, skipCopy: true)
	{
	}

	public override void CopyFrom(AsnEncodedData asnEncodedData)
	{
		base.CopyFrom(asnEncodedData);
		_decoded = false;
	}

	private void Decode(byte[] rawData)
	{
		X509Pal.Instance.DecodeX509SubjectKeyIdentifierExtension(rawData, out _subjectKeyIdentifierBytes);
		_subjectKeyIdentifierString = _subjectKeyIdentifierBytes.ToHexStringUpper();
		_decoded = true;
	}

	private static byte[] EncodeExtension(ReadOnlySpan<byte> subjectKeyIdentifier)
	{
		if (subjectKeyIdentifier.Length == 0)
		{
			throw new ArgumentException(System.SR.Arg_EmptyOrNullArray, "subjectKeyIdentifier");
		}
		return X509Pal.Instance.EncodeX509SubjectKeyIdentifierExtension(subjectKeyIdentifier);
	}

	private static byte[] EncodeExtension(string subjectKeyIdentifier)
	{
		ArgumentNullException.ThrowIfNull(subjectKeyIdentifier, "subjectKeyIdentifier");
		byte[] array = subjectKeyIdentifier.LaxDecodeHexString();
		return EncodeExtension(array);
	}

	private static byte[] EncodeExtension(PublicKey key, X509SubjectKeyIdentifierHashAlgorithm algorithm)
	{
		ArgumentNullException.ThrowIfNull(key, "key");
		byte[] array = GenerateSubjectKeyIdentifierFromPublicKey(key, algorithm);
		return EncodeExtension(array);
	}

	private static byte[] GenerateSubjectKeyIdentifierFromPublicKey(PublicKey key, X509SubjectKeyIdentifierHashAlgorithm algorithm)
	{
		switch (algorithm)
		{
		case X509SubjectKeyIdentifierHashAlgorithm.Sha1:
			return SHA1.HashData(key.EncodedKeyValue.RawData);
		case X509SubjectKeyIdentifierHashAlgorithm.ShortSha1:
		{
			Span<byte> destination = stackalloc byte[20];
			int num = SHA1.HashData(key.EncodedKeyValue.RawData, destination);
			byte[] array = destination.Slice(12).ToArray();
			array[0] &= 15;
			array[0] |= 64;
			return array;
		}
		case X509SubjectKeyIdentifierHashAlgorithm.CapiSha1:
			return X509Pal.Instance.ComputeCapiSha1OfPublicKey(key);
		default:
			throw new ArgumentException(System.SR.Format(System.SR.Arg_EnumIllegalVal, algorithm), "algorithm");
		}
	}
}
