using System.Collections;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates.Asn1;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates;

public class X509Certificate2Collection : X509CertificateCollection, IEnumerable<X509Certificate2>, IEnumerable
{
	public new X509Certificate2 this[int index]
	{
		get
		{
			return (X509Certificate2)base[index];
		}
		set
		{
			base[index] = value;
		}
	}

	public X509Certificate2Collection()
	{
	}

	public X509Certificate2Collection(X509Certificate2 certificate)
	{
		Add(certificate);
	}

	public X509Certificate2Collection(X509Certificate2[] certificates)
	{
		AddRange(certificates);
	}

	public X509Certificate2Collection(X509Certificate2Collection certificates)
	{
		AddRange(certificates);
	}

	public int Add(X509Certificate2 certificate)
	{
		ArgumentNullException.ThrowIfNull(certificate, "certificate");
		return Add((X509Certificate)certificate);
	}

	public void AddRange(X509Certificate2[] certificates)
	{
		ArgumentNullException.ThrowIfNull(certificates, "certificates");
		int i = 0;
		try
		{
			for (; i < certificates.Length; i++)
			{
				Add(certificates[i]);
			}
		}
		catch
		{
			for (int j = 0; j < i; j++)
			{
				Remove(certificates[j]);
			}
			throw;
		}
	}

	public void AddRange(X509Certificate2Collection certificates)
	{
		ArgumentNullException.ThrowIfNull(certificates, "certificates");
		int i = 0;
		try
		{
			for (; i < certificates.Count; i++)
			{
				Add(certificates[i]);
			}
		}
		catch
		{
			for (int j = 0; j < i; j++)
			{
				Remove(certificates[j]);
			}
			throw;
		}
	}

	public bool Contains(X509Certificate2 certificate)
	{
		return Contains((X509Certificate)certificate);
	}

	public byte[]? Export(X509ContentType contentType)
	{
		using SafePasswordHandle password = new SafePasswordHandle((string)null, passwordProvided: false);
		using IExportPal exportPal = StorePal.LinkFromCertificateCollection(this);
		return exportPal.Export(contentType, password);
	}

	public byte[]? Export(X509ContentType contentType, string? password)
	{
		using SafePasswordHandle password2 = new SafePasswordHandle(password, passwordProvided: true);
		using IExportPal exportPal = StorePal.LinkFromCertificateCollection(this);
		return exportPal.Export(contentType, password2);
	}

	public X509Certificate2Collection Find(X509FindType findType, object findValue, bool validOnly)
	{
		ArgumentNullException.ThrowIfNull(findValue, "findValue");
		return FindPal.FindFromCollection(this, findType, findValue, validOnly);
	}

	public new X509Certificate2Enumerator GetEnumerator()
	{
		return new X509Certificate2Enumerator(this);
	}

	IEnumerator<X509Certificate2> IEnumerable<X509Certificate2>.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Import(byte[] rawData)
	{
		ArgumentNullException.ThrowIfNull(rawData, "rawData");
		Import(rawData.AsSpan());
	}

	public void Import(ReadOnlySpan<byte> rawData)
	{
		using SafePasswordHandle password = new SafePasswordHandle((string)null, passwordProvided: false);
		using ILoaderPal loaderPal = StorePal.FromBlob(rawData, password, X509KeyStorageFlags.DefaultKeySet);
		loaderPal.MoveTo(this);
	}

	public void Import(byte[] rawData, string? password, X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.DefaultKeySet)
	{
		ArgumentNullException.ThrowIfNull(rawData, "rawData");
		Import(rawData.AsSpan(), password.AsSpan(), keyStorageFlags);
	}

	public void Import(ReadOnlySpan<byte> rawData, string? password, X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.DefaultKeySet)
	{
		Import(rawData, password.AsSpan(), keyStorageFlags);
	}

	public void Import(ReadOnlySpan<byte> rawData, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.DefaultKeySet)
	{
		X509Certificate.ValidateKeyStorageFlags(keyStorageFlags);
		using SafePasswordHandle password2 = new SafePasswordHandle(password, passwordProvided: true);
		using ILoaderPal loaderPal = StorePal.FromBlob(rawData, password2, keyStorageFlags);
		loaderPal.MoveTo(this);
	}

	public void Import(string fileName)
	{
		ArgumentNullException.ThrowIfNull(fileName, "fileName");
		using SafePasswordHandle password = new SafePasswordHandle((string)null, passwordProvided: false);
		using ILoaderPal loaderPal = StorePal.FromFile(fileName, password, X509KeyStorageFlags.DefaultKeySet);
		loaderPal.MoveTo(this);
	}

	public void Import(string fileName, string? password, X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.DefaultKeySet)
	{
		ArgumentNullException.ThrowIfNull(fileName, "fileName");
		X509Certificate.ValidateKeyStorageFlags(keyStorageFlags);
		using SafePasswordHandle password2 = new SafePasswordHandle(password, passwordProvided: true);
		using ILoaderPal loaderPal = StorePal.FromFile(fileName, password2, keyStorageFlags);
		loaderPal.MoveTo(this);
	}

	public void Import(string fileName, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.DefaultKeySet)
	{
		ArgumentNullException.ThrowIfNull(fileName, "fileName");
		X509Certificate.ValidateKeyStorageFlags(keyStorageFlags);
		using SafePasswordHandle password2 = new SafePasswordHandle(password, passwordProvided: true);
		using ILoaderPal loaderPal = StorePal.FromFile(fileName, password2, keyStorageFlags);
		loaderPal.MoveTo(this);
	}

	public void Insert(int index, X509Certificate2 certificate)
	{
		ArgumentNullException.ThrowIfNull(certificate, "certificate");
		Insert(index, (X509Certificate)certificate);
	}

	public void Remove(X509Certificate2 certificate)
	{
		ArgumentNullException.ThrowIfNull(certificate, "certificate");
		Remove((X509Certificate)certificate);
	}

	public void RemoveRange(X509Certificate2[] certificates)
	{
		ArgumentNullException.ThrowIfNull(certificates, "certificates");
		int i = 0;
		try
		{
			for (; i < certificates.Length; i++)
			{
				Remove(certificates[i]);
			}
		}
		catch
		{
			for (int j = 0; j < i; j++)
			{
				Add(certificates[j]);
			}
			throw;
		}
	}

	public void RemoveRange(X509Certificate2Collection certificates)
	{
		ArgumentNullException.ThrowIfNull(certificates, "certificates");
		int i = 0;
		try
		{
			for (; i < certificates.Count; i++)
			{
				Remove(certificates[i]);
			}
		}
		catch
		{
			for (int j = 0; j < i; j++)
			{
				Add(certificates[j]);
			}
			throw;
		}
	}

	public void ImportFromPemFile(string certPemFilePath)
	{
		ArgumentNullException.ThrowIfNull(certPemFilePath, "certPemFilePath");
		ReadOnlySpan<char> certPem = File.ReadAllText(certPemFilePath);
		ImportFromPem(certPem);
	}

	public void ImportFromPem(ReadOnlySpan<char> certPem)
	{
		int num = 0;
		try
		{
			PemEnumerator.Enumerator enumerator = new PemEnumerator(certPem).GetEnumerator();
			while (enumerator.MoveNext())
			{
				enumerator.Current.Deconstruct(out var contents, out var pemFields);
				ReadOnlySpan<char> readOnlySpan = contents;
				PemFields pemFields2 = pemFields;
				Range label = pemFields2.Label;
				ReadOnlySpan<char> span = readOnlySpan[label.Start..label.End];
				if (span.SequenceEqual("CERTIFICATE"))
				{
					byte[] array = GC.AllocateUninitializedArray<byte>(pemFields2.DecodedDataLength);
					label = pemFields2.Base64Data;
					if (!Convert.TryFromBase64Chars(readOnlySpan[label.Start..label.End], array, out var bytesWritten) || bytesWritten != pemFields2.DecodedDataLength)
					{
						throw new CryptographicException(System.SR.Cryptography_X509_NoPemCertificate);
					}
					try
					{
						CertificateAsn.Decode(array, AsnEncodingRules.DER);
					}
					catch (CryptographicException)
					{
						throw new CryptographicException(System.SR.Cryptography_X509_NoPemCertificate);
					}
					Import(array);
					num++;
				}
			}
		}
		catch
		{
			for (int i = 0; i < num; i++)
			{
				RemoveAt(base.Count - 1);
			}
			throw;
		}
	}

	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public string ExportPkcs7Pem()
	{
		byte[] array = Export(X509ContentType.Pkcs7);
		if (array == null)
		{
			throw new CryptographicException(System.SR.Cryptography_X509_ExportFailed);
		}
		return PemEncoding.WriteString("PKCS7", array);
	}

	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public bool TryExportPkcs7Pem(Span<char> destination, out int charsWritten)
	{
		byte[] array = Export(X509ContentType.Pkcs7);
		if (array == null)
		{
			throw new CryptographicException(System.SR.Cryptography_X509_ExportFailed);
		}
		return PemEncoding.TryWrite("PKCS7", array, destination, out charsWritten);
	}

	public string ExportCertificatePems()
	{
		int certificatePemsSize = GetCertificatePemsSize();
		return string.Create(certificatePemsSize, this, delegate(Span<char> destination, X509Certificate2Collection col)
		{
			if (!col.TryExportCertificatePems(destination, out var charsWritten) || charsWritten != destination.Length)
			{
				throw new CryptographicException();
			}
		});
	}

	public bool TryExportCertificatePems(Span<char> destination, out int charsWritten)
	{
		Span<char> destination2 = destination;
		int num = 0;
		for (int i = 0; i < base.Count; i++)
		{
			ReadOnlyMemory<byte> rawDataMemory = this[i].RawDataMemory;
			int encodedSize = PemEncoding.GetEncodedSize("CERTIFICATE".Length, rawDataMemory.Length);
			if (destination2.Length < encodedSize)
			{
				charsWritten = 0;
				return false;
			}
			if (!PemEncoding.TryWrite("CERTIFICATE", rawDataMemory.Span, destination2, out var charsWritten2) || charsWritten2 != encodedSize)
			{
				throw new CryptographicException();
			}
			destination2 = destination2.Slice(charsWritten2);
			num += charsWritten2;
			if (i < base.Count - 1)
			{
				if (destination2.IsEmpty)
				{
					charsWritten = 0;
					return false;
				}
				destination2[0] = '\n';
				destination2 = destination2.Slice(1);
				num++;
			}
		}
		charsWritten = num;
		return true;
	}

	private int GetCertificatePemsSize()
	{
		int num = 0;
		checked
		{
			for (int i = 0; i < base.Count; i++)
			{
				num += PemEncoding.GetEncodedSize("CERTIFICATE".Length, this[i].RawDataMemory.Length);
				if (i < base.Count - 1)
				{
					num++;
				}
			}
			return num;
		}
	}
}
