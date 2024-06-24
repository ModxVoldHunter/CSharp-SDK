using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;

namespace System.Net;

public class WebProxy : IWebProxy, ISerializable
{
	private sealed class ChangeTrackingArrayList : ArrayList
	{
		public volatile bool IsChanged;

		public override object this[int index]
		{
			get
			{
				return base[index];
			}
			set
			{
				IsChanged = true;
				base[index] = value;
			}
		}

		public ChangeTrackingArrayList()
		{
		}

		public ChangeTrackingArrayList(ICollection c)
			: base(c)
		{
		}

		public override int Add(object value)
		{
			IsChanged = true;
			return base.Add(value);
		}

		public override void AddRange(ICollection c)
		{
			IsChanged = true;
			base.AddRange(c);
		}

		public override void Insert(int index, object value)
		{
			IsChanged = true;
			base.Insert(index, value);
		}

		public override void InsertRange(int index, ICollection c)
		{
			IsChanged = true;
			base.InsertRange(index, c);
		}

		public override void SetRange(int index, ICollection c)
		{
			IsChanged = true;
			base.SetRange(index, c);
		}

		public override void Remove(object obj)
		{
			IsChanged = true;
			base.Remove(obj);
		}

		public override void RemoveAt(int index)
		{
			IsChanged = true;
			base.RemoveAt(index);
		}

		public override void RemoveRange(int index, int count)
		{
			IsChanged = true;
			base.RemoveRange(index, count);
		}

		public override void Clear()
		{
			IsChanged = true;
			base.Clear();
		}
	}

	private ChangeTrackingArrayList _bypassList;

	private Regex[] _regexBypassList;

	private static volatile string s_domainName;

	private static volatile IPAddress[] s_localAddresses;

	private static int s_networkChangeRegistered;

	public Uri? Address { get; set; }

	public bool BypassProxyOnLocal { get; set; }

	public string[] BypassList
	{
		get
		{
			if (_bypassList == null)
			{
				return Array.Empty<string>();
			}
			string[] array = new string[_bypassList.Count];
			_bypassList.CopyTo(array);
			return array;
		}
		[param: AllowNull]
		set
		{
			_bypassList = ((value != null) ? new ChangeTrackingArrayList(value) : null);
			UpdateRegexList();
		}
	}

	public ArrayList BypassArrayList => _bypassList ?? (_bypassList = new ChangeTrackingArrayList());

	public ICredentials? Credentials { get; set; }

	public bool UseDefaultCredentials
	{
		get
		{
			return Credentials == CredentialCache.DefaultCredentials;
		}
		set
		{
			Credentials = (value ? CredentialCache.DefaultCredentials : null);
		}
	}

	public WebProxy()
		: this((Uri?)null, BypassOnLocal: false, (string[]?)null, (ICredentials?)null)
	{
	}

	public WebProxy(Uri? Address)
		: this(Address, BypassOnLocal: false, null, null)
	{
	}

	public WebProxy(Uri? Address, bool BypassOnLocal)
		: this(Address, BypassOnLocal, null, null)
	{
	}

	public WebProxy(Uri? Address, bool BypassOnLocal, [StringSyntax("Regex", new object[] { RegexOptions.IgnoreCase | RegexOptions.CultureInvariant })] string[]? BypassList)
		: this(Address, BypassOnLocal, BypassList, null)
	{
	}

	public WebProxy(Uri? Address, bool BypassOnLocal, [StringSyntax("Regex", new object[] { RegexOptions.IgnoreCase | RegexOptions.CultureInvariant })] string[]? BypassList, ICredentials? Credentials)
	{
		this.Address = Address;
		this.Credentials = Credentials;
		BypassProxyOnLocal = BypassOnLocal;
		if (BypassList != null)
		{
			_bypassList = new ChangeTrackingArrayList(BypassList);
			UpdateRegexList();
		}
	}

	public WebProxy(string Host, int Port)
		: this(CreateProxyUri(Host, Port), BypassOnLocal: false, null, null)
	{
	}

	public WebProxy(string? Address)
		: this(CreateProxyUri(Address), BypassOnLocal: false, null, null)
	{
	}

	public WebProxy(string? Address, bool BypassOnLocal)
		: this(CreateProxyUri(Address), BypassOnLocal, null, null)
	{
	}

	public WebProxy(string? Address, bool BypassOnLocal, [StringSyntax("Regex", new object[] { RegexOptions.IgnoreCase | RegexOptions.CultureInvariant })] string[]? BypassList)
		: this(CreateProxyUri(Address), BypassOnLocal, BypassList, null)
	{
	}

	public WebProxy(string? Address, bool BypassOnLocal, [StringSyntax("Regex", new object[] { RegexOptions.IgnoreCase | RegexOptions.CultureInvariant })] string[]? BypassList, ICredentials? Credentials)
		: this(CreateProxyUri(Address), BypassOnLocal, BypassList, Credentials)
	{
	}

	public Uri? GetProxy(Uri destination)
	{
		ArgumentNullException.ThrowIfNull(destination, "destination");
		if (!IsBypassed(destination))
		{
			return Address;
		}
		return destination;
	}

	private static Uri CreateProxyUri(string address, int? port = null)
	{
		if (address == null)
		{
			return null;
		}
		if (!address.Contains("://", StringComparison.Ordinal))
		{
			address = "http://" + address;
		}
		Uri uri = new Uri(address);
		if (port.HasValue && uri.IsAbsoluteUri)
		{
			uri = new UriBuilder(uri)
			{
				Port = port.Value
			}.Uri;
		}
		return uri;
	}

	private void UpdateRegexList()
	{
		ChangeTrackingArrayList bypassList = _bypassList;
		if (bypassList != null)
		{
			Regex[] array = null;
			if (bypassList.Count > 0)
			{
				array = new Regex[bypassList.Count];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = new Regex((string)bypassList[i], RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
				}
			}
			_regexBypassList = array;
			bypassList.IsChanged = false;
		}
		else
		{
			_regexBypassList = null;
		}
	}

	private bool IsMatchInBypassList(Uri input)
	{
		ChangeTrackingArrayList bypassList = _bypassList;
		if (bypassList != null && bypassList.IsChanged)
		{
			try
			{
				UpdateRegexList();
			}
			catch
			{
				_regexBypassList = null;
			}
		}
		Regex[] regexBypassList = _regexBypassList;
		if (regexBypassList != null)
		{
			bool isDefaultPort = input.IsDefaultPort;
			int num = input.Scheme.Length + 3 + input.Host.Length;
			if (!isDefaultPort)
			{
				num += 6;
			}
			Span<char> span = ((num > 256) ? ((Span<char>)new char[num]) : stackalloc char[256]);
			Span<char> span2 = span;
			bool num2;
			int charsWritten;
			if (!isDefaultPort)
			{
				Span<char> span3 = span2;
				Span<char> destination = span3;
				bool shouldAppend;
				MemoryExtensions.TryWriteInterpolatedStringHandler handler = new MemoryExtensions.TryWriteInterpolatedStringHandler(4, 3, span3, out shouldAppend);
				if (shouldAppend && handler.AppendFormatted(input.Scheme) && handler.AppendLiteral("://") && handler.AppendFormatted(input.Host) && handler.AppendLiteral(":"))
				{
					handler.AppendFormatted((uint)input.Port);
				}
				else
					_ = 0;
				num2 = destination.TryWrite(ref handler, out charsWritten);
			}
			else
			{
				Span<char> span3 = span2;
				Span<char> destination2 = span3;
				bool shouldAppend2;
				MemoryExtensions.TryWriteInterpolatedStringHandler handler2 = new MemoryExtensions.TryWriteInterpolatedStringHandler(3, 2, span3, out shouldAppend2);
				if (shouldAppend2 && handler2.AppendFormatted(input.Scheme) && handler2.AppendLiteral("://"))
				{
					handler2.AppendFormatted(input.Host);
				}
				else
					_ = 0;
				num2 = destination2.TryWrite(ref handler2, out charsWritten);
			}
			bool flag = num2;
			span2 = span2.Slice(0, charsWritten);
			Regex[] array = regexBypassList;
			foreach (Regex regex in array)
			{
				if (regex.IsMatch(span2))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsBypassed(Uri host)
	{
		ArgumentNullException.ThrowIfNull(host, "host");
		if (!(Address == null) && (!BypassProxyOnLocal || !IsLocal(host)))
		{
			return IsMatchInBypassList(host);
		}
		return true;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected WebProxy(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	protected virtual void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	[Obsolete("WebProxy.GetDefaultProxy has been deprecated. Use the proxy selected for you by default.")]
	public static WebProxy GetDefaultProxy()
	{
		throw new PlatformNotSupportedException();
	}

	private static bool IsLocal(Uri host)
	{
		if (host.IsLoopback)
		{
			return true;
		}
		string host2 = host.Host;
		if (IPAddress.TryParse(host2, out IPAddress address))
		{
			EnsureNetworkChangeRegistration();
			IPAddress[] array = s_localAddresses ?? (s_localAddresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList);
			return Array.IndexOf(array, address) != -1;
		}
		int num = host2.IndexOf('.');
		if (num == -1)
		{
			return true;
		}
		EnsureNetworkChangeRegistration();
		string text = s_domainName ?? (s_domainName = "." + IPGlobalProperties.GetIPGlobalProperties().DomainName);
		if (text.Length == host2.Length - num)
		{
			return string.Compare(text, 0, host2, num, text.Length, StringComparison.OrdinalIgnoreCase) == 0;
		}
		return false;
	}

	private static void EnsureNetworkChangeRegistration()
	{
		if (s_networkChangeRegistered == 0)
		{
			Register();
		}
		static void Register()
		{
			if (Interlocked.Exchange(ref s_networkChangeRegistered, 1) == 0)
			{
				NetworkChange.NetworkAddressChanged += delegate
				{
					s_domainName = null;
					s_localAddresses = null;
				};
				NetworkChange.NetworkAvailabilityChanged += delegate
				{
					s_domainName = null;
					s_localAddresses = null;
				};
			}
		}
	}
}
