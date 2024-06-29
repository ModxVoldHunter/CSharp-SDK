using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace System.Net.Http.Headers;

public class CacheControlHeaderValue : ICloneable
{
	[Flags]
	private enum Flags
	{
		None = 0,
		MaxAgeHasValue = 1,
		SharedMaxAgeHasValue = 2,
		MaxStaleLimitHasValue = 4,
		MinFreshHasValue = 8,
		NoCache = 0x10,
		NoStore = 0x20,
		MaxStale = 0x40,
		NoTransform = 0x80,
		OnlyIfCached = 0x100,
		Public = 0x200,
		Private = 0x400,
		MustRevalidate = 0x800,
		ProxyRevalidate = 0x1000
	}

	private sealed class TokenObjectCollection : ObjectCollection<string>
	{
		public override void Validate(string item)
		{
			HeaderUtilities.CheckValidToken(item, "item");
		}

		public int GetHashCode(StringComparer comparer)
		{
			int num = 0;
			using Enumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				string current = enumerator.Current;
				num ^= comparer.GetHashCode(current);
			}
			return num;
		}
	}

	private static readonly GenericHeaderParser s_nameValueListParser = GenericHeaderParser.MultipleValueNameValueParser;

	private Flags _flags;

	private TokenObjectCollection _noCacheHeaders;

	private TimeSpan _maxAge;

	private TimeSpan _sharedMaxAge;

	private TimeSpan _maxStaleLimit;

	private TimeSpan _minFresh;

	private TokenObjectCollection _privateHeaders;

	private UnvalidatedObjectCollection<NameValueHeaderValue> _extensions;

	public bool NoCache
	{
		get
		{
			return (_flags & Flags.NoCache) != 0;
		}
		set
		{
			SetFlag(Flags.NoCache, value);
		}
	}

	public ICollection<string> NoCacheHeaders => _noCacheHeaders ?? (_noCacheHeaders = new TokenObjectCollection());

	public bool NoStore
	{
		get
		{
			return (_flags & Flags.NoStore) != 0;
		}
		set
		{
			SetFlag(Flags.NoStore, value);
		}
	}

	public TimeSpan? MaxAge
	{
		get
		{
			if ((_flags & Flags.MaxAgeHasValue) != 0)
			{
				return _maxAge;
			}
			return null;
		}
		set
		{
			SetTimeSpan(ref _maxAge, Flags.MaxAgeHasValue, value);
		}
	}

	public TimeSpan? SharedMaxAge
	{
		get
		{
			if ((_flags & Flags.SharedMaxAgeHasValue) != 0)
			{
				return _sharedMaxAge;
			}
			return null;
		}
		set
		{
			SetTimeSpan(ref _sharedMaxAge, Flags.SharedMaxAgeHasValue, value);
		}
	}

	public bool MaxStale
	{
		get
		{
			return (_flags & Flags.MaxStale) != 0;
		}
		set
		{
			SetFlag(Flags.MaxStale, value);
		}
	}

	public TimeSpan? MaxStaleLimit
	{
		get
		{
			if ((_flags & Flags.MaxStaleLimitHasValue) != 0)
			{
				return _maxStaleLimit;
			}
			return null;
		}
		set
		{
			SetTimeSpan(ref _maxStaleLimit, Flags.MaxStaleLimitHasValue, value);
		}
	}

	public TimeSpan? MinFresh
	{
		get
		{
			if ((_flags & Flags.MinFreshHasValue) != 0)
			{
				return _minFresh;
			}
			return null;
		}
		set
		{
			SetTimeSpan(ref _minFresh, Flags.MinFreshHasValue, value);
		}
	}

	public bool NoTransform
	{
		get
		{
			return (_flags & Flags.NoTransform) != 0;
		}
		set
		{
			SetFlag(Flags.NoTransform, value);
		}
	}

	public bool OnlyIfCached
	{
		get
		{
			return (_flags & Flags.OnlyIfCached) != 0;
		}
		set
		{
			SetFlag(Flags.OnlyIfCached, value);
		}
	}

	public bool Public
	{
		get
		{
			return (_flags & Flags.Public) != 0;
		}
		set
		{
			SetFlag(Flags.Public, value);
		}
	}

	public bool Private
	{
		get
		{
			return (_flags & Flags.Private) != 0;
		}
		set
		{
			SetFlag(Flags.Private, value);
		}
	}

	public ICollection<string> PrivateHeaders => _privateHeaders ?? (_privateHeaders = new TokenObjectCollection());

	public bool MustRevalidate
	{
		get
		{
			return (_flags & Flags.MustRevalidate) != 0;
		}
		set
		{
			SetFlag(Flags.MustRevalidate, value);
		}
	}

	public bool ProxyRevalidate
	{
		get
		{
			return (_flags & Flags.ProxyRevalidate) != 0;
		}
		set
		{
			SetFlag(Flags.ProxyRevalidate, value);
		}
	}

	public ICollection<NameValueHeaderValue> Extensions => _extensions ?? (_extensions = new UnvalidatedObjectCollection<NameValueHeaderValue>());

	private void SetTimeSpan(ref TimeSpan fieldRef, Flags flag, TimeSpan? value)
	{
		fieldRef = value.GetValueOrDefault();
		SetFlag(flag, value.HasValue);
	}

	private void SetFlag(Flags flag, bool value)
	{
		if (value)
		{
			Interlocked.Or(ref Unsafe.As<Flags, int>(ref _flags), (int)flag);
		}
		else
		{
			Interlocked.And(ref Unsafe.As<Flags, int>(ref _flags), (int)(~flag));
		}
	}

	public CacheControlHeaderValue()
	{
	}

	private CacheControlHeaderValue(CacheControlHeaderValue source)
	{
		_flags = source._flags;
		_maxAge = source._maxAge;
		_sharedMaxAge = source._sharedMaxAge;
		_maxStaleLimit = source._maxStaleLimit;
		_minFresh = source._minFresh;
		if (source._noCacheHeaders != null)
		{
			foreach (string noCacheHeader in source._noCacheHeaders)
			{
				NoCacheHeaders.Add(noCacheHeader);
			}
		}
		if (source._privateHeaders != null)
		{
			foreach (string privateHeader in source._privateHeaders)
			{
				PrivateHeaders.Add(privateHeader);
			}
		}
		_extensions = source._extensions.Clone();
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = System.Text.StringBuilderCache.Acquire();
		AppendValueIfRequired(stringBuilder, NoStore, "no-store");
		AppendValueIfRequired(stringBuilder, NoTransform, "no-transform");
		AppendValueIfRequired(stringBuilder, OnlyIfCached, "only-if-cached");
		AppendValueIfRequired(stringBuilder, Public, "public");
		AppendValueIfRequired(stringBuilder, MustRevalidate, "must-revalidate");
		AppendValueIfRequired(stringBuilder, ProxyRevalidate, "proxy-revalidate");
		if (NoCache)
		{
			AppendValueWithSeparatorIfRequired(stringBuilder, "no-cache");
			if (_noCacheHeaders != null && _noCacheHeaders.Count > 0)
			{
				stringBuilder.Append("=\"");
				AppendValues(stringBuilder, _noCacheHeaders);
				stringBuilder.Append('"');
			}
		}
		if ((_flags & Flags.MaxAgeHasValue) != 0)
		{
			AppendValueWithSeparatorIfRequired(stringBuilder, "max-age");
			stringBuilder.Append('=');
			int num = (int)_maxAge.TotalSeconds;
			if (num >= 0)
			{
				stringBuilder.Append(num);
			}
			else
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				IFormatProvider invariantInfo = NumberFormatInfo.InvariantInfo;
				IFormatProvider provider = invariantInfo;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2, invariantInfo);
				handler.AppendFormatted(num);
				stringBuilder3.Append(provider, ref handler);
			}
		}
		if ((_flags & Flags.SharedMaxAgeHasValue) != 0)
		{
			AppendValueWithSeparatorIfRequired(stringBuilder, "s-maxage");
			stringBuilder.Append('=');
			int num2 = (int)_sharedMaxAge.TotalSeconds;
			if (num2 >= 0)
			{
				stringBuilder.Append(num2);
			}
			else
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				IFormatProvider invariantInfo = NumberFormatInfo.InvariantInfo;
				IFormatProvider provider2 = invariantInfo;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2, invariantInfo);
				handler.AppendFormatted(num2);
				stringBuilder4.Append(provider2, ref handler);
			}
		}
		if (MaxStale)
		{
			AppendValueWithSeparatorIfRequired(stringBuilder, "max-stale");
			if ((_flags & Flags.MaxStaleLimitHasValue) != 0)
			{
				stringBuilder.Append('=');
				int num3 = (int)_maxStaleLimit.TotalSeconds;
				if (num3 >= 0)
				{
					stringBuilder.Append(num3);
				}
				else
				{
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder5 = stringBuilder2;
					IFormatProvider invariantInfo = NumberFormatInfo.InvariantInfo;
					IFormatProvider provider3 = invariantInfo;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2, invariantInfo);
					handler.AppendFormatted(num3);
					stringBuilder5.Append(provider3, ref handler);
				}
			}
		}
		if ((_flags & Flags.MinFreshHasValue) != 0)
		{
			AppendValueWithSeparatorIfRequired(stringBuilder, "min-fresh");
			stringBuilder.Append('=');
			int num4 = (int)_minFresh.TotalSeconds;
			if (num4 >= 0)
			{
				stringBuilder.Append(num4);
			}
			else
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder6 = stringBuilder2;
				IFormatProvider invariantInfo = NumberFormatInfo.InvariantInfo;
				IFormatProvider provider4 = invariantInfo;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2, invariantInfo);
				handler.AppendFormatted(num4);
				stringBuilder6.Append(provider4, ref handler);
			}
		}
		if (Private)
		{
			AppendValueWithSeparatorIfRequired(stringBuilder, "private");
			if (_privateHeaders != null && _privateHeaders.Count > 0)
			{
				stringBuilder.Append("=\"");
				AppendValues(stringBuilder, _privateHeaders);
				stringBuilder.Append('"');
			}
		}
		NameValueHeaderValue.ToString(_extensions, ',', leadingSeparator: false, stringBuilder);
		return System.Text.StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is CacheControlHeaderValue cacheControlHeaderValue && _flags == cacheControlHeaderValue._flags && _maxAge == cacheControlHeaderValue._maxAge && _sharedMaxAge == cacheControlHeaderValue._sharedMaxAge && _maxStaleLimit == cacheControlHeaderValue._maxStaleLimit && _minFresh == cacheControlHeaderValue._minFresh && HeaderUtilities.AreEqualCollections(_noCacheHeaders, cacheControlHeaderValue._noCacheHeaders, StringComparer.OrdinalIgnoreCase) && HeaderUtilities.AreEqualCollections(_privateHeaders, cacheControlHeaderValue._privateHeaders, StringComparer.OrdinalIgnoreCase))
		{
			return HeaderUtilities.AreEqualCollections(_extensions, cacheControlHeaderValue._extensions);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(_flags, _maxAge, _sharedMaxAge, _maxStaleLimit, _minFresh, (_noCacheHeaders != null) ? _noCacheHeaders.GetHashCode(StringComparer.OrdinalIgnoreCase) : 0, (_privateHeaders != null) ? _privateHeaders.GetHashCode(StringComparer.OrdinalIgnoreCase) : 0, NameValueHeaderValue.GetHashCode(_extensions));
	}

	public static CacheControlHeaderValue Parse(string? input)
	{
		int index = 0;
		return ((CacheControlHeaderValue)CacheControlHeaderParser.Parser.ParseValue(input, null, ref index)) ?? new CacheControlHeaderValue();
	}

	public static bool TryParse(string? input, [NotNullWhen(true)] out CacheControlHeaderValue? parsedValue)
	{
		int index = 0;
		parsedValue = null;
		if (CacheControlHeaderParser.Parser.TryParseValue(input, null, ref index, out var parsedValue2))
		{
			parsedValue = ((CacheControlHeaderValue)parsedValue2) ?? new CacheControlHeaderValue();
			return true;
		}
		return false;
	}

	internal static int GetCacheControlLength(string input, int startIndex, CacheControlHeaderValue storeValue, out CacheControlHeaderValue parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
		{
			return 0;
		}
		int index = startIndex;
		List<NameValueHeaderValue> list = new List<NameValueHeaderValue>();
		while (index < input.Length)
		{
			if (!s_nameValueListParser.TryParseValue(input, null, ref index, out var parsedValue2))
			{
				return 0;
			}
			list.Add((NameValueHeaderValue)parsedValue2);
		}
		CacheControlHeaderValue cacheControlHeaderValue = storeValue ?? new CacheControlHeaderValue();
		if (!TrySetCacheControlValues(cacheControlHeaderValue, list))
		{
			return 0;
		}
		if (storeValue == null)
		{
			parsedValue = cacheControlHeaderValue;
		}
		return input.Length - startIndex;
	}

	private static bool TrySetCacheControlValues(CacheControlHeaderValue cc, List<NameValueHeaderValue> nameValueList)
	{
		foreach (NameValueHeaderValue nameValue in nameValueList)
		{
			string text = nameValue.Name.ToLowerInvariant();
			string value = nameValue.Value;
			Flags flags = Flags.None;
			bool flag = value == null;
			switch (text)
			{
			case "no-cache":
				flags = Flags.NoCache;
				flag = TrySetOptionalTokenList(nameValue, ref cc._noCacheHeaders);
				break;
			case "no-store":
				flags = Flags.NoStore;
				break;
			case "max-age":
				flags = Flags.MaxAgeHasValue;
				flag = TrySetTimeSpan(value, ref cc._maxAge);
				break;
			case "max-stale":
				flags = Flags.MaxStale;
				if (TrySetTimeSpan(value, ref cc._maxStaleLimit))
				{
					flag = true;
					flags = Flags.MaxStaleLimitHasValue | Flags.MaxStale;
				}
				break;
			case "min-fresh":
				flags = Flags.MinFreshHasValue;
				flag = TrySetTimeSpan(value, ref cc._minFresh);
				break;
			case "no-transform":
				flags = Flags.NoTransform;
				break;
			case "only-if-cached":
				flags = Flags.OnlyIfCached;
				break;
			case "public":
				flags = Flags.Public;
				break;
			case "private":
				flags = Flags.Private;
				flag = TrySetOptionalTokenList(nameValue, ref cc._privateHeaders);
				break;
			case "must-revalidate":
				flags = Flags.MustRevalidate;
				break;
			case "proxy-revalidate":
				flags = Flags.ProxyRevalidate;
				break;
			case "s-maxage":
				flags = Flags.SharedMaxAgeHasValue;
				flag = TrySetTimeSpan(value, ref cc._sharedMaxAge);
				break;
			default:
				flag = true;
				cc.Extensions.Add(nameValue);
				break;
			}
			if (flag)
			{
				cc._flags |= flags;
				continue;
			}
			return false;
		}
		return true;
	}

	private static bool TrySetOptionalTokenList(NameValueHeaderValue nameValue, ref TokenObjectCollection destination)
	{
		if (nameValue.Value == null)
		{
			return true;
		}
		string value = nameValue.Value;
		if (value.Length < 3 || !value.StartsWith('"') || !value.EndsWith('"'))
		{
			return false;
		}
		int num = 1;
		int num2 = value.Length - 1;
		int num3 = ((destination != null) ? destination.Count : 0);
		while (num < num2)
		{
			num = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(value, num, skipEmptyValues: true, out var _);
			if (num == num2)
			{
				break;
			}
			int tokenLength = HttpRuleParser.GetTokenLength(value, num);
			if (tokenLength == 0)
			{
				return false;
			}
			if (destination == null)
			{
				destination = new TokenObjectCollection();
			}
			destination.Add(value.Substring(num, tokenLength));
			num += tokenLength;
		}
		if (destination != null && destination.Count > num3)
		{
			return true;
		}
		return false;
	}

	private static bool TrySetTimeSpan(string value, ref TimeSpan timeSpan)
	{
		if (value == null || !HeaderUtilities.TryParseInt32(value, out var result))
		{
			return false;
		}
		timeSpan = new TimeSpan(0, 0, result);
		return true;
	}

	private static void AppendValueIfRequired(StringBuilder sb, bool appendValue, string value)
	{
		if (appendValue)
		{
			AppendValueWithSeparatorIfRequired(sb, value);
		}
	}

	private static void AppendValueWithSeparatorIfRequired(StringBuilder sb, string value)
	{
		if (sb.Length > 0)
		{
			sb.Append(", ");
		}
		sb.Append(value);
	}

	private static void AppendValues(StringBuilder sb, TokenObjectCollection values)
	{
		bool flag = true;
		foreach (string value in values)
		{
			if (flag)
			{
				flag = false;
			}
			else
			{
				sb.Append(", ");
			}
			sb.Append(value);
		}
	}

	object ICloneable.Clone()
	{
		return new CacheControlHeaderValue(this);
	}
}
