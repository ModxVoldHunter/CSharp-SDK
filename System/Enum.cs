using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public abstract class Enum : ValueType, IComparable, ISpanFormattable, IFormattable, IConvertible
{
	internal sealed class EnumInfo<TStorage> where TStorage : struct, INumber<TStorage>
	{
		public readonly bool HasFlagsAttribute;

		public readonly bool ValuesAreSequentialFromZero;

		public readonly TStorage[] Values;

		public readonly string[] Names;

		public EnumInfo(bool hasFlagsAttribute, TStorage[] values, string[] names)
		{
			HasFlagsAttribute = hasFlagsAttribute;
			Values = values;
			Names = names;
			if (!AreSorted(values))
			{
				Array.Sort(values, names);
			}
			ValuesAreSequentialFromZero = AreSequentialFromZero(values);
		}

		public TResult[] CloneValues<TResult>() where TResult : struct
		{
			return MemoryMarshal.Cast<TStorage, TResult>(Values).ToArray();
		}
	}

	private static readonly RuntimeType[] s_underlyingTypes = new RuntimeType[26]
	{
		null,
		null,
		(RuntimeType)typeof(bool),
		(RuntimeType)typeof(char),
		(RuntimeType)typeof(sbyte),
		(RuntimeType)typeof(byte),
		(RuntimeType)typeof(short),
		(RuntimeType)typeof(ushort),
		(RuntimeType)typeof(int),
		(RuntimeType)typeof(uint),
		(RuntimeType)typeof(long),
		(RuntimeType)typeof(ulong),
		(RuntimeType)typeof(float),
		(RuntimeType)typeof(double),
		null,
		null,
		null,
		null,
		null,
		null,
		null,
		null,
		null,
		null,
		(RuntimeType)typeof(nint),
		(RuntimeType)typeof(nuint)
	};

	private const char EnumSeparatorChar = ',';

	[DllImport("QCall", EntryPoint = "Enum_GetValuesAndNames", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "Enum_GetValuesAndNames")]
	private static extern void GetEnumValuesAndNames(QCallTypeHandle enumType, ObjectHandleOnStack values, ObjectHandleOnStack names, Interop.BOOL getNames);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern object InternalBoxEnum(RuntimeType enumType, long value);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern CorElementType InternalGetCorElementType(MethodTable* pMT);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static CorElementType InternalGetCorElementType(RuntimeType rt)
	{
		CorElementType result = InternalGetCorElementType((MethodTable*)rt.GetUnderlyingNativeHandle());
		GC.KeepAlive(rt);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe CorElementType InternalGetCorElementType()
	{
		CorElementType result = InternalGetCorElementType(RuntimeHelpers.GetMethodTable(this));
		GC.KeepAlive(this);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static RuntimeType InternalGetUnderlyingType(RuntimeType enumType)
	{
		RuntimeType result = s_underlyingTypes[(uint)InternalGetCorElementType((MethodTable*)enumType.GetUnderlyingNativeHandle())];
		GC.KeepAlive(enumType);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static EnumInfo<TStorage> GetEnumInfo<TStorage>(RuntimeType enumType, bool getNames = true) where TStorage : struct, INumber<TStorage>
	{
		if (!(enumType.GenericCache is EnumInfo<TStorage> enumInfo) || (getNames && enumInfo.Names == null))
		{
			return InitializeEnumInfo(enumType, getNames);
		}
		return enumInfo;
		[MethodImpl(MethodImplOptions.NoInlining)]
		static EnumInfo<TStorage> InitializeEnumInfo(RuntimeType enumType, bool getNames)
		{
			TStorage[] o = null;
			string[] o2 = null;
			GetEnumValuesAndNames(new QCallTypeHandle(ref enumType), ObjectHandleOnStack.Create(ref o), ObjectHandleOnStack.Create(ref o2), getNames ? Interop.BOOL.TRUE : Interop.BOOL.FALSE);
			bool hasFlagsAttribute = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);
			return (EnumInfo<TStorage>)(enumType.GenericCache = new EnumInfo<TStorage>(hasFlagsAttribute, o, o2));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static string? GetName<TEnum>(TEnum value) where TEnum : struct, Enum
	{
		RuntimeType enumType = (RuntimeType)typeof(TEnum);
		Type enumUnderlyingType = typeof(TEnum).GetEnumUnderlyingType();
		if (enumUnderlyingType == typeof(sbyte) || enumUnderlyingType == typeof(byte))
		{
			return GetNameInlined(GetEnumInfo<byte>(enumType), *(byte*)(&value));
		}
		if (enumUnderlyingType == typeof(short) || enumUnderlyingType == typeof(ushort))
		{
			return GetNameInlined(GetEnumInfo<ushort>(enumType), *(ushort*)(&value));
		}
		if (enumUnderlyingType == typeof(int) || enumUnderlyingType == typeof(uint))
		{
			return GetNameInlined(GetEnumInfo<uint>(enumType), *(uint*)(&value));
		}
		if (enumUnderlyingType == typeof(long) || enumUnderlyingType == typeof(ulong))
		{
			return GetNameInlined(GetEnumInfo<ulong>(enumType), *(ulong*)(&value));
		}
		if (enumUnderlyingType == typeof(nint) || enumUnderlyingType == typeof(nuint))
		{
			return GetNameInlined<nuint>(GetEnumInfo<nuint>(enumType, getNames: true), *(nuint*)(&value));
		}
		if (enumUnderlyingType == typeof(float))
		{
			return GetNameInlined(GetEnumInfo<float>(enumType), *(float*)(&value));
		}
		if (enumUnderlyingType == typeof(double))
		{
			return GetNameInlined(GetEnumInfo<double>(enumType), *(double*)(&value));
		}
		if (enumUnderlyingType == typeof(char))
		{
			return GetNameInlined(GetEnumInfo<char>(enumType), *(char*)(&value));
		}
		throw CreateUnknownEnumTypeException();
	}

	public static string? GetName(Type enumType, object value)
	{
		ArgumentNullException.ThrowIfNull(enumType, "enumType");
		return enumType.GetEnumName(value);
	}

	internal static string GetName(RuntimeType enumType, ulong uint64Value)
	{
		Type enumUnderlyingType = enumType.GetEnumUnderlyingType();
		switch (Type.GetTypeCode(enumUnderlyingType))
		{
		case TypeCode.SByte:
			if ((long)uint64Value < -128L || (long)uint64Value > 127L)
			{
				return null;
			}
			return GetName(GetEnumInfo<byte>(enumType), (byte)(sbyte)uint64Value);
		case TypeCode.Byte:
			if (uint64Value > 255)
			{
				return null;
			}
			return GetName(GetEnumInfo<byte>(enumType), (byte)uint64Value);
		case TypeCode.Int16:
			if ((long)uint64Value < -32768L || (long)uint64Value > 32767L)
			{
				return null;
			}
			return GetName(GetEnumInfo<ushort>(enumType), (ushort)(short)uint64Value);
		case TypeCode.UInt16:
			if (uint64Value > 65535)
			{
				return null;
			}
			return GetName(GetEnumInfo<ushort>(enumType), (ushort)uint64Value);
		case TypeCode.Int32:
			if ((long)uint64Value < -2147483648L || (long)uint64Value > 2147483647L)
			{
				return null;
			}
			return GetName(GetEnumInfo<uint>(enumType), (uint)uint64Value);
		case TypeCode.UInt32:
			if (uint64Value > uint.MaxValue)
			{
				return null;
			}
			return GetName(GetEnumInfo<uint>(enumType), (uint)uint64Value);
		case TypeCode.Int64:
			return GetName(GetEnumInfo<ulong>(enumType), uint64Value);
		case TypeCode.UInt64:
			return GetName(GetEnumInfo<ulong>(enumType), uint64Value);
		case TypeCode.Char:
			if (uint64Value > 65535)
			{
				return null;
			}
			return GetName(GetEnumInfo<char>(enumType), (char)uint64Value);
		default:
			if (enumUnderlyingType == typeof(nint))
			{
				if ((long)uint64Value < (long)IntPtr.MinValue || (long)uint64Value > (long)IntPtr.MaxValue)
				{
					return null;
				}
				return GetName<nuint>(GetEnumInfo<nuint>(enumType, getNames: true), (nuint)uint64Value);
			}
			if (enumUnderlyingType == typeof(nuint))
			{
				if (uint64Value > UIntPtr.MaxValue)
				{
					return null;
				}
				return GetName<nuint>(GetEnumInfo<nuint>(enumType, getNames: true), (nuint)uint64Value);
			}
			throw CreateUnknownEnumTypeException();
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string GetName<TStorage>(EnumInfo<TStorage> enumInfo, TStorage value) where TStorage : struct, INumber<TStorage>
	{
		return GetNameInlined(enumInfo, value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string GetNameInlined<TStorage>(EnumInfo<TStorage> enumInfo, TStorage value) where TStorage : struct, INumber<TStorage>
	{
		string[] names = enumInfo.Names;
		if (enumInfo.ValuesAreSequentialFromZero)
		{
			if (Unsafe.SizeOf<TStorage>() <= 4)
			{
				uint num = uint.CreateTruncating(value);
				if (num < (uint)names.Length)
				{
					return names[num];
				}
			}
			else if (ulong.CreateTruncating(value) < (ulong)names.Length)
			{
				return names[uint.CreateTruncating(value)];
			}
		}
		else
		{
			int num2 = FindDefinedIndex(enumInfo.Values, value);
			if ((uint)num2 < (uint)names.Length)
			{
				return names[num2];
			}
		}
		return null;
	}

	public static string[] GetNames<TEnum>() where TEnum : struct, Enum
	{
		RuntimeType enumType = (RuntimeType)typeof(TEnum);
		Type enumUnderlyingType = typeof(TEnum).GetEnumUnderlyingType();
		string[] names;
		if (enumUnderlyingType == typeof(sbyte) || enumUnderlyingType == typeof(byte))
		{
			names = GetEnumInfo<byte>(enumType).Names;
		}
		else if (enumUnderlyingType == typeof(short) || enumUnderlyingType == typeof(ushort))
		{
			names = GetEnumInfo<ushort>(enumType).Names;
		}
		else if (enumUnderlyingType == typeof(int) || enumUnderlyingType == typeof(uint))
		{
			names = GetEnumInfo<uint>(enumType).Names;
		}
		else if (enumUnderlyingType == typeof(long) || enumUnderlyingType == typeof(ulong))
		{
			names = GetEnumInfo<ulong>(enumType).Names;
		}
		else if (enumUnderlyingType == typeof(nint) || enumUnderlyingType == typeof(nuint))
		{
			names = GetEnumInfo<nuint>(enumType, getNames: true).Names;
		}
		else if (enumUnderlyingType == typeof(float))
		{
			names = GetEnumInfo<float>(enumType).Names;
		}
		else if (enumUnderlyingType == typeof(double))
		{
			names = GetEnumInfo<double>(enumType).Names;
		}
		else
		{
			if (!(enumUnderlyingType == typeof(char)))
			{
				throw CreateUnknownEnumTypeException();
			}
			names = GetEnumInfo<char>(enumType).Names;
		}
		return new ReadOnlySpan<string>(names).ToArray();
	}

	public static string[] GetNames(Type enumType)
	{
		ArgumentNullException.ThrowIfNull(enumType, "enumType");
		return enumType.GetEnumNames();
	}

	internal static string[] GetNamesNoCopy(RuntimeType enumType)
	{
		switch (InternalGetCorElementType(enumType))
		{
		case CorElementType.ELEMENT_TYPE_I1:
		case CorElementType.ELEMENT_TYPE_U1:
			return GetEnumInfo<byte>(enumType).Names;
		case CorElementType.ELEMENT_TYPE_I2:
		case CorElementType.ELEMENT_TYPE_U2:
			return GetEnumInfo<ushort>(enumType).Names;
		case CorElementType.ELEMENT_TYPE_I4:
		case CorElementType.ELEMENT_TYPE_U4:
			return GetEnumInfo<uint>(enumType).Names;
		case CorElementType.ELEMENT_TYPE_I8:
		case CorElementType.ELEMENT_TYPE_U8:
			return GetEnumInfo<ulong>(enumType).Names;
		case CorElementType.ELEMENT_TYPE_I:
		case CorElementType.ELEMENT_TYPE_U:
			return GetEnumInfo<nuint>(enumType, getNames: true).Names;
		case CorElementType.ELEMENT_TYPE_R4:
			return GetEnumInfo<float>(enumType).Names;
		case CorElementType.ELEMENT_TYPE_R8:
			return GetEnumInfo<double>(enumType).Names;
		case CorElementType.ELEMENT_TYPE_CHAR:
			return GetEnumInfo<char>(enumType).Names;
		default:
			throw CreateUnknownEnumTypeException();
		}
	}

	public static Type GetUnderlyingType(Type enumType)
	{
		ArgumentNullException.ThrowIfNull(enumType, "enumType");
		return enumType.GetEnumUnderlyingType();
	}

	public static TEnum[] GetValues<TEnum>() where TEnum : struct, Enum
	{
		Array valuesAsUnderlyingTypeNoCopy = GetValuesAsUnderlyingTypeNoCopy((RuntimeType)typeof(TEnum));
		TEnum[] array = new TEnum[valuesAsUnderlyingTypeNoCopy.Length];
		Array.Copy(valuesAsUnderlyingTypeNoCopy, array, valuesAsUnderlyingTypeNoCopy.Length);
		return array;
	}

	[RequiresDynamicCode("It might not be possible to create an array of the enum type at runtime. Use the GetValues<TEnum> overload or the GetValuesAsUnderlyingType method instead.")]
	public static Array GetValues(Type enumType)
	{
		ArgumentNullException.ThrowIfNull(enumType, "enumType");
		return enumType.GetEnumValues();
	}

	public static Array GetValuesAsUnderlyingType<TEnum>() where TEnum : struct, Enum
	{
		return typeof(TEnum).GetEnumValuesAsUnderlyingType();
	}

	public static Array GetValuesAsUnderlyingType(Type enumType)
	{
		ArgumentNullException.ThrowIfNull(enumType, "enumType");
		return enumType.GetEnumValuesAsUnderlyingType();
	}

	internal static Array GetValuesAsUnderlyingType(RuntimeType enumType)
	{
		return InternalGetCorElementType(enumType) switch
		{
			CorElementType.ELEMENT_TYPE_I1 => GetEnumInfo<byte>(enumType, getNames: false).CloneValues<sbyte>(), 
			CorElementType.ELEMENT_TYPE_U1 => GetEnumInfo<byte>(enumType, getNames: false).CloneValues<byte>(), 
			CorElementType.ELEMENT_TYPE_I2 => GetEnumInfo<ushort>(enumType, getNames: false).CloneValues<short>(), 
			CorElementType.ELEMENT_TYPE_U2 => GetEnumInfo<ushort>(enumType, getNames: false).CloneValues<ushort>(), 
			CorElementType.ELEMENT_TYPE_I4 => GetEnumInfo<uint>(enumType, getNames: false).CloneValues<int>(), 
			CorElementType.ELEMENT_TYPE_U4 => GetEnumInfo<uint>(enumType, getNames: false).CloneValues<uint>(), 
			CorElementType.ELEMENT_TYPE_I8 => GetEnumInfo<ulong>(enumType, getNames: false).CloneValues<long>(), 
			CorElementType.ELEMENT_TYPE_U8 => GetEnumInfo<ulong>(enumType, getNames: false).CloneValues<ulong>(), 
			CorElementType.ELEMENT_TYPE_I => GetEnumInfo<nuint>(enumType, getNames: false).CloneValues<nint>(), 
			CorElementType.ELEMENT_TYPE_U => GetEnumInfo<nuint>(enumType, getNames: false).CloneValues<nuint>(), 
			CorElementType.ELEMENT_TYPE_R4 => GetEnumInfo<float>(enumType, getNames: false).CloneValues<float>(), 
			CorElementType.ELEMENT_TYPE_R8 => GetEnumInfo<double>(enumType, getNames: false).CloneValues<double>(), 
			CorElementType.ELEMENT_TYPE_CHAR => GetEnumInfo<char>(enumType, getNames: false).CloneValues<char>(), 
			_ => throw CreateUnknownEnumTypeException(), 
		};
	}

	internal static Array GetValuesAsUnderlyingTypeNoCopy(RuntimeType enumType)
	{
		return InternalGetCorElementType(enumType) switch
		{
			CorElementType.ELEMENT_TYPE_I1 => GetEnumInfo<byte>(enumType, getNames: false).Values, 
			CorElementType.ELEMENT_TYPE_U1 => GetEnumInfo<byte>(enumType, getNames: false).Values, 
			CorElementType.ELEMENT_TYPE_I2 => GetEnumInfo<ushort>(enumType, getNames: false).Values, 
			CorElementType.ELEMENT_TYPE_U2 => GetEnumInfo<ushort>(enumType, getNames: false).Values, 
			CorElementType.ELEMENT_TYPE_I4 => GetEnumInfo<uint>(enumType, getNames: false).Values, 
			CorElementType.ELEMENT_TYPE_U4 => GetEnumInfo<uint>(enumType, getNames: false).Values, 
			CorElementType.ELEMENT_TYPE_I8 => GetEnumInfo<ulong>(enumType, getNames: false).Values, 
			CorElementType.ELEMENT_TYPE_U8 => GetEnumInfo<ulong>(enumType, getNames: false).Values, 
			CorElementType.ELEMENT_TYPE_I => GetEnumInfo<nuint>(enumType, getNames: false).Values, 
			CorElementType.ELEMENT_TYPE_U => GetEnumInfo<nuint>(enumType, getNames: false).Values, 
			CorElementType.ELEMENT_TYPE_R4 => GetEnumInfo<float>(enumType, getNames: false).Values, 
			CorElementType.ELEMENT_TYPE_R8 => GetEnumInfo<double>(enumType, getNames: false).Values, 
			CorElementType.ELEMENT_TYPE_CHAR => GetEnumInfo<char>(enumType, getNames: false).Values, 
			_ => throw CreateUnknownEnumTypeException(), 
		};
	}

	[Intrinsic]
	public bool HasFlag(Enum flag)
	{
		ArgumentNullException.ThrowIfNull(flag, "flag");
		if (GetType() != flag.GetType() && !GetType().IsEquivalentTo(flag.GetType()))
		{
			throw new ArgumentException(SR.Format(SR.Argument_EnumTypeDoesNotMatch, flag.GetType(), GetType()));
		}
		ref byte rawData = ref this.GetRawData();
		ref byte rawData2 = ref flag.GetRawData();
		switch (InternalGetCorElementType())
		{
		case CorElementType.ELEMENT_TYPE_BOOLEAN:
		case CorElementType.ELEMENT_TYPE_I1:
		case CorElementType.ELEMENT_TYPE_U1:
		{
			byte b = rawData2;
			return (rawData & b) == b;
		}
		case CorElementType.ELEMENT_TYPE_CHAR:
		case CorElementType.ELEMENT_TYPE_I2:
		case CorElementType.ELEMENT_TYPE_U2:
		{
			ushort num3 = Unsafe.As<byte, ushort>(ref rawData2);
			return (Unsafe.As<byte, ushort>(ref rawData) & num3) == num3;
		}
		case CorElementType.ELEMENT_TYPE_I4:
		case CorElementType.ELEMENT_TYPE_U4:
		case CorElementType.ELEMENT_TYPE_R4:
		{
			uint num2 = Unsafe.As<byte, uint>(ref rawData2);
			return (Unsafe.As<byte, uint>(ref rawData) & num2) == num2;
		}
		case CorElementType.ELEMENT_TYPE_I8:
		case CorElementType.ELEMENT_TYPE_U8:
		case CorElementType.ELEMENT_TYPE_R8:
		case CorElementType.ELEMENT_TYPE_I:
		case CorElementType.ELEMENT_TYPE_U:
		{
			ulong num = Unsafe.As<byte, ulong>(ref rawData2);
			return (Unsafe.As<byte, ulong>(ref rawData) & num) == num;
		}
		default:
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static bool IsDefined<TEnum>(TEnum value) where TEnum : struct, Enum
	{
		RuntimeType enumType = (RuntimeType)typeof(TEnum);
		Type enumUnderlyingType = typeof(TEnum).GetEnumUnderlyingType();
		if (enumUnderlyingType == typeof(sbyte) || enumUnderlyingType == typeof(byte))
		{
			return IsDefinedPrimitive(enumType, *(byte*)(&value));
		}
		if (enumUnderlyingType == typeof(short) || enumUnderlyingType == typeof(ushort))
		{
			return IsDefinedPrimitive(enumType, *(ushort*)(&value));
		}
		if (enumUnderlyingType == typeof(int) || enumUnderlyingType == typeof(uint))
		{
			return IsDefinedPrimitive(enumType, *(uint*)(&value));
		}
		if (enumUnderlyingType == typeof(long) || enumUnderlyingType == typeof(ulong))
		{
			return IsDefinedPrimitive(enumType, *(ulong*)(&value));
		}
		if (enumUnderlyingType == typeof(nint) || enumUnderlyingType == typeof(nuint))
		{
			return Enum.IsDefinedPrimitive<nuint>(enumType, *(nuint*)(&value));
		}
		if (enumUnderlyingType == typeof(float))
		{
			return IsDefinedPrimitive(enumType, *(float*)(&value));
		}
		if (enumUnderlyingType == typeof(double))
		{
			return IsDefinedPrimitive(enumType, *(double*)(&value));
		}
		if (enumUnderlyingType == typeof(char))
		{
			return IsDefinedPrimitive(enumType, *(char*)(&value));
		}
		throw CreateUnknownEnumTypeException();
	}

	internal static bool IsDefinedPrimitive<TStorage>(RuntimeType enumType, TStorage value) where TStorage : struct, INumber<TStorage>
	{
		EnumInfo<TStorage> enumInfo = GetEnumInfo<TStorage>(enumType, getNames: false);
		TStorage[] values = enumInfo.Values;
		if (enumInfo.ValuesAreSequentialFromZero)
		{
			return ulong.CreateTruncating(value) < (ulong)values.Length;
		}
		return FindDefinedIndex(values, value) >= 0;
	}

	public static bool IsDefined(Type enumType, object value)
	{
		ArgumentNullException.ThrowIfNull(enumType, "enumType");
		return enumType.IsEnumDefined(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int FindDefinedIndex<TStorage>(TStorage[] values, TStorage value) where TStorage : struct, IEquatable<TStorage>, IComparable<TStorage>
	{
		ReadOnlySpan<TStorage> span = values;
		if (values.Length > 32)
		{
			return SpanHelpers.BinarySearch(span, value);
		}
		return span.IndexOf(value);
	}

	public static object Parse(Type enumType, string value)
	{
		return Parse(enumType, value, ignoreCase: false);
	}

	public static object Parse(Type enumType, ReadOnlySpan<char> value)
	{
		return Parse(enumType, value, ignoreCase: false);
	}

	public static object Parse(Type enumType, string value, bool ignoreCase)
	{
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
		}
		object result;
		bool flag = TryParse(enumType, value.AsSpan(), ignoreCase, throwOnFailure: true, out result);
		return result;
	}

	public static object Parse(Type enumType, ReadOnlySpan<char> value, bool ignoreCase)
	{
		object result;
		bool flag = TryParse(enumType, value, ignoreCase, throwOnFailure: true, out result);
		return result;
	}

	public static TEnum Parse<TEnum>(string value) where TEnum : struct
	{
		return Parse<TEnum>(value, ignoreCase: false);
	}

	public static TEnum Parse<TEnum>(ReadOnlySpan<char> value) where TEnum : struct
	{
		return Parse<TEnum>(value, ignoreCase: false);
	}

	public static TEnum Parse<TEnum>(string value, bool ignoreCase) where TEnum : struct
	{
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
		}
		TEnum result;
		bool flag = TryParse<TEnum>(value.AsSpan(), ignoreCase, throwOnFailure: true, out result);
		return result;
	}

	public static TEnum Parse<TEnum>(ReadOnlySpan<char> value, bool ignoreCase) where TEnum : struct
	{
		TEnum result;
		bool flag = TryParse<TEnum>(value, ignoreCase, throwOnFailure: true, out result);
		return result;
	}

	public static bool TryParse(Type enumType, string? value, [NotNullWhen(true)] out object? result)
	{
		return TryParse(enumType, value, ignoreCase: false, out result);
	}

	public static bool TryParse(Type enumType, ReadOnlySpan<char> value, [NotNullWhen(true)] out object? result)
	{
		return TryParse(enumType, value, ignoreCase: false, out result);
	}

	public static bool TryParse(Type enumType, string? value, bool ignoreCase, [NotNullWhen(true)] out object? result)
	{
		if (value != null)
		{
			return TryParse(enumType, value.AsSpan(), ignoreCase, throwOnFailure: false, out result);
		}
		result = null;
		return false;
	}

	public static bool TryParse(Type enumType, ReadOnlySpan<char> value, bool ignoreCase, [NotNullWhen(true)] out object? result)
	{
		return TryParse(enumType, value, ignoreCase, throwOnFailure: false, out result);
	}

	private unsafe static bool TryParse(Type enumType, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, [NotNullWhen(true)] out object result)
	{
		bool flag = false;
		long result2 = 0L;
		RuntimeType runtimeType = ValidateRuntimeType(enumType);
		switch (InternalGetCorElementType(runtimeType))
		{
		case CorElementType.ELEMENT_TYPE_I1:
			flag = TryParseByValueOrName<sbyte, byte>(runtimeType, value, ignoreCase, throwOnFailure, out *(sbyte*)(&result2));
			result2 = *(sbyte*)(&result2);
			break;
		case CorElementType.ELEMENT_TYPE_U1:
			flag = TryParseByValueOrName<byte, byte>(runtimeType, value, ignoreCase, throwOnFailure, out *(byte*)(&result2));
			result2 = *(byte*)(&result2);
			break;
		case CorElementType.ELEMENT_TYPE_I2:
			flag = TryParseByValueOrName<short, ushort>(runtimeType, value, ignoreCase, throwOnFailure, out *(short*)(&result2));
			result2 = *(short*)(&result2);
			break;
		case CorElementType.ELEMENT_TYPE_U2:
			flag = TryParseByValueOrName<ushort, ushort>(runtimeType, value, ignoreCase, throwOnFailure, out *(ushort*)(&result2));
			result2 = *(ushort*)(&result2);
			break;
		case CorElementType.ELEMENT_TYPE_I4:
			flag = TryParseByValueOrName<int, uint>(runtimeType, value, ignoreCase, throwOnFailure, out *(int*)(&result2));
			result2 = *(int*)(&result2);
			break;
		case CorElementType.ELEMENT_TYPE_U4:
			flag = TryParseByValueOrName<uint, uint>(runtimeType, value, ignoreCase, throwOnFailure, out *(uint*)(&result2));
			result2 = (uint)(*(int*)(&result2));
			break;
		case CorElementType.ELEMENT_TYPE_I8:
			flag = TryParseByValueOrName<long, ulong>(runtimeType, value, ignoreCase, throwOnFailure, out result2);
			break;
		case CorElementType.ELEMENT_TYPE_U8:
			flag = TryParseByValueOrName<ulong, ulong>(runtimeType, value, ignoreCase, throwOnFailure, out *(ulong*)(&result2));
			break;
		default:
			flag = TryParseRareTypes(runtimeType, value, ignoreCase, throwOnFailure, out result2);
			break;
		}
		result = (flag ? InternalBoxEnum(runtimeType, result2) : null);
		return flag;
		[MethodImpl(MethodImplOptions.NoInlining)]
		static bool TryParseRareTypes(RuntimeType rt, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, [NotNullWhen(true)] out long result)
		{
			bool result4;
			switch (InternalGetCorElementType(rt))
			{
			case CorElementType.ELEMENT_TYPE_R4:
			{
				result4 = TryParseRareTypeByValueOrName<float, float>(rt, value, ignoreCase, throwOnFailure, out var result8);
				result = BitConverter.SingleToInt32Bits(result8);
				break;
			}
			case CorElementType.ELEMENT_TYPE_R8:
			{
				result4 = TryParseRareTypeByValueOrName<double, double>(rt, value, ignoreCase, throwOnFailure, out var result7);
				result = BitConverter.DoubleToInt64Bits(result7);
				break;
			}
			case CorElementType.ELEMENT_TYPE_I:
			{
				result4 = TryParseRareTypeByValueOrName<nint, nuint>(rt, value, ignoreCase, throwOnFailure, out nint result6);
				result = result6;
				break;
			}
			case CorElementType.ELEMENT_TYPE_U:
			{
				result4 = TryParseRareTypeByValueOrName<nuint, nuint>(rt, value, ignoreCase, throwOnFailure, out nuint result5);
				result = (long)result5;
				break;
			}
			case CorElementType.ELEMENT_TYPE_CHAR:
			{
				result4 = TryParseRareTypeByValueOrName<char, char>(rt, value, ignoreCase, throwOnFailure, out var result3);
				result = result3;
				break;
			}
			default:
				throw CreateUnknownEnumTypeException();
			}
			return result4;
		}
	}

	public static bool TryParse<TEnum>([NotNullWhen(true)] string? value, out TEnum result) where TEnum : struct
	{
		return TryParse<TEnum>(value, ignoreCase: false, out result);
	}

	public static bool TryParse<TEnum>(ReadOnlySpan<char> value, out TEnum result) where TEnum : struct
	{
		return TryParse<TEnum>(value, ignoreCase: false, throwOnFailure: false, out result);
	}

	public static bool TryParse<TEnum>([NotNullWhen(true)] string? value, bool ignoreCase, out TEnum result) where TEnum : struct
	{
		if (value != null)
		{
			return TryParse<TEnum>(value.AsSpan(), ignoreCase, throwOnFailure: false, out result);
		}
		result = default(TEnum);
		return false;
	}

	public static bool TryParse<TEnum>(ReadOnlySpan<char> value, bool ignoreCase, out TEnum result) where TEnum : struct
	{
		return TryParse<TEnum>(value, ignoreCase, throwOnFailure: false, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryParse<TEnum>(ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out TEnum result) where TEnum : struct
	{
		if (!typeof(TEnum).IsEnum)
		{
			throw new ArgumentException(SR.Arg_MustBeEnum, "TEnum");
		}
		Unsafe.SkipInit<TEnum>(out result);
		RuntimeType enumType = (RuntimeType)typeof(TEnum);
		Type enumUnderlyingType = typeof(TEnum).GetEnumUnderlyingType();
		if (enumUnderlyingType == typeof(sbyte))
		{
			return TryParseByValueOrName<sbyte, byte>(enumType, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, sbyte>(ref result));
		}
		if (enumUnderlyingType == typeof(byte))
		{
			return TryParseByValueOrName<byte, byte>(enumType, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, byte>(ref result));
		}
		if (enumUnderlyingType == typeof(short))
		{
			return TryParseByValueOrName<short, ushort>(enumType, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, short>(ref result));
		}
		if (enumUnderlyingType == typeof(ushort))
		{
			return TryParseByValueOrName<ushort, ushort>(enumType, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, ushort>(ref result));
		}
		if (enumUnderlyingType == typeof(int))
		{
			return TryParseByValueOrName<int, uint>(enumType, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, int>(ref result));
		}
		if (enumUnderlyingType == typeof(uint))
		{
			return TryParseByValueOrName<uint, uint>(enumType, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, uint>(ref result));
		}
		if (enumUnderlyingType == typeof(long))
		{
			return TryParseByValueOrName<long, ulong>(enumType, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, long>(ref result));
		}
		if (enumUnderlyingType == typeof(ulong))
		{
			return TryParseByValueOrName<ulong, ulong>(enumType, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, ulong>(ref result));
		}
		if (enumUnderlyingType == typeof(nint))
		{
			return TryParseRareTypeByValueOrName<nint, nuint>(enumType, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, nint>(ref result));
		}
		if (enumUnderlyingType == typeof(nuint))
		{
			return TryParseRareTypeByValueOrName<nuint, nuint>(enumType, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, nuint>(ref result));
		}
		if (enumUnderlyingType == typeof(float))
		{
			return TryParseRareTypeByValueOrName<float, float>(enumType, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, float>(ref result));
		}
		if (enumUnderlyingType == typeof(double))
		{
			return TryParseRareTypeByValueOrName<double, double>(enumType, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, double>(ref result));
		}
		if (enumUnderlyingType == typeof(char))
		{
			return TryParseRareTypeByValueOrName<char, char>(enumType, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, char>(ref result));
		}
		throw CreateUnknownEnumTypeException();
	}

	private static bool TryParseByValueOrName<TUnderlying, TStorage>(RuntimeType enumType, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out TUnderlying result) where TUnderlying : unmanaged, IBinaryIntegerParseAndFormatInfo<TUnderlying> where TStorage : unmanaged, IBinaryIntegerParseAndFormatInfo<TStorage>
	{
		if (!value.IsEmpty)
		{
			char c = value[0];
			if (char.IsWhiteSpace(c))
			{
				value = value.TrimStart();
				if (value.IsEmpty)
				{
					goto IL_00a2;
				}
				c = value[0];
			}
			if (!char.IsAsciiDigit(c) && c != '-' && c != '+')
			{
				Unsafe.SkipInit<TUnderlying>(out result);
				return TryParseByName<TStorage>(enumType, value, ignoreCase, throwOnFailure, out Unsafe.As<TUnderlying, TStorage>(ref result));
			}
			NumberFormatInfo numberFormat = CultureInfo.InvariantCulture.NumberFormat;
			switch (Number.TryParseBinaryIntegerStyle<char, TUnderlying>(value, NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign, numberFormat, out result))
			{
			case Number.ParsingStatus.OK:
				return true;
			default:
				Unsafe.SkipInit<TUnderlying>(out result);
				return TryParseByName<TStorage>(enumType, value, ignoreCase, throwOnFailure, out Unsafe.As<TUnderlying, TStorage>(ref result));
			case Number.ParsingStatus.Overflow:
				break;
			}
			if (throwOnFailure)
			{
				Number.ThrowOverflowException<TUnderlying>();
			}
		}
		goto IL_00a2;
		IL_00a2:
		if (throwOnFailure)
		{
			ThrowInvalidEmptyParseArgument();
		}
		result = default(TUnderlying);
		return false;
	}

	private static bool TryParseRareTypeByValueOrName<TUnderlying, TStorage>(RuntimeType enumType, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out TUnderlying result) where TUnderlying : struct, INumber<TUnderlying>, IBitwiseOperators<TUnderlying, TUnderlying, TUnderlying>, IMinMaxValue<TUnderlying> where TStorage : struct, INumber<TStorage>, IBitwiseOperators<TStorage, TStorage, TStorage>, IMinMaxValue<TStorage>
	{
		if (!value.IsEmpty)
		{
			char c = value[0];
			if (char.IsWhiteSpace(c))
			{
				value = value.TrimStart();
				if (value.IsEmpty)
				{
					goto IL_00d3;
				}
				c = value[0];
			}
			if (!char.IsAsciiDigit(c) && c != '-' && c != '+')
			{
				Unsafe.SkipInit<TUnderlying>(out result);
				return TryParseByName<TStorage>(enumType, value, ignoreCase, throwOnFailure, out Unsafe.As<TUnderlying, TStorage>(ref result));
			}
			Type underlyingType = GetUnderlyingType(enumType);
			Number.ParsingStatus parsingStatus;
			try
			{
				result = (TUnderlying)ToObject(enumType, Convert.ChangeType(value.ToString(), underlyingType, CultureInfo.InvariantCulture));
				return true;
			}
			catch (FormatException)
			{
				parsingStatus = Number.ParsingStatus.Failed;
			}
			catch when (!throwOnFailure)
			{
				parsingStatus = Number.ParsingStatus.Overflow;
			}
			if (parsingStatus != Number.ParsingStatus.Overflow)
			{
				Unsafe.SkipInit<TUnderlying>(out result);
				return TryParseByName<TStorage>(enumType, value, ignoreCase, throwOnFailure, out Unsafe.As<TUnderlying, TStorage>(ref result));
			}
			if (throwOnFailure)
			{
				ThrowHelper.ThrowOverflowException();
			}
		}
		goto IL_00d3;
		IL_00d3:
		if (throwOnFailure)
		{
			ThrowInvalidEmptyParseArgument();
		}
		result = default(TUnderlying);
		return false;
	}

	private static bool TryParseByName<TStorage>(RuntimeType enumType, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out TStorage result) where TStorage : struct, INumber<TStorage>, IBitwiseOperators<TStorage, TStorage, TStorage>
	{
		ReadOnlySpan<char> readOnlySpan = value;
		EnumInfo<TStorage> enumInfo = GetEnumInfo<TStorage>(enumType);
		string[] names = enumInfo.Names;
		TStorage[] values = enumInfo.Values;
		bool flag = true;
		TStorage val = default(TStorage);
		while (value.Length > 0)
		{
			int num = value.IndexOf(',');
			ReadOnlySpan<char> span;
			if (num < 0)
			{
				span = value.Trim();
				value = default(ReadOnlySpan<char>);
			}
			else
			{
				if (num == value.Length - 1)
				{
					flag = false;
					break;
				}
				span = value.Slice(0, num).Trim();
				value = value.Slice(num + 1);
			}
			bool flag2 = false;
			if (ignoreCase)
			{
				for (int i = 0; i < names.Length; i++)
				{
					if (span.EqualsOrdinalIgnoreCase(names[i]))
					{
						val |= values[i];
						flag2 = true;
						break;
					}
				}
			}
			else
			{
				for (int j = 0; j < names.Length; j++)
				{
					if (span.SequenceEqual(names[j]))
					{
						val |= values[j];
						flag2 = true;
						break;
					}
				}
			}
			if (!flag2)
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			result = val;
			return true;
		}
		if (throwOnFailure)
		{
			throw new ArgumentException(SR.Format(SR.Arg_EnumValueNotFound, readOnlySpan.ToString()));
		}
		result = default(TStorage);
		return false;
	}

	internal static ulong ToUInt64(object value)
	{
		switch (Convert.GetTypeCode(value))
		{
		case TypeCode.SByte:
			return (ulong)(sbyte)value;
		case TypeCode.Byte:
			return (byte)value;
		case TypeCode.Int16:
			return (ulong)(short)value;
		case TypeCode.UInt16:
			return (ushort)value;
		case TypeCode.Int32:
			return (ulong)(int)value;
		case TypeCode.Int64:
			return (ulong)(long)value;
		case TypeCode.UInt32:
			return (uint)value;
		case TypeCode.UInt64:
			return (ulong)value;
		case TypeCode.Char:
			return (char)value;
		default:
			if (value != null)
			{
				Type type = value.GetType();
				if (type.IsEnum)
				{
					type = type.GetEnumUnderlyingType();
				}
				if (type == typeof(nint))
				{
					return (ulong)(nint)value;
				}
				if (type == typeof(nuint))
				{
					return (nuint)value;
				}
			}
			throw CreateUnknownEnumTypeException();
		}
	}

	internal object GetValue()
	{
		ref byte rawData = ref this.GetRawData();
		return InternalGetCorElementType() switch
		{
			CorElementType.ELEMENT_TYPE_I1 => Unsafe.As<byte, sbyte>(ref rawData), 
			CorElementType.ELEMENT_TYPE_U1 => rawData, 
			CorElementType.ELEMENT_TYPE_I2 => Unsafe.As<byte, short>(ref rawData), 
			CorElementType.ELEMENT_TYPE_U2 => Unsafe.As<byte, ushort>(ref rawData), 
			CorElementType.ELEMENT_TYPE_I4 => Unsafe.As<byte, int>(ref rawData), 
			CorElementType.ELEMENT_TYPE_U4 => Unsafe.As<byte, uint>(ref rawData), 
			CorElementType.ELEMENT_TYPE_I8 => Unsafe.As<byte, long>(ref rawData), 
			CorElementType.ELEMENT_TYPE_U8 => Unsafe.As<byte, ulong>(ref rawData), 
			CorElementType.ELEMENT_TYPE_R4 => Unsafe.As<byte, float>(ref rawData), 
			CorElementType.ELEMENT_TYPE_R8 => Unsafe.As<byte, double>(ref rawData), 
			CorElementType.ELEMENT_TYPE_I => Unsafe.As<byte, nint>(ref rawData), 
			CorElementType.ELEMENT_TYPE_U => Unsafe.As<byte, nuint>(ref rawData), 
			CorElementType.ELEMENT_TYPE_CHAR => Unsafe.As<byte, char>(ref rawData), 
			CorElementType.ELEMENT_TYPE_BOOLEAN => Unsafe.As<byte, bool>(ref rawData), 
			_ => throw CreateUnknownEnumTypeException(), 
		};
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (GetType() != obj.GetType())
		{
			return false;
		}
		ref byte rawData = ref this.GetRawData();
		ref byte rawData2 = ref obj.GetRawData();
		switch (InternalGetCorElementType())
		{
		case CorElementType.ELEMENT_TYPE_BOOLEAN:
		case CorElementType.ELEMENT_TYPE_I1:
		case CorElementType.ELEMENT_TYPE_U1:
			return rawData == rawData2;
		case CorElementType.ELEMENT_TYPE_CHAR:
		case CorElementType.ELEMENT_TYPE_I2:
		case CorElementType.ELEMENT_TYPE_U2:
			return Unsafe.As<byte, ushort>(ref rawData) == Unsafe.As<byte, ushort>(ref rawData2);
		case CorElementType.ELEMENT_TYPE_I4:
		case CorElementType.ELEMENT_TYPE_U4:
		case CorElementType.ELEMENT_TYPE_R4:
			return Unsafe.As<byte, uint>(ref rawData) == Unsafe.As<byte, uint>(ref rawData2);
		case CorElementType.ELEMENT_TYPE_I8:
		case CorElementType.ELEMENT_TYPE_U8:
		case CorElementType.ELEMENT_TYPE_R8:
		case CorElementType.ELEMENT_TYPE_I:
		case CorElementType.ELEMENT_TYPE_U:
			return Unsafe.As<byte, ulong>(ref rawData) == Unsafe.As<byte, ulong>(ref rawData2);
		default:
			return false;
		}
	}

	public override int GetHashCode()
	{
		ref byte rawData = ref this.GetRawData();
		return InternalGetCorElementType() switch
		{
			CorElementType.ELEMENT_TYPE_I1 => Unsafe.As<byte, sbyte>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_U1 => rawData.GetHashCode(), 
			CorElementType.ELEMENT_TYPE_I2 => Unsafe.As<byte, short>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_U2 => Unsafe.As<byte, ushort>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_I4 => Unsafe.As<byte, int>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_U4 => Unsafe.As<byte, uint>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_I8 => Unsafe.As<byte, long>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_U8 => Unsafe.As<byte, ulong>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_R4 => Unsafe.As<byte, float>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_R8 => Unsafe.As<byte, double>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_I => ((IntPtr)Unsafe.As<byte, nint>(ref rawData)).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_U => ((UIntPtr)Unsafe.As<byte, nuint>(ref rawData)).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_CHAR => Unsafe.As<byte, char>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_BOOLEAN => Unsafe.As<byte, bool>(ref rawData).GetHashCode(), 
			_ => throw CreateUnknownEnumTypeException(), 
		};
	}

	public int CompareTo(object? target)
	{
		if (target == this)
		{
			return 0;
		}
		if (target == null)
		{
			return 1;
		}
		if (GetType() != target.GetType())
		{
			throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, target.GetType(), GetType()));
		}
		ref byte rawData = ref this.GetRawData();
		ref byte rawData2 = ref target.GetRawData();
		switch (InternalGetCorElementType())
		{
		case CorElementType.ELEMENT_TYPE_I1:
			return Unsafe.As<byte, sbyte>(ref rawData).CompareTo(Unsafe.As<byte, sbyte>(ref rawData2));
		case CorElementType.ELEMENT_TYPE_BOOLEAN:
		case CorElementType.ELEMENT_TYPE_U1:
			return rawData.CompareTo(rawData2);
		case CorElementType.ELEMENT_TYPE_I2:
			return Unsafe.As<byte, short>(ref rawData).CompareTo(Unsafe.As<byte, short>(ref rawData2));
		case CorElementType.ELEMENT_TYPE_CHAR:
		case CorElementType.ELEMENT_TYPE_U2:
			return Unsafe.As<byte, ushort>(ref rawData).CompareTo(Unsafe.As<byte, ushort>(ref rawData2));
		case CorElementType.ELEMENT_TYPE_I4:
			return Unsafe.As<byte, int>(ref rawData).CompareTo(Unsafe.As<byte, int>(ref rawData2));
		case CorElementType.ELEMENT_TYPE_U4:
			return Unsafe.As<byte, uint>(ref rawData).CompareTo(Unsafe.As<byte, uint>(ref rawData2));
		case CorElementType.ELEMENT_TYPE_I8:
		case CorElementType.ELEMENT_TYPE_I:
			return Unsafe.As<byte, long>(ref rawData).CompareTo(Unsafe.As<byte, long>(ref rawData2));
		case CorElementType.ELEMENT_TYPE_U8:
		case CorElementType.ELEMENT_TYPE_U:
			return Unsafe.As<byte, ulong>(ref rawData).CompareTo(Unsafe.As<byte, ulong>(ref rawData2));
		case CorElementType.ELEMENT_TYPE_R4:
			return Unsafe.As<byte, float>(ref rawData).CompareTo(Unsafe.As<byte, float>(ref rawData2));
		case CorElementType.ELEMENT_TYPE_R8:
			return Unsafe.As<byte, double>(ref rawData).CompareTo(Unsafe.As<byte, double>(ref rawData2));
		default:
			return 0;
		}
	}

	public override string ToString()
	{
		RuntimeType runtimeType = (RuntimeType)GetType();
		ref byte rawData2 = ref this.GetRawData();
		return InternalGetCorElementType() switch
		{
			CorElementType.ELEMENT_TYPE_I1 => ToString<sbyte, byte>(runtimeType, ref rawData2), 
			CorElementType.ELEMENT_TYPE_U1 => ToStringInlined<byte, byte>(runtimeType, ref rawData2), 
			CorElementType.ELEMENT_TYPE_I2 => ToString<short, ushort>(runtimeType, ref rawData2), 
			CorElementType.ELEMENT_TYPE_U2 => ToString<ushort, ushort>(runtimeType, ref rawData2), 
			CorElementType.ELEMENT_TYPE_I4 => ToStringInlined<int, uint>(runtimeType, ref rawData2), 
			CorElementType.ELEMENT_TYPE_U4 => ToString<uint, uint>(runtimeType, ref rawData2), 
			CorElementType.ELEMENT_TYPE_I8 => ToString<long, ulong>(runtimeType, ref rawData2), 
			CorElementType.ELEMENT_TYPE_U8 => ToString<ulong, ulong>(runtimeType, ref rawData2), 
			_ => HandleRareTypes(runtimeType, ref rawData2), 
		};
		[MethodImpl(MethodImplOptions.NoInlining)]
		static string HandleRareTypes(RuntimeType enumType, ref byte rawData)
		{
			return InternalGetCorElementType(enumType) switch
			{
				CorElementType.ELEMENT_TYPE_R4 => ToString<float, float>(enumType, ref rawData), 
				CorElementType.ELEMENT_TYPE_R8 => ToString<double, double>(enumType, ref rawData), 
				CorElementType.ELEMENT_TYPE_I => ToString<nint, nuint>(enumType, ref rawData), 
				CorElementType.ELEMENT_TYPE_U => ToString<nuint, nuint>(enumType, ref rawData), 
				CorElementType.ELEMENT_TYPE_CHAR => ToString<char, char>(enumType, ref rawData), 
				_ => throw CreateUnknownEnumTypeException(), 
			};
		}
	}

	public string ToString([StringSyntax("EnumFormat")] string? format)
	{
		if (string.IsNullOrEmpty(format))
		{
			return ToString();
		}
		if (format.Length == 1)
		{
			char c = format[0];
			RuntimeType runtimeType = (RuntimeType)GetType();
			ref byte rawData2 = ref this.GetRawData();
			return InternalGetCorElementType() switch
			{
				CorElementType.ELEMENT_TYPE_I1 => ToString<sbyte, byte>(runtimeType, c, ref rawData2), 
				CorElementType.ELEMENT_TYPE_U1 => ToStringInlined<byte, byte>(runtimeType, c, ref rawData2), 
				CorElementType.ELEMENT_TYPE_I2 => ToString<short, ushort>(runtimeType, c, ref rawData2), 
				CorElementType.ELEMENT_TYPE_U2 => ToString<ushort, ushort>(runtimeType, c, ref rawData2), 
				CorElementType.ELEMENT_TYPE_I4 => ToStringInlined<int, uint>(runtimeType, c, ref rawData2), 
				CorElementType.ELEMENT_TYPE_U4 => ToString<uint, uint>(runtimeType, c, ref rawData2), 
				CorElementType.ELEMENT_TYPE_I8 => ToString<long, ulong>(runtimeType, c, ref rawData2), 
				CorElementType.ELEMENT_TYPE_U8 => ToString<ulong, ulong>(runtimeType, c, ref rawData2), 
				_ => HandleRareTypes(runtimeType, c, ref rawData2), 
			};
		}
		throw CreateInvalidFormatSpecifierException();
		[MethodImpl(MethodImplOptions.NoInlining)]
		static string HandleRareTypes(RuntimeType enumType, char formatChar, ref byte rawData)
		{
			return InternalGetCorElementType(enumType) switch
			{
				CorElementType.ELEMENT_TYPE_R4 => ToString<float, float>(enumType, formatChar, ref rawData), 
				CorElementType.ELEMENT_TYPE_R8 => ToString<double, double>(enumType, formatChar, ref rawData), 
				CorElementType.ELEMENT_TYPE_I => ToString<nint, nuint>(enumType, formatChar, ref rawData), 
				CorElementType.ELEMENT_TYPE_U => ToString<nuint, nuint>(enumType, formatChar, ref rawData), 
				CorElementType.ELEMENT_TYPE_CHAR => ToString<char, char>(enumType, formatChar, ref rawData), 
				_ => throw CreateUnknownEnumTypeException(), 
			};
		}
	}

	[Obsolete("The provider argument is not used. Use ToString() instead.")]
	public string ToString(IFormatProvider? provider)
	{
		return ToString();
	}

	[Obsolete("The provider argument is not used. Use ToString(String) instead.")]
	public string ToString([StringSyntax("EnumFormat")] string? format, IFormatProvider? provider)
	{
		return ToString(format);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string ToString<TUnderlying, TStorage>(RuntimeType enumType, ref byte rawData) where TUnderlying : struct, INumber<TUnderlying>, IBitwiseOperators<TUnderlying, TUnderlying, TUnderlying> where TStorage : struct, INumber<TStorage>, IBitwiseOperators<TStorage, TStorage, TStorage>
	{
		return ToStringInlined<TUnderlying, TStorage>(enumType, ref rawData);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string ToStringInlined<TUnderlying, TStorage>(RuntimeType enumType, ref byte rawData) where TUnderlying : struct, INumber<TUnderlying>, IBitwiseOperators<TUnderlying, TUnderlying, TUnderlying> where TStorage : struct, INumber<TStorage>, IBitwiseOperators<TStorage, TStorage, TStorage>
	{
		TStorage val = Unsafe.As<byte, TStorage>(ref rawData);
		EnumInfo<TStorage> enumInfo = GetEnumInfo<TStorage>(enumType);
		string text = (enumInfo.HasFlagsAttribute ? FormatFlagNames(enumInfo, val) : GetNameInlined(enumInfo, val));
		return text ?? Unsafe.BitCast<TStorage, TUnderlying>(val).ToString();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string ToString<TUnderlying, TStorage>(RuntimeType enumType, char format, ref byte rawData) where TUnderlying : struct, INumber<TUnderlying>, IBitwiseOperators<TUnderlying, TUnderlying, TUnderlying>, IMinMaxValue<TUnderlying> where TStorage : struct, INumber<TStorage>, IBitwiseOperators<TStorage, TStorage, TStorage>, IMinMaxValue<TStorage>
	{
		return ToStringInlined<TUnderlying, TStorage>(enumType, format, ref rawData);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string ToStringInlined<TUnderlying, TStorage>(RuntimeType enumType, char format, ref byte rawData) where TUnderlying : struct, INumber<TUnderlying>, IBitwiseOperators<TUnderlying, TUnderlying, TUnderlying>, IMinMaxValue<TUnderlying> where TStorage : struct, INumber<TStorage>, IBitwiseOperators<TStorage, TStorage, TStorage>, IMinMaxValue<TStorage>
	{
		TStorage val = Unsafe.As<byte, TStorage>(ref rawData);
		string text;
		switch (format | 0x20)
		{
		case 103:
		{
			EnumInfo<TStorage> enumInfo = GetEnumInfo<TStorage>(enumType);
			text = (enumInfo.HasFlagsAttribute ? FormatFlagNames(enumInfo, val) : GetNameInlined(enumInfo, val));
			if (text != null)
			{
				break;
			}
			goto case 100;
		}
		case 100:
			text = Unsafe.BitCast<TStorage, TUnderlying>(val).ToString();
			break;
		case 120:
			text = FormatNumberAsHex<TStorage>(ref rawData);
			break;
		case 102:
			text = FormatFlagNames(GetEnumInfo<TStorage>(enumType), val);
			if (text != null)
			{
				break;
			}
			goto case 100;
		default:
			throw CreateInvalidFormatSpecifierException();
		}
		return text;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static string FormatNumberAsHex<TStorage>(ref byte data) where TStorage : struct
	{
		fixed (byte* state = &data)
		{
			return string.Create(Unsafe.SizeOf<TStorage>() * 2, (nint)state, delegate(Span<char> destination, nint intptr)
			{
				int charsWritten;
				bool flag = TryFormatNumberAsHex<TStorage>(ref *(byte*)intptr, destination, out charsWritten);
			});
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryFormatNumberAsHex<TStorage>(ref byte data, Span<char> destination, out int charsWritten) where TStorage : struct
	{
		if (Unsafe.SizeOf<TStorage>() * 2 <= destination.Length)
		{
			if (typeof(TStorage) == typeof(byte) || typeof(TStorage) == typeof(sbyte))
			{
				HexConverter.ToCharsBuffer(data, destination);
			}
			else if (typeof(TStorage) == typeof(ushort) || typeof(TStorage) == typeof(short) || typeof(TStorage) == typeof(char))
			{
				ushort num = Unsafe.As<byte, ushort>(ref data);
				HexConverter.ToCharsBuffer((byte)(num >> 8), destination);
				HexConverter.ToCharsBuffer((byte)num, destination, 2);
			}
			else if (typeof(TStorage) == typeof(uint) || typeof(TStorage) == typeof(int))
			{
				uint num2 = Unsafe.As<byte, uint>(ref data);
				HexConverter.ToCharsBuffer((byte)(num2 >> 24), destination);
				HexConverter.ToCharsBuffer((byte)(num2 >> 16), destination, 2);
				HexConverter.ToCharsBuffer((byte)(num2 >> 8), destination, 4);
				HexConverter.ToCharsBuffer((byte)num2, destination, 6);
			}
			else
			{
				if (!(typeof(TStorage) == typeof(ulong)) && !(typeof(TStorage) == typeof(nuint)) && !(typeof(TStorage) == typeof(nint)) && !(typeof(TStorage) == typeof(long)))
				{
					throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
				}
				ulong num3 = Unsafe.As<byte, ulong>(ref data);
				HexConverter.ToCharsBuffer((byte)(num3 >> 56), destination);
				HexConverter.ToCharsBuffer((byte)(num3 >> 48), destination, 2);
				HexConverter.ToCharsBuffer((byte)(num3 >> 40), destination, 4);
				HexConverter.ToCharsBuffer((byte)(num3 >> 32), destination, 6);
				HexConverter.ToCharsBuffer((byte)(num3 >> 24), destination, 8);
				HexConverter.ToCharsBuffer((byte)(num3 >> 16), destination, 10);
				HexConverter.ToCharsBuffer((byte)(num3 >> 8), destination, 12);
				HexConverter.ToCharsBuffer((byte)num3, destination, 14);
			}
			charsWritten = Unsafe.SizeOf<TStorage>() * 2;
			return true;
		}
		charsWritten = 0;
		return false;
	}

	public static string Format(Type enumType, object value, [StringSyntax("EnumFormat")] string format)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		ArgumentNullException.ThrowIfNull(format, "format");
		RuntimeType runtimeType = ValidateRuntimeType(enumType);
		Type type = value.GetType();
		if (type.IsEnum)
		{
			if (!type.IsEquivalentTo(runtimeType))
			{
				throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, type, runtimeType));
			}
			if (format.Length == 1)
			{
				return ((Enum)value).ToString(format);
			}
		}
		else
		{
			Type underlyingType = GetUnderlyingType(runtimeType);
			if (type != underlyingType)
			{
				throw new ArgumentException(SR.Format(SR.Arg_EnumFormatUnderlyingTypeAndObjectMustBeSameType, type, underlyingType));
			}
			if (format.Length == 1)
			{
				char c = format[0];
				ref byte rawData = ref value.GetRawData();
				return InternalGetCorElementType(runtimeType) switch
				{
					CorElementType.ELEMENT_TYPE_I1 => ToString<sbyte, byte>(runtimeType, c, ref rawData), 
					CorElementType.ELEMENT_TYPE_U1 => ToString<byte, byte>(runtimeType, c, ref rawData), 
					CorElementType.ELEMENT_TYPE_I2 => ToString<short, ushort>(runtimeType, c, ref rawData), 
					CorElementType.ELEMENT_TYPE_U2 => ToString<ushort, ushort>(runtimeType, c, ref rawData), 
					CorElementType.ELEMENT_TYPE_I4 => ToString<int, uint>(runtimeType, c, ref rawData), 
					CorElementType.ELEMENT_TYPE_U4 => ToString<uint, uint>(runtimeType, c, ref rawData), 
					CorElementType.ELEMENT_TYPE_I8 => ToString<long, ulong>(runtimeType, c, ref rawData), 
					CorElementType.ELEMENT_TYPE_U8 => ToString<ulong, ulong>(runtimeType, c, ref rawData), 
					CorElementType.ELEMENT_TYPE_R4 => ToString<float, float>(runtimeType, c, ref rawData), 
					CorElementType.ELEMENT_TYPE_R8 => ToString<double, double>(runtimeType, c, ref rawData), 
					CorElementType.ELEMENT_TYPE_I => ToString<nint, nuint>(runtimeType, c, ref rawData), 
					CorElementType.ELEMENT_TYPE_U => ToString<nuint, nuint>(runtimeType, c, ref rawData), 
					CorElementType.ELEMENT_TYPE_CHAR => ToString<char, char>(runtimeType, c, ref rawData), 
					_ => throw CreateUnknownEnumTypeException(), 
				};
			}
		}
		throw CreateInvalidFormatSpecifierException();
	}

	bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
	{
		RuntimeType enumType = (RuntimeType)GetType();
		ref byte rawData = ref this.GetRawData();
		CorElementType corElementType = InternalGetCorElementType();
		if (format.IsEmpty)
		{
			return corElementType switch
			{
				CorElementType.ELEMENT_TYPE_I1 => TryFormatPrimitiveDefault<sbyte, byte>(enumType, (sbyte)rawData, destination, out charsWritten), 
				CorElementType.ELEMENT_TYPE_U1 => TryFormatPrimitiveDefault<byte, byte>(enumType, rawData, destination, out charsWritten), 
				CorElementType.ELEMENT_TYPE_I2 => TryFormatPrimitiveDefault<short, ushort>(enumType, Unsafe.As<byte, short>(ref rawData), destination, out charsWritten), 
				CorElementType.ELEMENT_TYPE_U2 => TryFormatPrimitiveDefault<ushort, ushort>(enumType, Unsafe.As<byte, ushort>(ref rawData), destination, out charsWritten), 
				CorElementType.ELEMENT_TYPE_I4 => TryFormatPrimitiveDefault<int, uint>(enumType, Unsafe.As<byte, int>(ref rawData), destination, out charsWritten), 
				CorElementType.ELEMENT_TYPE_U4 => TryFormatPrimitiveDefault<uint, uint>(enumType, Unsafe.As<byte, uint>(ref rawData), destination, out charsWritten), 
				CorElementType.ELEMENT_TYPE_I8 => TryFormatPrimitiveDefault<long, ulong>(enumType, Unsafe.As<byte, long>(ref rawData), destination, out charsWritten), 
				CorElementType.ELEMENT_TYPE_U8 => TryFormatPrimitiveDefault<ulong, ulong>(enumType, Unsafe.As<byte, ulong>(ref rawData), destination, out charsWritten), 
				CorElementType.ELEMENT_TYPE_R4 => TryFormatPrimitiveDefault<float, float>(enumType, Unsafe.As<byte, float>(ref rawData), destination, out charsWritten), 
				CorElementType.ELEMENT_TYPE_R8 => TryFormatPrimitiveDefault<double, double>(enumType, Unsafe.As<byte, double>(ref rawData), destination, out charsWritten), 
				CorElementType.ELEMENT_TYPE_I => TryFormatPrimitiveDefault<nint, nuint>(enumType, Unsafe.As<byte, nint>(ref rawData), destination, out charsWritten), 
				CorElementType.ELEMENT_TYPE_U => TryFormatPrimitiveDefault<nuint, nuint>(enumType, Unsafe.As<byte, nuint>(ref rawData), destination, out charsWritten), 
				CorElementType.ELEMENT_TYPE_CHAR => TryFormatPrimitiveDefault<char, char>(enumType, Unsafe.As<byte, char>(ref rawData), destination, out charsWritten), 
				_ => throw CreateUnknownEnumTypeException(), 
			};
		}
		return corElementType switch
		{
			CorElementType.ELEMENT_TYPE_I1 => TryFormatPrimitiveNonDefault<sbyte, byte>(enumType, (sbyte)rawData, destination, out charsWritten, format), 
			CorElementType.ELEMENT_TYPE_U1 => TryFormatPrimitiveNonDefault<byte, byte>(enumType, rawData, destination, out charsWritten, format), 
			CorElementType.ELEMENT_TYPE_I2 => TryFormatPrimitiveNonDefault<short, ushort>(enumType, Unsafe.As<byte, short>(ref rawData), destination, out charsWritten, format), 
			CorElementType.ELEMENT_TYPE_U2 => TryFormatPrimitiveNonDefault<ushort, ushort>(enumType, Unsafe.As<byte, ushort>(ref rawData), destination, out charsWritten, format), 
			CorElementType.ELEMENT_TYPE_I4 => TryFormatPrimitiveNonDefault<int, uint>(enumType, Unsafe.As<byte, int>(ref rawData), destination, out charsWritten, format), 
			CorElementType.ELEMENT_TYPE_U4 => TryFormatPrimitiveNonDefault<uint, uint>(enumType, Unsafe.As<byte, uint>(ref rawData), destination, out charsWritten, format), 
			CorElementType.ELEMENT_TYPE_I8 => TryFormatPrimitiveNonDefault<long, ulong>(enumType, Unsafe.As<byte, long>(ref rawData), destination, out charsWritten, format), 
			CorElementType.ELEMENT_TYPE_U8 => TryFormatPrimitiveNonDefault<ulong, ulong>(enumType, Unsafe.As<byte, ulong>(ref rawData), destination, out charsWritten, format), 
			CorElementType.ELEMENT_TYPE_R4 => TryFormatPrimitiveNonDefault<float, float>(enumType, Unsafe.As<byte, float>(ref rawData), destination, out charsWritten, format), 
			CorElementType.ELEMENT_TYPE_R8 => TryFormatPrimitiveNonDefault<double, double>(enumType, Unsafe.As<byte, double>(ref rawData), destination, out charsWritten, format), 
			CorElementType.ELEMENT_TYPE_I => TryFormatPrimitiveNonDefault<nint, nuint>(enumType, Unsafe.As<byte, nint>(ref rawData), destination, out charsWritten, format), 
			CorElementType.ELEMENT_TYPE_U => TryFormatPrimitiveNonDefault<nuint, nuint>(enumType, Unsafe.As<byte, nuint>(ref rawData), destination, out charsWritten, format), 
			CorElementType.ELEMENT_TYPE_CHAR => TryFormatPrimitiveNonDefault<char, char>(enumType, Unsafe.As<byte, char>(ref rawData), destination, out charsWritten, format), 
			_ => throw CreateUnknownEnumTypeException(), 
		};
	}

	public unsafe static bool TryFormat<TEnum>(TEnum value, Span<char> destination, out int charsWritten, [StringSyntax("EnumFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>)) where TEnum : struct, Enum
	{
		RuntimeType enumType = (RuntimeType)typeof(TEnum);
		Type enumUnderlyingType = typeof(TEnum).GetEnumUnderlyingType();
		if (format.IsEmpty)
		{
			if (enumUnderlyingType == typeof(int))
			{
				return TryFormatPrimitiveDefault<int, uint>(enumType, *(int*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(uint))
			{
				return TryFormatPrimitiveDefault<uint, uint>(enumType, *(uint*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(long))
			{
				return TryFormatPrimitiveDefault<long, ulong>(enumType, *(long*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(ulong))
			{
				return TryFormatPrimitiveDefault<ulong, ulong>(enumType, *(ulong*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(byte))
			{
				return TryFormatPrimitiveDefault<byte, byte>(enumType, *(byte*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(sbyte))
			{
				return TryFormatPrimitiveDefault<sbyte, byte>(enumType, *(sbyte*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(short))
			{
				return TryFormatPrimitiveDefault<short, ushort>(enumType, *(short*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(ushort))
			{
				return TryFormatPrimitiveDefault<ushort, ushort>(enumType, *(ushort*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(nint))
			{
				return TryFormatPrimitiveDefault<nint, nuint>(enumType, *(nint*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(nuint))
			{
				return TryFormatPrimitiveDefault<nuint, nuint>(enumType, *(nuint*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(float))
			{
				return TryFormatPrimitiveDefault<float, float>(enumType, *(float*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(double))
			{
				return TryFormatPrimitiveDefault<double, double>(enumType, *(double*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(char))
			{
				return TryFormatPrimitiveDefault<char, char>(enumType, *(char*)(&value), destination, out charsWritten);
			}
		}
		else
		{
			if (enumUnderlyingType == typeof(int))
			{
				return TryFormatPrimitiveNonDefault<int, uint>(enumType, *(int*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(uint))
			{
				return TryFormatPrimitiveNonDefault<uint, uint>(enumType, *(uint*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(long))
			{
				return TryFormatPrimitiveNonDefault<long, ulong>(enumType, *(long*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(ulong))
			{
				return TryFormatPrimitiveNonDefault<ulong, ulong>(enumType, *(ulong*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(byte))
			{
				return TryFormatPrimitiveNonDefault<byte, byte>(enumType, *(byte*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(sbyte))
			{
				return TryFormatPrimitiveNonDefault<sbyte, byte>(enumType, *(sbyte*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(short))
			{
				return TryFormatPrimitiveNonDefault<short, ushort>(enumType, *(short*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(ushort))
			{
				return TryFormatPrimitiveNonDefault<ushort, ushort>(enumType, *(ushort*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(nint))
			{
				return TryFormatPrimitiveNonDefault<nint, nuint>(enumType, *(nint*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(nuint))
			{
				return TryFormatPrimitiveNonDefault<nuint, nuint>(enumType, *(nuint*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(float))
			{
				return TryFormatPrimitiveNonDefault<float, float>(enumType, *(float*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(double))
			{
				return TryFormatPrimitiveNonDefault<double, double>(enumType, *(double*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(char))
			{
				return TryFormatPrimitiveNonDefault<char, char>(enumType, *(char*)(&value), destination, out charsWritten, format);
			}
		}
		throw CreateUnknownEnumTypeException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static bool TryFormatUnconstrained<TEnum>(TEnum value, Span<char> destination, out int charsWritten, [StringSyntax("EnumFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>))
	{
		RuntimeType enumType = (RuntimeType)typeof(TEnum);
		Type enumUnderlyingType = typeof(TEnum).GetEnumUnderlyingType();
		if (format.IsEmpty)
		{
			if (enumUnderlyingType == typeof(int))
			{
				return TryFormatPrimitiveDefault<int, uint>(enumType, *(int*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(uint))
			{
				return TryFormatPrimitiveDefault<uint, uint>(enumType, *(uint*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(long))
			{
				return TryFormatPrimitiveDefault<long, ulong>(enumType, *(long*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(ulong))
			{
				return TryFormatPrimitiveDefault<ulong, ulong>(enumType, *(ulong*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(byte))
			{
				return TryFormatPrimitiveDefault<byte, byte>(enumType, *(byte*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(sbyte))
			{
				return TryFormatPrimitiveDefault<sbyte, byte>(enumType, *(sbyte*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(short))
			{
				return TryFormatPrimitiveDefault<short, ushort>(enumType, *(short*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(ushort))
			{
				return TryFormatPrimitiveDefault<ushort, ushort>(enumType, *(ushort*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(nint))
			{
				return TryFormatPrimitiveDefault<nint, nuint>(enumType, *(nint*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(nuint))
			{
				return TryFormatPrimitiveDefault<nuint, nuint>(enumType, *(nuint*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(float))
			{
				return TryFormatPrimitiveDefault<float, float>(enumType, *(float*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(double))
			{
				return TryFormatPrimitiveDefault<double, double>(enumType, *(double*)(&value), destination, out charsWritten);
			}
			if (enumUnderlyingType == typeof(char))
			{
				return TryFormatPrimitiveDefault<char, char>(enumType, *(char*)(&value), destination, out charsWritten);
			}
		}
		else
		{
			if (enumUnderlyingType == typeof(int))
			{
				return TryFormatPrimitiveNonDefault<int, uint>(enumType, *(int*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(uint))
			{
				return TryFormatPrimitiveNonDefault<uint, uint>(enumType, *(uint*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(long))
			{
				return TryFormatPrimitiveNonDefault<long, ulong>(enumType, *(long*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(ulong))
			{
				return TryFormatPrimitiveNonDefault<ulong, ulong>(enumType, *(ulong*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(byte))
			{
				return TryFormatPrimitiveNonDefault<byte, byte>(enumType, *(byte*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(sbyte))
			{
				return TryFormatPrimitiveNonDefault<sbyte, byte>(enumType, *(sbyte*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(short))
			{
				return TryFormatPrimitiveNonDefault<short, ushort>(enumType, *(short*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(ushort))
			{
				return TryFormatPrimitiveNonDefault<ushort, ushort>(enumType, *(ushort*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(nint))
			{
				return TryFormatPrimitiveNonDefault<nint, nuint>(enumType, *(nint*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(nuint))
			{
				return TryFormatPrimitiveNonDefault<nuint, nuint>(enumType, *(nuint*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(float))
			{
				return TryFormatPrimitiveNonDefault<float, float>(enumType, *(float*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(double))
			{
				return TryFormatPrimitiveNonDefault<double, double>(enumType, *(double*)(&value), destination, out charsWritten, format);
			}
			if (enumUnderlyingType == typeof(char))
			{
				return TryFormatPrimitiveNonDefault<char, char>(enumType, *(char*)(&value), destination, out charsWritten, format);
			}
		}
		throw CreateUnknownEnumTypeException();
	}

	private static bool TryFormatPrimitiveDefault<TUnderlying, TStorage>(RuntimeType enumType, TUnderlying value, Span<char> destination, out int charsWritten) where TUnderlying : struct, INumber<TUnderlying>, IBitwiseOperators<TUnderlying, TUnderlying, TUnderlying>, IMinMaxValue<TUnderlying> where TStorage : struct, INumber<TStorage>, IBitwiseOperators<TStorage, TStorage, TStorage>, IMinMaxValue<TStorage>
	{
		EnumInfo<TStorage> enumInfo = GetEnumInfo<TStorage>(enumType);
		if (!enumInfo.HasFlagsAttribute)
		{
			string nameInlined = GetNameInlined(enumInfo, Unsafe.BitCast<TUnderlying, TStorage>(value));
			if (nameInlined != null)
			{
				if (nameInlined.TryCopyTo(destination))
				{
					charsWritten = nameInlined.Length;
					return true;
				}
				charsWritten = 0;
				return false;
			}
		}
		else
		{
			bool isDestinationTooSmall = false;
			if (TryFormatFlagNames(enumInfo, Unsafe.BitCast<TUnderlying, TStorage>(value), destination, out charsWritten, ref isDestinationTooSmall) || isDestinationTooSmall)
			{
				return !isDestinationTooSmall;
			}
		}
		return value.TryFormat(destination, out charsWritten, default(ReadOnlySpan<char>), null);
	}

	private static bool TryFormatPrimitiveNonDefault<TUnderlying, TStorage>(RuntimeType enumType, TUnderlying value, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format) where TUnderlying : struct, INumber<TUnderlying>, IBitwiseOperators<TUnderlying, TUnderlying, TUnderlying>, IMinMaxValue<TUnderlying> where TStorage : struct, INumber<TStorage>, IBitwiseOperators<TStorage, TStorage, TStorage>, IMinMaxValue<TStorage>
	{
		if (format.Length == 1)
		{
			switch (format[0] | 0x20)
			{
			case 103:
				return TryFormatPrimitiveDefault<TUnderlying, TStorage>(enumType, value, destination, out charsWritten);
			case 100:
				return value.TryFormat(destination, out charsWritten, default(ReadOnlySpan<char>), null);
			case 120:
				return TryFormatNumberAsHex<TStorage>(ref Unsafe.As<TUnderlying, byte>(ref value), destination, out charsWritten);
			case 102:
			{
				bool isDestinationTooSmall = false;
				if (TryFormatFlagNames(GetEnumInfo<TStorage>(enumType), Unsafe.BitCast<TUnderlying, TStorage>(value), destination, out charsWritten, ref isDestinationTooSmall) || isDestinationTooSmall)
				{
					return !isDestinationTooSmall;
				}
				goto case 100;
			}
			}
		}
		throw CreateInvalidFormatSpecifierException();
	}

	private static string FormatFlagNames<TStorage>(EnumInfo<TStorage> enumInfo, TStorage resultValue) where TStorage : struct, INumber<TStorage>, IBitwiseOperators<TStorage, TStorage, TStorage>
	{
		string[] names = enumInfo.Names;
		TStorage[] values = enumInfo.Values;
		int index;
		string text = GetSingleFlagsEnumNameForValue(resultValue, names, values, out index);
		if (text == null)
		{
			Span<int> span = stackalloc int[64];
			if (TryFindFlagsNames(resultValue, names, values, index, span, out var resultLength, out var foundItemsCount))
			{
				span = span.Slice(0, foundItemsCount);
				int multipleEnumsFlagsFormatResultLength = GetMultipleEnumsFlagsFormatResultLength(resultLength, foundItemsCount);
				text = string.FastAllocateString(multipleEnumsFlagsFormatResultLength);
				WriteMultipleFoundFlagsNames(names, span, new Span<char>(ref text.GetRawStringData(), text.Length));
			}
		}
		return text;
	}

	private static bool TryFormatFlagNames<TStorage>(EnumInfo<TStorage> enumInfo, TStorage resultValue, Span<char> destination, out int charsWritten, ref bool isDestinationTooSmall) where TStorage : struct, INumber<TStorage>, IBitwiseOperators<TStorage, TStorage, TStorage>
	{
		string[] names = enumInfo.Names;
		TStorage[] values = enumInfo.Values;
		int index;
		string singleFlagsEnumNameForValue = GetSingleFlagsEnumNameForValue(resultValue, names, values, out index);
		if (singleFlagsEnumNameForValue != null)
		{
			if (singleFlagsEnumNameForValue.TryCopyTo(destination))
			{
				charsWritten = singleFlagsEnumNameForValue.Length;
				return true;
			}
			isDestinationTooSmall = true;
		}
		else
		{
			Span<int> span = stackalloc int[64];
			if (TryFindFlagsNames(resultValue, names, values, index, span, out var resultLength, out var foundItemsCount))
			{
				span = span.Slice(0, foundItemsCount);
				int multipleEnumsFlagsFormatResultLength = GetMultipleEnumsFlagsFormatResultLength(resultLength, foundItemsCount);
				if (multipleEnumsFlagsFormatResultLength <= destination.Length)
				{
					charsWritten = multipleEnumsFlagsFormatResultLength;
					WriteMultipleFoundFlagsNames(names, span, destination);
					return true;
				}
				isDestinationTooSmall = true;
			}
		}
		charsWritten = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int GetMultipleEnumsFlagsFormatResultLength(int resultLength, int foundItemsCount)
	{
		int num = 2 * (foundItemsCount - 1);
		return checked(resultLength + num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string GetSingleFlagsEnumNameForValue<TStorage>(TStorage resultValue, string[] names, TStorage[] values, out int index) where TStorage : struct, INumber<TStorage>
	{
		if (resultValue == TStorage.Zero)
		{
			index = 0;
			if (values.Length == 0 || !(values[0] == TStorage.Zero))
			{
				return "0";
			}
			return names[0];
		}
		int num = values.Length - 1;
		while ((uint)num < (uint)values.Length)
		{
			if (values[num] <= resultValue)
			{
				if (!(values[num] == resultValue))
				{
					break;
				}
				index = num;
				return names[num];
			}
			num--;
		}
		index = num;
		return null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryFindFlagsNames<TStorage>(TStorage resultValue, string[] names, TStorage[] values, int index, Span<int> foundItems, out int resultLength, out int foundItemsCount) where TStorage : struct, INumber<TStorage>, IBitwiseOperators<TStorage, TStorage, TStorage>
	{
		resultLength = 0;
		foundItemsCount = 0;
		while ((uint)index < (uint)values.Length)
		{
			TStorage val = values[index];
			if (index == 0 && val == TStorage.Zero)
			{
				break;
			}
			if ((resultValue & val) == val)
			{
				resultValue &= ~val;
				foundItems[foundItemsCount] = index;
				foundItemsCount++;
				checked
				{
					resultLength += names[index].Length;
					if (resultValue == TStorage.Zero)
					{
						break;
					}
				}
			}
			index--;
		}
		return resultValue == TStorage.Zero;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void WriteMultipleFoundFlagsNames(string[] names, ReadOnlySpan<int> foundItems, Span<char> destination)
	{
		for (int num = foundItems.Length - 1; num != 0; num--)
		{
			string text = names[foundItems[num]];
			text.CopyTo(destination);
			destination = destination.Slice(text.Length);
			Span<char> span = destination.Slice(2);
			destination[0] = ',';
			destination[1] = ' ';
			destination = span;
		}
		names[foundItems[0]].CopyTo(destination);
	}

	private static RuntimeType ValidateRuntimeType(Type enumType)
	{
		ArgumentNullException.ThrowIfNull(enumType, "enumType");
		RuntimeType runtimeType = enumType as RuntimeType;
		if ((object)runtimeType == null || !runtimeType.IsActualEnum)
		{
			ThrowInvalidRuntimeType(enumType);
		}
		return runtimeType;
	}

	[DoesNotReturn]
	private static void ThrowInvalidRuntimeType(Type enumType)
	{
		throw new ArgumentException((!(enumType is RuntimeType)) ? SR.Arg_MustBeType : SR.Arg_MustBeEnum, "enumType");
	}

	private static void ThrowInvalidEmptyParseArgument()
	{
		throw new ArgumentException(SR.Arg_MustContainEnumInfo, "value");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static FormatException CreateInvalidFormatSpecifierException()
	{
		return new FormatException(SR.Format_InvalidEnumFormatSpecification);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static InvalidOperationException CreateUnknownEnumTypeException()
	{
		return new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
	}

	public TypeCode GetTypeCode()
	{
		return InternalGetCorElementType() switch
		{
			CorElementType.ELEMENT_TYPE_I1 => TypeCode.SByte, 
			CorElementType.ELEMENT_TYPE_U1 => TypeCode.Byte, 
			CorElementType.ELEMENT_TYPE_I2 => TypeCode.Int16, 
			CorElementType.ELEMENT_TYPE_U2 => TypeCode.UInt16, 
			CorElementType.ELEMENT_TYPE_I4 => TypeCode.Int32, 
			CorElementType.ELEMENT_TYPE_U4 => TypeCode.UInt32, 
			CorElementType.ELEMENT_TYPE_I8 => TypeCode.Int64, 
			CorElementType.ELEMENT_TYPE_U8 => TypeCode.UInt64, 
			CorElementType.ELEMENT_TYPE_CHAR => TypeCode.Char, 
			_ => throw CreateUnknownEnumTypeException(), 
		};
	}

	bool IConvertible.ToBoolean(IFormatProvider provider)
	{
		return Convert.ToBoolean(GetValue());
	}

	char IConvertible.ToChar(IFormatProvider provider)
	{
		return Convert.ToChar(GetValue());
	}

	sbyte IConvertible.ToSByte(IFormatProvider provider)
	{
		return Convert.ToSByte(GetValue());
	}

	byte IConvertible.ToByte(IFormatProvider provider)
	{
		return Convert.ToByte(GetValue());
	}

	short IConvertible.ToInt16(IFormatProvider provider)
	{
		return Convert.ToInt16(GetValue());
	}

	ushort IConvertible.ToUInt16(IFormatProvider provider)
	{
		return Convert.ToUInt16(GetValue());
	}

	int IConvertible.ToInt32(IFormatProvider provider)
	{
		return Convert.ToInt32(GetValue());
	}

	uint IConvertible.ToUInt32(IFormatProvider provider)
	{
		return Convert.ToUInt32(GetValue());
	}

	long IConvertible.ToInt64(IFormatProvider provider)
	{
		return Convert.ToInt64(GetValue());
	}

	ulong IConvertible.ToUInt64(IFormatProvider provider)
	{
		return Convert.ToUInt64(GetValue());
	}

	float IConvertible.ToSingle(IFormatProvider provider)
	{
		return Convert.ToSingle(GetValue());
	}

	double IConvertible.ToDouble(IFormatProvider provider)
	{
		return Convert.ToDouble(GetValue());
	}

	decimal IConvertible.ToDecimal(IFormatProvider provider)
	{
		return Convert.ToDecimal(GetValue());
	}

	DateTime IConvertible.ToDateTime(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Enum", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	public static object ToObject(Type enumType, object value)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		switch (Convert.GetTypeCode(value))
		{
		case TypeCode.Int32:
			return ToObject(enumType, (int)value);
		case TypeCode.SByte:
			return ToObject(enumType, (sbyte)value);
		case TypeCode.Int16:
			return ToObject(enumType, (short)value);
		case TypeCode.Int64:
			return ToObject(enumType, (long)value);
		case TypeCode.UInt32:
			return ToObject(enumType, (uint)value);
		case TypeCode.Byte:
			return ToObject(enumType, (byte)value);
		case TypeCode.UInt16:
			return ToObject(enumType, (ushort)value);
		case TypeCode.UInt64:
			return ToObject(enumType, (ulong)value);
		case TypeCode.Single:
			return ToObject(enumType, BitConverter.SingleToInt32Bits((float)value));
		case TypeCode.Double:
			return ToObject(enumType, BitConverter.DoubleToInt64Bits((double)value));
		case TypeCode.Char:
			return ToObject(enumType, (char)value);
		case TypeCode.Boolean:
			return ToObject(enumType, (long)(((bool)value) ? 1 : 0));
		default:
		{
			Type type = value.GetType();
			if (type.IsEnum)
			{
				type = type.GetEnumUnderlyingType();
			}
			if (type == typeof(nint))
			{
				ToObject(enumType, (nint)value);
			}
			if (type == typeof(nuint))
			{
				ToObject(enumType, (nuint)value);
			}
			throw new ArgumentException(SR.Arg_MustBeEnumBaseTypeOrEnum, "value");
		}
		}
	}

	[CLSCompliant(false)]
	public static object ToObject(Type enumType, sbyte value)
	{
		return InternalBoxEnum(ValidateRuntimeType(enumType), value);
	}

	public static object ToObject(Type enumType, short value)
	{
		return InternalBoxEnum(ValidateRuntimeType(enumType), value);
	}

	public static object ToObject(Type enumType, int value)
	{
		return InternalBoxEnum(ValidateRuntimeType(enumType), value);
	}

	public static object ToObject(Type enumType, byte value)
	{
		return InternalBoxEnum(ValidateRuntimeType(enumType), value);
	}

	[CLSCompliant(false)]
	public static object ToObject(Type enumType, ushort value)
	{
		return InternalBoxEnum(ValidateRuntimeType(enumType), value);
	}

	[CLSCompliant(false)]
	public static object ToObject(Type enumType, uint value)
	{
		return InternalBoxEnum(ValidateRuntimeType(enumType), value);
	}

	public static object ToObject(Type enumType, long value)
	{
		return InternalBoxEnum(ValidateRuntimeType(enumType), value);
	}

	[CLSCompliant(false)]
	public static object ToObject(Type enumType, ulong value)
	{
		return InternalBoxEnum(ValidateRuntimeType(enumType), (long)value);
	}

	internal static bool AreSequentialFromZero<TStorage>(TStorage[] values) where TStorage : struct, INumber<TStorage>
	{
		for (int i = 0; i < values.Length; i++)
		{
			if (ulong.CreateTruncating(values[i]) != (ulong)i)
			{
				return false;
			}
		}
		return true;
	}

	internal static bool AreSorted<TStorage>(TStorage[] values) where TStorage : struct, IComparable<TStorage>
	{
		for (int i = 1; i < values.Length; i++)
		{
			if (values[i - 1].CompareTo(values[i]) > 0)
			{
				return false;
			}
		}
		return true;
	}

	[Conditional("DEBUG")]
	private static void AssertValidGenerics<TUnderlying, TStorage>()
	{
		if (!(typeof(TUnderlying) == typeof(sbyte)) && !(typeof(TUnderlying) == typeof(short)) && !(typeof(TUnderlying) == typeof(int)) && !(typeof(TUnderlying) == typeof(long)))
		{
			_ = typeof(TUnderlying) == typeof(nint);
		}
	}
}
