using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace System.Transactions.DtcProxyShim;

[NativeMarshalling(typeof(Marshaller))]
internal struct Xactopt
{
	[CustomMarshaller(typeof(Xactopt), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
	[CustomMarshaller(typeof(Xactopt), MarshalMode.UnmanagedToManagedIn, typeof(Marshaller))]
	internal static class Marshaller
	{
		internal struct XactoptNative
		{
			public uint UlTimeout;

			public unsafe fixed byte SzDescription[40];
		}

		public unsafe static XactoptNative ConvertToUnmanaged(Xactopt managed)
		{
			XactoptNative xactoptNative = default(XactoptNative);
			xactoptNative.UlTimeout = managed.UlTimeout;
			XactoptNative result = xactoptNative;
			Encoding.ASCII.TryGetBytes(managed.SzDescription, new Span<byte>(result.SzDescription, 40), out var _);
			return result;
		}

		public unsafe static Xactopt ConvertToManaged(XactoptNative unmanaged)
		{
			return new Xactopt(unmanaged.UlTimeout, Encoding.ASCII.GetString(unmanaged.SzDescription, 40));
		}
	}

	public uint UlTimeout;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
	public string SzDescription;

	internal Xactopt(uint ulTimeout, string szDescription)
	{
		UlTimeout = ulTimeout;
		SzDescription = szDescription;
	}
}
