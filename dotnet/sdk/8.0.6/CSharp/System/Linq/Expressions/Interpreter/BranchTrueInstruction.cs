namespace System.Linq.Expressions.Interpreter;

internal sealed class BranchTrueInstruction : OffsetInstruction
{
	private static Instruction[] s_cache;

	public override Instruction[] Cache => s_cache ?? (s_cache = new Instruction[32]);

	public override string InstructionName => "BranchTrue";

	public override int ConsumedStack => 1;

	public override int Run(InterpretedFrame frame)
	{
		if ((bool)frame.Pop())
		{
			return _offset;
		}
		return 1;
	}
}
