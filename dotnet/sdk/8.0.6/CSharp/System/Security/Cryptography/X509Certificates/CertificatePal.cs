using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates;

internal sealed class CertificatePal : IDisposable, ICertificatePal, ICertificatePalCore
{
	private unsafe delegate T CertContextCallback<T>(global::Interop.Crypt32.CERT_CONTEXT* certContext);

	private SafeCertContextHandle _certContext;

	public nint Handle => _certContext.DangerousGetHandle();

	public string Issuer => GetIssuerOrSubject(issuer: true, reverse: true);

	public string Subject => GetIssuerOrSubject(issuer: false, reverse: true);

	public string LegacyIssuer => GetIssuerOrSubject(issuer: true, reverse: false);

	public string LegacySubject => GetIssuerOrSubject(issuer: false, reverse: false);

	public byte[] Thumbprint
	{
		get
		{
			int pcbData = 0;
			if (!global::Interop.Crypt32.CertGetCertificateContextProperty(_certContext, global::Interop.Crypt32.CertContextPropId.CERT_SHA1_HASH_PROP_ID, null, ref pcbData))
			{
				throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
			}
			byte[] array = new byte[pcbData];
			if (!global::Interop.Crypt32.CertGetCertificateContextProperty(_certContext, global::Interop.Crypt32.CertContextPropId.CERT_SHA1_HASH_PROP_ID, array, ref pcbData))
			{
				throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
			}
			return array;
		}
	}

	public unsafe string KeyAlgorithm => InvokeWithCertContext((global::Interop.Crypt32.CERT_CONTEXT* certContext) => Marshal.PtrToStringAnsi(certContext->pCertInfo->SubjectPublicKeyInfo.Algorithm.pszObjId));

	public unsafe byte[] KeyAlgorithmParameters => InvokeWithCertContext(delegate(global::Interop.Crypt32.CERT_CONTEXT* pCertContext)
	{
		string text = Marshal.PtrToStringAnsi(pCertContext->pCertInfo->SubjectPublicKeyInfo.Algorithm.pszObjId);
		int num = ((!(text == "1.2.840.113549.1.1.1")) ? global::Interop.Crypt32.FindOidInfo(global::Interop.Crypt32.CryptOidInfoKeyType.CRYPT_OID_INFO_OID_KEY, text, OidGroup.PublicKeyAlgorithm, fallBackToAllGroups: true).AlgId : 41984);
		byte* ptr = (byte*)5;
		return (num == 8704 && pCertContext->pCertInfo->SubjectPublicKeyInfo.Algorithm.Parameters.cbData == 0 && ((IntPtr*)(&pCertContext->pCertInfo->SubjectPublicKeyInfo.Algorithm.Parameters.pbData))->ToPointer() == ptr) ? PropagateKeyAlgorithmParametersFromChain() : pCertContext->pCertInfo->SubjectPublicKeyInfo.Algorithm.Parameters.ToByteArray();
	});

	public unsafe byte[] PublicKeyValue => InvokeWithCertContext((global::Interop.Crypt32.CERT_CONTEXT* pCertContext) => pCertContext->pCertInfo->SubjectPublicKeyInfo.PublicKey.ToByteArray());

	public unsafe byte[] SerialNumber => InvokeWithCertContext(delegate(global::Interop.Crypt32.CERT_CONTEXT* pCertContext)
	{
		byte[] array = pCertContext->pCertInfo->SerialNumber.ToByteArray();
		Array.Reverse(array);
		return array;
	});

	public unsafe string SignatureAlgorithm => InvokeWithCertContext((global::Interop.Crypt32.CERT_CONTEXT* pCertContext) => Marshal.PtrToStringAnsi(pCertContext->pCertInfo->SignatureAlgorithm.pszObjId));

	public unsafe DateTime NotAfter => InvokeWithCertContext((global::Interop.Crypt32.CERT_CONTEXT* pCertContext) => pCertContext->pCertInfo->NotAfter.ToDateTime());

	public unsafe DateTime NotBefore => InvokeWithCertContext((global::Interop.Crypt32.CERT_CONTEXT* pCertContext) => pCertContext->pCertInfo->NotBefore.ToDateTime());

	public unsafe byte[] RawData => InvokeWithCertContext((global::Interop.Crypt32.CERT_CONTEXT* pCertContext) => new Span<byte>(pCertContext->pbCertEncoded, pCertContext->cbCertEncoded).ToArray());

	public unsafe int Version => InvokeWithCertContext((global::Interop.Crypt32.CERT_CONTEXT* pCertContext) => pCertContext->pCertInfo->dwVersion + 1);

	public unsafe bool Archived
	{
		get
		{
			int pcbData = 0;
			return global::Interop.Crypt32.CertGetCertificateContextProperty(_certContext, global::Interop.Crypt32.CertContextPropId.CERT_ARCHIVED_PROP_ID, null, ref pcbData);
		}
		set
		{
			global::Interop.Crypt32.DATA_BLOB dATA_BLOB = new global::Interop.Crypt32.DATA_BLOB(IntPtr.Zero, 0u);
			global::Interop.Crypt32.DATA_BLOB* pvData = (value ? (&dATA_BLOB) : null);
			if (!global::Interop.Crypt32.CertSetCertificateContextProperty(_certContext, global::Interop.Crypt32.CertContextPropId.CERT_ARCHIVED_PROP_ID, global::Interop.Crypt32.CertSetPropertyFlags.None, pvData))
			{
				throw Marshal.GetLastPInvokeError().ToCryptographicException();
			}
		}
	}

	public unsafe string FriendlyName
	{
		get
		{
			uint pcbData = 0u;
			if (!global::Interop.Crypt32.CertGetCertificateContextPropertyString(_certContext, global::Interop.Crypt32.CertContextPropId.CERT_FRIENDLY_NAME_PROP_ID, null, ref pcbData))
			{
				return string.Empty;
			}
			uint num = (pcbData + 1) / 2;
			Span<char> span = ((num > 256) ? ((Span<char>)new char[num]) : stackalloc char[(int)num]);
			Span<char> span2 = span;
			fixed (char* pvData = &MemoryMarshal.GetReference(span2))
			{
				if (!global::Interop.Crypt32.CertGetCertificateContextPropertyString(_certContext, global::Interop.Crypt32.CertContextPropId.CERT_FRIENDLY_NAME_PROP_ID, (byte*)pvData, ref pcbData))
				{
					return string.Empty;
				}
			}
			return new string(span2.Slice(0, (int)pcbData / 2 - 1));
		}
		set
		{
			string text = value ?? string.Empty;
			nint num = Marshal.StringToHGlobalUni(text);
			try
			{
				global::Interop.Crypt32.DATA_BLOB dATA_BLOB = new global::Interop.Crypt32.DATA_BLOB(num, checked(2 * ((uint)text.Length + 1)));
				if (!global::Interop.Crypt32.CertSetCertificateContextProperty(_certContext, global::Interop.Crypt32.CertContextPropId.CERT_FRIENDLY_NAME_PROP_ID, global::Interop.Crypt32.CertSetPropertyFlags.None, &dATA_BLOB))
				{
					throw Marshal.GetLastPInvokeError().ToCryptographicException();
				}
			}
			finally
			{
				Marshal.FreeHGlobal(num);
			}
		}
	}

	public unsafe X500DistinguishedName SubjectName => InvokeWithCertContext(delegate(global::Interop.Crypt32.CERT_CONTEXT* certContext)
	{
		ReadOnlySpan<byte> encodedDistinguishedName = certContext->pCertInfo->Subject.DangerousAsSpan();
		return new X500DistinguishedName(encodedDistinguishedName);
	});

	public unsafe X500DistinguishedName IssuerName => InvokeWithCertContext(delegate(global::Interop.Crypt32.CERT_CONTEXT* certContext)
	{
		ReadOnlySpan<byte> encodedDistinguishedName = certContext->pCertInfo->Issuer.DangerousAsSpan();
		return new X500DistinguishedName(encodedDistinguishedName);
	});

	public unsafe IEnumerable<X509Extension> Extensions => InvokeWithCertContext(delegate(global::Interop.Crypt32.CERT_CONTEXT* certContext)
	{
		global::Interop.Crypt32.CERT_INFO* pCertInfo = certContext->pCertInfo;
		int cExtension = pCertInfo->cExtension;
		X509Extension[] array = new X509Extension[cExtension];
		for (int i = 0; i < cExtension; i++)
		{
			global::Interop.Crypt32.CERT_EXTENSION* ptr = (global::Interop.Crypt32.CERT_EXTENSION*)((byte*)((IntPtr*)(&pCertInfo->rgExtension))->ToPointer() + (nint)i * (nint)sizeof(global::Interop.Crypt32.CERT_EXTENSION));
			string value = Marshal.PtrToStringAnsi(ptr->pszObjId);
			Oid oid = new Oid(value, null);
			bool critical = ptr->fCritical != 0;
			ReadOnlySpan<byte> rawData = ptr->Value.DangerousAsSpan();
			array[i] = new X509Extension(oid, rawData, critical);
		}
		return array;
	});

	public bool HasPrivateKey => _certContext.ContainsPrivateKey;

	internal static ICertificatePal FromHandle(nint handle)
	{
		if (handle == IntPtr.Zero)
		{
			throw new ArgumentException(System.SR.Arg_InvalidHandle, "handle");
		}
		SafeCertContextHandle safeCertContextHandle = global::Interop.Crypt32.CertDuplicateCertificateContext(handle);
		if (safeCertContextHandle.IsInvalid)
		{
			Exception ex = (-2147024890).ToCryptographicException();
			safeCertContextHandle.Dispose();
			throw ex;
		}
		int pcbData = 0;
		global::Interop.Crypt32.DATA_BLOB pvData;
		bool deleteKeyContainer = global::Interop.Crypt32.CertGetCertificateContextProperty(safeCertContextHandle, global::Interop.Crypt32.CertContextPropId.CERT_CLR_DELETE_KEY_PROP_ID, out pvData, ref pcbData);
		return new CertificatePal(safeCertContextHandle, deleteKeyContainer);
	}

	internal static ICertificatePal FromOtherCert(X509Certificate copyFrom)
	{
		return new CertificatePal((CertificatePal)copyFrom.Pal);
	}

	internal static ICertificatePal FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
	{
		return FromBlobOrFile(rawData, null, password, keyStorageFlags);
	}

	internal static ICertificatePal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
	{
		return FromBlobOrFile(ReadOnlySpan<byte>.Empty, fileName, password, keyStorageFlags);
	}

	private unsafe byte[] PropagateKeyAlgorithmParametersFromChain()
	{
		SafeX509ChainHandle ppChainContext = null;
		try
		{
			int pcbData = 0;
			if (!global::Interop.Crypt32.CertGetCertificateContextProperty(_certContext, global::Interop.Crypt32.CertContextPropId.CERT_PUBKEY_ALG_PARA_PROP_ID, null, ref pcbData))
			{
				global::Interop.Crypt32.CERT_CHAIN_PARA pChainPara = default(global::Interop.Crypt32.CERT_CHAIN_PARA);
				pChainPara.cbSize = sizeof(global::Interop.Crypt32.CERT_CHAIN_PARA);
				if (!global::Interop.Crypt32.CertGetCertificateChain(0, _certContext, null, SafeCrypt32Handle<SafeCertStoreHandle>.InvalidHandle, ref pChainPara, global::Interop.Crypt32.CertChainFlags.None, IntPtr.Zero, out ppChainContext))
				{
					throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
				}
				if (!global::Interop.Crypt32.CertGetCertificateContextProperty(_certContext, global::Interop.Crypt32.CertContextPropId.CERT_PUBKEY_ALG_PARA_PROP_ID, null, ref pcbData))
				{
					throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
				}
			}
			byte[] array = new byte[pcbData];
			if (!global::Interop.Crypt32.CertGetCertificateContextProperty(_certContext, global::Interop.Crypt32.CertContextPropId.CERT_PUBKEY_ALG_PARA_PROP_ID, array, ref pcbData))
			{
				throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
			}
			return array;
		}
		finally
		{
			ppChainContext?.Dispose();
		}
	}

	public PolicyData GetPolicyData()
	{
		throw new PlatformNotSupportedException();
	}

	public string GetNameInfo(X509NameType nameType, bool forIssuer)
	{
		return global::Interop.crypt32.CertGetNameString(_certContext, MapNameType(nameType), forIssuer ? global::Interop.Crypt32.CertNameFlags.CERT_NAME_ISSUER_FLAG : global::Interop.Crypt32.CertNameFlags.None, (global::Interop.Crypt32.CertNameStringType)33554435);
	}

	public void AppendPrivateKeyInfo(StringBuilder sb)
	{
		if (!HasPrivateKey)
		{
			return;
		}
		sb.AppendLine();
		sb.AppendLine();
		sb.AppendLine("[Private Key]");
		CspKeyContainerInfo cspKeyContainerInfo = null;
		try
		{
			CspParameters privateKeyCsp = GetPrivateKeyCsp();
			if (privateKeyCsp != null)
			{
				cspKeyContainerInfo = new CspKeyContainerInfo(privateKeyCsp);
			}
		}
		catch (CryptographicException)
		{
		}
		if (cspKeyContainerInfo == null)
		{
			return;
		}
		sb.AppendLine().Append("  Key Store: ").Append(cspKeyContainerInfo.MachineKeyStore ? "Machine" : "User");
		sb.AppendLine().Append("  Provider Name: ").Append(cspKeyContainerInfo.ProviderName);
		sb.AppendLine().Append("  Provider type: ").Append(cspKeyContainerInfo.ProviderType);
		sb.AppendLine().Append("  Key Spec: ").Append(cspKeyContainerInfo.KeyNumber);
		sb.AppendLine().Append("  Key Container Name: ").Append(cspKeyContainerInfo.KeyContainerName);
		try
		{
			string uniqueKeyContainerName = cspKeyContainerInfo.UniqueKeyContainerName;
			sb.AppendLine().Append("  Unique Key Container Name: ").Append(uniqueKeyContainerName);
		}
		catch (CryptographicException)
		{
		}
		catch (NotSupportedException)
		{
		}
		try
		{
			bool hardwareDevice = cspKeyContainerInfo.HardwareDevice;
			sb.AppendLine().Append("  Hardware Device: ").Append(hardwareDevice);
		}
		catch (CryptographicException)
		{
		}
		try
		{
			bool removable = cspKeyContainerInfo.Removable;
			sb.AppendLine().Append("  Removable: ").Append(removable);
		}
		catch (CryptographicException)
		{
		}
		try
		{
			bool @protected = cspKeyContainerInfo.Protected;
			sb.AppendLine().Append("  Protected: ").Append(@protected);
		}
		catch (CryptographicException)
		{
		}
		catch (NotSupportedException)
		{
		}
	}

	public void Dispose()
	{
		SafeCertContextHandle certContext = _certContext;
		_certContext = null;
		if (certContext != null && !certContext.IsInvalid)
		{
			certContext.Dispose();
		}
	}

	internal unsafe SafeCertContextHandle GetCertContext()
	{
		return InvokeWithCertContext((global::Interop.Crypt32.CERT_CONTEXT* certContext) => global::Interop.Crypt32.CertDuplicateCertificateContext((nint)certContext));
	}

	private static global::Interop.Crypt32.CertNameType MapNameType(X509NameType nameType)
	{
		switch (nameType)
		{
		case X509NameType.SimpleName:
			return global::Interop.Crypt32.CertNameType.CERT_NAME_SIMPLE_DISPLAY_TYPE;
		case X509NameType.EmailName:
			return global::Interop.Crypt32.CertNameType.CERT_NAME_EMAIL_TYPE;
		case X509NameType.UpnName:
			return global::Interop.Crypt32.CertNameType.CERT_NAME_UPN_TYPE;
		case X509NameType.DnsName:
		case X509NameType.DnsFromAlternativeName:
			return global::Interop.Crypt32.CertNameType.CERT_NAME_DNS_TYPE;
		case X509NameType.UrlName:
			return global::Interop.Crypt32.CertNameType.CERT_NAME_URL_TYPE;
		default:
			throw new ArgumentException(System.SR.Argument_InvalidNameType);
		}
	}

	private string GetIssuerOrSubject(bool issuer, bool reverse)
	{
		return global::Interop.crypt32.CertGetNameString(_certContext, global::Interop.Crypt32.CertNameType.CERT_NAME_RDN_TYPE, issuer ? global::Interop.Crypt32.CertNameFlags.CERT_NAME_ISSUER_FLAG : global::Interop.Crypt32.CertNameFlags.None, global::Interop.Crypt32.CertNameStringType.CERT_X500_NAME_STR | (reverse ? global::Interop.Crypt32.CertNameStringType.CERT_NAME_STR_REVERSE_FLAG : ((global::Interop.Crypt32.CertNameStringType)0)));
	}

	private CertificatePal(CertificatePal copyFrom)
	{
		_certContext = new SafeCertContextHandle(copyFrom._certContext);
	}

	private CertificatePal(SafeCertContextHandle certContext, bool deleteKeyContainer)
	{
		if (deleteKeyContainer)
		{
			using SafeCertContextHandle safeCertContextHandle = certContext;
			certContext = global::Interop.Crypt32.CertDuplicateCertificateContextWithKeyContainerDeletion(safeCertContextHandle.DangerousGetHandle());
		}
		_certContext = certContext;
	}

	public byte[] Export(X509ContentType contentType, SafePasswordHandle password)
	{
		using IExportPal exportPal = StorePal.FromCertificate(this);
		return exportPal.Export(contentType, password);
	}

	private unsafe T InvokeWithCertContext<T>(CertContextCallback<T> callback)
	{
		bool success = false;
		_certContext.DangerousAddRef(ref success);
		try
		{
			return callback(_certContext.DangerousCertContext);
		}
		finally
		{
			if (success)
			{
				_certContext.DangerousRelease();
			}
		}
	}

	private unsafe static CertificatePal FromBlobOrFile(ReadOnlySpan<byte> rawData, string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
	{
		bool flag = fileName != null;
		global::Interop.Crypt32.PfxCertStoreFlags pfxCertStoreFlags = MapKeyStorageFlags(keyStorageFlags);
		bool deleteKeyContainer = false;
		SafeCertStoreHandle phCertStore = null;
		SafeCryptMsgHandle phMsg = null;
		SafeCertContextHandle ppvContext = null;
		try
		{
			global::Interop.Crypt32.ContentType pdwContentType;
			fixed (byte* value = rawData)
			{
				fixed (char* ptr = fileName)
				{
					global::Interop.Crypt32.DATA_BLOB dATA_BLOB = new global::Interop.Crypt32.DATA_BLOB(new IntPtr(value), (!flag) ? ((uint)rawData.Length) : 0u);
					global::Interop.Crypt32.CertQueryObjectType dwObjectType = (flag ? global::Interop.Crypt32.CertQueryObjectType.CERT_QUERY_OBJECT_FILE : global::Interop.Crypt32.CertQueryObjectType.CERT_QUERY_OBJECT_BLOB);
					void* pvObject = (flag ? ((void*)ptr) : ((void*)(&dATA_BLOB)));
					if (!global::Interop.Crypt32.CryptQueryObject(dwObjectType, pvObject, global::Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_CERT | global::Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT | global::Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED | global::Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED | global::Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PFX, global::Interop.Crypt32.ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_ALL, 0, out var _, out pdwContentType, out var _, out phCertStore, out phMsg, out ppvContext))
					{
						int hRForLastWin32Error = Marshal.GetHRForLastWin32Error();
						throw hRForLastWin32Error.ToCryptographicException();
					}
				}
			}
			switch (pdwContentType)
			{
			case global::Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED:
			case global::Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED:
				ppvContext?.Dispose();
				ppvContext = GetSignerInPKCS7Store(phCertStore, phMsg);
				break;
			case global::Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_PFX:
				if (flag)
				{
					rawData = File.ReadAllBytes(fileName);
				}
				ppvContext?.Dispose();
				X509Certificate.EnforceIterationCountLimit(ref rawData, flag, password.PasswordProvided);
				ppvContext = FilterPFXStore(rawData, password, pfxCertStoreFlags);
				deleteKeyContainer = (keyStorageFlags & (X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.EphemeralKeySet)) == 0;
				break;
			}
			CertificatePal result = new CertificatePal(ppvContext, deleteKeyContainer);
			ppvContext = null;
			return result;
		}
		finally
		{
			phCertStore?.Dispose();
			phMsg?.Dispose();
			ppvContext?.Dispose();
		}
	}

	private unsafe static SafeCertContextHandle GetSignerInPKCS7Store(SafeCertStoreHandle hCertStore, SafeCryptMsgHandle hCryptMsg)
	{
		int pcbData = 4;
		if (!global::Interop.Crypt32.CryptMsgGetParam(hCryptMsg, global::Interop.Crypt32.CryptMsgParamType.CMSG_SIGNER_COUNT_PARAM, 0, out var pvData, ref pcbData))
		{
			throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
		}
		if (pvData == 0)
		{
			throw (-2146889714).ToCryptographicException();
		}
		int pcbData2 = 0;
		if (!global::Interop.Crypt32.CryptMsgGetParam(hCryptMsg, global::Interop.Crypt32.CryptMsgParamType.CMSG_SIGNER_INFO_PARAM, 0, null, ref pcbData2))
		{
			throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
		}
		fixed (byte* ptr = new byte[pcbData2])
		{
			if (!global::Interop.Crypt32.CryptMsgGetParam(hCryptMsg, global::Interop.Crypt32.CryptMsgParamType.CMSG_SIGNER_INFO_PARAM, 0, ptr, ref pcbData2))
			{
				throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
			}
			CMSG_SIGNER_INFO_Partial* ptr2 = (CMSG_SIGNER_INFO_Partial*)ptr;
			global::Interop.Crypt32.CERT_INFO cERT_INFO = default(global::Interop.Crypt32.CERT_INFO);
			cERT_INFO.Issuer.cbData = ptr2->Issuer.cbData;
			cERT_INFO.Issuer.pbData = ptr2->Issuer.pbData;
			cERT_INFO.SerialNumber.cbData = ptr2->SerialNumber.cbData;
			cERT_INFO.SerialNumber.pbData = ptr2->SerialNumber.pbData;
			SafeCertContextHandle pCertContext = null;
			if (!global::Interop.crypt32.CertFindCertificateInStore(hCertStore, global::Interop.Crypt32.CertFindType.CERT_FIND_SUBJECT_CERT, &cERT_INFO, ref pCertContext))
			{
				Exception ex = Marshal.GetHRForLastWin32Error().ToCryptographicException();
				pCertContext.Dispose();
				throw ex;
			}
			return pCertContext;
		}
	}

	private unsafe static SafeCertContextHandle FilterPFXStore(ReadOnlySpan<byte> rawData, SafePasswordHandle password, global::Interop.Crypt32.PfxCertStoreFlags pfxCertStoreFlags)
	{
		SafeCertStoreHandle safeCertStoreHandle;
		fixed (byte* value = rawData)
		{
			global::Interop.Crypt32.DATA_BLOB pPFX = new global::Interop.Crypt32.DATA_BLOB(new IntPtr(value), (uint)rawData.Length);
			safeCertStoreHandle = global::Interop.Crypt32.PFXImportCertStore(ref pPFX, password, pfxCertStoreFlags);
			if (safeCertStoreHandle.IsInvalid)
			{
				Exception ex = Marshal.GetHRForLastWin32Error().ToCryptographicException();
				safeCertStoreHandle.Dispose();
				throw ex;
			}
		}
		try
		{
			SafeCertContextHandle safeCertContextHandle = SafeCrypt32Handle<SafeCertContextHandle>.InvalidHandle;
			SafeCertContextHandle pCertContext = null;
			while (global::Interop.crypt32.CertEnumCertificatesInStore(safeCertStoreHandle, ref pCertContext))
			{
				if (pCertContext.ContainsPrivateKey)
				{
					if (!safeCertContextHandle.IsInvalid && safeCertContextHandle.ContainsPrivateKey)
					{
						if (pCertContext.HasPersistedPrivateKey)
						{
							SafeCertContextHandleWithKeyContainerDeletion.DeleteKeyContainer(pCertContext);
						}
					}
					else
					{
						safeCertContextHandle.Dispose();
						safeCertContextHandle = pCertContext.Duplicate();
					}
				}
				else if (safeCertContextHandle.IsInvalid)
				{
					safeCertContextHandle.Dispose();
					safeCertContextHandle = pCertContext.Duplicate();
				}
			}
			if (safeCertContextHandle.IsInvalid)
			{
				safeCertContextHandle.Dispose();
				throw new CryptographicException(System.SR.Cryptography_Pfx_NoCertificates);
			}
			return safeCertContextHandle;
		}
		finally
		{
			safeCertStoreHandle.Dispose();
		}
	}

	private static global::Interop.Crypt32.PfxCertStoreFlags MapKeyStorageFlags(X509KeyStorageFlags keyStorageFlags)
	{
		if ((keyStorageFlags & (X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable | X509KeyStorageFlags.UserProtected | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.EphemeralKeySet)) != keyStorageFlags)
		{
			throw new ArgumentException(System.SR.Argument_InvalidFlag, "keyStorageFlags");
		}
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

	public RSA GetRSAPrivateKey()
	{
		return GetPrivateKey<RSA>((CspParameters csp) => new RSACryptoServiceProvider(csp), (CngKey cngKey) => new RSACng(cngKey, transferOwnership: true));
	}

	public DSA GetDSAPrivateKey()
	{
		return GetPrivateKey((Func<CspParameters, DSA>)((CspParameters csp) => new DSACryptoServiceProvider(csp)), (Func<CngKey, DSA>)((CngKey cngKey) => new DSACng(cngKey, transferOwnership: true)));
	}

	public ECDsa GetECDsaPrivateKey()
	{
		return GetPrivateKey<ECDsa>(delegate
		{
			throw new NotSupportedException(System.SR.NotSupported_ECDsa_Csp);
		}, (CngKey cngKey) => new ECDsaCng(cngKey, transferOwnership: true));
	}

	public ECDiffieHellman GetECDiffieHellmanPrivateKey()
	{
		return GetPrivateKey<ECDiffieHellman>(delegate
		{
			throw new NotSupportedException(System.SR.NotSupported_ECDiffieHellman_Csp);
		}, (CngKey cngKey) => new ECDiffieHellmanCng(cngKey, transferOwnership: true));
	}

	public ICertificatePal CopyWithPrivateKey(DSA dsa)
	{
		if (dsa is DSACng dSACng)
		{
			ICertificatePal certificatePal = CopyWithPersistedCngKey(dSACng.Key);
			if (certificatePal != null)
			{
				return certificatePal;
			}
		}
		if (dsa is DSACryptoServiceProvider dSACryptoServiceProvider)
		{
			ICertificatePal certificatePal = CopyWithPersistedCapiKey(dSACryptoServiceProvider.CspKeyContainerInfo);
			if (certificatePal != null)
			{
				return certificatePal;
			}
		}
		DSAParameters parameters = dsa.ExportParameters(includePrivateParameters: true);
		using (PinAndClear.Track(parameters.X))
		{
			using DSACng dSACng2 = new DSACng();
			dSACng2.ImportParameters(parameters);
			return CopyWithEphemeralKey(dSACng2.Key);
		}
	}

	public ICertificatePal CopyWithPrivateKey(ECDsa ecdsa)
	{
		if (ecdsa is ECDsaCng eCDsaCng)
		{
			ICertificatePal certificatePal = CopyWithPersistedCngKey(eCDsaCng.Key);
			if (certificatePal != null)
			{
				return certificatePal;
			}
		}
		ECParameters parameters = ecdsa.ExportParameters(includePrivateParameters: true);
		using (PinAndClear.Track(parameters.D))
		{
			using ECDsaCng eCDsaCng2 = new ECDsaCng();
			eCDsaCng2.ImportParameters(parameters);
			return CopyWithEphemeralKey(eCDsaCng2.Key);
		}
	}

	public ICertificatePal CopyWithPrivateKey(ECDiffieHellman ecdh)
	{
		if (ecdh is ECDiffieHellmanCng eCDiffieHellmanCng)
		{
			ICertificatePal certificatePal = CopyWithPersistedCngKey(eCDiffieHellmanCng.Key);
			if (certificatePal != null)
			{
				return certificatePal;
			}
		}
		ECParameters parameters = ecdh.ExportParameters(includePrivateParameters: true);
		using (PinAndClear.Track(parameters.D))
		{
			using ECDiffieHellmanCng eCDiffieHellmanCng2 = new ECDiffieHellmanCng();
			eCDiffieHellmanCng2.ImportParameters(parameters);
			return CopyWithEphemeralKey(eCDiffieHellmanCng2.Key);
		}
	}

	public ICertificatePal CopyWithPrivateKey(RSA rsa)
	{
		if (rsa is RSACng rSACng)
		{
			ICertificatePal certificatePal = CopyWithPersistedCngKey(rSACng.Key);
			if (certificatePal != null)
			{
				return certificatePal;
			}
		}
		if (rsa is RSACryptoServiceProvider rSACryptoServiceProvider)
		{
			ICertificatePal certificatePal = CopyWithPersistedCapiKey(rSACryptoServiceProvider.CspKeyContainerInfo);
			if (certificatePal != null)
			{
				return certificatePal;
			}
		}
		RSAParameters parameters = rsa.ExportParameters(includePrivateParameters: true);
		using (PinAndClear.Track(parameters.D))
		{
			using (PinAndClear.Track(parameters.P))
			{
				using (PinAndClear.Track(parameters.Q))
				{
					using (PinAndClear.Track(parameters.DP))
					{
						using (PinAndClear.Track(parameters.DQ))
						{
							using (PinAndClear.Track(parameters.InverseQ))
							{
								using RSACng rSACng2 = new RSACng();
								rSACng2.ImportParameters(parameters);
								return CopyWithEphemeralKey(rSACng2.Key);
							}
						}
					}
				}
			}
		}
	}

	private T GetPrivateKey<T>(Func<CspParameters, T> createCsp, Func<CngKey, T> createCng) where T : AsymmetricAlgorithm
	{
		using (SafeCertContextHandle certificateContext = GetCertContext())
		{
			CngKeyHandleOpenOptions handleOptions;
			SafeNCryptKeyHandle safeNCryptKeyHandle = TryAcquireCngPrivateKey(certificateContext, out handleOptions);
			if (safeNCryptKeyHandle != null)
			{
				CngKey arg = CngKey.OpenNoDuplicate(safeNCryptKeyHandle, handleOptions);
				return createCng(arg);
			}
		}
		CspParameters privateKeyCsp = GetPrivateKeyCsp();
		if (privateKeyCsp == null)
		{
			return null;
		}
		if (privateKeyCsp.ProviderType == 0)
		{
			string providerName = privateKeyCsp.ProviderName;
			string keyContainerName = privateKeyCsp.KeyContainerName;
			CngKey arg2 = CngKey.Open(keyContainerName, new CngProvider(providerName));
			return createCng(arg2);
		}
		privateKeyCsp.Flags |= CspProviderFlags.UseExistingKey;
		return createCsp(privateKeyCsp);
	}

	private static SafeNCryptKeyHandle TryAcquireCngPrivateKey(SafeCertContextHandle certificateContext, out CngKeyHandleOpenOptions handleOptions)
	{
		if (!certificateContext.HasPersistedPrivateKey)
		{
			int pcbData = IntPtr.Size;
			if (global::Interop.Crypt32.CertGetCertificateContextProperty(certificateContext, global::Interop.Crypt32.CertContextPropId.CERT_NCRYPT_KEY_HANDLE_PROP_ID, out nint pvData, ref pcbData))
			{
				handleOptions = CngKeyHandleOpenOptions.EphemeralKey;
				return new SafeNCryptKeyHandle(pvData, certificateContext);
			}
		}
		bool pfCallerFreeProvOrNCryptKey = true;
		SafeNCryptKeyHandle phCryptProvOrNCryptKey = null;
		handleOptions = CngKeyHandleOpenOptions.None;
		try
		{
			int pdwKeySpec = 0;
			if (!global::Interop.crypt32.CryptAcquireCertificatePrivateKey(certificateContext, global::Interop.Crypt32.CryptAcquireCertificatePrivateKeyFlags.CRYPT_ACQUIRE_ONLY_NCRYPT_KEY_FLAG, IntPtr.Zero, out phCryptProvOrNCryptKey, out pdwKeySpec, out pfCallerFreeProvOrNCryptKey))
			{
				pfCallerFreeProvOrNCryptKey = false;
				phCryptProvOrNCryptKey?.SetHandleAsInvalid();
				return null;
			}
			if (!pfCallerFreeProvOrNCryptKey && phCryptProvOrNCryptKey != null && !phCryptProvOrNCryptKey.IsInvalid)
			{
				SafeNCryptKeyHandle safeNCryptKeyHandle = new SafeNCryptKeyHandle(phCryptProvOrNCryptKey.DangerousGetHandle(), certificateContext);
				phCryptProvOrNCryptKey.SetHandleAsInvalid();
				phCryptProvOrNCryptKey = safeNCryptKeyHandle;
				pfCallerFreeProvOrNCryptKey = true;
			}
			return phCryptProvOrNCryptKey;
		}
		catch
		{
			if (phCryptProvOrNCryptKey != null && !pfCallerFreeProvOrNCryptKey)
			{
				phCryptProvOrNCryptKey.SetHandleAsInvalid();
			}
			throw;
		}
	}

	private unsafe CspParameters GetPrivateKeyCsp()
	{
		int pcbData = 0;
		if (!global::Interop.Crypt32.CertGetCertificateContextProperty(_certContext, global::Interop.Crypt32.CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, null, ref pcbData))
		{
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			if (lastPInvokeError == -2146885628)
			{
				return null;
			}
			throw lastPInvokeError.ToCryptographicException();
		}
		byte[] array = new byte[pcbData];
		fixed (byte* ptr = array)
		{
			if (!global::Interop.Crypt32.CertGetCertificateContextProperty(_certContext, global::Interop.Crypt32.CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, array, ref pcbData))
			{
				throw Marshal.GetLastPInvokeError().ToCryptographicException();
			}
			global::Interop.Crypt32.CRYPT_KEY_PROV_INFO* ptr2 = (global::Interop.Crypt32.CRYPT_KEY_PROV_INFO*)ptr;
			CspParameters cspParameters = new CspParameters();
			cspParameters.ProviderName = Marshal.PtrToStringUni((nint)ptr2->pwszProvName);
			cspParameters.KeyContainerName = Marshal.PtrToStringUni((nint)ptr2->pwszContainerName);
			cspParameters.ProviderType = ptr2->dwProvType;
			cspParameters.KeyNumber = ptr2->dwKeySpec;
			cspParameters.Flags = (((ptr2->dwFlags & global::Interop.Crypt32.CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET) == global::Interop.Crypt32.CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET) ? CspProviderFlags.UseMachineKeyStore : CspProviderFlags.NoFlags);
			return cspParameters;
		}
	}

	private unsafe CertificatePal CopyWithPersistedCngKey(CngKey cngKey)
	{
		//The blocks IL_0090, IL_00a9, IL_00ac, IL_00ae, IL_00ce are reachable both inside and outside the pinned region starting at IL_008b. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (string.IsNullOrEmpty(cngKey.KeyName))
		{
			return null;
		}
		CertificatePal certificatePal = (CertificatePal)FromBlob(RawData, SafePasswordHandle.InvalidHandle, X509KeyStorageFlags.PersistKeySet);
		CngProvider provider = cngKey.Provider;
		string keyName = cngKey.KeyName;
		bool isMachineKey = cngKey.IsMachineKey;
		int dwKeySpec = GuessKeySpec(provider, keyName, isMachineKey, cngKey.AlgorithmGroup);
		global::Interop.Crypt32.CRYPT_KEY_PROV_INFO cRYPT_KEY_PROV_INFO = default(global::Interop.Crypt32.CRYPT_KEY_PROV_INFO);
		fixed (char* pwszContainerName = cngKey.KeyName)
		{
			string provider2 = cngKey.Provider.Provider;
			char* intPtr;
			ref global::Interop.Crypt32.CRYPT_KEY_PROV_INFO reference;
			int dwFlags;
			if (provider2 == null)
			{
				char* pwszProvName;
				intPtr = (pwszProvName = null);
				cRYPT_KEY_PROV_INFO.pwszContainerName = pwszContainerName;
				cRYPT_KEY_PROV_INFO.pwszProvName = pwszProvName;
				reference = ref cRYPT_KEY_PROV_INFO;
				dwFlags = (isMachineKey ? 32 : 0);
				reference.dwFlags = (global::Interop.Crypt32.CryptAcquireContextFlags)dwFlags;
				cRYPT_KEY_PROV_INFO.dwKeySpec = dwKeySpec;
				if (!global::Interop.Crypt32.CertSetCertificateContextProperty(certificatePal._certContext, global::Interop.Crypt32.CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, global::Interop.Crypt32.CertSetPropertyFlags.None, &cRYPT_KEY_PROV_INFO))
				{
					Exception ex = Marshal.GetLastPInvokeError().ToCryptographicException();
					certificatePal.Dispose();
					throw ex;
				}
			}
			else
			{
				fixed (char* ptr = &provider2.GetPinnableReference())
				{
					char* pwszProvName;
					intPtr = (pwszProvName = ptr);
					cRYPT_KEY_PROV_INFO.pwszContainerName = pwszContainerName;
					cRYPT_KEY_PROV_INFO.pwszProvName = pwszProvName;
					reference = ref cRYPT_KEY_PROV_INFO;
					dwFlags = (int)(reference.dwFlags = (isMachineKey ? global::Interop.Crypt32.CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET : global::Interop.Crypt32.CryptAcquireContextFlags.None));
					cRYPT_KEY_PROV_INFO.dwKeySpec = dwKeySpec;
					if (!global::Interop.Crypt32.CertSetCertificateContextProperty(certificatePal._certContext, global::Interop.Crypt32.CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, global::Interop.Crypt32.CertSetPropertyFlags.None, &cRYPT_KEY_PROV_INFO))
					{
						Exception ex = Marshal.GetLastPInvokeError().ToCryptographicException();
						certificatePal.Dispose();
						throw ex;
					}
				}
			}
		}
		return certificatePal;
	}

	private static int GuessKeySpec(CngProvider provider, string keyName, bool machineKey, CngAlgorithmGroup algorithmGroup)
	{
		if (provider == CngProvider.MicrosoftSoftwareKeyStorageProvider || provider == CngProvider.MicrosoftSmartCardKeyStorageProvider)
		{
			return 0;
		}
		try
		{
			CngKeyOpenOptions openOptions = (machineKey ? CngKeyOpenOptions.MachineKey : CngKeyOpenOptions.None);
			using (CngKey.Open(keyName, provider, openOptions))
			{
				return 0;
			}
		}
		catch (CryptographicException)
		{
			CspParameters cspParameters = new CspParameters
			{
				ProviderName = provider.Provider,
				KeyContainerName = keyName,
				Flags = CspProviderFlags.UseExistingKey,
				KeyNumber = 2
			};
			if (machineKey)
			{
				cspParameters.Flags |= CspProviderFlags.UseMachineKeyStore;
			}
			if (TryGuessKeySpec(cspParameters, algorithmGroup, out var keySpec))
			{
				return keySpec;
			}
			throw;
		}
	}

	private static bool TryGuessKeySpec(CspParameters cspParameters, CngAlgorithmGroup algorithmGroup, out int keySpec)
	{
		if (algorithmGroup == CngAlgorithmGroup.Rsa)
		{
			return TryGuessRsaKeySpec(cspParameters, out keySpec);
		}
		if (algorithmGroup == CngAlgorithmGroup.Dsa)
		{
			return TryGuessDsaKeySpec(cspParameters, out keySpec);
		}
		keySpec = 0;
		return false;
	}

	private static bool TryGuessRsaKeySpec(CspParameters cspParameters, out int keySpec)
	{
		int[] array = new int[4] { 1, 24, 12, 2 };
		int[] array2 = array;
		foreach (int providerType in array2)
		{
			cspParameters.ProviderType = providerType;
			try
			{
				using (new RSACryptoServiceProvider(cspParameters))
				{
					keySpec = cspParameters.KeyNumber;
					return true;
				}
			}
			catch (CryptographicException)
			{
			}
		}
		keySpec = 0;
		return false;
	}

	private static bool TryGuessDsaKeySpec(CspParameters cspParameters, out int keySpec)
	{
		int[] array = new int[2] { 13, 3 };
		int[] array2 = array;
		foreach (int providerType in array2)
		{
			cspParameters.ProviderType = providerType;
			try
			{
				using (new DSACryptoServiceProvider(cspParameters))
				{
					keySpec = cspParameters.KeyNumber;
					return true;
				}
			}
			catch (CryptographicException)
			{
			}
		}
		keySpec = 0;
		return false;
	}

	private unsafe CertificatePal CopyWithPersistedCapiKey(CspKeyContainerInfo keyContainerInfo)
	{
		//The blocks IL_0063, IL_0080, IL_0083, IL_0085, IL_00b6 are reachable both inside and outside the pinned region starting at IL_005e. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (string.IsNullOrEmpty(keyContainerInfo.KeyContainerName))
		{
			return null;
		}
		CertificatePal certificatePal = (CertificatePal)FromBlob(RawData, SafePasswordHandle.InvalidHandle, X509KeyStorageFlags.PersistKeySet);
		global::Interop.Crypt32.CRYPT_KEY_PROV_INFO cRYPT_KEY_PROV_INFO = default(global::Interop.Crypt32.CRYPT_KEY_PROV_INFO);
		fixed (char* pwszContainerName = keyContainerInfo.KeyContainerName)
		{
			string? providerName = keyContainerInfo.ProviderName;
			char* intPtr;
			ref global::Interop.Crypt32.CRYPT_KEY_PROV_INFO reference;
			int dwFlags;
			if (providerName == null)
			{
				char* pwszProvName;
				intPtr = (pwszProvName = null);
				cRYPT_KEY_PROV_INFO.pwszContainerName = pwszContainerName;
				cRYPT_KEY_PROV_INFO.pwszProvName = pwszProvName;
				reference = ref cRYPT_KEY_PROV_INFO;
				dwFlags = (keyContainerInfo.MachineKeyStore ? 32 : 0);
				reference.dwFlags = (global::Interop.Crypt32.CryptAcquireContextFlags)dwFlags;
				cRYPT_KEY_PROV_INFO.dwProvType = keyContainerInfo.ProviderType;
				cRYPT_KEY_PROV_INFO.dwKeySpec = (int)keyContainerInfo.KeyNumber;
				if (!global::Interop.Crypt32.CertSetCertificateContextProperty(certificatePal._certContext, global::Interop.Crypt32.CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, global::Interop.Crypt32.CertSetPropertyFlags.None, &cRYPT_KEY_PROV_INFO))
				{
					Exception ex = Marshal.GetLastPInvokeError().ToCryptographicException();
					certificatePal.Dispose();
					throw ex;
				}
			}
			else
			{
				fixed (char* ptr = &providerName.GetPinnableReference())
				{
					char* pwszProvName;
					intPtr = (pwszProvName = ptr);
					cRYPT_KEY_PROV_INFO.pwszContainerName = pwszContainerName;
					cRYPT_KEY_PROV_INFO.pwszProvName = pwszProvName;
					reference = ref cRYPT_KEY_PROV_INFO;
					dwFlags = (int)(reference.dwFlags = (keyContainerInfo.MachineKeyStore ? global::Interop.Crypt32.CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET : global::Interop.Crypt32.CryptAcquireContextFlags.None));
					cRYPT_KEY_PROV_INFO.dwProvType = keyContainerInfo.ProviderType;
					cRYPT_KEY_PROV_INFO.dwKeySpec = (int)keyContainerInfo.KeyNumber;
					if (!global::Interop.Crypt32.CertSetCertificateContextProperty(certificatePal._certContext, global::Interop.Crypt32.CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, global::Interop.Crypt32.CertSetPropertyFlags.None, &cRYPT_KEY_PROV_INFO))
					{
						Exception ex = Marshal.GetLastPInvokeError().ToCryptographicException();
						certificatePal.Dispose();
						throw ex;
					}
				}
			}
		}
		return certificatePal;
	}

	private CertificatePal CopyWithEphemeralKey(CngKey cngKey)
	{
		using SafeNCryptKeyHandle safeNCryptKeyHandle = cngKey.Handle;
		CertificatePal certificatePal = (CertificatePal)FromBlob(RawData, SafePasswordHandle.InvalidHandle, X509KeyStorageFlags.PersistKeySet);
		try
		{
			if (!global::Interop.Crypt32.CertSetCertificateContextProperty(certificatePal._certContext, global::Interop.Crypt32.CertContextPropId.CERT_NCRYPT_KEY_HANDLE_PROP_ID, global::Interop.Crypt32.CertSetPropertyFlags.CERT_SET_PROPERTY_INHIBIT_PERSIST_FLAG, safeNCryptKeyHandle))
			{
				throw Marshal.GetLastPInvokeError().ToCryptographicException();
			}
			safeNCryptKeyHandle.SetHandleAsInvalid();
			return certificatePal;
		}
		catch
		{
			certificatePal.Dispose();
			throw;
		}
	}
}
