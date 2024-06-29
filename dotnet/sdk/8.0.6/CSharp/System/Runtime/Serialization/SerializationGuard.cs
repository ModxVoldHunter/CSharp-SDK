using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization;

internal static class SerializationGuard
{
	public static void ThrowIfDeserializationInProgress(string switchSuffix, ref int cachedValue)
	{
		ThrowIfDeserializationInProgress(null, switchSuffix, ref cachedValue);
		[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ThrowIfDeserializationInProgress")]
		static extern void ThrowIfDeserializationInProgress(SerializationInfo thisPtr, string switchSuffix, ref int cachedValue);
	}
}
