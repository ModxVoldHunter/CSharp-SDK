using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Xml;

public class UniqueId
{
	private long _idLow;

	private long _idHigh;

	private string _s;

	private const int guidLength = 16;

	private const int uuidLength = 45;

	private static ReadOnlySpan<short> Char2val => RuntimeHelpers.CreateSpan<short>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	public int CharArrayLength
	{
		get
		{
			if (_s != null)
			{
				return _s.Length;
			}
			return 45;
		}
	}

	public bool IsGuid => (_idLow | _idHigh) != 0;

	public UniqueId()
		: this(Guid.NewGuid())
	{
	}

	public UniqueId(Guid guid)
		: this(guid.ToByteArray())
	{
	}

	public UniqueId(byte[] guid)
		: this(guid, 0)
	{
	}

	public unsafe UniqueId(byte[] guid, int offset)
	{
		ArgumentNullException.ThrowIfNull(guid, "guid");
		ArgumentOutOfRangeException.ThrowIfNegative(offset, "offset");
		if (offset > guid.Length)
		{
			throw new ArgumentOutOfRangeException("offset", System.SR.Format(System.SR.OffsetExceedsBufferSize, guid.Length));
		}
		if (16 > guid.Length - offset)
		{
			throw new ArgumentException(System.SR.Format(System.SR.XmlArrayTooSmallInput, 16), "guid");
		}
		fixed (byte* ptr = &guid[offset])
		{
			_idLow = UnsafeGetInt64(ptr);
			_idHigh = UnsafeGetInt64(ptr + 8);
		}
	}

	public unsafe UniqueId(string value)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		if (value.Length == 0)
		{
			throw new FormatException(System.SR.XmlInvalidUniqueId);
		}
		fixed (char* chars = value)
		{
			UnsafeParse(chars, value.Length);
		}
		_s = value;
	}

	public unsafe UniqueId(char[] chars, int offset, int count)
	{
		ArgumentNullException.ThrowIfNull(chars, "chars");
		ArgumentOutOfRangeException.ThrowIfNegative(offset, "offset");
		if (offset > chars.Length)
		{
			throw new ArgumentOutOfRangeException("offset", System.SR.Format(System.SR.OffsetExceedsBufferSize, chars.Length));
		}
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		if (count > chars.Length - offset)
		{
			throw new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, chars.Length - offset));
		}
		if (count == 0)
		{
			throw new FormatException(System.SR.XmlInvalidUniqueId);
		}
		fixed (char* chars2 = &chars[offset])
		{
			UnsafeParse(chars2, count);
		}
		if (!IsGuid)
		{
			_s = new string(chars, offset, count);
		}
	}

	private unsafe int UnsafeDecode(short* char2val, char ch1, char ch2)
	{
		if ((ch1 | ch2) >= 128)
		{
			return 256;
		}
		return char2val[(int)ch1] | char2val[128 + ch2];
	}

	private unsafe static void UnsafeEncode(byte b, char* pch)
	{
		*pch = System.HexConverter.ToCharLower(b >> 4);
		pch[1] = System.HexConverter.ToCharLower(b);
	}

	private unsafe void UnsafeParse(char* chars, int charCount)
	{
		if (charCount != 45 || *chars != 'u' || chars[1] != 'r' || chars[2] != 'n' || chars[3] != ':' || chars[4] != 'u' || chars[5] != 'u' || chars[6] != 'i' || chars[7] != 'd' || chars[8] != ':' || chars[17] != '-' || chars[22] != '-' || chars[27] != '-' || chars[32] != '-')
		{
			return;
		}
		byte* ptr = stackalloc byte[16];
		int num = 0;
		int num2 = 0;
		fixed (short* ptr2 = &Char2val[0])
		{
			short* char2val = ptr2;
			num = UnsafeDecode(char2val, chars[15], chars[16]);
			*ptr = (byte)num;
			num2 |= num;
			num = UnsafeDecode(char2val, chars[13], chars[14]);
			ptr[1] = (byte)num;
			num2 |= num;
			num = UnsafeDecode(char2val, chars[11], chars[12]);
			ptr[2] = (byte)num;
			num2 |= num;
			num = UnsafeDecode(char2val, chars[9], chars[10]);
			ptr[3] = (byte)num;
			num2 |= num;
			num = UnsafeDecode(char2val, chars[20], chars[21]);
			ptr[4] = (byte)num;
			num2 |= num;
			num = UnsafeDecode(char2val, chars[18], chars[19]);
			ptr[5] = (byte)num;
			num2 |= num;
			num = UnsafeDecode(char2val, chars[25], chars[26]);
			ptr[6] = (byte)num;
			num2 |= num;
			num = UnsafeDecode(char2val, chars[23], chars[24]);
			ptr[7] = (byte)num;
			num2 |= num;
			num = UnsafeDecode(char2val, chars[28], chars[29]);
			ptr[8] = (byte)num;
			num2 |= num;
			num = UnsafeDecode(char2val, chars[30], chars[31]);
			ptr[9] = (byte)num;
			num2 |= num;
			num = UnsafeDecode(char2val, chars[33], chars[34]);
			ptr[10] = (byte)num;
			num2 |= num;
			num = UnsafeDecode(char2val, chars[35], chars[36]);
			ptr[11] = (byte)num;
			num2 |= num;
			num = UnsafeDecode(char2val, chars[37], chars[38]);
			ptr[12] = (byte)num;
			num2 |= num;
			num = UnsafeDecode(char2val, chars[39], chars[40]);
			ptr[13] = (byte)num;
			num2 |= num;
			num = UnsafeDecode(char2val, chars[41], chars[42]);
			ptr[14] = (byte)num;
			num2 |= num;
			num = UnsafeDecode(char2val, chars[43], chars[44]);
			ptr[15] = (byte)num;
			num2 |= num;
			if (num2 >= 256)
			{
				return;
			}
			_idLow = UnsafeGetInt64(ptr);
			_idHigh = UnsafeGetInt64(ptr + 8);
		}
	}

	public int ToCharArray(char[] chars, int offset)
	{
		ArgumentNullException.ThrowIfNull(chars, "chars");
		int charArrayLength = CharArrayLength;
		ArgumentOutOfRangeException.ThrowIfNegative(offset, "offset");
		if (offset > chars.Length)
		{
			throw new ArgumentOutOfRangeException("offset", System.SR.Format(System.SR.OffsetExceedsBufferSize, chars.Length));
		}
		if (charArrayLength > chars.Length - offset)
		{
			throw new ArgumentOutOfRangeException("chars", System.SR.Format(System.SR.XmlArrayTooSmallOutput, charArrayLength));
		}
		ToSpan(chars.AsSpan(offset, charArrayLength));
		return charArrayLength;
	}

	private unsafe void ToSpan(Span<char> chars)
	{
		if (_s != null)
		{
			_s.CopyTo(chars);
			return;
		}
		byte* ptr = stackalloc byte[16];
		UnsafeSetInt64(_idLow, ptr);
		UnsafeSetInt64(_idHigh, ptr + 8);
		fixed (char* ptr2 = &chars[0])
		{
			char* ptr3 = ptr2;
			*ptr3 = 'u';
			ptr3[1] = 'r';
			ptr3[2] = 'n';
			ptr3[3] = ':';
			ptr3[4] = 'u';
			ptr3[5] = 'u';
			ptr3[6] = 'i';
			ptr3[7] = 'd';
			ptr3[8] = ':';
			ptr3[17] = '-';
			ptr3[22] = '-';
			ptr3[27] = '-';
			ptr3[32] = '-';
			UnsafeEncode(*ptr, ptr3 + 15);
			UnsafeEncode(ptr[1], ptr3 + 13);
			UnsafeEncode(ptr[2], ptr3 + 11);
			UnsafeEncode(ptr[3], ptr3 + 9);
			UnsafeEncode(ptr[4], ptr3 + 20);
			UnsafeEncode(ptr[5], ptr3 + 18);
			UnsafeEncode(ptr[6], ptr3 + 25);
			UnsafeEncode(ptr[7], ptr3 + 23);
			UnsafeEncode(ptr[8], ptr3 + 28);
			UnsafeEncode(ptr[9], ptr3 + 30);
			UnsafeEncode(ptr[10], ptr3 + 33);
			UnsafeEncode(ptr[11], ptr3 + 35);
			UnsafeEncode(ptr[12], ptr3 + 37);
			UnsafeEncode(ptr[13], ptr3 + 39);
			UnsafeEncode(ptr[14], ptr3 + 41);
			UnsafeEncode(ptr[15], ptr3 + 43);
		}
	}

	public bool TryGetGuid(out Guid guid)
	{
		byte[] array = new byte[16];
		if (!TryGetGuid(array, 0))
		{
			guid = Guid.Empty;
			return false;
		}
		guid = new Guid(array);
		return true;
	}

	public unsafe bool TryGetGuid(byte[] buffer, int offset)
	{
		if (!IsGuid)
		{
			return false;
		}
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		ArgumentOutOfRangeException.ThrowIfNegative(offset, "offset");
		if (offset > buffer.Length)
		{
			throw new ArgumentOutOfRangeException("offset", System.SR.Format(System.SR.OffsetExceedsBufferSize, buffer.Length));
		}
		if (16 > buffer.Length - offset)
		{
			throw new ArgumentOutOfRangeException("buffer", System.SR.Format(System.SR.XmlArrayTooSmallOutput, 16));
		}
		fixed (byte* ptr = &buffer[offset])
		{
			UnsafeSetInt64(_idLow, ptr);
			UnsafeSetInt64(_idHigh, ptr + 8);
		}
		return true;
	}

	public override string ToString()
	{
		return _s ?? (_s = string.Create(CharArrayLength, this, delegate(Span<char> destination, UniqueId thisRef)
		{
			thisRef.ToSpan(destination);
		}));
	}

	public static bool operator ==(UniqueId? id1, UniqueId? id2)
	{
		if ((object)id1 == id2)
		{
			return true;
		}
		if ((object)id1 == null || (object)id2 == null)
		{
			return false;
		}
		if (id1.IsGuid && id2.IsGuid)
		{
			if (id1._idLow == id2._idLow)
			{
				return id1._idHigh == id2._idHigh;
			}
			return false;
		}
		return id1.ToString() == id2.ToString();
	}

	public static bool operator !=(UniqueId? id1, UniqueId? id2)
	{
		return !(id1 == id2);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return this == obj as UniqueId;
	}

	public override int GetHashCode()
	{
		if (IsGuid)
		{
			long num = _idLow ^ _idHigh;
			return (int)(num >> 32) ^ (int)num;
		}
		return ToString().GetHashCode();
	}

	private unsafe long UnsafeGetInt64(byte* pb)
	{
		int num = UnsafeGetInt32(pb);
		int num2 = UnsafeGetInt32(pb + 4);
		return ((long)num2 << 32) | (uint)num;
	}

	private unsafe int UnsafeGetInt32(byte* pb)
	{
		int num = pb[3];
		num <<= 8;
		num |= pb[2];
		num <<= 8;
		num |= pb[1];
		num <<= 8;
		return num | *pb;
	}

	private unsafe void UnsafeSetInt64(long value, byte* pb)
	{
		UnsafeSetInt32((int)value, pb);
		UnsafeSetInt32((int)(value >> 32), pb + 4);
	}

	private unsafe void UnsafeSetInt32(int value, byte* pb)
	{
		*pb = (byte)value;
		value >>= 8;
		pb[1] = (byte)value;
		value >>= 8;
		pb[2] = (byte)value;
		value >>= 8;
		pb[3] = (byte)value;
	}
}
