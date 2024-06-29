using System.Collections.Generic;

namespace System.Diagnostics;

internal static class DiagnosticsHelper
{
	internal static bool CompareTags(List<KeyValuePair<string, object>> sortedTags, IEnumerable<KeyValuePair<string, object>> tags2)
	{
		if (sortedTags == tags2)
		{
			return true;
		}
		if (sortedTags == null || tags2 == null)
		{
			return false;
		}
		int count = sortedTags.Count;
		int num = count / 64 + 1;
		Span<ulong> bitMap = ((num > 100) ? ((Span<ulong>)new ulong[num]) : stackalloc ulong[num]);
		BitMapper bitMapper = new BitMapper(bitMap);
		if (tags2 is ICollection<KeyValuePair<string, object>> collection)
		{
			if (collection.Count != count)
			{
				return false;
			}
			if (collection is IList<KeyValuePair<string, object>> list)
			{
				for (int i = 0; i < count; i++)
				{
					KeyValuePair<string, object> keyValuePair = list[i];
					for (int j = 0; j < count; j++)
					{
						if (!bitMapper.IsSet(j))
						{
							KeyValuePair<string, object> keyValuePair2 = sortedTags[j];
							int num2 = string.CompareOrdinal(keyValuePair.Key, keyValuePair2.Key);
							if (num2 == 0 && object.Equals(keyValuePair.Value, keyValuePair2.Value))
							{
								bitMapper.SetBit(j);
								break;
							}
							if (num2 < 0 || j == count - 1)
							{
								return false;
							}
						}
					}
				}
				return true;
			}
		}
		int num3 = 0;
		using IEnumerator<KeyValuePair<string, object>> enumerator = tags2.GetEnumerator();
		while (enumerator.MoveNext())
		{
			num3++;
			if (num3 > sortedTags.Count)
			{
				return false;
			}
			KeyValuePair<string, object> current = enumerator.Current;
			for (int k = 0; k < count; k++)
			{
				if (!bitMapper.IsSet(k))
				{
					KeyValuePair<string, object> keyValuePair3 = sortedTags[k];
					int num4 = string.CompareOrdinal(current.Key, keyValuePair3.Key);
					if (num4 == 0 && object.Equals(current.Value, keyValuePair3.Value))
					{
						bitMapper.SetBit(k);
						break;
					}
					if (num4 < 0 || k == count - 1)
					{
						return false;
					}
				}
			}
		}
		return num3 == sortedTags.Count;
	}
}
