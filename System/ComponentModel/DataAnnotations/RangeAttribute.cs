using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class RangeAttribute : ValidationAttribute
{
	public object Minimum { get; private set; }

	public object Maximum { get; private set; }

	public bool MinimumIsExclusive { get; set; }

	public bool MaximumIsExclusive { get; set; }

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public Type OperandType { get; }

	public bool ParseLimitsInInvariantCulture { get; set; }

	public bool ConvertValueInInvariantCulture { get; set; }

	private Func<object, object?>? Conversion { get; set; }

	public RangeAttribute(int minimum, int maximum)
		: base(populateErrorMessageResourceAccessor: false)
	{
		Minimum = minimum;
		Maximum = maximum;
		OperandType = typeof(int);
		base.ErrorMessageResourceAccessor = GetValidationErrorMessage;
	}

	public RangeAttribute(double minimum, double maximum)
		: base(populateErrorMessageResourceAccessor: false)
	{
		Minimum = minimum;
		Maximum = maximum;
		OperandType = typeof(double);
		base.ErrorMessageResourceAccessor = GetValidationErrorMessage;
	}

	[RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
	public RangeAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, string minimum, string maximum)
		: base(populateErrorMessageResourceAccessor: false)
	{
		OperandType = type;
		Minimum = minimum;
		Maximum = maximum;
		base.ErrorMessageResourceAccessor = GetValidationErrorMessage;
	}

	private void Initialize(IComparable minimum, IComparable maximum, Func<object, object> conversion)
	{
		int num = minimum.CompareTo(maximum);
		if (num > 0)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.RangeAttribute_MinGreaterThanMax, maximum, minimum));
		}
		if (num == 0 && (MinimumIsExclusive || MaximumIsExclusive))
		{
			throw new InvalidOperationException(System.SR.RangeAttribute_CannotUseExclusiveBoundsWhenTheyAreEqual);
		}
		Minimum = minimum;
		Maximum = maximum;
		Conversion = conversion;
	}

	public override bool IsValid(object? value)
	{
		SetupConversion();
		if ((value == null || value is string { Length: 0 }) ? true : false)
		{
			return true;
		}
		object obj;
		try
		{
			obj = Conversion(value);
		}
		catch (FormatException)
		{
			return false;
		}
		catch (InvalidCastException)
		{
			return false;
		}
		catch (NotSupportedException)
		{
			return false;
		}
		IComparable comparable = (IComparable)Minimum;
		IComparable comparable2 = (IComparable)Maximum;
		if (MinimumIsExclusive ? (comparable.CompareTo(obj) < 0) : (comparable.CompareTo(obj) <= 0))
		{
			if (!MaximumIsExclusive)
			{
				return comparable2.CompareTo(obj) >= 0;
			}
			return comparable2.CompareTo(obj) > 0;
		}
		return false;
	}

	public override string FormatErrorMessage(string name)
	{
		SetupConversion();
		return string.Format(CultureInfo.CurrentCulture, base.ErrorMessageString, name, Minimum, Maximum);
	}

	private void SetupConversion()
	{
		if (Conversion != null)
		{
			return;
		}
		object minimum = Minimum;
		object maximum = Maximum;
		if (minimum == null || maximum == null)
		{
			throw new InvalidOperationException(System.SR.RangeAttribute_Must_Set_Min_And_Max);
		}
		Type type2 = minimum.GetType();
		if (type2 == typeof(int))
		{
			Initialize((int)minimum, (int)maximum, (object v) => Convert.ToInt32(v, CultureInfo.InvariantCulture));
			return;
		}
		if (type2 == typeof(double))
		{
			Initialize((double)minimum, (double)maximum, (object v) => Convert.ToDouble(v, CultureInfo.InvariantCulture));
			return;
		}
		Type type = OperandType;
		if (type == null)
		{
			throw new InvalidOperationException(System.SR.RangeAttribute_Must_Set_Operand_Type);
		}
		Type typeFromHandle = typeof(IComparable);
		if (!typeFromHandle.IsAssignableFrom(type))
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.RangeAttribute_ArbitraryTypeNotIComparable, type.FullName, typeFromHandle.FullName));
		}
		TypeConverter converter = GetOperandTypeConverter();
		IComparable minimum2 = (IComparable)(ParseLimitsInInvariantCulture ? converter.ConvertFromInvariantString((string)minimum) : converter.ConvertFromString((string)minimum));
		IComparable maximum2 = (IComparable)(ParseLimitsInInvariantCulture ? converter.ConvertFromInvariantString((string)maximum) : converter.ConvertFromString((string)maximum));
		Func<object, object> conversion = ((!ConvertValueInInvariantCulture) ? ((Func<object, object>)((object value) => (!(value.GetType() == type)) ? converter.ConvertFrom(value) : value)) : ((Func<object, object>)((object value) => (!(value.GetType() == type)) ? converter.ConvertFrom(null, CultureInfo.InvariantCulture, value) : value)));
		Initialize(minimum2, maximum2, conversion);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor that allows this code to be called is marked with RequiresUnreferencedCode.")]
	private TypeConverter GetOperandTypeConverter()
	{
		return TypeDescriptor.GetConverter(OperandType);
	}

	private string GetValidationErrorMessage()
	{
		bool minimumIsExclusive = MinimumIsExclusive;
		bool maximumIsExclusive = MaximumIsExclusive;
		if (!minimumIsExclusive)
		{
			if (!maximumIsExclusive)
			{
				return System.SR.RangeAttribute_ValidationError;
			}
			return System.SR.RangeAttribute_ValidationError_MaxExclusive;
		}
		if (!maximumIsExclusive)
		{
			return System.SR.RangeAttribute_ValidationError_MinExclusive;
		}
		return System.SR.RangeAttribute_ValidationError_MinExclusive_MaxExclusive;
	}
}
