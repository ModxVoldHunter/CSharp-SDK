using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Diagnostics.Tracing;

internal readonly struct PropertyValue
{
	[StructLayout(LayoutKind.Explicit)]
	public struct Scalar
	{
		[FieldOffset(0)]
		public bool AsBoolean;

		[FieldOffset(0)]
		public byte AsByte;

		[FieldOffset(0)]
		public sbyte AsSByte;

		[FieldOffset(0)]
		public char AsChar;

		[FieldOffset(0)]
		public short AsInt16;

		[FieldOffset(0)]
		public ushort AsUInt16;

		[FieldOffset(0)]
		public int AsInt32;

		[FieldOffset(0)]
		public uint AsUInt32;

		[FieldOffset(0)]
		public long AsInt64;

		[FieldOffset(0)]
		public ulong AsUInt64;

		[FieldOffset(0)]
		public nint AsIntPtr;

		[FieldOffset(0)]
		public nuint AsUIntPtr;

		[FieldOffset(0)]
		public float AsSingle;

		[FieldOffset(0)]
		public double AsDouble;

		[FieldOffset(0)]
		public Guid AsGuid;

		[FieldOffset(0)]
		public DateTime AsDateTime;

		[FieldOffset(0)]
		public DateTimeOffset AsDateTimeOffset;

		[FieldOffset(0)]
		public TimeSpan AsTimeSpan;

		[FieldOffset(0)]
		public decimal AsDecimal;
	}

	private abstract class TypeHelper
	{
		public abstract Func<PropertyValue, PropertyValue> GetPropertyGetter(PropertyInfo property);

		[UnconditionalSuppressMessage("AotAnalysis", "IL3050:AotUnfriendlyApi", Justification = "Instantiation over a reference type. See comments above.")]
		protected static Delegate GetGetMethod(PropertyInfo property, Type propertyType)
		{
			return property.GetMethod.CreateDelegate(typeof(Func<, >).MakeGenericType(property.DeclaringType, propertyType));
		}
	}

	private sealed class ReferenceTypeHelper<TContainer> : TypeHelper where TContainer : class
	{
		private static Func<TContainer, TProperty> GetGetMethod<TProperty>(PropertyInfo property) where TProperty : struct
		{
			return property.GetMethod.CreateDelegate<Func<TContainer, TProperty>>();
		}

		public override Func<PropertyValue, PropertyValue> GetPropertyGetter(PropertyInfo property)
		{
			Type type = property.PropertyType;
			if (!type.IsValueType)
			{
				Func<TContainer, object> getter = (Func<TContainer, object>)TypeHelper.GetGetMethod(property, type);
				return (PropertyValue container) => new PropertyValue(getter((TContainer)container.ReferenceValue));
			}
			if (type.IsEnum)
			{
				type = Enum.GetUnderlyingType(type);
			}
			if (type == typeof(bool))
			{
				Func<TContainer, bool> f = GetGetMethod<bool>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			if (type == typeof(byte))
			{
				Func<TContainer, byte> f = GetGetMethod<byte>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			if (type == typeof(sbyte))
			{
				Func<TContainer, sbyte> f = GetGetMethod<sbyte>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			if (type == typeof(char))
			{
				Func<TContainer, char> f = GetGetMethod<char>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			if (type == typeof(short))
			{
				Func<TContainer, short> f = GetGetMethod<short>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			if (type == typeof(ushort))
			{
				Func<TContainer, ushort> f = GetGetMethod<ushort>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			if (type == typeof(int))
			{
				Func<TContainer, int> f = GetGetMethod<int>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			if (type == typeof(uint))
			{
				Func<TContainer, uint> f = GetGetMethod<uint>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			if (type == typeof(long))
			{
				Func<TContainer, long> f = GetGetMethod<long>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			if (type == typeof(ulong))
			{
				Func<TContainer, ulong> f = GetGetMethod<ulong>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			if (type == typeof(nint))
			{
				Func<TContainer, nint> f = GetGetMethod<nint>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			if (type == typeof(nuint))
			{
				Func<TContainer, nuint> f = GetGetMethod<nuint>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			if (type == typeof(float))
			{
				Func<TContainer, float> f = GetGetMethod<float>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			if (type == typeof(double))
			{
				Func<TContainer, double> f = GetGetMethod<double>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			if (type == typeof(Guid))
			{
				Func<TContainer, Guid> f = GetGetMethod<Guid>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			if (type == typeof(DateTime))
			{
				Func<TContainer, DateTime> f = GetGetMethod<DateTime>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			if (type == typeof(DateTimeOffset))
			{
				Func<TContainer, DateTimeOffset> f = GetGetMethod<DateTimeOffset>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			if (type == typeof(TimeSpan))
			{
				Func<TContainer, TimeSpan> f = GetGetMethod<TimeSpan>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			if (type == typeof(decimal))
			{
				Func<TContainer, decimal> f = GetGetMethod<decimal>(property);
				return (PropertyValue container) => new PropertyValue(f((TContainer)container.ReferenceValue));
			}
			return (PropertyValue container) => new PropertyValue(property.GetValue(container.ReferenceValue));
		}
	}

	private readonly object _reference;

	private readonly Scalar _scalar;

	private readonly int _scalarLength;

	public object ReferenceValue => _reference;

	public Scalar ScalarValue => _scalar;

	public int ScalarLength => _scalarLength;

	private PropertyValue(object value)
	{
		_reference = value;
		_scalar = default(Scalar);
		_scalarLength = 0;
	}

	private PropertyValue(Scalar scalar, int scalarLength)
	{
		_reference = null;
		_scalar = scalar;
		_scalarLength = scalarLength;
	}

	private PropertyValue(bool value)
		: this(new Scalar
		{
			AsBoolean = value
		}, 1)
	{
	}

	private PropertyValue(byte value)
		: this(new Scalar
		{
			AsByte = value
		}, 1)
	{
	}

	private PropertyValue(sbyte value)
		: this(new Scalar
		{
			AsSByte = value
		}, 1)
	{
	}

	private PropertyValue(char value)
		: this(new Scalar
		{
			AsChar = value
		}, 2)
	{
	}

	private PropertyValue(short value)
		: this(new Scalar
		{
			AsInt16 = value
		}, 2)
	{
	}

	private PropertyValue(ushort value)
		: this(new Scalar
		{
			AsUInt16 = value
		}, 2)
	{
	}

	private PropertyValue(int value)
		: this(new Scalar
		{
			AsInt32 = value
		}, 4)
	{
	}

	private PropertyValue(uint value)
		: this(new Scalar
		{
			AsUInt32 = value
		}, 4)
	{
	}

	private PropertyValue(long value)
		: this(new Scalar
		{
			AsInt64 = value
		}, 8)
	{
	}

	private PropertyValue(ulong value)
		: this(new Scalar
		{
			AsUInt64 = value
		}, 8)
	{
	}

	private PropertyValue(nint value)
		: this(new Scalar
		{
			AsIntPtr = value
		}, 8)
	{
	}

	private PropertyValue(nuint value)
		: this(new Scalar
		{
			AsUIntPtr = value
		}, 8)
	{
	}

	private PropertyValue(float value)
		: this(new Scalar
		{
			AsSingle = value
		}, 4)
	{
	}

	private PropertyValue(double value)
		: this(new Scalar
		{
			AsDouble = value
		}, 8)
	{
	}

	private unsafe PropertyValue(Guid value)
		: this(new Scalar
		{
			AsGuid = value
		}, sizeof(Guid))
	{
	}

	private unsafe PropertyValue(DateTime value)
		: this(new Scalar
		{
			AsDateTime = value
		}, sizeof(DateTime))
	{
	}

	private unsafe PropertyValue(DateTimeOffset value)
		: this(new Scalar
		{
			AsDateTimeOffset = value
		}, sizeof(DateTimeOffset))
	{
	}

	private unsafe PropertyValue(TimeSpan value)
		: this(new Scalar
		{
			AsTimeSpan = value
		}, sizeof(TimeSpan))
	{
	}

	private PropertyValue(decimal value)
		: this(new Scalar
		{
			AsDecimal = value
		}, 16)
	{
	}

	public static Func<object, PropertyValue> GetFactory(Type type)
	{
		if (type == typeof(bool))
		{
			return (object value) => new PropertyValue((bool)value);
		}
		if (type == typeof(byte))
		{
			return (object value) => new PropertyValue((byte)value);
		}
		if (type == typeof(sbyte))
		{
			return (object value) => new PropertyValue((sbyte)value);
		}
		if (type == typeof(char))
		{
			return (object value) => new PropertyValue((char)value);
		}
		if (type == typeof(short))
		{
			return (object value) => new PropertyValue((short)value);
		}
		if (type == typeof(ushort))
		{
			return (object value) => new PropertyValue((ushort)value);
		}
		if (type == typeof(int))
		{
			return (object value) => new PropertyValue((int)value);
		}
		if (type == typeof(uint))
		{
			return (object value) => new PropertyValue((uint)value);
		}
		if (type == typeof(long))
		{
			return (object value) => new PropertyValue((long)value);
		}
		if (type == typeof(ulong))
		{
			return (object value) => new PropertyValue((ulong)value);
		}
		if (type == typeof(nint))
		{
			return (object value) => new PropertyValue((nint)value);
		}
		if (type == typeof(nuint))
		{
			return (object value) => new PropertyValue((nuint)value);
		}
		if (type == typeof(float))
		{
			return (object value) => new PropertyValue((float)value);
		}
		if (type == typeof(double))
		{
			return (object value) => new PropertyValue((double)value);
		}
		if (type == typeof(Guid))
		{
			return (object value) => new PropertyValue((Guid)value);
		}
		if (type == typeof(DateTime))
		{
			return (object value) => new PropertyValue((DateTime)value);
		}
		if (type == typeof(DateTimeOffset))
		{
			return (object value) => new PropertyValue((DateTimeOffset)value);
		}
		if (type == typeof(TimeSpan))
		{
			return (object value) => new PropertyValue((TimeSpan)value);
		}
		if (type == typeof(decimal))
		{
			return (object value) => new PropertyValue((decimal)value);
		}
		return (object value) => new PropertyValue(value);
	}

	public static Func<PropertyValue, PropertyValue> GetPropertyGetter(PropertyInfo property)
	{
		if (property.DeclaringType.IsValueType)
		{
			return GetBoxedValueTypePropertyGetter(property);
		}
		return GetReferenceTypePropertyGetter(property);
	}

	private static Func<PropertyValue, PropertyValue> GetBoxedValueTypePropertyGetter(PropertyInfo property)
	{
		Type type = property.PropertyType;
		if (type.IsEnum)
		{
			type = Enum.GetUnderlyingType(type);
		}
		Func<object, PropertyValue> factory = GetFactory(type);
		return (PropertyValue container) => factory(property.GetValue(container.ReferenceValue));
	}

	[UnconditionalSuppressMessage("AotAnalysis", "IL3050:AotUnfriendlyApi", Justification = "Instantiation over a reference type. See comments above.")]
	private static Func<PropertyValue, PropertyValue> GetReferenceTypePropertyGetter(PropertyInfo property)
	{
		TypeHelper typeHelper = (TypeHelper)Activator.CreateInstance(typeof(ReferenceTypeHelper<>).MakeGenericType(property.DeclaringType));
		return typeHelper.GetPropertyGetter(property);
	}
}
