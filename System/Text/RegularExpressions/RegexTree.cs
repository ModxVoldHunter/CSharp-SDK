using System.Collections;
using System.Globalization;

namespace System.Text.RegularExpressions;

internal sealed class RegexTree
{
	public readonly RegexOptions Options;

	public readonly RegexNode Root;

	public readonly RegexFindOptimizations FindOptimizations;

	public readonly int CaptureCount;

	public readonly CultureInfo Culture;

	public readonly string[] CaptureNames;

	public readonly Hashtable CaptureNameToNumberMapping;

	public readonly Hashtable CaptureNumberSparseMapping;

	internal RegexTree(RegexNode root, int captureCount, string[] captureNames, Hashtable captureNameToNumberMapping, Hashtable captureNumberSparseMapping, RegexOptions options, CultureInfo culture)
	{
		Root = root;
		Culture = culture;
		CaptureNumberSparseMapping = captureNumberSparseMapping;
		CaptureCount = captureCount;
		CaptureNameToNumberMapping = captureNameToNumberMapping;
		CaptureNames = captureNames;
		Options = options;
		FindOptimizations = new RegexFindOptimizations(root, options);
	}
}
