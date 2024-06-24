using System.Text;

namespace System.IO.Compression;

internal static class ZipHelper
{
	private static readonly DateTime s_invalidDateIndicator = new DateTime(1980, 1, 1, 0, 0, 0);

	internal static Encoding GetEncoding(string text)
	{
		if (text.AsSpan().ContainsAnyExceptInRange(' ', '~'))
		{
			return Encoding.UTF8;
		}
		return Encoding.ASCII;
	}

	internal static void ReadBytes(Stream stream, byte[] buffer, int bytesToRead)
	{
		int num = stream.ReadAtLeast(buffer.AsSpan(0, bytesToRead), bytesToRead, throwOnEndOfStream: false);
		if (num < bytesToRead)
		{
			throw new IOException(System.SR.UnexpectedEndOfStream);
		}
	}

	internal static DateTime DosTimeToDateTime(uint dateTime)
	{
		if (dateTime == 0)
		{
			return s_invalidDateIndicator;
		}
		int year = (int)(1980 + (dateTime >> 25));
		int month = (int)((dateTime >> 21) & 0xF);
		int day = (int)((dateTime >> 16) & 0x1F);
		int hour = (int)((dateTime >> 11) & 0x1F);
		int minute = (int)((dateTime >> 5) & 0x3F);
		int second = (int)((dateTime & 0x1F) * 2);
		try
		{
			return new DateTime(year, month, day, hour, minute, second, 0);
		}
		catch (ArgumentOutOfRangeException)
		{
			return s_invalidDateIndicator;
		}
		catch (ArgumentException)
		{
			return s_invalidDateIndicator;
		}
	}

	internal static uint DateTimeToDosTime(DateTime dateTime)
	{
		int num = (dateTime.Year - 1980) & 0x7F;
		num = (num << 4) + dateTime.Month;
		num = (num << 5) + dateTime.Day;
		num = (num << 5) + dateTime.Hour;
		num = (num << 6) + dateTime.Minute;
		return (uint)((num << 5) + dateTime.Second / 2);
	}

	internal static bool SeekBackwardsToSignature(Stream stream, uint signatureToFind, int maxBytesToRead)
	{
		int bufferPointer = 0;
		uint num = 0u;
		byte[] array = new byte[32];
		bool flag = false;
		bool flag2 = false;
		int num2 = 0;
		while (!flag2 && !flag && num2 <= maxBytesToRead)
		{
			flag = SeekBackwardsAndRead(stream, array, out bufferPointer);
			while (bufferPointer >= 0 && !flag2)
			{
				num = (num << 8) | array[bufferPointer];
				if (num == signatureToFind)
				{
					flag2 = true;
				}
				else
				{
					bufferPointer--;
				}
			}
			num2 += array.Length;
		}
		if (!flag2)
		{
			return false;
		}
		stream.Seek(bufferPointer, SeekOrigin.Current);
		return true;
	}

	internal static void AdvanceToPosition(this Stream stream, long position)
	{
		long num = position - stream.Position;
		if (num <= 0)
		{
			return;
		}
		byte[] array = new byte[64];
		do
		{
			int count = (int)Math.Min(num, array.Length);
			int num2 = stream.Read(array, 0, count);
			if (num2 == 0)
			{
				throw new IOException(System.SR.UnexpectedEndOfStream);
			}
			num -= num2;
		}
		while (num > 0);
	}

	private static bool SeekBackwardsAndRead(Stream stream, byte[] buffer, out int bufferPointer)
	{
		if (stream.Position >= buffer.Length)
		{
			stream.Seek(-buffer.Length, SeekOrigin.Current);
			ReadBytes(stream, buffer, buffer.Length);
			stream.Seek(-buffer.Length, SeekOrigin.Current);
			bufferPointer = buffer.Length - 1;
			return false;
		}
		int num = (int)stream.Position;
		stream.Seek(0L, SeekOrigin.Begin);
		ReadBytes(stream, buffer, num);
		stream.Seek(0L, SeekOrigin.Begin);
		bufferPointer = num - 1;
		return true;
	}

	internal static byte[] GetEncodedTruncatedBytesFromString(string text, Encoding encoding, int maxBytes, out bool isUTF8)
	{
		if (string.IsNullOrEmpty(text))
		{
			isUTF8 = false;
			return Array.Empty<byte>();
		}
		if (encoding == null)
		{
			encoding = GetEncoding(text);
		}
		isUTF8 = encoding.CodePage == 65001;
		if (maxBytes == 0)
		{
			return encoding.GetBytes(text);
		}
		byte[] bytes;
		if (isUTF8 && encoding.GetMaxByteCount(text.Length) > maxBytes)
		{
			int num = 0;
			foreach (Rune item in text.EnumerateRunes())
			{
				if (num + item.Utf8SequenceLength > maxBytes)
				{
					break;
				}
				num += item.Utf8SequenceLength;
			}
			bytes = encoding.GetBytes(text);
			return bytes[0..num];
		}
		bytes = encoding.GetBytes(text);
		if (maxBytes >= bytes.Length)
		{
			return bytes;
		}
		return bytes[0..maxBytes];
	}
}
