using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Net.Http.Headers;

public abstract class HttpHeaders : IEnumerable<KeyValuePair<string, IEnumerable<string>>>, IEnumerable
{
	internal sealed class InvalidValue
	{
		private readonly string _value;

		public InvalidValue(string value)
		{
			_value = value;
		}

		public override string ToString()
		{
			return _value;
		}
	}

	internal sealed class HeaderStoreItemInfo
	{
		internal object RawValue;

		internal object ParsedAndInvalidValues;

		public bool IsEmpty
		{
			get
			{
				if (RawValue == null)
				{
					return ParsedAndInvalidValues == null;
				}
				return false;
			}
		}

		internal HeaderStoreItemInfo()
		{
		}

		public bool CanAddParsedValue(HttpHeaderParser parser)
		{
			if (!parser.SupportsMultipleValues)
			{
				return ParsedAndInvalidValues == null;
			}
			return true;
		}

		public object GetSingleParsedValue()
		{
			if (ParsedAndInvalidValues != null)
			{
				if (ParsedAndInvalidValues is List<object> list)
				{
					foreach (object item in list)
					{
						if (!(item is InvalidValue))
						{
							return item;
						}
					}
				}
				else if (!(ParsedAndInvalidValues is InvalidValue))
				{
					return ParsedAndInvalidValues;
				}
			}
			return null;
		}
	}

	private object _headerStore;

	private int _count;

	private readonly HttpHeaderType _allowedHeaderTypes;

	private readonly HttpHeaderType _treatAsCustomHeaderTypes;

	public HttpHeadersNonValidated NonValidated => new HttpHeadersNonValidated(this);

	internal int Count => _count;

	private bool EntriesAreLiveView => _headerStore is HeaderEntry[];

	protected HttpHeaders()
		: this(HttpHeaderType.All, HttpHeaderType.None)
	{
	}

	internal HttpHeaders(HttpHeaderType allowedHeaderTypes, HttpHeaderType treatAsCustomHeaderTypes)
	{
		_allowedHeaderTypes = allowedHeaderTypes & ~HttpHeaderType.NonTrailing;
		_treatAsCustomHeaderTypes = treatAsCustomHeaderTypes & ~HttpHeaderType.NonTrailing;
	}

	public void Add(string name, string? value)
	{
		Add(GetHeaderDescriptor(name), value);
	}

	internal void Add(HeaderDescriptor descriptor, string value)
	{
		PrepareHeaderInfoForAdd(descriptor, out var info, out var addToStore);
		ParseAndAddValue(descriptor, info, value);
		if (addToStore && info.ParsedAndInvalidValues != null)
		{
			AddEntryToStore(new HeaderEntry(descriptor, info));
		}
	}

	public void Add(string name, IEnumerable<string?> values)
	{
		Add(GetHeaderDescriptor(name), values);
	}

	internal void Add(HeaderDescriptor descriptor, IEnumerable<string> values)
	{
		ArgumentNullException.ThrowIfNull(values, "values");
		PrepareHeaderInfoForAdd(descriptor, out var info, out var addToStore);
		try
		{
			foreach (string value in values)
			{
				ParseAndAddValue(descriptor, info, value);
			}
		}
		finally
		{
			if (addToStore && info.ParsedAndInvalidValues != null)
			{
				AddEntryToStore(new HeaderEntry(descriptor, info));
			}
		}
	}

	public bool TryAddWithoutValidation(string name, string? value)
	{
		if (TryGetHeaderDescriptor(name, out var descriptor))
		{
			return TryAddWithoutValidation(descriptor, value);
		}
		return false;
	}

	internal bool TryAddWithoutValidation(HeaderDescriptor descriptor, string value)
	{
		if (value == null)
		{
			value = string.Empty;
		}
		ref object valueRefOrAddDefault = ref GetValueRefOrAddDefault(descriptor);
		object obj = valueRefOrAddDefault;
		if (obj == null)
		{
			valueRefOrAddDefault = value;
		}
		else
		{
			HeaderStoreItemInfo headerStoreItemInfo = obj as HeaderStoreItemInfo;
			if (headerStoreItemInfo == null)
			{
				HeaderStoreItemInfo obj2 = new HeaderStoreItemInfo
				{
					RawValue = obj
				};
				headerStoreItemInfo = obj2;
				valueRefOrAddDefault = obj2;
			}
			AddRawValue(headerStoreItemInfo, value);
		}
		return true;
	}

	public bool TryAddWithoutValidation(string name, IEnumerable<string?> values)
	{
		if (TryGetHeaderDescriptor(name, out var descriptor))
		{
			return TryAddWithoutValidation(descriptor, values);
		}
		return false;
	}

	internal bool TryAddWithoutValidation(HeaderDescriptor descriptor, IEnumerable<string> values)
	{
		ArgumentNullException.ThrowIfNull(values, "values");
		using IEnumerator<string> enumerator = values.GetEnumerator();
		if (enumerator.MoveNext())
		{
			TryAddWithoutValidation(descriptor, enumerator.Current);
			if (enumerator.MoveNext())
			{
				ref object valueRefOrAddDefault = ref GetValueRefOrAddDefault(descriptor);
				object obj = valueRefOrAddDefault;
				HeaderStoreItemInfo headerStoreItemInfo = obj as HeaderStoreItemInfo;
				if (headerStoreItemInfo == null)
				{
					HeaderStoreItemInfo obj2 = new HeaderStoreItemInfo
					{
						RawValue = obj
					};
					headerStoreItemInfo = obj2;
					valueRefOrAddDefault = obj2;
				}
				do
				{
					AddRawValue(headerStoreItemInfo, enumerator.Current ?? string.Empty);
				}
				while (enumerator.MoveNext());
			}
		}
		return true;
	}

	public IEnumerable<string> GetValues(string name)
	{
		return GetValues(GetHeaderDescriptor(name));
	}

	internal IEnumerable<string> GetValues(HeaderDescriptor descriptor)
	{
		if (TryGetValues(descriptor, out var values))
		{
			return values;
		}
		throw new InvalidOperationException(System.SR.net_http_headers_not_found);
	}

	public bool TryGetValues(string name, [NotNullWhen(true)] out IEnumerable<string>? values)
	{
		if (TryGetHeaderDescriptor(name, out var descriptor))
		{
			return TryGetValues(descriptor, out values);
		}
		values = null;
		return false;
	}

	internal bool TryGetValues(HeaderDescriptor descriptor, [NotNullWhen(true)] out IEnumerable<string> values)
	{
		if (TryGetAndParseHeaderInfo(descriptor, out var info))
		{
			values = GetStoreValuesAsStringArray(descriptor, info);
			return true;
		}
		values = null;
		return false;
	}

	public bool Contains(string name)
	{
		return Contains(GetHeaderDescriptor(name));
	}

	public override string ToString()
	{
		Span<char> initialBuffer = stackalloc char[512];
		System.Text.ValueStringBuilder valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		ReadOnlySpan<HeaderEntry> entries = GetEntries();
		for (int i = 0; i < entries.Length; i++)
		{
			HeaderEntry headerEntry = entries[i];
			valueStringBuilder.Append(headerEntry.Key.Name);
			valueStringBuilder.Append(": ");
			GetStoreValuesAsStringOrStringArray(headerEntry.Key, headerEntry.Value, out var singleValue, out var multiValue);
			if (singleValue != null)
			{
				valueStringBuilder.Append(singleValue);
			}
			else
			{
				HttpHeaderParser parser = headerEntry.Key.Parser;
				string s = ((parser != null && parser.SupportsMultipleValues) ? parser.Separator : ", ");
				valueStringBuilder.Append(multiValue[0]);
				for (int j = 1; j < multiValue.Length; j++)
				{
					valueStringBuilder.Append(s);
					valueStringBuilder.Append(multiValue[j]);
				}
			}
			valueStringBuilder.Append(Environment.NewLine);
		}
		return valueStringBuilder.ToString();
	}

	internal string GetHeaderString(HeaderDescriptor descriptor)
	{
		if (TryGetHeaderValue(descriptor, out var value))
		{
			GetStoreValuesAsStringOrStringArray(descriptor, value, out var singleValue, out var multiValue);
			if (singleValue != null)
			{
				return singleValue;
			}
			string separator = ((descriptor.Parser != null && descriptor.Parser.SupportsMultipleValues) ? descriptor.Parser.Separator : ", ");
			return string.Join(separator, multiValue);
		}
		return string.Empty;
	}

	public IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumerator()
	{
		if (_count != 0)
		{
			return GetEnumeratorCore();
		}
		return ((IEnumerable<KeyValuePair<string, IEnumerable<string>>>)Array.Empty<KeyValuePair<string, IEnumerable<string>>>()).GetEnumerator();
	}

	private IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumeratorCore()
	{
		HeaderEntry[] entries = GetEntriesArray();
		for (int i = 0; i < _count; i++)
		{
			HeaderEntry headerEntry = entries[i];
			HeaderStoreItemInfo headerStoreItemInfo = headerEntry.Value as HeaderStoreItemInfo;
			if (headerStoreItemInfo == null)
			{
				headerStoreItemInfo = new HeaderStoreItemInfo
				{
					RawValue = headerEntry.Value
				};
				if (EntriesAreLiveView)
				{
					entries[i].Value = headerStoreItemInfo;
				}
				else
				{
					((Dictionary<HeaderDescriptor, object>)_headerStore)[headerEntry.Key] = headerStoreItemInfo;
				}
			}
			ParseRawHeaderValues(headerEntry.Key, headerStoreItemInfo);
			string[] storeValuesAsStringArray = GetStoreValuesAsStringArray(headerEntry.Key, headerStoreItemInfo);
			yield return new KeyValuePair<string, IEnumerable<string>>(headerEntry.Key.Name, storeValuesAsStringArray);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	internal void AddParsedValue(HeaderDescriptor descriptor, object value)
	{
		HeaderStoreItemInfo orCreateHeaderInfo = GetOrCreateHeaderInfo(descriptor);
		AddParsedValue(orCreateHeaderInfo, value);
	}

	internal void SetParsedValue(HeaderDescriptor descriptor, object value)
	{
		HeaderStoreItemInfo orCreateHeaderInfo = GetOrCreateHeaderInfo(descriptor);
		orCreateHeaderInfo.ParsedAndInvalidValues = null;
		orCreateHeaderInfo.RawValue = null;
		AddParsedValue(orCreateHeaderInfo, value);
	}

	internal void SetOrRemoveParsedValue(HeaderDescriptor descriptor, object value)
	{
		if (value == null)
		{
			Remove(descriptor);
		}
		else
		{
			SetParsedValue(descriptor, value);
		}
	}

	public bool Remove(string name)
	{
		return Remove(GetHeaderDescriptor(name));
	}

	internal bool RemoveParsedValue(HeaderDescriptor descriptor, object value)
	{
		if (TryGetAndParseHeaderInfo(descriptor, out var info))
		{
			object parsedAndInvalidValues = info.ParsedAndInvalidValues;
			if (parsedAndInvalidValues == null)
			{
				return false;
			}
			bool result = false;
			IEqualityComparer comparer = descriptor.Parser.Comparer;
			if (!(parsedAndInvalidValues is List<object> list))
			{
				if (!(parsedAndInvalidValues is InvalidValue) && AreEqual(value, parsedAndInvalidValues, comparer))
				{
					info.ParsedAndInvalidValues = null;
					result = true;
				}
			}
			else
			{
				foreach (object item in list)
				{
					if (!(item is InvalidValue) && AreEqual(value, item, comparer))
					{
						result = list.Remove(item);
						break;
					}
				}
				if (list.Count == 0)
				{
					info.ParsedAndInvalidValues = null;
				}
			}
			if (info.IsEmpty)
			{
				bool flag = Remove(descriptor);
			}
			return result;
		}
		return false;
	}

	internal bool ContainsParsedValue(HeaderDescriptor descriptor, object value)
	{
		if (TryGetAndParseHeaderInfo(descriptor, out var info))
		{
			object parsedAndInvalidValues = info.ParsedAndInvalidValues;
			if (parsedAndInvalidValues == null)
			{
				return false;
			}
			List<object> list = parsedAndInvalidValues as List<object>;
			IEqualityComparer comparer = descriptor.Parser.Comparer;
			if (list != null)
			{
				foreach (object item in list)
				{
					if (!(item is InvalidValue) && AreEqual(value, item, comparer))
					{
						return true;
					}
				}
				return false;
			}
			if (!(parsedAndInvalidValues is InvalidValue))
			{
				return AreEqual(value, parsedAndInvalidValues, comparer);
			}
		}
		return false;
	}

	internal virtual void AddHeaders(HttpHeaders sourceHeaders)
	{
		if (_count == 0 && sourceHeaders._headerStore is HeaderEntry[] array)
		{
			_count = sourceHeaders._count;
			HeaderEntry[] array2 = _headerStore as HeaderEntry[];
			if (array2 == null || array2.Length < _count)
			{
				array2 = (HeaderEntry[])(_headerStore = new HeaderEntry[array.Length]);
			}
			for (int i = 0; i < _count && i < array.Length; i++)
			{
				HeaderEntry headerEntry = array[i];
				if (headerEntry.Value is HeaderStoreItemInfo sourceInfo)
				{
					headerEntry.Value = CloneHeaderInfo(headerEntry.Key, sourceInfo);
				}
				array2[i] = headerEntry;
			}
			return;
		}
		ReadOnlySpan<HeaderEntry> entries = sourceHeaders.GetEntries();
		for (int j = 0; j < entries.Length; j++)
		{
			HeaderEntry headerEntry2 = entries[j];
			ref object valueRefOrAddDefault = ref GetValueRefOrAddDefault(headerEntry2.Key);
			if (valueRefOrAddDefault == null)
			{
				object value = headerEntry2.Value;
				if (value is HeaderStoreItemInfo sourceInfo2)
				{
					valueRefOrAddDefault = CloneHeaderInfo(headerEntry2.Key, sourceInfo2);
				}
				else
				{
					valueRefOrAddDefault = value;
				}
			}
		}
	}

	private static HeaderStoreItemInfo CloneHeaderInfo(HeaderDescriptor descriptor, HeaderStoreItemInfo sourceInfo)
	{
		lock (sourceInfo)
		{
			HeaderStoreItemInfo headerStoreItemInfo = new HeaderStoreItemInfo
			{
				RawValue = CloneStringHeaderInfoValues(sourceInfo.RawValue)
			};
			if (descriptor.Parser == null)
			{
				headerStoreItemInfo.ParsedAndInvalidValues = CloneStringHeaderInfoValues(sourceInfo.ParsedAndInvalidValues);
			}
			else if (sourceInfo.ParsedAndInvalidValues != null)
			{
				if (!(sourceInfo.ParsedAndInvalidValues is List<object> list))
				{
					CloneAndAddValue(headerStoreItemInfo, sourceInfo.ParsedAndInvalidValues);
				}
				else
				{
					foreach (object item in list)
					{
						CloneAndAddValue(headerStoreItemInfo, item);
					}
				}
			}
			return headerStoreItemInfo;
		}
	}

	private static void CloneAndAddValue(HeaderStoreItemInfo destinationInfo, object source)
	{
		if (source is ICloneable cloneable)
		{
			AddParsedValue(destinationInfo, cloneable.Clone());
		}
		else
		{
			AddParsedValue(destinationInfo, source);
		}
	}

	[return: NotNullIfNotNull("source")]
	private static object CloneStringHeaderInfoValues(object source)
	{
		if (source == null)
		{
			return null;
		}
		if (!(source is List<object> collection))
		{
			return source;
		}
		return new List<object>(collection);
	}

	private HeaderStoreItemInfo GetOrCreateHeaderInfo(HeaderDescriptor descriptor)
	{
		if (TryGetAndParseHeaderInfo(descriptor, out var info))
		{
			return info;
		}
		return CreateAndAddHeaderToStore(descriptor);
	}

	private HeaderStoreItemInfo CreateAndAddHeaderToStore(HeaderDescriptor descriptor)
	{
		HeaderStoreItemInfo headerStoreItemInfo = new HeaderStoreItemInfo();
		AddEntryToStore(new HeaderEntry(descriptor, headerStoreItemInfo));
		return headerStoreItemInfo;
	}

	internal bool TryGetHeaderValue(HeaderDescriptor descriptor, [NotNullWhen(true)] out object value)
	{
		ref object valueRefOrNullRef = ref GetValueRefOrNullRef(descriptor);
		if (Unsafe.IsNullRef(ref valueRefOrNullRef))
		{
			value = null;
			return false;
		}
		value = valueRefOrNullRef;
		return true;
	}

	private bool TryGetAndParseHeaderInfo(HeaderDescriptor key, [NotNullWhen(true)] out HeaderStoreItemInfo info)
	{
		ref object valueRefOrNullRef = ref GetValueRefOrNullRef(key);
		if (!Unsafe.IsNullRef(ref valueRefOrNullRef))
		{
			object obj = valueRefOrNullRef;
			if (obj is HeaderStoreItemInfo headerStoreItemInfo)
			{
				info = headerStoreItemInfo;
			}
			else
			{
				HeaderStoreItemInfo obj2 = new HeaderStoreItemInfo
				{
					RawValue = obj
				};
				HeaderStoreItemInfo headerStoreItemInfo2 = obj2;
				info = obj2;
				valueRefOrNullRef = headerStoreItemInfo2;
			}
			ParseRawHeaderValues(key, info);
			return true;
		}
		info = null;
		return false;
	}

	private static void ParseRawHeaderValues(HeaderDescriptor descriptor, HeaderStoreItemInfo info)
	{
		lock (info)
		{
			if (info.RawValue == null)
			{
				return;
			}
			if (info.RawValue is List<string> list)
			{
				foreach (string item in list)
				{
					ParseSingleRawHeaderValue(info, descriptor, item);
				}
			}
			else
			{
				string rawValue = info.RawValue as string;
				ParseSingleRawHeaderValue(info, descriptor, rawValue);
			}
			info.RawValue = null;
		}
	}

	private static void ParseSingleRawHeaderValue(HeaderStoreItemInfo info, HeaderDescriptor descriptor, string rawValue)
	{
		if (descriptor.Parser == null)
		{
			if (HttpRuleParser.ContainsNewLine(rawValue))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(null, System.SR.Format(System.SR.net_http_log_headers_no_newlines, descriptor.Name, rawValue), "ParseSingleRawHeaderValue");
				}
				AddInvalidValue(info, rawValue);
			}
			else
			{
				AddParsedValue(info, rawValue);
			}
		}
		else if (!TryParseAndAddRawHeaderValue(descriptor, info, rawValue, addWhenInvalid: true) && System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Log.HeadersInvalidValue(descriptor.Name, rawValue);
		}
	}

	internal bool TryParseAndAddValue(HeaderDescriptor descriptor, string value)
	{
		PrepareHeaderInfoForAdd(descriptor, out var info, out var addToStore);
		bool flag = TryParseAndAddRawHeaderValue(descriptor, info, value, addWhenInvalid: false);
		if (flag && addToStore && info.ParsedAndInvalidValues != null)
		{
			AddEntryToStore(new HeaderEntry(descriptor, info));
		}
		return flag;
	}

	private static bool TryParseAndAddRawHeaderValue(HeaderDescriptor descriptor, HeaderStoreItemInfo info, string value, bool addWhenInvalid)
	{
		if (!info.CanAddParsedValue(descriptor.Parser))
		{
			if (addWhenInvalid)
			{
				AddInvalidValue(info, value ?? string.Empty);
			}
			return false;
		}
		int index = 0;
		if (descriptor.Parser.TryParseValue(value, info.ParsedAndInvalidValues, ref index, out var parsedValue))
		{
			if (value == null || index == value.Length)
			{
				if (parsedValue != null)
				{
					AddParsedValue(info, parsedValue);
				}
				else if (addWhenInvalid && info.ParsedAndInvalidValues == null)
				{
					AddInvalidValue(info, value ?? string.Empty);
				}
				return true;
			}
			List<object> list = new List<object>();
			if (parsedValue != null)
			{
				list.Add(parsedValue);
			}
			while (index < value.Length)
			{
				if (descriptor.Parser.TryParseValue(value, info.ParsedAndInvalidValues, ref index, out parsedValue))
				{
					if (parsedValue != null)
					{
						list.Add(parsedValue);
					}
					continue;
				}
				if (addWhenInvalid)
				{
					AddInvalidValue(info, value);
				}
				return false;
			}
			foreach (object item in list)
			{
				AddParsedValue(info, item);
			}
			if (list.Count == 0 && addWhenInvalid && info.ParsedAndInvalidValues == null)
			{
				AddInvalidValue(info, value);
			}
			return true;
		}
		if (addWhenInvalid)
		{
			AddInvalidValue(info, value ?? string.Empty);
		}
		return false;
	}

	private static void AddParsedValue(HeaderStoreItemInfo info, object value)
	{
		AddValueToStoreValue(value, ref info.ParsedAndInvalidValues);
	}

	private static void AddInvalidValue(HeaderStoreItemInfo info, string value)
	{
		AddValueToStoreValue((object)new InvalidValue(value), ref info.ParsedAndInvalidValues);
	}

	private static void AddRawValue(HeaderStoreItemInfo info, string value)
	{
		AddValueToStoreValue(value, ref info.RawValue);
	}

	private static void AddValueToStoreValue<T>(T value, ref object currentStoreValue) where T : class
	{
		if (currentStoreValue == null)
		{
			currentStoreValue = value;
			return;
		}
		List<T> list = currentStoreValue as List<T>;
		if (list == null)
		{
			list = new List<T>(2);
			list.Add((T)currentStoreValue);
			currentStoreValue = list;
		}
		list.Add(value);
	}

	internal object GetSingleParsedValue(HeaderDescriptor descriptor)
	{
		if (!TryGetAndParseHeaderInfo(descriptor, out var info))
		{
			return null;
		}
		return info.GetSingleParsedValue();
	}

	internal object GetParsedAndInvalidValues(HeaderDescriptor descriptor)
	{
		if (!TryGetAndParseHeaderInfo(descriptor, out var info))
		{
			return null;
		}
		return info.ParsedAndInvalidValues;
	}

	internal virtual bool IsAllowedHeaderName(HeaderDescriptor descriptor)
	{
		return true;
	}

	private void PrepareHeaderInfoForAdd(HeaderDescriptor descriptor, out HeaderStoreItemInfo info, out bool addToStore)
	{
		if (!IsAllowedHeaderName(descriptor))
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_http_headers_not_allowed_header_name, descriptor.Name));
		}
		addToStore = false;
		if (!TryGetAndParseHeaderInfo(descriptor, out info))
		{
			info = new HeaderStoreItemInfo();
			addToStore = true;
		}
	}

	private static void ParseAndAddValue(HeaderDescriptor descriptor, HeaderStoreItemInfo info, string value)
	{
		if (descriptor.Parser == null)
		{
			CheckContainsNewLine(value);
			AddParsedValue(info, value ?? string.Empty);
			return;
		}
		if (!info.CanAddParsedValue(descriptor.Parser))
		{
			throw new FormatException(System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_headers_single_value_header, descriptor.Name));
		}
		int index = 0;
		object obj = descriptor.Parser.ParseValue(value, info.ParsedAndInvalidValues, ref index);
		if (value == null || index == value.Length)
		{
			if (obj != null)
			{
				AddParsedValue(info, obj);
			}
			return;
		}
		List<object> list = new List<object>();
		if (obj != null)
		{
			list.Add(obj);
		}
		while (index < value.Length)
		{
			obj = descriptor.Parser.ParseValue(value, info.ParsedAndInvalidValues, ref index);
			if (obj != null)
			{
				list.Add(obj);
			}
		}
		foreach (object item in list)
		{
			AddParsedValue(info, item);
		}
	}

	private HeaderDescriptor GetHeaderDescriptor(string name)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name, "name");
		if (!HeaderDescriptor.TryGet(name, out var descriptor))
		{
			throw new FormatException(System.SR.Format(System.SR.net_http_headers_invalid_header_name, name));
		}
		if ((descriptor.HeaderType & _allowedHeaderTypes) != 0)
		{
			return descriptor;
		}
		if ((descriptor.HeaderType & _treatAsCustomHeaderTypes) != 0)
		{
			return descriptor.AsCustomHeader();
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.net_http_headers_not_allowed_header_name, name));
	}

	internal bool TryGetHeaderDescriptor(string name, out HeaderDescriptor descriptor)
	{
		if (string.IsNullOrEmpty(name))
		{
			descriptor = default(HeaderDescriptor);
			return false;
		}
		if (HeaderDescriptor.TryGet(name, out descriptor))
		{
			HttpHeaderType headerType = descriptor.HeaderType;
			if ((headerType & _allowedHeaderTypes) != 0)
			{
				return true;
			}
			if ((headerType & _treatAsCustomHeaderTypes) != 0)
			{
				descriptor = descriptor.AsCustomHeader();
				return true;
			}
		}
		return false;
	}

	internal static void CheckContainsNewLine(string value)
	{
		if (value == null || !HttpRuleParser.ContainsNewLine(value))
		{
			return;
		}
		throw new FormatException(System.SR.net_http_headers_no_newlines);
	}

	internal static string[] GetStoreValuesAsStringArray(HeaderDescriptor descriptor, HeaderStoreItemInfo info)
	{
		GetStoreValuesAsStringOrStringArray(descriptor, info, out var singleValue, out var multiValue);
		return multiValue ?? new string[1] { singleValue };
	}

	internal static void GetStoreValuesAsStringOrStringArray(HeaderDescriptor descriptor, object sourceValues, out string singleValue, out string[] multiValue)
	{
		if (!(sourceValues is HeaderStoreItemInfo headerStoreItemInfo))
		{
			singleValue = (string)sourceValues;
			multiValue = null;
			return;
		}
		lock (headerStoreItemInfo)
		{
			int valueCount = GetValueCount(headerStoreItemInfo);
			singleValue = null;
			Span<string> values;
			if (valueCount == 1)
			{
				multiValue = null;
				values = new Span<string>(ref singleValue);
			}
			else
			{
				values = (multiValue = new string[valueCount]);
			}
			int currentIndex = 0;
			ReadStoreValues<object>(values, headerStoreItemInfo.ParsedAndInvalidValues, descriptor.Parser, ref currentIndex);
			ReadStoreValues<string>(values, headerStoreItemInfo.RawValue, null, ref currentIndex);
		}
	}

	internal static int GetStoreValuesIntoStringArray(HeaderDescriptor descriptor, object sourceValues, [NotNull] ref string[] values)
	{
		if (values == null)
		{
			values = Array.Empty<string>();
		}
		if (!(sourceValues is HeaderStoreItemInfo headerStoreItemInfo))
		{
			if (values.Length == 0)
			{
				values = new string[1];
			}
			values[0] = (string)sourceValues;
			return 1;
		}
		lock (headerStoreItemInfo)
		{
			int valueCount = GetValueCount(headerStoreItemInfo);
			if (values.Length < valueCount)
			{
				values = new string[valueCount];
			}
			int currentIndex = 0;
			ReadStoreValues<object>(values, headerStoreItemInfo.ParsedAndInvalidValues, descriptor.Parser, ref currentIndex);
			ReadStoreValues<string>(values, headerStoreItemInfo.RawValue, null, ref currentIndex);
			return valueCount;
		}
	}

	private static int GetValueCount(HeaderStoreItemInfo info)
	{
		return Count<object>(info.ParsedAndInvalidValues) + Count<string>(info.RawValue);
		static int Count<T>(object valueStore)
		{
			if (valueStore != null)
			{
				if (!(valueStore is List<T> list))
				{
					return 1;
				}
				return list.Count;
			}
			return 0;
		}
	}

	private static void ReadStoreValues<T>(Span<string> values, object storeValue, HttpHeaderParser parser, ref int currentIndex)
	{
		if (storeValue == null)
		{
			return;
		}
		if (!(storeValue is List<T> list))
		{
			values[currentIndex] = ((parser == null || storeValue is InvalidValue) ? storeValue.ToString() : parser.ToString(storeValue));
			currentIndex++;
			return;
		}
		foreach (T item in list)
		{
			object obj = item;
			values[currentIndex] = ((parser == null || obj is InvalidValue) ? obj.ToString() : parser.ToString(obj));
			currentIndex++;
		}
	}

	private static bool AreEqual(object value, object storeValue, IEqualityComparer comparer)
	{
		return comparer?.Equals(value, storeValue) ?? value.Equals(storeValue);
	}

	internal HeaderEntry[] GetEntriesArray()
	{
		object headerStore = _headerStore;
		if (headerStore == null)
		{
			return null;
		}
		if (headerStore is HeaderEntry[] result)
		{
			return result;
		}
		return GetEntriesFromDictionary();
		HeaderEntry[] GetEntriesFromDictionary()
		{
			Dictionary<HeaderDescriptor, object> dictionary = (Dictionary<HeaderDescriptor, object>)_headerStore;
			HeaderEntry[] array = new HeaderEntry[dictionary.Count];
			int num = 0;
			foreach (KeyValuePair<HeaderDescriptor, object> item in dictionary)
			{
				array[num++] = new HeaderEntry
				{
					Key = item.Key,
					Value = item.Value
				};
			}
			return array;
		}
	}

	internal ReadOnlySpan<HeaderEntry> GetEntries()
	{
		return new ReadOnlySpan<HeaderEntry>(GetEntriesArray(), 0, _count);
	}

	private ref object GetValueRefOrNullRef(HeaderDescriptor key)
	{
		ref object result = ref Unsafe.NullRef<object>();
		object headerStore = _headerStore;
		if (headerStore is HeaderEntry[] array)
		{
			for (int i = 0; i < _count && i < array.Length; i++)
			{
				if (key.Equals(array[i].Key))
				{
					result = ref array[i].Value;
					break;
				}
			}
		}
		else if (headerStore != null)
		{
			result = ref CollectionsMarshal.GetValueRefOrNullRef(Unsafe.As<Dictionary<HeaderDescriptor, object>>(headerStore), key);
		}
		return ref result;
	}

	private ref object GetValueRefOrAddDefault(HeaderDescriptor key)
	{
		object headerStore = _headerStore;
		if (headerStore is HeaderEntry[] array)
		{
			for (int i = 0; i < _count && i < array.Length; i++)
			{
				if (key.Equals(array[i].Key))
				{
					return ref array[i].Value;
				}
			}
			int count = _count;
			_count++;
			if ((uint)count < (uint)array.Length)
			{
				array[count].Key = key;
				return ref array[count].Value;
			}
			return ref GrowEntriesAndAddDefault(key);
		}
		if (headerStore == null)
		{
			_count++;
			ref HeaderEntry arrayDataReference = ref MemoryMarshal.GetArrayDataReference((HeaderEntry[])(_headerStore = new HeaderEntry[4]));
			arrayDataReference.Key = key;
			return ref arrayDataReference.Value;
		}
		return ref DictionaryGetValueRefOrAddDefault(key);
		ref object ConvertToDictionaryAndAddDefault(HeaderDescriptor key)
		{
			HeaderEntry[] array2 = (HeaderEntry[])_headerStore;
			Dictionary<HeaderDescriptor, object> dictionary2 = (Dictionary<HeaderDescriptor, object>)(_headerStore = new Dictionary<HeaderDescriptor, object>(64));
			HeaderEntry[] array3 = array2;
			for (int j = 0; j < array3.Length; j++)
			{
				HeaderEntry headerEntry = array3[j];
				dictionary2.Add(headerEntry.Key, headerEntry.Value);
			}
			bool exists2;
			return ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary2, key, out exists2);
		}
		ref object DictionaryGetValueRefOrAddDefault(HeaderDescriptor key)
		{
			Dictionary<HeaderDescriptor, object> dictionary = (Dictionary<HeaderDescriptor, object>)_headerStore;
			bool exists;
			ref object valueRefOrAddDefault = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out exists);
			if (valueRefOrAddDefault == null)
			{
				_count++;
			}
			return ref valueRefOrAddDefault;
		}
		ref object GrowEntriesAndAddDefault(HeaderDescriptor key)
		{
			HeaderEntry[] array4 = (HeaderEntry[])_headerStore;
			if (array4.Length == 64)
			{
				return ref ConvertToDictionaryAndAddDefault(key);
			}
			Array.Resize(ref array4, array4.Length << 1);
			_headerStore = array4;
			ref HeaderEntry reference = ref array4[array4.Length >> 1];
			reference.Key = key;
			return ref reference.Value;
		}
	}

	private void AddEntryToStore(HeaderEntry entry)
	{
		if (_headerStore is HeaderEntry[] array)
		{
			int count = _count;
			if ((uint)count < (uint)array.Length)
			{
				array[count] = entry;
				_count++;
				return;
			}
		}
		GetValueRefOrAddDefault(entry.Key) = entry.Value;
	}

	internal bool Contains(HeaderDescriptor key)
	{
		return !Unsafe.IsNullRef(ref GetValueRefOrNullRef(key));
	}

	public void Clear()
	{
		if (_headerStore is HeaderEntry[] array)
		{
			Array.Clear(array, 0, _count);
		}
		else
		{
			_headerStore = null;
		}
		_count = 0;
	}

	internal bool Remove(HeaderDescriptor key)
	{
		bool flag = false;
		object headerStore = _headerStore;
		if (headerStore is HeaderEntry[] array)
		{
			for (int i = 0; i < _count && i < array.Length; i++)
			{
				if (key.Equals(array[i].Key))
				{
					for (; i + 1 < _count && (uint)(i + 1) < (uint)array.Length; i++)
					{
						array[i] = array[i + 1];
					}
					array[i] = default(HeaderEntry);
					flag = true;
					break;
				}
			}
		}
		else if (headerStore != null)
		{
			flag = Unsafe.As<Dictionary<HeaderDescriptor, object>>(headerStore).Remove(key);
		}
		if (flag)
		{
			_count--;
		}
		return flag;
	}
}
