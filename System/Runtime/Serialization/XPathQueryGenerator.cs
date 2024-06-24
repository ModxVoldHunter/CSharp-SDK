using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization.DataContracts;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization;

public static class XPathQueryGenerator
{
	private sealed class ExportContext
	{
		private readonly XmlNamespaceManager _namespaces;

		private int _nextPrefix;

		private readonly StringBuilder _xPathBuilder;

		public XmlNamespaceManager Namespaces => _namespaces;

		public string XPath => _xPathBuilder.ToString();

		public ExportContext(DataContract rootContract)
		{
			_namespaces = new XmlNamespaceManager(new NameTable());
			string text = SetNamespace(rootContract.TopLevelElementNamespace.Value);
			_xPathBuilder = new StringBuilder("/" + text + ":" + rootContract.TopLevelElementName.Value);
		}

		public ExportContext(StringBuilder rootContractXPath)
		{
			_namespaces = new XmlNamespaceManager(new NameTable());
			_xPathBuilder = rootContractXPath;
		}

		public void WriteChildToContext(DataMember contextMember, string prefix)
		{
			_xPathBuilder.Append("/" + prefix + ":" + contextMember.Name);
		}

		public string SetNamespace(string ns)
		{
			string text = _namespaces.LookupPrefix(ns);
			if (string.IsNullOrEmpty(text))
			{
				text = "xg" + _nextPrefix++.ToString(NumberFormatInfo.InvariantInfo);
				Namespaces.AddNamespace(text, ns);
			}
			return text;
		}
	}

	private const string XPathSeparator = "/";

	private const string NsSeparator = ":";

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public static string CreateFromDataContractSerializer(Type type, MemberInfo[] pathToMember, out XmlNamespaceManager namespaces)
	{
		return CreateFromDataContractSerializer(type, pathToMember, null, out namespaces);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public static string CreateFromDataContractSerializer(Type type, MemberInfo[] pathToMember, StringBuilder? rootElementXpath, out XmlNamespaceManager namespaces)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		ArgumentNullException.ThrowIfNull(pathToMember, "pathToMember");
		DataContract dataContract = DataContract.GetDataContract(type);
		ExportContext exportContext = ((rootElementXpath != null) ? new ExportContext(rootElementXpath) : new ExportContext(dataContract));
		for (int i = 0; i < pathToMember.Length; i++)
		{
			dataContract = ProcessDataContract(dataContract, exportContext, pathToMember[i]);
		}
		namespaces = exportContext.Namespaces;
		return exportContext.XPath;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static DataContract ProcessDataContract(DataContract contract, ExportContext context, MemberInfo memberNode)
	{
		if (contract is ClassDataContract)
		{
			return ProcessClassDataContract((ClassDataContract)contract, context, memberNode);
		}
		throw XmlObjectSerializer.CreateSerializationException(System.SR.QueryGeneratorPathToMemberNotFound);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static DataContract ProcessClassDataContract(ClassDataContract contract, ExportContext context, MemberInfo memberNode)
	{
		string prefix = context.SetNamespace(contract.Namespace.Value);
		foreach (DataMember dataMember in GetDataMembers(contract))
		{
			if (dataMember.MemberInfo.Name == memberNode.Name && dataMember.MemberInfo.DeclaringType.IsAssignableFrom(memberNode.DeclaringType))
			{
				context.WriteChildToContext(dataMember, prefix);
				return dataMember.MemberTypeContract;
			}
		}
		throw XmlObjectSerializer.CreateSerializationException(System.SR.QueryGeneratorPathToMemberNotFound);
	}

	private static IEnumerable<DataMember> GetDataMembers(ClassDataContract contract)
	{
		if (contract.BaseClassContract != null)
		{
			foreach (DataMember dataMember in GetDataMembers(contract.BaseClassContract))
			{
				yield return dataMember;
			}
		}
		if (contract.Members == null)
		{
			yield break;
		}
		foreach (DataMember member in contract.Members)
		{
			yield return member;
		}
	}
}
