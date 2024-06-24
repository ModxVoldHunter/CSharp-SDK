using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.All)]
public class PropertyTabAttribute : Attribute
{
	private Type[] _tabClasses;

	private string[] _tabClassNames;

	public Type[] TabClasses
	{
		get
		{
			if (_tabClasses == null && _tabClassNames != null)
			{
				InitializeTabClasses();
			}
			return _tabClasses;
		}
	}

	protected string[]? TabClassNames => (string[])_tabClassNames?.Clone();

	public PropertyTabScope[] TabScopes { get; private set; }

	public PropertyTabAttribute()
	{
		TabScopes = Array.Empty<PropertyTabScope>();
		_tabClassNames = Array.Empty<string>();
	}

	public PropertyTabAttribute(Type tabClass)
		: this(tabClass, PropertyTabScope.Component)
	{
	}

	public PropertyTabAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string tabClassName)
		: this(tabClassName, PropertyTabScope.Component)
	{
	}

	public PropertyTabAttribute(Type tabClass, PropertyTabScope tabScope)
	{
		_tabClasses = new Type[1] { tabClass };
		if (tabScope < PropertyTabScope.Document)
		{
			throw new ArgumentException(System.SR.PropertyTabAttributeBadPropertyTabScope, "tabScope");
		}
		TabScopes = new PropertyTabScope[1] { tabScope };
	}

	public PropertyTabAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string tabClassName, PropertyTabScope tabScope)
	{
		_tabClassNames = new string[1] { tabClassName };
		if (tabScope < PropertyTabScope.Document)
		{
			throw new ArgumentException(System.SR.PropertyTabAttributeBadPropertyTabScope, "tabScope");
		}
		TabScopes = new PropertyTabScope[1] { tabScope };
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The APIs that specify _tabClassNames are either marked with DynamicallyAccessedMembers or RequiresUnreferencedCode.")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057:TypeGetType", Justification = "The APIs that specify _tabClassNames are either marked with DynamicallyAccessedMembers or RequiresUnreferencedCode.")]
	[MemberNotNull("_tabClasses")]
	private void InitializeTabClasses()
	{
		_tabClasses = new Type[_tabClassNames.Length];
		for (int i = 0; i < _tabClassNames.Length; i++)
		{
			int num = _tabClassNames[i].IndexOf(',');
			string text = null;
			string text2;
			if (num != -1)
			{
				text2 = _tabClassNames[i].AsSpan(0, num).Trim().ToString();
				text = _tabClassNames[i].AsSpan(num + 1).Trim().ToString();
			}
			else
			{
				text2 = _tabClassNames[i];
			}
			_tabClasses[i] = Type.GetType(text2, throwOnError: false);
			if (_tabClasses[i] == null)
			{
				if (text == null)
				{
					throw new TypeLoadException(System.SR.Format(System.SR.PropertyTabAttributeTypeLoadException, text2));
				}
				Assembly assembly = Assembly.Load(text);
				if (assembly != null)
				{
					_tabClasses[i] = assembly.GetType(text2, throwOnError: true);
				}
			}
		}
	}

	public override bool Equals([NotNullWhen(true)] object? other)
	{
		return Equals(other as PropertyTabAttribute);
	}

	public bool Equals([NotNullWhen(true)] PropertyTabAttribute? other)
	{
		if (other == null)
		{
			return false;
		}
		if (this == other)
		{
			return true;
		}
		if (other.TabClasses.Length != TabClasses.Length || other.TabScopes.Length != TabScopes.Length)
		{
			return false;
		}
		for (int i = 0; i < TabClasses.Length; i++)
		{
			if (TabClasses[i] != other.TabClasses[i] || TabScopes[i] != other.TabScopes[i])
			{
				return false;
			}
		}
		return true;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	[RequiresUnreferencedCode("The Types referenced by tabClassNames may be trimmed.")]
	protected void InitializeArrays(string[]? tabClassNames, PropertyTabScope[]? tabScopes)
	{
		InitializeArrays(tabClassNames, null, tabScopes);
	}

	protected void InitializeArrays(Type[]? tabClasses, PropertyTabScope[]? tabScopes)
	{
		InitializeArrays(null, tabClasses, tabScopes);
	}

	private void InitializeArrays(string[] tabClassNames, Type[] tabClasses, PropertyTabScope[] tabScopes)
	{
		if (tabClasses != null)
		{
			if (tabScopes != null && tabClasses.Length != tabScopes.Length)
			{
				throw new ArgumentException(System.SR.PropertyTabAttributeArrayLengthMismatch);
			}
			_tabClasses = (Type[])tabClasses.Clone();
		}
		else if (tabClassNames != null)
		{
			if (tabScopes != null && tabClassNames.Length != tabScopes.Length)
			{
				throw new ArgumentException(System.SR.PropertyTabAttributeArrayLengthMismatch);
			}
			_tabClassNames = (string[])tabClassNames.Clone();
			_tabClasses = null;
		}
		else if (_tabClasses == null && _tabClassNames == null)
		{
			throw new ArgumentException(System.SR.PropertyTabAttributeParamsBothNull);
		}
		if (tabScopes != null)
		{
			for (int i = 0; i < tabScopes.Length; i++)
			{
				if (tabScopes[i] < PropertyTabScope.Document)
				{
					throw new ArgumentException(System.SR.PropertyTabAttributeBadPropertyTabScope);
				}
			}
			TabScopes = (PropertyTabScope[])tabScopes.Clone();
		}
		else
		{
			TabScopes = new PropertyTabScope[tabClasses.Length];
			for (int j = 0; j < TabScopes.Length; j++)
			{
				TabScopes[j] = PropertyTabScope.Component;
			}
		}
	}
}
