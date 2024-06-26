using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;
using System.Xml.Xsl.Xslt;

namespace System.Xml.Xsl;

[RequiresDynamicCode("XslCompiledTransform requires dynamic code because it generates IL at runtime.")]
public sealed class XslCompiledTransform
{
	private static readonly Version s_version = typeof(XslCompiledTransform).Assembly.GetName().Version;

	private readonly bool _enableDebug;

	private CompilerErrorCollection _compilerErrorColl;

	private QilExpression _qil;

	private XmlILCommand _command;

	public XmlWriterSettings? OutputSettings { get; private set; }

	public XslCompiledTransform()
	{
	}

	public XslCompiledTransform(bool enableDebug)
	{
		_enableDebug = enableDebug;
	}

	private void Reset()
	{
		_compilerErrorColl = null;
		OutputSettings = null;
		_qil = null;
		_command = null;
	}

	public void Load(XmlReader stylesheet)
	{
		Reset();
		LoadInternal(stylesheet, XsltSettings.Default, CreateDefaultResolver(), null);
	}

	public void Load(XmlReader stylesheet, XsltSettings? settings, XmlResolver? stylesheetResolver)
	{
		Reset();
		LoadInternal(stylesheet, settings, stylesheetResolver, stylesheetResolver);
	}

	public void Load(IXPathNavigable stylesheet)
	{
		Reset();
		LoadInternal(stylesheet, XsltSettings.Default, CreateDefaultResolver(), null);
	}

	public void Load(IXPathNavigable stylesheet, XsltSettings? settings, XmlResolver? stylesheetResolver)
	{
		Reset();
		LoadInternal(stylesheet, settings, stylesheetResolver, stylesheetResolver);
	}

	public void Load(string stylesheetUri)
	{
		Reset();
		ArgumentNullException.ThrowIfNull(stylesheetUri, "stylesheetUri");
		LoadInternal(stylesheetUri, XsltSettings.Default, CreateDefaultResolver(), null);
	}

	public void Load(string stylesheetUri, XsltSettings? settings, XmlResolver? stylesheetResolver)
	{
		Reset();
		ArgumentNullException.ThrowIfNull(stylesheetUri, "stylesheetUri");
		LoadInternal(stylesheetUri, settings, stylesheetResolver, stylesheetResolver);
	}

	private void LoadInternal(object stylesheet, XsltSettings settings, XmlResolver stylesheetResolver, XmlResolver originalStylesheetResolver)
	{
		ArgumentNullException.ThrowIfNull(stylesheet, "stylesheet");
		if (settings == null)
		{
			settings = XsltSettings.Default;
		}
		CompileXsltToQil(stylesheet, settings, stylesheetResolver, originalStylesheetResolver);
		CompilerError firstError = GetFirstError();
		if (firstError != null)
		{
			throw new XslLoadException(firstError);
		}
		if (!settings.CheckOnly)
		{
			CompileQilToMsil();
		}
	}

	[MemberNotNull("_compilerErrorColl")]
	[MemberNotNull("_qil")]
	private void CompileXsltToQil(object stylesheet, XsltSettings settings, XmlResolver stylesheetResolver, XmlResolver originalStylesheetResolver)
	{
		_compilerErrorColl = new Compiler(settings, _enableDebug, null).Compile(stylesheet, stylesheetResolver, originalStylesheetResolver, out _qil);
	}

	private CompilerError GetFirstError()
	{
		foreach (CompilerError item in _compilerErrorColl)
		{
			if (!item.IsWarning)
			{
				return item;
			}
		}
		return null;
	}

	private void CompileQilToMsil()
	{
		_command = new XmlILGenerator().Generate(_qil, null);
		OutputSettings = _command.StaticData.DefaultWriterSettings;
		_qil = null;
	}

	[RequiresUnreferencedCode("This method will get fields and types from the assembly of the passed in compiledStylesheet and call their constructors which cannot be statically analyzed")]
	public void Load(Type compiledStylesheet)
	{
		Reset();
		ArgumentNullException.ThrowIfNull(compiledStylesheet, "compiledStylesheet");
		object[] customAttributes = compiledStylesheet.GetCustomAttributes(typeof(GeneratedCodeAttribute), inherit: false);
		GeneratedCodeAttribute generatedCodeAttribute = ((customAttributes.Length != 0) ? ((GeneratedCodeAttribute)customAttributes[0]) : null);
		if (generatedCodeAttribute != null && generatedCodeAttribute.Tool == typeof(XslCompiledTransform).FullName)
		{
			if (s_version < Version.Parse(generatedCodeAttribute.Version))
			{
				throw new ArgumentException(System.SR.Format(System.SR.Xslt_IncompatibleCompiledStylesheetVersion, generatedCodeAttribute.Version, s_version), "compiledStylesheet");
			}
			FieldInfo field = compiledStylesheet.GetField("staticData", BindingFlags.Static | BindingFlags.NonPublic);
			FieldInfo field2 = compiledStylesheet.GetField("ebTypes", BindingFlags.Static | BindingFlags.NonPublic);
			if (field != null && field2 != null && field.GetValue(null) is byte[] queryData)
			{
				MethodInfo method = compiledStylesheet.GetMethod("Execute", BindingFlags.Static | BindingFlags.NonPublic);
				Type[] earlyBoundTypes = (Type[])field2.GetValue(null);
				Load(method, queryData, earlyBoundTypes);
				return;
			}
		}
		if (_command == null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Xslt_NotCompiledStylesheet, compiledStylesheet.FullName), "compiledStylesheet");
		}
	}

	[RequiresUnreferencedCode("This method will call into constructors of the earlyBoundTypes array which cannot be statically analyzed.")]
	public void Load(MethodInfo executeMethod, byte[] queryData, Type[]? earlyBoundTypes)
	{
		Reset();
		ArgumentNullException.ThrowIfNull(executeMethod, "executeMethod");
		ArgumentNullException.ThrowIfNull(queryData, "queryData");
		Delegate @delegate = ((executeMethod is DynamicMethod dynamicMethod) ? dynamicMethod.CreateDelegate(typeof(ExecuteDelegate)) : executeMethod.CreateDelegate(typeof(ExecuteDelegate)));
		_command = new XmlILCommand((ExecuteDelegate)@delegate, new XmlQueryStaticData(queryData, earlyBoundTypes));
		OutputSettings = _command.StaticData.DefaultWriterSettings;
	}

	public void Transform(IXPathNavigable input, XmlWriter results)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		ArgumentNullException.ThrowIfNull(results, "results");
		Transform(input, null, results, CreateDefaultResolver());
	}

	public void Transform(IXPathNavigable input, XsltArgumentList? arguments, XmlWriter results)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		ArgumentNullException.ThrowIfNull(results, "results");
		Transform(input, arguments, results, CreateDefaultResolver());
	}

	public void Transform(IXPathNavigable input, XsltArgumentList? arguments, TextWriter results)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		ArgumentNullException.ThrowIfNull(results, "results");
		using XmlWriter results2 = XmlWriter.Create(results, OutputSettings);
		Transform(input, arguments, results2, CreateDefaultResolver());
	}

	public void Transform(IXPathNavigable input, XsltArgumentList? arguments, Stream results)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		ArgumentNullException.ThrowIfNull(results, "results");
		using XmlWriter results2 = XmlWriter.Create(results, OutputSettings);
		Transform(input, arguments, results2, CreateDefaultResolver());
	}

	public void Transform(XmlReader input, XmlWriter results)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		ArgumentNullException.ThrowIfNull(results, "results");
		Transform(input, null, results, CreateDefaultResolver());
	}

	public void Transform(XmlReader input, XsltArgumentList? arguments, XmlWriter results)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		ArgumentNullException.ThrowIfNull(results, "results");
		Transform(input, arguments, results, CreateDefaultResolver());
	}

	public void Transform(XmlReader input, XsltArgumentList? arguments, TextWriter results)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		ArgumentNullException.ThrowIfNull(results, "results");
		using XmlWriter results2 = XmlWriter.Create(results, OutputSettings);
		Transform(input, arguments, results2, CreateDefaultResolver());
	}

	public void Transform(XmlReader input, XsltArgumentList? arguments, Stream results)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		ArgumentNullException.ThrowIfNull(results, "results");
		using XmlWriter results2 = XmlWriter.Create(results, OutputSettings);
		Transform(input, arguments, results2, CreateDefaultResolver());
	}

	public void Transform(string inputUri, XmlWriter results)
	{
		ArgumentNullException.ThrowIfNull(inputUri, "inputUri");
		ArgumentNullException.ThrowIfNull(results, "results");
		using XmlReader input = XmlReader.Create(inputUri);
		Transform(input, null, results, CreateDefaultResolver());
	}

	public void Transform(string inputUri, XsltArgumentList? arguments, XmlWriter results)
	{
		ArgumentNullException.ThrowIfNull(inputUri, "inputUri");
		ArgumentNullException.ThrowIfNull(results, "results");
		using XmlReader input = XmlReader.Create(inputUri);
		Transform(input, arguments, results, CreateDefaultResolver());
	}

	public void Transform(string inputUri, XsltArgumentList? arguments, TextWriter results)
	{
		ArgumentNullException.ThrowIfNull(inputUri, "inputUri");
		ArgumentNullException.ThrowIfNull(results, "results");
		using XmlReader input = XmlReader.Create(inputUri);
		using XmlWriter results2 = XmlWriter.Create(results, OutputSettings);
		Transform(input, arguments, results2, CreateDefaultResolver());
	}

	public void Transform(string inputUri, XsltArgumentList? arguments, Stream results)
	{
		ArgumentNullException.ThrowIfNull(inputUri, "inputUri");
		ArgumentNullException.ThrowIfNull(results, "results");
		using XmlReader input = XmlReader.Create(inputUri);
		using XmlWriter results2 = XmlWriter.Create(results, OutputSettings);
		Transform(input, arguments, results2, CreateDefaultResolver());
	}

	public void Transform(string inputUri, string resultsFile)
	{
		ArgumentNullException.ThrowIfNull(inputUri, "inputUri");
		ArgumentNullException.ThrowIfNull(resultsFile, "resultsFile");
		using XmlReader input = XmlReader.Create(inputUri);
		using XmlWriter results = XmlWriter.Create(resultsFile, OutputSettings);
		Transform(input, null, results, CreateDefaultResolver());
	}

	public void Transform(XmlReader input, XsltArgumentList? arguments, XmlWriter results, XmlResolver? documentResolver)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		ArgumentNullException.ThrowIfNull(results, "results");
		CheckCommand();
		_command.Execute(input, documentResolver, arguments, results);
	}

	public void Transform(IXPathNavigable input, XsltArgumentList? arguments, XmlWriter results, XmlResolver? documentResolver)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		ArgumentNullException.ThrowIfNull(results, "results");
		CheckCommand();
		_command.Execute(input.CreateNavigator(), documentResolver, arguments, results);
	}

	[MemberNotNull("_command")]
	private void CheckCommand()
	{
		if (_command == null)
		{
			throw new InvalidOperationException(System.SR.Xslt_NoStylesheetLoaded);
		}
	}

	private static XmlResolver CreateDefaultResolver()
	{
		if (LocalAppContextSwitches.AllowDefaultResolver)
		{
			return XmlReaderSettings.GetDefaultPermissiveResolver();
		}
		return XmlResolver.ThrowingResolver;
	}
}
