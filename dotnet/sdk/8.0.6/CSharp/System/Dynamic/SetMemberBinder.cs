using System.Dynamic.Utils;

namespace System.Dynamic;

public abstract class SetMemberBinder : DynamicMetaObjectBinder
{
	public sealed override Type ReturnType => typeof(object);

	public string Name { get; }

	public bool IgnoreCase { get; }

	internal sealed override bool IsStandardBinder => true;

	protected SetMemberBinder(string name, bool ignoreCase)
	{
		ArgumentNullException.ThrowIfNull(name, "name");
		Name = name;
		IgnoreCase = ignoreCase;
	}

	public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
	{
		ArgumentNullException.ThrowIfNull(target, "target");
		ArgumentNullException.ThrowIfNull(args, "args");
		ContractUtils.Requires(args.Length == 1, "args");
		DynamicMetaObject dynamicMetaObject = args[0];
		ArgumentNullException.ThrowIfNull(dynamicMetaObject, "args");
		return target.BindSetMember(this, dynamicMetaObject);
	}

	public DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value)
	{
		return FallbackSetMember(target, value, null);
	}

	public abstract DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject? errorSuggestion);
}
