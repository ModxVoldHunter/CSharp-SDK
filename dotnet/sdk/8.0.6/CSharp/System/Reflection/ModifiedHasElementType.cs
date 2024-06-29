using System.Threading;

namespace System.Reflection;

internal sealed class ModifiedHasElementType : ModifiedType
{
	private Type _elementType;

	internal ModifiedHasElementType(Type unmodifiedType, TypeSignature typeSignature)
		: base(unmodifiedType, typeSignature)
	{
	}

	public override Type GetElementType()
	{
		return _elementType ?? Initialize();
		Type Initialize()
		{
			Interlocked.CompareExchange(ref _elementType, GetTypeParameter(base.UnmodifiedType.GetElementType(), 0), null);
			return _elementType;
		}
	}
}
