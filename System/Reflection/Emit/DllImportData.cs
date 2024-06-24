using System.Runtime.InteropServices;

namespace System.Reflection.Emit;

internal sealed class DllImportData
{
	private readonly string _moduleName;

	private readonly string _entryPoint;

	private readonly MethodImportAttributes _flags;

	public string ModuleName => _moduleName;

	public string EntryPoint => _entryPoint;

	public MethodImportAttributes Flags => _flags;

	internal DllImportData(string moduleName, string entryPoint, MethodImportAttributes flags)
	{
		_moduleName = moduleName;
		_entryPoint = entryPoint;
		_flags = flags;
	}

	internal static DllImportData CreateDllImportData(CustomAttributeInfo attr, out bool preserveSig)
	{
		string text = (string)attr._ctorArgs[0];
		if (string.IsNullOrEmpty(text))
		{
			throw new ArgumentException(System.SR.Argument_DllNameCannotBeEmpty);
		}
		MethodImportAttributes methodImportAttributes = MethodImportAttributes.None;
		string entryPoint = null;
		preserveSig = true;
		for (int i = 0; i < attr._namedParamNames.Length; i++)
		{
			string text2 = attr._namedParamNames[i];
			object obj = attr._namedParamValues[i];
			switch (text2)
			{
			case "PreserveSig":
				preserveSig = (bool)obj;
				break;
			case "CallingConvention":
			{
				MethodImportAttributes methodImportAttributes2 = methodImportAttributes;
				methodImportAttributes = methodImportAttributes2 | ((CallingConvention)obj switch
				{
					CallingConvention.Cdecl => MethodImportAttributes.CallingConventionCDecl, 
					CallingConvention.FastCall => MethodImportAttributes.CallingConventionFastCall, 
					CallingConvention.StdCall => MethodImportAttributes.CallingConventionStdCall, 
					CallingConvention.ThisCall => MethodImportAttributes.CallingConventionThisCall, 
					_ => MethodImportAttributes.CallingConventionWinApi, 
				});
				break;
			}
			case "CharSet":
			{
				MethodImportAttributes methodImportAttributes3 = methodImportAttributes;
				methodImportAttributes = methodImportAttributes3 | ((CharSet)obj switch
				{
					CharSet.Ansi => MethodImportAttributes.CharSetAnsi, 
					CharSet.Auto => MethodImportAttributes.CharSetAuto, 
					CharSet.Unicode => MethodImportAttributes.CharSetUnicode, 
					_ => MethodImportAttributes.CharSetAuto, 
				});
				break;
			}
			case "EntryPoint":
				entryPoint = (string)obj;
				break;
			case "ExactSpelling":
				if ((bool)obj)
				{
					methodImportAttributes |= MethodImportAttributes.ExactSpelling;
				}
				break;
			case "SetLastError":
				if ((bool)obj)
				{
					methodImportAttributes |= MethodImportAttributes.SetLastError;
				}
				break;
			case "BestFitMapping":
				methodImportAttributes = ((!(bool)obj) ? (methodImportAttributes | MethodImportAttributes.BestFitMappingDisable) : (methodImportAttributes | MethodImportAttributes.BestFitMappingEnable));
				break;
			case "ThrowOnUnmappableChar":
				methodImportAttributes = ((!(bool)obj) ? (methodImportAttributes | MethodImportAttributes.ThrowOnUnmappableCharDisable) : (methodImportAttributes | MethodImportAttributes.ThrowOnUnmappableCharEnable));
				break;
			}
		}
		return new DllImportData(text, entryPoint, methodImportAttributes);
	}
}
