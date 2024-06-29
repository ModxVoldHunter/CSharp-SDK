using System.Collections.Immutable;

namespace System.Runtime.InteropServices;

public static class ImmutableCollectionsMarshal
{
	public static ImmutableArray<T> AsImmutableArray<T>(T[]? array)
	{
		return new ImmutableArray<T>(array);
	}

	public static T[]? AsArray<T>(ImmutableArray<T> array)
	{
		return array.array;
	}
}
