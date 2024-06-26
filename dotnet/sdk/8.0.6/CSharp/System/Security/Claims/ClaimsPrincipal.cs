using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Threading;

namespace System.Security.Claims;

[DebuggerDisplay("{DebuggerToString(),nq}")]
public class ClaimsPrincipal : IPrincipal
{
	private enum SerializationMask
	{
		None,
		HasIdentities,
		UserData
	}

	private readonly List<ClaimsIdentity> _identities = new List<ClaimsIdentity>();

	private readonly byte[] _userSerializationData;

	private static Func<IEnumerable<ClaimsIdentity>, ClaimsIdentity> s_identitySelector = SelectPrimaryIdentity;

	private static Func<ClaimsPrincipal> s_principalSelector = ClaimsPrincipalSelector;

	public static Func<IEnumerable<ClaimsIdentity>, ClaimsIdentity?> PrimaryIdentitySelector
	{
		get
		{
			return s_identitySelector;
		}
		set
		{
			s_identitySelector = value;
		}
	}

	public static Func<ClaimsPrincipal> ClaimsPrincipalSelector
	{
		get
		{
			return s_principalSelector;
		}
		set
		{
			s_principalSelector = value;
		}
	}

	public virtual IEnumerable<Claim> Claims
	{
		get
		{
			foreach (ClaimsIdentity identity in Identities)
			{
				foreach (Claim claim in identity.Claims)
				{
					yield return claim;
				}
			}
		}
	}

	protected virtual byte[]? CustomSerializationData => _userSerializationData;

	public static ClaimsPrincipal? Current
	{
		get
		{
			if (s_principalSelector == null)
			{
				return SelectClaimsPrincipal();
			}
			return s_principalSelector();
		}
	}

	public virtual IEnumerable<ClaimsIdentity> Identities => _identities;

	public virtual IIdentity? Identity
	{
		get
		{
			if (s_identitySelector != null)
			{
				return s_identitySelector(_identities);
			}
			return SelectPrimaryIdentity(_identities);
		}
	}

	private static ClaimsPrincipal SelectClaimsPrincipal()
	{
		IPrincipal currentPrincipal = Thread.CurrentPrincipal;
		if (!(currentPrincipal is ClaimsPrincipal result))
		{
			if (currentPrincipal != null)
			{
				return new ClaimsPrincipal(currentPrincipal);
			}
			return null;
		}
		return result;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected ClaimsPrincipal(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	private static ClaimsIdentity SelectPrimaryIdentity(IEnumerable<ClaimsIdentity> identities)
	{
		ArgumentNullException.ThrowIfNull(identities, "identities");
		foreach (ClaimsIdentity identity in identities)
		{
			if (identity != null)
			{
				return identity;
			}
		}
		return null;
	}

	public ClaimsPrincipal()
	{
	}

	public ClaimsPrincipal(IEnumerable<ClaimsIdentity> identities)
	{
		ArgumentNullException.ThrowIfNull(identities, "identities");
		_identities.AddRange(identities);
	}

	public ClaimsPrincipal(IIdentity identity)
	{
		ArgumentNullException.ThrowIfNull(identity, "identity");
		if (identity is ClaimsIdentity item)
		{
			_identities.Add(item);
		}
		else
		{
			_identities.Add(new ClaimsIdentity(identity));
		}
	}

	public ClaimsPrincipal(IPrincipal principal)
	{
		ArgumentNullException.ThrowIfNull(principal, "principal");
		if (!(principal is ClaimsPrincipal claimsPrincipal))
		{
			_identities.Add(new ClaimsIdentity(principal.Identity));
		}
		else if (claimsPrincipal.Identities != null)
		{
			_identities.AddRange(claimsPrincipal.Identities);
		}
	}

	public ClaimsPrincipal(BinaryReader reader)
	{
		ArgumentNullException.ThrowIfNull(reader, "reader");
		SerializationMask serializationMask = (SerializationMask)reader.ReadInt32();
		int num = reader.ReadInt32();
		int num2 = 0;
		if ((serializationMask & SerializationMask.HasIdentities) == SerializationMask.HasIdentities)
		{
			num2++;
			int num3 = reader.ReadInt32();
			for (int i = 0; i < num3; i++)
			{
				_identities.Add(CreateClaimsIdentity(reader));
			}
		}
		if ((serializationMask & SerializationMask.UserData) == SerializationMask.UserData)
		{
			int count = reader.ReadInt32();
			_userSerializationData = reader.ReadBytes(count);
			num2++;
		}
		for (int j = num2; j < num; j++)
		{
			reader.ReadString();
		}
	}

	public virtual void AddIdentity(ClaimsIdentity identity)
	{
		ArgumentNullException.ThrowIfNull(identity, "identity");
		_identities.Add(identity);
	}

	public virtual void AddIdentities(IEnumerable<ClaimsIdentity> identities)
	{
		ArgumentNullException.ThrowIfNull(identities, "identities");
		_identities.AddRange(identities);
	}

	public virtual ClaimsPrincipal Clone()
	{
		return new ClaimsPrincipal(this);
	}

	protected virtual ClaimsIdentity CreateClaimsIdentity(BinaryReader reader)
	{
		ArgumentNullException.ThrowIfNull(reader, "reader");
		return new ClaimsIdentity(reader);
	}

	public virtual IEnumerable<Claim> FindAll(Predicate<Claim> match)
	{
		ArgumentNullException.ThrowIfNull(match, "match");
		return Core(match);
		IEnumerable<Claim> Core(Predicate<Claim> match)
		{
			foreach (ClaimsIdentity identity in Identities)
			{
				if (identity != null)
				{
					foreach (Claim item in identity.FindAll(match))
					{
						yield return item;
					}
				}
			}
		}
	}

	public virtual IEnumerable<Claim> FindAll(string type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return Core(type);
		IEnumerable<Claim> Core(string type)
		{
			foreach (ClaimsIdentity identity in Identities)
			{
				if (identity != null)
				{
					foreach (Claim item in identity.FindAll(type))
					{
						yield return item;
					}
				}
			}
		}
	}

	public virtual Claim? FindFirst(Predicate<Claim> match)
	{
		ArgumentNullException.ThrowIfNull(match, "match");
		Claim claim = null;
		foreach (ClaimsIdentity identity in Identities)
		{
			if (identity != null)
			{
				claim = identity.FindFirst(match);
				if (claim != null)
				{
					return claim;
				}
			}
		}
		return claim;
	}

	public virtual Claim? FindFirst(string type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		Claim claim = null;
		for (int i = 0; i < _identities.Count; i++)
		{
			if (_identities[i] != null)
			{
				claim = _identities[i].FindFirst(type);
				if (claim != null)
				{
					return claim;
				}
			}
		}
		return claim;
	}

	public virtual bool HasClaim(Predicate<Claim> match)
	{
		ArgumentNullException.ThrowIfNull(match, "match");
		for (int i = 0; i < _identities.Count; i++)
		{
			if (_identities[i] != null && _identities[i].HasClaim(match))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool HasClaim(string type, string value)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		ArgumentNullException.ThrowIfNull(value, "value");
		for (int i = 0; i < _identities.Count; i++)
		{
			if (_identities[i] != null && _identities[i].HasClaim(type, value))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool IsInRole(string role)
	{
		for (int i = 0; i < _identities.Count; i++)
		{
			if (_identities[i] != null && _identities[i].HasClaim(_identities[i].RoleClaimType, role))
			{
				return true;
			}
		}
		return false;
	}

	public virtual void WriteTo(BinaryWriter writer)
	{
		WriteTo(writer, null);
	}

	protected virtual void WriteTo(BinaryWriter writer, byte[]? userData)
	{
		ArgumentNullException.ThrowIfNull(writer, "writer");
		int num = 0;
		SerializationMask serializationMask = SerializationMask.None;
		if (_identities.Count > 0)
		{
			serializationMask |= SerializationMask.HasIdentities;
			num++;
		}
		if (userData != null && userData.Length != 0)
		{
			num++;
			serializationMask |= SerializationMask.UserData;
		}
		writer.Write((int)serializationMask);
		writer.Write(num);
		if ((serializationMask & SerializationMask.HasIdentities) == SerializationMask.HasIdentities)
		{
			writer.Write(_identities.Count);
			foreach (ClaimsIdentity identity in _identities)
			{
				identity.WriteTo(writer);
			}
		}
		if ((serializationMask & SerializationMask.UserData) == SerializationMask.UserData)
		{
			writer.Write(userData.Length);
			writer.Write(userData);
		}
		writer.Flush();
	}

	[OnSerializing]
	private void OnSerializingMethod(StreamingContext context)
	{
		if (this is ISerializable || _identities.Count <= 0)
		{
			return;
		}
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_Serialization);
	}

	protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	private string DebuggerToString()
	{
		int num = 0;
		foreach (ClaimsIdentity identity in Identities)
		{
			num++;
		}
		if (num == 1 && Identity is ClaimsIdentity claimsIdentity)
		{
			return claimsIdentity.DebuggerToString();
		}
		int num2 = 0;
		foreach (Claim claim in Claims)
		{
			num2++;
		}
		return $"Identities = {num}, Claims = {num2}";
	}
}
