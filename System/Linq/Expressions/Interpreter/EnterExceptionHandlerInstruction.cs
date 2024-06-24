using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Expressions.Interpreter;

internal sealed class EnterExceptionHandlerInstruction : Instruction
{
	internal static readonly EnterExceptionHandlerInstruction Void = new EnterExceptionHandlerInstruction(hasValue: false);

	internal static readonly EnterExceptionHandlerInstruction NonVoid = new EnterExceptionHandlerInstruction(hasValue: true);

	private readonly bool _hasValue;

	public override string InstructionName => "EnterExceptionHandler";

	public override int ConsumedStack => _hasValue ? 1 : 0;

	public override int ProducedStack => 1;

	private EnterExceptionHandlerInstruction(bool hasValue)
	{
		_hasValue = hasValue;
	}

	[ExcludeFromCodeCoverage(Justification = "Known to be a no-op, this instruction is skipped on execution")]
	public override int Run(InterpretedFrame frame)
	{
		return 1;
	}
}
