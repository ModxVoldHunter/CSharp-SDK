namespace System.Linq.Expressions.Interpreter;

internal sealed class GotoInstruction : IndexedBranchInstruction
{
	private static readonly GotoInstruction[] s_cache = new GotoInstruction[256];

	private readonly bool _hasResult;

	private readonly bool _hasValue;

	private readonly bool _labelTargetGetsValue;

	public override string InstructionName => "Goto";

	public override int ConsumedStack => _hasValue ? 1 : 0;

	public override int ProducedStack => _hasResult ? 1 : 0;

	private GotoInstruction(int targetIndex, bool hasResult, bool hasValue, bool labelTargetGetsValue)
		: base(targetIndex)
	{
		_hasResult = hasResult;
		_hasValue = hasValue;
		_labelTargetGetsValue = labelTargetGetsValue;
	}

	internal static GotoInstruction Create(int labelIndex, bool hasResult, bool hasValue, bool labelTargetGetsValue)
	{
		if (labelIndex < 32)
		{
			int num = (8 * labelIndex) | (labelTargetGetsValue ? 4 : 0) | (hasResult ? 2 : 0) | (hasValue ? 1 : 0);
			ref GotoInstruction reference = ref s_cache[num];
			return reference ?? (reference = new GotoInstruction(labelIndex, hasResult, hasValue, labelTargetGetsValue));
		}
		return new GotoInstruction(labelIndex, hasResult, hasValue, labelTargetGetsValue);
	}

	public override int Run(InterpretedFrame frame)
	{
		object obj = (_hasValue ? frame.Pop() : Interpreter.NoValue);
		return frame.Goto(_labelIndex, _labelTargetGetsValue ? obj : Interpreter.NoValue, gotoExceptionHandler: false);
	}
}
