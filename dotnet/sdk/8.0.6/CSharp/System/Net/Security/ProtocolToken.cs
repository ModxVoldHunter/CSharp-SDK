namespace System.Net.Security;

internal struct ProtocolToken
{
	internal SecurityStatusPal Status;

	internal byte[] Payload;

	internal int Size;

	internal bool Failed
	{
		get
		{
			if (Status.ErrorCode != SecurityStatusPalErrorCode.OK)
			{
				return Status.ErrorCode != SecurityStatusPalErrorCode.ContinueNeeded;
			}
			return false;
		}
	}

	internal bool Done => Status.ErrorCode == SecurityStatusPalErrorCode.OK;

	internal Exception GetException()
	{
		if (!Done)
		{
			return SslStreamPal.GetException(Status);
		}
		return null;
	}
}
