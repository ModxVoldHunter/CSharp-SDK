using System.ComponentModel;

namespace System.Runtime.InteropServices;

[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("CurrencyWrapper and support for marshalling to the VARIANT type may be unavailable in future releases.")]
public sealed class CurrencyWrapper
{
	public decimal WrappedObject { get; }

	public CurrencyWrapper(decimal obj)
	{
		WrappedObject = obj;
	}

	public CurrencyWrapper(object obj)
	{
		if (!(obj is decimal))
		{
			throw new ArgumentException(SR.Arg_MustBeDecimal, "obj");
		}
		WrappedObject = (decimal)obj;
	}
}
