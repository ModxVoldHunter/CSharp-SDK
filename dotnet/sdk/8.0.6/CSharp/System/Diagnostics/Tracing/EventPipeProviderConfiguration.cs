using System.Runtime.InteropServices;

namespace System.Diagnostics.Tracing;

internal struct EventPipeProviderConfiguration
{
	[MarshalAs(UnmanagedType.LPWStr)]
	private readonly string m_providerName;

	private readonly ulong m_keywords;

	private readonly uint m_loggingLevel;

	[MarshalAs(UnmanagedType.LPWStr)]
	private readonly string m_filterData;

	internal string ProviderName => m_providerName;

	internal ulong Keywords => m_keywords;

	internal uint LoggingLevel => m_loggingLevel;

	internal string FilterData => m_filterData;

	internal EventPipeProviderConfiguration(string providerName, ulong keywords, uint loggingLevel, string filterData)
	{
		ArgumentException.ThrowIfNullOrEmpty(providerName, "providerName");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(loggingLevel, 5u, "loggingLevel");
		m_providerName = providerName;
		m_keywords = keywords;
		m_loggingLevel = loggingLevel;
		m_filterData = filterData;
	}
}
