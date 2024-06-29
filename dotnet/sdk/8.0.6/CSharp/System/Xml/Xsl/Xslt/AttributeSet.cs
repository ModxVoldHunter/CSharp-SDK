using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt;

internal sealed class AttributeSet : ProtoTemplate
{
	public CycleCheck CycleCheck;

	public AttributeSet(QilName name, XslVersion xslVer)
		: base(XslNodeType.AttributeSet, name, xslVer)
	{
	}

	public override string GetDebugName()
	{
		return "<xsl:attribute-set name=\"" + Name.QualifiedName + "\">";
	}

	public new void AddContent(XslNode node)
	{
		base.AddContent(node);
	}

	public void MergeContent(AttributeSet other)
	{
		InsertContent(other.Content);
	}
}
