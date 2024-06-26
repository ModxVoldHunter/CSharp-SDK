using System.CodeDom.Compiler;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace System.ComponentModel.Design;

public class DesignerVerb : MenuCommand
{
	public string Description
	{
		get
		{
			object obj = Properties["Description"];
			if (obj == null)
			{
				return string.Empty;
			}
			return (string)obj;
		}
		set
		{
			Properties["Description"] = value;
		}
	}

	public string Text
	{
		get
		{
			object obj = Properties["Text"];
			if (obj == null)
			{
				return string.Empty;
			}
			return (string)obj;
		}
	}

	public DesignerVerb(string text, EventHandler handler)
		: this(text, handler, StandardCommands.VerbFirst)
	{
	}

	public DesignerVerb(string text, EventHandler handler, CommandID startCommandID)
		: base(handler, startCommandID)
	{
		Properties["Text"] = ((text == null) ? null : GetParameterReplacementRegex().Replace(text, ""));
	}

	[GeneratedRegex("\\(\\&.\\)")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "8.0.10.26715")]
	private static Regex GetParameterReplacementRegex()
	{
		return _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__GetParameterReplacementRegex_0.Instance;
	}

	public override string ToString()
	{
		return Text + " : " + base.ToString();
	}
}
