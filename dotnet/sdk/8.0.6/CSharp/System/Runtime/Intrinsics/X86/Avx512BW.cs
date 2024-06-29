using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86;

[Intrinsic]
[CLSCompliant(false)]
public abstract class Avx512BW : Avx512F
{
	[Intrinsic]
	public new abstract class VL : Avx512F.VL
	{
		public new static bool IsSupported => IsSupported;

		public static Vector128<byte> CompareGreaterThan(Vector128<byte> left, Vector128<byte> right)
		{
			return CompareGreaterThan(left, right);
		}

		public static Vector128<byte> CompareGreaterThanOrEqual(Vector128<byte> left, Vector128<byte> right)
		{
			return CompareGreaterThanOrEqual(left, right);
		}

		public static Vector128<byte> CompareLessThan(Vector128<byte> left, Vector128<byte> right)
		{
			return CompareLessThan(left, right);
		}

		public static Vector128<byte> CompareLessThanOrEqual(Vector128<byte> left, Vector128<byte> right)
		{
			return CompareLessThanOrEqual(left, right);
		}

		public static Vector128<byte> CompareNotEqual(Vector128<byte> left, Vector128<byte> right)
		{
			return CompareNotEqual(left, right);
		}

		public static Vector256<byte> CompareGreaterThan(Vector256<byte> left, Vector256<byte> right)
		{
			return CompareGreaterThan(left, right);
		}

		public static Vector256<byte> CompareGreaterThanOrEqual(Vector256<byte> left, Vector256<byte> right)
		{
			return CompareGreaterThanOrEqual(left, right);
		}

		public static Vector256<byte> CompareLessThan(Vector256<byte> left, Vector256<byte> right)
		{
			return CompareLessThan(left, right);
		}

		public static Vector256<byte> CompareLessThanOrEqual(Vector256<byte> left, Vector256<byte> right)
		{
			return CompareLessThanOrEqual(left, right);
		}

		public static Vector256<byte> CompareNotEqual(Vector256<byte> left, Vector256<byte> right)
		{
			return CompareNotEqual(left, right);
		}

		public static Vector128<short> CompareGreaterThanOrEqual(Vector128<short> left, Vector128<short> right)
		{
			return CompareGreaterThanOrEqual(left, right);
		}

		public static Vector128<short> CompareLessThan(Vector128<short> left, Vector128<short> right)
		{
			return CompareLessThan(left, right);
		}

		public static Vector128<short> CompareLessThanOrEqual(Vector128<short> left, Vector128<short> right)
		{
			return CompareLessThanOrEqual(left, right);
		}

		public static Vector128<short> CompareNotEqual(Vector128<short> left, Vector128<short> right)
		{
			return CompareNotEqual(left, right);
		}

		public static Vector256<short> CompareGreaterThanOrEqual(Vector256<short> left, Vector256<short> right)
		{
			return CompareGreaterThanOrEqual(left, right);
		}

		public static Vector256<short> CompareLessThan(Vector256<short> left, Vector256<short> right)
		{
			return CompareLessThan(left, right);
		}

		public static Vector256<short> CompareLessThanOrEqual(Vector256<short> left, Vector256<short> right)
		{
			return CompareLessThanOrEqual(left, right);
		}

		public static Vector256<short> CompareNotEqual(Vector256<short> left, Vector256<short> right)
		{
			return CompareNotEqual(left, right);
		}

		public static Vector128<sbyte> CompareGreaterThanOrEqual(Vector128<sbyte> left, Vector128<sbyte> right)
		{
			return CompareGreaterThanOrEqual(left, right);
		}

		public static Vector128<sbyte> CompareLessThan(Vector128<sbyte> left, Vector128<sbyte> right)
		{
			return CompareLessThan(left, right);
		}

		public static Vector128<sbyte> CompareLessThanOrEqual(Vector128<sbyte> left, Vector128<sbyte> right)
		{
			return CompareLessThanOrEqual(left, right);
		}

		public static Vector128<sbyte> CompareNotEqual(Vector128<sbyte> left, Vector128<sbyte> right)
		{
			return CompareNotEqual(left, right);
		}

		public static Vector256<sbyte> CompareGreaterThanOrEqual(Vector256<sbyte> left, Vector256<sbyte> right)
		{
			return CompareGreaterThanOrEqual(left, right);
		}

		public static Vector256<sbyte> CompareLessThan(Vector256<sbyte> left, Vector256<sbyte> right)
		{
			return CompareLessThan(left, right);
		}

		public static Vector256<sbyte> CompareLessThanOrEqual(Vector256<sbyte> left, Vector256<sbyte> right)
		{
			return CompareLessThanOrEqual(left, right);
		}

		public static Vector256<sbyte> CompareNotEqual(Vector256<sbyte> left, Vector256<sbyte> right)
		{
			return CompareNotEqual(left, right);
		}

		public static Vector128<ushort> CompareGreaterThan(Vector128<ushort> left, Vector128<ushort> right)
		{
			return CompareGreaterThan(left, right);
		}

		public static Vector128<ushort> CompareGreaterThanOrEqual(Vector128<ushort> left, Vector128<ushort> right)
		{
			return CompareGreaterThanOrEqual(left, right);
		}

		public static Vector128<ushort> CompareLessThan(Vector128<ushort> left, Vector128<ushort> right)
		{
			return CompareLessThan(left, right);
		}

		public static Vector128<ushort> CompareLessThanOrEqual(Vector128<ushort> left, Vector128<ushort> right)
		{
			return CompareLessThanOrEqual(left, right);
		}

		public static Vector128<ushort> CompareNotEqual(Vector128<ushort> left, Vector128<ushort> right)
		{
			return CompareNotEqual(left, right);
		}

		public static Vector256<ushort> CompareGreaterThan(Vector256<ushort> left, Vector256<ushort> right)
		{
			return CompareGreaterThan(left, right);
		}

		public static Vector256<ushort> CompareGreaterThanOrEqual(Vector256<ushort> left, Vector256<ushort> right)
		{
			return CompareGreaterThanOrEqual(left, right);
		}

		public static Vector256<ushort> CompareLessThan(Vector256<ushort> left, Vector256<ushort> right)
		{
			return CompareLessThan(left, right);
		}

		public static Vector256<ushort> CompareLessThanOrEqual(Vector256<ushort> left, Vector256<ushort> right)
		{
			return CompareLessThanOrEqual(left, right);
		}

		public static Vector256<ushort> CompareNotEqual(Vector256<ushort> left, Vector256<ushort> right)
		{
			return CompareNotEqual(left, right);
		}

		public static Vector128<byte> ConvertToVector128Byte(Vector128<short> value)
		{
			return ConvertToVector128Byte(value);
		}

		public static Vector128<byte> ConvertToVector128Byte(Vector128<ushort> value)
		{
			return ConvertToVector128Byte(value);
		}

		public static Vector128<byte> ConvertToVector128Byte(Vector256<short> value)
		{
			return ConvertToVector128Byte(value);
		}

		public static Vector128<byte> ConvertToVector128Byte(Vector256<ushort> value)
		{
			return ConvertToVector128Byte(value);
		}

		public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector128<ushort> value)
		{
			return ConvertToVector128ByteWithSaturation(value);
		}

		public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector256<ushort> value)
		{
			return ConvertToVector128ByteWithSaturation(value);
		}

		public static Vector128<sbyte> ConvertToVector128SByte(Vector128<short> value)
		{
			return ConvertToVector128SByte(value);
		}

		public static Vector128<sbyte> ConvertToVector128SByte(Vector128<ushort> value)
		{
			return ConvertToVector128SByte(value);
		}

		public static Vector128<sbyte> ConvertToVector128SByte(Vector256<short> value)
		{
			return ConvertToVector128SByte(value);
		}

		public static Vector128<sbyte> ConvertToVector128SByte(Vector256<ushort> value)
		{
			return ConvertToVector128SByte(value);
		}

		public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector128<short> value)
		{
			return ConvertToVector128SByteWithSaturation(value);
		}

		public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector256<short> value)
		{
			return ConvertToVector128SByteWithSaturation(value);
		}

		public static Vector128<short> PermuteVar8x16(Vector128<short> left, Vector128<short> control)
		{
			return PermuteVar8x16(left, control);
		}

		public static Vector128<ushort> PermuteVar8x16(Vector128<ushort> left, Vector128<ushort> control)
		{
			return PermuteVar8x16(left, control);
		}

		public static Vector128<short> PermuteVar8x16x2(Vector128<short> lower, Vector128<short> indices, Vector128<short> upper)
		{
			return PermuteVar8x16x2(lower, indices, upper);
		}

		public static Vector128<ushort> PermuteVar8x16x2(Vector128<ushort> lower, Vector128<ushort> indices, Vector128<ushort> upper)
		{
			return PermuteVar8x16x2(lower, indices, upper);
		}

		public static Vector256<short> PermuteVar16x16(Vector256<short> left, Vector256<short> control)
		{
			return PermuteVar16x16(left, control);
		}

		public static Vector256<ushort> PermuteVar16x16(Vector256<ushort> left, Vector256<ushort> control)
		{
			return PermuteVar16x16(left, control);
		}

		public static Vector256<short> PermuteVar16x16x2(Vector256<short> lower, Vector256<short> indices, Vector256<short> upper)
		{
			return PermuteVar16x16x2(lower, indices, upper);
		}

		public static Vector256<ushort> PermuteVar16x16x2(Vector256<ushort> lower, Vector256<ushort> indices, Vector256<ushort> upper)
		{
			return PermuteVar16x16x2(lower, indices, upper);
		}

		public static Vector128<short> ShiftLeftLogicalVariable(Vector128<short> value, Vector128<ushort> count)
		{
			return ShiftLeftLogicalVariable(value, count);
		}

		public static Vector128<ushort> ShiftLeftLogicalVariable(Vector128<ushort> value, Vector128<ushort> count)
		{
			return ShiftLeftLogicalVariable(value, count);
		}

		public static Vector256<short> ShiftLeftLogicalVariable(Vector256<short> value, Vector256<ushort> count)
		{
			return ShiftLeftLogicalVariable(value, count);
		}

		public static Vector256<ushort> ShiftLeftLogicalVariable(Vector256<ushort> value, Vector256<ushort> count)
		{
			return ShiftLeftLogicalVariable(value, count);
		}

		public static Vector128<short> ShiftRightArithmeticVariable(Vector128<short> value, Vector128<ushort> count)
		{
			return ShiftRightArithmeticVariable(value, count);
		}

		public static Vector256<short> ShiftRightArithmeticVariable(Vector256<short> value, Vector256<ushort> count)
		{
			return ShiftRightArithmeticVariable(value, count);
		}

		public static Vector128<short> ShiftRightLogicalVariable(Vector128<short> value, Vector128<ushort> count)
		{
			return ShiftRightLogicalVariable(value, count);
		}

		public static Vector128<ushort> ShiftRightLogicalVariable(Vector128<ushort> value, Vector128<ushort> count)
		{
			return ShiftRightLogicalVariable(value, count);
		}

		public static Vector256<short> ShiftRightLogicalVariable(Vector256<short> value, Vector256<ushort> count)
		{
			return ShiftRightLogicalVariable(value, count);
		}

		public static Vector256<ushort> ShiftRightLogicalVariable(Vector256<ushort> value, Vector256<ushort> count)
		{
			return ShiftRightLogicalVariable(value, count);
		}

		public static Vector128<ushort> SumAbsoluteDifferencesInBlock32(Vector128<byte> left, Vector128<byte> right, [ConstantExpected] byte control)
		{
			return SumAbsoluteDifferencesInBlock32(left, right, control);
		}

		public static Vector256<ushort> SumAbsoluteDifferencesInBlock32(Vector256<byte> left, Vector256<byte> right, [ConstantExpected] byte control)
		{
			return SumAbsoluteDifferencesInBlock32(left, right, control);
		}
	}

	[Intrinsic]
	public new abstract class X64 : Avx512F.X64
	{
		public new static bool IsSupported => IsSupported;
	}

	public new static bool IsSupported => IsSupported;

	public static Vector512<byte> Abs(Vector512<sbyte> value)
	{
		return Abs(value);
	}

	public static Vector512<ushort> Abs(Vector512<short> value)
	{
		return Abs(value);
	}

	public static Vector512<sbyte> Add(Vector512<sbyte> left, Vector512<sbyte> right)
	{
		return Add(left, right);
	}

	public static Vector512<byte> Add(Vector512<byte> left, Vector512<byte> right)
	{
		return Add(left, right);
	}

	public static Vector512<short> Add(Vector512<short> left, Vector512<short> right)
	{
		return Add(left, right);
	}

	public static Vector512<ushort> Add(Vector512<ushort> left, Vector512<ushort> right)
	{
		return Add(left, right);
	}

	public static Vector512<sbyte> AddSaturate(Vector512<sbyte> left, Vector512<sbyte> right)
	{
		return AddSaturate(left, right);
	}

	public static Vector512<byte> AddSaturate(Vector512<byte> left, Vector512<byte> right)
	{
		return AddSaturate(left, right);
	}

	public static Vector512<short> AddSaturate(Vector512<short> left, Vector512<short> right)
	{
		return AddSaturate(left, right);
	}

	public static Vector512<ushort> AddSaturate(Vector512<ushort> left, Vector512<ushort> right)
	{
		return AddSaturate(left, right);
	}

	public static Vector512<sbyte> AlignRight(Vector512<sbyte> left, Vector512<sbyte> right, [ConstantExpected] byte mask)
	{
		return AlignRight(left, right, mask);
	}

	public static Vector512<byte> AlignRight(Vector512<byte> left, Vector512<byte> right, [ConstantExpected] byte mask)
	{
		return AlignRight(left, right, mask);
	}

	public static Vector512<byte> Average(Vector512<byte> left, Vector512<byte> right)
	{
		return Average(left, right);
	}

	public static Vector512<ushort> Average(Vector512<ushort> left, Vector512<ushort> right)
	{
		return Average(left, right);
	}

	public static Vector512<byte> BlendVariable(Vector512<byte> left, Vector512<byte> right, Vector512<byte> mask)
	{
		return BlendVariable(left, right, mask);
	}

	public static Vector512<short> BlendVariable(Vector512<short> left, Vector512<short> right, Vector512<short> mask)
	{
		return BlendVariable(left, right, mask);
	}

	public static Vector512<sbyte> BlendVariable(Vector512<sbyte> left, Vector512<sbyte> right, Vector512<sbyte> mask)
	{
		return BlendVariable(left, right, mask);
	}

	public static Vector512<ushort> BlendVariable(Vector512<ushort> left, Vector512<ushort> right, Vector512<ushort> mask)
	{
		return BlendVariable(left, right, mask);
	}

	public static Vector512<byte> BroadcastScalarToVector512(Vector128<byte> value)
	{
		return BroadcastScalarToVector512(value);
	}

	public static Vector512<sbyte> BroadcastScalarToVector512(Vector128<sbyte> value)
	{
		return BroadcastScalarToVector512(value);
	}

	public static Vector512<short> BroadcastScalarToVector512(Vector128<short> value)
	{
		return BroadcastScalarToVector512(value);
	}

	public static Vector512<ushort> BroadcastScalarToVector512(Vector128<ushort> value)
	{
		return BroadcastScalarToVector512(value);
	}

	public static Vector512<byte> CompareEqual(Vector512<byte> left, Vector512<byte> right)
	{
		return CompareEqual(left, right);
	}

	public static Vector512<byte> CompareGreaterThan(Vector512<byte> left, Vector512<byte> right)
	{
		return CompareGreaterThan(left, right);
	}

	public static Vector512<byte> CompareGreaterThanOrEqual(Vector512<byte> left, Vector512<byte> right)
	{
		return CompareGreaterThanOrEqual(left, right);
	}

	public static Vector512<byte> CompareLessThan(Vector512<byte> left, Vector512<byte> right)
	{
		return CompareLessThan(left, right);
	}

	public static Vector512<byte> CompareLessThanOrEqual(Vector512<byte> left, Vector512<byte> right)
	{
		return CompareLessThanOrEqual(left, right);
	}

	public static Vector512<byte> CompareNotEqual(Vector512<byte> left, Vector512<byte> right)
	{
		return CompareNotEqual(left, right);
	}

	public static Vector512<short> CompareEqual(Vector512<short> left, Vector512<short> right)
	{
		return CompareEqual(left, right);
	}

	public static Vector512<short> CompareGreaterThan(Vector512<short> left, Vector512<short> right)
	{
		return CompareGreaterThan(left, right);
	}

	public static Vector512<short> CompareGreaterThanOrEqual(Vector512<short> left, Vector512<short> right)
	{
		return CompareGreaterThanOrEqual(left, right);
	}

	public static Vector512<short> CompareLessThan(Vector512<short> left, Vector512<short> right)
	{
		return CompareLessThan(left, right);
	}

	public static Vector512<short> CompareLessThanOrEqual(Vector512<short> left, Vector512<short> right)
	{
		return CompareLessThanOrEqual(left, right);
	}

	public static Vector512<short> CompareNotEqual(Vector512<short> left, Vector512<short> right)
	{
		return CompareNotEqual(left, right);
	}

	public static Vector512<sbyte> CompareEqual(Vector512<sbyte> left, Vector512<sbyte> right)
	{
		return CompareEqual(left, right);
	}

	public static Vector512<sbyte> CompareGreaterThan(Vector512<sbyte> left, Vector512<sbyte> right)
	{
		return CompareGreaterThan(left, right);
	}

	public static Vector512<sbyte> CompareGreaterThanOrEqual(Vector512<sbyte> left, Vector512<sbyte> right)
	{
		return CompareGreaterThanOrEqual(left, right);
	}

	public static Vector512<sbyte> CompareLessThan(Vector512<sbyte> left, Vector512<sbyte> right)
	{
		return CompareLessThan(left, right);
	}

	public static Vector512<sbyte> CompareLessThanOrEqual(Vector512<sbyte> left, Vector512<sbyte> right)
	{
		return CompareLessThanOrEqual(left, right);
	}

	public static Vector512<sbyte> CompareNotEqual(Vector512<sbyte> left, Vector512<sbyte> right)
	{
		return CompareNotEqual(left, right);
	}

	public static Vector512<ushort> CompareEqual(Vector512<ushort> left, Vector512<ushort> right)
	{
		return CompareEqual(left, right);
	}

	public static Vector512<ushort> CompareGreaterThan(Vector512<ushort> left, Vector512<ushort> right)
	{
		return CompareGreaterThan(left, right);
	}

	public static Vector512<ushort> CompareGreaterThanOrEqual(Vector512<ushort> left, Vector512<ushort> right)
	{
		return CompareGreaterThanOrEqual(left, right);
	}

	public static Vector512<ushort> CompareLessThan(Vector512<ushort> left, Vector512<ushort> right)
	{
		return CompareLessThan(left, right);
	}

	public static Vector512<ushort> CompareLessThanOrEqual(Vector512<ushort> left, Vector512<ushort> right)
	{
		return CompareLessThanOrEqual(left, right);
	}

	public static Vector512<ushort> CompareNotEqual(Vector512<ushort> left, Vector512<ushort> right)
	{
		return CompareNotEqual(left, right);
	}

	public static Vector256<byte> ConvertToVector256Byte(Vector512<short> value)
	{
		return ConvertToVector256Byte(value);
	}

	public static Vector256<byte> ConvertToVector256Byte(Vector512<ushort> value)
	{
		return ConvertToVector256Byte(value);
	}

	public static Vector256<byte> ConvertToVector256ByteWithSaturation(Vector512<ushort> value)
	{
		return ConvertToVector256ByteWithSaturation(value);
	}

	public static Vector256<sbyte> ConvertToVector256SByte(Vector512<short> value)
	{
		return ConvertToVector256SByte(value);
	}

	public static Vector256<sbyte> ConvertToVector256SByte(Vector512<ushort> value)
	{
		return ConvertToVector256SByte(value);
	}

	public static Vector256<sbyte> ConvertToVector256SByteWithSaturation(Vector512<short> value)
	{
		return ConvertToVector256SByteWithSaturation(value);
	}

	public static Vector512<short> ConvertToVector512Int16(Vector256<sbyte> value)
	{
		return ConvertToVector512Int16(value);
	}

	public static Vector512<short> ConvertToVector512Int16(Vector256<byte> value)
	{
		return ConvertToVector512Int16(value);
	}

	public static Vector512<ushort> ConvertToVector512UInt16(Vector256<sbyte> value)
	{
		return ConvertToVector512UInt16(value);
	}

	public static Vector512<ushort> ConvertToVector512UInt16(Vector256<byte> value)
	{
		return ConvertToVector512UInt16(value);
	}

	public new unsafe static Vector512<sbyte> LoadVector512(sbyte* address)
	{
		return LoadVector512(address);
	}

	public new unsafe static Vector512<byte> LoadVector512(byte* address)
	{
		return LoadVector512(address);
	}

	public new unsafe static Vector512<short> LoadVector512(short* address)
	{
		return LoadVector512(address);
	}

	public new unsafe static Vector512<ushort> LoadVector512(ushort* address)
	{
		return LoadVector512(address);
	}

	public static Vector512<sbyte> Max(Vector512<sbyte> left, Vector512<sbyte> right)
	{
		return Max(left, right);
	}

	public static Vector512<byte> Max(Vector512<byte> left, Vector512<byte> right)
	{
		return Max(left, right);
	}

	public static Vector512<short> Max(Vector512<short> left, Vector512<short> right)
	{
		return Max(left, right);
	}

	public static Vector512<ushort> Max(Vector512<ushort> left, Vector512<ushort> right)
	{
		return Max(left, right);
	}

	public static Vector512<sbyte> Min(Vector512<sbyte> left, Vector512<sbyte> right)
	{
		return Min(left, right);
	}

	public static Vector512<byte> Min(Vector512<byte> left, Vector512<byte> right)
	{
		return Min(left, right);
	}

	public static Vector512<short> Min(Vector512<short> left, Vector512<short> right)
	{
		return Min(left, right);
	}

	public static Vector512<ushort> Min(Vector512<ushort> left, Vector512<ushort> right)
	{
		return Min(left, right);
	}

	public static Vector512<int> MultiplyAddAdjacent(Vector512<short> left, Vector512<short> right)
	{
		return MultiplyAddAdjacent(left, right);
	}

	public static Vector512<short> MultiplyAddAdjacent(Vector512<byte> left, Vector512<sbyte> right)
	{
		return MultiplyAddAdjacent(left, right);
	}

	public static Vector512<short> MultiplyHigh(Vector512<short> left, Vector512<short> right)
	{
		return MultiplyHigh(left, right);
	}

	public static Vector512<ushort> MultiplyHigh(Vector512<ushort> left, Vector512<ushort> right)
	{
		return MultiplyHigh(left, right);
	}

	public static Vector512<short> MultiplyHighRoundScale(Vector512<short> left, Vector512<short> right)
	{
		return MultiplyHighRoundScale(left, right);
	}

	public static Vector512<short> MultiplyLow(Vector512<short> left, Vector512<short> right)
	{
		return MultiplyLow(left, right);
	}

	public static Vector512<ushort> MultiplyLow(Vector512<ushort> left, Vector512<ushort> right)
	{
		return MultiplyLow(left, right);
	}

	public static Vector512<sbyte> PackSignedSaturate(Vector512<short> left, Vector512<short> right)
	{
		return PackSignedSaturate(left, right);
	}

	public static Vector512<short> PackSignedSaturate(Vector512<int> left, Vector512<int> right)
	{
		return PackSignedSaturate(left, right);
	}

	public static Vector512<byte> PackUnsignedSaturate(Vector512<short> left, Vector512<short> right)
	{
		return PackUnsignedSaturate(left, right);
	}

	public static Vector512<ushort> PackUnsignedSaturate(Vector512<int> left, Vector512<int> right)
	{
		return PackUnsignedSaturate(left, right);
	}

	public static Vector512<short> PermuteVar32x16(Vector512<short> left, Vector512<short> control)
	{
		return PermuteVar32x16(left, control);
	}

	public static Vector512<ushort> PermuteVar32x16(Vector512<ushort> left, Vector512<ushort> control)
	{
		return PermuteVar32x16(left, control);
	}

	public static Vector512<short> PermuteVar32x16x2(Vector512<short> lower, Vector512<short> indices, Vector512<short> upper)
	{
		return PermuteVar32x16x2(lower, indices, upper);
	}

	public static Vector512<ushort> PermuteVar32x16x2(Vector512<ushort> lower, Vector512<ushort> indices, Vector512<ushort> upper)
	{
		return PermuteVar32x16x2(lower, indices, upper);
	}

	public static Vector512<short> ShiftLeftLogical(Vector512<short> value, Vector128<short> count)
	{
		return ShiftLeftLogical(value, count);
	}

	public static Vector512<ushort> ShiftLeftLogical(Vector512<ushort> value, Vector128<ushort> count)
	{
		return ShiftLeftLogical(value, count);
	}

	public static Vector512<short> ShiftLeftLogical(Vector512<short> value, [ConstantExpected] byte count)
	{
		return ShiftLeftLogical(value, count);
	}

	public static Vector512<ushort> ShiftLeftLogical(Vector512<ushort> value, [ConstantExpected] byte count)
	{
		return ShiftLeftLogical(value, count);
	}

	public static Vector512<sbyte> ShiftLeftLogical128BitLane(Vector512<sbyte> value, [ConstantExpected] byte numBytes)
	{
		return ShiftLeftLogical128BitLane(value, numBytes);
	}

	public static Vector512<byte> ShiftLeftLogical128BitLane(Vector512<byte> value, [ConstantExpected] byte numBytes)
	{
		return ShiftLeftLogical128BitLane(value, numBytes);
	}

	public static Vector512<short> ShiftLeftLogicalVariable(Vector512<short> value, Vector512<ushort> count)
	{
		return ShiftLeftLogicalVariable(value, count);
	}

	public static Vector512<ushort> ShiftLeftLogicalVariable(Vector512<ushort> value, Vector512<ushort> count)
	{
		return ShiftLeftLogicalVariable(value, count);
	}

	public static Vector512<short> ShiftRightArithmetic(Vector512<short> value, Vector128<short> count)
	{
		return ShiftRightArithmetic(value, count);
	}

	public static Vector512<short> ShiftRightArithmetic(Vector512<short> value, [ConstantExpected] byte count)
	{
		return ShiftRightArithmetic(value, count);
	}

	public static Vector512<short> ShiftRightArithmeticVariable(Vector512<short> value, Vector512<ushort> count)
	{
		return ShiftRightArithmeticVariable(value, count);
	}

	public static Vector512<short> ShiftRightLogical(Vector512<short> value, Vector128<short> count)
	{
		return ShiftRightLogical(value, count);
	}

	public static Vector512<ushort> ShiftRightLogical(Vector512<ushort> value, Vector128<ushort> count)
	{
		return ShiftRightLogical(value, count);
	}

	public static Vector512<short> ShiftRightLogical(Vector512<short> value, [ConstantExpected] byte count)
	{
		return ShiftRightLogical(value, count);
	}

	public static Vector512<ushort> ShiftRightLogical(Vector512<ushort> value, [ConstantExpected] byte count)
	{
		return ShiftRightLogical(value, count);
	}

	public static Vector512<sbyte> ShiftRightLogical128BitLane(Vector512<sbyte> value, [ConstantExpected] byte numBytes)
	{
		return ShiftRightLogical128BitLane(value, numBytes);
	}

	public static Vector512<byte> ShiftRightLogical128BitLane(Vector512<byte> value, [ConstantExpected] byte numBytes)
	{
		return ShiftRightLogical128BitLane(value, numBytes);
	}

	public static Vector512<short> ShiftRightLogicalVariable(Vector512<short> value, Vector512<ushort> count)
	{
		return ShiftRightLogicalVariable(value, count);
	}

	public static Vector512<ushort> ShiftRightLogicalVariable(Vector512<ushort> value, Vector512<ushort> count)
	{
		return ShiftRightLogicalVariable(value, count);
	}

	public static Vector512<sbyte> Shuffle(Vector512<sbyte> value, Vector512<sbyte> mask)
	{
		return Shuffle(value, mask);
	}

	public static Vector512<byte> Shuffle(Vector512<byte> value, Vector512<byte> mask)
	{
		return Shuffle(value, mask);
	}

	public static Vector512<short> ShuffleHigh(Vector512<short> value, [ConstantExpected] byte control)
	{
		return ShuffleHigh(value, control);
	}

	public static Vector512<ushort> ShuffleHigh(Vector512<ushort> value, [ConstantExpected] byte control)
	{
		return ShuffleHigh(value, control);
	}

	public static Vector512<short> ShuffleLow(Vector512<short> value, [ConstantExpected] byte control)
	{
		return ShuffleLow(value, control);
	}

	public static Vector512<ushort> ShuffleLow(Vector512<ushort> value, [ConstantExpected] byte control)
	{
		return ShuffleLow(value, control);
	}

	public new unsafe static void Store(sbyte* address, Vector512<sbyte> source)
	{
		Store(address, source);
	}

	public new unsafe static void Store(byte* address, Vector512<byte> source)
	{
		Store(address, source);
	}

	public new unsafe static void Store(short* address, Vector512<short> source)
	{
		Store(address, source);
	}

	public new unsafe static void Store(ushort* address, Vector512<ushort> source)
	{
		Store(address, source);
	}

	public static Vector512<sbyte> Subtract(Vector512<sbyte> left, Vector512<sbyte> right)
	{
		return Subtract(left, right);
	}

	public static Vector512<byte> Subtract(Vector512<byte> left, Vector512<byte> right)
	{
		return Subtract(left, right);
	}

	public static Vector512<short> Subtract(Vector512<short> left, Vector512<short> right)
	{
		return Subtract(left, right);
	}

	public static Vector512<ushort> Subtract(Vector512<ushort> left, Vector512<ushort> right)
	{
		return Subtract(left, right);
	}

	public static Vector512<sbyte> SubtractSaturate(Vector512<sbyte> left, Vector512<sbyte> right)
	{
		return SubtractSaturate(left, right);
	}

	public static Vector512<short> SubtractSaturate(Vector512<short> left, Vector512<short> right)
	{
		return SubtractSaturate(left, right);
	}

	public static Vector512<byte> SubtractSaturate(Vector512<byte> left, Vector512<byte> right)
	{
		return SubtractSaturate(left, right);
	}

	public static Vector512<ushort> SubtractSaturate(Vector512<ushort> left, Vector512<ushort> right)
	{
		return SubtractSaturate(left, right);
	}

	public static Vector512<ushort> SumAbsoluteDifferences(Vector512<byte> left, Vector512<byte> right)
	{
		return SumAbsoluteDifferences(left, right);
	}

	public static Vector512<ushort> SumAbsoluteDifferencesInBlock32(Vector512<byte> left, Vector512<byte> right, [ConstantExpected] byte control)
	{
		return SumAbsoluteDifferencesInBlock32(left, right, control);
	}

	public static Vector512<sbyte> UnpackHigh(Vector512<sbyte> left, Vector512<sbyte> right)
	{
		return UnpackHigh(left, right);
	}

	public static Vector512<byte> UnpackHigh(Vector512<byte> left, Vector512<byte> right)
	{
		return UnpackHigh(left, right);
	}

	public static Vector512<short> UnpackHigh(Vector512<short> left, Vector512<short> right)
	{
		return UnpackHigh(left, right);
	}

	public static Vector512<ushort> UnpackHigh(Vector512<ushort> left, Vector512<ushort> right)
	{
		return UnpackHigh(left, right);
	}

	public static Vector512<sbyte> UnpackLow(Vector512<sbyte> left, Vector512<sbyte> right)
	{
		return UnpackLow(left, right);
	}

	public static Vector512<byte> UnpackLow(Vector512<byte> left, Vector512<byte> right)
	{
		return UnpackLow(left, right);
	}

	public static Vector512<short> UnpackLow(Vector512<short> left, Vector512<short> right)
	{
		return UnpackLow(left, right);
	}

	public static Vector512<ushort> UnpackLow(Vector512<ushort> left, Vector512<ushort> right)
	{
		return UnpackLow(left, right);
	}
}
