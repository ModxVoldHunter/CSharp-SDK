namespace System.Net;

internal sealed class SyncRequestContext : RequestContextBase
{
	internal unsafe SyncRequestContext(int size)
	{
		BaseConstruction(Allocate(size));
	}

	private unsafe global::Interop.HttpApi.HTTP_REQUEST* Allocate(int newSize)
	{
		if (base.Size != 0 && base.Size == newSize)
		{
			return base.RequestBlob;
		}
		SetBuffer(newSize);
		if (base.RequestBuffer != IntPtr.Zero)
		{
			return (global::Interop.HttpApi.HTTP_REQUEST*)((IntPtr)base.RequestBuffer).ToPointer();
		}
		return null;
	}

	internal unsafe void Reset(int size)
	{
		SetBlob(Allocate(size));
	}

	protected override void OnReleasePins()
	{
	}
}
