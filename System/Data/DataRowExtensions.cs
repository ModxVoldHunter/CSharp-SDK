using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Data;

public static class DataRowExtensions
{
	private static class UnboxT<T>
	{
		internal static readonly Func<object, T> s_unbox = Create();

		private static Func<object, T> Create()
		{
			if (typeof(T).IsValueType && default(T) == null)
			{
				if (!RuntimeFeature.IsDynamicCodeSupported)
				{
					return NullableFieldUsingReflection;
				}
				return CreateWhenDynamicCodeSupported();
			}
			return NonNullableField;
			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2090:MakeGenericMethod", Justification = "'NullableField<TElem> where TElem : struct' implies 'TElem : new()'. Nullable does not make use of new() so it is safe.The warning is only issued when IsDynamicCodeSupported is true.")]
			static Func<object, T> CreateWhenDynamicCodeSupported()
			{
				return typeof(UnboxT<T>).GetMethod("NullableField", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(Nullable.GetUnderlyingType(typeof(T))).CreateDelegate<Func<object, T>>();
			}
		}

		private static T NonNullableField(object value)
		{
			if (value == DBNull.Value)
			{
				if (default(T) == null)
				{
					return default(T);
				}
				throw DataSetUtil.InvalidCast(System.SR.Format(System.SR.DataSetLinq_NonNullableCast, typeof(T)));
			}
			return (T)value;
		}

		private static T NullableFieldUsingReflection(object value)
		{
			if (value == DBNull.Value)
			{
				return default(T);
			}
			if (value is T)
			{
				return (T)value;
			}
			Type type = value.GetType();
			Type underlyingType = Nullable.GetUnderlyingType(typeof(T));
			Type type2 = (type.IsEnum ? Enum.GetUnderlyingType(type) : type);
			Type type3 = (underlyingType.IsEnum ? Enum.GetUnderlyingType(underlyingType) : underlyingType);
			if (type2 == type3)
			{
				value = (underlyingType.IsEnum ? Enum.ToObject(underlyingType, value) : Convert.ChangeType(value, underlyingType, null));
			}
			return (T)value;
		}

		private static TElem? NullableField<TElem>(object value) where TElem : struct
		{
			if (value != DBNull.Value)
			{
				return (TElem)value;
			}
			return null;
		}
	}

	public static T? Field<T>(this DataRow row, string columnName)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		return UnboxT<T>.s_unbox(row[columnName]);
	}

	public static T? Field<T>(this DataRow row, DataColumn column)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		return UnboxT<T>.s_unbox(row[column]);
	}

	public static T? Field<T>(this DataRow row, int columnIndex)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		return UnboxT<T>.s_unbox(row[columnIndex]);
	}

	public static T? Field<T>(this DataRow row, int columnIndex, DataRowVersion version)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		return UnboxT<T>.s_unbox(row[columnIndex, version]);
	}

	public static T? Field<T>(this DataRow row, string columnName, DataRowVersion version)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		return UnboxT<T>.s_unbox(row[columnName, version]);
	}

	public static T? Field<T>(this DataRow row, DataColumn column, DataRowVersion version)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		return UnboxT<T>.s_unbox(row[column, version]);
	}

	public static void SetField<T>(this DataRow row, int columnIndex, T? value)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		row[columnIndex] = ((object)value) ?? DBNull.Value;
	}

	public static void SetField<T>(this DataRow row, string columnName, T? value)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		row[columnName] = ((object)value) ?? DBNull.Value;
	}

	public static void SetField<T>(this DataRow row, DataColumn column, T? value)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		row[column] = ((object)value) ?? DBNull.Value;
	}
}
