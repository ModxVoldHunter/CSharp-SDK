using System.Runtime.Versioning;

namespace System.Security.Cryptography;

[SupportedOSPlatform("windows")]
public sealed class CspKeyContainerInfo
{
	private readonly CspParameters _parameters;

	private readonly bool _randomKeyContainer;

	public bool Accessible
	{
		get
		{
			object obj = ReadKeyParameterSilent(CapiHelper.ClrPropertyId.CLR_ACCESSIBLE, throwOnNotFound: false);
			if (obj == null)
			{
				return false;
			}
			return (bool)obj;
		}
	}

	public bool Exportable
	{
		get
		{
			if (HardwareDevice)
			{
				return false;
			}
			return (bool)ReadKeyParameterSilent(CapiHelper.ClrPropertyId.CLR_EXPORTABLE);
		}
	}

	public bool HardwareDevice => (bool)ReadDeviceParameterVerifyContext(CapiHelper.ClrPropertyId.CLR_HARDWARE);

	public string? KeyContainerName => _parameters.KeyContainerName;

	public KeyNumber KeyNumber => (KeyNumber)_parameters.KeyNumber;

	public bool MachineKeyStore => CapiHelper.IsFlagBitSet((uint)_parameters.Flags, 1u);

	public bool Protected
	{
		get
		{
			if (HardwareDevice)
			{
				return true;
			}
			return (bool)ReadKeyParameterSilent(CapiHelper.ClrPropertyId.CLR_PROTECTED);
		}
	}

	public string? ProviderName => _parameters.ProviderName;

	public int ProviderType => _parameters.ProviderType;

	public bool RandomlyGenerated => _randomKeyContainer;

	public bool Removable => (bool)ReadDeviceParameterVerifyContext(CapiHelper.ClrPropertyId.CLR_REMOVABLE);

	public string UniqueKeyContainerName => (string)ReadKeyParameterSilent(CapiHelper.ClrPropertyId.CLR_UNIQUE_CONTAINER);

	public CspKeyContainerInfo(CspParameters parameters)
		: this(parameters, randomKeyContainer: false)
	{
	}

	internal CspKeyContainerInfo(CspParameters parameters, bool randomKeyContainer)
	{
		_parameters = new CspParameters(parameters);
		if (_parameters.KeyNumber == -1)
		{
			if (_parameters.ProviderType == 1 || _parameters.ProviderType == 24)
			{
				_parameters.KeyNumber = 1;
			}
			else if (_parameters.ProviderType == 13)
			{
				_parameters.KeyNumber = 2;
			}
		}
		_randomKeyContainer = randomKeyContainer;
	}

	private object ReadKeyParameterSilent(CapiHelper.ClrPropertyId keyParam, bool throwOnNotFound = true)
	{
		SafeProvHandle safeProvHandle;
		int num = CapiHelper.OpenCSP(_parameters, 64u, out safeProvHandle);
		using (safeProvHandle)
		{
			if (num != 0)
			{
				if (throwOnNotFound)
				{
					throw new CryptographicException(System.SR.Cryptography_CSP_NotFound);
				}
				return null;
			}
			return CapiHelper.GetProviderParameter(safeProvHandle, _parameters.KeyNumber, keyParam);
		}
	}

	private object ReadDeviceParameterVerifyContext(CapiHelper.ClrPropertyId keyParam)
	{
		CspParameters cspParameters = new CspParameters(_parameters);
		cspParameters.Flags &= CspProviderFlags.UseMachineKeyStore;
		cspParameters.KeyContainerName = null;
		SafeProvHandle safeProvHandle;
		int num = CapiHelper.OpenCSP(cspParameters, 4026531840u, out safeProvHandle);
		using (safeProvHandle)
		{
			if (num != 0)
			{
				throw new CryptographicException(System.SR.Cryptography_CSP_NotFound);
			}
			return CapiHelper.GetProviderParameter(safeProvHandle, cspParameters.KeyNumber, keyParam);
		}
	}
}
