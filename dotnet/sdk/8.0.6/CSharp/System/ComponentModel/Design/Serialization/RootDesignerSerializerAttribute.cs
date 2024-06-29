namespace System.ComponentModel.Design.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
[Obsolete("RootDesignerSerializerAttribute has been deprecated. Use DesignerSerializerAttribute instead. For example, to specify a root designer for CodeDom, use DesignerSerializerAttribute(...,typeof(TypeCodeDomSerializer)) instead.")]
public sealed class RootDesignerSerializerAttribute : Attribute
{
	private string _typeId;

	public bool Reloadable { get; }

	public string? SerializerTypeName { get; }

	public string? SerializerBaseTypeName { get; }

	public override object TypeId
	{
		get
		{
			if (_typeId == null)
			{
				ReadOnlySpan<char> readOnlySpan = SerializerBaseTypeName;
				int num = readOnlySpan.IndexOf(',');
				if (num >= 0)
				{
					readOnlySpan = readOnlySpan.Slice(0, num);
				}
				_typeId = GetType().FullName + readOnlySpan;
			}
			return _typeId;
		}
	}

	public RootDesignerSerializerAttribute(Type serializerType, Type baseSerializerType, bool reloadable)
	{
		ArgumentNullException.ThrowIfNull(serializerType, "serializerType");
		ArgumentNullException.ThrowIfNull(baseSerializerType, "baseSerializerType");
		SerializerTypeName = serializerType.AssemblyQualifiedName;
		SerializerBaseTypeName = baseSerializerType.AssemblyQualifiedName;
		Reloadable = reloadable;
	}

	public RootDesignerSerializerAttribute(string serializerTypeName, Type baseSerializerType, bool reloadable)
	{
		ArgumentNullException.ThrowIfNull(baseSerializerType, "baseSerializerType");
		SerializerTypeName = serializerTypeName;
		SerializerBaseTypeName = baseSerializerType.AssemblyQualifiedName;
		Reloadable = reloadable;
	}

	public RootDesignerSerializerAttribute(string? serializerTypeName, string? baseSerializerTypeName, bool reloadable)
	{
		SerializerTypeName = serializerTypeName;
		SerializerBaseTypeName = baseSerializerTypeName;
		Reloadable = reloadable;
	}
}
