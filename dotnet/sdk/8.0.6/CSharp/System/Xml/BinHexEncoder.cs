using System.Threading.Tasks;

namespace System.Xml;

internal static class BinHexEncoder
{
	internal static void Encode(byte[] buffer, int index, int count, XmlWriter writer)
	{
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		ArgumentOutOfRangeException.ThrowIfNegative(index, "index");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - index, "count");
		char[] array = new char[(count * 2 < 128) ? (count * 2) : 128];
		int num = index + count;
		while (index < num)
		{
			int num2 = ((count < 64) ? count : 64);
			System.HexConverter.EncodeToUtf16(buffer.AsSpan(index, num2), array);
			writer.WriteRaw(array, 0, num2 * 2);
			index += num2;
			count -= num2;
		}
	}

	internal static string Encode(byte[] inArray, int offsetIn, int count)
	{
		return Convert.ToHexString(inArray, offsetIn, count);
	}

	internal static Task EncodeAsync(byte[] buffer, int index, int count, XmlWriter writer)
	{
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		if (index < 0 || (uint)count > buffer.Length - index)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count");
		}
		return Core(buffer, index, count, writer);
		static async Task Core(byte[] buffer, int index, int count, XmlWriter writer)
		{
			char[] chars = new char[(count * 2 < 128) ? (count * 2) : 128];
			int endIndex = index + count;
			while (index < endIndex)
			{
				int cnt = ((count < 64) ? count : 64);
				System.HexConverter.EncodeToUtf16(buffer.AsSpan(index, cnt), chars);
				await writer.WriteRawAsync(chars, 0, cnt * 2).ConfigureAwait(continueOnCapturedContext: false);
				index += cnt;
				count -= cnt;
			}
		}
	}
}
