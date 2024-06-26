using System.Collections.Generic;
using System.Reflection;

namespace System.Linq.Expressions;

internal static class Error
{
	internal static ArgumentException ReducibleMustOverrideReduce()
	{
		return new ArgumentException(Strings.ReducibleMustOverrideReduce);
	}

	internal static ArgumentException ArgCntMustBeGreaterThanNameCnt()
	{
		return new ArgumentException(Strings.ArgCntMustBeGreaterThanNameCnt);
	}

	internal static InvalidOperationException InvalidMetaObjectCreated(object p0)
	{
		return new InvalidOperationException(Strings.InvalidMetaObjectCreated(p0));
	}

	internal static AmbiguousMatchException AmbiguousMatchInExpandoObject(object p0)
	{
		return new AmbiguousMatchException(Strings.AmbiguousMatchInExpandoObject(p0));
	}

	internal static ArgumentException SameKeyExistsInExpando(object key)
	{
		return new ArgumentException(Strings.SameKeyExistsInExpando(key), "key");
	}

	internal static KeyNotFoundException KeyDoesNotExistInExpando(object p0)
	{
		return new KeyNotFoundException(Strings.KeyDoesNotExistInExpando(p0));
	}

	internal static InvalidOperationException CollectionModifiedWhileEnumerating()
	{
		return new InvalidOperationException(Strings.CollectionModifiedWhileEnumerating);
	}

	internal static NotSupportedException CollectionReadOnly()
	{
		return new NotSupportedException(Strings.CollectionReadOnly);
	}

	internal static ArgumentException MustReduceToDifferent()
	{
		return new ArgumentException(Strings.MustReduceToDifferent);
	}

	internal static InvalidOperationException BinderNotCompatibleWithCallSite(object p0, object p1, object p2)
	{
		return new InvalidOperationException(Strings.BinderNotCompatibleWithCallSite(p0, p1, p2));
	}

	internal static InvalidOperationException DynamicBindingNeedsRestrictions(object p0, object p1)
	{
		return new InvalidOperationException(Strings.DynamicBindingNeedsRestrictions(p0, p1));
	}

	internal static InvalidCastException DynamicObjectResultNotAssignable(object p0, object p1, object p2, object p3)
	{
		return new InvalidCastException(Strings.DynamicObjectResultNotAssignable(p0, p1, p2, p3));
	}

	internal static InvalidCastException DynamicBinderResultNotAssignable(object p0, object p1, object p2)
	{
		return new InvalidCastException(Strings.DynamicBinderResultNotAssignable(p0, p1, p2));
	}

	internal static InvalidOperationException BindingCannotBeNull()
	{
		return new InvalidOperationException(Strings.BindingCannotBeNull);
	}

	internal static ArgumentException ReducedNotCompatible()
	{
		return new ArgumentException(Strings.ReducedNotCompatible);
	}

	internal static ArgumentException SetterHasNoParams(string paramName)
	{
		return new ArgumentException(Strings.SetterHasNoParams, paramName);
	}

	internal static ArgumentException PropertyCannotHaveRefType(string paramName)
	{
		return new ArgumentException(Strings.PropertyCannotHaveRefType, paramName);
	}

	internal static ArgumentException IndexesOfSetGetMustMatch(string paramName)
	{
		return new ArgumentException(Strings.IndexesOfSetGetMustMatch, paramName);
	}

	internal static InvalidOperationException TypeParameterIsNotDelegate(object p0)
	{
		return new InvalidOperationException(Strings.TypeParameterIsNotDelegate(p0));
	}

	internal static ArgumentException FirstArgumentMustBeCallSite()
	{
		return new ArgumentException(Strings.FirstArgumentMustBeCallSite);
	}

	internal static ArgumentException AccessorsCannotHaveVarArgs(string paramName)
	{
		return new ArgumentException(Strings.AccessorsCannotHaveVarArgs, paramName);
	}

	private static ArgumentException AccessorsCannotHaveByRefArgs(string paramName)
	{
		return new ArgumentException(Strings.AccessorsCannotHaveByRefArgs, paramName);
	}

	internal static ArgumentException AccessorsCannotHaveByRefArgs(string paramName, int index)
	{
		return AccessorsCannotHaveByRefArgs(GetParamName(paramName, index));
	}

	internal static ArgumentException TypeMustBeDerivedFromSystemDelegate()
	{
		return new ArgumentException(Strings.TypeMustBeDerivedFromSystemDelegate);
	}

	internal static InvalidOperationException NoOrInvalidRuleProduced()
	{
		return new InvalidOperationException(Strings.NoOrInvalidRuleProduced);
	}

	internal static ArgumentException BoundsCannotBeLessThanOne(string paramName)
	{
		return new ArgumentException(Strings.BoundsCannotBeLessThanOne, paramName);
	}

	internal static ArgumentException TypeMustNotBeByRef(string paramName)
	{
		return new ArgumentException(Strings.TypeMustNotBeByRef, paramName);
	}

	internal static ArgumentException TypeMustNotBePointer(string paramName)
	{
		return new ArgumentException(Strings.TypeMustNotBePointer, paramName);
	}

	internal static ArgumentException SetterMustBeVoid(string paramName)
	{
		return new ArgumentException(Strings.SetterMustBeVoid, paramName);
	}

	internal static ArgumentException PropertyTypeMustMatchGetter(string paramName)
	{
		return new ArgumentException(Strings.PropertyTypeMustMatchGetter, paramName);
	}

	internal static ArgumentException PropertyTypeMustMatchSetter(string paramName)
	{
		return new ArgumentException(Strings.PropertyTypeMustMatchSetter, paramName);
	}

	internal static ArgumentException BothAccessorsMustBeStatic(string paramName)
	{
		return new ArgumentException(Strings.BothAccessorsMustBeStatic, paramName);
	}

	internal static ArgumentException OnlyStaticFieldsHaveNullInstance(string paramName)
	{
		return new ArgumentException(Strings.OnlyStaticFieldsHaveNullInstance, paramName);
	}

	internal static ArgumentException OnlyStaticPropertiesHaveNullInstance(string paramName)
	{
		return new ArgumentException(Strings.OnlyStaticPropertiesHaveNullInstance, paramName);
	}

	internal static ArgumentException OnlyStaticMethodsHaveNullInstance()
	{
		return new ArgumentException(Strings.OnlyStaticMethodsHaveNullInstance);
	}

	internal static ArgumentException PropertyTypeCannotBeVoid(string paramName)
	{
		return new ArgumentException(Strings.PropertyTypeCannotBeVoid, paramName);
	}

	internal static ArgumentException InvalidUnboxType(string paramName)
	{
		return new ArgumentException(Strings.InvalidUnboxType, paramName);
	}

	internal static ArgumentException ExpressionMustBeWriteable(string paramName)
	{
		return new ArgumentException(Strings.ExpressionMustBeWriteable, paramName);
	}

	internal static ArgumentException ArgumentMustNotHaveValueType(string paramName)
	{
		return new ArgumentException(Strings.ArgumentMustNotHaveValueType, paramName);
	}

	internal static ArgumentException MustBeReducible()
	{
		return new ArgumentException(Strings.MustBeReducible);
	}

	internal static ArgumentException AllTestValuesMustHaveSameType(string paramName)
	{
		return new ArgumentException(Strings.AllTestValuesMustHaveSameType, paramName);
	}

	internal static ArgumentException AllCaseBodiesMustHaveSameType(string paramName)
	{
		return new ArgumentException(Strings.AllCaseBodiesMustHaveSameType, paramName);
	}

	internal static ArgumentException DefaultBodyMustBeSupplied(string paramName)
	{
		return new ArgumentException(Strings.DefaultBodyMustBeSupplied, paramName);
	}

	internal static ArgumentException LabelMustBeVoidOrHaveExpression(string paramName)
	{
		return new ArgumentException(Strings.LabelMustBeVoidOrHaveExpression, paramName);
	}

	internal static ArgumentException LabelTypeMustBeVoid(string paramName)
	{
		return new ArgumentException(Strings.LabelTypeMustBeVoid, paramName);
	}

	internal static ArgumentException QuotedExpressionMustBeLambda(string paramName)
	{
		return new ArgumentException(Strings.QuotedExpressionMustBeLambda, paramName);
	}

	internal static ArgumentException VariableMustNotBeByRef(object p0, object p1, string paramName)
	{
		return new ArgumentException(Strings.VariableMustNotBeByRef(p0, p1), paramName);
	}

	internal static ArgumentException VariableMustNotBeByRef(object p0, object p1, string paramName, int index)
	{
		return VariableMustNotBeByRef(p0, p1, GetParamName(paramName, index));
	}

	private static ArgumentException DuplicateVariable(object p0, string paramName)
	{
		return new ArgumentException(Strings.DuplicateVariable(p0), paramName);
	}

	internal static ArgumentException DuplicateVariable(object p0, string paramName, int index)
	{
		return DuplicateVariable(p0, GetParamName(paramName, index));
	}

	internal static ArgumentException StartEndMustBeOrdered()
	{
		return new ArgumentException(Strings.StartEndMustBeOrdered);
	}

	internal static ArgumentException FaultCannotHaveCatchOrFinally(string paramName)
	{
		return new ArgumentException(Strings.FaultCannotHaveCatchOrFinally, paramName);
	}

	internal static ArgumentException TryMustHaveCatchFinallyOrFault()
	{
		return new ArgumentException(Strings.TryMustHaveCatchFinallyOrFault);
	}

	internal static ArgumentException BodyOfCatchMustHaveSameTypeAsBodyOfTry()
	{
		return new ArgumentException(Strings.BodyOfCatchMustHaveSameTypeAsBodyOfTry);
	}

	internal static InvalidOperationException ExtensionNodeMustOverrideProperty(object p0)
	{
		return new InvalidOperationException(Strings.ExtensionNodeMustOverrideProperty(p0));
	}

	internal static ArgumentException UserDefinedOperatorMustBeStatic(object p0, string paramName)
	{
		return new ArgumentException(Strings.UserDefinedOperatorMustBeStatic(p0), paramName);
	}

	internal static ArgumentException UserDefinedOperatorMustNotBeVoid(object p0, string paramName)
	{
		return new ArgumentException(Strings.UserDefinedOperatorMustNotBeVoid(p0), paramName);
	}

	internal static InvalidOperationException CoercionOperatorNotDefined(object p0, object p1)
	{
		return new InvalidOperationException(Strings.CoercionOperatorNotDefined(p0, p1));
	}

	internal static InvalidOperationException UnaryOperatorNotDefined(object p0, object p1)
	{
		return new InvalidOperationException(Strings.UnaryOperatorNotDefined(p0, p1));
	}

	internal static InvalidOperationException BinaryOperatorNotDefined(object p0, object p1, object p2)
	{
		return new InvalidOperationException(Strings.BinaryOperatorNotDefined(p0, p1, p2));
	}

	internal static InvalidOperationException ReferenceEqualityNotDefined(object p0, object p1)
	{
		return new InvalidOperationException(Strings.ReferenceEqualityNotDefined(p0, p1));
	}

	internal static InvalidOperationException OperandTypesDoNotMatchParameters(object p0, object p1)
	{
		return new InvalidOperationException(Strings.OperandTypesDoNotMatchParameters(p0, p1));
	}

	internal static InvalidOperationException OverloadOperatorTypeDoesNotMatchConversionType(object p0, object p1)
	{
		return new InvalidOperationException(Strings.OverloadOperatorTypeDoesNotMatchConversionType(p0, p1));
	}

	internal static InvalidOperationException ConversionIsNotSupportedForArithmeticTypes()
	{
		return new InvalidOperationException(Strings.ConversionIsNotSupportedForArithmeticTypes);
	}

	internal static ArgumentException ArgumentTypeCannotBeVoid()
	{
		return new ArgumentException(Strings.ArgumentTypeCannotBeVoid);
	}

	internal static ArgumentException ArgumentMustBeArray(string paramName)
	{
		return new ArgumentException(Strings.ArgumentMustBeArray, paramName);
	}

	internal static ArgumentException ArgumentMustBeBoolean(string paramName)
	{
		return new ArgumentException(Strings.ArgumentMustBeBoolean, paramName);
	}

	internal static ArgumentException EqualityMustReturnBoolean(object p0, string paramName)
	{
		return new ArgumentException(Strings.EqualityMustReturnBoolean(p0), paramName);
	}

	internal static ArgumentException ArgumentMustBeFieldInfoOrPropertyInfo(string paramName)
	{
		return new ArgumentException(Strings.ArgumentMustBeFieldInfoOrPropertyInfo, paramName);
	}

	private static ArgumentException ArgumentMustBeFieldInfoOrPropertyInfoOrMethod(string paramName)
	{
		return new ArgumentException(Strings.ArgumentMustBeFieldInfoOrPropertyInfoOrMethod, paramName);
	}

	internal static ArgumentException ArgumentMustBeFieldInfoOrPropertyInfoOrMethod(string paramName, int index)
	{
		return ArgumentMustBeFieldInfoOrPropertyInfoOrMethod(GetParamName(paramName, index));
	}

	private static ArgumentException ArgumentMustBeInstanceMember(string paramName)
	{
		return new ArgumentException(Strings.ArgumentMustBeInstanceMember, paramName);
	}

	internal static ArgumentException ArgumentMustBeInstanceMember(string paramName, int index)
	{
		return ArgumentMustBeInstanceMember(GetParamName(paramName, index));
	}

	private static ArgumentException ArgumentMustBeInteger(string paramName)
	{
		return new ArgumentException(Strings.ArgumentMustBeInteger, paramName);
	}

	internal static ArgumentException ArgumentMustBeInteger(string paramName, int index)
	{
		return ArgumentMustBeInteger(GetParamName(paramName, index));
	}

	internal static ArgumentException ArgumentMustBeArrayIndexType(string paramName)
	{
		return new ArgumentException(Strings.ArgumentMustBeArrayIndexType, paramName);
	}

	internal static ArgumentException ArgumentMustBeArrayIndexType(string paramName, int index)
	{
		return ArgumentMustBeArrayIndexType(GetParamName(paramName, index));
	}

	internal static ArgumentException ArgumentMustBeSingleDimensionalArrayType(string paramName)
	{
		return new ArgumentException(Strings.ArgumentMustBeSingleDimensionalArrayType, paramName);
	}

	internal static ArgumentException ArgumentTypesMustMatch()
	{
		return new ArgumentException(Strings.ArgumentTypesMustMatch);
	}

	internal static ArgumentException ArgumentTypesMustMatch(string paramName)
	{
		return new ArgumentException(Strings.ArgumentTypesMustMatch, paramName);
	}

	internal static InvalidOperationException CannotAutoInitializeValueTypeElementThroughProperty(object p0)
	{
		return new InvalidOperationException(Strings.CannotAutoInitializeValueTypeElementThroughProperty(p0));
	}

	internal static InvalidOperationException CannotAutoInitializeValueTypeMemberThroughProperty(object p0)
	{
		return new InvalidOperationException(Strings.CannotAutoInitializeValueTypeMemberThroughProperty(p0));
	}

	internal static ArgumentException IncorrectTypeForTypeAs(object p0, string paramName)
	{
		return new ArgumentException(Strings.IncorrectTypeForTypeAs(p0), paramName);
	}

	internal static InvalidOperationException CoalesceUsedOnNonNullType()
	{
		return new InvalidOperationException(Strings.CoalesceUsedOnNonNullType);
	}

	internal static InvalidOperationException ExpressionTypeCannotInitializeArrayType(object p0, object p1)
	{
		return new InvalidOperationException(Strings.ExpressionTypeCannotInitializeArrayType(p0, p1));
	}

	private static ArgumentException ArgumentTypeDoesNotMatchMember(object p0, object p1, string paramName)
	{
		return new ArgumentException(Strings.ArgumentTypeDoesNotMatchMember(p0, p1), paramName);
	}

	internal static ArgumentException ArgumentTypeDoesNotMatchMember(object p0, object p1, string paramName, int index)
	{
		return ArgumentTypeDoesNotMatchMember(p0, p1, GetParamName(paramName, index));
	}

	private static ArgumentException ArgumentMemberNotDeclOnType(object p0, object p1, string paramName)
	{
		return new ArgumentException(Strings.ArgumentMemberNotDeclOnType(p0, p1), paramName);
	}

	internal static ArgumentException ArgumentMemberNotDeclOnType(object p0, object p1, string paramName, int index)
	{
		return ArgumentMemberNotDeclOnType(p0, p1, GetParamName(paramName, index));
	}

	internal static ArgumentException ExpressionTypeDoesNotMatchReturn(object p0, object p1)
	{
		return new ArgumentException(Strings.ExpressionTypeDoesNotMatchReturn(p0, p1));
	}

	internal static ArgumentException ExpressionTypeDoesNotMatchAssignment(object p0, object p1)
	{
		return new ArgumentException(Strings.ExpressionTypeDoesNotMatchAssignment(p0, p1));
	}

	internal static ArgumentException ExpressionTypeDoesNotMatchLabel(object p0, object p1)
	{
		return new ArgumentException(Strings.ExpressionTypeDoesNotMatchLabel(p0, p1));
	}

	internal static ArgumentException ExpressionTypeNotInvocable(object p0, string paramName)
	{
		return new ArgumentException(Strings.ExpressionTypeNotInvocable(p0), paramName);
	}

	internal static ArgumentException FieldNotDefinedForType(object p0, object p1)
	{
		return new ArgumentException(Strings.FieldNotDefinedForType(p0, p1));
	}

	internal static ArgumentException InstanceFieldNotDefinedForType(object p0, object p1)
	{
		return new ArgumentException(Strings.InstanceFieldNotDefinedForType(p0, p1));
	}

	internal static ArgumentException FieldInfoNotDefinedForType(object p0, object p1, object p2)
	{
		return new ArgumentException(Strings.FieldInfoNotDefinedForType(p0, p1, p2));
	}

	internal static ArgumentException IncorrectNumberOfIndexes()
	{
		return new ArgumentException(Strings.IncorrectNumberOfIndexes);
	}

	internal static ArgumentException IncorrectNumberOfLambdaDeclarationParameters()
	{
		return new ArgumentException(Strings.IncorrectNumberOfLambdaDeclarationParameters);
	}

	internal static ArgumentException IncorrectNumberOfMembersForGivenConstructor()
	{
		return new ArgumentException(Strings.IncorrectNumberOfMembersForGivenConstructor);
	}

	internal static ArgumentException IncorrectNumberOfArgumentsForMembers()
	{
		return new ArgumentException(Strings.IncorrectNumberOfArgumentsForMembers);
	}

	internal static ArgumentException LambdaTypeMustBeDerivedFromSystemDelegate(string paramName)
	{
		return new ArgumentException(Strings.LambdaTypeMustBeDerivedFromSystemDelegate, paramName);
	}

	internal static ArgumentException MemberNotFieldOrProperty(object p0, string paramName)
	{
		return new ArgumentException(Strings.MemberNotFieldOrProperty(p0), paramName);
	}

	internal static ArgumentException MethodContainsGenericParameters(object p0, string paramName)
	{
		return new ArgumentException(Strings.MethodContainsGenericParameters(p0), paramName);
	}

	internal static ArgumentException MethodIsGeneric(object p0, string paramName)
	{
		return new ArgumentException(Strings.MethodIsGeneric(p0), paramName);
	}

	private static ArgumentException MethodNotPropertyAccessor(object p0, object p1, string paramName)
	{
		return new ArgumentException(Strings.MethodNotPropertyAccessor(p0, p1), paramName);
	}

	internal static ArgumentException MethodNotPropertyAccessor(object p0, object p1, string paramName, int index)
	{
		return MethodNotPropertyAccessor(p0, p1, GetParamName(paramName, index));
	}

	internal static ArgumentException PropertyDoesNotHaveGetter(object p0, string paramName)
	{
		return new ArgumentException(Strings.PropertyDoesNotHaveGetter(p0), paramName);
	}

	internal static ArgumentException PropertyDoesNotHaveGetter(object p0, string paramName, int index)
	{
		return PropertyDoesNotHaveGetter(p0, GetParamName(paramName, index));
	}

	internal static ArgumentException PropertyDoesNotHaveSetter(object p0, string paramName)
	{
		return new ArgumentException(Strings.PropertyDoesNotHaveSetter(p0), paramName);
	}

	internal static ArgumentException PropertyDoesNotHaveAccessor(object p0, string paramName)
	{
		return new ArgumentException(Strings.PropertyDoesNotHaveAccessor(p0), paramName);
	}

	internal static ArgumentException NotAMemberOfType(object p0, object p1, string paramName)
	{
		return new ArgumentException(Strings.NotAMemberOfType(p0, p1), paramName);
	}

	internal static ArgumentException NotAMemberOfType(object p0, object p1, string paramName, int index)
	{
		return NotAMemberOfType(p0, p1, GetParamName(paramName, index));
	}

	internal static ArgumentException NotAMemberOfAnyType(object p0, string paramName)
	{
		return new ArgumentException(Strings.NotAMemberOfAnyType(p0), paramName);
	}

	internal static ArgumentException ParameterExpressionNotValidAsDelegate(object p0, object p1)
	{
		return new ArgumentException(Strings.ParameterExpressionNotValidAsDelegate(p0, p1));
	}

	internal static ArgumentException PropertyNotDefinedForType(object p0, object p1, string paramName)
	{
		return new ArgumentException(Strings.PropertyNotDefinedForType(p0, p1), paramName);
	}

	internal static ArgumentException InstancePropertyNotDefinedForType(object p0, object p1, string paramName)
	{
		return new ArgumentException(Strings.InstancePropertyNotDefinedForType(p0, p1), paramName);
	}

	internal static ArgumentException InstancePropertyWithoutParameterNotDefinedForType(object p0, object p1)
	{
		return new ArgumentException(Strings.InstancePropertyWithoutParameterNotDefinedForType(p0, p1));
	}

	internal static ArgumentException InstancePropertyWithSpecifiedParametersNotDefinedForType(object p0, object p1, object p2, string paramName)
	{
		return new ArgumentException(Strings.InstancePropertyWithSpecifiedParametersNotDefinedForType(p0, p1, p2), paramName);
	}

	internal static ArgumentException InstanceAndMethodTypeMismatch(object p0, object p1, object p2)
	{
		return new ArgumentException(Strings.InstanceAndMethodTypeMismatch(p0, p1, p2));
	}

	internal static ArgumentException TypeMissingDefaultConstructor(object p0, string paramName)
	{
		return new ArgumentException(Strings.TypeMissingDefaultConstructor(p0), paramName);
	}

	internal static ArgumentException ElementInitializerMethodNotAdd(string paramName)
	{
		return new ArgumentException(Strings.ElementInitializerMethodNotAdd, paramName);
	}

	internal static ArgumentException ElementInitializerMethodNoRefOutParam(object p0, object p1, string paramName)
	{
		return new ArgumentException(Strings.ElementInitializerMethodNoRefOutParam(p0, p1), paramName);
	}

	internal static ArgumentException ElementInitializerMethodWithZeroArgs(string paramName)
	{
		return new ArgumentException(Strings.ElementInitializerMethodWithZeroArgs, paramName);
	}

	internal static ArgumentException ElementInitializerMethodStatic(string paramName)
	{
		return new ArgumentException(Strings.ElementInitializerMethodStatic, paramName);
	}

	internal static ArgumentException TypeNotIEnumerable(object p0, string paramName)
	{
		return new ArgumentException(Strings.TypeNotIEnumerable(p0), paramName);
	}

	internal static ArgumentException UnhandledBinary(object p0, string paramName)
	{
		return new ArgumentException(Strings.UnhandledBinary(p0), paramName);
	}

	internal static ArgumentException UnhandledBinding()
	{
		return new ArgumentException(Strings.UnhandledBinding);
	}

	internal static ArgumentException UnhandledBindingType(object p0)
	{
		return new ArgumentException(Strings.UnhandledBindingType(p0));
	}

	internal static ArgumentException UnhandledUnary(object p0, string paramName)
	{
		return new ArgumentException(Strings.UnhandledUnary(p0), paramName);
	}

	internal static ArgumentException UnknownBindingType(int index)
	{
		return new ArgumentException(Strings.UnknownBindingType, $"bindings[{index}]");
	}

	internal static ArgumentException UserDefinedOpMustHaveConsistentTypes(object p0, object p1)
	{
		return new ArgumentException(Strings.UserDefinedOpMustHaveConsistentTypes(p0, p1));
	}

	internal static ArgumentException UserDefinedOpMustHaveValidReturnType(object p0, object p1)
	{
		return new ArgumentException(Strings.UserDefinedOpMustHaveValidReturnType(p0, p1));
	}

	internal static ArgumentException LogicalOperatorMustHaveBooleanOperators(object p0, object p1)
	{
		return new ArgumentException(Strings.LogicalOperatorMustHaveBooleanOperators(p0, p1));
	}

	internal static InvalidOperationException MethodWithArgsDoesNotExistOnType(object p0, object p1)
	{
		return new InvalidOperationException(Strings.MethodWithArgsDoesNotExistOnType(p0, p1));
	}

	internal static InvalidOperationException GenericMethodWithArgsDoesNotExistOnType(object p0, object p1)
	{
		return new InvalidOperationException(Strings.GenericMethodWithArgsDoesNotExistOnType(p0, p1));
	}

	internal static InvalidOperationException MethodWithMoreThanOneMatch(object p0, object p1)
	{
		return new InvalidOperationException(Strings.MethodWithMoreThanOneMatch(p0, p1));
	}

	internal static InvalidOperationException PropertyWithMoreThanOneMatch(object p0, object p1)
	{
		return new InvalidOperationException(Strings.PropertyWithMoreThanOneMatch(p0, p1));
	}

	internal static ArgumentException IncorrectNumberOfTypeArgsForFunc(string paramName)
	{
		return new ArgumentException(Strings.IncorrectNumberOfTypeArgsForFunc, paramName);
	}

	internal static ArgumentException IncorrectNumberOfTypeArgsForAction(string paramName)
	{
		return new ArgumentException(Strings.IncorrectNumberOfTypeArgsForAction, paramName);
	}

	internal static ArgumentException ArgumentCannotBeOfTypeVoid(string paramName)
	{
		return new ArgumentException(Strings.ArgumentCannotBeOfTypeVoid, paramName);
	}

	internal static InvalidOperationException LabelTargetAlreadyDefined(object p0)
	{
		return new InvalidOperationException(Strings.LabelTargetAlreadyDefined(p0));
	}

	internal static InvalidOperationException LabelTargetUndefined(object p0)
	{
		return new InvalidOperationException(Strings.LabelTargetUndefined(p0));
	}

	internal static InvalidOperationException ControlCannotLeaveFinally()
	{
		return new InvalidOperationException(Strings.ControlCannotLeaveFinally);
	}

	internal static InvalidOperationException ControlCannotLeaveFilterTest()
	{
		return new InvalidOperationException(Strings.ControlCannotLeaveFilterTest);
	}

	internal static InvalidOperationException AmbiguousJump(object p0)
	{
		return new InvalidOperationException(Strings.AmbiguousJump(p0));
	}

	internal static InvalidOperationException ControlCannotEnterTry()
	{
		return new InvalidOperationException(Strings.ControlCannotEnterTry);
	}

	internal static InvalidOperationException ControlCannotEnterExpression()
	{
		return new InvalidOperationException(Strings.ControlCannotEnterExpression);
	}

	internal static InvalidOperationException NonLocalJumpWithValue(object p0)
	{
		return new InvalidOperationException(Strings.NonLocalJumpWithValue(p0));
	}

	internal static InvalidOperationException InvalidLvalue(ExpressionType p0)
	{
		return new InvalidOperationException(Strings.InvalidLvalue(p0));
	}

	internal static InvalidOperationException UndefinedVariable(object p0, object p1, object p2)
	{
		return new InvalidOperationException(Strings.UndefinedVariable(p0, p1, p2));
	}

	internal static InvalidOperationException CannotCloseOverByRef(object p0, object p1)
	{
		return new InvalidOperationException(Strings.CannotCloseOverByRef(p0, p1));
	}

	internal static InvalidOperationException UnexpectedVarArgsCall(object p0)
	{
		return new InvalidOperationException(Strings.UnexpectedVarArgsCall(p0));
	}

	internal static InvalidOperationException RethrowRequiresCatch()
	{
		return new InvalidOperationException(Strings.RethrowRequiresCatch);
	}

	internal static InvalidOperationException TryNotAllowedInFilter()
	{
		return new InvalidOperationException(Strings.TryNotAllowedInFilter);
	}

	internal static InvalidOperationException MustRewriteToSameNode(object p0, object p1, object p2)
	{
		return new InvalidOperationException(Strings.MustRewriteToSameNode(p0, p1, p2));
	}

	internal static InvalidOperationException MustRewriteChildToSameType(object p0, object p1, object p2)
	{
		return new InvalidOperationException(Strings.MustRewriteChildToSameType(p0, p1, p2));
	}

	internal static InvalidOperationException MustRewriteWithoutMethod(object p0, object p1)
	{
		return new InvalidOperationException(Strings.MustRewriteWithoutMethod(p0, p1));
	}

	internal static NotSupportedException TryNotSupportedForMethodsWithRefArgs(object p0)
	{
		return new NotSupportedException(Strings.TryNotSupportedForMethodsWithRefArgs(p0));
	}

	internal static NotSupportedException TryNotSupportedForValueTypeInstances(object p0)
	{
		return new NotSupportedException(Strings.TryNotSupportedForValueTypeInstances(p0));
	}

	internal static ArgumentException TestValueTypeDoesNotMatchComparisonMethodParameter(object p0, object p1)
	{
		return new ArgumentException(Strings.TestValueTypeDoesNotMatchComparisonMethodParameter(p0, p1));
	}

	internal static ArgumentException SwitchValueTypeDoesNotMatchComparisonMethodParameter(object p0, object p1)
	{
		return new ArgumentException(Strings.SwitchValueTypeDoesNotMatchComparisonMethodParameter(p0, p1));
	}

	internal static ArgumentOutOfRangeException ArgumentOutOfRange(string paramName)
	{
		return new ArgumentOutOfRangeException(paramName);
	}

	internal static NotSupportedException NotSupported()
	{
		return new NotSupportedException();
	}

	internal static ArgumentException NonStaticConstructorRequired(string paramName)
	{
		return new ArgumentException(Strings.NonStaticConstructorRequired, paramName);
	}

	internal static InvalidOperationException NonAbstractConstructorRequired()
	{
		return new InvalidOperationException(Strings.NonAbstractConstructorRequired);
	}

	internal static InvalidProgramException InvalidProgram()
	{
		return new InvalidProgramException();
	}

	internal static InvalidOperationException EnumerationIsDone()
	{
		return new InvalidOperationException(Strings.EnumerationIsDone);
	}

	private static ArgumentException TypeContainsGenericParameters(object p0, string paramName)
	{
		return new ArgumentException(Strings.TypeContainsGenericParameters(p0), paramName);
	}

	internal static ArgumentException TypeContainsGenericParameters(object p0, string paramName, int index)
	{
		return TypeContainsGenericParameters(p0, GetParamName(paramName, index));
	}

	internal static ArgumentException TypeIsGeneric(object p0, string paramName)
	{
		return new ArgumentException(Strings.TypeIsGeneric(p0), paramName);
	}

	internal static ArgumentException TypeIsGeneric(object p0, string paramName, int index)
	{
		return TypeIsGeneric(p0, GetParamName(paramName, index));
	}

	internal static ArgumentException IncorrectNumberOfConstructorArguments()
	{
		return new ArgumentException(Strings.IncorrectNumberOfConstructorArguments);
	}

	internal static ArgumentException ExpressionTypeDoesNotMatchMethodParameter(object p0, object p1, object p2, string paramName)
	{
		return new ArgumentException(Strings.ExpressionTypeDoesNotMatchMethodParameter(p0, p1, p2), paramName);
	}

	internal static ArgumentException ExpressionTypeDoesNotMatchMethodParameter(object p0, object p1, object p2, string paramName, int index)
	{
		return ExpressionTypeDoesNotMatchMethodParameter(p0, p1, p2, GetParamName(paramName, index));
	}

	internal static ArgumentException ExpressionTypeDoesNotMatchParameter(object p0, object p1, string paramName)
	{
		return new ArgumentException(Strings.ExpressionTypeDoesNotMatchParameter(p0, p1), paramName);
	}

	internal static ArgumentException ExpressionTypeDoesNotMatchParameter(object p0, object p1, string paramName, int index)
	{
		return ExpressionTypeDoesNotMatchParameter(p0, p1, GetParamName(paramName, index));
	}

	internal static InvalidOperationException IncorrectNumberOfLambdaArguments()
	{
		return new InvalidOperationException(Strings.IncorrectNumberOfLambdaArguments);
	}

	internal static ArgumentException IncorrectNumberOfMethodCallArguments(object p0, string paramName)
	{
		return new ArgumentException(Strings.IncorrectNumberOfMethodCallArguments(p0), paramName);
	}

	internal static ArgumentException ExpressionTypeDoesNotMatchConstructorParameter(object p0, object p1, string paramName)
	{
		return new ArgumentException(Strings.ExpressionTypeDoesNotMatchConstructorParameter(p0, p1), paramName);
	}

	internal static ArgumentException ExpressionTypeDoesNotMatchConstructorParameter(object p0, object p1, string paramName, int index)
	{
		return ExpressionTypeDoesNotMatchConstructorParameter(p0, p1, GetParamName(paramName, index));
	}

	internal static ArgumentException ExpressionMustBeReadable(string paramName)
	{
		return new ArgumentException(Strings.ExpressionMustBeReadable, paramName);
	}

	internal static ArgumentException ExpressionMustBeReadable(string paramName, int index)
	{
		return ExpressionMustBeReadable(GetParamName(paramName, index));
	}

	internal static ArgumentException InvalidArgumentValue(string paramName)
	{
		return new ArgumentException(Strings.InvalidArgumentValue_ParamName, paramName);
	}

	internal static ArgumentException NonEmptyCollectionRequired(string paramName)
	{
		return new ArgumentException(Strings.NonEmptyCollectionRequired, paramName);
	}

	internal static ArgumentException InvalidNullValue(Type type, string paramName)
	{
		return new ArgumentException(Strings.InvalidNullValue(type), paramName);
	}

	internal static ArgumentException InvalidTypeException(object value, Type type, string paramName)
	{
		return new ArgumentException(Strings.InvalidObjectType(((object)value?.GetType()) ?? ((object)"null"), type), paramName);
	}

	private static string GetParamName(string paramName, int index)
	{
		if (index >= 0)
		{
			return $"{paramName}[{index}]";
		}
		return paramName;
	}
}
