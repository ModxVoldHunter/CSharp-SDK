using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("59313E01-B36C-11cf-A539-00AA006887C3")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionTransmitter_003EF3665447C32FB5B43FB802B423EB4106A11458CB878E9879D0B126787E0ECF584__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionTransmitter_003EF3665447C32FB5B43FB802B423EB4106A11458CB878E9879D0B126787E0ECF584__InterfaceImplementation>]
internal interface ITransactionTransmitter
{
	void Set([MarshalAs(UnmanagedType.Interface)] ITransaction transaction);

	void GetPropagationTokenSize(out uint pcbToken);

	void MarshalPropagationToken(uint cbToken, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] rgbToken, out uint pcbUsed);

	void UnmarshalReturnToken(uint cbReturnToken, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] rgbToken);

	void Reset();
}
