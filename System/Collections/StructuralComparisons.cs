namespace System.Collections;

public static class StructuralComparisons
{
	private static volatile IComparer s_StructuralComparer;

	private static volatile IEqualityComparer s_StructuralEqualityComparer;

	public static IComparer StructuralComparer => s_StructuralComparer ?? (s_StructuralComparer = new StructuralComparer());

	public static IEqualityComparer StructuralEqualityComparer => s_StructuralEqualityComparer ?? (s_StructuralEqualityComparer = new StructuralEqualityComparer());
}
