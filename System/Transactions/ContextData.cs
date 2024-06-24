using System.Diagnostics.CodeAnalysis;

namespace System.Transactions;

internal sealed class ContextData
{
	internal TransactionScope CurrentScope;

	internal Transaction CurrentTransaction;

	internal DefaultComContextState DefaultComContextState;

	internal WeakReference WeakDefaultComContext;

	internal bool _asyncFlow;

	[ThreadStatic]
	private static ContextData t_staticData;

	internal static ContextData TLSCurrentData
	{
		get
		{
			return t_staticData ?? (t_staticData = new ContextData(asyncFlow: false));
		}
		[param: AllowNull]
		set
		{
			if (value == null && t_staticData != null)
			{
				t_staticData.CurrentScope = null;
				t_staticData.CurrentTransaction = null;
				t_staticData.DefaultComContextState = DefaultComContextState.Unknown;
				t_staticData.WeakDefaultComContext = null;
			}
			else
			{
				t_staticData = value;
			}
		}
	}

	internal ContextData(bool asyncFlow)
	{
		_asyncFlow = asyncFlow;
	}

	internal static ContextData LookupContextData(TxLookup defaultLookup)
	{
		if (CallContextCurrentData.TryGetCurrentData(out var currentData))
		{
			if (currentData.CurrentScope == null && currentData.CurrentTransaction == null && defaultLookup != TxLookup.DefaultCallContext)
			{
				CallContextCurrentData.ClearCurrentData(null, removeContextData: true);
				return TLSCurrentData;
			}
			return currentData;
		}
		return TLSCurrentData;
	}
}
