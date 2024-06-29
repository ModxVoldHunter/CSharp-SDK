using System.Text.Encodings.Web;

namespace System.Text.Json;

public struct JsonWriterOptions
{
	private int _maxDepth;

	private int _optionsMask;

	public JavaScriptEncoder? Encoder { get; set; }

	public bool Indented
	{
		get
		{
			return (_optionsMask & 1) != 0;
		}
		set
		{
			if (value)
			{
				_optionsMask |= 1;
			}
			else
			{
				_optionsMask &= -2;
			}
		}
	}

	public int MaxDepth
	{
		readonly get
		{
			return _maxDepth;
		}
		set
		{
			if (value < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_MaxDepthMustBePositive("value");
			}
			_maxDepth = value;
		}
	}

	public bool SkipValidation
	{
		get
		{
			return (_optionsMask & 2) != 0;
		}
		set
		{
			if (value)
			{
				_optionsMask |= 2;
			}
			else
			{
				_optionsMask &= -3;
			}
		}
	}

	internal bool IndentedOrNotSkipValidation => _optionsMask != 2;
}
