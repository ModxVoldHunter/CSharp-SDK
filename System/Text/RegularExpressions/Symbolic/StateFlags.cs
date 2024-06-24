namespace System.Text.RegularExpressions.Symbolic;

[Flags]
internal enum StateFlags : byte
{
	IsInitialFlag = 1,
	IsDeadendFlag = 2,
	IsNullableFlag = 4,
	CanBeNullableFlag = 8,
	SimulatesBacktrackingFlag = 0x10
}
