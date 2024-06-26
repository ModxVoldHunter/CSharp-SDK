using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization;

public sealed class NumberFormatInfo : IFormatProvider, ICloneable
{
	private static volatile NumberFormatInfo s_invariantInfo;

	internal static readonly string[] s_asciiDigits = new string[10] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

	internal int[] _numberGroupSizes = new int[1] { 3 };

	internal int[] _currencyGroupSizes = new int[1] { 3 };

	internal int[] _percentGroupSizes = new int[1] { 3 };

	internal string _positiveSign = "+";

	internal string _negativeSign = "-";

	internal string _numberDecimalSeparator = ".";

	internal string _numberGroupSeparator = ",";

	internal string _currencyGroupSeparator = ",";

	internal string _currencyDecimalSeparator = ".";

	internal string _currencySymbol = "¤";

	internal string _nanSymbol = "NaN";

	internal string _positiveInfinitySymbol = "Infinity";

	internal string _negativeInfinitySymbol = "-Infinity";

	internal string _percentDecimalSeparator = ".";

	internal string _percentGroupSeparator = ",";

	internal string _percentSymbol = "%";

	internal string _perMilleSymbol = "‰";

	internal byte[] _positiveSignUtf8;

	internal byte[] _negativeSignUtf8;

	internal byte[] _currencySymbolUtf8;

	internal byte[] _numberDecimalSeparatorUtf8;

	internal byte[] _currencyDecimalSeparatorUtf8;

	internal byte[] _currencyGroupSeparatorUtf8;

	internal byte[] _numberGroupSeparatorUtf8;

	internal byte[] _percentSymbolUtf8;

	internal byte[] _percentDecimalSeparatorUtf8;

	internal byte[] _percentGroupSeparatorUtf8;

	internal byte[] _perMilleSymbolUtf8;

	internal byte[] _nanSymbolUtf8;

	internal byte[] _positiveInfinitySymbolUtf8;

	internal byte[] _negativeInfinitySymbolUtf8;

	internal string[] _nativeDigits = new string[10] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

	internal int _numberDecimalDigits = 2;

	internal int _currencyDecimalDigits = 2;

	internal int _currencyPositivePattern;

	internal int _currencyNegativePattern;

	internal int _numberNegativePattern = 1;

	internal int _percentPositivePattern;

	internal int _percentNegativePattern;

	internal int _percentDecimalDigits = 2;

	internal int _digitSubstitution = 1;

	internal bool _isReadOnly;

	private bool _hasInvariantNumberSigns = true;

	private bool _allowHyphenDuringParsing;

	internal bool HasInvariantNumberSigns => _hasInvariantNumberSigns;

	internal bool AllowHyphenDuringParsing => _allowHyphenDuringParsing;

	public static NumberFormatInfo InvariantInfo => s_invariantInfo ?? (s_invariantInfo = CultureInfo.InvariantCulture.NumberFormat);

	public int CurrencyDecimalDigits
	{
		get
		{
			return _currencyDecimalDigits;
		}
		set
		{
			if (value < 0 || value > 99)
			{
				ThrowHelper.ThrowArgumentOutOfRange_Range("value", value, 0, 99);
			}
			VerifyWritable();
			_currencyDecimalDigits = value;
		}
	}

	public string CurrencyDecimalSeparator
	{
		get
		{
			return _currencyDecimalSeparator;
		}
		set
		{
			VerifyWritable();
			ArgumentException.ThrowIfNullOrEmpty(value, "value");
			_currencyDecimalSeparator = value;
			_currencyDecimalSeparatorUtf8 = null;
		}
	}

	public bool IsReadOnly => _isReadOnly;

	public int[] CurrencyGroupSizes
	{
		get
		{
			return (int[])_currencyGroupSizes.Clone();
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			VerifyWritable();
			int[] array = (int[])value.Clone();
			CheckGroupSize("value", array);
			_currencyGroupSizes = array;
		}
	}

	public int[] NumberGroupSizes
	{
		get
		{
			return (int[])_numberGroupSizes.Clone();
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			VerifyWritable();
			int[] array = (int[])value.Clone();
			CheckGroupSize("value", array);
			_numberGroupSizes = array;
		}
	}

	public int[] PercentGroupSizes
	{
		get
		{
			return (int[])_percentGroupSizes.Clone();
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			VerifyWritable();
			int[] array = (int[])value.Clone();
			CheckGroupSize("value", array);
			_percentGroupSizes = array;
		}
	}

	public string CurrencyGroupSeparator
	{
		get
		{
			return _currencyGroupSeparator;
		}
		set
		{
			VerifyWritable();
			ArgumentNullException.ThrowIfNull(value, "value");
			_currencyGroupSeparator = value;
			_currencyGroupSeparatorUtf8 = null;
		}
	}

	public string CurrencySymbol
	{
		get
		{
			return _currencySymbol;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			VerifyWritable();
			_currencySymbol = value;
			_currencySymbolUtf8 = null;
		}
	}

	public static NumberFormatInfo CurrentInfo
	{
		get
		{
			CultureInfo currentCulture = CultureInfo.CurrentCulture;
			if (!currentCulture._isInherited)
			{
				NumberFormatInfo numInfo = currentCulture._numInfo;
				if (numInfo != null)
				{
					return numInfo;
				}
			}
			return (NumberFormatInfo)currentCulture.GetFormat(typeof(NumberFormatInfo));
		}
	}

	public string NaNSymbol
	{
		get
		{
			return _nanSymbol;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			VerifyWritable();
			_nanSymbol = value;
			_nanSymbolUtf8 = null;
		}
	}

	public int CurrencyNegativePattern
	{
		get
		{
			return _currencyNegativePattern;
		}
		set
		{
			if (value < 0 || value > 16)
			{
				ThrowHelper.ThrowArgumentOutOfRange_Range("value", value, 0, 16);
			}
			VerifyWritable();
			_currencyNegativePattern = value;
		}
	}

	public int NumberNegativePattern
	{
		get
		{
			return _numberNegativePattern;
		}
		set
		{
			if (value < 0 || value > 4)
			{
				ThrowHelper.ThrowArgumentOutOfRange_Range("value", value, 0, 4);
			}
			VerifyWritable();
			_numberNegativePattern = value;
		}
	}

	public int PercentPositivePattern
	{
		get
		{
			return _percentPositivePattern;
		}
		set
		{
			if (value < 0 || value > 3)
			{
				ThrowHelper.ThrowArgumentOutOfRange_Range("value", value, 0, 3);
			}
			VerifyWritable();
			_percentPositivePattern = value;
		}
	}

	public int PercentNegativePattern
	{
		get
		{
			return _percentNegativePattern;
		}
		set
		{
			if (value < 0 || value > 11)
			{
				ThrowHelper.ThrowArgumentOutOfRange_Range("value", value, 0, 11);
			}
			VerifyWritable();
			_percentNegativePattern = value;
		}
	}

	public string NegativeInfinitySymbol
	{
		get
		{
			return _negativeInfinitySymbol;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			VerifyWritable();
			_negativeInfinitySymbol = value;
			_negativeInfinitySymbolUtf8 = null;
		}
	}

	public string NegativeSign
	{
		get
		{
			return _negativeSign;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			VerifyWritable();
			_negativeSign = value;
			_negativeSignUtf8 = null;
			InitializeInvariantAndNegativeSignFlags();
		}
	}

	public int NumberDecimalDigits
	{
		get
		{
			return _numberDecimalDigits;
		}
		set
		{
			if (value < 0 || value > 99)
			{
				ThrowHelper.ThrowArgumentOutOfRange_Range("value", value, 0, 99);
			}
			VerifyWritable();
			_numberDecimalDigits = value;
		}
	}

	public string NumberDecimalSeparator
	{
		get
		{
			return _numberDecimalSeparator;
		}
		set
		{
			VerifyWritable();
			ArgumentException.ThrowIfNullOrEmpty(value, "value");
			_numberDecimalSeparator = value;
			_numberDecimalSeparatorUtf8 = null;
		}
	}

	public string NumberGroupSeparator
	{
		get
		{
			return _numberGroupSeparator;
		}
		set
		{
			VerifyWritable();
			ArgumentNullException.ThrowIfNull(value, "value");
			_numberGroupSeparator = value;
			_numberGroupSeparatorUtf8 = null;
		}
	}

	public int CurrencyPositivePattern
	{
		get
		{
			return _currencyPositivePattern;
		}
		set
		{
			if (value < 0 || value > 3)
			{
				ThrowHelper.ThrowArgumentOutOfRange_Range("value", value, 0, 3);
			}
			VerifyWritable();
			_currencyPositivePattern = value;
		}
	}

	public string PositiveInfinitySymbol
	{
		get
		{
			return _positiveInfinitySymbol;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			VerifyWritable();
			_positiveInfinitySymbol = value;
			_positiveInfinitySymbolUtf8 = null;
		}
	}

	public string PositiveSign
	{
		get
		{
			return _positiveSign;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			VerifyWritable();
			_positiveSign = value;
			_positiveSignUtf8 = null;
			InitializeInvariantAndNegativeSignFlags();
		}
	}

	public int PercentDecimalDigits
	{
		get
		{
			return _percentDecimalDigits;
		}
		set
		{
			if (value < 0 || value > 99)
			{
				ThrowHelper.ThrowArgumentOutOfRange_Range("value", value, 0, 99);
			}
			VerifyWritable();
			_percentDecimalDigits = value;
		}
	}

	public string PercentDecimalSeparator
	{
		get
		{
			return _percentDecimalSeparator;
		}
		set
		{
			VerifyWritable();
			ArgumentException.ThrowIfNullOrEmpty(value, "value");
			_percentDecimalSeparator = value;
			_percentDecimalSeparatorUtf8 = null;
		}
	}

	public string PercentGroupSeparator
	{
		get
		{
			return _percentGroupSeparator;
		}
		set
		{
			VerifyWritable();
			ArgumentNullException.ThrowIfNull(value, "value");
			_percentGroupSeparator = value;
			_percentGroupSeparatorUtf8 = null;
		}
	}

	public string PercentSymbol
	{
		get
		{
			return _percentSymbol;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			VerifyWritable();
			_percentSymbol = value;
			_percentSymbolUtf8 = null;
		}
	}

	public string PerMilleSymbol
	{
		get
		{
			return _perMilleSymbol;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			VerifyWritable();
			_perMilleSymbol = value;
			_perMilleSymbolUtf8 = null;
		}
	}

	public string[] NativeDigits
	{
		get
		{
			return (string[])_nativeDigits.Clone();
		}
		set
		{
			VerifyWritable();
			VerifyNativeDigits(value, "value");
			_nativeDigits = value;
		}
	}

	public DigitShapes DigitSubstitution
	{
		get
		{
			return (DigitShapes)_digitSubstitution;
		}
		set
		{
			VerifyWritable();
			VerifyDigitSubstitution(value, "value");
			_digitSubstitution = (int)value;
		}
	}

	public NumberFormatInfo()
	{
	}

	private static void VerifyNativeDigits(string[] nativeDig, string propertyName)
	{
		ArgumentNullException.ThrowIfNull(nativeDig, "nativeDig");
		if (nativeDig.Length != 10)
		{
			throw new ArgumentException(SR.Argument_InvalidNativeDigitCount, propertyName);
		}
		for (int i = 0; i < nativeDig.Length; i++)
		{
			if (nativeDig[i] == null)
			{
				throw new ArgumentNullException(propertyName, SR.ArgumentNull_ArrayValue);
			}
			if (nativeDig[i].Length != 1)
			{
				if (nativeDig[i].Length != 2)
				{
					throw new ArgumentException(SR.Argument_InvalidNativeDigitValue, propertyName);
				}
				if (!char.IsSurrogatePair(nativeDig[i][0], nativeDig[i][1]))
				{
					throw new ArgumentException(SR.Argument_InvalidNativeDigitValue, propertyName);
				}
			}
			if (CharUnicodeInfo.GetDecimalDigitValue(nativeDig[i], 0) != i && CharUnicodeInfo.GetUnicodeCategory(nativeDig[i], 0) != UnicodeCategory.PrivateUse)
			{
				throw new ArgumentException(SR.Argument_InvalidNativeDigitValue, propertyName);
			}
		}
	}

	private static void VerifyDigitSubstitution(DigitShapes digitSub, string propertyName)
	{
		if ((uint)digitSub > 2u)
		{
			throw new ArgumentException(SR.Argument_InvalidDigitSubstitution, propertyName);
		}
	}

	private void InitializeInvariantAndNegativeSignFlags()
	{
		_hasInvariantNumberSigns = _positiveSign == "+" && _negativeSign == "-";
		bool flag = _negativeSign.Length == 1;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3;
			switch (_negativeSign[0])
			{
			case '‒':
			case '⁻':
			case '₋':
			case '−':
			case '➖':
			case '﹣':
			case '－':
				flag3 = true;
				break;
			default:
				flag3 = false;
				break;
			}
			flag2 = flag3;
		}
		_allowHyphenDuringParsing = flag2;
	}

	internal NumberFormatInfo(CultureData cultureData)
	{
		if (cultureData != null)
		{
			cultureData.GetNFIValues(this);
			InitializeInvariantAndNegativeSignFlags();
		}
	}

	private void VerifyWritable()
	{
		if (_isReadOnly)
		{
			throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
		}
	}

	public static NumberFormatInfo GetInstance(IFormatProvider? formatProvider)
	{
		if (formatProvider != null)
		{
			return GetProviderNonNull(formatProvider);
		}
		return CurrentInfo;
		static NumberFormatInfo GetProviderNonNull(IFormatProvider provider)
		{
			if (provider is CultureInfo { _isInherited: false } cultureInfo)
			{
				return cultureInfo._numInfo ?? cultureInfo.NumberFormat;
			}
			return (provider as NumberFormatInfo) ?? (provider.GetFormat(typeof(NumberFormatInfo)) as NumberFormatInfo) ?? CurrentInfo;
		}
	}

	public object Clone()
	{
		NumberFormatInfo numberFormatInfo = (NumberFormatInfo)MemberwiseClone();
		numberFormatInfo._isReadOnly = false;
		return numberFormatInfo;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ReadOnlySpan<TChar> CurrencyDecimalSeparatorTChar<TChar>() where TChar : unmanaged, IUtfChar<TChar>
	{
		if (!(typeof(TChar) == typeof(char)))
		{
			return MemoryMarshal.Cast<byte, TChar>(_currencyDecimalSeparatorUtf8 ?? (_currencyDecimalSeparatorUtf8 = Encoding.UTF8.GetBytes(_currencyDecimalSeparator)));
		}
		return MemoryMarshal.Cast<char, TChar>(_currencyDecimalSeparator);
	}

	internal static void CheckGroupSize(string propName, int[] groupSize)
	{
		for (int i = 0; i < groupSize.Length; i++)
		{
			if (groupSize[i] < 1)
			{
				if (i == groupSize.Length - 1 && groupSize[i] == 0)
				{
					break;
				}
				throw new ArgumentException(SR.Argument_InvalidGroupSize, propName);
			}
			if (groupSize[i] > 9)
			{
				throw new ArgumentException(SR.Argument_InvalidGroupSize, propName);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ReadOnlySpan<TChar> CurrencyGroupSeparatorTChar<TChar>() where TChar : unmanaged, IUtfChar<TChar>
	{
		if (!(typeof(TChar) == typeof(char)))
		{
			return MemoryMarshal.Cast<byte, TChar>(_currencyGroupSeparatorUtf8 ?? (_currencyGroupSeparatorUtf8 = Encoding.UTF8.GetBytes(_currencyGroupSeparator)));
		}
		return MemoryMarshal.Cast<char, TChar>(_currencyGroupSeparator);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ReadOnlySpan<TChar> CurrencySymbolTChar<TChar>() where TChar : unmanaged, IUtfChar<TChar>
	{
		if (!(typeof(TChar) == typeof(char)))
		{
			return MemoryMarshal.Cast<byte, TChar>(_currencySymbolUtf8 ?? (_currencySymbolUtf8 = Encoding.UTF8.GetBytes(_currencySymbol)));
		}
		return MemoryMarshal.Cast<char, TChar>(_currencySymbol);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ReadOnlySpan<TChar> NaNSymbolTChar<TChar>() where TChar : unmanaged, IUtfChar<TChar>
	{
		if (!(typeof(TChar) == typeof(char)))
		{
			return MemoryMarshal.Cast<byte, TChar>(_nanSymbolUtf8 ?? (_nanSymbolUtf8 = Encoding.UTF8.GetBytes(_nanSymbol)));
		}
		return MemoryMarshal.Cast<char, TChar>(_nanSymbol);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ReadOnlySpan<TChar> NegativeInfinitySymbolTChar<TChar>() where TChar : unmanaged, IUtfChar<TChar>
	{
		if (!(typeof(TChar) == typeof(char)))
		{
			return MemoryMarshal.Cast<byte, TChar>(_negativeInfinitySymbolUtf8 ?? (_negativeInfinitySymbolUtf8 = Encoding.UTF8.GetBytes(_negativeInfinitySymbol)));
		}
		return MemoryMarshal.Cast<char, TChar>(_negativeInfinitySymbol);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ReadOnlySpan<TChar> NegativeSignTChar<TChar>() where TChar : unmanaged, IUtfChar<TChar>
	{
		if (!(typeof(TChar) == typeof(char)))
		{
			return MemoryMarshal.Cast<byte, TChar>(_negativeSignUtf8 ?? (_negativeSignUtf8 = Encoding.UTF8.GetBytes(_negativeSign)));
		}
		return MemoryMarshal.Cast<char, TChar>(_negativeSign);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ReadOnlySpan<TChar> NumberDecimalSeparatorTChar<TChar>() where TChar : unmanaged, IUtfChar<TChar>
	{
		if (!(typeof(TChar) == typeof(char)))
		{
			return MemoryMarshal.Cast<byte, TChar>(_numberDecimalSeparatorUtf8 ?? (_numberDecimalSeparatorUtf8 = Encoding.UTF8.GetBytes(_numberDecimalSeparator)));
		}
		return MemoryMarshal.Cast<char, TChar>(_numberDecimalSeparator);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ReadOnlySpan<TChar> NumberGroupSeparatorTChar<TChar>() where TChar : unmanaged, IUtfChar<TChar>
	{
		if (!(typeof(TChar) == typeof(char)))
		{
			return MemoryMarshal.Cast<byte, TChar>(_numberGroupSeparatorUtf8 ?? (_numberGroupSeparatorUtf8 = Encoding.UTF8.GetBytes(_numberGroupSeparator)));
		}
		return MemoryMarshal.Cast<char, TChar>(_numberGroupSeparator);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ReadOnlySpan<TChar> PositiveInfinitySymbolTChar<TChar>() where TChar : unmanaged, IUtfChar<TChar>
	{
		if (!(typeof(TChar) == typeof(char)))
		{
			return MemoryMarshal.Cast<byte, TChar>(_positiveInfinitySymbolUtf8 ?? (_positiveInfinitySymbolUtf8 = Encoding.UTF8.GetBytes(_positiveInfinitySymbol)));
		}
		return MemoryMarshal.Cast<char, TChar>(_positiveInfinitySymbol);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ReadOnlySpan<TChar> PositiveSignTChar<TChar>() where TChar : unmanaged, IUtfChar<TChar>
	{
		if (!(typeof(TChar) == typeof(char)))
		{
			return MemoryMarshal.Cast<byte, TChar>(_positiveSignUtf8 ?? (_positiveSignUtf8 = Encoding.UTF8.GetBytes(_positiveSign)));
		}
		return MemoryMarshal.Cast<char, TChar>(_positiveSign);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ReadOnlySpan<TChar> PercentDecimalSeparatorTChar<TChar>() where TChar : unmanaged, IUtfChar<TChar>
	{
		if (!(typeof(TChar) == typeof(char)))
		{
			return MemoryMarshal.Cast<byte, TChar>(_percentDecimalSeparatorUtf8 ?? (_percentDecimalSeparatorUtf8 = Encoding.UTF8.GetBytes(_percentDecimalSeparator)));
		}
		return MemoryMarshal.Cast<char, TChar>(_percentDecimalSeparator);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ReadOnlySpan<TChar> PercentGroupSeparatorTChar<TChar>() where TChar : unmanaged, IUtfChar<TChar>
	{
		if (!(typeof(TChar) == typeof(char)))
		{
			return MemoryMarshal.Cast<byte, TChar>(_percentGroupSeparatorUtf8 ?? (_percentGroupSeparatorUtf8 = Encoding.UTF8.GetBytes(_percentGroupSeparator)));
		}
		return MemoryMarshal.Cast<char, TChar>(_percentGroupSeparator);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ReadOnlySpan<TChar> PercentSymbolTChar<TChar>() where TChar : unmanaged, IUtfChar<TChar>
	{
		if (!(typeof(TChar) == typeof(char)))
		{
			return MemoryMarshal.Cast<byte, TChar>(_percentSymbolUtf8 ?? (_percentSymbolUtf8 = Encoding.UTF8.GetBytes(_percentSymbol)));
		}
		return MemoryMarshal.Cast<char, TChar>(_percentSymbol);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ReadOnlySpan<TChar> PerMilleSymbolTChar<TChar>() where TChar : unmanaged, IUtfChar<TChar>
	{
		if (!(typeof(TChar) == typeof(char)))
		{
			return MemoryMarshal.Cast<byte, TChar>(_perMilleSymbolUtf8 ?? (_perMilleSymbolUtf8 = Encoding.UTF8.GetBytes(_perMilleSymbol)));
		}
		return MemoryMarshal.Cast<char, TChar>(_perMilleSymbol);
	}

	public object? GetFormat(Type? formatType)
	{
		if (!(formatType == typeof(NumberFormatInfo)))
		{
			return null;
		}
		return this;
	}

	public static NumberFormatInfo ReadOnly(NumberFormatInfo nfi)
	{
		ArgumentNullException.ThrowIfNull(nfi, "nfi");
		if (nfi.IsReadOnly)
		{
			return nfi;
		}
		NumberFormatInfo numberFormatInfo = (NumberFormatInfo)nfi.MemberwiseClone();
		numberFormatInfo._isReadOnly = true;
		return numberFormatInfo;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ValidateParseStyleInteger(NumberStyles style)
	{
		if (((uint)style & 0xFFFFFE00u) != 0 && ((uint)style & 0xFFFFFDFCu) != 0 && ((uint)style & 0xFFFFFBFCu) != 0)
		{
			ThrowInvalid(style);
		}
		static void ThrowInvalid(NumberStyles value)
		{
			throw new ArgumentException((((uint)value & 0xFFFFF800u) != 0) ? SR.Argument_InvalidNumberStyles : SR.Arg_InvalidHexBinaryStyle, "style");
		}
	}

	internal static void ValidateParseStyleFloatingPoint(NumberStyles style)
	{
		if (((uint)style & 0xFFFFFE00u) != 0)
		{
			ThrowInvalid(style);
		}
		static void ThrowInvalid(NumberStyles value)
		{
			throw new ArgumentException((((uint)value & 0xFFFFF800u) != 0) ? SR.Argument_InvalidNumberStyles : SR.Arg_HexBinaryStylesNotSupported, "style");
		}
	}
}
