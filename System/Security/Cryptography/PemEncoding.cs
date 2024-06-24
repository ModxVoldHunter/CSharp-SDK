using System.Buffers.Text;
using System.Runtime.CompilerServices;

namespace System.Security.Cryptography;

public static class PemEncoding
{
	public static PemFields Find(ReadOnlySpan<char> pemData)
	{
		if (!TryFind(pemData, out var fields))
		{
			throw new ArgumentException(System.SR.Argument_PemEncoding_NoPemFound, "pemData");
		}
		return fields;
	}

	public static bool TryFind(ReadOnlySpan<char> pemData, out PemFields fields)
	{
		if (pemData.Length < "-----BEGIN ".Length + "-----".Length * 2 + "-----END ".Length)
		{
			fields = default(PemFields);
			return false;
		}
		Span<char> span = stackalloc char[256];
		int num = 0;
		int num2;
		while ((num2 = pemData.IndexOfByOffset("-----BEGIN ", num)) >= 0)
		{
			int num3 = num2 + "-----BEGIN ".Length;
			if (num2 > 0 && !IsWhiteSpaceCharacter(pemData[num2 - 1]))
			{
				num = num3;
				continue;
			}
			int num4 = pemData.IndexOfByOffset("-----", num3);
			if (num4 < 0)
			{
				fields = default(PemFields);
				return false;
			}
			Range range = num3..num4;
			Range range2 = range;
			ReadOnlySpan<char> readOnlySpan = pemData[range2.Start..range2.End];
			if (IsValidLabel(readOnlySpan))
			{
				int num5 = num4 + "-----".Length;
				int num6 = "-----END ".Length + readOnlySpan.Length + "-----".Length;
				Span<char> destination2 = ((num6 > 256) ? ((Span<char>)new char[num6]) : span);
				ReadOnlySpan<char> value = WritePostEB(readOnlySpan, destination2);
				int num7 = pemData.IndexOfByOffset(value, num5);
				if (num7 >= 0)
				{
					int num8 = num7 + num6;
					if (num8 >= pemData.Length - 1 || IsWhiteSpaceCharacter(pemData[num8]))
					{
						Range range3 = num5..num7;
						range2 = range3;
						if (TryCountBase64(pemData[range2.Start..range2.End], out var base64Start, out var base64End, out var base64DecodedSize))
						{
							Range location = num2..num8;
							Range base64data = (num5 + base64Start)..(num5 + base64End);
							fields = new PemFields(range, base64data, location, base64DecodedSize);
							return true;
						}
					}
				}
			}
			if (num4 <= num)
			{
				fields = default(PemFields);
				return false;
			}
			num = num4;
		}
		fields = default(PemFields);
		return false;
		static ReadOnlySpan<char> WritePostEB(ReadOnlySpan<char> label, Span<char> destination)
		{
			int length = "-----END ".Length + label.Length + "-----".Length;
			"-----END ".CopyTo(destination);
			label.CopyTo(destination.Slice("-----END ".Length));
			"-----".CopyTo(destination.Slice("-----END ".Length + label.Length));
			return destination.Slice(0, length);
		}
	}

	private static int IndexOfByOffset(this ReadOnlySpan<char> str, ReadOnlySpan<char> value, int startPosition)
	{
		int num = str.Slice(startPosition).IndexOf(value);
		if (num != -1)
		{
			return num + startPosition;
		}
		return -1;
	}

	private static bool IsValidLabel(ReadOnlySpan<char> data)
	{
		if (data.IsEmpty)
		{
			return true;
		}
		bool flag = false;
		for (int i = 0; i < data.Length; i++)
		{
			char c2 = data[i];
			if (IsLabelChar(c2))
			{
				flag = true;
				continue;
			}
			if ((c2 != ' ' && c2 != '-') || !flag)
			{
				return false;
			}
			flag = false;
		}
		return flag;
		static bool IsLabelChar(char c)
		{
			if ((uint)(c - 33) <= 93u)
			{
				return c != '-';
			}
			return false;
		}
	}

	private static bool TryCountBase64(ReadOnlySpan<char> str, out int base64Start, out int base64End, out int base64DecodedSize)
	{
		int i = 0;
		int num = str.Length - 1;
		for (; i < str.Length && IsWhiteSpaceCharacter(str[i]); i++)
		{
		}
		while (num > i && IsWhiteSpaceCharacter(str[num]))
		{
			num--;
		}
		if (Base64.IsValid(str.Slice(i, num + 1 - i), out base64DecodedSize))
		{
			base64Start = i;
			base64End = num + 1;
			return true;
		}
		base64Start = 0;
		base64End = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsWhiteSpaceCharacter(char ch)
	{
		switch (ch)
		{
		case '\t':
		case '\n':
		case '\r':
		case ' ':
			return true;
		default:
			return false;
		}
	}

	public static int GetEncodedSize(int labelLength, int dataLength)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(labelLength, "labelLength");
		ArgumentOutOfRangeException.ThrowIfNegative(dataLength, "dataLength");
		if (labelLength > 1073741808)
		{
			throw new ArgumentOutOfRangeException("labelLength", System.SR.Argument_PemEncoding_EncodedSizeTooLarge);
		}
		if (dataLength > 1585834053)
		{
			throw new ArgumentOutOfRangeException("dataLength", System.SR.Argument_PemEncoding_EncodedSizeTooLarge);
		}
		int num = "-----BEGIN ".Length + labelLength + "-----".Length;
		int num2 = "-----END ".Length + labelLength + "-----".Length;
		int num3 = num + num2 + 1;
		int num4 = (dataLength + 2) / 3 << 2;
		int result;
		int num5 = Math.DivRem(num4, 64, out result);
		if (result > 0)
		{
			num5++;
		}
		int num6 = num4 + num5;
		if (int.MaxValue - num6 < num3)
		{
			throw new ArgumentException(System.SR.Argument_PemEncoding_EncodedSizeTooLarge);
		}
		return num6 + num3;
	}

	public static bool TryWrite(ReadOnlySpan<char> label, ReadOnlySpan<byte> data, Span<char> destination, out int charsWritten)
	{
		if (!IsValidLabel(label))
		{
			throw new ArgumentException(System.SR.Argument_PemEncoding_InvalidLabel, "label");
		}
		int encodedSize = GetEncodedSize(label.Length, data.Length);
		if (destination.Length < encodedSize)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = WriteCore(label, data, destination);
		return true;
	}

	private static int WriteCore(ReadOnlySpan<char> label, ReadOnlySpan<byte> data, Span<char> destination)
	{
		int num = 0;
		num += Write("-----BEGIN ", destination, num);
		num += Write(label, destination, num);
		num += Write("-----", destination, num);
		num += Write("\n", destination, num);
		ReadOnlySpan<byte> bytes2 = data;
		while (bytes2.Length >= 48)
		{
			num += WriteBase64(bytes2.Slice(0, 48), destination, num);
			num += Write("\n", destination, num);
			bytes2 = bytes2.Slice(48);
		}
		if (bytes2.Length > 0)
		{
			num += WriteBase64(bytes2, destination, num);
			num += Write("\n", destination, num);
		}
		num += Write("-----END ", destination, num);
		num += Write(label, destination, num);
		return num + Write("-----", destination, num);
		static int Write(ReadOnlySpan<char> str, Span<char> dest, int offset)
		{
			str.CopyTo(dest.Slice(offset));
			return str.Length;
		}
		static int WriteBase64(ReadOnlySpan<byte> bytes, Span<char> dest, int offset)
		{
			if (!Convert.TryToBase64Chars(bytes, dest.Slice(offset), out var charsWritten))
			{
				throw new ArgumentException(null, "destination");
			}
			return charsWritten;
		}
	}

	public static char[] Write(ReadOnlySpan<char> label, ReadOnlySpan<byte> data)
	{
		if (!IsValidLabel(label))
		{
			throw new ArgumentException(System.SR.Argument_PemEncoding_InvalidLabel, "label");
		}
		int encodedSize = GetEncodedSize(label.Length, data.Length);
		char[] array = new char[encodedSize];
		int num = WriteCore(label, data, array);
		return array;
	}

	public unsafe static string WriteString(ReadOnlySpan<char> label, ReadOnlySpan<byte> data)
	{
		if (!IsValidLabel(label))
		{
			throw new ArgumentException(System.SR.Argument_PemEncoding_InvalidLabel, "label");
		}
		int encodedSize = GetEncodedSize(label.Length, data.Length);
		return string.Create(encodedSize, ((nint)(&label), (nint)(&data)), delegate(Span<char> destination, (nint LabelPointer, nint DataPointer) state)
		{
			ReadOnlySpan<char> item = Unsafe.Read<ReadOnlySpan<char>>((void*)state.LabelPointer);
			ReadOnlySpan<byte> item2 = Unsafe.Read<ReadOnlySpan<byte>>((void*)state.DataPointer);
			int num = WriteCore(item, item2, destination);
			if (num != destination.Length)
			{
				throw new CryptographicException();
			}
		});
	}
}
