using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("59313E03-B36C-11cf-A539-00AA006887C3")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionReceiver_003EFA95655E7CA5C9C75B92C80B658DAD09F0B33DBD6BD50F627A8DA29FE70D46153__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionReceiver_003EFA95655E7CA5C9C75B92C80B658DAD09F0B33DBD6BD50F627A8DA29FE70D46153__InterfaceImplementation>]
internal interface ITransactionReceiver
{
	void UnmarshalPropagationToken(uint cbToken, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] rgbToken, [MarshalAs(UnmanagedType.Interface)] out ITransaction ppTransaction);

	void GetReturnTokenSize(out uint pcbReturnToken);

	void MarshalReturnToken(uint cbReturnToken, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] out byte[] rgbReturnToken, out uint pcbUsed);

	void Reset();
}
