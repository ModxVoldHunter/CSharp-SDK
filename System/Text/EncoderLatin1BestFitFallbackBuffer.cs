using System.Runtime.CompilerServices;

namespace System.Text;

internal sealed class EncoderLatin1BestFitFallbackBuffer : EncoderFallbackBuffer
{
	private char _cBestFit;

	private int _iCount = -1;

	private int _iSize;

	public override int Remaining
	{
		get
		{
			if (_iCount <= 0)
			{
				return 0;
			}
			return _iCount;
		}
	}

	private static ReadOnlySpan<char> ArrayCharBestFit => RuntimeHelpers.CreateSpan<char>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	public override bool Fallback(char charUnknown, int index)
	{
		_iCount = (_iSize = 1);
		_cBestFit = TryBestFit(charUnknown);
		if (_cBestFit == '\0')
		{
			_cBestFit = '?';
		}
		return true;
	}

	public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
	{
		if (!char.IsHighSurrogate(charUnknownHigh))
		{
			throw new ArgumentOutOfRangeException("charUnknownHigh", SR.Format(SR.ArgumentOutOfRange_Range, 55296, 56319));
		}
		if (!char.IsLowSurrogate(charUnknownLow))
		{
			throw new ArgumentOutOfRangeException("charUnknownLow", SR.Format(SR.ArgumentOutOfRange_Range, 56320, 57343));
		}
		_cBestFit = '?';
		_iCount = (_iSize = 2);
		return true;
	}

	public override char GetNextChar()
	{
		_iCount--;
		if (_iCount < 0)
		{
			return '\0';
		}
		if (_iCount == int.MaxValue)
		{
			_iCount = -1;
			return '\0';
		}
		return _cBestFit;
	}

	public override bool MovePrevious()
	{
		if (_iCount >= 0)
		{
			_iCount++;
		}
		if (_iCount >= 0)
		{
			return _iCount <= _iSize;
		}
		return false;
	}

	public unsafe override void Reset()
	{
		_iCount = -1;
		charStart = null;
		bFallingBack = false;
	}

	private static char TryBestFit(char cUnknown)
	{
		int num = 0;
		int num2 = ArrayCharBestFit.Length;
		int num3;
		while ((num3 = num2 - num) > 6)
		{
			int num4 = (num3 / 2 + num) & 0xFFFE;
			char c = ArrayCharBestFit[num4];
			if (c == cUnknown)
			{
				return ArrayCharBestFit[num4 + 1];
			}
			if (c < cUnknown)
			{
				num = num4;
			}
			else
			{
				num2 = num4;
			}
		}
		for (int num4 = num; num4 < num2; num4 += 2)
		{
			if (ArrayCharBestFit[num4] == cUnknown)
			{
				return ArrayCharBestFit[num4 + 1];
			}
		}
		return '\0';
	}
}
