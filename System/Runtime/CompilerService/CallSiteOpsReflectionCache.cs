using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Runtime.CompilerServices;

internal static class CallSiteOpsReflectionCache<T> where T : class
{
	[Obsolete("CallSiteOps has been deprecated and is not supported.", false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static readonly MethodInfo CreateMatchmaker = new Func<CallSite<T>, CallSite<T>>(CallSiteOps.CreateMatchmaker).GetMethodInfo();

	[Obsolete("CallSiteOps has been deprecated and is not supported.", false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static readonly MethodInfo UpdateRules = new Action<CallSite<T>, int>(CallSiteOps.UpdateRules).GetMethodInfo();

	[Obsolete("CallSiteOps has been deprecated and is not supported.", false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static readonly MethodInfo GetRules = new Func<CallSite<T>, T[]>(CallSiteOps.GetRules).GetMethodInfo();

	[Obsolete("CallSiteOps has been deprecated and is not supported.", false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static readonly MethodInfo GetRuleCache = new Func<CallSite<T>, RuleCache<T>>(CallSiteOps.GetRuleCache).GetMethodInfo();

	[Obsolete("CallSiteOps has been deprecated and is not supported.", false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static readonly MethodInfo GetCachedRules = new Func<RuleCache<T>, T[]>(CallSiteOps.GetCachedRules).GetMethodInfo();

	[Obsolete("CallSiteOps has been deprecated and is not supported.", false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static readonly MethodInfo AddRule = new Action<CallSite<T>, T>(CallSiteOps.AddRule).GetMethodInfo();

	[Obsolete("CallSiteOps has been deprecated and is not supported.", false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static readonly MethodInfo MoveRule = new Action<RuleCache<T>, T, int>(CallSiteOps.MoveRule).GetMethodInfo();

	[Obsolete("CallSiteOps has been deprecated and is not supported.", false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	[UnconditionalSuppressMessage("DynamicCode", "IL3050", Justification = "CallSiteOps is obsolete and CallSite has RUC. Propagating warnings through fields isn't worth it.")]
	public static readonly MethodInfo Bind = new Func<CallSiteBinder, CallSite<T>, object[], T>(CallSiteOps.Bind).GetMethodInfo();
}
