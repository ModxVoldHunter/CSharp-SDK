using System.Collections;

namespace System.Text.RegularExpressions;

internal sealed class MatchSparse : Match
{
	private new readonly Hashtable _caps;

	public override GroupCollection Groups => _groupcoll ?? (_groupcoll = new GroupCollection(this, _caps));

	internal MatchSparse(Regex regex, Hashtable caps, int capcount, string text, int textLength)
		: base(regex, capcount, text, textLength)
	{
		_caps = caps;
	}
}
