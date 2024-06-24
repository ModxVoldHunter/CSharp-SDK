using System.Collections.Generic;

namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class DynamicAttribute : Attribute
{
	private readonly bool[] _transformFlags;

	public IList<bool> TransformFlags => _transformFlags;

	public DynamicAttribute()
	{
		_transformFlags = new bool[1] { true };
	}

	public DynamicAttribute(bool[] transformFlags)
	{
		ArgumentNullException.ThrowIfNull(transformFlags, "transformFlags");
		_transformFlags = transformFlags;
	}
}
