using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Reflection.Emit;

internal sealed class SymbolType : TypeInfo
{
	internal Type _baseType;

	internal TypeKind _typeKind;

	internal int _rank;

	internal int[] _iaLowerBound;

	internal int[] _iaUpperBound;

	private string _format;

	private bool _isSzArray = true;

	public override bool IsTypeDefinition => false;

	public override bool IsSZArray
	{
		get
		{
			if (_rank <= 1)
			{
				return _isSzArray;
			}
			return false;
		}
	}

	public override Guid GUID
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_NonReflectedType);
		}
	}

	public override Module Module
	{
		get
		{
			Type baseType = _baseType;
			while (baseType is SymbolType)
			{
				baseType = ((SymbolType)baseType)._baseType;
			}
			return baseType.Module;
		}
	}

	public override Assembly Assembly
	{
		get
		{
			Type baseType = _baseType;
			while (baseType is SymbolType)
			{
				baseType = ((SymbolType)baseType)._baseType;
			}
			return baseType.Assembly;
		}
	}

	public override RuntimeTypeHandle TypeHandle
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_NonReflectedType);
		}
	}

	public override string Name
	{
		get
		{
			string text = _format;
			Type baseType = _baseType;
			while (baseType is SymbolType)
			{
				text = ((SymbolType)baseType)._format + text;
				baseType = ((SymbolType)baseType)._baseType;
			}
			return baseType.Name + text;
		}
	}

	public override string FullName => TypeNameBuilder.ToString(this, TypeNameBuilder.Format.FullName);

	public override string AssemblyQualifiedName => TypeNameBuilder.ToString(this, TypeNameBuilder.Format.AssemblyQualifiedName);

	public override string Namespace => _baseType.Namespace;

	public override Type BaseType => typeof(Array);

	public override bool IsConstructedGenericType => false;

	public override Type UnderlyingSystemType => this;

	public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo typeInfo)
	{
		if (typeInfo == null)
		{
			return false;
		}
		return IsAssignableFrom(typeInfo.AsType());
	}

	internal static Type FormCompoundType(string format, Type baseType, int curIndex)
	{
		if (format == null || curIndex == format.Length)
		{
			return baseType;
		}
		if (format[curIndex] == '&')
		{
			SymbolType symbolType = new SymbolType(baseType, TypeKind.IsByRef);
			symbolType.SetFormat(format, curIndex, 1);
			curIndex++;
			if (curIndex != format.Length)
			{
				throw new ArgumentException(SR.Argument_BadSigFormat);
			}
			return symbolType;
		}
		if (format[curIndex] == '[')
		{
			SymbolType symbolType = new SymbolType(baseType, TypeKind.IsArray);
			int num = curIndex;
			curIndex++;
			int num2 = 0;
			int num3 = -1;
			while (format[curIndex] != ']')
			{
				if (format[curIndex] == '*')
				{
					symbolType._isSzArray = false;
					curIndex++;
				}
				if (char.IsAsciiDigit(format[curIndex]) || format[curIndex] == '-')
				{
					bool flag = false;
					if (format[curIndex] == '-')
					{
						flag = true;
						curIndex++;
					}
					while (char.IsAsciiDigit(format[curIndex]))
					{
						num2 *= 10;
						num2 += format[curIndex] - 48;
						curIndex++;
					}
					if (flag)
					{
						num2 = -num2;
					}
					num3 = num2 - 1;
				}
				if (format[curIndex] == '.')
				{
					curIndex++;
					if (format[curIndex] != '.')
					{
						throw new ArgumentException(SR.Argument_BadSigFormat);
					}
					curIndex++;
					if (char.IsAsciiDigit(format[curIndex]) || format[curIndex] == '-')
					{
						bool flag2 = false;
						num3 = 0;
						if (format[curIndex] == '-')
						{
							flag2 = true;
							curIndex++;
						}
						while (char.IsAsciiDigit(format[curIndex]))
						{
							num3 *= 10;
							num3 += format[curIndex] - 48;
							curIndex++;
						}
						if (flag2)
						{
							num3 = -num3;
						}
						if (num3 < num2)
						{
							throw new ArgumentException(SR.Argument_BadSigFormat);
						}
					}
				}
				if (format[curIndex] == ',')
				{
					curIndex++;
					symbolType.SetBounds(num2, num3);
					num2 = 0;
					num3 = -1;
				}
				else if (format[curIndex] != ']')
				{
					throw new ArgumentException(SR.Argument_BadSigFormat);
				}
			}
			symbolType.SetBounds(num2, num3);
			curIndex++;
			symbolType.SetFormat(format, num, curIndex - num);
			return FormCompoundType(format, symbolType, curIndex);
		}
		if (format[curIndex] == '*')
		{
			SymbolType symbolType = new SymbolType(baseType, TypeKind.IsPointer);
			symbolType.SetFormat(format, curIndex, 1);
			curIndex++;
			return FormCompoundType(format, symbolType, curIndex);
		}
		return null;
	}

	internal SymbolType(Type baseType, TypeKind typeKind)
	{
		ArgumentNullException.ThrowIfNull(baseType, "baseType");
		_baseType = baseType;
		_typeKind = typeKind;
		_iaLowerBound = new int[4];
		_iaUpperBound = new int[4];
	}

	private void SetBounds(int lower, int upper)
	{
		if (lower != 0 || upper != -1)
		{
			_isSzArray = false;
		}
		if (_iaLowerBound.Length <= _rank)
		{
			int[] array = new int[_rank * 2];
			Array.Copy(_iaLowerBound, array, _rank);
			_iaLowerBound = array;
			Array.Copy(_iaUpperBound, array, _rank);
			_iaUpperBound = array;
		}
		_iaLowerBound[_rank] = lower;
		_iaUpperBound[_rank] = upper;
		_rank++;
	}

	internal void SetFormat(string format, int curIndex, int length)
	{
		_format = format.Substring(curIndex, length);
	}

	public override Type MakePointerType()
	{
		return FormCompoundType(_format + "*", _baseType, 0);
	}

	public override Type MakeByRefType()
	{
		return FormCompoundType(_format + "&", _baseType, 0);
	}

	public override Type MakeArrayType()
	{
		return FormCompoundType(_format + "[]", _baseType, 0);
	}

	public override Type MakeArrayType(int rank)
	{
		string rankString = TypeInfo.GetRankString(rank);
		return FormCompoundType(_format + rankString, _baseType, 0) as SymbolType;
	}

	public override int GetArrayRank()
	{
		if (!base.IsArray)
		{
			throw new NotSupportedException(SR.NotSupported_SubclassOverride);
		}
		return _rank;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	public override string ToString()
	{
		return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.ToString);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public override FieldInfo GetField(string name, BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public override FieldInfo[] GetFields(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public override Type GetInterface(string name, bool ignoreCase)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public override Type[] GetInterfaces()
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
	public override EventInfo[] GetEvents()
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public override Type[] GetNestedTypes(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public override Type GetNestedType(string name, BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo[] GetEvents(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	protected override TypeAttributes GetAttributeFlagsImpl()
	{
		Type baseType = _baseType;
		while (baseType is SymbolType)
		{
			baseType = ((SymbolType)baseType)._baseType;
		}
		return baseType.Attributes;
	}

	protected override bool IsArrayImpl()
	{
		return _typeKind == TypeKind.IsArray;
	}

	protected override bool IsPointerImpl()
	{
		return _typeKind == TypeKind.IsPointer;
	}

	protected override bool IsByRefImpl()
	{
		return _typeKind == TypeKind.IsByRef;
	}

	protected override bool IsPrimitiveImpl()
	{
		return false;
	}

	protected override bool IsValueTypeImpl()
	{
		return false;
	}

	protected override bool IsCOMObjectImpl()
	{
		return false;
	}

	public override Type GetElementType()
	{
		return _baseType;
	}

	protected override bool HasElementTypeImpl()
	{
		return _baseType != null;
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}
}
