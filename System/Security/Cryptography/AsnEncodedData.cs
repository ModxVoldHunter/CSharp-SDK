using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;

namespace System.Security.Cryptography;

public class AsnEncodedData
{
	private Oid _oid;

	private byte[] _rawData;

	public Oid? Oid
	{
		get
		{
			return _oid;
		}
		set
		{
			_oid = value;
		}
	}

	public byte[] RawData
	{
		get
		{
			return _rawData;
		}
		[MemberNotNull("_rawData")]
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			_rawData = value.CloneByteArray();
		}
	}

	protected AsnEncodedData()
	{
		_rawData = Array.Empty<byte>();
	}

	public AsnEncodedData(byte[] rawData)
	{
		Reset(null, rawData);
	}

	public AsnEncodedData(ReadOnlySpan<byte> rawData)
	{
		Reset(null, rawData);
	}

	public AsnEncodedData(AsnEncodedData asnEncodedData)
	{
		ArgumentNullException.ThrowIfNull(asnEncodedData, "asnEncodedData");
		Reset(asnEncodedData._oid, asnEncodedData._rawData);
	}

	public AsnEncodedData(Oid? oid, byte[] rawData)
		: this(oid, rawData, skipCopy: false)
	{
	}

	public AsnEncodedData(string oid, byte[] rawData)
		: this(new Oid(oid), rawData, skipCopy: false)
	{
	}

	public AsnEncodedData(Oid? oid, ReadOnlySpan<byte> rawData)
	{
		Reset(oid, rawData);
	}

	public AsnEncodedData(string oid, ReadOnlySpan<byte> rawData)
	{
		Reset(new Oid(oid), rawData);
	}

	internal AsnEncodedData(Oid oid, byte[] rawData, bool skipCopy)
	{
		if (skipCopy)
		{
			ArgumentNullException.ThrowIfNull(rawData, "rawData");
			Oid = oid;
			_rawData = rawData;
		}
		else
		{
			Reset(oid, rawData);
		}
	}

	public virtual void CopyFrom(AsnEncodedData asnEncodedData)
	{
		ArgumentNullException.ThrowIfNull(asnEncodedData, "asnEncodedData");
		Reset(asnEncodedData._oid, asnEncodedData._rawData);
	}

	public virtual string Format(bool multiLine)
	{
		if (_rawData == null || _rawData.Length == 0)
		{
			return string.Empty;
		}
		return AsnFormatter.Instance.Format(_oid, _rawData, multiLine);
	}

	[MemberNotNull("_rawData")]
	private void Reset(Oid oid, byte[] rawData)
	{
		Oid = oid;
		RawData = rawData;
	}

	[MemberNotNull("_rawData")]
	private void Reset(Oid oid, ReadOnlySpan<byte> rawData)
	{
		Oid = oid;
		_rawData = rawData.ToArray();
	}
}
