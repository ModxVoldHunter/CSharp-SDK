namespace System.Reflection;

internal enum SignatureCallingConvention : byte
{
	Default = 0,
	Cdecl = 1,
	StdCall = 2,
	ThisCall = 3,
	FastCall = 4,
	Unmanaged = 9
}
