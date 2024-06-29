namespace System.Text.RegularExpressions.Symbolic;

internal enum SymbolicRegexNodeKind
{
	Epsilon,
	Singleton,
	Concat,
	Loop,
	Alternate,
	BeginningAnchor,
	EndAnchor,
	EndAnchorZ,
	EndAnchorZReverse,
	BOLAnchor,
	EOLAnchor,
	BoundaryAnchor,
	NonBoundaryAnchor,
	FixedLengthMarker,
	Effect,
	CaptureStart,
	CaptureEnd,
	DisableBacktrackingSimulation
}
