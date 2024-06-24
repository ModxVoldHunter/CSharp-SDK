using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.DataContracts;

internal sealed class GenericInfo : IGenericNameProvider
{
	private readonly string _genericTypeName;

	private readonly XmlQualifiedName _xmlName;

	private List<GenericInfo> _paramGenericInfos;

	private readonly List<int> _nestedParamCounts;

	public XmlQualifiedName XmlName => _xmlName;

	public IList<GenericInfo> Parameters => _paramGenericInfos;

	bool IGenericNameProvider.ParametersFromBuiltInNamespaces
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			bool flag = true;
			if (_paramGenericInfos == null || _paramGenericInfos.Count == 0)
			{
				return flag;
			}
			for (int i = 0; i < _paramGenericInfos.Count; i++)
			{
				if (!flag)
				{
					break;
				}
				flag = DataContract.IsBuiltInNamespace(_paramGenericInfos[i].GetXmlNamespace());
			}
			return flag;
		}
	}

	internal GenericInfo(XmlQualifiedName xmlName, string genericTypeName)
	{
		_xmlName = xmlName;
		_genericTypeName = genericTypeName;
		_nestedParamCounts = new List<int>();
		_nestedParamCounts.Add(0);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public XmlQualifiedName GetExpandedXmlName()
	{
		if (_paramGenericInfos == null)
		{
			return _xmlName;
		}
		return new XmlQualifiedName(DataContract.EncodeLocalName(DataContract.ExpandGenericParameters(XmlConvert.DecodeName(_xmlName.Name), this)), _xmlName.Namespace);
	}

	internal void Add(GenericInfo actualParamInfo)
	{
		if (_paramGenericInfos == null)
		{
			_paramGenericInfos = new List<GenericInfo>();
		}
		_paramGenericInfos.Add(actualParamInfo);
	}

	internal void AddToLevel(int level, int count)
	{
		if (level >= _nestedParamCounts.Count)
		{
			do
			{
				_nestedParamCounts.Add((level == _nestedParamCounts.Count) ? count : 0);
			}
			while (level >= _nestedParamCounts.Count);
		}
		else
		{
			_nestedParamCounts[level] += count;
		}
	}

	internal string GetXmlNamespace()
	{
		return _xmlName.Namespace;
	}

	int IGenericNameProvider.GetParameterCount()
	{
		return _paramGenericInfos?.Count ?? 0;
	}

	IList<int> IGenericNameProvider.GetNestedParameterCounts()
	{
		return _nestedParamCounts;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	string IGenericNameProvider.GetParameterName(int paramIndex)
	{
		return _paramGenericInfos[paramIndex].GetExpandedXmlName().Name;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	string IGenericNameProvider.GetNamespaces()
	{
		if (_paramGenericInfos == null || _paramGenericInfos.Count == 0)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < _paramGenericInfos.Count; i++)
		{
			stringBuilder.Append(' ').Append(_paramGenericInfos[i].GetXmlNamespace());
		}
		return stringBuilder.ToString();
	}

	string IGenericNameProvider.GetGenericTypeName()
	{
		return _genericTypeName;
	}
}
