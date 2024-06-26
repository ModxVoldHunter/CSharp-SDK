namespace System.Transactions;

internal static class EnterpriseServices
{
	internal static bool CreatedServiceDomain { get; }

	internal static void VerifyEnterpriseServicesOk()
	{
		_ = 0;
		ThrowNotSupported();
	}

	internal static void PushServiceDomain()
	{
		ThrowNotSupported();
	}

	internal static void LeaveServiceDomain()
	{
		ThrowNotSupported();
	}

	private static void ThrowNotSupported()
	{
		throw new PlatformNotSupportedException(System.SR.EsNotSupported);
	}
}
