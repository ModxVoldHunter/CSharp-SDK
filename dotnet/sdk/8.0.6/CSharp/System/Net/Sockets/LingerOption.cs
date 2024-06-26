namespace System.Net.Sockets;

public class LingerOption
{
	private bool _enabled;

	private int _lingerTime;

	public bool Enabled
	{
		get
		{
			return _enabled;
		}
		set
		{
			_enabled = value;
		}
	}

	public int LingerTime
	{
		get
		{
			return _lingerTime;
		}
		set
		{
			_lingerTime = value;
		}
	}

	public LingerOption(bool enable, int seconds)
	{
		Enabled = enable;
		LingerTime = seconds;
	}

	public override bool Equals(object? comparand)
	{
		if (comparand is LingerOption lingerOption && lingerOption.Enabled == _enabled)
		{
			return lingerOption.LingerTime == _lingerTime;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(_enabled, _lingerTime);
	}
}
