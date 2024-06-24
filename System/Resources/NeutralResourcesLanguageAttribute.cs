using System.ComponentModel;

namespace System.Resources;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class NeutralResourcesLanguageAttribute : Attribute
{
	public string CultureName { get; }

	public UltimateResourceFallbackLocation Location { get; }

	public NeutralResourcesLanguageAttribute(string cultureName)
	{
		ArgumentNullException.ThrowIfNull(cultureName, "cultureName");
		CultureName = cultureName;
		Location = UltimateResourceFallbackLocation.MainAssembly;
	}

	public NeutralResourcesLanguageAttribute(string cultureName, UltimateResourceFallbackLocation location)
	{
		ArgumentNullException.ThrowIfNull(cultureName, "cultureName");
		if (!Enum.IsDefined(location))
		{
			throw new ArgumentException(SR.Format(SR.Arg_InvalidNeutralResourcesLanguage_FallbackLoc, location));
		}
		CultureName = cultureName;
		Location = location;
	}
}
