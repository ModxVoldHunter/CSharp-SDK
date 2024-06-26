using System.ComponentModel;

namespace System.Runtime.InteropServices;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ErrorWrapper
{
	public int ErrorCode { get; }

	public ErrorWrapper(int errorCode)
	{
		ErrorCode = errorCode;
	}

	public ErrorWrapper(object errorCode)
	{
		if (!(errorCode is int))
		{
			throw new ArgumentException(SR.Arg_MustBeInt32, "errorCode");
		}
		ErrorCode = (int)errorCode;
	}

	public ErrorWrapper(Exception e)
	{
		ErrorCode = Marshal.GetHRForException(e);
	}
}
