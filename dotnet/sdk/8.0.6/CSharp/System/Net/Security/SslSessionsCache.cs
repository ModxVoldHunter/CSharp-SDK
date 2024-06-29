using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;

namespace System.Net.Security;

internal static class SslSessionsCache
{
	private readonly struct SslCredKey : IEquatable<SslCredKey>
	{
		private readonly byte[] _thumbPrint;

		private readonly int _allowedProtocols;

		private readonly EncryptionPolicy _encryptionPolicy;

		private readonly bool _isServerMode;

		private readonly bool _sendTrustList;

		private readonly bool _checkRevocation;

		private readonly bool _allowTlsResume;

		internal SslCredKey(byte[] thumbPrint, int allowedProtocols, bool isServerMode, EncryptionPolicy encryptionPolicy, bool sendTrustList, bool checkRevocation, bool allowTlsResume)
		{
			_thumbPrint = thumbPrint ?? Array.Empty<byte>();
			_allowedProtocols = allowedProtocols;
			_encryptionPolicy = encryptionPolicy;
			_isServerMode = isServerMode;
			_checkRevocation = checkRevocation;
			_sendTrustList = sendTrustList;
			_allowTlsResume = allowTlsResume;
		}

		public override int GetHashCode()
		{
			int num = 0;
			if (_thumbPrint.Length > 3)
			{
				num ^= _thumbPrint[0] | (_thumbPrint[1] << 8) | (_thumbPrint[2] << 16) | (_thumbPrint[3] << 24);
			}
			return HashCode.Combine(_allowedProtocols, (int)_encryptionPolicy, _isServerMode, _sendTrustList, _checkRevocation, _allowedProtocols, num);
		}

		public override bool Equals([NotNullWhen(true)] object obj)
		{
			if (obj is SslCredKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public bool Equals(SslCredKey other)
		{
			byte[] thumbPrint = _thumbPrint;
			byte[] thumbPrint2 = other._thumbPrint;
			if (thumbPrint.Length == thumbPrint2.Length && _encryptionPolicy == other._encryptionPolicy && _allowedProtocols == other._allowedProtocols && _isServerMode == other._isServerMode && _sendTrustList == other._sendTrustList && _checkRevocation == other._checkRevocation && _allowTlsResume == other._allowTlsResume)
			{
				return thumbPrint.AsSpan().SequenceEqual(thumbPrint2);
			}
			return false;
		}
	}

	private static readonly ConcurrentDictionary<SslCredKey, SafeCredentialReference> s_cachedCreds = new ConcurrentDictionary<SslCredKey, SafeCredentialReference>();

	internal static SafeFreeCredentials TryCachedCredential(byte[] thumbPrint, SslProtocols sslProtocols, bool isServer, EncryptionPolicy encryptionPolicy, bool checkRevocation, bool allowTlsResume, bool sendTrustList)
	{
		if (s_cachedCreds.IsEmpty)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, $"Not found, Current Cache Count = {s_cachedCreds.Count}", "TryCachedCredential");
			}
			return null;
		}
		SslCredKey key = new SslCredKey(thumbPrint, (int)sslProtocols, isServer, encryptionPolicy, sendTrustList, checkRevocation, allowTlsResume);
		SafeFreeCredentials cachedCredential = GetCachedCredential(key);
		if (cachedCredential == null || cachedCredential.IsClosed || cachedCredential.IsInvalid || cachedCredential.Expiry < DateTime.UtcNow)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, $"Not found or invalid, Current Cache Count = {s_cachedCreds.Count}", "TryCachedCredential");
			}
			return null;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(null, $"Found a cached Handle = {cachedCredential}", "TryCachedCredential");
		}
		return cachedCredential;
	}

	private static SafeFreeCredentials GetCachedCredential(SslCredKey key)
	{
		if (!s_cachedCreds.TryGetValue(key, out var value))
		{
			return null;
		}
		return value.Target;
	}

	internal static void CacheCredential(SafeFreeCredentials creds, byte[] thumbPrint, SslProtocols sslProtocols, bool isServer, EncryptionPolicy encryptionPolicy, bool checkRevocation, bool allowTlsResume, bool sendTrustList)
	{
		if (creds.IsInvalid)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, $"Refused to cache an Invalid Handle {creds}, Current Cache Count = {s_cachedCreds.Count}", "CacheCredential");
			}
			return;
		}
		SslCredKey key = new SslCredKey(thumbPrint, (int)sslProtocols, isServer, encryptionPolicy, sendTrustList, checkRevocation, allowTlsResume);
		SafeFreeCredentials cachedCredential = GetCachedCredential(key);
		DateTime utcNow = DateTime.UtcNow;
		if (cachedCredential == null || cachedCredential.IsClosed || cachedCredential.IsInvalid || cachedCredential.Expiry < utcNow)
		{
			lock (s_cachedCreds)
			{
				cachedCredential = GetCachedCredential(key);
				if (cachedCredential == null || cachedCredential.IsClosed || cachedCredential.IsInvalid || cachedCredential.Expiry < utcNow)
				{
					SafeCredentialReference safeCredentialReference = SafeCredentialReference.CreateReference(creds);
					if (safeCredentialReference != null)
					{
						s_cachedCreds[key] = safeCredentialReference;
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(null, $"Caching New Handle = {creds}, Current Cache Count = {s_cachedCreds.Count}", "CacheCredential");
						}
						ShrinkCredentialCache();
					}
				}
				else if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(null, $"CacheCredential() (locked retry) Found already cached Handle = {cachedCredential}", "CacheCredential");
				}
				return;
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(null, $"CacheCredential() Ignoring incoming handle = {creds} since found already cached Handle = {cachedCredential}", "CacheCredential");
		}
		static void ShrinkCredentialCache()
		{
			if (s_cachedCreds.Count % 32 == 0)
			{
				KeyValuePair<SslCredKey, SafeCredentialReference>[] array = s_cachedCreds.ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					SafeCredentialReference value = array[i].Value;
					SafeFreeCredentials target = value.Target;
					SafeCredentialReference value2;
					if (target == null)
					{
						s_cachedCreds.TryRemove(array[i].Key, out value2);
					}
					else
					{
						value.Dispose();
						value = SafeCredentialReference.CreateReference(target);
						if (value != null)
						{
							s_cachedCreds[array[i].Key] = value;
						}
						else
						{
							s_cachedCreds.TryRemove(array[i].Key, out value2);
						}
					}
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(null, $"Scavenged cache, New Cache Count = {s_cachedCreds.Count}", "CacheCredential");
				}
			}
		}
	}
}
