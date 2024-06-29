using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;

namespace System.Text.Json;

internal static class JsonHelpers
{
	[StructLayout(LayoutKind.Auto)]
	private struct DateTimeParseData
	{
		public int Year;

		public int Month;

		public int Day;

		public bool IsCalendarDateOnly;

		public int Hour;

		public int Minute;

		public int Second;

		public int Fraction;

		public int OffsetHours;

		public int OffsetMinutes;

		public byte OffsetToken;

		public bool OffsetNegative => OffsetToken == 45;
	}

	private static ReadOnlySpan<int> DaysToMonth365 => RuntimeHelpers.CreateSpan<int>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<int> DaysToMonth366 => RuntimeHelpers.CreateSpan<int>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	internal static bool RequiresSpecialNumberHandlingOnWrite(JsonNumberHandling? handling)
	{
		if (!handling.HasValue)
		{
			return false;
		}
		return (handling.Value & (JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowNamedFloatingPointLiterals)) != 0;
	}

	internal static void StableSortByKey<T, TKey>(this List<T> items, Func<T, TKey> keySelector) where TKey : unmanaged, IComparable<TKey>
	{
		Span<T> items2 = CollectionsMarshal.AsSpan(items);
		Span<(TKey, int)> span = ((items2.Length > 32) ? ((Span<(TKey, int)>)new(TKey, int)[items2.Length]) : stackalloc(TKey, int)[32].Slice(0, items2.Length));
		Span<(TKey, int)> keys = span;
		for (int i = 0; i < keys.Length; i++)
		{
			keys[i] = (keySelector(items2[i]), i);
		}
		keys.Sort(items2);
	}

	public static T[] TraverseGraphWithTopologicalSort<T>(T entryNode, Func<T, ICollection<T>> getChildren, IEqualityComparer<T> comparer = null)
	{
		if (comparer == null)
		{
			comparer = EqualityComparer<T>.Default;
		}
		List<T> list = new List<T> { entryNode };
		Dictionary<T, int> dictionary = new Dictionary<T, int>(comparer) { [entryNode] = 0 };
		List<bool[]> list2 = new List<bool[]>();
		Queue<int> queue = new Queue<int>();
		for (int i = 0; i < list.Count; i++)
		{
			T arg = list[i];
			ICollection<T> collection = getChildren(arg);
			int count = collection.Count;
			if (count == 0)
			{
				list2.Add(null);
				queue.Enqueue(i);
				continue;
			}
			bool[] array = new bool[Math.Max(list.Count, count)];
			foreach (T item in collection)
			{
				if (!dictionary.TryGetValue(item, out var value))
				{
					value = list.Count;
					dictionary.Add(item, value);
					list.Add(item);
				}
				if (value >= array.Length)
				{
					Array.Resize(ref array, value + 1);
				}
				array[value] = true;
			}
			list2.Add(array);
		}
		T[] array2 = new T[list.Count];
		int num = array2.Length;
		do
		{
			int num2 = queue.Dequeue();
			array2[--num] = list[num2];
			for (int j = 0; j < list2.Count; j++)
			{
				bool[] array3 = list2[j];
				if (array3 != null && num2 < array3.Length && array3[num2])
				{
					array3[num2] = false;
					if (array3.AsSpan().IndexOf(value: true) == -1)
					{
						queue.Enqueue(j);
					}
				}
			}
		}
		while (queue.Count > 0);
		return array2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<byte> GetSpan(this scoped ref Utf8JsonReader reader)
	{
		if (!reader.HasValueSequence)
		{
			return reader.ValueSpan;
		}
		ReadOnlySequence<byte> sequence = reader.ValueSequence;
		return BuffersExtensions.ToArray(in sequence);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInRangeInclusive(uint value, uint lowerBound, uint upperBound)
	{
		return value - lowerBound <= upperBound - lowerBound;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInRangeInclusive(int value, int lowerBound, int upperBound)
	{
		return (uint)(value - lowerBound) <= (uint)(upperBound - lowerBound);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInRangeInclusive(long value, long lowerBound, long upperBound)
	{
		return (ulong)(value - lowerBound) <= (ulong)(upperBound - lowerBound);
	}

	public static bool IsDigit(byte value)
	{
		return (uint)(value - 48) <= 9u;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ReadWithVerify(this ref Utf8JsonReader reader)
	{
		bool flag = reader.Read();
	}

	public static string Utf8GetString(ReadOnlySpan<byte> bytes)
	{
		return Encoding.UTF8.GetString(bytes);
	}

	public static Dictionary<TKey, TValue> CreateDictionaryFromCollection<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
	{
		return new Dictionary<TKey, TValue>(collection, comparer);
	}

	public static bool IsFinite(double value)
	{
		return double.IsFinite(value);
	}

	public static bool IsFinite(float value)
	{
		return float.IsFinite(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidateInt32MaxArrayLength(uint length)
	{
		if (length > 2146435071)
		{
			ThrowHelper.ThrowOutOfMemoryException(length);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsValidDateTimeOffsetParseLength(int length)
	{
		return IsInRangeInclusive(length, 10, 252);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsValidUnescapedDateTimeOffsetParseLength(int length)
	{
		return IsInRangeInclusive(length, 10, 42);
	}

	public static bool TryParseAsISO(ReadOnlySpan<byte> source, out DateTime value)
	{
		if (!TryParseDateTimeOffset(source, out var parseData))
		{
			value = default(DateTime);
			return false;
		}
		if (parseData.OffsetToken == 90)
		{
			return TryCreateDateTime(parseData, DateTimeKind.Utc, out value);
		}
		if (parseData.OffsetToken == 43 || parseData.OffsetToken == 45)
		{
			if (!TryCreateDateTimeOffset(ref parseData, out var value2))
			{
				value = default(DateTime);
				return false;
			}
			value = value2.LocalDateTime;
			return true;
		}
		return TryCreateDateTime(parseData, DateTimeKind.Unspecified, out value);
	}

	public static bool TryParseAsISO(ReadOnlySpan<byte> source, out DateTimeOffset value)
	{
		if (!TryParseDateTimeOffset(source, out var parseData))
		{
			value = default(DateTimeOffset);
			return false;
		}
		if (parseData.OffsetToken == 90 || parseData.OffsetToken == 43 || parseData.OffsetToken == 45)
		{
			return TryCreateDateTimeOffset(ref parseData, out value);
		}
		return TryCreateDateTimeOffsetInterpretingDataAsLocalTime(parseData, out value);
	}

	public static bool TryParseAsIso(ReadOnlySpan<byte> source, out DateOnly value)
	{
		if (TryParseDateTimeOffset(source, out var parseData) && parseData.IsCalendarDateOnly && TryCreateDateTime(parseData, DateTimeKind.Unspecified, out var value2))
		{
			value = DateOnly.FromDateTime(value2);
			return true;
		}
		value = default(DateOnly);
		return false;
	}

	private static bool TryParseDateTimeOffset(ReadOnlySpan<byte> source, out DateTimeParseData parseData)
	{
		parseData = default(DateTimeParseData);
		uint num = (uint)(source[0] - 48);
		uint num2 = (uint)(source[1] - 48);
		uint num3 = (uint)(source[2] - 48);
		uint num4 = (uint)(source[3] - 48);
		if (num > 9 || num2 > 9 || num3 > 9 || num4 > 9)
		{
			return false;
		}
		parseData.Year = (int)(num * 1000 + num2 * 100 + num3 * 10 + num4);
		if (source[4] != 45 || !TryGetNextTwoDigits(source.Slice(5, 2), ref parseData.Month) || source[7] != 45 || !TryGetNextTwoDigits(source.Slice(8, 2), ref parseData.Day))
		{
			return false;
		}
		if (source.Length == 10)
		{
			parseData.IsCalendarDateOnly = true;
			return true;
		}
		if (source.Length < 16)
		{
			return false;
		}
		if (source[10] != 84 || source[13] != 58 || !TryGetNextTwoDigits(source.Slice(11, 2), ref parseData.Hour) || !TryGetNextTwoDigits(source.Slice(14, 2), ref parseData.Minute))
		{
			return false;
		}
		if (source.Length == 16)
		{
			return true;
		}
		byte b = source[16];
		int num5 = 17;
		switch (b)
		{
		case 90:
			parseData.OffsetToken = 90;
			return num5 == source.Length;
		case 43:
		case 45:
			parseData.OffsetToken = b;
			return ParseOffset(ref parseData, source.Slice(num5));
		default:
			return false;
		case 58:
			if (source.Length < 19 || !TryGetNextTwoDigits(source.Slice(17, 2), ref parseData.Second))
			{
				return false;
			}
			if (source.Length == 19)
			{
				return true;
			}
			b = source[19];
			num5 = 20;
			switch (b)
			{
			case 90:
				parseData.OffsetToken = 90;
				return num5 == source.Length;
			case 43:
			case 45:
				parseData.OffsetToken = b;
				return ParseOffset(ref parseData, source.Slice(num5));
			default:
				return false;
			case 46:
			{
				if (source.Length < 21)
				{
					return false;
				}
				int i = 0;
				for (int num6 = Math.Min(num5 + 16, source.Length); num5 < num6; num5++)
				{
					if (!IsDigit(b = source[num5]))
					{
						break;
					}
					if (i < 7)
					{
						parseData.Fraction = parseData.Fraction * 10 + (b - 48);
						i++;
					}
				}
				if (parseData.Fraction != 0)
				{
					for (; i < 7; i++)
					{
						parseData.Fraction *= 10;
					}
				}
				if (num5 == source.Length)
				{
					return true;
				}
				b = source[num5++];
				switch (b)
				{
				case 90:
					parseData.OffsetToken = 90;
					return num5 == source.Length;
				case 43:
				case 45:
					parseData.OffsetToken = b;
					return ParseOffset(ref parseData, source.Slice(num5));
				default:
					return false;
				}
			}
			}
		}
		static bool ParseOffset(ref DateTimeParseData parseData, ReadOnlySpan<byte> offsetData)
		{
			if (offsetData.Length < 2 || !TryGetNextTwoDigits(offsetData.Slice(0, 2), ref parseData.OffsetHours))
			{
				return false;
			}
			if (offsetData.Length == 2)
			{
				return true;
			}
			if (offsetData.Length != 5 || offsetData[2] != 58 || !TryGetNextTwoDigits(offsetData.Slice(3), ref parseData.OffsetMinutes))
			{
				return false;
			}
			return true;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryGetNextTwoDigits(ReadOnlySpan<byte> source, ref int value)
	{
		uint num = (uint)(source[0] - 48);
		uint num2 = (uint)(source[1] - 48);
		if (num > 9 || num2 > 9)
		{
			value = 0;
			return false;
		}
		value = (int)(num * 10 + num2);
		return true;
	}

	private static bool TryCreateDateTimeOffset(DateTime dateTime, ref DateTimeParseData parseData, out DateTimeOffset value)
	{
		if ((uint)parseData.OffsetHours > 14u)
		{
			value = default(DateTimeOffset);
			return false;
		}
		if ((uint)parseData.OffsetMinutes > 59u)
		{
			value = default(DateTimeOffset);
			return false;
		}
		if (parseData.OffsetHours == 14 && parseData.OffsetMinutes != 0)
		{
			value = default(DateTimeOffset);
			return false;
		}
		long num = ((long)parseData.OffsetHours * 3600L + (long)parseData.OffsetMinutes * 60L) * 10000000;
		if (parseData.OffsetNegative)
		{
			num = -num;
		}
		try
		{
			value = new DateTimeOffset(dateTime.Ticks, new TimeSpan(num));
		}
		catch (ArgumentOutOfRangeException)
		{
			value = default(DateTimeOffset);
			return false;
		}
		return true;
	}

	private static bool TryCreateDateTimeOffset(ref DateTimeParseData parseData, out DateTimeOffset value)
	{
		if (!TryCreateDateTime(parseData, DateTimeKind.Unspecified, out var value2))
		{
			value = default(DateTimeOffset);
			return false;
		}
		if (!TryCreateDateTimeOffset(value2, ref parseData, out value))
		{
			value = default(DateTimeOffset);
			return false;
		}
		return true;
	}

	private static bool TryCreateDateTimeOffsetInterpretingDataAsLocalTime(DateTimeParseData parseData, out DateTimeOffset value)
	{
		if (!TryCreateDateTime(parseData, DateTimeKind.Local, out var value2))
		{
			value = default(DateTimeOffset);
			return false;
		}
		try
		{
			value = new DateTimeOffset(value2);
		}
		catch (ArgumentOutOfRangeException)
		{
			value = default(DateTimeOffset);
			return false;
		}
		return true;
	}

	private static bool TryCreateDateTime(DateTimeParseData parseData, DateTimeKind kind, out DateTime value)
	{
		if (parseData.Year == 0)
		{
			value = default(DateTime);
			return false;
		}
		if ((uint)(parseData.Month - 1) >= 12u)
		{
			value = default(DateTime);
			return false;
		}
		uint num = (uint)(parseData.Day - 1);
		if (num >= 28 && num >= DateTime.DaysInMonth(parseData.Year, parseData.Month))
		{
			value = default(DateTime);
			return false;
		}
		if ((uint)parseData.Hour > 23u)
		{
			value = default(DateTime);
			return false;
		}
		if ((uint)parseData.Minute > 59u)
		{
			value = default(DateTime);
			return false;
		}
		if ((uint)parseData.Second > 59u)
		{
			value = default(DateTime);
			return false;
		}
		ReadOnlySpan<int> readOnlySpan = (DateTime.IsLeapYear(parseData.Year) ? DaysToMonth366 : DaysToMonth365);
		int num2 = parseData.Year - 1;
		int num3 = num2 * 365 + num2 / 4 - num2 / 100 + num2 / 400 + readOnlySpan[parseData.Month - 1] + parseData.Day - 1;
		long num4 = num3 * 864000000000L;
		int num5 = parseData.Hour * 3600 + parseData.Minute * 60 + parseData.Second;
		num4 += (long)num5 * 10000000L;
		num4 += parseData.Fraction;
		value = new DateTime(num4, kind);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte[] GetEscapedPropertyNameSection(ReadOnlySpan<byte> utf8Value, JavaScriptEncoder encoder)
	{
		int num = JsonWriterHelper.NeedsEscaping(utf8Value, encoder);
		if (num != -1)
		{
			return GetEscapedPropertyNameSection(utf8Value, num, encoder);
		}
		return GetPropertyNameSection(utf8Value);
	}

	public static byte[] EscapeValue(ReadOnlySpan<byte> utf8Value, int firstEscapeIndexVal, JavaScriptEncoder encoder)
	{
		byte[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndexVal);
		Span<byte> span = ((maxEscapedLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(maxEscapedLength))) : stackalloc byte[256]);
		Span<byte> destination = span;
		JsonWriterHelper.EscapeString(utf8Value, destination, firstEscapeIndexVal, encoder, out var written);
		byte[] result = destination.Slice(0, written).ToArray();
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
		return result;
	}

	private static byte[] GetEscapedPropertyNameSection(ReadOnlySpan<byte> utf8Value, int firstEscapeIndexVal, JavaScriptEncoder encoder)
	{
		byte[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndexVal);
		Span<byte> span = ((maxEscapedLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(maxEscapedLength))) : stackalloc byte[256]);
		Span<byte> destination = span;
		JsonWriterHelper.EscapeString(utf8Value, destination, firstEscapeIndexVal, encoder, out var written);
		byte[] propertyNameSection = GetPropertyNameSection(destination.Slice(0, written));
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
		return propertyNameSection;
	}

	private static byte[] GetPropertyNameSection(ReadOnlySpan<byte> utf8Value)
	{
		int length = utf8Value.Length;
		byte[] array = new byte[length + 3];
		array[0] = 34;
		utf8Value.CopyTo(array.AsSpan(1, length));
		array[++length] = 34;
		array[++length] = 58;
		return array;
	}
}
