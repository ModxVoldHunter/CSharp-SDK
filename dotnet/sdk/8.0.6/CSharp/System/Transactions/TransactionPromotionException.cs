using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Transactions;

[Serializable]
[TypeForwardedFrom("System.Transactions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class TransactionPromotionException : TransactionException
{
	public TransactionPromotionException()
		: this(System.SR.PromotionFailed)
	{
	}

	public TransactionPromotionException(string? message)
		: base(message)
	{
	}

	public TransactionPromotionException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected TransactionPromotionException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
