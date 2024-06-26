using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers;

internal sealed class AltSvcHeaderValue
{
	public static AltSvcHeaderValue Clear { get; } = new AltSvcHeaderValue("clear", null, 0, TimeSpan.Zero, persist: false);


	public string AlpnProtocolName { get; }

	public string Host { get; }

	public int Port { get; }

	public TimeSpan MaxAge { get; }

	public bool Persist { get; }

	public AltSvcHeaderValue(string alpnProtocolName, string host, int port, TimeSpan maxAge, bool persist)
	{
		AlpnProtocolName = alpnProtocolName;
		Host = host;
		Port = port;
		MaxAge = maxAge;
		Persist = persist;
	}

	public override string ToString()
	{
		Span<char> initialBuffer = stackalloc char[256];
		System.Text.ValueStringBuilder valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		valueStringBuilder.Append(AlpnProtocolName);
		valueStringBuilder.Append("=\"");
		if (Host != null)
		{
			valueStringBuilder.Append(Host);
		}
		valueStringBuilder.Append(':');
		valueStringBuilder.AppendSpanFormattable((uint)Port);
		valueStringBuilder.Append('"');
		if (MaxAge != TimeSpan.FromTicks(864000000000L))
		{
			valueStringBuilder.Append("; ma=");
			valueStringBuilder.AppendSpanFormattable(MaxAge.Ticks / 10000000, null, CultureInfo.InvariantCulture);
		}
		if (Persist)
		{
			valueStringBuilder.Append("; persist=1");
		}
		return valueStringBuilder.ToString();
	}
}
