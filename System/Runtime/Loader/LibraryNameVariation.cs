using System.Collections.Generic;

namespace System.Runtime.Loader;

internal struct LibraryNameVariation
{
	public string Prefix;

	public string Suffix;

	public LibraryNameVariation(string prefix, string suffix)
	{
		Prefix = prefix;
		Suffix = suffix;
	}

	internal static IEnumerable<LibraryNameVariation> DetermineLibraryNameVariations(string libName, bool isRelativePath)
	{
		yield return new LibraryNameVariation(string.Empty, string.Empty);
		if (isRelativePath && !libName.EndsWith('.') && !libName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && !libName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
		{
			yield return new LibraryNameVariation(string.Empty, ".dll");
		}
	}
}
