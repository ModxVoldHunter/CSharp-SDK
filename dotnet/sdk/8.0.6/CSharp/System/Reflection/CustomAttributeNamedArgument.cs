using System.Diagnostics.CodeAnalysis;

namespace System.Reflection;

public readonly struct CustomAttributeNamedArgument : IEquatable<CustomAttributeNamedArgument>
{
	private readonly MemberInfo _memberInfo;

	private readonly CustomAttributeTypedArgument _value;

	internal Type ArgumentType
	{
		get
		{
			if (!(_memberInfo is FieldInfo fieldInfo))
			{
				return ((PropertyInfo)_memberInfo).PropertyType;
			}
			return fieldInfo.FieldType;
		}
	}

	public MemberInfo MemberInfo => _memberInfo;

	public CustomAttributeTypedArgument TypedValue => _value;

	public string MemberName => MemberInfo.Name;

	public bool IsField => MemberInfo is FieldInfo;

	public static bool operator ==(CustomAttributeNamedArgument left, CustomAttributeNamedArgument right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(CustomAttributeNamedArgument left, CustomAttributeNamedArgument right)
	{
		return !left.Equals(right);
	}

	public CustomAttributeNamedArgument(MemberInfo memberInfo, object? value)
	{
		ArgumentNullException.ThrowIfNull(memberInfo, "memberInfo");
		Type type;
		if (!(memberInfo is FieldInfo fieldInfo))
		{
			if (!(memberInfo is PropertyInfo propertyInfo))
			{
				throw new ArgumentException(SR.Argument_InvalidMemberForNamedArgument);
			}
			type = propertyInfo.PropertyType;
		}
		else
		{
			type = fieldInfo.FieldType;
		}
		Type argumentType = type;
		_memberInfo = memberInfo;
		_value = new CustomAttributeTypedArgument(argumentType, value);
	}

	public CustomAttributeNamedArgument(MemberInfo memberInfo, CustomAttributeTypedArgument typedArgument)
	{
		ArgumentNullException.ThrowIfNull(memberInfo, "memberInfo");
		_memberInfo = memberInfo;
		_value = typedArgument;
	}

	public override string ToString()
	{
		if ((object)_memberInfo == null)
		{
			return base.ToString();
		}
		return MemberInfo.Name + " = " + TypedValue.ToString(ArgumentType != typeof(object));
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is CustomAttributeNamedArgument other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(CustomAttributeNamedArgument other)
	{
		if (_memberInfo == other._memberInfo)
		{
			return _value == other._value;
		}
		return false;
	}
}
