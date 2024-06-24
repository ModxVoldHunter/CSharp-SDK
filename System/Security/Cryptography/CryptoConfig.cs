using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography;

public class CryptoConfig
{
	private static volatile Dictionary<string, string> s_defaultOidHT;

	private static volatile Dictionary<string, object> s_defaultNameHT;

	private static readonly ConcurrentDictionary<string, Type> appNameHT = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

	private static readonly ConcurrentDictionary<string, string> appOidHT = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private static Dictionary<string, string> DefaultOidHT
	{
		get
		{
			if (s_defaultOidHT != null)
			{
				return s_defaultOidHT;
			}
			int capacity = 37;
			Dictionary<string, string> dictionary = new Dictionary<string, string>(capacity, StringComparer.OrdinalIgnoreCase);
			dictionary.Add("SHA", "1.3.14.3.2.26");
			dictionary.Add("SHA1", "1.3.14.3.2.26");
			dictionary.Add("System.Security.Cryptography.SHA1", "1.3.14.3.2.26");
			dictionary.Add("System.Security.Cryptography.SHA1CryptoServiceProvider", "1.3.14.3.2.26");
			dictionary.Add("System.Security.Cryptography.SHA1Cng", "1.3.14.3.2.26");
			dictionary.Add("System.Security.Cryptography.SHA1Managed", "1.3.14.3.2.26");
			dictionary.Add("SHA256", "2.16.840.1.101.3.4.2.1");
			dictionary.Add("System.Security.Cryptography.SHA256", "2.16.840.1.101.3.4.2.1");
			dictionary.Add("System.Security.Cryptography.SHA256CryptoServiceProvider", "2.16.840.1.101.3.4.2.1");
			dictionary.Add("System.Security.Cryptography.SHA256Cng", "2.16.840.1.101.3.4.2.1");
			dictionary.Add("System.Security.Cryptography.SHA256Managed", "2.16.840.1.101.3.4.2.1");
			dictionary.Add("SHA384", "2.16.840.1.101.3.4.2.2");
			dictionary.Add("System.Security.Cryptography.SHA384", "2.16.840.1.101.3.4.2.2");
			dictionary.Add("System.Security.Cryptography.SHA384CryptoServiceProvider", "2.16.840.1.101.3.4.2.2");
			dictionary.Add("System.Security.Cryptography.SHA384Cng", "2.16.840.1.101.3.4.2.2");
			dictionary.Add("System.Security.Cryptography.SHA384Managed", "2.16.840.1.101.3.4.2.2");
			dictionary.Add("SHA512", "2.16.840.1.101.3.4.2.3");
			dictionary.Add("System.Security.Cryptography.SHA512", "2.16.840.1.101.3.4.2.3");
			dictionary.Add("System.Security.Cryptography.SHA512CryptoServiceProvider", "2.16.840.1.101.3.4.2.3");
			dictionary.Add("System.Security.Cryptography.SHA512Cng", "2.16.840.1.101.3.4.2.3");
			dictionary.Add("System.Security.Cryptography.SHA512Managed", "2.16.840.1.101.3.4.2.3");
			dictionary.Add("RIPEMD160", "1.3.36.3.2.1");
			dictionary.Add("System.Security.Cryptography.RIPEMD160", "1.3.36.3.2.1");
			dictionary.Add("System.Security.Cryptography.RIPEMD160Managed", "1.3.36.3.2.1");
			dictionary.Add("MD5", "1.2.840.113549.2.5");
			dictionary.Add("System.Security.Cryptography.MD5", "1.2.840.113549.2.5");
			dictionary.Add("System.Security.Cryptography.MD5CryptoServiceProvider", "1.2.840.113549.2.5");
			dictionary.Add("System.Security.Cryptography.MD5Managed", "1.2.840.113549.2.5");
			dictionary.Add("TripleDESKeyWrap", "1.2.840.113549.1.9.16.3.6");
			dictionary.Add("RC2", "1.2.840.113549.3.2");
			dictionary.Add("System.Security.Cryptography.RC2CryptoServiceProvider", "1.2.840.113549.3.2");
			dictionary.Add("DES", "1.3.14.3.2.7");
			dictionary.Add("System.Security.Cryptography.DESCryptoServiceProvider", "1.3.14.3.2.7");
			dictionary.Add("TripleDES", "1.2.840.113549.3.7");
			dictionary.Add("System.Security.Cryptography.TripleDESCryptoServiceProvider", "1.2.840.113549.3.7");
			s_defaultOidHT = dictionary;
			return s_defaultOidHT;
		}
	}

	private static Dictionary<string, object> DefaultNameHT
	{
		get
		{
			if (s_defaultNameHT != null)
			{
				return s_defaultNameHT;
			}
			Dictionary<string, object> dictionary = new Dictionary<string, object>(89, StringComparer.OrdinalIgnoreCase);
			Type typeFromHandle = typeof(HMACMD5);
			Type typeFromHandle2 = typeof(HMACSHA1);
			Type typeFromHandle3 = typeof(HMACSHA256);
			Type typeFromHandle4 = typeof(HMACSHA384);
			Type typeFromHandle5 = typeof(HMACSHA512);
			Type typeFromHandle6 = typeof(RijndaelManaged);
			Type typeFromHandle7 = typeof(AesManaged);
			Type typeFromHandle8 = typeof(SHA256Managed);
			Type typeFromHandle9 = typeof(SHA384Managed);
			Type typeFromHandle10 = typeof(SHA512Managed);
			Type typeFromHandle11 = typeof(SHA1CryptoServiceProvider);
			Type typeFromHandle12 = typeof(MD5CryptoServiceProvider);
			Type typeFromHandle13 = typeof(RSACryptoServiceProvider);
			Type typeFromHandle14 = typeof(DSACryptoServiceProvider);
			Type typeFromHandle15 = typeof(DESCryptoServiceProvider);
			Type typeFromHandle16 = typeof(TripleDESCryptoServiceProvider);
			Type typeFromHandle17 = typeof(RC2CryptoServiceProvider);
			Type typeFromHandle18 = typeof(AesCryptoServiceProvider);
			Type typeFromHandle19 = typeof(RNGCryptoServiceProvider);
			Type typeFromHandle20 = typeof(ECDsaCng);
			dictionary.Add("RandomNumberGenerator", typeFromHandle19);
			dictionary.Add("System.Security.Cryptography.RandomNumberGenerator", typeFromHandle19);
			dictionary.Add("SHA", typeFromHandle11);
			dictionary.Add("SHA1", typeFromHandle11);
			dictionary.Add("System.Security.Cryptography.SHA1", typeFromHandle11);
			dictionary.Add("System.Security.Cryptography.HashAlgorithm", typeFromHandle11);
			dictionary.Add("MD5", typeFromHandle12);
			dictionary.Add("System.Security.Cryptography.MD5", typeFromHandle12);
			dictionary.Add("SHA256", typeFromHandle8);
			dictionary.Add("SHA-256", typeFromHandle8);
			dictionary.Add("System.Security.Cryptography.SHA256", typeFromHandle8);
			dictionary.Add("SHA384", typeFromHandle9);
			dictionary.Add("SHA-384", typeFromHandle9);
			dictionary.Add("System.Security.Cryptography.SHA384", typeFromHandle9);
			dictionary.Add("SHA512", typeFromHandle10);
			dictionary.Add("SHA-512", typeFromHandle10);
			dictionary.Add("System.Security.Cryptography.SHA512", typeFromHandle10);
			dictionary.Add("System.Security.Cryptography.HMAC", typeFromHandle2);
			dictionary.Add("System.Security.Cryptography.KeyedHashAlgorithm", typeFromHandle2);
			dictionary.Add("HMACMD5", typeFromHandle);
			dictionary.Add("System.Security.Cryptography.HMACMD5", typeFromHandle);
			dictionary.Add("HMACSHA1", typeFromHandle2);
			dictionary.Add("System.Security.Cryptography.HMACSHA1", typeFromHandle2);
			dictionary.Add("HMACSHA256", typeFromHandle3);
			dictionary.Add("System.Security.Cryptography.HMACSHA256", typeFromHandle3);
			dictionary.Add("HMACSHA384", typeFromHandle4);
			dictionary.Add("System.Security.Cryptography.HMACSHA384", typeFromHandle4);
			dictionary.Add("HMACSHA512", typeFromHandle5);
			dictionary.Add("System.Security.Cryptography.HMACSHA512", typeFromHandle5);
			dictionary.Add("RSA", typeFromHandle13);
			dictionary.Add("System.Security.Cryptography.RSA", typeFromHandle13);
			dictionary.Add("System.Security.Cryptography.AsymmetricAlgorithm", typeFromHandle13);
			if (!OperatingSystem.IsIOS() && !OperatingSystem.IsTvOS())
			{
				dictionary.Add("DSA", typeFromHandle14);
				dictionary.Add("System.Security.Cryptography.DSA", typeFromHandle14);
			}
			if (OperatingSystem.IsWindows())
			{
				dictionary.Add("ECDsa", typeFromHandle20);
			}
			dictionary.Add("ECDsaCng", typeFromHandle20);
			dictionary.Add("System.Security.Cryptography.ECDsaCng", typeFromHandle20);
			dictionary.Add("DES", typeFromHandle15);
			dictionary.Add("System.Security.Cryptography.DES", typeFromHandle15);
			dictionary.Add("3DES", typeFromHandle16);
			dictionary.Add("TripleDES", typeFromHandle16);
			dictionary.Add("Triple DES", typeFromHandle16);
			dictionary.Add("System.Security.Cryptography.TripleDES", typeFromHandle16);
			dictionary.Add("RC2", typeFromHandle17);
			dictionary.Add("System.Security.Cryptography.RC2", typeFromHandle17);
			dictionary.Add("Rijndael", typeFromHandle6);
			dictionary.Add("System.Security.Cryptography.Rijndael", typeFromHandle6);
			dictionary.Add("System.Security.Cryptography.SymmetricAlgorithm", typeFromHandle6);
			dictionary.Add("AES", typeFromHandle18);
			dictionary.Add("AesCryptoServiceProvider", typeFromHandle18);
			dictionary.Add("System.Security.Cryptography.AesCryptoServiceProvider", typeFromHandle18);
			dictionary.Add("AesManaged", typeFromHandle7);
			dictionary.Add("System.Security.Cryptography.AesManaged", typeFromHandle7);
			dictionary.Add("http://www.w3.org/2000/09/xmldsig#sha1", typeFromHandle11);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#sha256", typeFromHandle8);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#sha512", typeFromHandle10);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#des-cbc", typeFromHandle15);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#tripledes-cbc", typeFromHandle16);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#kw-tripledes", typeFromHandle16);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#aes128-cbc", typeFromHandle6);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#kw-aes128", typeFromHandle6);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#aes192-cbc", typeFromHandle6);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#kw-aes192", typeFromHandle6);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#aes256-cbc", typeFromHandle6);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#kw-aes256", typeFromHandle6);
			dictionary.Add("http://www.w3.org/2000/09/xmldsig#hmac-sha1", typeFromHandle2);
			dictionary.Add("http://www.w3.org/2001/04/xmldsig-more#md5", typeFromHandle12);
			dictionary.Add("http://www.w3.org/2001/04/xmldsig-more#sha384", typeFromHandle9);
			dictionary.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-md5", typeFromHandle);
			dictionary.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha256", typeFromHandle3);
			dictionary.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha384", typeFromHandle4);
			dictionary.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha512", typeFromHandle5);
			dictionary.Add("2.5.29.10", typeof(X509BasicConstraintsExtension));
			dictionary.Add("2.5.29.19", typeof(X509BasicConstraintsExtension));
			dictionary.Add("2.5.29.14", typeof(X509SubjectKeyIdentifierExtension));
			dictionary.Add("2.5.29.15", typeof(X509KeyUsageExtension));
			dictionary.Add("2.5.29.35", typeof(X509AuthorityKeyIdentifierExtension));
			dictionary.Add("2.5.29.37", typeof(X509EnhancedKeyUsageExtension));
			dictionary.Add("1.3.6.1.5.5.7.1.1", typeof(X509AuthorityInformationAccessExtension));
			dictionary.Add("2.5.29.17", typeof(X509SubjectAlternativeNameExtension));
			dictionary.Add("X509Chain", typeof(X509Chain));
			dictionary.Add("1.2.840.113549.1.9.3", "System.Security.Cryptography.Pkcs.Pkcs9ContentType, System.Security.Cryptography.Pkcs");
			dictionary.Add("1.2.840.113549.1.9.4", "System.Security.Cryptography.Pkcs.Pkcs9MessageDigest, System.Security.Cryptography.Pkcs");
			dictionary.Add("1.2.840.113549.1.9.5", "System.Security.Cryptography.Pkcs.Pkcs9SigningTime, System.Security.Cryptography.Pkcs");
			dictionary.Add("1.3.6.1.4.1.311.88.2.1", "System.Security.Cryptography.Pkcs.Pkcs9DocumentName, System.Security.Cryptography.Pkcs");
			dictionary.Add("1.3.6.1.4.1.311.88.2.2", "System.Security.Cryptography.Pkcs.Pkcs9DocumentDescription, System.Security.Cryptography.Pkcs");
			s_defaultNameHT = dictionary;
			return s_defaultNameHT;
		}
	}

	public static bool AllowOnlyFipsAlgorithms => false;

	[UnsupportedOSPlatform("browser")]
	public static void AddAlgorithm(Type algorithm, params string[] names)
	{
		ArgumentNullException.ThrowIfNull(algorithm, "algorithm");
		if (!algorithm.IsVisible)
		{
			throw new ArgumentException(System.SR.Cryptography_AlgorithmTypesMustBeVisible, "algorithm");
		}
		ArgumentNullException.ThrowIfNull(names, "names");
		string[] array = new string[names.Length];
		Array.Copy(names, array, array.Length);
		string[] array2 = array;
		foreach (string argument in array2)
		{
			ArgumentException.ThrowIfNullOrEmpty(argument, "names");
		}
		string[] array3 = array;
		foreach (string key in array3)
		{
			appNameHT[key] = algorithm;
		}
	}

	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public static object? CreateFromName(string name, params object?[]? args)
	{
		ArgumentNullException.ThrowIfNull(name, "name");
		appNameHT.TryGetValue(name, out var value);
		if (value == null && DefaultNameHT.TryGetValue(name, out object value2))
		{
			value = value2 as Type;
			if (value == null && value2 is string typeName)
			{
				value = Type.GetType(typeName, throwOnError: false, ignoreCase: false);
				if (value != null && !value.IsVisible)
				{
					value = null;
				}
				if (value != null)
				{
					appNameHT[name] = value;
				}
			}
		}
		if (value == null && (args == null || args.Length == 1) && name == "ECDsa")
		{
			return ECDsa.Create();
		}
		if (value == null)
		{
			value = Type.GetType(name, throwOnError: false, ignoreCase: false);
			if (value != null && !value.IsVisible)
			{
				value = null;
			}
		}
		if (value == null)
		{
			return null;
		}
		MethodBase[] constructors = value.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance);
		MethodBase[] array = constructors;
		if (array == null)
		{
			return null;
		}
		if (args == null)
		{
			args = Array.Empty<object>();
		}
		List<MethodBase> list = new List<MethodBase>();
		foreach (MethodBase methodBase in array)
		{
			if (methodBase.GetParameters().Length == args.Length)
			{
				list.Add(methodBase);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		array = list.ToArray();
		object state;
		ConstructorInfo constructorInfo = Type.DefaultBinder.BindToMethod(BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, array, ref args, null, null, null, out state) as ConstructorInfo;
		if (constructorInfo == null || typeof(Delegate).IsAssignableFrom(constructorInfo.DeclaringType))
		{
			return null;
		}
		object result = constructorInfo.Invoke(BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, Type.DefaultBinder, args, null);
		if (state != null)
		{
			Type.DefaultBinder.ReorderArgumentArray(ref args, state);
		}
		return result;
	}

	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public static object? CreateFromName(string name)
	{
		return CreateFromName(name, null);
	}

	[UnsupportedOSPlatform("browser")]
	public static void AddOID(string oid, params string[] names)
	{
		ArgumentNullException.ThrowIfNull(oid, "oid");
		ArgumentNullException.ThrowIfNull(names, "names");
		string[] array = new string[names.Length];
		Array.Copy(names, array, array.Length);
		string[] array2 = array;
		foreach (string argument in array2)
		{
			ArgumentException.ThrowIfNullOrEmpty(argument, "names");
		}
		string[] array3 = array;
		foreach (string key in array3)
		{
			appOidHT[key] = oid;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public static string? MapNameToOID(string name)
	{
		ArgumentNullException.ThrowIfNull(name, "name");
		appOidHT.TryGetValue(name, out var value);
		if (string.IsNullOrEmpty(value) && !DefaultOidHT.TryGetValue(name, out value))
		{
			try
			{
				Oid oid = Oid.FromFriendlyName(name, OidGroup.All);
				value = oid.Value;
				return value;
			}
			catch (CryptographicException)
			{
			}
		}
		return value;
	}

	[UnsupportedOSPlatform("browser")]
	[Obsolete("EncodeOID is obsolete. Use the ASN.1 functionality provided in System.Formats.Asn1.", DiagnosticId = "SYSLIB0031", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static byte[] EncodeOID(string str)
	{
		ArgumentNullException.ThrowIfNull(str, "str");
		string[] array = str.Split('.');
		uint[] array2 = new uint[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = (uint)int.Parse(array[i], CultureInfo.InvariantCulture);
		}
		if (array2.Length < 2)
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_InvalidOID);
		}
		uint value = array2[0] * 40 + array2[1];
		int index = 2;
		EncodeSingleOidNum(value, null, ref index);
		for (int j = 2; j < array2.Length; j++)
		{
			EncodeSingleOidNum(array2[j], null, ref index);
		}
		byte[] array3 = new byte[index];
		int index2 = 2;
		EncodeSingleOidNum(value, array3, ref index2);
		for (int k = 2; k < array2.Length; k++)
		{
			EncodeSingleOidNum(array2[k], array3, ref index2);
		}
		if (index2 - 2 > 127)
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_Config_EncodedOIDError);
		}
		array3[0] = 6;
		array3[1] = (byte)(index2 - 2);
		return array3;
	}

	private static void EncodeSingleOidNum(uint value, byte[] destination, ref int index)
	{
		if ((int)value < 128)
		{
			if (destination != null)
			{
				destination[index++] = (byte)value;
			}
			else
			{
				index++;
			}
		}
		else if (value < 16384)
		{
			if (destination != null)
			{
				destination[index++] = (byte)((value >> 7) | 0x80u);
				destination[index++] = (byte)(value & 0x7Fu);
			}
			else
			{
				index += 2;
			}
		}
		else if (value < 2097152)
		{
			if (destination != null)
			{
				destination[index++] = (byte)((value >> 14) | 0x80u);
				destination[index++] = (byte)((value >> 7) | 0x80u);
				destination[index++] = (byte)(value & 0x7Fu);
			}
			else
			{
				index += 3;
			}
		}
		else if (value < 268435456)
		{
			if (destination != null)
			{
				destination[index++] = (byte)((value >> 21) | 0x80u);
				destination[index++] = (byte)((value >> 14) | 0x80u);
				destination[index++] = (byte)((value >> 7) | 0x80u);
				destination[index++] = (byte)(value & 0x7Fu);
			}
			else
			{
				index += 4;
			}
		}
		else if (destination != null)
		{
			destination[index++] = (byte)((value >> 28) | 0x80u);
			destination[index++] = (byte)((value >> 21) | 0x80u);
			destination[index++] = (byte)((value >> 14) | 0x80u);
			destination[index++] = (byte)((value >> 7) | 0x80u);
			destination[index++] = (byte)(value & 0x7Fu);
		}
		else
		{
			index += 5;
		}
	}
}
