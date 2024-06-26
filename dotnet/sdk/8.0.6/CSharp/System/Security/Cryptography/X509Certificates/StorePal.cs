using System.IO;
using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates;

internal sealed class StorePal : IDisposable, IStorePal, IExportPal, ILoaderPal
{
	private SafeCertStoreHandle _certStore;

	internal SafeCertStoreHandle SafeCertStoreHandle => _certStore;

	SafeHandle IStorePal.SafeHandle
	{
		get
		{
			if (_certStore == null || _certStore.IsInvalid || _certStore.IsClosed)
			{
				throw new CryptographicException(System.SR.Cryptography_X509_StoreNotOpen);
			}
			return _certStore;
		}
	}

	internal static IStorePal FromHandle(nint storeHandle)
	{
		if (storeHandle == IntPtr.Zero)
		{
			throw new ArgumentNullException("storeHandle");
		}
		SafeCertStoreHandle safeCertStoreHandle = global::Interop.Crypt32.CertDuplicateStore(storeHandle);
		if (safeCertStoreHandle == null || safeCertStoreHandle.IsInvalid)
		{
			safeCertStoreHandle?.Dispose();
			throw new CryptographicException(System.SR.Cryptography_InvalidStoreHandle, "storeHandle");
		}
		return new StorePal(safeCertStoreHandle);
	}

	internal static ILoaderPal FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
	{
		return FromBlobOrFile(rawData, null, password, keyStorageFlags);
	}

	internal static ILoaderPal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
	{
		return FromBlobOrFile(null, fileName, password, keyStorageFlags);
	}

	internal static IExportPal FromCertificate(ICertificatePalCore cert)
	{
		CertificatePal certificatePal = (CertificatePal)cert;
		SafeCertStoreHandle safeCertStoreHandle = global::Interop.crypt32.CertOpenStore(CertStoreProvider.CERT_STORE_PROV_MEMORY, global::Interop.Crypt32.CertEncodingType.All, IntPtr.Zero, global::Interop.Crypt32.CertStoreFlags.CERT_STORE_DEFER_CLOSE_UNTIL_LAST_FREE_FLAG | global::Interop.Crypt32.CertStoreFlags.CERT_STORE_ENUM_ARCHIVED_FLAG | global::Interop.Crypt32.CertStoreFlags.CERT_STORE_CREATE_NEW_FLAG, null);
		using (SafeCertContextHandle pCertContext = certificatePal.GetCertContext())
		{
			if (safeCertStoreHandle.IsInvalid || !global::Interop.Crypt32.CertAddCertificateLinkToStore(safeCertStoreHandle, pCertContext, global::Interop.Crypt32.CertStoreAddDisposition.CERT_STORE_ADD_ALWAYS, IntPtr.Zero))
			{
				Exception ex = Marshal.GetHRForLastWin32Error().ToCryptographicException();
				safeCertStoreHandle.Dispose();
				throw ex;
			}
		}
		return new StorePal(safeCertStoreHandle);
	}

	internal static IExportPal LinkFromCertificateCollection(X509Certificate2Collection certificates)
	{
		SafeCertStoreHandle safeCertStoreHandle = global::Interop.crypt32.CertOpenStore(CertStoreProvider.CERT_STORE_PROV_MEMORY, global::Interop.Crypt32.CertEncodingType.All, IntPtr.Zero, global::Interop.Crypt32.CertStoreFlags.CERT_STORE_ENUM_ARCHIVED_FLAG | global::Interop.Crypt32.CertStoreFlags.CERT_STORE_CREATE_NEW_FLAG, null);
		try
		{
			if (safeCertStoreHandle.IsInvalid)
			{
				throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
			}
			for (int i = 0; i < certificates.Count; i++)
			{
				using SafeCertContextHandle pCertContext = ((CertificatePal)certificates[i].Pal).GetCertContext();
				if (!global::Interop.Crypt32.CertAddCertificateLinkToStore(safeCertStoreHandle, pCertContext, global::Interop.Crypt32.CertStoreAddDisposition.CERT_STORE_ADD_ALWAYS, IntPtr.Zero))
				{
					throw Marshal.GetLastPInvokeError().ToCryptographicException();
				}
			}
			return new StorePal(safeCertStoreHandle);
		}
		catch
		{
			safeCertStoreHandle.Dispose();
			throw;
		}
	}

	internal static IStorePal FromSystemStore(string storeName, StoreLocation storeLocation, OpenFlags openFlags)
	{
		global::Interop.Crypt32.CertStoreFlags dwFlags = MapX509StoreFlags(storeLocation, openFlags);
		SafeCertStoreHandle safeCertStoreHandle = global::Interop.crypt32.CertOpenStore(CertStoreProvider.CERT_STORE_PROV_SYSTEM_W, global::Interop.Crypt32.CertEncodingType.All, IntPtr.Zero, dwFlags, storeName);
		if (safeCertStoreHandle.IsInvalid)
		{
			Exception ex = Marshal.GetLastPInvokeError().ToCryptographicException();
			safeCertStoreHandle.Dispose();
			throw ex;
		}
		global::Interop.Crypt32.CertControlStore(safeCertStoreHandle, global::Interop.Crypt32.CertControlStoreFlags.None, global::Interop.Crypt32.CertControlStoreType.CERT_STORE_CTRL_AUTO_RESYNC, IntPtr.Zero);
		return new StorePal(safeCertStoreHandle);
	}

	public void CloneTo(X509Certificate2Collection collection)
	{
		CopyTo(collection);
	}

	public void CopyTo(X509Certificate2Collection collection)
	{
		SafeCertContextHandle pCertContext = null;
		while (global::Interop.crypt32.CertEnumCertificatesInStore(_certStore, ref pCertContext))
		{
			X509Certificate2 certificate = new X509Certificate2(pCertContext.DangerousGetHandle());
			collection.Add(certificate);
		}
	}

	public void Add(ICertificatePal certificate)
	{
		using SafeCertContextHandle pCertContext = ((CertificatePal)certificate).GetCertContext();
		if (!global::Interop.Crypt32.CertAddCertificateContextToStore(_certStore, pCertContext, global::Interop.Crypt32.CertStoreAddDisposition.CERT_STORE_ADD_REPLACE_EXISTING_INHERIT_PROPERTIES, IntPtr.Zero))
		{
			throw Marshal.GetLastPInvokeError().ToCryptographicException();
		}
	}

	public unsafe void Remove(ICertificatePal certificate)
	{
		using SafeCertContextHandle safeCertContextHandle = ((CertificatePal)certificate).GetCertContext();
		SafeCertContextHandle pCertContext = null;
		global::Interop.Crypt32.CERT_CONTEXT* dangerousCertContext = safeCertContextHandle.DangerousCertContext;
		if (global::Interop.crypt32.CertFindCertificateInStore(_certStore, global::Interop.Crypt32.CertFindType.CERT_FIND_EXISTING, dangerousCertContext, ref pCertContext))
		{
			global::Interop.Crypt32.CERT_CONTEXT* pCertContext2 = pCertContext.Disconnect();
			pCertContext.Dispose();
			if (!global::Interop.Crypt32.CertDeleteCertificateFromStore(pCertContext2))
			{
				throw Marshal.GetLastPInvokeError().ToCryptographicException();
			}
		}
	}

	public void Dispose()
	{
		SafeCertStoreHandle certStore = _certStore;
		if (certStore != null)
		{
			_certStore = null;
			certStore.Dispose();
		}
	}

	internal StorePal(SafeCertStoreHandle certStore)
	{
		_certStore = certStore;
	}

	public void MoveTo(X509Certificate2Collection collection)
	{
		CopyTo(collection);
		Dispose();
	}

	public unsafe byte[] Export(X509ContentType contentType, SafePasswordHandle password)
	{
		switch (contentType)
		{
		case X509ContentType.Cert:
		{
			SafeCertContextHandle pCertContext = null;
			if (!global::Interop.crypt32.CertEnumCertificatesInStore(_certStore, ref pCertContext))
			{
				return null;
			}
			try
			{
				byte[] array2 = new byte[pCertContext.DangerousCertContext->cbCertEncoded];
				Marshal.Copy((nint)pCertContext.DangerousCertContext->pbCertEncoded, array2, 0, array2.Length);
				GC.KeepAlive(pCertContext);
				return array2;
			}
			finally
			{
				pCertContext.Dispose();
			}
		}
		case X509ContentType.SerializedCert:
		{
			SafeCertContextHandle pCertContext2 = null;
			if (!global::Interop.crypt32.CertEnumCertificatesInStore(_certStore, ref pCertContext2))
			{
				return null;
			}
			try
			{
				int pcbElement = 0;
				if (!global::Interop.Crypt32.CertSerializeCertificateStoreElement(pCertContext2, 0, null, ref pcbElement))
				{
					throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
				}
				byte[] array3 = new byte[pcbElement];
				if (!global::Interop.Crypt32.CertSerializeCertificateStoreElement(pCertContext2, 0, array3, ref pcbElement))
				{
					throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
				}
				return array3;
			}
			finally
			{
				pCertContext2.Dispose();
			}
		}
		case X509ContentType.Pfx:
		{
			global::Interop.Crypt32.DATA_BLOB pPFX = new global::Interop.Crypt32.DATA_BLOB(IntPtr.Zero, 0u);
			if (!global::Interop.Crypt32.PFXExportCertStore(_certStore, ref pPFX, password, global::Interop.Crypt32.PFXExportFlags.REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY | global::Interop.Crypt32.PFXExportFlags.EXPORT_PRIVATE_KEYS))
			{
				throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
			}
			byte[] array = new byte[pPFX.cbData];
			fixed (byte* value = array)
			{
				pPFX.pbData = new IntPtr(value);
				if (!global::Interop.Crypt32.PFXExportCertStore(_certStore, ref pPFX, password, global::Interop.Crypt32.PFXExportFlags.REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY | global::Interop.Crypt32.PFXExportFlags.EXPORT_PRIVATE_KEYS))
				{
					throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
				}
			}
			return array;
		}
		case X509ContentType.SerializedStore:
			return SaveToMemoryStore(global::Interop.Crypt32.CertStoreSaveAs.CERT_STORE_SAVE_AS_STORE);
		case X509ContentType.Pkcs7:
			return SaveToMemoryStore(global::Interop.Crypt32.CertStoreSaveAs.CERT_STORE_SAVE_AS_PKCS7);
		default:
			throw new CryptographicException(System.SR.Cryptography_X509_InvalidContentType);
		}
	}

	private unsafe byte[] SaveToMemoryStore(global::Interop.Crypt32.CertStoreSaveAs dwSaveAs)
	{
		global::Interop.Crypt32.DATA_BLOB pvSaveToPara = new global::Interop.Crypt32.DATA_BLOB(IntPtr.Zero, 0u);
		if (!global::Interop.Crypt32.CertSaveStore(_certStore, global::Interop.Crypt32.CertEncodingType.All, dwSaveAs, global::Interop.Crypt32.CertStoreSaveTo.CERT_STORE_SAVE_TO_MEMORY, ref pvSaveToPara, 0))
		{
			throw Marshal.GetLastPInvokeError().ToCryptographicException();
		}
		byte[] array = new byte[pvSaveToPara.cbData];
		fixed (byte* value = array)
		{
			pvSaveToPara.pbData = new IntPtr(value);
			if (!global::Interop.Crypt32.CertSaveStore(_certStore, global::Interop.Crypt32.CertEncodingType.All, dwSaveAs, global::Interop.Crypt32.CertStoreSaveTo.CERT_STORE_SAVE_TO_MEMORY, ref pvSaveToPara, 0))
			{
				throw Marshal.GetLastPInvokeError().ToCryptographicException();
			}
		}
		if (array.Length != pvSaveToPara.cbData)
		{
			return array[0..(int)pvSaveToPara.cbData];
		}
		return array;
	}

	private unsafe static StorePal FromBlobOrFile(ReadOnlySpan<byte> rawData, string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
	{
		bool flag = fileName != null;
		fixed (byte* value = rawData)
		{
			fixed (char* ptr = fileName)
			{
				global::Interop.Crypt32.DATA_BLOB dATA_BLOB = new global::Interop.Crypt32.DATA_BLOB(new IntPtr(value), (!flag) ? ((uint)rawData.Length) : 0u);
				bool flag2 = (keyStorageFlags & X509KeyStorageFlags.PersistKeySet) != 0;
				global::Interop.Crypt32.PfxCertStoreFlags dwFlags = MapKeyStorageFlags(keyStorageFlags);
				void* pvObject = (flag ? ((void*)ptr) : ((void*)(&dATA_BLOB)));
				if (!global::Interop.Crypt32.CryptQueryObject(flag ? global::Interop.Crypt32.CertQueryObjectType.CERT_QUERY_OBJECT_FILE : global::Interop.Crypt32.CertQueryObjectType.CERT_QUERY_OBJECT_BLOB, pvObject, global::Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_CERT | global::Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_SERIALIZED_STORE | global::Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT | global::Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED | global::Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_UNSIGNED | global::Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED | global::Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PFX, global::Interop.Crypt32.ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_ALL, 0, IntPtr.Zero, out var pdwContentType, IntPtr.Zero, out var phCertStore, IntPtr.Zero, IntPtr.Zero))
				{
					Exception ex = Marshal.GetLastPInvokeError().ToCryptographicException();
					phCertStore.Dispose();
					throw ex;
				}
				if (pdwContentType == global::Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_PFX)
				{
					phCertStore.Dispose();
					if (flag)
					{
						rawData = File.ReadAllBytes(fileName);
					}
					else
					{
						X509Certificate.EnforceIterationCountLimit(ref rawData, readingFromFile: false, password.PasswordProvided);
					}
					fixed (byte* value2 = rawData)
					{
						global::Interop.Crypt32.DATA_BLOB pPFX = new global::Interop.Crypt32.DATA_BLOB(new IntPtr(value2), (uint)rawData.Length);
						phCertStore = global::Interop.Crypt32.PFXImportCertStore(ref pPFX, password, dwFlags);
						if (phCertStore == null || phCertStore.IsInvalid)
						{
							Exception ex2 = Marshal.GetLastPInvokeError().ToCryptographicException();
							phCertStore?.Dispose();
							throw ex2;
						}
					}
					if (!flag2)
					{
						SafeCertContextHandle pCertContext = null;
						while (global::Interop.crypt32.CertEnumCertificatesInStore(phCertStore, ref pCertContext))
						{
							global::Interop.Crypt32.DATA_BLOB dATA_BLOB2 = new global::Interop.Crypt32.DATA_BLOB(IntPtr.Zero, 0u);
							if (!global::Interop.Crypt32.CertSetCertificateContextProperty(pCertContext, global::Interop.Crypt32.CertContextPropId.CERT_CLR_DELETE_KEY_PROP_ID, global::Interop.Crypt32.CertSetPropertyFlags.CERT_SET_PROPERTY_INHIBIT_PERSIST_FLAG, &dATA_BLOB2))
							{
								Exception ex3 = Marshal.GetLastPInvokeError().ToCryptographicException();
								phCertStore.Dispose();
								throw ex3;
							}
						}
					}
				}
				return new StorePal(phCertStore);
			}
		}
	}

	private static global::Interop.Crypt32.PfxCertStoreFlags MapKeyStorageFlags(X509KeyStorageFlags keyStorageFlags)
	{
		global::Interop.Crypt32.PfxCertStoreFlags pfxCertStoreFlags = global::Interop.Crypt32.PfxCertStoreFlags.None;
		if ((keyStorageFlags & X509KeyStorageFlags.UserKeySet) == X509KeyStorageFlags.UserKeySet)
		{
			pfxCertStoreFlags |= global::Interop.Crypt32.PfxCertStoreFlags.CRYPT_USER_KEYSET;
		}
		else if ((keyStorageFlags & X509KeyStorageFlags.MachineKeySet) == X509KeyStorageFlags.MachineKeySet)
		{
			pfxCertStoreFlags |= global::Interop.Crypt32.PfxCertStoreFlags.CRYPT_MACHINE_KEYSET;
		}
		if ((keyStorageFlags & X509KeyStorageFlags.Exportable) == X509KeyStorageFlags.Exportable)
		{
			pfxCertStoreFlags |= global::Interop.Crypt32.PfxCertStoreFlags.CRYPT_EXPORTABLE;
		}
		if ((keyStorageFlags & X509KeyStorageFlags.UserProtected) == X509KeyStorageFlags.UserProtected)
		{
			pfxCertStoreFlags |= global::Interop.Crypt32.PfxCertStoreFlags.CRYPT_USER_PROTECTED;
		}
		if ((keyStorageFlags & X509KeyStorageFlags.EphemeralKeySet) == X509KeyStorageFlags.EphemeralKeySet)
		{
			pfxCertStoreFlags |= global::Interop.Crypt32.PfxCertStoreFlags.PKCS12_ALWAYS_CNG_KSP | global::Interop.Crypt32.PfxCertStoreFlags.PKCS12_NO_PERSIST_KEY;
		}
		return pfxCertStoreFlags;
	}

	private static global::Interop.Crypt32.CertStoreFlags MapX509StoreFlags(StoreLocation storeLocation, OpenFlags flags)
	{
		global::Interop.Crypt32.CertStoreFlags certStoreFlags = global::Interop.Crypt32.CertStoreFlags.None;
		switch ((uint)(flags & (OpenFlags.ReadWrite | OpenFlags.MaxAllowed)))
		{
		case 0u:
			certStoreFlags |= global::Interop.Crypt32.CertStoreFlags.CERT_STORE_READONLY_FLAG;
			break;
		case 2u:
			certStoreFlags |= global::Interop.Crypt32.CertStoreFlags.CERT_STORE_MAXIMUM_ALLOWED_FLAG;
			break;
		}
		if ((flags & OpenFlags.OpenExistingOnly) == OpenFlags.OpenExistingOnly)
		{
			certStoreFlags |= global::Interop.Crypt32.CertStoreFlags.CERT_STORE_OPEN_EXISTING_FLAG;
		}
		if ((flags & OpenFlags.IncludeArchived) == OpenFlags.IncludeArchived)
		{
			certStoreFlags |= global::Interop.Crypt32.CertStoreFlags.CERT_STORE_ENUM_ARCHIVED_FLAG;
		}
		switch (storeLocation)
		{
		case StoreLocation.LocalMachine:
			certStoreFlags |= global::Interop.Crypt32.CertStoreFlags.CERT_SYSTEM_STORE_LOCAL_MACHINE;
			break;
		case StoreLocation.CurrentUser:
			certStoreFlags |= global::Interop.Crypt32.CertStoreFlags.CERT_SYSTEM_STORE_CURRENT_USER;
			break;
		}
		return certStoreFlags;
	}
}
