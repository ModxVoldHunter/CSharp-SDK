namespace System.Security.Cryptography.X509Certificates;

public sealed class X509BasicConstraintsExtension : X509Extension
{
	private bool _certificateAuthority;

	private bool _hasPathLenConstraint;

	private int _pathLenConstraint;

	private bool _decoded;

	public bool CertificateAuthority
	{
		get
		{
			if (!_decoded)
			{
				DecodeExtension();
			}
			return _certificateAuthority;
		}
	}

	public bool HasPathLengthConstraint
	{
		get
		{
			if (!_decoded)
			{
				DecodeExtension();
			}
			return _hasPathLenConstraint;
		}
	}

	public int PathLengthConstraint
	{
		get
		{
			if (!_decoded)
			{
				DecodeExtension();
			}
			return _pathLenConstraint;
		}
	}

	public X509BasicConstraintsExtension()
		: base(Oids.BasicConstraints2Oid)
	{
		_decoded = true;
	}

	public X509BasicConstraintsExtension(bool certificateAuthority, bool hasPathLengthConstraint, int pathLengthConstraint, bool critical)
		: base(Oids.BasicConstraints2Oid, EncodeExtension(certificateAuthority, hasPathLengthConstraint, pathLengthConstraint), critical, skipCopy: true)
	{
	}

	public X509BasicConstraintsExtension(AsnEncodedData encodedBasicConstraints, bool critical)
		: base(Oids.BasicConstraints2Oid, encodedBasicConstraints.RawData, critical)
	{
	}

	public override void CopyFrom(AsnEncodedData asnEncodedData)
	{
		base.CopyFrom(asnEncodedData);
		_decoded = false;
	}

	public static X509BasicConstraintsExtension CreateForCertificateAuthority(int? pathLengthConstraint = null)
	{
		return new X509BasicConstraintsExtension(certificateAuthority: true, pathLengthConstraint.HasValue, pathLengthConstraint.GetValueOrDefault(), critical: true);
	}

	public static X509BasicConstraintsExtension CreateForEndEntity(bool critical = false)
	{
		return new X509BasicConstraintsExtension(certificateAuthority: false, hasPathLengthConstraint: false, 0, critical);
	}

	private static byte[] EncodeExtension(bool certificateAuthority, bool hasPathLengthConstraint, int pathLengthConstraint)
	{
		if (hasPathLengthConstraint)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(pathLengthConstraint, "pathLengthConstraint");
		}
		return X509Pal.Instance.EncodeX509BasicConstraints2Extension(certificateAuthority, hasPathLengthConstraint, pathLengthConstraint);
	}

	private void DecodeExtension()
	{
		if (base.Oid.Value == "2.5.29.10")
		{
			X509Pal.Instance.DecodeX509BasicConstraintsExtension(base.RawData, out _certificateAuthority, out _hasPathLenConstraint, out _pathLenConstraint);
		}
		else
		{
			X509Pal.Instance.DecodeX509BasicConstraints2Extension(base.RawData, out _certificateAuthority, out _hasPathLenConstraint, out _pathLenConstraint);
		}
		_decoded = true;
	}
}
