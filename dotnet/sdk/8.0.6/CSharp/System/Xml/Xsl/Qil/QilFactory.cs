using System.Collections.Generic;

namespace System.Xml.Xsl.Qil;

internal sealed class QilFactory
{
	public QilExpression QilExpression(QilNode root, QilFactory factory)
	{
		QilExpression qilExpression = new QilExpression(QilNodeType.QilExpression, root, factory);
		qilExpression.XmlType = QilTypeChecker.CheckQilExpression(qilExpression);
		return qilExpression;
	}

	public QilList ActualParameterList(IList<QilNode> values)
	{
		QilList qilList = ActualParameterList();
		qilList.Add(values);
		return qilList;
	}

	public QilList FormalParameterList(IList<QilNode> values)
	{
		QilList qilList = FormalParameterList();
		qilList.Add(values);
		return qilList;
	}

	public QilList BranchList(IList<QilNode> values)
	{
		QilList qilList = BranchList();
		qilList.Add(values);
		return qilList;
	}

	public QilList Sequence(IList<QilNode> values)
	{
		QilList qilList = Sequence();
		qilList.Add(values);
		return qilList;
	}

	public QilParameter Parameter(XmlQueryType xmlType)
	{
		return Parameter(null, null, xmlType);
	}

	public QilStrConcat StrConcat(QilNode values)
	{
		return StrConcat(LiteralString(""), values);
	}

	public QilName LiteralQName(string local)
	{
		return LiteralQName(local, string.Empty, string.Empty);
	}

	public QilTargetType TypeAssert(QilNode expr, XmlQueryType xmlType)
	{
		return TypeAssert(expr, (QilNode)LiteralType(xmlType));
	}

	public QilTargetType IsType(QilNode expr, XmlQueryType xmlType)
	{
		return IsType(expr, (QilNode)LiteralType(xmlType));
	}

	public QilTargetType XsltConvert(QilNode expr, XmlQueryType xmlType)
	{
		return XsltConvert(expr, (QilNode)LiteralType(xmlType));
	}

	public QilFunction Function(QilNode arguments, QilNode sideEffects, XmlQueryType xmlType)
	{
		return Function(arguments, Unknown(xmlType), sideEffects, xmlType);
	}

	public QilList FunctionList()
	{
		QilList qilList = new QilList(QilNodeType.FunctionList);
		qilList.XmlType = QilTypeChecker.CheckFunctionList(qilList);
		return qilList;
	}

	public QilList GlobalVariableList()
	{
		QilList qilList = new QilList(QilNodeType.GlobalVariableList);
		qilList.XmlType = QilTypeChecker.CheckGlobalVariableList(qilList);
		return qilList;
	}

	public QilList GlobalParameterList()
	{
		QilList qilList = new QilList(QilNodeType.GlobalParameterList);
		qilList.XmlType = QilTypeChecker.CheckGlobalParameterList(qilList);
		return qilList;
	}

	public QilList ActualParameterList()
	{
		QilList qilList = new QilList(QilNodeType.ActualParameterList);
		qilList.XmlType = QilTypeChecker.CheckActualParameterList(qilList);
		return qilList;
	}

	public QilList FormalParameterList()
	{
		QilList qilList = new QilList(QilNodeType.FormalParameterList);
		qilList.XmlType = QilTypeChecker.CheckFormalParameterList(qilList);
		return qilList;
	}

	public QilList SortKeyList()
	{
		QilList qilList = new QilList(QilNodeType.SortKeyList);
		qilList.XmlType = QilTypeChecker.CheckSortKeyList(qilList);
		return qilList;
	}

	public QilList BranchList()
	{
		QilList qilList = new QilList(QilNodeType.BranchList);
		qilList.XmlType = QilTypeChecker.CheckBranchList(qilList);
		return qilList;
	}

	public QilUnary OptimizeBarrier(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.OptimizeBarrier, child);
		qilUnary.XmlType = QilTypeChecker.CheckOptimizeBarrier(qilUnary);
		return qilUnary;
	}

	public QilNode Unknown(XmlQueryType xmlType)
	{
		QilNode qilNode = new QilNode(QilNodeType.Unknown, xmlType);
		qilNode.XmlType = QilTypeChecker.CheckUnknown(qilNode);
		return qilNode;
	}

	public QilDataSource DataSource(QilNode name, QilNode baseUri)
	{
		QilDataSource qilDataSource = new QilDataSource(QilNodeType.DataSource, name, baseUri);
		qilDataSource.XmlType = QilTypeChecker.CheckDataSource(qilDataSource);
		return qilDataSource;
	}

	public QilUnary Nop(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.Nop, child);
		qilUnary.XmlType = QilTypeChecker.CheckNop(qilUnary);
		return qilUnary;
	}

	public QilUnary Error(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.Error, child);
		qilUnary.XmlType = QilTypeChecker.CheckError(qilUnary);
		return qilUnary;
	}

	public QilUnary Warning(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.Warning, child);
		qilUnary.XmlType = QilTypeChecker.CheckWarning(qilUnary);
		return qilUnary;
	}

	public QilIterator For(QilNode binding)
	{
		QilIterator qilIterator = new QilIterator(QilNodeType.For, binding);
		qilIterator.XmlType = QilTypeChecker.CheckFor(qilIterator);
		return qilIterator;
	}

	public QilIterator Let(QilNode binding)
	{
		QilIterator qilIterator = new QilIterator(QilNodeType.Let, binding);
		qilIterator.XmlType = QilTypeChecker.CheckLet(qilIterator);
		return qilIterator;
	}

	public QilParameter Parameter(QilNode defaultValue, QilNode name, XmlQueryType xmlType)
	{
		QilParameter qilParameter = new QilParameter(QilNodeType.Parameter, defaultValue, name, xmlType);
		qilParameter.XmlType = QilTypeChecker.CheckParameter(qilParameter);
		return qilParameter;
	}

	public QilUnary PositionOf(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.PositionOf, child);
		qilUnary.XmlType = QilTypeChecker.CheckPositionOf();
		return qilUnary;
	}

	public QilNode True()
	{
		QilNode qilNode = new QilNode(QilNodeType.True);
		qilNode.XmlType = QilTypeChecker.CheckTrue();
		return qilNode;
	}

	public QilNode False()
	{
		QilNode qilNode = new QilNode(QilNodeType.False);
		qilNode.XmlType = QilTypeChecker.CheckFalse();
		return qilNode;
	}

	public QilLiteral LiteralString(string value)
	{
		QilLiteral qilLiteral = new QilLiteral(QilNodeType.LiteralString, value);
		qilLiteral.XmlType = QilTypeChecker.CheckLiteralString(qilLiteral);
		return qilLiteral;
	}

	public QilLiteral LiteralInt32(int value)
	{
		QilLiteral qilLiteral = new QilLiteral(QilNodeType.LiteralInt32, value);
		qilLiteral.XmlType = QilTypeChecker.CheckLiteralInt32(qilLiteral);
		return qilLiteral;
	}

	public QilLiteral LiteralInt64(long value)
	{
		QilLiteral qilLiteral = new QilLiteral(QilNodeType.LiteralInt64, value);
		qilLiteral.XmlType = QilTypeChecker.CheckLiteralInt64(qilLiteral);
		return qilLiteral;
	}

	public QilLiteral LiteralDouble(double value)
	{
		QilLiteral qilLiteral = new QilLiteral(QilNodeType.LiteralDouble, value);
		qilLiteral.XmlType = QilTypeChecker.CheckLiteralDouble(qilLiteral);
		return qilLiteral;
	}

	public QilLiteral LiteralDecimal(decimal value)
	{
		QilLiteral qilLiteral = new QilLiteral(QilNodeType.LiteralDecimal, value);
		qilLiteral.XmlType = QilTypeChecker.CheckLiteralDecimal(qilLiteral);
		return qilLiteral;
	}

	public QilName LiteralQName(string localName, string namespaceUri, string prefix)
	{
		QilName qilName = new QilName(QilNodeType.LiteralQName, localName, namespaceUri, prefix);
		qilName.XmlType = QilTypeChecker.CheckLiteralQName(qilName);
		return qilName;
	}

	public QilLiteral LiteralType(XmlQueryType value)
	{
		QilLiteral qilLiteral = new QilLiteral(QilNodeType.LiteralType, value);
		qilLiteral.XmlType = QilTypeChecker.CheckLiteralType(qilLiteral);
		return qilLiteral;
	}

	public QilLiteral LiteralObject(object value)
	{
		QilLiteral qilLiteral = new QilLiteral(QilNodeType.LiteralObject, value);
		qilLiteral.XmlType = QilTypeChecker.CheckLiteralObject(qilLiteral);
		return qilLiteral;
	}

	public QilBinary And(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.And, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckAnd(qilBinary);
		return qilBinary;
	}

	public QilBinary Or(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Or, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckOr(qilBinary);
		return qilBinary;
	}

	public QilUnary Not(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.Not, child);
		qilUnary.XmlType = QilTypeChecker.CheckNot(qilUnary);
		return qilUnary;
	}

	public QilTernary Conditional(QilNode left, QilNode center, QilNode right)
	{
		QilTernary qilTernary = new QilTernary(QilNodeType.Conditional, left, center, right);
		qilTernary.XmlType = QilTypeChecker.CheckConditional(qilTernary);
		return qilTernary;
	}

	public QilChoice Choice(QilNode expression, QilNode branches)
	{
		QilChoice qilChoice = new QilChoice(QilNodeType.Choice, expression, branches);
		qilChoice.XmlType = QilTypeChecker.CheckChoice(qilChoice);
		return qilChoice;
	}

	public QilUnary Length(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.Length, child);
		qilUnary.XmlType = QilTypeChecker.CheckLength();
		return qilUnary;
	}

	public QilList Sequence()
	{
		QilList qilList = new QilList(QilNodeType.Sequence);
		qilList.XmlType = QilTypeChecker.CheckSequence(qilList);
		return qilList;
	}

	public QilBinary Union(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Union, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckUnion(qilBinary);
		return qilBinary;
	}

	public QilBinary Intersection(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Intersection, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckIntersection(qilBinary);
		return qilBinary;
	}

	public QilBinary Difference(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Difference, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckDifference(qilBinary);
		return qilBinary;
	}

	public QilUnary Sum(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.Sum, child);
		qilUnary.XmlType = QilTypeChecker.CheckSum(qilUnary);
		return qilUnary;
	}

	public QilUnary Negate(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.Negate, child);
		qilUnary.XmlType = QilTypeChecker.CheckNegate(qilUnary);
		return qilUnary;
	}

	public QilBinary Add(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Add, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckAdd(qilBinary);
		return qilBinary;
	}

	public QilBinary Subtract(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Subtract, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckSubtract(qilBinary);
		return qilBinary;
	}

	public QilBinary Multiply(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Multiply, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckMultiply(qilBinary);
		return qilBinary;
	}

	public QilBinary Divide(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Divide, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckDivide(qilBinary);
		return qilBinary;
	}

	public QilBinary Modulo(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Modulo, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckModulo(qilBinary);
		return qilBinary;
	}

	public QilUnary StrLength(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.StrLength, child);
		qilUnary.XmlType = QilTypeChecker.CheckStrLength(qilUnary);
		return qilUnary;
	}

	public QilStrConcat StrConcat(QilNode delimiter, QilNode values)
	{
		QilStrConcat qilStrConcat = new QilStrConcat(QilNodeType.StrConcat, delimiter, values);
		qilStrConcat.XmlType = QilTypeChecker.CheckStrConcat(qilStrConcat);
		return qilStrConcat;
	}

	public QilBinary StrParseQName(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.StrParseQName, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckStrParseQName(qilBinary);
		return qilBinary;
	}

	public QilBinary Ne(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Ne, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckNe(qilBinary);
		return qilBinary;
	}

	public QilBinary Eq(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Eq, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckEq(qilBinary);
		return qilBinary;
	}

	public QilBinary Gt(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Gt, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckGt(qilBinary);
		return qilBinary;
	}

	public QilBinary Ge(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Ge, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckGe(qilBinary);
		return qilBinary;
	}

	public QilBinary Lt(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Lt, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckLt(qilBinary);
		return qilBinary;
	}

	public QilBinary Le(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Le, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckLe(qilBinary);
		return qilBinary;
	}

	public QilBinary Is(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Is, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckIs(qilBinary);
		return qilBinary;
	}

	public QilBinary Before(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Before, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckBefore(qilBinary);
		return qilBinary;
	}

	public QilLoop Loop(QilNode variable, QilNode body)
	{
		QilLoop qilLoop = new QilLoop(QilNodeType.Loop, variable, body);
		qilLoop.XmlType = QilTypeChecker.CheckLoop(qilLoop);
		return qilLoop;
	}

	public QilLoop Filter(QilNode variable, QilNode body)
	{
		QilLoop qilLoop = new QilLoop(QilNodeType.Filter, variable, body);
		qilLoop.XmlType = QilTypeChecker.CheckFilter(qilLoop);
		return qilLoop;
	}

	public QilLoop Sort(QilNode variable, QilNode body)
	{
		QilLoop qilLoop = new QilLoop(QilNodeType.Sort, variable, body);
		qilLoop.XmlType = QilTypeChecker.CheckSort(qilLoop);
		return qilLoop;
	}

	public QilSortKey SortKey(QilNode key, QilNode collation)
	{
		QilSortKey qilSortKey = new QilSortKey(QilNodeType.SortKey, key, collation);
		qilSortKey.XmlType = QilTypeChecker.CheckSortKey(qilSortKey);
		return qilSortKey;
	}

	public QilUnary DocOrderDistinct(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.DocOrderDistinct, child);
		qilUnary.XmlType = QilTypeChecker.CheckDocOrderDistinct(qilUnary);
		return qilUnary;
	}

	public QilFunction Function(QilNode arguments, QilNode definition, QilNode sideEffects, XmlQueryType xmlType)
	{
		QilFunction qilFunction = new QilFunction(QilNodeType.Function, arguments, definition, sideEffects, xmlType);
		qilFunction.XmlType = QilTypeChecker.CheckFunction(qilFunction);
		return qilFunction;
	}

	public QilInvoke Invoke(QilNode function, QilNode arguments)
	{
		QilInvoke qilInvoke = new QilInvoke(QilNodeType.Invoke, function, arguments);
		qilInvoke.XmlType = QilTypeChecker.CheckInvoke(qilInvoke);
		return qilInvoke;
	}

	public QilUnary Content(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.Content, child);
		qilUnary.XmlType = QilTypeChecker.CheckContent(qilUnary);
		return qilUnary;
	}

	public QilBinary Attribute(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Attribute, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckAttribute(qilBinary);
		return qilBinary;
	}

	public QilUnary Parent(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.Parent, child);
		qilUnary.XmlType = QilTypeChecker.CheckParent(qilUnary);
		return qilUnary;
	}

	public QilUnary Root(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.Root, child);
		qilUnary.XmlType = QilTypeChecker.CheckRoot(qilUnary);
		return qilUnary;
	}

	public QilNode XmlContext()
	{
		QilNode qilNode = new QilNode(QilNodeType.XmlContext);
		qilNode.XmlType = QilTypeChecker.CheckXmlContext();
		return qilNode;
	}

	public QilUnary Descendant(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.Descendant, child);
		qilUnary.XmlType = QilTypeChecker.CheckDescendant(qilUnary);
		return qilUnary;
	}

	public QilUnary DescendantOrSelf(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.DescendantOrSelf, child);
		qilUnary.XmlType = QilTypeChecker.CheckDescendantOrSelf(qilUnary);
		return qilUnary;
	}

	public QilUnary Ancestor(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.Ancestor, child);
		qilUnary.XmlType = QilTypeChecker.CheckAncestor(qilUnary);
		return qilUnary;
	}

	public QilUnary AncestorOrSelf(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.AncestorOrSelf, child);
		qilUnary.XmlType = QilTypeChecker.CheckAncestorOrSelf(qilUnary);
		return qilUnary;
	}

	public QilUnary Preceding(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.Preceding, child);
		qilUnary.XmlType = QilTypeChecker.CheckPreceding(qilUnary);
		return qilUnary;
	}

	public QilUnary FollowingSibling(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.FollowingSibling, child);
		qilUnary.XmlType = QilTypeChecker.CheckFollowingSibling(qilUnary);
		return qilUnary;
	}

	public QilUnary PrecedingSibling(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.PrecedingSibling, child);
		qilUnary.XmlType = QilTypeChecker.CheckPrecedingSibling(qilUnary);
		return qilUnary;
	}

	public QilBinary NodeRange(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.NodeRange, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckNodeRange(qilBinary);
		return qilBinary;
	}

	public QilBinary Deref(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.Deref, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckDeref(qilBinary);
		return qilBinary;
	}

	public QilBinary ElementCtor(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.ElementCtor, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckElementCtor(qilBinary);
		return qilBinary;
	}

	public QilBinary AttributeCtor(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.AttributeCtor, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckAttributeCtor(qilBinary);
		return qilBinary;
	}

	public QilUnary CommentCtor(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.CommentCtor, child);
		qilUnary.XmlType = QilTypeChecker.CheckCommentCtor(qilUnary);
		return qilUnary;
	}

	public QilBinary PICtor(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.PICtor, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckPICtor(qilBinary);
		return qilBinary;
	}

	public QilUnary TextCtor(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.TextCtor, child);
		qilUnary.XmlType = QilTypeChecker.CheckTextCtor(qilUnary);
		return qilUnary;
	}

	public QilUnary RawTextCtor(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.RawTextCtor, child);
		qilUnary.XmlType = QilTypeChecker.CheckRawTextCtor(qilUnary);
		return qilUnary;
	}

	public QilUnary DocumentCtor(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.DocumentCtor, child);
		qilUnary.XmlType = QilTypeChecker.CheckDocumentCtor(qilUnary);
		return qilUnary;
	}

	public QilBinary NamespaceDecl(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.NamespaceDecl, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckNamespaceDecl(qilBinary);
		return qilBinary;
	}

	public QilBinary RtfCtor(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.RtfCtor, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckRtfCtor(qilBinary);
		return qilBinary;
	}

	public QilUnary NameOf(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.NameOf, child);
		qilUnary.XmlType = QilTypeChecker.CheckNameOf(qilUnary);
		return qilUnary;
	}

	public QilUnary LocalNameOf(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.LocalNameOf, child);
		qilUnary.XmlType = QilTypeChecker.CheckLocalNameOf(qilUnary);
		return qilUnary;
	}

	public QilUnary NamespaceUriOf(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.NamespaceUriOf, child);
		qilUnary.XmlType = QilTypeChecker.CheckNamespaceUriOf(qilUnary);
		return qilUnary;
	}

	public QilUnary PrefixOf(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.PrefixOf, child);
		qilUnary.XmlType = QilTypeChecker.CheckPrefixOf(qilUnary);
		return qilUnary;
	}

	public QilTargetType TypeAssert(QilNode source, QilNode targetType)
	{
		QilTargetType qilTargetType = new QilTargetType(QilNodeType.TypeAssert, source, targetType);
		qilTargetType.XmlType = QilTypeChecker.CheckTypeAssert(qilTargetType);
		return qilTargetType;
	}

	public QilTargetType IsType(QilNode source, QilNode targetType)
	{
		QilTargetType qilTargetType = new QilTargetType(QilNodeType.IsType, source, targetType);
		qilTargetType.XmlType = QilTypeChecker.CheckIsType(qilTargetType);
		return qilTargetType;
	}

	public QilUnary IsEmpty(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.IsEmpty, child);
		qilUnary.XmlType = QilTypeChecker.CheckIsEmpty();
		return qilUnary;
	}

	public QilUnary XPathNodeValue(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.XPathNodeValue, child);
		qilUnary.XmlType = QilTypeChecker.CheckXPathNodeValue(qilUnary);
		return qilUnary;
	}

	public QilUnary XPathFollowing(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.XPathFollowing, child);
		qilUnary.XmlType = QilTypeChecker.CheckXPathFollowing(qilUnary);
		return qilUnary;
	}

	public QilUnary XPathPreceding(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.XPathPreceding, child);
		qilUnary.XmlType = QilTypeChecker.CheckXPathPreceding(qilUnary);
		return qilUnary;
	}

	public QilUnary XPathNamespace(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.XPathNamespace, child);
		qilUnary.XmlType = QilTypeChecker.CheckXPathNamespace(qilUnary);
		return qilUnary;
	}

	public QilUnary XsltGenerateId(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.XsltGenerateId, child);
		qilUnary.XmlType = QilTypeChecker.CheckXsltGenerateId(qilUnary);
		return qilUnary;
	}

	public QilInvokeLateBound XsltInvokeLateBound(QilNode name, QilNode arguments)
	{
		QilInvokeLateBound qilInvokeLateBound = new QilInvokeLateBound(QilNodeType.XsltInvokeLateBound, name, arguments);
		qilInvokeLateBound.XmlType = QilTypeChecker.CheckXsltInvokeLateBound(qilInvokeLateBound);
		return qilInvokeLateBound;
	}

	public QilInvokeEarlyBound XsltInvokeEarlyBound(QilNode name, QilNode clrMethod, QilNode arguments, XmlQueryType xmlType)
	{
		QilInvokeEarlyBound qilInvokeEarlyBound = new QilInvokeEarlyBound(QilNodeType.XsltInvokeEarlyBound, name, clrMethod, arguments, xmlType);
		qilInvokeEarlyBound.XmlType = QilTypeChecker.CheckXsltInvokeEarlyBound(qilInvokeEarlyBound);
		return qilInvokeEarlyBound;
	}

	public QilBinary XsltCopy(QilNode left, QilNode right)
	{
		QilBinary qilBinary = new QilBinary(QilNodeType.XsltCopy, left, right);
		qilBinary.XmlType = QilTypeChecker.CheckXsltCopy(qilBinary);
		return qilBinary;
	}

	public QilUnary XsltCopyOf(QilNode child)
	{
		QilUnary qilUnary = new QilUnary(QilNodeType.XsltCopyOf, child);
		qilUnary.XmlType = QilTypeChecker.CheckXsltCopyOf(qilUnary);
		return qilUnary;
	}

	public QilTargetType XsltConvert(QilNode source, QilNode targetType)
	{
		QilTargetType qilTargetType = new QilTargetType(QilNodeType.XsltConvert, source, targetType);
		qilTargetType.XmlType = QilTypeChecker.CheckXsltConvert(qilTargetType);
		return qilTargetType;
	}
}
