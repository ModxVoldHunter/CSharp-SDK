namespace System.IO;

public class FileSystemEventArgs : EventArgs
{
	private readonly WatcherChangeTypes _changeType;

	private readonly string _name;

	private readonly string _fullPath;

	public WatcherChangeTypes ChangeType => _changeType;

	public string FullPath => _fullPath;

	public string? Name => _name;

	public FileSystemEventArgs(WatcherChangeTypes changeType, string directory, string? name)
	{
		ArgumentNullException.ThrowIfNull(directory, "directory");
		_changeType = changeType;
		_name = name;
		_fullPath = Combine(directory, name);
	}

	internal static string Combine(string directory, string name)
	{
		if (directory.Length != 0)
		{
			if (System.IO.PathInternal.IsDirectorySeparator(directory[directory.Length - 1]))
			{
				return directory + name;
			}
		}
		return directory + "\\" + name;
	}
}
