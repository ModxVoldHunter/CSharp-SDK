namespace System.Net.NetworkInformation;

public class PingOptions
{
	private int _ttl;

	private bool _dontFragment;

	public int Ttl
	{
		get
		{
			return _ttl;
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, "value");
			_ttl = value;
		}
	}

	public bool DontFragment
	{
		get
		{
			return _dontFragment;
		}
		set
		{
			_dontFragment = value;
		}
	}

	public PingOptions()
	{
		_ttl = 128;
	}

	public PingOptions(int ttl, bool dontFragment)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ttl, "ttl");
		_ttl = ttl;
		_dontFragment = dontFragment;
	}
}
