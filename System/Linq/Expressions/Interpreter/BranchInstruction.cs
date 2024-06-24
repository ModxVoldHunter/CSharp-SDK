namespace System.Linq.Expressions.Interpreter;

internal sealed class BranchInstruction : OffsetInstruction
{
	private static Instruction[][][] s_caches;

	internal readonly bool _hasResult;

	internal readonly bool _hasValue;

	public override Instruction[] Cache
	{
		get
		{
			if (s_caches == null)
			{
				s_caches = new Instruction[2][][]
				{
					new Instruction[2][],
					new Instruction[2][]
				};
			}
			Instruction[][] array = s_caches[ConsumedStack];
			int producedStack = ProducedStack;
			return array[producedStack] ?? (array[producedStack] = new Instruction[32]);
		}
	}

	public override string InstructionName => "Branch";

	public override int ConsumedStack => _hasValue ? 1 : 0;

	public override int ProducedStack => _hasResult ? 1 : 0;

	internal BranchInstruction()
		: this(hasResult: false, hasValue: false)
	{
	}

	public BranchInstruction(bool hasResult, bool hasValue)
	{
		_hasResult = hasResult;
		_hasValue = hasValue;
	}

	public override int Run(InterpretedFrame frame)
	{
		return _offset;
	}
}
