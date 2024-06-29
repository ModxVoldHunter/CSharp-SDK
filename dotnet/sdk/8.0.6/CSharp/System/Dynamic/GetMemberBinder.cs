using System.Dynamic.Utils;

namespace System.Dynamic;

public abstract class GetMemberBinder : DynamicMetaObjectBinder
{
	public sealed override Type ReturnType => typeof(object);

	public string Name { get; }

	public bool IgnoreCase { get; }

	internal sealed override bool IsStandardBinder => true;

	protected GetMemberBinder(string name, bool ignoreCase)
	{
		ArgumentNullException.ThrowIfNull(name, "name");
		Name = name;
		IgnoreCase = ignoreCase;
	}

	public DynamicMetaObject FallbackGetMember(DynamicMetaObject target)
	{
		return FallbackGetMember(target, null);
	}

	public abstract DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject? errorSuggestion);

	public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[]? args)
	{
		ArgumentNullException.ThrowIfNull(target, "target");
		ContractUtils.Requires(args == null || args.Length == 0, "args");
		return target.BindGetMember(this);
	}
}
