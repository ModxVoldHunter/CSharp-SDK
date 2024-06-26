using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;

namespace System.Security.AccessControl;

public abstract class KnownAce : GenericAce
{
	private int _accessMask;

	private SecurityIdentifier _sid;

	public int AccessMask
	{
		get
		{
			return _accessMask;
		}
		set
		{
			_accessMask = value;
		}
	}

	public SecurityIdentifier SecurityIdentifier
	{
		get
		{
			return _sid;
		}
		[MemberNotNull("_sid")]
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			_sid = value;
		}
	}

	internal KnownAce(AceType type, AceFlags flags, int accessMask, SecurityIdentifier securityIdentifier)
		: base(type, flags)
	{
		ArgumentNullException.ThrowIfNull(securityIdentifier, "securityIdentifier");
		AccessMask = accessMask;
		SecurityIdentifier = securityIdentifier;
	}
}
