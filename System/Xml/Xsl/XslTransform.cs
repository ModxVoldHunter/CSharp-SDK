using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.XPath;
using System.Xml.Xsl.XsltOld;

namespace System.Xml.Xsl;

public sealed class XslTransform
{
	private XmlResolver _documentResolver;

	private bool _isDocumentResolverSet;

	private Stylesheet _CompiledStylesheet;

	private List<TheQuery> _QueryStore;

	private RootAction _RootAction;

	private XmlResolver? _DocumentResolver
	{
		get
		{
			if (_isDocumentResolverSet)
			{
				return _documentResolver;
			}
			return CreateDefaultResolver();
		}
	}

	public XmlResolver? XmlResolver
	{
		set
		{
			_documentResolver = value;
			_isDocumentResolverSet = true;
		}
	}

	public void Load(XmlReader stylesheet)
	{
		Load(stylesheet, CreateDefaultResolver());
	}

	public void Load(XmlReader stylesheet, XmlResolver? resolver)
	{
		ArgumentNullException.ThrowIfNull(stylesheet, "stylesheet");
		Load(new XPathDocument(stylesheet, XmlSpace.Preserve), resolver);
	}

	public void Load(IXPathNavigable stylesheet)
	{
		Load(stylesheet, CreateDefaultResolver());
	}

	public void Load(IXPathNavigable stylesheet, XmlResolver? resolver)
	{
		ArgumentNullException.ThrowIfNull(stylesheet, "stylesheet");
		Load(stylesheet.CreateNavigator(), resolver);
	}

	public void Load(XPathNavigator stylesheet)
	{
		ArgumentNullException.ThrowIfNull(stylesheet, "stylesheet");
		Load(stylesheet, CreateDefaultResolver());
	}

	public void Load(XPathNavigator stylesheet, XmlResolver? resolver)
	{
		ArgumentNullException.ThrowIfNull(stylesheet, "stylesheet");
		Compile(stylesheet, resolver);
	}

	public void Load([StringSyntax("Uri")] string url)
	{
		XmlTextReaderImpl reader = new XmlTextReaderImpl(url);
		Compile(Compiler.LoadDocument(reader).CreateNavigator(), CreateDefaultResolver());
	}

	public void Load([StringSyntax("Uri")] string url, XmlResolver? resolver)
	{
		XmlTextReaderImpl xmlTextReaderImpl = new XmlTextReaderImpl(url);
		xmlTextReaderImpl.XmlResolver = resolver;
		Compile(Compiler.LoadDocument(xmlTextReaderImpl).CreateNavigator(), resolver);
	}

	[MemberNotNull("_CompiledStylesheet")]
	[MemberNotNull("_QueryStore")]
	[MemberNotNull("_RootAction")]
	private void CheckCommand()
	{
		if (_CompiledStylesheet == null)
		{
			throw new InvalidOperationException(System.SR.Xslt_NoStylesheetLoaded);
		}
	}

	public XmlReader Transform(XPathNavigator input, XsltArgumentList? args, XmlResolver? resolver)
	{
		CheckCommand();
		Processor processor = new Processor(input, args, resolver, _CompiledStylesheet, _QueryStore, _RootAction, null);
		return processor.StartReader();
	}

	public XmlReader Transform(XPathNavigator input, XsltArgumentList? args)
	{
		return Transform(input, args, _DocumentResolver);
	}

	public void Transform(XPathNavigator input, XsltArgumentList? args, XmlWriter output, XmlResolver? resolver)
	{
		CheckCommand();
		Processor processor = new Processor(input, args, resolver, _CompiledStylesheet, _QueryStore, _RootAction, null);
		processor.Execute(output);
	}

	public void Transform(XPathNavigator input, XsltArgumentList? args, XmlWriter output)
	{
		Transform(input, args, output, _DocumentResolver);
	}

	public void Transform(XPathNavigator input, XsltArgumentList? args, Stream output, XmlResolver? resolver)
	{
		CheckCommand();
		Processor processor = new Processor(input, args, resolver, _CompiledStylesheet, _QueryStore, _RootAction, null);
		processor.Execute(output);
	}

	public void Transform(XPathNavigator input, XsltArgumentList? args, Stream output)
	{
		Transform(input, args, output, _DocumentResolver);
	}

	public void Transform(XPathNavigator input, XsltArgumentList? args, TextWriter output, XmlResolver? resolver)
	{
		CheckCommand();
		Processor processor = new Processor(input, args, resolver, _CompiledStylesheet, _QueryStore, _RootAction, null);
		processor.Execute(output);
	}

	public void Transform(XPathNavigator input, XsltArgumentList? args, TextWriter output)
	{
		CheckCommand();
		Processor processor = new Processor(input, args, _DocumentResolver, _CompiledStylesheet, _QueryStore, _RootAction, null);
		processor.Execute(output);
	}

	public XmlReader Transform(IXPathNavigable input, XsltArgumentList? args, XmlResolver? resolver)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		return Transform(input.CreateNavigator(), args, resolver);
	}

	public XmlReader Transform(IXPathNavigable input, XsltArgumentList? args)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		return Transform(input.CreateNavigator(), args, _DocumentResolver);
	}

	public void Transform(IXPathNavigable input, XsltArgumentList? args, TextWriter output, XmlResolver? resolver)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		Transform(input.CreateNavigator(), args, output, resolver);
	}

	public void Transform(IXPathNavigable input, XsltArgumentList? args, TextWriter output)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		Transform(input.CreateNavigator(), args, output, _DocumentResolver);
	}

	public void Transform(IXPathNavigable input, XsltArgumentList? args, Stream output, XmlResolver? resolver)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		Transform(input.CreateNavigator(), args, output, resolver);
	}

	public void Transform(IXPathNavigable input, XsltArgumentList? args, Stream output)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		Transform(input.CreateNavigator(), args, output, _DocumentResolver);
	}

	public void Transform(IXPathNavigable input, XsltArgumentList? args, XmlWriter output, XmlResolver? resolver)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		Transform(input.CreateNavigator(), args, output, resolver);
	}

	public void Transform(IXPathNavigable input, XsltArgumentList? args, XmlWriter output)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		Transform(input.CreateNavigator(), args, output, _DocumentResolver);
	}

	public void Transform(string inputfile, string outputfile, XmlResolver? resolver)
	{
		FileStream fileStream = null;
		try
		{
			XPathDocument input = new XPathDocument(inputfile);
			fileStream = new FileStream(outputfile, FileMode.Create, FileAccess.ReadWrite);
			Transform(input, null, fileStream, resolver);
		}
		finally
		{
			fileStream?.Dispose();
		}
	}

	public void Transform(string inputfile, string outputfile)
	{
		Transform(inputfile, outputfile, _DocumentResolver);
	}

	private void Compile(XPathNavigator stylesheet, XmlResolver resolver)
	{
		Compiler compiler = new Compiler();
		NavigatorInput input = new NavigatorInput(stylesheet);
		compiler.Compile(input, resolver ?? System.Xml.XmlResolver.ThrowingResolver);
		_CompiledStylesheet = compiler.CompiledStylesheet;
		_QueryStore = compiler.QueryStore;
		_RootAction = compiler.RootAction;
	}

	private static XmlResolver CreateDefaultResolver()
	{
		if (LocalAppContextSwitches.AllowDefaultResolver)
		{
			return XmlReaderSettings.GetDefaultPermissiveResolver();
		}
		return System.Xml.XmlResolver.ThrowingResolver;
	}
}
