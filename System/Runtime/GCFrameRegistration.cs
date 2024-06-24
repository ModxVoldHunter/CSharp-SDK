using System.Runtime.CompilerServices;

namespace System.Runtime;

internal struct GCFrameRegistration
{
	private nuint _reserved1;

	private nuint _reserved2;

	private unsafe void** _pObjRefs;

	private uint _numObjRefs;

	private int _maybeInterior;

	public unsafe GCFrameRegistration(void** allocation, uint elemCount, bool areByRefs = true)
	{
		_reserved1 = 0u;
		_reserved2 = 0u;
		_pObjRefs = allocation;
		_numObjRefs = elemCount;
		_maybeInterior = (areByRefs ? 1 : 0);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal unsafe static extern void RegisterForGCReporting(GCFrameRegistration* pRegistration);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal unsafe static extern void UnregisterForGCReporting(GCFrameRegistration* pRegistration);
}
