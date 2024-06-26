using System.Collections;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;

namespace System.Security.Authentication.ExtendedProtection;

public class ExtendedProtectionPolicy : ISerializable
{
	private readonly ServiceNameCollection _customServiceNames;

	private readonly PolicyEnforcement _policyEnforcement;

	private readonly ProtectionScenario _protectionScenario;

	private readonly ChannelBinding _customChannelBinding;

	public ServiceNameCollection? CustomServiceNames => _customServiceNames;

	public PolicyEnforcement PolicyEnforcement => _policyEnforcement;

	public ProtectionScenario ProtectionScenario => _protectionScenario;

	public ChannelBinding? CustomChannelBinding => _customChannelBinding;

	public static bool OSSupportsExtendedProtection => OperatingSystem.IsWindows();

	public ExtendedProtectionPolicy(PolicyEnforcement policyEnforcement, ProtectionScenario protectionScenario, ServiceNameCollection? customServiceNames)
	{
		if (policyEnforcement == PolicyEnforcement.Never)
		{
			throw new ArgumentException(System.SR.security_ExtendedProtectionPolicy_UseDifferentConstructorForNever, "policyEnforcement");
		}
		if (customServiceNames != null && customServiceNames.Count == 0)
		{
			throw new ArgumentException(System.SR.security_ExtendedProtectionPolicy_NoEmptyServiceNameCollection, "customServiceNames");
		}
		_policyEnforcement = policyEnforcement;
		_protectionScenario = protectionScenario;
		_customServiceNames = customServiceNames;
	}

	public ExtendedProtectionPolicy(PolicyEnforcement policyEnforcement, ProtectionScenario protectionScenario, ICollection? customServiceNames)
		: this(policyEnforcement, protectionScenario, (customServiceNames == null) ? null : new ServiceNameCollection(customServiceNames))
	{
	}

	public ExtendedProtectionPolicy(PolicyEnforcement policyEnforcement, ChannelBinding customChannelBinding)
	{
		if (policyEnforcement == PolicyEnforcement.Never)
		{
			throw new ArgumentException(System.SR.security_ExtendedProtectionPolicy_UseDifferentConstructorForNever, "policyEnforcement");
		}
		ArgumentNullException.ThrowIfNull(customChannelBinding, "customChannelBinding");
		_policyEnforcement = policyEnforcement;
		_protectionScenario = ProtectionScenario.TransportSelected;
		_customChannelBinding = customChannelBinding;
	}

	public ExtendedProtectionPolicy(PolicyEnforcement policyEnforcement)
	{
		_policyEnforcement = policyEnforcement;
		_protectionScenario = ProtectionScenario.TransportSelected;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected ExtendedProtectionPolicy(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("ProtectionScenario=");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2);
		handler.AppendFormatted(_protectionScenario);
		stringBuilder3.Append(ref handler);
		stringBuilder.Append("; PolicyEnforcement=");
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2);
		handler.AppendFormatted(_policyEnforcement);
		stringBuilder4.Append(ref handler);
		stringBuilder.Append("; CustomChannelBinding=");
		if (_customChannelBinding == null)
		{
			stringBuilder.Append("<null>");
		}
		else
		{
			stringBuilder.Append(_customChannelBinding.ToString());
		}
		stringBuilder.Append("; ServiceNames=");
		if (_customServiceNames == null)
		{
			stringBuilder.Append("<null>");
		}
		else
		{
			bool flag = true;
			foreach (string customServiceName in _customServiceNames)
			{
				if (flag)
				{
					flag = false;
				}
				else
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(customServiceName);
			}
		}
		return stringBuilder.ToString();
	}
}
