using System.Xml.Schema;

namespace System.Xml.Xsl.Qil;

internal static class QilTypeChecker
{
	public static XmlQueryType Check(QilNode n)
	{
		return n.NodeType switch
		{
			QilNodeType.QilExpression => CheckQilExpression((QilExpression)n), 
			QilNodeType.FunctionList => CheckFunctionList((QilList)n), 
			QilNodeType.GlobalVariableList => CheckGlobalVariableList((QilList)n), 
			QilNodeType.GlobalParameterList => CheckGlobalParameterList((QilList)n), 
			QilNodeType.ActualParameterList => CheckActualParameterList((QilList)n), 
			QilNodeType.FormalParameterList => CheckFormalParameterList((QilList)n), 
			QilNodeType.SortKeyList => CheckSortKeyList((QilList)n), 
			QilNodeType.BranchList => CheckBranchList((QilList)n), 
			QilNodeType.OptimizeBarrier => CheckOptimizeBarrier((QilUnary)n), 
			QilNodeType.Unknown => CheckUnknown(n), 
			QilNodeType.DataSource => CheckDataSource((QilDataSource)n), 
			QilNodeType.Nop => CheckNop((QilUnary)n), 
			QilNodeType.Error => CheckError((QilUnary)n), 
			QilNodeType.Warning => CheckWarning((QilUnary)n), 
			QilNodeType.For => CheckFor((QilIterator)n), 
			QilNodeType.Let => CheckLet((QilIterator)n), 
			QilNodeType.Parameter => CheckParameter((QilParameter)n), 
			QilNodeType.PositionOf => CheckPositionOf(), 
			QilNodeType.True => CheckTrue(), 
			QilNodeType.False => CheckFalse(), 
			QilNodeType.LiteralString => CheckLiteralString((QilLiteral)n), 
			QilNodeType.LiteralInt32 => CheckLiteralInt32((QilLiteral)n), 
			QilNodeType.LiteralInt64 => CheckLiteralInt64((QilLiteral)n), 
			QilNodeType.LiteralDouble => CheckLiteralDouble((QilLiteral)n), 
			QilNodeType.LiteralDecimal => CheckLiteralDecimal((QilLiteral)n), 
			QilNodeType.LiteralQName => CheckLiteralQName((QilName)n), 
			QilNodeType.LiteralType => CheckLiteralType((QilLiteral)n), 
			QilNodeType.LiteralObject => CheckLiteralObject((QilLiteral)n), 
			QilNodeType.And => CheckAnd((QilBinary)n), 
			QilNodeType.Or => CheckOr((QilBinary)n), 
			QilNodeType.Not => CheckNot((QilUnary)n), 
			QilNodeType.Conditional => CheckConditional((QilTernary)n), 
			QilNodeType.Choice => CheckChoice((QilChoice)n), 
			QilNodeType.Length => CheckLength(), 
			QilNodeType.Sequence => CheckSequence((QilList)n), 
			QilNodeType.Union => CheckUnion((QilBinary)n), 
			QilNodeType.Intersection => CheckIntersection((QilBinary)n), 
			QilNodeType.Difference => CheckDifference((QilBinary)n), 
			QilNodeType.Average => CheckAverage((QilUnary)n), 
			QilNodeType.Sum => CheckSum((QilUnary)n), 
			QilNodeType.Minimum => CheckMinimum((QilUnary)n), 
			QilNodeType.Maximum => CheckMaximum((QilUnary)n), 
			QilNodeType.Negate => CheckNegate((QilUnary)n), 
			QilNodeType.Add => CheckAdd((QilBinary)n), 
			QilNodeType.Subtract => CheckSubtract((QilBinary)n), 
			QilNodeType.Multiply => CheckMultiply((QilBinary)n), 
			QilNodeType.Divide => CheckDivide((QilBinary)n), 
			QilNodeType.Modulo => CheckModulo((QilBinary)n), 
			QilNodeType.StrLength => CheckStrLength((QilUnary)n), 
			QilNodeType.StrConcat => CheckStrConcat((QilStrConcat)n), 
			QilNodeType.StrParseQName => CheckStrParseQName((QilBinary)n), 
			QilNodeType.Ne => CheckNe((QilBinary)n), 
			QilNodeType.Eq => CheckEq((QilBinary)n), 
			QilNodeType.Gt => CheckGt((QilBinary)n), 
			QilNodeType.Ge => CheckGe((QilBinary)n), 
			QilNodeType.Lt => CheckLt((QilBinary)n), 
			QilNodeType.Le => CheckLe((QilBinary)n), 
			QilNodeType.Is => CheckIs((QilBinary)n), 
			QilNodeType.After => CheckAfter((QilBinary)n), 
			QilNodeType.Before => CheckBefore((QilBinary)n), 
			QilNodeType.Loop => CheckLoop((QilLoop)n), 
			QilNodeType.Filter => CheckFilter((QilLoop)n), 
			QilNodeType.Sort => CheckSort((QilLoop)n), 
			QilNodeType.SortKey => CheckSortKey((QilSortKey)n), 
			QilNodeType.DocOrderDistinct => CheckDocOrderDistinct((QilUnary)n), 
			QilNodeType.Function => CheckFunction((QilFunction)n), 
			QilNodeType.Invoke => CheckInvoke((QilInvoke)n), 
			QilNodeType.Content => CheckContent((QilUnary)n), 
			QilNodeType.Attribute => CheckAttribute((QilBinary)n), 
			QilNodeType.Parent => CheckParent((QilUnary)n), 
			QilNodeType.Root => CheckRoot((QilUnary)n), 
			QilNodeType.XmlContext => CheckXmlContext(), 
			QilNodeType.Descendant => CheckDescendant((QilUnary)n), 
			QilNodeType.DescendantOrSelf => CheckDescendantOrSelf((QilUnary)n), 
			QilNodeType.Ancestor => CheckAncestor((QilUnary)n), 
			QilNodeType.AncestorOrSelf => CheckAncestorOrSelf((QilUnary)n), 
			QilNodeType.Preceding => CheckPreceding((QilUnary)n), 
			QilNodeType.FollowingSibling => CheckFollowingSibling((QilUnary)n), 
			QilNodeType.PrecedingSibling => CheckPrecedingSibling((QilUnary)n), 
			QilNodeType.NodeRange => CheckNodeRange((QilBinary)n), 
			QilNodeType.Deref => CheckDeref((QilBinary)n), 
			QilNodeType.ElementCtor => CheckElementCtor((QilBinary)n), 
			QilNodeType.AttributeCtor => CheckAttributeCtor((QilBinary)n), 
			QilNodeType.CommentCtor => CheckCommentCtor((QilUnary)n), 
			QilNodeType.PICtor => CheckPICtor((QilBinary)n), 
			QilNodeType.TextCtor => CheckTextCtor((QilUnary)n), 
			QilNodeType.RawTextCtor => CheckRawTextCtor((QilUnary)n), 
			QilNodeType.DocumentCtor => CheckDocumentCtor((QilUnary)n), 
			QilNodeType.NamespaceDecl => CheckNamespaceDecl((QilBinary)n), 
			QilNodeType.RtfCtor => CheckRtfCtor((QilBinary)n), 
			QilNodeType.NameOf => CheckNameOf((QilUnary)n), 
			QilNodeType.LocalNameOf => CheckLocalNameOf((QilUnary)n), 
			QilNodeType.NamespaceUriOf => CheckNamespaceUriOf((QilUnary)n), 
			QilNodeType.PrefixOf => CheckPrefixOf((QilUnary)n), 
			QilNodeType.TypeAssert => CheckTypeAssert((QilTargetType)n), 
			QilNodeType.IsType => CheckIsType((QilTargetType)n), 
			QilNodeType.IsEmpty => CheckIsEmpty(), 
			QilNodeType.XPathNodeValue => CheckXPathNodeValue((QilUnary)n), 
			QilNodeType.XPathFollowing => CheckXPathFollowing((QilUnary)n), 
			QilNodeType.XPathPreceding => CheckXPathPreceding((QilUnary)n), 
			QilNodeType.XPathNamespace => CheckXPathNamespace((QilUnary)n), 
			QilNodeType.XsltGenerateId => CheckXsltGenerateId((QilUnary)n), 
			QilNodeType.XsltInvokeLateBound => CheckXsltInvokeLateBound((QilInvokeLateBound)n), 
			QilNodeType.XsltInvokeEarlyBound => CheckXsltInvokeEarlyBound((QilInvokeEarlyBound)n), 
			QilNodeType.XsltCopy => CheckXsltCopy((QilBinary)n), 
			QilNodeType.XsltCopyOf => CheckXsltCopyOf((QilUnary)n), 
			QilNodeType.XsltConvert => CheckXsltConvert((QilTargetType)n), 
			_ => CheckUnknown(n), 
		};
	}

	public static XmlQueryType CheckQilExpression(QilExpression node)
	{
		return XmlQueryTypeFactory.ItemS;
	}

	public static XmlQueryType CheckFunctionList(QilList node)
	{
		foreach (QilNode item in node)
		{
		}
		return node.XmlType;
	}

	public static XmlQueryType CheckGlobalVariableList(QilList node)
	{
		foreach (QilNode item in node)
		{
		}
		return node.XmlType;
	}

	public static XmlQueryType CheckGlobalParameterList(QilList node)
	{
		foreach (QilNode item in node)
		{
		}
		return node.XmlType;
	}

	public static XmlQueryType CheckActualParameterList(QilList node)
	{
		return node.XmlType;
	}

	public static XmlQueryType CheckFormalParameterList(QilList node)
	{
		foreach (QilNode item in node)
		{
		}
		return node.XmlType;
	}

	public static XmlQueryType CheckSortKeyList(QilList node)
	{
		foreach (QilNode item in node)
		{
		}
		return node.XmlType;
	}

	public static XmlQueryType CheckBranchList(QilList node)
	{
		return node.XmlType;
	}

	public static XmlQueryType CheckOptimizeBarrier(QilUnary node)
	{
		return node.Child.XmlType;
	}

	public static XmlQueryType CheckUnknown(QilNode node)
	{
		return node.XmlType;
	}

	public static XmlQueryType CheckDataSource(QilDataSource node)
	{
		return XmlQueryTypeFactory.NodeNotRtfQ;
	}

	public static XmlQueryType CheckNop(QilUnary node)
	{
		return node.Child.XmlType;
	}

	public static XmlQueryType CheckError(QilUnary node)
	{
		return XmlQueryTypeFactory.None;
	}

	public static XmlQueryType CheckWarning(QilUnary node)
	{
		return XmlQueryTypeFactory.Empty;
	}

	public static XmlQueryType CheckFor(QilIterator node)
	{
		return node.Binding.XmlType.Prime;
	}

	public static XmlQueryType CheckLet(QilIterator node)
	{
		return node.Binding.XmlType;
	}

	public static XmlQueryType CheckParameter(QilParameter node)
	{
		return node.XmlType;
	}

	public static XmlQueryType CheckPositionOf()
	{
		return XmlQueryTypeFactory.IntX;
	}

	public static XmlQueryType CheckTrue()
	{
		return XmlQueryTypeFactory.BooleanX;
	}

	public static XmlQueryType CheckFalse()
	{
		return XmlQueryTypeFactory.BooleanX;
	}

	public static XmlQueryType CheckLiteralString(QilLiteral node)
	{
		return XmlQueryTypeFactory.StringX;
	}

	public static XmlQueryType CheckLiteralInt32(QilLiteral node)
	{
		return XmlQueryTypeFactory.IntX;
	}

	public static XmlQueryType CheckLiteralInt64(QilLiteral node)
	{
		return XmlQueryTypeFactory.IntegerX;
	}

	public static XmlQueryType CheckLiteralDouble(QilLiteral node)
	{
		return XmlQueryTypeFactory.DoubleX;
	}

	public static XmlQueryType CheckLiteralDecimal(QilLiteral node)
	{
		return XmlQueryTypeFactory.DecimalX;
	}

	public static XmlQueryType CheckLiteralQName(QilName node)
	{
		return XmlQueryTypeFactory.QNameX;
	}

	public static XmlQueryType CheckLiteralType(QilLiteral node)
	{
		return node;
	}

	public static XmlQueryType CheckLiteralObject(QilLiteral node)
	{
		return XmlQueryTypeFactory.ItemS;
	}

	public static XmlQueryType CheckAnd(QilBinary node)
	{
		return XmlQueryTypeFactory.BooleanX;
	}

	public static XmlQueryType CheckOr(QilBinary node)
	{
		return CheckAnd(node);
	}

	public static XmlQueryType CheckNot(QilUnary node)
	{
		return XmlQueryTypeFactory.BooleanX;
	}

	public static XmlQueryType CheckConditional(QilTernary node)
	{
		return XmlQueryTypeFactory.Choice(node.Center.XmlType, node.Right.XmlType);
	}

	public static XmlQueryType CheckChoice(QilChoice node)
	{
		return node.Branches.XmlType;
	}

	public static XmlQueryType CheckLength()
	{
		return XmlQueryTypeFactory.IntX;
	}

	public static XmlQueryType CheckSequence(QilList node)
	{
		return node.XmlType;
	}

	public static XmlQueryType CheckUnion(QilBinary node)
	{
		return DistinctType(XmlQueryTypeFactory.Sequence(node.Left.XmlType, node.Right.XmlType));
	}

	public static XmlQueryType CheckIntersection(QilBinary node)
	{
		return CheckUnion(node);
	}

	public static XmlQueryType CheckDifference(QilBinary node)
	{
		return XmlQueryTypeFactory.AtMost(node.Left.XmlType, node.Left.XmlType.Cardinality);
	}

	public static XmlQueryType CheckAverage(QilUnary node)
	{
		XmlQueryType xmlType = node.Child.XmlType;
		return XmlQueryTypeFactory.PrimeProduct(xmlType, xmlType.MaybeEmpty ? XmlQueryCardinality.ZeroOrOne : XmlQueryCardinality.One);
	}

	public static XmlQueryType CheckSum(QilUnary node)
	{
		return CheckAverage(node);
	}

	public static XmlQueryType CheckMinimum(QilUnary node)
	{
		return CheckAverage(node);
	}

	public static XmlQueryType CheckMaximum(QilUnary node)
	{
		return CheckAverage(node);
	}

	public static XmlQueryType CheckNegate(QilUnary node)
	{
		return node.Child.XmlType;
	}

	public static XmlQueryType CheckAdd(QilBinary node)
	{
		if (node.Left.XmlType.TypeCode != 0)
		{
			return node.Left.XmlType;
		}
		return node.Right.XmlType;
	}

	public static XmlQueryType CheckSubtract(QilBinary node)
	{
		return CheckAdd(node);
	}

	public static XmlQueryType CheckMultiply(QilBinary node)
	{
		return CheckAdd(node);
	}

	public static XmlQueryType CheckDivide(QilBinary node)
	{
		return CheckAdd(node);
	}

	public static XmlQueryType CheckModulo(QilBinary node)
	{
		return CheckAdd(node);
	}

	public static XmlQueryType CheckStrLength(QilUnary node)
	{
		return XmlQueryTypeFactory.IntX;
	}

	public static XmlQueryType CheckStrConcat(QilStrConcat node)
	{
		return XmlQueryTypeFactory.StringX;
	}

	public static XmlQueryType CheckStrParseQName(QilBinary node)
	{
		return XmlQueryTypeFactory.QNameX;
	}

	public static XmlQueryType CheckNe(QilBinary node)
	{
		return XmlQueryTypeFactory.BooleanX;
	}

	public static XmlQueryType CheckEq(QilBinary node)
	{
		return CheckNe(node);
	}

	public static XmlQueryType CheckGt(QilBinary node)
	{
		return CheckNe(node);
	}

	public static XmlQueryType CheckGe(QilBinary node)
	{
		return CheckNe(node);
	}

	public static XmlQueryType CheckLt(QilBinary node)
	{
		return CheckNe(node);
	}

	public static XmlQueryType CheckLe(QilBinary node)
	{
		return CheckNe(node);
	}

	public static XmlQueryType CheckIs(QilBinary node)
	{
		return XmlQueryTypeFactory.BooleanX;
	}

	public static XmlQueryType CheckAfter(QilBinary node)
	{
		return CheckIs(node);
	}

	public static XmlQueryType CheckBefore(QilBinary node)
	{
		return CheckIs(node);
	}

	public static XmlQueryType CheckLoop(QilLoop node)
	{
		XmlQueryType xmlType = node.Body.XmlType;
		XmlQueryCardinality xmlQueryCardinality = ((node.Variable.NodeType == QilNodeType.Let) ? XmlQueryCardinality.One : node.Variable.Binding.XmlType.Cardinality);
		return XmlQueryTypeFactory.PrimeProduct(xmlType, xmlQueryCardinality * xmlType.Cardinality);
	}

	public static XmlQueryType CheckFilter(QilLoop node)
	{
		XmlQueryType xmlQueryType = FindFilterType(node.Variable, node.Body);
		if (xmlQueryType != null)
		{
			return xmlQueryType;
		}
		return XmlQueryTypeFactory.AtMost(node.Variable.Binding.XmlType, node.Variable.Binding.XmlType.Cardinality);
	}

	public static XmlQueryType CheckSort(QilLoop node)
	{
		XmlQueryType xmlType = node.Variable.Binding.XmlType;
		return XmlQueryTypeFactory.PrimeProduct(xmlType, xmlType.Cardinality);
	}

	public static XmlQueryType CheckSortKey(QilSortKey node)
	{
		return node.Key.XmlType;
	}

	public static XmlQueryType CheckDocOrderDistinct(QilUnary node)
	{
		return DistinctType(node.Child.XmlType);
	}

	public static XmlQueryType CheckFunction(QilFunction node)
	{
		return node.XmlType;
	}

	public static XmlQueryType CheckInvoke(QilInvoke node)
	{
		return node.Function.XmlType;
	}

	public static XmlQueryType CheckContent(QilUnary node)
	{
		return XmlQueryTypeFactory.AttributeOrContentS;
	}

	public static XmlQueryType CheckAttribute(QilBinary node)
	{
		return XmlQueryTypeFactory.AttributeQ;
	}

	public static XmlQueryType CheckParent(QilUnary node)
	{
		return XmlQueryTypeFactory.DocumentOrElementQ;
	}

	public static XmlQueryType CheckRoot(QilUnary node)
	{
		return XmlQueryTypeFactory.NodeNotRtf;
	}

	public static XmlQueryType CheckXmlContext()
	{
		return XmlQueryTypeFactory.NodeNotRtf;
	}

	public static XmlQueryType CheckDescendant(QilUnary node)
	{
		return XmlQueryTypeFactory.ContentS;
	}

	public static XmlQueryType CheckDescendantOrSelf(QilUnary node)
	{
		return XmlQueryTypeFactory.Choice(node.Child.XmlType, XmlQueryTypeFactory.ContentS);
	}

	public static XmlQueryType CheckAncestor(QilUnary node)
	{
		return XmlQueryTypeFactory.DocumentOrElementS;
	}

	public static XmlQueryType CheckAncestorOrSelf(QilUnary node)
	{
		return XmlQueryTypeFactory.Choice(node.Child.XmlType, XmlQueryTypeFactory.DocumentOrElementS);
	}

	public static XmlQueryType CheckPreceding(QilUnary node)
	{
		return XmlQueryTypeFactory.DocumentOrContentS;
	}

	public static XmlQueryType CheckFollowingSibling(QilUnary node)
	{
		return XmlQueryTypeFactory.ContentS;
	}

	public static XmlQueryType CheckPrecedingSibling(QilUnary node)
	{
		return XmlQueryTypeFactory.ContentS;
	}

	public static XmlQueryType CheckNodeRange(QilBinary node)
	{
		return XmlQueryTypeFactory.Choice(node.Left.XmlType, XmlQueryTypeFactory.ContentS, node.Right.XmlType);
	}

	public static XmlQueryType CheckDeref(QilBinary node)
	{
		return XmlQueryTypeFactory.ElementS;
	}

	public static XmlQueryType CheckElementCtor(QilBinary node)
	{
		return XmlQueryTypeFactory.UntypedElement;
	}

	public static XmlQueryType CheckAttributeCtor(QilBinary node)
	{
		return XmlQueryTypeFactory.UntypedAttribute;
	}

	public static XmlQueryType CheckCommentCtor(QilUnary node)
	{
		return XmlQueryTypeFactory.Comment;
	}

	public static XmlQueryType CheckPICtor(QilBinary node)
	{
		return XmlQueryTypeFactory.PI;
	}

	public static XmlQueryType CheckTextCtor(QilUnary node)
	{
		return XmlQueryTypeFactory.Text;
	}

	public static XmlQueryType CheckRawTextCtor(QilUnary node)
	{
		return XmlQueryTypeFactory.Text;
	}

	public static XmlQueryType CheckDocumentCtor(QilUnary node)
	{
		return XmlQueryTypeFactory.UntypedDocument;
	}

	public static XmlQueryType CheckNamespaceDecl(QilBinary node)
	{
		return XmlQueryTypeFactory.Namespace;
	}

	public static XmlQueryType CheckRtfCtor(QilBinary node)
	{
		return XmlQueryTypeFactory.Node;
	}

	public static XmlQueryType CheckNameOf(QilUnary node)
	{
		return XmlQueryTypeFactory.QNameX;
	}

	public static XmlQueryType CheckLocalNameOf(QilUnary node)
	{
		return XmlQueryTypeFactory.StringX;
	}

	public static XmlQueryType CheckNamespaceUriOf(QilUnary node)
	{
		return XmlQueryTypeFactory.StringX;
	}

	public static XmlQueryType CheckPrefixOf(QilUnary node)
	{
		return XmlQueryTypeFactory.StringX;
	}

	public static XmlQueryType CheckTypeAssert(QilTargetType node)
	{
		return node.TargetType;
	}

	public static XmlQueryType CheckIsType(QilTargetType node)
	{
		return XmlQueryTypeFactory.BooleanX;
	}

	public static XmlQueryType CheckIsEmpty()
	{
		return XmlQueryTypeFactory.BooleanX;
	}

	public static XmlQueryType CheckXPathNodeValue(QilUnary node)
	{
		return XmlQueryTypeFactory.StringX;
	}

	public static XmlQueryType CheckXPathFollowing(QilUnary node)
	{
		return XmlQueryTypeFactory.ContentS;
	}

	public static XmlQueryType CheckXPathPreceding(QilUnary node)
	{
		return XmlQueryTypeFactory.ContentS;
	}

	public static XmlQueryType CheckXPathNamespace(QilUnary node)
	{
		return XmlQueryTypeFactory.NamespaceS;
	}

	public static XmlQueryType CheckXsltGenerateId(QilUnary node)
	{
		return XmlQueryTypeFactory.StringX;
	}

	public static XmlQueryType CheckXsltInvokeLateBound(QilInvokeLateBound node)
	{
		return XmlQueryTypeFactory.ItemS;
	}

	public static XmlQueryType CheckXsltInvokeEarlyBound(QilInvokeEarlyBound node)
	{
		return node.XmlType;
	}

	public static XmlQueryType CheckXsltCopy(QilBinary node)
	{
		return XmlQueryTypeFactory.Choice(node.Left.XmlType, node.Right.XmlType);
	}

	public static XmlQueryType CheckXsltCopyOf(QilUnary node)
	{
		if ((node.Child.XmlType.NodeKinds & XmlNodeKindFlags.Document) != 0)
		{
			return XmlQueryTypeFactory.NodeNotRtfS;
		}
		return node.Child.XmlType;
	}

	public static XmlQueryType CheckXsltConvert(QilTargetType node)
	{
		return node.TargetType;
	}

	private static XmlQueryType DistinctType(XmlQueryType type)
	{
		if (type.Cardinality == XmlQueryCardinality.More)
		{
			return XmlQueryTypeFactory.PrimeProduct(type, XmlQueryCardinality.OneOrMore);
		}
		if (type.Cardinality == XmlQueryCardinality.NotOne)
		{
			return XmlQueryTypeFactory.PrimeProduct(type, XmlQueryCardinality.ZeroOrMore);
		}
		return type;
	}

	private static XmlQueryType FindFilterType(QilIterator variable, QilNode body)
	{
		if (body.XmlType.TypeCode == XmlTypeCode.None)
		{
			return XmlQueryTypeFactory.None;
		}
		switch (body.NodeType)
		{
		case QilNodeType.False:
			return XmlQueryTypeFactory.Empty;
		case QilNodeType.IsType:
			if (((QilTargetType)body).Source == variable)
			{
				return XmlQueryTypeFactory.AtMost(((QilTargetType)body).TargetType, variable.Binding.XmlType.Cardinality);
			}
			break;
		case QilNodeType.And:
		{
			XmlQueryType xmlQueryType = FindFilterType(variable, ((QilBinary)body).Left);
			if (xmlQueryType != null)
			{
				return xmlQueryType;
			}
			return FindFilterType(variable, ((QilBinary)body).Right);
		}
		case QilNodeType.Eq:
		{
			QilBinary qilBinary = (QilBinary)body;
			if (qilBinary.Left.NodeType == QilNodeType.PositionOf && ((QilUnary)qilBinary.Left).Child == variable)
			{
				return XmlQueryTypeFactory.AtMost(variable.Binding.XmlType, XmlQueryCardinality.ZeroOrOne);
			}
			break;
		}
		}
		return null;
	}
}
