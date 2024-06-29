using System.Runtime.Versioning;

namespace System.IO;

public sealed class FileStreamOptions
{
	private FileMode _mode = FileMode.Open;

	private FileAccess _access = FileAccess.Read;

	private FileShare _share = FileShare.Read;

	private FileOptions _options;

	private long _preallocationSize;

	private int _bufferSize = 4096;

	private UnixFileMode? _unixCreateMode;

	public FileMode Mode
	{
		get
		{
			return _mode;
		}
		set
		{
			if (value < FileMode.CreateNew || value > FileMode.Append)
			{
				ThrowHelper.ArgumentOutOfRangeException_Enum_Value();
			}
			_mode = value;
		}
	}

	public FileAccess Access
	{
		get
		{
			return _access;
		}
		set
		{
			if (value < FileAccess.Read || value > FileAccess.ReadWrite)
			{
				ThrowHelper.ArgumentOutOfRangeException_Enum_Value();
			}
			_access = value;
		}
	}

	public FileShare Share
	{
		get
		{
			return _share;
		}
		set
		{
			FileShare fileShare = value & ~FileShare.Inheritable;
			if ((fileShare < FileShare.None) || fileShare > (FileShare.ReadWrite | FileShare.Delete))
			{
				ThrowHelper.ArgumentOutOfRangeException_Enum_Value();
			}
			_share = value;
		}
	}

	public FileOptions Options
	{
		get
		{
			return _options;
		}
		set
		{
			if (value != 0 && (value & (FileOptions)67092479) != 0)
			{
				ThrowHelper.ArgumentOutOfRangeException_Enum_Value();
			}
			_options = value;
		}
	}

	public long PreallocationSize
	{
		get
		{
			return _preallocationSize;
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value, "value");
			_preallocationSize = value;
		}
	}

	public int BufferSize
	{
		get
		{
			return _bufferSize;
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value, "value");
			_bufferSize = value;
		}
	}

	public UnixFileMode? UnixCreateMode
	{
		get
		{
			return _unixCreateMode;
		}
		[UnsupportedOSPlatform("windows")]
		set
		{
			_ = 1;
			throw new PlatformNotSupportedException(SR.PlatformNotSupported_UnixFileMode);
		}
	}
}
