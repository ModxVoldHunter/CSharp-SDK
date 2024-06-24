using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Runtime.Serialization.DataContracts;

public sealed class DataMember
{
	private sealed class CriticalHelper
	{
		private DataContract _memberTypeContract;

		private string _name;

		private long _order;

		private bool _isRequired;

		private bool _emitDefaultValue;

		private bool _isNullable;

		private bool _isGetOnlyCollection;

		private readonly MemberInfo _memberInfo;

		private Type _memberType;

		private bool _hasConflictingNameAndType;

		private DataMember _conflictingMember;

		private PrimitiveDataContract _memberPrimitiveContract;

		internal MemberInfo MemberInfo => _memberInfo;

		internal string Name
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
			}
		}

		internal long Order
		{
			get
			{
				return _order;
			}
			set
			{
				_order = value;
			}
		}

		internal bool IsRequired
		{
			get
			{
				return _isRequired;
			}
			set
			{
				_isRequired = value;
			}
		}

		internal bool EmitDefaultValue
		{
			get
			{
				return _emitDefaultValue;
			}
			set
			{
				_emitDefaultValue = value;
			}
		}

		internal bool IsNullable
		{
			get
			{
				return _isNullable;
			}
			set
			{
				_isNullable = value;
			}
		}

		internal bool IsGetOnlyCollection
		{
			get
			{
				return _isGetOnlyCollection;
			}
			set
			{
				_isGetOnlyCollection = value;
			}
		}

		internal Type MemberType
		{
			get
			{
				if (_memberType == null)
				{
					if (MemberInfo is FieldInfo fieldInfo)
					{
						_memberType = fieldInfo.FieldType;
					}
					else if (MemberInfo is PropertyInfo propertyInfo)
					{
						_memberType = propertyInfo.PropertyType;
					}
					else
					{
						_memberType = (Type)MemberInfo;
					}
				}
				return _memberType;
			}
		}

		internal DataContract MemberTypeContract
		{
			[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
			[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
			get
			{
				if (_memberTypeContract == null)
				{
					if (IsGetOnlyCollection)
					{
						_memberTypeContract = DataContract.GetGetOnlyCollectionDataContract(DataContract.GetId(MemberType.TypeHandle), MemberType.TypeHandle, MemberType);
					}
					else
					{
						_memberTypeContract = DataContract.GetDataContract(MemberType);
					}
				}
				return _memberTypeContract;
			}
			set
			{
				_memberTypeContract = value;
			}
		}

		internal bool HasConflictingNameAndType
		{
			get
			{
				return _hasConflictingNameAndType;
			}
			set
			{
				_hasConflictingNameAndType = value;
			}
		}

		internal DataMember ConflictingMember
		{
			get
			{
				return _conflictingMember;
			}
			set
			{
				_conflictingMember = value;
			}
		}

		internal PrimitiveDataContract MemberPrimitiveContract
		{
			[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
			[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
			get
			{
				if (_memberPrimitiveContract == PrimitiveDataContract.NullContract)
				{
					_memberPrimitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(MemberType);
				}
				return _memberPrimitiveContract;
			}
		}

		internal CriticalHelper(MemberInfo memberInfo)
		{
			_emitDefaultValue = true;
			_memberInfo = memberInfo;
			_memberPrimitiveContract = PrimitiveDataContract.NullContract;
		}

		internal CriticalHelper(DataContract memberTypeContract, string name, bool isNullable, bool isRequired, bool emitDefaultValue, long order)
		{
			_memberTypeContract = memberTypeContract;
			_name = name;
			_isNullable = isNullable;
			_isRequired = isRequired;
			_emitDefaultValue = emitDefaultValue;
			_order = order;
			_memberInfo = memberTypeContract.UnderlyingType;
		}
	}

	private readonly CriticalHelper _helper;

	private FastInvokerBuilder.Getter _getter;

	private FastInvokerBuilder.Setter _setter;

	internal MemberInfo MemberInfo => _helper.MemberInfo;

	public string Name
	{
		get
		{
			return _helper.Name;
		}
		internal set
		{
			_helper.Name = value;
		}
	}

	public long Order
	{
		get
		{
			return _helper.Order;
		}
		internal set
		{
			_helper.Order = value;
		}
	}

	public bool IsRequired
	{
		get
		{
			return _helper.IsRequired;
		}
		internal set
		{
			_helper.IsRequired = value;
		}
	}

	public bool EmitDefaultValue
	{
		get
		{
			return _helper.EmitDefaultValue;
		}
		internal set
		{
			_helper.EmitDefaultValue = value;
		}
	}

	public bool IsNullable
	{
		get
		{
			return _helper.IsNullable;
		}
		internal set
		{
			_helper.IsNullable = value;
		}
	}

	internal bool IsGetOnlyCollection
	{
		get
		{
			return _helper.IsGetOnlyCollection;
		}
		set
		{
			_helper.IsGetOnlyCollection = value;
		}
	}

	internal Type MemberType => _helper.MemberType;

	public DataContract MemberTypeContract
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			return _helper.MemberTypeContract;
		}
	}

	internal PrimitiveDataContract? MemberPrimitiveContract
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			return _helper.MemberPrimitiveContract;
		}
	}

	internal bool HasConflictingNameAndType
	{
		get
		{
			return _helper.HasConflictingNameAndType;
		}
		set
		{
			_helper.HasConflictingNameAndType = value;
		}
	}

	internal DataMember? ConflictingMember
	{
		get
		{
			return _helper.ConflictingMember;
		}
		set
		{
			_helper.ConflictingMember = value;
		}
	}

	internal FastInvokerBuilder.Getter Getter => _getter ?? (_getter = FastInvokerBuilder.CreateGetter(MemberInfo));

	internal FastInvokerBuilder.Setter Setter => _setter ?? (_setter = FastInvokerBuilder.CreateSetter(MemberInfo));

	internal DataMember(MemberInfo memberInfo)
	{
		_helper = new CriticalHelper(memberInfo);
	}

	internal DataMember(DataContract memberTypeContract, string name, bool isNullable, bool isRequired, bool emitDefaultValue, long order)
	{
		_helper = new CriticalHelper(memberTypeContract, name, isNullable, isRequired, emitDefaultValue, order);
	}

	internal bool RequiresMemberAccessForGet()
	{
		MemberInfo memberInfo = MemberInfo;
		FieldInfo fieldInfo = memberInfo as FieldInfo;
		if (fieldInfo != null)
		{
			return DataContract.FieldRequiresMemberAccess(fieldInfo);
		}
		PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
		MethodInfo getMethod = propertyInfo.GetMethod;
		if (getMethod != null)
		{
			if (!DataContract.MethodRequiresMemberAccess(getMethod))
			{
				return !DataContract.IsTypeVisible(propertyInfo.PropertyType);
			}
			return true;
		}
		return false;
	}

	internal bool RequiresMemberAccessForSet()
	{
		MemberInfo memberInfo = MemberInfo;
		FieldInfo fieldInfo = memberInfo as FieldInfo;
		if (fieldInfo != null)
		{
			return DataContract.FieldRequiresMemberAccess(fieldInfo);
		}
		PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
		MethodInfo setMethod = propertyInfo.SetMethod;
		if (setMethod != null)
		{
			if (!DataContract.MethodRequiresMemberAccess(setMethod))
			{
				return !DataContract.IsTypeVisible(propertyInfo.PropertyType);
			}
			return true;
		}
		return false;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal DataMember BindGenericParameters(DataContract[] paramContracts, Dictionary<DataContract, DataContract> boundContracts = null)
	{
		DataContract dataContract = MemberTypeContract.BindGenericParameters(paramContracts, boundContracts);
		return new DataMember(dataContract, Name, !dataContract.IsValueType, IsRequired, EmitDefaultValue, Order);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal bool Equals(object other, HashSet<DataContractPairKey> checkedContracts)
	{
		if (this == other)
		{
			return true;
		}
		if (other is DataMember dataMember)
		{
			bool flag = MemberTypeContract != null && !MemberTypeContract.IsValueType;
			bool flag2 = dataMember.MemberTypeContract != null && !dataMember.MemberTypeContract.IsValueType;
			if (Name == dataMember.Name && (IsNullable || flag) == (dataMember.IsNullable || flag2) && IsRequired == dataMember.IsRequired && EmitDefaultValue == dataMember.EmitDefaultValue)
			{
				return MemberTypeContract.Equals(dataMember.MemberTypeContract, checkedContracts);
			}
			return false;
		}
		return false;
	}
}
