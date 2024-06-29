namespace System.Text.RegularExpressions.Symbolic;

internal readonly struct DerivativeEffect
{
	public DerivativeEffectKind Kind { get; }

	public int CaptureNumber { get; }

	public DerivativeEffect(DerivativeEffectKind kind, int captureNumber)
	{
		Kind = kind;
		CaptureNumber = captureNumber;
	}
}
