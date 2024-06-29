using System.Buffers;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Security.Cryptography.Asn1.Pkcs12;
using System.Text;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates;

public class X509Certificate : IDisposable, IDeserializationCallback, ISerializable
{
	private volatile byte[] _lazyCertHash;

	private volatile string _lazyIssuer;

	private volatile string _lazySubject;

	private volatile byte[] _lazySerialNumber;

	private volatile string _lazyKeyAlgorithm;

	private volatile byte[] _lazyKeyAlgorithmParameters;

	private volatile byte[] _lazyPublicKey;

	private DateTime _lazyNotBefore = DateTime.MinValue;

	private DateTime _lazyNotAfter = DateTime.MinValue;

	public nint Handle
	{
		get
		{
			if (Pal != null)
			{
				return Pal.Handle;
			}
			return IntPtr.Zero;
		}
	}

	public string Issuer
	{
		get
		{
			ThrowIfInvalid();
			return _lazyIssuer ?? (_lazyIssuer = Pal.Issuer);
		}
	}

	public string Subject
	{
		get
		{
			ThrowIfInvalid();
			return _lazySubject ?? (_lazySubject = Pal.Subject);
		}
	}

	public ReadOnlyMemory<byte> SerialNumberBytes
	{
		get
		{
			ThrowIfInvalid();
			return GetRawSerialNumber();
		}
	}

	internal ICertificatePalCore? Pal { get; private set; }

	public virtual void Reset()
	{
		_lazyCertHash = null;
		_lazyIssuer = null;
		_lazySubject = null;
		_lazySerialNumber = null;
		_lazyKeyAlgorithm = null;
		_lazyKeyAlgorithmParameters = null;
		_lazyPublicKey = null;
		_lazyNotBefore = DateTime.MinValue;
		_lazyNotAfter = DateTime.MinValue;
		ICertificatePalCore pal = Pal;
		if (pal != null)
		{
			Pal = null;
			pal.Dispose();
		}
	}

	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[UnsupportedOSPlatform("browser")]
	public X509Certificate()
	{
	}

	[UnsupportedOSPlatform("browser")]
	public X509Certificate(byte[] data)
		: this(new ReadOnlySpan<byte>(data))
	{
	}

	private protected X509Certificate(ReadOnlySpan<byte> data)
	{
		if (!data.IsEmpty)
		{
			using (SafePasswordHandle password = new SafePasswordHandle((string)null, passwordProvided: false))
			{
				Pal = CertificatePal.FromBlob(data, password, X509KeyStorageFlags.DefaultKeySet);
			}
		}
	}

	[UnsupportedOSPlatform("browser")]
	public X509Certificate(byte[] rawData, string? password)
		: this(rawData, password, X509KeyStorageFlags.DefaultKeySet)
	{
	}

	[UnsupportedOSPlatform("browser")]
	[CLSCompliant(false)]
	public X509Certificate(byte[] rawData, SecureString? password)
		: this(rawData, password, X509KeyStorageFlags.DefaultKeySet)
	{
	}

	[UnsupportedOSPlatform("browser")]
	public X509Certificate(byte[] rawData, string? password, X509KeyStorageFlags keyStorageFlags)
	{
		if (rawData == null || rawData.Length == 0)
		{
			throw new ArgumentException(System.SR.Arg_EmptyOrNullArray, "rawData");
		}
		ValidateKeyStorageFlags(keyStorageFlags);
		using SafePasswordHandle password2 = new SafePasswordHandle(password, passwordProvided: true);
		Pal = CertificatePal.FromBlob(rawData, password2, keyStorageFlags);
	}

	[UnsupportedOSPlatform("browser")]
	[CLSCompliant(false)]
	public X509Certificate(byte[] rawData, SecureString? password, X509KeyStorageFlags keyStorageFlags)
	{
		if (rawData == null || rawData.Length == 0)
		{
			throw new ArgumentException(System.SR.Arg_EmptyOrNullArray, "rawData");
		}
		ValidateKeyStorageFlags(keyStorageFlags);
		using SafePasswordHandle password2 = new SafePasswordHandle(password, passwordProvided: true);
		Pal = CertificatePal.FromBlob(rawData, password2, keyStorageFlags);
	}

	private protected X509Certificate(ReadOnlySpan<byte> rawData, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags)
	{
		if (rawData.IsEmpty)
		{
			throw new ArgumentException(System.SR.Arg_EmptyOrNullArray, "rawData");
		}
		ValidateKeyStorageFlags(keyStorageFlags);
		using SafePasswordHandle password2 = new SafePasswordHandle(password, passwordProvided: true);
		Pal = CertificatePal.FromBlob(rawData, password2, keyStorageFlags);
	}

	[UnsupportedOSPlatform("browser")]
	public X509Certificate(nint handle)
	{
		Pal = CertificatePal.FromHandle(handle);
	}

	internal X509Certificate(ICertificatePalCore pal)
	{
		Pal = pal;
	}

	[UnsupportedOSPlatform("browser")]
	public X509Certificate(string fileName)
		: this(fileName, (string?)null, X509KeyStorageFlags.DefaultKeySet)
	{
	}

	[UnsupportedOSPlatform("browser")]
	public X509Certificate(string fileName, string? password)
		: this(fileName, password, X509KeyStorageFlags.DefaultKeySet)
	{
	}

	[UnsupportedOSPlatform("browser")]
	[CLSCompliant(false)]
	public X509Certificate(string fileName, SecureString? password)
		: this(fileName, password, X509KeyStorageFlags.DefaultKeySet)
	{
	}

	[UnsupportedOSPlatform("browser")]
	public X509Certificate(string fileName, string? password, X509KeyStorageFlags keyStorageFlags)
	{
		ArgumentNullException.ThrowIfNull(fileName, "fileName");
		ValidateKeyStorageFlags(keyStorageFlags);
		using SafePasswordHandle password2 = new SafePasswordHandle(password, passwordProvided: true);
		Pal = CertificatePal.FromFile(fileName, password2, keyStorageFlags);
	}

	private protected X509Certificate(string fileName, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags)
	{
		ArgumentNullException.ThrowIfNull(fileName, "fileName");
		ValidateKeyStorageFlags(keyStorageFlags);
		using SafePasswordHandle password2 = new SafePasswordHandle(password, passwordProvided: true);
		Pal = CertificatePal.FromFile(fileName, password2, keyStorageFlags);
	}

	[UnsupportedOSPlatform("browser")]
	[CLSCompliant(false)]
	public X509Certificate(string fileName, SecureString? password, X509KeyStorageFlags keyStorageFlags)
		: this()
	{
		ArgumentNullException.ThrowIfNull(fileName, "fileName");
		ValidateKeyStorageFlags(keyStorageFlags);
		using SafePasswordHandle password2 = new SafePasswordHandle(password, passwordProvided: true);
		Pal = CertificatePal.FromFile(fileName, password2, keyStorageFlags);
	}

	[UnsupportedOSPlatform("browser")]
	public X509Certificate(X509Certificate cert)
	{
		ArgumentNullException.ThrowIfNull(cert, "cert");
		if (cert.Pal != null)
		{
			Pal = CertificatePal.FromOtherCert(cert);
		}
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public X509Certificate(SerializationInfo info, StreamingContext context)
		: this()
	{
		throw new PlatformNotSupportedException();
	}

	[UnsupportedOSPlatform("browser")]
	public static X509Certificate CreateFromCertFile(string filename)
	{
		return new X509Certificate(filename);
	}

	[UnsupportedOSPlatform("browser")]
	public static X509Certificate CreateFromSignedFile(string filename)
	{
		return new X509Certificate(filename);
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	void IDeserializationCallback.OnDeserialization(object sender)
	{
		throw new PlatformNotSupportedException();
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			Reset();
		}
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is X509Certificate other)
		{
			return Equals(other);
		}
		return false;
	}

	public virtual bool Equals([NotNullWhen(true)] X509Certificate? other)
	{
		if (other == null)
		{
			return false;
		}
		if (Pal == null)
		{
			return other.Pal == null;
		}
		if (!Issuer.Equals(other.Issuer))
		{
			return false;
		}
		ReadOnlySpan<byte> span = GetRawSerialNumber();
		ReadOnlySpan<byte> other2 = other.GetRawSerialNumber();
		return span.SequenceEqual(other2);
	}

	public virtual byte[] Export(X509ContentType contentType)
	{
		return Export(contentType, (string?)null);
	}

	public virtual byte[] Export(X509ContentType contentType, string? password)
	{
		VerifyContentType(contentType);
		if (Pal == null)
		{
			throw new CryptographicException(-2147467261);
		}
		using SafePasswordHandle password2 = new SafePasswordHandle(password, passwordProvided: true);
		return Pal.Export(contentType, password2);
	}

	[CLSCompliant(false)]
	public virtual byte[] Export(X509ContentType contentType, SecureString? password)
	{
		VerifyContentType(contentType);
		if (Pal == null)
		{
			throw new CryptographicException(-2147467261);
		}
		using SafePasswordHandle password2 = new SafePasswordHandle(password, passwordProvided: true);
		return Pal.Export(contentType, password2);
	}

	public virtual string GetRawCertDataString()
	{
		ThrowIfInvalid();
		return GetRawCertData().ToHexStringUpper();
	}

	public virtual byte[] GetCertHash()
	{
		ThrowIfInvalid();
		return GetRawCertHash().CloneByteArray();
	}

	public virtual byte[] GetCertHash(HashAlgorithmName hashAlgorithm)
	{
		ThrowIfInvalid();
		return GetCertHash(hashAlgorithm, Pal);
	}

	private static byte[] GetCertHash(HashAlgorithmName hashAlgorithm, ICertificatePalCore certPal)
	{
		return HashOneShotHelpers.HashData(hashAlgorithm, certPal.RawData);
	}

	public virtual bool TryGetCertHash(HashAlgorithmName hashAlgorithm, Span<byte> destination, out int bytesWritten)
	{
		ThrowIfInvalid();
		return HashOneShotHelpers.TryHashData(hashAlgorithm, Pal.RawData, destination, out bytesWritten);
	}

	public virtual string GetCertHashString()
	{
		ThrowIfInvalid();
		return GetRawCertHash().ToHexStringUpper();
	}

	public virtual string GetCertHashString(HashAlgorithmName hashAlgorithm)
	{
		ThrowIfInvalid();
		return GetCertHashString(hashAlgorithm, Pal);
	}

	internal static string GetCertHashString(HashAlgorithmName hashAlgorithm, ICertificatePalCore certPal)
	{
		return GetCertHash(hashAlgorithm, certPal).ToHexStringUpper();
	}

	private byte[] GetRawCertHash()
	{
		return _lazyCertHash ?? (_lazyCertHash = Pal.Thumbprint);
	}

	public virtual string GetEffectiveDateString()
	{
		return GetNotBefore().ToString();
	}

	public virtual string GetExpirationDateString()
	{
		return GetNotAfter().ToString();
	}

	public virtual string GetFormat()
	{
		return "X509";
	}

	public virtual string GetPublicKeyString()
	{
		return GetPublicKey().ToHexStringUpper();
	}

	public virtual byte[] GetRawCertData()
	{
		ThrowIfInvalid();
		return Pal.RawData.CloneByteArray();
	}

	public override int GetHashCode()
	{
		if (Pal == null)
		{
			return 0;
		}
		byte[] rawCertHash = GetRawCertHash();
		int num = 0;
		for (int i = 0; i < rawCertHash.Length && i < 4; i++)
		{
			num = (num << 8) | rawCertHash[i];
		}
		return num;
	}

	public virtual string GetKeyAlgorithm()
	{
		ThrowIfInvalid();
		return _lazyKeyAlgorithm ?? (_lazyKeyAlgorithm = Pal.KeyAlgorithm);
	}

	public virtual byte[] GetKeyAlgorithmParameters()
	{
		ThrowIfInvalid();
		byte[] src = _lazyKeyAlgorithmParameters ?? (_lazyKeyAlgorithmParameters = Pal.KeyAlgorithmParameters);
		return src.CloneByteArray();
	}

	public virtual string GetKeyAlgorithmParametersString()
	{
		ThrowIfInvalid();
		byte[] keyAlgorithmParameters = GetKeyAlgorithmParameters();
		return keyAlgorithmParameters.ToHexStringUpper();
	}

	public virtual byte[] GetPublicKey()
	{
		ThrowIfInvalid();
		byte[] src = _lazyPublicKey ?? (_lazyPublicKey = Pal.PublicKeyValue);
		return src.CloneByteArray();
	}

	public virtual byte[] GetSerialNumber()
	{
		ThrowIfInvalid();
		byte[] array = GetRawSerialNumber().CloneByteArray();
		Array.Reverse(array);
		return array;
	}

	public virtual string GetSerialNumberString()
	{
		ThrowIfInvalid();
		return GetRawSerialNumber().ToHexStringUpper();
	}

	private byte[] GetRawSerialNumber()
	{
		return _lazySerialNumber ?? (_lazySerialNumber = Pal.SerialNumber);
	}

	[Obsolete("X509Certificate.GetName has been deprecated. Use the Subject property instead.")]
	public virtual string GetName()
	{
		ThrowIfInvalid();
		return Pal.LegacySubject;
	}

	[Obsolete("X509Certificate.GetIssuerName has been deprecated. Use the Issuer property instead.")]
	public virtual string GetIssuerName()
	{
		ThrowIfInvalid();
		return Pal.LegacyIssuer;
	}

	public override string ToString()
	{
		return ToString(fVerbose: false);
	}

	public virtual string ToString(bool fVerbose)
	{
		if (!fVerbose || Pal == null)
		{
			return GetType().ToString();
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[Subject]");
		stringBuilder.Append("  ");
		stringBuilder.AppendLine(Subject);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[Issuer]");
		stringBuilder.Append("  ");
		stringBuilder.AppendLine(Issuer);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[Serial Number]");
		stringBuilder.Append("  ");
		byte[] serialNumber = GetSerialNumber();
		Array.Reverse(serialNumber);
		stringBuilder.Append(serialNumber.ToHexArrayUpper());
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[Not Before]");
		stringBuilder.Append("  ");
		stringBuilder.AppendLine(FormatDate(GetNotBefore()));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[Not After]");
		stringBuilder.Append("  ");
		stringBuilder.AppendLine(FormatDate(GetNotAfter()));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[Thumbprint]");
		stringBuilder.Append("  ");
		stringBuilder.Append(GetRawCertHash().ToHexArrayUpper());
		stringBuilder.AppendLine();
		return stringBuilder.ToString();
	}

	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public virtual void Import(byte[] rawData)
	{
		throw new PlatformNotSupportedException(System.SR.NotSupported_ImmutableX509Certificate);
	}

	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public virtual void Import(byte[] rawData, string? password, X509KeyStorageFlags keyStorageFlags)
	{
		throw new PlatformNotSupportedException(System.SR.NotSupported_ImmutableX509Certificate);
	}

	[CLSCompliant(false)]
	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public virtual void Import(byte[] rawData, SecureString? password, X509KeyStorageFlags keyStorageFlags)
	{
		throw new PlatformNotSupportedException(System.SR.NotSupported_ImmutableX509Certificate);
	}

	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public virtual void Import(string fileName)
	{
		throw new PlatformNotSupportedException(System.SR.NotSupported_ImmutableX509Certificate);
	}

	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public virtual void Import(string fileName, string? password, X509KeyStorageFlags keyStorageFlags)
	{
		throw new PlatformNotSupportedException(System.SR.NotSupported_ImmutableX509Certificate);
	}

	[CLSCompliant(false)]
	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public virtual void Import(string fileName, SecureString? password, X509KeyStorageFlags keyStorageFlags)
	{
		throw new PlatformNotSupportedException(System.SR.NotSupported_ImmutableX509Certificate);
	}

	internal DateTime GetNotAfter()
	{
		ThrowIfInvalid();
		DateTime dateTime = _lazyNotAfter;
		if (dateTime == DateTime.MinValue)
		{
			dateTime = (_lazyNotAfter = Pal.NotAfter);
		}
		return dateTime;
	}

	internal DateTime GetNotBefore()
	{
		ThrowIfInvalid();
		DateTime dateTime = _lazyNotBefore;
		if (dateTime == DateTime.MinValue)
		{
			dateTime = (_lazyNotBefore = Pal.NotBefore);
		}
		return dateTime;
	}

	[MemberNotNull("Pal")]
	internal void ThrowIfInvalid()
	{
		if (Pal == null)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_InvalidHandle, "m_safeCertContext"));
		}
	}

	protected static string FormatDate(DateTime date)
	{
		CultureInfo cultureInfo = CultureInfo.CurrentCulture;
		if (!cultureInfo.DateTimeFormat.Calendar.IsValidDay(date.Year, date.Month, date.Day, 0))
		{
			if (cultureInfo.DateTimeFormat.Calendar is UmAlQuraCalendar)
			{
				cultureInfo = cultureInfo.Clone() as CultureInfo;
				cultureInfo.DateTimeFormat.Calendar = new HijriCalendar();
			}
			else
			{
				cultureInfo = CultureInfo.InvariantCulture;
			}
		}
		return date.ToString(cultureInfo);
	}

	internal static void ValidateKeyStorageFlags(X509KeyStorageFlags keyStorageFlags)
	{
		if (((uint)keyStorageFlags & 0xFFFFFFC0u) != 0)
		{
			throw new ArgumentException(System.SR.Argument_InvalidFlag, "keyStorageFlags");
		}
		X509KeyStorageFlags x509KeyStorageFlags = keyStorageFlags & (X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.EphemeralKeySet);
		if (x509KeyStorageFlags == (X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.EphemeralKeySet))
		{
			throw new ArgumentException(System.SR.Format(System.SR.Cryptography_X509_InvalidFlagCombination, x509KeyStorageFlags), "keyStorageFlags");
		}
	}

	private static void VerifyContentType(X509ContentType contentType)
	{
		if (contentType != X509ContentType.Cert && contentType != X509ContentType.SerializedCert && contentType != X509ContentType.Pfx)
		{
			throw new CryptographicException(System.SR.Cryptography_X509_InvalidContentType);
		}
	}

	internal static void EnforceIterationCountLimit(ref ReadOnlySpan<byte> pkcs12, bool readingFromFile, bool passwordProvided)
	{
		if (readingFromFile || passwordProvided)
		{
			return;
		}
		long num = System.LocalAppContextSwitches.Pkcs12UnspecifiedPasswordIterationLimit;
		if (System.LocalAppContextSwitches.Pkcs12UnspecifiedPasswordIterationLimit == -1)
		{
			return;
		}
		if (num < 0)
		{
			num = 600000L;
		}
		checked
		{
			try
			{
				try
				{
					KdfWorkLimiter.SetIterationLimit((ulong)num);
					int bytesConsumed;
					ulong iterationCount = GetIterationCount(pkcs12, out bytesConsumed);
					pkcs12 = pkcs12.Slice(0, bytesConsumed);
					if (iterationCount > (ulong)num || KdfWorkLimiter.WasWorkLimitExceeded())
					{
						throw new X509IterationCountExceededException();
					}
				}
				finally
				{
					KdfWorkLimiter.ResetIterationLimit();
				}
			}
			catch (X509IterationCountExceededException)
			{
				throw new CryptographicException(System.SR.Cryptography_X509_PfxWithoutPassword_MaxAllowedIterationsExceeded);
			}
			catch (Exception inner)
			{
				throw new CryptographicException(System.SR.Cryptography_X509_PfxWithoutPassword_ProblemFound, inner);
			}
		}
	}

	internal unsafe static ulong GetIterationCount(ReadOnlySpan<byte> pkcs12, out int bytesConsumed)
	{
		ulong result;
		fixed (byte* pointer = pkcs12)
		{
			using PointerMemoryManager<byte> pointerMemoryManager = new PointerMemoryManager<byte>(pointer, pkcs12.Length);
			AsnValueReader reader = new AsnValueReader(pkcs12, AsnEncodingRules.BER);
			int length = reader.PeekEncodedValue().Length;
			PfxAsn.Decode(ref reader, pointerMemoryManager.Memory, out var decoded);
			result = decoded.CountTotalIterations();
			bytesConsumed = length;
		}
		return result;
	}
}
