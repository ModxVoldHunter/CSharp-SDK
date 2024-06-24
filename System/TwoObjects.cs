using System.Runtime.CompilerServices;

namespace System;

[InlineArray(2)]
internal struct TwoObjects
{
	internal object Arg0;

	public TwoObjects(object arg0, object arg1)
	{
		this = default(TwoObjects);
		_003CPrivateImplementationDetails_003E.InlineArrayFirstElementRef<TwoObjects, object>(ref this) = arg0;
		_003CPrivateImplementationDetails_003E.InlineArrayElementRef<TwoObjects, object>(ref this, 1) = arg1;
	}
}
