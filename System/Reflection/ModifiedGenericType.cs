using System.Threading;

namespace System.Reflection;

internal sealed class ModifiedGenericType : ModifiedType
{
	private Type[] _genericArguments;

	internal ModifiedGenericType(Type unmodifiedType, TypeSignature typeSignature)
		: base(unmodifiedType, typeSignature)
	{
	}

	public override Type[] GetGenericArguments()
	{
		return (Type[])(_genericArguments ?? Initialize()).Clone();
		Type[] Initialize()
		{
			Type[] genericArguments = base.UnmodifiedType.GetGenericArguments();
			for (int i = 0; i < genericArguments.Length; i++)
			{
				genericArguments[i] = GetTypeParameter(genericArguments[i], i);
			}
			Interlocked.CompareExchange(ref _genericArguments, genericArguments, null);
			return _genericArguments;
		}
	}
}
