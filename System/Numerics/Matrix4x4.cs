using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics;

[Intrinsic]
public struct Matrix4x4 : IEquatable<Matrix4x4>
{
	internal struct Impl : IEquatable<Impl>
	{
		private struct CanonicalBasis
		{
			public Vector3 Row0;

			public Vector3 Row1;

			public Vector3 Row2;
		}

		private struct VectorBasis
		{
			public unsafe Vector3* Element0;

			public unsafe Vector3* Element1;

			public unsafe Vector3* Element2;
		}

		public Vector4 X;

		public Vector4 Y;

		public Vector4 Z;

		public Vector4 W;

		public static Impl Identity
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				Unsafe.SkipInit(out Impl result);
				result.X = Vector4.UnitX;
				result.Y = Vector4.UnitY;
				result.Z = Vector4.UnitZ;
				result.W = Vector4.UnitW;
				return result;
			}
		}

		public float this[int row, int column]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				if ((uint)row >= 4u)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException();
				}
				return Unsafe.Add(ref Unsafe.AsRef(ref X), row)[column];
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				if ((uint)row >= 4u)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException();
				}
				Unsafe.Add(ref X, row)[column] = value;
			}
		}

		public readonly bool IsIdentity
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				if (X == Vector4.UnitX && Y == Vector4.UnitY && Z == Vector4.UnitZ)
				{
					return W == Vector4.UnitW;
				}
				return false;
			}
		}

		public Vector3 Translation
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return new Vector3(W.X, W.Y, W.Z);
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				W = new Vector4(value, W.W);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[UnscopedRef]
		public ref Matrix4x4 AsM4x4()
		{
			return ref Unsafe.As<Impl, Matrix4x4>(ref this);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Init(float m11, float m12, float m13, float m14, float m21, float m22, float m23, float m24, float m31, float m32, float m33, float m34, float m41, float m42, float m43, float m44)
		{
			X = new Vector4(m11, m12, m13, m14);
			Y = new Vector4(m21, m22, m23, m24);
			Z = new Vector4(m31, m32, m33, m34);
			W = new Vector4(m41, m42, m43, m44);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Init(in Matrix3x2.Impl value)
		{
			X = new Vector4(value.X, 0f, 0f);
			Y = new Vector4(value.Y, 0f, 0f);
			Z = Vector4.UnitZ;
			W = new Vector4(value.Z, 0f, 1f);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl operator +(in Impl left, in Impl right)
		{
			Unsafe.SkipInit(out Impl result);
			result.X = left.X + right.X;
			result.Y = left.Y + right.Y;
			result.Z = left.Z + right.Z;
			result.W = left.W + right.W;
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(in Impl left, in Impl right)
		{
			if (left.X == right.X && left.Y == right.Y && left.Z == right.Z)
			{
				return left.W == right.W;
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(in Impl left, in Impl right)
		{
			if (!(left.X != right.X) && !(left.Y != right.Y) && !(left.Z != right.Z))
			{
				return left.W != right.W;
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl operator *(in Impl left, in Impl right)
		{
			Unsafe.SkipInit(out Impl result);
			result.X = right.X * left.X.X;
			result.X += right.Y * left.X.Y;
			result.X += right.Z * left.X.Z;
			result.X += right.W * left.X.W;
			result.Y = right.X * left.Y.X;
			result.Y += right.Y * left.Y.Y;
			result.Y += right.Z * left.Y.Z;
			result.Y += right.W * left.Y.W;
			result.Z = right.X * left.Z.X;
			result.Z += right.Y * left.Z.Y;
			result.Z += right.Z * left.Z.Z;
			result.Z += right.W * left.Z.W;
			result.W = right.X * left.W.X;
			result.W += right.Y * left.W.Y;
			result.W += right.Z * left.W.Z;
			result.W += right.W * left.W.W;
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl operator *(in Impl left, float right)
		{
			Unsafe.SkipInit(out Impl result);
			result.X = left.X * right;
			result.Y = left.Y * right;
			result.Z = left.Z * right;
			result.W = left.W * right;
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl operator -(in Impl left, in Impl right)
		{
			Unsafe.SkipInit(out Impl result);
			result.X = left.X - right.X;
			result.Y = left.Y - right.Y;
			result.Z = left.Z - right.Z;
			result.W = left.W - right.W;
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl operator -(in Impl value)
		{
			Unsafe.SkipInit(out Impl result);
			result.X = -value.X;
			result.Y = -value.Y;
			result.Z = -value.Z;
			result.W = -value.W;
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateBillboard(in Vector3 objectPosition, in Vector3 cameraPosition, in Vector3 cameraUpVector, in Vector3 cameraForwardVector)
		{
			Vector3 left = objectPosition - cameraPosition;
			float num = left.LengthSquared();
			left = ((!(num < 0.0001f)) ? Vector3.Multiply(left, 1f / MathF.Sqrt(num)) : (-cameraForwardVector));
			Vector3 vector = Vector3.Normalize(Vector3.Cross(cameraUpVector, left));
			Vector3 value = Vector3.Cross(left, vector);
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(vector, 0f);
			result.Y = new Vector4(value, 0f);
			result.Z = new Vector4(left, 0f);
			result.W = new Vector4(objectPosition, 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateConstrainedBillboard(in Vector3 objectPosition, in Vector3 cameraPosition, in Vector3 rotateAxis, in Vector3 cameraForwardVector, in Vector3 objectForwardVector)
		{
			Vector3 left = objectPosition - cameraPosition;
			float num = left.LengthSquared();
			left = ((!(num < 0.0001f)) ? Vector3.Multiply(left, 1f / MathF.Sqrt(num)) : (-cameraForwardVector));
			Vector3 vector = rotateAxis;
			float x = Vector3.Dot(vector, left);
			if (MathF.Abs(x) > 0.99825466f)
			{
				left = objectForwardVector;
				x = Vector3.Dot(vector, left);
				if (MathF.Abs(x) > 0.99825466f)
				{
					left = ((MathF.Abs(vector.Z) > 0.99825466f) ? Vector3.UnitX : new Vector3(0f, 0f, -1f));
				}
			}
			Vector3 vector2 = Vector3.Normalize(Vector3.Cross(vector, left));
			Vector3 value = Vector3.Normalize(Vector3.Cross(vector2, vector));
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(vector2, 0f);
			result.Y = new Vector4(vector, 0f);
			result.Z = new Vector4(value, 0f);
			result.W = new Vector4(objectPosition, 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateFromAxisAngle(in Vector3 axis, float angle)
		{
			float x = axis.X;
			float y = axis.Y;
			float z = axis.Z;
			float num = MathF.Sin(angle);
			float num2 = MathF.Cos(angle);
			float num3 = x * x;
			float num4 = y * y;
			float num5 = z * z;
			float num6 = x * y;
			float num7 = x * z;
			float num8 = y * z;
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(num3 + num2 * (1f - num3), num6 - num2 * num6 + num * z, num7 - num2 * num7 - num * y, 0f);
			result.Y = new Vector4(num6 - num2 * num6 - num * z, num4 + num2 * (1f - num4), num8 - num2 * num8 + num * x, 0f);
			result.Z = new Vector4(num7 - num2 * num7 + num * y, num8 - num2 * num8 - num * x, num5 + num2 * (1f - num5), 0f);
			result.W = Vector4.UnitW;
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateFromQuaternion(in Quaternion quaternion)
		{
			float num = quaternion.X * quaternion.X;
			float num2 = quaternion.Y * quaternion.Y;
			float num3 = quaternion.Z * quaternion.Z;
			float num4 = quaternion.X * quaternion.Y;
			float num5 = quaternion.Z * quaternion.W;
			float num6 = quaternion.Z * quaternion.X;
			float num7 = quaternion.Y * quaternion.W;
			float num8 = quaternion.Y * quaternion.Z;
			float num9 = quaternion.X * quaternion.W;
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(1f - 2f * (num2 + num3), 2f * (num4 + num5), 2f * (num6 - num7), 0f);
			result.Y = new Vector4(2f * (num4 - num5), 1f - 2f * (num3 + num), 2f * (num8 + num9), 0f);
			result.Z = new Vector4(2f * (num6 + num7), 2f * (num8 - num9), 1f - 2f * (num2 + num), 0f);
			result.W = Vector4.UnitW;
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateFromYawPitchRoll(float yaw, float pitch, float roll)
		{
			Quaternion quaternion = Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);
			return CreateFromQuaternion(in quaternion);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateLookTo(in Vector3 cameraPosition, in Vector3 cameraDirection, in Vector3 cameraUpVector)
		{
			Vector3 vector = Vector3.Normalize(-cameraDirection);
			Vector3 vector2 = Vector3.Normalize(Vector3.Cross(cameraUpVector, vector));
			Vector3 vector3 = Vector3.Cross(vector, vector2);
			Vector3 vector4 = -cameraPosition;
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(vector2.X, vector3.X, vector.X, 0f);
			result.Y = new Vector4(vector2.Y, vector3.Y, vector.Y, 0f);
			result.Z = new Vector4(vector2.Z, vector3.Z, vector.Z, 0f);
			result.W = new Vector4(Vector3.Dot(vector2, vector4), Vector3.Dot(vector3, vector4), Vector3.Dot(vector, vector4), 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateLookToLeftHanded(in Vector3 cameraPosition, in Vector3 cameraDirection, in Vector3 cameraUpVector)
		{
			Vector3 vector = Vector3.Normalize(cameraDirection);
			Vector3 vector2 = Vector3.Normalize(Vector3.Cross(cameraUpVector, vector));
			Vector3 vector3 = Vector3.Cross(vector, vector2);
			Vector3 vector4 = -cameraPosition;
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(vector2.X, vector3.X, vector.X, 0f);
			result.Y = new Vector4(vector2.Y, vector3.Y, vector.Y, 0f);
			result.Z = new Vector4(vector2.Z, vector3.Z, vector.Z, 0f);
			result.W = new Vector4(Vector3.Dot(vector2, vector4), Vector3.Dot(vector3, vector4), Vector3.Dot(vector, vector4), 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane)
		{
			float num = 1f / (zNearPlane - zFarPlane);
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(2f / width, 0f, 0f, 0f);
			result.Y = new Vector4(0f, 2f / height, 0f, 0f);
			result.Z = new Vector4(0f, 0f, num, 0f);
			result.W = new Vector4(0f, 0f, num * zNearPlane, 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateOrthographicLeftHanded(float width, float height, float zNearPlane, float zFarPlane)
		{
			float num = 1f / (zFarPlane - zNearPlane);
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(2f / width, 0f, 0f, 0f);
			result.Y = new Vector4(0f, 2f / height, 0f, 0f);
			result.Z = new Vector4(0f, 0f, num, 0f);
			result.W = new Vector4(0f, 0f, (0f - num) * zNearPlane, 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
		{
			float num = 1f / (right - left);
			float num2 = 1f / (top - bottom);
			float num3 = 1f / (zNearPlane - zFarPlane);
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(num + num, 0f, 0f, 0f);
			result.Y = new Vector4(0f, num2 + num2, 0f, 0f);
			result.Z = new Vector4(0f, 0f, num3, 0f);
			result.W = new Vector4((0f - (left + right)) * num, (0f - (top + bottom)) * num2, num3 * zNearPlane, 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateOrthographicOffCenterLeftHanded(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
		{
			float num = 1f / (right - left);
			float num2 = 1f / (top - bottom);
			float num3 = 1f / (zFarPlane - zNearPlane);
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(num + num, 0f, 0f, 0f);
			result.Y = new Vector4(0f, num2 + num2, 0f, 0f);
			result.Z = new Vector4(0f, 0f, num3, 0f);
			result.W = new Vector4((0f - (left + right)) * num, (0f - (top + bottom)) * num2, (0f - num3) * zNearPlane, 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreatePerspective(float width, float height, float nearPlaneDistance, float farPlaneDistance)
		{
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(nearPlaneDistance, 0f, "nearPlaneDistance");
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(farPlaneDistance, 0f, "farPlaneDistance");
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(nearPlaneDistance, farPlaneDistance, "nearPlaneDistance");
			float num = nearPlaneDistance + nearPlaneDistance;
			float num2 = (float.IsPositiveInfinity(farPlaneDistance) ? (-1f) : (farPlaneDistance / (nearPlaneDistance - farPlaneDistance)));
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(num / width, 0f, 0f, 0f);
			result.Y = new Vector4(0f, num / height, 0f, 0f);
			result.Z = new Vector4(0f, 0f, num2, -1f);
			result.W = new Vector4(0f, 0f, num2 * nearPlaneDistance, 0f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreatePerspectiveLeftHanded(float width, float height, float nearPlaneDistance, float farPlaneDistance)
		{
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(nearPlaneDistance, 0f, "nearPlaneDistance");
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(farPlaneDistance, 0f, "farPlaneDistance");
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(nearPlaneDistance, farPlaneDistance, "nearPlaneDistance");
			float num = nearPlaneDistance + nearPlaneDistance;
			float num2 = (float.IsPositiveInfinity(farPlaneDistance) ? 1f : (farPlaneDistance / (farPlaneDistance - nearPlaneDistance)));
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(num / width, 0f, 0f, 0f);
			result.Y = new Vector4(0f, num / height, 0f, 0f);
			result.Z = new Vector4(0f, 0f, num2, 1f);
			result.W = new Vector4(0f, 0f, (0f - num2) * nearPlaneDistance, 0f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
		{
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fieldOfView, 0f, "fieldOfView");
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fieldOfView, (float)Math.PI, "fieldOfView");
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(nearPlaneDistance, 0f, "nearPlaneDistance");
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(farPlaneDistance, 0f, "farPlaneDistance");
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(nearPlaneDistance, farPlaneDistance, "nearPlaneDistance");
			float num = 1f / MathF.Tan(fieldOfView * 0.5f);
			float x = num / aspectRatio;
			float num2 = (float.IsPositiveInfinity(farPlaneDistance) ? (-1f) : (farPlaneDistance / (nearPlaneDistance - farPlaneDistance)));
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(x, 0f, 0f, 0f);
			result.Y = new Vector4(0f, num, 0f, 0f);
			result.Z = new Vector4(0f, 0f, num2, -1f);
			result.W = new Vector4(0f, 0f, num2 * nearPlaneDistance, 0f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreatePerspectiveFieldOfViewLeftHanded(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
		{
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fieldOfView, 0f, "fieldOfView");
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fieldOfView, (float)Math.PI, "fieldOfView");
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(nearPlaneDistance, 0f, "nearPlaneDistance");
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(farPlaneDistance, 0f, "farPlaneDistance");
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(nearPlaneDistance, farPlaneDistance, "nearPlaneDistance");
			float num = 1f / MathF.Tan(fieldOfView * 0.5f);
			float x = num / aspectRatio;
			float num2 = (float.IsPositiveInfinity(farPlaneDistance) ? 1f : (farPlaneDistance / (farPlaneDistance - nearPlaneDistance)));
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(x, 0f, 0f, 0f);
			result.Y = new Vector4(0f, num, 0f, 0f);
			result.Z = new Vector4(0f, 0f, num2, 1f);
			result.W = new Vector4(0f, 0f, (0f - num2) * nearPlaneDistance, 0f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float nearPlaneDistance, float farPlaneDistance)
		{
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(nearPlaneDistance, 0f, "nearPlaneDistance");
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(farPlaneDistance, 0f, "farPlaneDistance");
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(nearPlaneDistance, farPlaneDistance, "nearPlaneDistance");
			float num = nearPlaneDistance + nearPlaneDistance;
			float num2 = 1f / (right - left);
			float num3 = 1f / (top - bottom);
			float num4 = (float.IsPositiveInfinity(farPlaneDistance) ? (-1f) : (farPlaneDistance / (nearPlaneDistance - farPlaneDistance)));
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(num * num2, 0f, 0f, 0f);
			result.Y = new Vector4(0f, num * num3, 0f, 0f);
			result.Z = new Vector4((left + right) * num2, (top + bottom) * num3, num4, -1f);
			result.W = new Vector4(0f, 0f, num4 * nearPlaneDistance, 0f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreatePerspectiveOffCenterLeftHanded(float left, float right, float bottom, float top, float nearPlaneDistance, float farPlaneDistance)
		{
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(nearPlaneDistance, 0f, "nearPlaneDistance");
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(farPlaneDistance, 0f, "farPlaneDistance");
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(nearPlaneDistance, farPlaneDistance, "nearPlaneDistance");
			float num = nearPlaneDistance + nearPlaneDistance;
			float num2 = 1f / (right - left);
			float num3 = 1f / (top - bottom);
			float num4 = (float.IsPositiveInfinity(farPlaneDistance) ? 1f : (farPlaneDistance / (farPlaneDistance - nearPlaneDistance)));
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(num * num2, 0f, 0f, 0f);
			result.Y = new Vector4(0f, num * num3, 0f, 0f);
			result.Z = new Vector4((0f - (left + right)) * num2, (0f - (top + bottom)) * num3, num4, 1f);
			result.W = new Vector4(0f, 0f, (0f - num4) * nearPlaneDistance, 0f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateReflection(in Plane value)
		{
			Plane plane = Plane.Normalize(value);
			Vector3 vector = plane.Normal * -2f;
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(vector * plane.Normal.X, 0f) + Vector4.UnitX;
			result.Y = new Vector4(vector * plane.Normal.Y, 0f) + Vector4.UnitY;
			result.Z = new Vector4(vector * plane.Normal.Z, 0f) + Vector4.UnitZ;
			result.W = new Vector4(vector * plane.D, 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateRotationX(float radians)
		{
			float num = MathF.Cos(radians);
			float num2 = MathF.Sin(radians);
			Unsafe.SkipInit(out Impl result);
			result.X = Vector4.UnitX;
			result.Y = new Vector4(0f, num, num2, 0f);
			result.Z = new Vector4(0f, 0f - num2, num, 0f);
			result.W = Vector4.UnitW;
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateRotationX(float radians, in Vector3 centerPoint)
		{
			float num = MathF.Cos(radians);
			float num2 = MathF.Sin(radians);
			float y = centerPoint.Y * (1f - num) + centerPoint.Z * num2;
			float z = centerPoint.Z * (1f - num) - centerPoint.Y * num2;
			Unsafe.SkipInit(out Impl result);
			result.X = Vector4.UnitX;
			result.Y = new Vector4(0f, num, num2, 0f);
			result.Z = new Vector4(0f, 0f - num2, num, 0f);
			result.W = new Vector4(0f, y, z, 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateRotationY(float radians)
		{
			float num = MathF.Cos(radians);
			float num2 = MathF.Sin(radians);
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(num, 0f, 0f - num2, 0f);
			result.Y = Vector4.UnitY;
			result.Z = new Vector4(num2, 0f, num, 0f);
			result.W = Vector4.UnitW;
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateRotationY(float radians, in Vector3 centerPoint)
		{
			float num = MathF.Cos(radians);
			float num2 = MathF.Sin(radians);
			float x = centerPoint.X * (1f - num) - centerPoint.Z * num2;
			float z = centerPoint.Z * (1f - num) + centerPoint.X * num2;
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(num, 0f, 0f - num2, 0f);
			result.Y = Vector4.UnitY;
			result.Z = new Vector4(num2, 0f, num, 0f);
			result.W = new Vector4(x, 0f, z, 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateRotationZ(float radians)
		{
			float num = MathF.Cos(radians);
			float num2 = MathF.Sin(radians);
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(num, num2, 0f, 0f);
			result.Y = new Vector4(0f - num2, num, 0f, 0f);
			result.Z = Vector4.UnitZ;
			result.W = Vector4.UnitW;
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateRotationZ(float radians, in Vector3 centerPoint)
		{
			float num = MathF.Cos(radians);
			float num2 = MathF.Sin(radians);
			float x = centerPoint.X * (1f - num) + centerPoint.Y * num2;
			float y = centerPoint.Y * (1f - num) - centerPoint.X * num2;
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(num, num2, 0f, 0f);
			result.Y = new Vector4(0f - num2, num, 0f, 0f);
			result.Z = Vector4.UnitZ;
			result.W = new Vector4(x, y, 0f, 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateScale(float scaleX, float scaleY, float scaleZ)
		{
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(scaleX, 0f, 0f, 0f);
			result.Y = new Vector4(0f, scaleY, 0f, 0f);
			result.Z = new Vector4(0f, 0f, scaleZ, 0f);
			result.W = Vector4.UnitW;
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateScale(float scaleX, float scaleY, float scaleZ, in Vector3 centerPoint)
		{
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(scaleX, 0f, 0f, 0f);
			result.Y = new Vector4(0f, scaleY, 0f, 0f);
			result.Z = new Vector4(0f, 0f, scaleZ, 0f);
			result.W = new Vector4(centerPoint * (Vector3.One - new Vector3(scaleX, scaleY, scaleZ)), 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateScale(in Vector3 scales)
		{
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(scales.X, 0f, 0f, 0f);
			result.Y = new Vector4(0f, scales.Y, 0f, 0f);
			result.Z = new Vector4(0f, 0f, scales.Z, 0f);
			result.W = Vector4.UnitW;
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateScale(in Vector3 scales, in Vector3 centerPoint)
		{
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(scales.X, 0f, 0f, 0f);
			result.Y = new Vector4(0f, scales.Y, 0f, 0f);
			result.Z = new Vector4(0f, 0f, scales.Z, 0f);
			result.W = new Vector4(centerPoint * (Vector3.One - scales), 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateScale(float scale)
		{
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(scale, 0f, 0f, 0f);
			result.Y = new Vector4(0f, scale, 0f, 0f);
			result.Z = new Vector4(0f, 0f, scale, 0f);
			result.W = Vector4.UnitW;
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateScale(float scale, in Vector3 centerPoint)
		{
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(scale, 0f, 0f, 0f);
			result.Y = new Vector4(0f, scale, 0f, 0f);
			result.Z = new Vector4(0f, 0f, scale, 0f);
			result.W = new Vector4(centerPoint * (Vector3.One - new Vector3(scale)), 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateShadow(in Vector3 lightDirection, in Plane plane)
		{
			Plane plane2 = Plane.Normalize(plane);
			float num = Vector3.Dot(lightDirection, plane2.Normal);
			Vector3 vector = -plane2.Normal;
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(lightDirection * vector.X, 0f) + new Vector4(num, 0f, 0f, 0f);
			result.Y = new Vector4(lightDirection * vector.Y, 0f) + new Vector4(0f, num, 0f, 0f);
			result.Z = new Vector4(lightDirection * vector.Z, 0f) + new Vector4(0f, 0f, num, 0f);
			result.W = new Vector4(lightDirection * (0f - plane2.D), num);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateTranslation(in Vector3 position)
		{
			Unsafe.SkipInit(out Impl result);
			result.X = Vector4.UnitX;
			result.Y = Vector4.UnitY;
			result.Z = Vector4.UnitZ;
			result.W = new Vector4(position, 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateTranslation(float positionX, float positionY, float positionZ)
		{
			Unsafe.SkipInit(out Impl result);
			result.X = Vector4.UnitX;
			result.Y = Vector4.UnitY;
			result.Z = Vector4.UnitZ;
			result.W = new Vector4(positionX, positionY, positionZ, 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateViewport(float x, float y, float width, float height, float minDepth, float maxDepth)
		{
			Unsafe.SkipInit(out Impl result);
			result.W = new Vector4(width, height, 0f, 0f);
			result.W *= new Vector4(0.5f, 0.5f, 0f, 0f);
			result.X = new Vector4(result.W.X, 0f, 0f, 0f);
			result.Y = new Vector4(0f, 0f - result.W.Y, 0f, 0f);
			result.Z = new Vector4(0f, 0f, minDepth - maxDepth, 0f);
			result.W += new Vector4(x, y, minDepth, 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateViewportLeftHanded(float x, float y, float width, float height, float minDepth, float maxDepth)
		{
			Unsafe.SkipInit(out Impl result);
			result.W = new Vector4(width, height, 0f, 0f);
			result.W *= new Vector4(0.5f, 0.5f, 0f, 0f);
			result.X = new Vector4(result.W.X, 0f, 0f, 0f);
			result.Y = new Vector4(0f, 0f - result.W.Y, 0f, 0f);
			result.Z = new Vector4(0f, 0f, maxDepth - minDepth, 0f);
			result.W += new Vector4(x, y, minDepth, 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl CreateWorld(in Vector3 position, in Vector3 forward, in Vector3 up)
		{
			Vector3 vector = Vector3.Normalize(-forward);
			Vector3 vector2 = Vector3.Normalize(Vector3.Cross(up, vector));
			Vector3 value = Vector3.Cross(vector, vector2);
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(vector2, 0f);
			result.Y = new Vector4(value, 0f);
			result.Z = new Vector4(vector, 0f);
			result.W = new Vector4(position, 1f);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static bool Decompose(in Impl matrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation)
		{
			bool result = true;
			fixed (Vector3* ptr = &scale)
			{
				float* ptr2 = (float*)ptr;
				Unsafe.SkipInit(out VectorBasis vectorBasis);
				Vector3** ptr3 = (Vector3**)(&vectorBasis);
				Impl source = Identity;
				CanonicalBasis canonicalBasis = default(CanonicalBasis);
				Vector3* ptr4 = &canonicalBasis.Row0;
				canonicalBasis.Row0 = new Vector3(1f, 0f, 0f);
				canonicalBasis.Row1 = new Vector3(0f, 1f, 0f);
				canonicalBasis.Row2 = new Vector3(0f, 0f, 1f);
				translation = new Vector3(matrix.W.X, matrix.W.Y, matrix.W.Z);
				*ptr3 = (Vector3*)(&source.X);
				ptr3[1] = (Vector3*)(&source.Y);
				ptr3[2] = (Vector3*)(&source.Z);
				*(*ptr3) = new Vector3(matrix.X.X, matrix.X.Y, matrix.X.Z);
				*ptr3[1] = new Vector3(matrix.Y.X, matrix.Y.Y, matrix.Y.Z);
				*ptr3[2] = new Vector3(matrix.Z.X, matrix.Z.Y, matrix.Z.Z);
				scale.X = (*ptr3)->Length();
				scale.Y = ptr3[1]->Length();
				scale.Z = ptr3[2]->Length();
				float num = *ptr2;
				float num2 = ptr2[1];
				float num3 = ptr2[2];
				uint num4;
				uint num5;
				uint num6;
				if (num < num2)
				{
					if (num2 < num3)
					{
						num4 = 2u;
						num5 = 1u;
						num6 = 0u;
					}
					else
					{
						num4 = 1u;
						if (num < num3)
						{
							num5 = 2u;
							num6 = 0u;
						}
						else
						{
							num5 = 0u;
							num6 = 2u;
						}
					}
				}
				else if (num < num3)
				{
					num4 = 2u;
					num5 = 0u;
					num6 = 1u;
				}
				else
				{
					num4 = 0u;
					if (num2 < num3)
					{
						num5 = 2u;
						num6 = 1u;
					}
					else
					{
						num5 = 1u;
						num6 = 2u;
					}
				}
				if (ptr2[num4] < 0.0001f)
				{
					*ptr3[num4] = ptr4[num4];
				}
				*ptr3[num4] = Vector3.Normalize(*ptr3[num4]);
				if (ptr2[num5] < 0.0001f)
				{
					float num7 = MathF.Abs(ptr3[num4]->X);
					float num8 = MathF.Abs(ptr3[num4]->Y);
					float num9 = MathF.Abs(ptr3[num4]->Z);
					uint num10 = ((num7 < num8) ? ((!(num8 < num9)) ? ((!(num7 < num9)) ? 2u : 0u) : 0u) : ((num7 < num9) ? 1u : ((num8 < num9) ? 1u : 2u)));
					*ptr3[num5] = Vector3.Cross(*ptr3[num4], ptr4[num10]);
				}
				*ptr3[num5] = Vector3.Normalize(*ptr3[num5]);
				if (ptr2[num6] < 0.0001f)
				{
					*ptr3[num6] = Vector3.Cross(*ptr3[num4], *ptr3[num5]);
				}
				*ptr3[num6] = Vector3.Normalize(*ptr3[num6]);
				float num11 = source.GetDeterminant();
				if (num11 < 0f)
				{
					ptr2[num4] = 0f - ptr2[num4];
					*ptr3[num4] = -(*ptr3[num4]);
					num11 = 0f - num11;
				}
				num11 -= 1f;
				num11 *= num11;
				if (0.0001f < num11)
				{
					rotation = Quaternion.Identity;
					result = false;
				}
				else
				{
					rotation = Quaternion.CreateFromRotationMatrix(Unsafe.As<Impl, Matrix4x4>(ref source));
				}
			}
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Invert(in Impl matrix, out Impl result)
		{
			if (Sse.IsSupported)
			{
				return SseImpl(in matrix, out result);
			}
			return SoftwareFallback(in matrix, out result);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static Vector128<float> Permute(Vector128<float> value, [ConstantExpected] byte control)
			{
				if (Avx.IsSupported)
				{
					return Avx.Permute(value, control);
				}
				if (Sse.IsSupported)
				{
					return Sse.Shuffle(value, value, control);
				}
				throw new PlatformNotSupportedException();
			}
			static bool SoftwareFallback(in Impl matrix, out Impl result)
			{
				float x = matrix.X.X;
				float y = matrix.X.Y;
				float z = matrix.X.Z;
				float w = matrix.X.W;
				float x2 = matrix.Y.X;
				float y2 = matrix.Y.Y;
				float z2 = matrix.Y.Z;
				float w2 = matrix.Y.W;
				float x3 = matrix.Z.X;
				float y3 = matrix.Z.Y;
				float z3 = matrix.Z.Z;
				float w3 = matrix.Z.W;
				float x4 = matrix.W.X;
				float y4 = matrix.W.Y;
				float z4 = matrix.W.Z;
				float w4 = matrix.W.W;
				float num = z3 * w4 - w3 * z4;
				float num2 = y3 * w4 - w3 * y4;
				float num3 = y3 * z4 - z3 * y4;
				float num4 = x3 * w4 - w3 * x4;
				float num5 = x3 * z4 - z3 * x4;
				float num6 = x3 * y4 - y3 * x4;
				float num7 = y2 * num - z2 * num2 + w2 * num3;
				float num8 = 0f - (x2 * num - z2 * num4 + w2 * num5);
				float num9 = x2 * num2 - y2 * num4 + w2 * num6;
				float num10 = 0f - (x2 * num3 - y2 * num5 + z2 * num6);
				float num11 = x * num7 + y * num8 + z * num9 + w * num10;
				if (MathF.Abs(num11) < float.Epsilon)
				{
					result.W = (result.Z = (result.Y = (result.X = new Vector4(float.NaN))));
					return false;
				}
				float num12 = 1f / num11;
				result.X.X = num7 * num12;
				result.Y.X = num8 * num12;
				result.Z.X = num9 * num12;
				result.W.X = num10 * num12;
				result.X.Y = (0f - (y * num - z * num2 + w * num3)) * num12;
				result.Y.Y = (x * num - z * num4 + w * num5) * num12;
				result.Z.Y = (0f - (x * num2 - y * num4 + w * num6)) * num12;
				result.W.Y = (x * num3 - y * num5 + z * num6) * num12;
				float num13 = z2 * w4 - w2 * z4;
				float num14 = y2 * w4 - w2 * y4;
				float num15 = y2 * z4 - z2 * y4;
				float num16 = x2 * w4 - w2 * x4;
				float num17 = x2 * z4 - z2 * x4;
				float num18 = x2 * y4 - y2 * x4;
				result.X.Z = (y * num13 - z * num14 + w * num15) * num12;
				result.Y.Z = (0f - (x * num13 - z * num16 + w * num17)) * num12;
				result.Z.Z = (x * num14 - y * num16 + w * num18) * num12;
				result.W.Z = (0f - (x * num15 - y * num17 + z * num18)) * num12;
				float num19 = z2 * w3 - w2 * z3;
				float num20 = y2 * w3 - w2 * y3;
				float num21 = y2 * z3 - z2 * y3;
				float num22 = x2 * w3 - w2 * x3;
				float num23 = x2 * z3 - z2 * x3;
				float num24 = x2 * y3 - y2 * x3;
				result.X.W = (0f - (y * num19 - z * num20 + w * num21)) * num12;
				result.Y.W = (x * num19 - z * num22 + w * num23) * num12;
				result.Z.W = (0f - (x * num20 - y * num22 + w * num24)) * num12;
				result.W.W = (x * num21 - y * num23 + z * num24) * num12;
				return true;
			}
			[CompExactlyDependsOn(typeof(Sse))]
			static bool SseImpl(in Impl matrix, out Impl result)
			{
				if (!Sse.IsSupported)
				{
					throw new PlatformNotSupportedException();
				}
				Vector128<float> left = matrix.X.AsVector128();
				Vector128<float> right = matrix.Y.AsVector128();
				Vector128<float> left2 = matrix.Z.AsVector128();
				Vector128<float> right2 = matrix.W.AsVector128();
				Vector128<float> left3 = Sse.Shuffle(left, right, 68);
				Vector128<float> left4 = Sse.Shuffle(left, right, 238);
				Vector128<float> right3 = Sse.Shuffle(left2, right2, 68);
				Vector128<float> right4 = Sse.Shuffle(left2, right2, 238);
				left = Sse.Shuffle(left3, right3, 136);
				right = Sse.Shuffle(left3, right3, 221);
				left2 = Sse.Shuffle(left4, right4, 136);
				right2 = Sse.Shuffle(left4, right4, 221);
				Vector128<float> vector = Permute(left2, 80);
				Vector128<float> vector2 = Permute(right2, 238);
				Vector128<float> vector3 = Permute(left, 80);
				Vector128<float> vector4 = Permute(right, 238);
				Vector128<float> vector5 = Sse.Shuffle(left2, left, 136);
				Vector128<float> vector6 = Sse.Shuffle(right2, right, 221);
				Vector128<float> vector7 = vector * vector2;
				Vector128<float> vector8 = vector3 * vector4;
				Vector128<float> right5 = vector5 * vector6;
				vector = Permute(left2, 238);
				vector2 = Permute(right2, 80);
				vector3 = Permute(left, 238);
				vector4 = Permute(right, 80);
				vector5 = Sse.Shuffle(left2, left, 221);
				vector6 = Sse.Shuffle(right2, right, 136);
				vector7 -= vector * vector2;
				vector8 -= vector3 * vector4;
				right5 -= vector5 * vector6;
				vector4 = Sse.Shuffle(vector7, right5, 93);
				vector = Permute(right, 73);
				vector2 = Sse.Shuffle(vector4, vector7, 50);
				vector3 = Permute(left, 18);
				vector4 = Sse.Shuffle(vector4, vector7, 153);
				Vector128<float> left5 = Sse.Shuffle(vector8, right5, 253);
				vector5 = Permute(right2, 73);
				vector6 = Sse.Shuffle(left5, vector8, 50);
				Vector128<float> vector9 = Permute(left2, 18);
				left5 = Sse.Shuffle(left5, vector8, 153);
				Vector128<float> vector10 = vector * vector2;
				Vector128<float> vector11 = vector3 * vector4;
				Vector128<float> vector12 = vector5 * vector6;
				Vector128<float> vector13 = vector9 * left5;
				vector4 = Sse.Shuffle(vector7, right5, 4);
				vector = Permute(right, 158);
				vector2 = Sse.Shuffle(vector7, vector4, 147);
				vector3 = Permute(left, 123);
				vector4 = Sse.Shuffle(vector7, vector4, 38);
				left5 = Sse.Shuffle(vector8, right5, 164);
				vector5 = Permute(right2, 158);
				vector6 = Sse.Shuffle(vector8, left5, 147);
				vector9 = Permute(left2, 123);
				left5 = Sse.Shuffle(vector8, left5, 38);
				vector10 -= vector * vector2;
				vector11 -= vector3 * vector4;
				vector12 -= vector5 * vector6;
				vector13 -= vector9 * left5;
				vector = Permute(right, 51);
				vector2 = Sse.Shuffle(vector7, right5, 74);
				vector2 = Permute(vector2, 44);
				vector3 = Permute(left, 141);
				vector4 = Sse.Shuffle(vector7, right5, 76);
				vector4 = Permute(vector4, 147);
				vector5 = Permute(right2, 51);
				vector6 = Sse.Shuffle(vector8, right5, 234);
				vector6 = Permute(vector6, 44);
				vector9 = Permute(left2, 141);
				left5 = Sse.Shuffle(vector8, right5, 236);
				left5 = Permute(left5, 147);
				vector *= vector2;
				vector3 *= vector4;
				vector5 *= vector6;
				vector9 *= left5;
				Vector128<float> right6 = vector10 - vector;
				vector10 += vector;
				Vector128<float> right7 = vector11 + vector3;
				vector11 -= vector3;
				Vector128<float> right8 = vector12 - vector5;
				vector12 += vector5;
				Vector128<float> right9 = vector13 + vector9;
				vector13 -= vector9;
				vector10 = Sse.Shuffle(vector10, right6, 216);
				vector11 = Sse.Shuffle(vector11, right7, 216);
				vector12 = Sse.Shuffle(vector12, right8, 216);
				vector13 = Sse.Shuffle(vector13, right9, 216);
				vector10 = Permute(vector10, 216);
				vector11 = Permute(vector11, 216);
				vector12 = Permute(vector12, 216);
				vector13 = Permute(vector13, 216);
				float num25 = Vector4.Dot(vector10.AsVector4(), left.AsVector4());
				if (MathF.Abs(num25) < float.Epsilon)
				{
					result.W = (result.Z = (result.Y = (result.X = new Vector4(float.NaN))));
					return false;
				}
				Vector128<float> vector14 = Vector128.Create(1f);
				Vector128<float> vector15 = Vector128.Create(num25);
				vector15 = vector14 / vector15;
				result.X = (vector10 * vector15).AsVector4();
				result.Y = (vector11 * vector15).AsVector4();
				result.Z = (vector12 * vector15).AsVector4();
				result.W = (vector13 * vector15).AsVector4();
				return true;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl Lerp(in Impl left, in Impl right, float amount)
		{
			Unsafe.SkipInit(out Impl result);
			result.X = Vector4.Lerp(left.X, right.X, amount);
			result.Y = Vector4.Lerp(left.Y, right.Y, amount);
			result.Z = Vector4.Lerp(left.Z, right.Z, amount);
			result.W = Vector4.Lerp(left.W, right.W, amount);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl Transform(in Impl value, in Quaternion rotation)
		{
			float num = rotation.X + rotation.X;
			float num2 = rotation.Y + rotation.Y;
			float num3 = rotation.Z + rotation.Z;
			float num4 = rotation.W * num;
			float num5 = rotation.W * num2;
			float num6 = rotation.W * num3;
			float num7 = rotation.X * num;
			float num8 = rotation.X * num2;
			float num9 = rotation.X * num3;
			float num10 = rotation.Y * num2;
			float num11 = rotation.Y * num3;
			float num12 = rotation.Z * num3;
			float num13 = 1f - num10 - num12;
			float num14 = num8 - num6;
			float num15 = num9 + num5;
			float num16 = num8 + num6;
			float num17 = 1f - num7 - num12;
			float num18 = num11 - num4;
			float num19 = num9 - num5;
			float num20 = num11 + num4;
			float num21 = 1f - num7 - num10;
			Unsafe.SkipInit(out Impl result);
			result.X = new Vector4(value.X.X * num13 + value.X.Y * num14 + value.X.Z * num15, value.X.X * num16 + value.X.Y * num17 + value.X.Z * num18, value.X.X * num19 + value.X.Y * num20 + value.X.Z * num21, value.X.W);
			result.Y = new Vector4(value.Y.X * num13 + value.Y.Y * num14 + value.Y.Z * num15, value.Y.X * num16 + value.Y.Y * num17 + value.Y.Z * num18, value.Y.X * num19 + value.Y.Y * num20 + value.Y.Z * num21, value.Y.W);
			result.Z = new Vector4(value.Z.X * num13 + value.Z.Y * num14 + value.Z.Z * num15, value.Z.X * num16 + value.Z.Y * num17 + value.Z.Z * num18, value.Z.X * num19 + value.Z.Y * num20 + value.Z.Z * num21, value.Z.W);
			result.W = new Vector4(value.W.X * num13 + value.W.Y * num14 + value.W.Z * num15, value.W.X * num16 + value.W.Y * num17 + value.W.Z * num18, value.W.X * num19 + value.W.Y * num20 + value.W.Z * num21, value.W.W);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Impl Transpose(in Impl matrix)
		{
			if (false)
			{
			}
			Unsafe.SkipInit(out Impl result);
			if (Sse.IsSupported)
			{
				Vector128<float> left = matrix.X.AsVector128();
				Vector128<float> left2 = matrix.Y.AsVector128();
				Vector128<float> right = matrix.Z.AsVector128();
				Vector128<float> right2 = matrix.W.AsVector128();
				Vector128<float> left3 = Sse.UnpackLow(left, right);
				Vector128<float> right3 = Sse.UnpackLow(left2, right2);
				Vector128<float> left4 = Sse.UnpackHigh(left, right);
				Vector128<float> right4 = Sse.UnpackHigh(left2, right2);
				result.X = Sse.UnpackLow(left3, right3).AsVector4();
				result.Y = Sse.UnpackHigh(left3, right3).AsVector4();
				result.Z = Sse.UnpackLow(left4, right4).AsVector4();
				result.W = Sse.UnpackHigh(left4, right4).AsVector4();
			}
			else
			{
				result.X = new Vector4(matrix.X.X, matrix.Y.X, matrix.Z.X, matrix.W.X);
				result.Y = new Vector4(matrix.X.Y, matrix.Y.Y, matrix.Z.Y, matrix.W.Y);
				result.Z = new Vector4(matrix.X.Z, matrix.Y.Z, matrix.Z.Z, matrix.W.Z);
				result.W = new Vector4(matrix.X.W, matrix.Y.W, matrix.Z.W, matrix.W.W);
			}
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override readonly bool Equals([NotNullWhen(true)] object obj)
		{
			if (obj is Matrix4x4 matrix4x)
			{
				return Equals(in matrix4x.AsImpl());
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool Equals(in Impl other)
		{
			if (X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z))
			{
				return W.Equals(other.W);
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly float GetDeterminant()
		{
			float x = X.X;
			float y = X.Y;
			float z = X.Z;
			float w = X.W;
			float x2 = Y.X;
			float y2 = Y.Y;
			float z2 = Y.Z;
			float w2 = Y.W;
			float x3 = Z.X;
			float y3 = Z.Y;
			float z3 = Z.Z;
			float w3 = Z.W;
			float x4 = W.X;
			float y4 = W.Y;
			float z4 = W.Z;
			float w4 = W.W;
			float num = z3 * w4 - w3 * z4;
			float num2 = y3 * w4 - w3 * y4;
			float num3 = y3 * z4 - z3 * y4;
			float num4 = x3 * w4 - w3 * x4;
			float num5 = x3 * z4 - z3 * x4;
			float num6 = x3 * y4 - y3 * x4;
			return x * (y2 * num - z2 * num2 + w2 * num3) - y * (x2 * num - z2 * num4 + w2 * num5) + z * (x2 * num2 - y2 * num4 + w2 * num6) - w * (x2 * num3 - y2 * num5 + z2 * num6);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override readonly int GetHashCode()
		{
			return HashCode.Combine(X, Y, Z, W);
		}

		bool IEquatable<Impl>.Equals(Impl other)
		{
			return Equals(in other);
		}
	}

	public float M11;

	public float M12;

	public float M13;

	public float M14;

	public float M21;

	public float M22;

	public float M23;

	public float M24;

	public float M31;

	public float M32;

	public float M33;

	public float M34;

	public float M41;

	public float M42;

	public float M43;

	public float M44;

	public static Matrix4x4 Identity
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return Impl.Identity.AsM4x4();
		}
	}

	public float this[int row, int column]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		readonly get
		{
			return AsROImpl()[row, column];
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set
		{
			AsImpl()[row, column] = value;
		}
	}

	public readonly bool IsIdentity
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return AsROImpl().IsIdentity;
		}
	}

	public Vector3 Translation
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		readonly get
		{
			return AsROImpl().Translation;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set
		{
			AsImpl().Translation = value;
		}
	}

	public Matrix4x4(float m11, float m12, float m13, float m14, float m21, float m22, float m23, float m24, float m31, float m32, float m33, float m34, float m41, float m42, float m43, float m44)
	{
		Unsafe.SkipInit<Matrix4x4>(out this);
		AsImpl().Init(m11, m12, m13, m14, m21, m22, m23, m24, m31, m32, m33, m34, m41, m42, m43, m44);
	}

	public Matrix4x4(Matrix3x2 value)
	{
		Unsafe.SkipInit<Matrix4x4>(out this);
		AsImpl().Init(in value.AsImpl());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Matrix4x4 operator +(Matrix4x4 value1, Matrix4x4 value2)
	{
		return (value1.AsImpl() + value2.AsImpl()).AsM4x4();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Matrix4x4 value1, Matrix4x4 value2)
	{
		return value1.AsImpl() == value2.AsImpl();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Matrix4x4 value1, Matrix4x4 value2)
	{
		return value1.AsImpl() != value2.AsImpl();
	}

	public static Matrix4x4 operator *(Matrix4x4 value1, Matrix4x4 value2)
	{
		return (value1.AsImpl() * value2.AsImpl()).AsM4x4();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Matrix4x4 operator *(Matrix4x4 value1, float value2)
	{
		return (value1.AsImpl() * value2).AsM4x4();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Matrix4x4 operator -(Matrix4x4 value1, Matrix4x4 value2)
	{
		return (value1.AsImpl() - value2.AsImpl()).AsM4x4();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Matrix4x4 operator -(Matrix4x4 value)
	{
		return (-value.AsImpl()).AsM4x4();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Matrix4x4 Add(Matrix4x4 value1, Matrix4x4 value2)
	{
		return (value1.AsImpl() + value2.AsImpl()).AsM4x4();
	}

	public static Matrix4x4 CreateBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 cameraUpVector, Vector3 cameraForwardVector)
	{
		return Impl.CreateBillboard(in objectPosition, in cameraPosition, in cameraUpVector, in cameraForwardVector).AsM4x4();
	}

	public static Matrix4x4 CreateConstrainedBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 rotateAxis, Vector3 cameraForwardVector, Vector3 objectForwardVector)
	{
		return Impl.CreateConstrainedBillboard(in objectPosition, in cameraPosition, in rotateAxis, in cameraForwardVector, in objectForwardVector).AsM4x4();
	}

	public static Matrix4x4 CreateFromAxisAngle(Vector3 axis, float angle)
	{
		return Impl.CreateFromAxisAngle(in axis, angle).AsM4x4();
	}

	public static Matrix4x4 CreateFromQuaternion(Quaternion quaternion)
	{
		return Impl.CreateFromQuaternion(in quaternion).AsM4x4();
	}

	public static Matrix4x4 CreateFromYawPitchRoll(float yaw, float pitch, float roll)
	{
		return Impl.CreateFromYawPitchRoll(yaw, pitch, roll).AsM4x4();
	}

	public static Matrix4x4 CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
	{
		Vector3 cameraDirection = cameraTarget - cameraPosition;
		return Impl.CreateLookTo(in cameraPosition, in cameraDirection, in cameraUpVector).AsM4x4();
	}

	public static Matrix4x4 CreateLookAtLeftHanded(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
	{
		Vector3 cameraDirection = cameraTarget - cameraPosition;
		return Impl.CreateLookToLeftHanded(in cameraPosition, in cameraDirection, in cameraUpVector).AsM4x4();
	}

	public static Matrix4x4 CreateLookTo(Vector3 cameraPosition, Vector3 cameraDirection, Vector3 cameraUpVector)
	{
		return Impl.CreateLookTo(in cameraPosition, in cameraDirection, in cameraUpVector).AsM4x4();
	}

	public static Matrix4x4 CreateLookToLeftHanded(Vector3 cameraPosition, Vector3 cameraDirection, Vector3 cameraUpVector)
	{
		return Impl.CreateLookToLeftHanded(in cameraPosition, in cameraDirection, in cameraUpVector).AsM4x4();
	}

	public static Matrix4x4 CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane)
	{
		return Impl.CreateOrthographic(width, height, zNearPlane, zFarPlane).AsM4x4();
	}

	public static Matrix4x4 CreateOrthographicLeftHanded(float width, float height, float zNearPlane, float zFarPlane)
	{
		return Impl.CreateOrthographicLeftHanded(width, height, zNearPlane, zFarPlane).AsM4x4();
	}

	public static Matrix4x4 CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
	{
		return Impl.CreateOrthographicOffCenter(left, right, bottom, top, zNearPlane, zFarPlane).AsM4x4();
	}

	public static Matrix4x4 CreateOrthographicOffCenterLeftHanded(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
	{
		return Impl.CreateOrthographicOffCenterLeftHanded(left, right, bottom, top, zNearPlane, zFarPlane).AsM4x4();
	}

	public static Matrix4x4 CreatePerspective(float width, float height, float nearPlaneDistance, float farPlaneDistance)
	{
		return Impl.CreatePerspective(width, height, nearPlaneDistance, farPlaneDistance).AsM4x4();
	}

	public static Matrix4x4 CreatePerspectiveLeftHanded(float width, float height, float nearPlaneDistance, float farPlaneDistance)
	{
		return Impl.CreatePerspectiveLeftHanded(width, height, nearPlaneDistance, farPlaneDistance).AsM4x4();
	}

	public static Matrix4x4 CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
	{
		return Impl.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, nearPlaneDistance, farPlaneDistance).AsM4x4();
	}

	public static Matrix4x4 CreatePerspectiveFieldOfViewLeftHanded(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
	{
		return Impl.CreatePerspectiveFieldOfViewLeftHanded(fieldOfView, aspectRatio, nearPlaneDistance, farPlaneDistance).AsM4x4();
	}

	public static Matrix4x4 CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float nearPlaneDistance, float farPlaneDistance)
	{
		return Impl.CreatePerspectiveOffCenter(left, right, bottom, top, nearPlaneDistance, farPlaneDistance).AsM4x4();
	}

	public static Matrix4x4 CreatePerspectiveOffCenterLeftHanded(float left, float right, float bottom, float top, float nearPlaneDistance, float farPlaneDistance)
	{
		return Impl.CreatePerspectiveOffCenterLeftHanded(left, right, bottom, top, nearPlaneDistance, farPlaneDistance).AsM4x4();
	}

	public static Matrix4x4 CreateReflection(Plane value)
	{
		return Impl.CreateReflection(in value).AsM4x4();
	}

	public static Matrix4x4 CreateRotationX(float radians)
	{
		return Impl.CreateRotationX(radians).AsM4x4();
	}

	public static Matrix4x4 CreateRotationX(float radians, Vector3 centerPoint)
	{
		return Impl.CreateRotationX(radians, in centerPoint).AsM4x4();
	}

	public static Matrix4x4 CreateRotationY(float radians)
	{
		return Impl.CreateRotationY(radians).AsM4x4();
	}

	public static Matrix4x4 CreateRotationY(float radians, Vector3 centerPoint)
	{
		return Impl.CreateRotationY(radians, in centerPoint).AsM4x4();
	}

	public static Matrix4x4 CreateRotationZ(float radians)
	{
		return Impl.CreateRotationZ(radians).AsM4x4();
	}

	public static Matrix4x4 CreateRotationZ(float radians, Vector3 centerPoint)
	{
		return Impl.CreateRotationZ(radians, in centerPoint).AsM4x4();
	}

	public static Matrix4x4 CreateScale(float xScale, float yScale, float zScale)
	{
		return Impl.CreateScale(xScale, yScale, zScale).AsM4x4();
	}

	public static Matrix4x4 CreateScale(float xScale, float yScale, float zScale, Vector3 centerPoint)
	{
		return Impl.CreateScale(xScale, yScale, zScale, in centerPoint).AsM4x4();
	}

	public static Matrix4x4 CreateScale(Vector3 scales)
	{
		return Impl.CreateScale(in scales).AsM4x4();
	}

	public static Matrix4x4 CreateScale(Vector3 scales, Vector3 centerPoint)
	{
		return Impl.CreateScale(in scales, in centerPoint).AsM4x4();
	}

	public static Matrix4x4 CreateScale(float scale)
	{
		return Impl.CreateScale(scale).AsM4x4();
	}

	public static Matrix4x4 CreateScale(float scale, Vector3 centerPoint)
	{
		return Impl.CreateScale(scale, in centerPoint).AsM4x4();
	}

	public static Matrix4x4 CreateShadow(Vector3 lightDirection, Plane plane)
	{
		return Impl.CreateShadow(in lightDirection, in plane).AsM4x4();
	}

	public static Matrix4x4 CreateTranslation(Vector3 position)
	{
		return Impl.CreateTranslation(in position).AsM4x4();
	}

	public static Matrix4x4 CreateTranslation(float xPosition, float yPosition, float zPosition)
	{
		return Impl.CreateTranslation(xPosition, yPosition, zPosition).AsM4x4();
	}

	public static Matrix4x4 CreateViewport(float x, float y, float width, float height, float minDepth, float maxDepth)
	{
		return Impl.CreateViewport(x, y, width, height, minDepth, maxDepth).AsM4x4();
	}

	public static Matrix4x4 CreateViewportLeftHanded(float x, float y, float width, float height, float minDepth, float maxDepth)
	{
		return Impl.CreateViewportLeftHanded(x, y, width, height, minDepth, maxDepth).AsM4x4();
	}

	public static Matrix4x4 CreateWorld(Vector3 position, Vector3 forward, Vector3 up)
	{
		return Impl.CreateWorld(in position, in forward, in up).AsM4x4();
	}

	public static bool Decompose(Matrix4x4 matrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation)
	{
		return Impl.Decompose(in matrix.AsImpl(), out scale, out rotation, out translation);
	}

	public static bool Invert(Matrix4x4 matrix, out Matrix4x4 result)
	{
		Unsafe.SkipInit<Matrix4x4>(out result);
		return Impl.Invert(in matrix.AsImpl(), out result.AsImpl());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Matrix4x4 Lerp(Matrix4x4 matrix1, Matrix4x4 matrix2, float amount)
	{
		return Impl.Lerp(in matrix1.AsImpl(), in matrix2.AsImpl(), amount).AsM4x4();
	}

	public static Matrix4x4 Multiply(Matrix4x4 value1, Matrix4x4 value2)
	{
		return (value1.AsImpl() * value2.AsImpl()).AsM4x4();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Matrix4x4 Multiply(Matrix4x4 value1, float value2)
	{
		return (value1.AsImpl() * value2).AsM4x4();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Matrix4x4 Negate(Matrix4x4 value)
	{
		return (-value.AsImpl()).AsM4x4();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Matrix4x4 Subtract(Matrix4x4 value1, Matrix4x4 value2)
	{
		return (value1.AsImpl() - value2.AsImpl()).AsM4x4();
	}

	public static Matrix4x4 Transform(Matrix4x4 value, Quaternion rotation)
	{
		return Impl.Transform(in value.AsImpl(), in rotation).AsM4x4();
	}

	public static Matrix4x4 Transpose(Matrix4x4 matrix)
	{
		return Impl.Transpose(in matrix.AsImpl()).AsM4x4();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override readonly bool Equals([NotNullWhen(true)] object? obj)
	{
		return AsROImpl().Equals(obj);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Equals(Matrix4x4 other)
	{
		return AsROImpl().Equals(in other.AsImpl());
	}

	public readonly float GetDeterminant()
	{
		return AsROImpl().GetDeterminant();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override readonly int GetHashCode()
	{
		return AsROImpl().GetHashCode();
	}

	public override readonly string ToString()
	{
		return $"{{ {{M11:{M11} M12:{M12} M13:{M13} M14:{M14}}} {{M21:{M21} M22:{M22} M23:{M23} M24:{M24}}} {{M31:{M31} M32:{M32} M33:{M33} M34:{M34}}} {{M41:{M41} M42:{M42} M43:{M43} M44:{M44}}} }}";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[UnscopedRef]
	internal ref Impl AsImpl()
	{
		return ref Unsafe.As<Matrix4x4, Impl>(ref this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[UnscopedRef]
	internal readonly ref readonly Impl AsROImpl()
	{
		return ref Unsafe.As<Matrix4x4, Impl>(ref Unsafe.AsRef(ref this));
	}
}
