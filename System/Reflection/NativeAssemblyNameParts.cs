namespace System.Reflection;

internal struct NativeAssemblyNameParts
{
	public unsafe char* _pName;

	public ushort _major;

	public ushort _minor;

	public ushort _build;

	public ushort _revision;

	public unsafe char* _pCultureName;

	public unsafe byte* _pPublicKeyOrToken;

	public int _cbPublicKeyOrToken;

	public AssemblyNameFlags _flags;

	public void SetVersion(Version version, ushort defaultValue)
	{
		if (version != null)
		{
			_major = (ushort)version.Major;
			_minor = (ushort)version.Minor;
			_build = (ushort)version.Build;
			_revision = (ushort)version.Revision;
		}
		else
		{
			_major = defaultValue;
			_minor = defaultValue;
			_build = defaultValue;
			_revision = defaultValue;
		}
	}

	public Version GetVersion()
	{
		if (_major == ushort.MaxValue || _minor == ushort.MaxValue)
		{
			return null;
		}
		if (_build == ushort.MaxValue)
		{
			return new Version(_major, _minor);
		}
		if (_revision == ushort.MaxValue)
		{
			return new Version(_major, _minor, _build);
		}
		return new Version(_major, _minor, _build, _revision);
	}
}
