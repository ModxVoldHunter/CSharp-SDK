using System.Runtime.CompilerServices;

namespace System;

[InlineArray(3)]
internal struct ThreeObjects
{
	internal object Arg0;

	public ThreeObjects(object arg0, object arg1, object arg2)
	{
		this = default(ThreeObjects);
		_003CPrivateImplementationDetails_003E.InlineArrayFirstElementRef<ThreeObjects, object>(ref this) = arg0;
		_003CPrivateImplementationDetails_003E.InlineArrayElementRef<ThreeObjects, object>(ref this, 1) = arg1;
		_003CPrivateImplementationDetails_003E.InlineArrayElementRef<ThreeObjects, object>(ref this, 2) = arg2;
	}
}
