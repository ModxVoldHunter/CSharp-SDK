namespace System.Security.Cryptography.X509Certificates;

public class X509Extension : AsnEncodedData
{
	public bool Critical { get; set; }

	protected X509Extension()
	{
	}

	public X509Extension(AsnEncodedData encodedExtension, bool critical)
		: this(encodedExtension.Oid, encodedExtension.RawData, critical)
	{
	}

	public X509Extension(Oid oid, byte[] rawData, bool critical)
		: this(oid, (ReadOnlySpan<byte>)(rawData ?? throw new ArgumentNullException("rawData")), critical)
	{
	}

	public X509Extension(Oid oid, ReadOnlySpan<byte> rawData, bool critical)
		: base(oid, rawData)
	{
		ArgumentException.ThrowIfNullOrEmpty(base.Oid?.Value, "oid.Value");
		Critical = critical;
	}

	public X509Extension(string oid, byte[] rawData, bool critical)
		: this(new Oid(oid), rawData, critical)
	{
	}

	public X509Extension(string oid, ReadOnlySpan<byte> rawData, bool critical)
		: this(new Oid(oid), rawData, critical)
	{
	}

	internal X509Extension(Oid oid, byte[] rawData, bool critical, bool skipCopy)
		: base(oid, rawData, skipCopy)
	{
		ArgumentException.ThrowIfNullOrEmpty(base.Oid?.Value, "oid.Value");
		Critical = critical;
	}

	public override void CopyFrom(AsnEncodedData asnEncodedData)
	{
		ArgumentNullException.ThrowIfNull(asnEncodedData, "asnEncodedData");
		if (!(asnEncodedData is X509Extension x509Extension))
		{
			throw new ArgumentException(System.SR.Cryptography_X509_ExtensionMismatch);
		}
		base.CopyFrom(asnEncodedData);
		Critical = x509Extension.Critical;
	}

	internal X509Extension(Oid oid)
	{
		base.Oid = oid;
	}
}
