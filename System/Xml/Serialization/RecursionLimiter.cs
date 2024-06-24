namespace System.Xml.Serialization;

internal sealed class RecursionLimiter
{
	private readonly int _maxDepth;

	private int _depth;

	private WorkItems _deferredWorkItems;

	internal bool IsExceededLimit => _depth > _maxDepth;

	internal int Depth
	{
		get
		{
			return _depth;
		}
		set
		{
			_depth = value;
		}
	}

	internal WorkItems DeferredWorkItems => _deferredWorkItems ?? (_deferredWorkItems = new WorkItems());

	internal RecursionLimiter()
	{
		_depth = 0;
		_maxDepth = (DiagnosticsSwitches.NonRecursiveTypeLoading.Enabled ? 1 : int.MaxValue);
	}
}
