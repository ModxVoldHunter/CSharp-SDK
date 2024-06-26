using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Serialization;

namespace System;

[StackTraceHidden]
internal static class ThrowHelper
{
	[DoesNotReturn]
	internal static void ThrowAccessViolationException()
	{
		throw new AccessViolationException();
	}

	[DoesNotReturn]
	internal static void ThrowArrayTypeMismatchException()
	{
		throw new ArrayTypeMismatchException();
	}

	[DoesNotReturn]
	internal static void ThrowInvalidTypeWithPointersNotSupported(Type targetType)
	{
		throw new ArgumentException(SR.Format(SR.Argument_InvalidTypeWithPointersNotSupported, targetType));
	}

	[DoesNotReturn]
	internal static void ThrowIndexOutOfRangeException()
	{
		throw new IndexOutOfRangeException();
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRangeException()
	{
		throw new ArgumentOutOfRangeException();
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException_DestinationTooShort()
	{
		throw new ArgumentException(SR.Argument_DestinationTooShort, "destination");
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException_OverlapAlignmentMismatch()
	{
		throw new ArgumentException(SR.Argument_OverlapAlignmentMismatch);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException_ArgumentNull_TypedRefType()
	{
		throw new ArgumentNullException("value", SR.ArgumentNull_TypedRefType);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException_CannotExtractScalar(ExceptionArgument argument)
	{
		throw GetArgumentException(ExceptionResource.Argument_CannotExtractScalar, argument);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException_TupleIncorrectType(object obj)
	{
		throw new ArgumentException(SR.Format(SR.ArgumentException_ValueTupleIncorrectType, obj.GetType()), "other");
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRange_IndexMustBeLessException()
	{
		throw GetArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_IndexMustBeLess);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException()
	{
		throw GetArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_IndexMustBeLessOrEqual);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException_BadComparer(object comparer)
	{
		throw new ArgumentException(SR.Format(SR.Arg_BogusIComparer, comparer));
	}

	[DoesNotReturn]
	internal static void ThrowIndexArgumentOutOfRange_NeedNonNegNumException()
	{
		throw GetArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
	}

	[DoesNotReturn]
	internal static void ThrowValueArgumentOutOfRange_NeedNonNegNumException()
	{
		throw GetArgumentOutOfRangeException(ExceptionArgument.value, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
	}

	[DoesNotReturn]
	internal static void ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum()
	{
		throw GetArgumentOutOfRangeException(ExceptionArgument.length, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
	}

	[DoesNotReturn]
	internal static void ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLessOrEqual()
	{
		throw GetArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLessOrEqual);
	}

	[DoesNotReturn]
	internal static void ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess()
	{
		throw GetArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLess);
	}

	[DoesNotReturn]
	internal static void ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count()
	{
		throw GetArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRange_Year()
	{
		throw GetArgumentOutOfRangeException(ExceptionArgument.year, ExceptionResource.ArgumentOutOfRange_Year);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRange_Month(int month)
	{
		throw new ArgumentOutOfRangeException("month", month, SR.ArgumentOutOfRange_Month);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRange_DayNumber(int dayNumber)
	{
		throw new ArgumentOutOfRangeException("dayNumber", dayNumber, SR.ArgumentOutOfRange_DayNumber);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRange_BadYearMonthDay()
	{
		throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRange_BadHourMinuteSecond()
	{
		throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadHourMinuteSecond);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRange_TimeSpanTooLong()
	{
		throw new ArgumentOutOfRangeException(null, SR.Overflow_TimeSpanTooLong);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRange_Range<T>(string parameterName, T value, T minInclusive, T maxInclusive)
	{
		throw new ArgumentOutOfRangeException(parameterName, value, SR.Format(SR.ArgumentOutOfRange_Range, minInclusive, maxInclusive));
	}

	[DoesNotReturn]
	internal static void ThrowOverflowException()
	{
		throw new OverflowException();
	}

	[DoesNotReturn]
	internal static void ThrowOverflowException_TimeSpanTooLong()
	{
		throw new OverflowException(SR.Overflow_TimeSpanTooLong);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException_Arg_CannotBeNaN()
	{
		throw new ArgumentException(SR.Arg_CannotBeNaN);
	}

	[DoesNotReturn]
	internal static void ThrowWrongKeyTypeArgumentException<T>(T key, Type targetType)
	{
		throw GetWrongKeyTypeArgumentException(key, targetType);
	}

	[DoesNotReturn]
	internal static void ThrowWrongValueTypeArgumentException<T>(T value, Type targetType)
	{
		throw GetWrongValueTypeArgumentException(value, targetType);
	}

	private static ArgumentException GetAddingDuplicateWithKeyArgumentException(object key)
	{
		return new ArgumentException(SR.Format(SR.Argument_AddingDuplicateWithKey, key));
	}

	[DoesNotReturn]
	internal static void ThrowAddingDuplicateWithKeyArgumentException<T>(T key)
	{
		throw GetAddingDuplicateWithKeyArgumentException(key);
	}

	[DoesNotReturn]
	internal static void ThrowKeyNotFoundException<T>(T key)
	{
		throw GetKeyNotFoundException(key);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException(ExceptionResource resource)
	{
		throw GetArgumentException(resource);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException(ExceptionResource resource, ExceptionArgument argument)
	{
		throw GetArgumentException(resource, argument);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException_HandleNotSync(string paramName)
	{
		throw new ArgumentException(SR.Arg_HandleNotSync, paramName);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException_HandleNotAsync(string paramName)
	{
		throw new ArgumentException(SR.Arg_HandleNotAsync, paramName);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentNullException(ExceptionArgument argument)
	{
		throw new ArgumentNullException(GetArgumentName(argument));
	}

	[DoesNotReturn]
	internal static void ThrowArgumentNullException(ExceptionArgument argument, ExceptionResource resource)
	{
		throw new ArgumentNullException(GetArgumentName(argument), GetResourceString(resource));
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument)
	{
		throw new ArgumentOutOfRangeException(GetArgumentName(argument));
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument, ExceptionResource resource)
	{
		throw GetArgumentOutOfRangeException(argument, resource);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument, int paramNumber, ExceptionResource resource)
	{
		throw GetArgumentOutOfRangeException(argument, paramNumber, resource);
	}

	[DoesNotReturn]
	internal static void ThrowEndOfFileException()
	{
		throw CreateEndOfFileException();
	}

	internal static Exception CreateEndOfFileException()
	{
		return new EndOfStreamException(SR.IO_EOF_ReadBeyondEOF);
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException()
	{
		throw new InvalidOperationException();
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException(ExceptionResource resource)
	{
		throw GetInvalidOperationException(resource);
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException(ExceptionResource resource, Exception e)
	{
		throw new InvalidOperationException(GetResourceString(resource), e);
	}

	[DoesNotReturn]
	internal static void ThrowSerializationException(ExceptionResource resource)
	{
		throw new SerializationException(GetResourceString(resource));
	}

	[DoesNotReturn]
	internal static void ThrowRankException(ExceptionResource resource)
	{
		throw new RankException(GetResourceString(resource));
	}

	[DoesNotReturn]
	internal static void ThrowNotSupportedException(ExceptionResource resource)
	{
		throw new NotSupportedException(GetResourceString(resource));
	}

	[DoesNotReturn]
	internal static void ThrowNotSupportedException_UnseekableStream()
	{
		throw new NotSupportedException(SR.NotSupported_UnseekableStream);
	}

	[DoesNotReturn]
	internal static void ThrowNotSupportedException_UnreadableStream()
	{
		throw new NotSupportedException(SR.NotSupported_UnreadableStream);
	}

	[DoesNotReturn]
	internal static void ThrowNotSupportedException_UnwritableStream()
	{
		throw new NotSupportedException(SR.NotSupported_UnwritableStream);
	}

	[DoesNotReturn]
	internal static void ThrowObjectDisposedException(object instance)
	{
		throw new ObjectDisposedException(instance?.GetType().FullName);
	}

	[DoesNotReturn]
	internal static void ThrowObjectDisposedException(Type type)
	{
		throw new ObjectDisposedException(type?.FullName);
	}

	[DoesNotReturn]
	internal static void ThrowObjectDisposedException_StreamClosed(string objectName)
	{
		throw new ObjectDisposedException(objectName, SR.ObjectDisposed_StreamClosed);
	}

	[DoesNotReturn]
	internal static void ThrowObjectDisposedException_FileClosed()
	{
		throw new ObjectDisposedException(null, SR.ObjectDisposed_FileClosed);
	}

	[DoesNotReturn]
	internal static void ThrowObjectDisposedException(ExceptionResource resource)
	{
		throw new ObjectDisposedException(null, GetResourceString(resource));
	}

	[DoesNotReturn]
	internal static void ThrowNotSupportedException()
	{
		throw new NotSupportedException();
	}

	[DoesNotReturn]
	internal static void ThrowAggregateException(List<Exception> exceptions)
	{
		throw new AggregateException(exceptions);
	}

	[DoesNotReturn]
	internal static void ThrowOutOfMemoryException()
	{
		throw new OutOfMemoryException();
	}

	[DoesNotReturn]
	internal static void ThrowOutOfMemoryException_StringTooLong()
	{
		throw new OutOfMemoryException(SR.OutOfMemory_StringTooLong);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException_Argument_IncompatibleArrayType()
	{
		throw new ArgumentException(SR.Argument_IncompatibleArrayType);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException_InvalidHandle(string paramName)
	{
		throw new ArgumentException(SR.Arg_InvalidHandle, paramName);
	}

	[DoesNotReturn]
	internal static void ThrowUnexpectedStateForKnownCallback(object state)
	{
		throw new ArgumentOutOfRangeException("state", state, SR.Argument_UnexpectedStateForKnownCallback);
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException_InvalidOperation_EnumNotStarted()
	{
		throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException_InvalidOperation_EnumEnded()
	{
		throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException_EnumCurrent(int index)
	{
		throw GetInvalidOperationException_EnumCurrent(index);
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion()
	{
		throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen()
	{
		throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException_InvalidOperation_NoValue()
	{
		throw new InvalidOperationException(SR.InvalidOperation_NoValue);
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException_ConcurrentOperationsNotSupported()
	{
		throw new InvalidOperationException(SR.InvalidOperation_ConcurrentOperationsNotSupported);
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException_HandleIsNotInitialized()
	{
		throw new InvalidOperationException(SR.InvalidOperation_HandleIsNotInitialized);
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException_HandleIsNotPinned()
	{
		throw new InvalidOperationException(SR.InvalidOperation_HandleIsNotPinned);
	}

	[DoesNotReturn]
	internal static void ThrowArraySegmentCtorValidationFailedExceptions(Array array, int offset, int count)
	{
		throw GetArraySegmentCtorValidationFailedException(array, offset, count);
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException_InvalidUtf8()
	{
		throw new InvalidOperationException(SR.InvalidOperation_InvalidUtf8);
	}

	[DoesNotReturn]
	internal static void ThrowFormatException_BadFormatSpecifier()
	{
		throw new FormatException(SR.Argument_BadFormatSpecifier);
	}

	[DoesNotReturn]
	internal static void ThrowFormatException_NeedSingleChar()
	{
		throw new FormatException(SR.Format_NeedSingleChar);
	}

	[DoesNotReturn]
	internal static void ThrowFormatException_BadBoolean(ReadOnlySpan<char> value)
	{
		throw new FormatException(SR.Format(SR.Format_BadBoolean, new string(value)));
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRangeException_PrecisionTooLarge()
	{
		throw new ArgumentOutOfRangeException("precision", SR.Format(SR.Argument_PrecisionTooLarge, (byte)99));
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRangeException_SymbolDoesNotFit()
	{
		throw new ArgumentOutOfRangeException("symbol", SR.Argument_BadFormatSpecifier);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRangeException_NeedNonNegNum(string paramName)
	{
		throw new ArgumentOutOfRangeException(paramName, SR.ArgumentOutOfRange_NeedNonNegNum);
	}

	[DoesNotReturn]
	internal static void ArgumentOutOfRangeException_Enum_Value()
	{
		throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_Enum);
	}

	[DoesNotReturn]
	internal static void ThrowApplicationException(int hr)
	{
		Exception exceptionForHR = Marshal.GetExceptionForHR(hr);
		exceptionForHR = ((exceptionForHR == null || string.IsNullOrEmpty(exceptionForHR.Message)) ? new ApplicationException() : new ApplicationException(exceptionForHR.Message));
		exceptionForHR.HResult = hr;
		throw exceptionForHR;
	}

	[DoesNotReturn]
	internal static void ThrowFormatInvalidString()
	{
		throw new FormatException(SR.Format_InvalidString);
	}

	[DoesNotReturn]
	internal static void ThrowFormatInvalidString(int offset, ExceptionResource resource)
	{
		throw new FormatException(SR.Format(SR.Format_InvalidStringWithOffsetAndReason, offset, GetResourceString(resource)));
	}

	[DoesNotReturn]
	internal static void ThrowFormatIndexOutOfRange()
	{
		throw new FormatException(SR.Format_IndexOutOfRange);
	}

	internal static AmbiguousMatchException GetAmbiguousMatchException(MemberInfo memberInfo)
	{
		Type declaringType = memberInfo.DeclaringType;
		return new AmbiguousMatchException(SR.Format(SR.Arg_AmbiguousMatchException_MemberInfo, declaringType, memberInfo));
	}

	internal static AmbiguousMatchException GetAmbiguousMatchException(Attribute attribute)
	{
		return new AmbiguousMatchException(SR.Format(SR.Arg_AmbiguousMatchException_Attribute, attribute));
	}

	private static Exception GetArraySegmentCtorValidationFailedException(Array array, int offset, int count)
	{
		if (array == null)
		{
			return new ArgumentNullException("array");
		}
		if (offset < 0)
		{
			return new ArgumentOutOfRangeException("offset", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			return new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		return new ArgumentException(SR.Argument_InvalidOffLen);
	}

	private static ArgumentException GetArgumentException(ExceptionResource resource)
	{
		return new ArgumentException(GetResourceString(resource));
	}

	private static InvalidOperationException GetInvalidOperationException(ExceptionResource resource)
	{
		return new InvalidOperationException(GetResourceString(resource));
	}

	private static ArgumentException GetWrongKeyTypeArgumentException(object key, Type targetType)
	{
		return new ArgumentException(SR.Format(SR.Arg_WrongType, key, targetType), "key");
	}

	private static ArgumentException GetWrongValueTypeArgumentException(object value, Type targetType)
	{
		return new ArgumentException(SR.Format(SR.Arg_WrongType, value, targetType), "value");
	}

	private static KeyNotFoundException GetKeyNotFoundException(object key)
	{
		return new KeyNotFoundException(SR.Format(SR.Arg_KeyNotFoundWithKey, key));
	}

	private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(ExceptionArgument argument, ExceptionResource resource)
	{
		return new ArgumentOutOfRangeException(GetArgumentName(argument), GetResourceString(resource));
	}

	private static ArgumentException GetArgumentException(ExceptionResource resource, ExceptionArgument argument)
	{
		return new ArgumentException(GetResourceString(resource), GetArgumentName(argument));
	}

	private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(ExceptionArgument argument, int paramNumber, ExceptionResource resource)
	{
		return new ArgumentOutOfRangeException(GetArgumentName(argument) + "[" + paramNumber + "]", GetResourceString(resource));
	}

	private static InvalidOperationException GetInvalidOperationException_EnumCurrent(int index)
	{
		return new InvalidOperationException((index < 0) ? SR.InvalidOperation_EnumNotStarted : SR.InvalidOperation_EnumEnded);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void IfNullAndNullsAreIllegalThenThrow<T>(object value, ExceptionArgument argName)
	{
		if (default(T) != null && value == null)
		{
			ThrowArgumentNullException(argName);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ThrowForUnsupportedNumericsVectorBaseType<T>()
	{
		if (!Vector<T>.IsSupported)
		{
			ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ThrowForUnsupportedIntrinsicsVector64BaseType<T>()
	{
		if (!Vector64<T>.IsSupported)
		{
			ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ThrowForUnsupportedIntrinsicsVector128BaseType<T>()
	{
		if (!Vector128<T>.IsSupported)
		{
			ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ThrowForUnsupportedIntrinsicsVector256BaseType<T>()
	{
		if (!Vector256<T>.IsSupported)
		{
			ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ThrowForUnsupportedIntrinsicsVector512BaseType<T>()
	{
		if (!Vector512<T>.IsSupported)
		{
			ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		}
	}

	private static string GetArgumentName(ExceptionArgument argument)
	{
		return argument switch
		{
			ExceptionArgument.obj => "obj", 
			ExceptionArgument.dictionary => "dictionary", 
			ExceptionArgument.array => "array", 
			ExceptionArgument.info => "info", 
			ExceptionArgument.key => "key", 
			ExceptionArgument.text => "text", 
			ExceptionArgument.values => "values", 
			ExceptionArgument.value => "value", 
			ExceptionArgument.startIndex => "startIndex", 
			ExceptionArgument.task => "task", 
			ExceptionArgument.bytes => "bytes", 
			ExceptionArgument.byteIndex => "byteIndex", 
			ExceptionArgument.byteCount => "byteCount", 
			ExceptionArgument.ch => "ch", 
			ExceptionArgument.chars => "chars", 
			ExceptionArgument.charIndex => "charIndex", 
			ExceptionArgument.charCount => "charCount", 
			ExceptionArgument.s => "s", 
			ExceptionArgument.input => "input", 
			ExceptionArgument.ownedMemory => "ownedMemory", 
			ExceptionArgument.list => "list", 
			ExceptionArgument.index => "index", 
			ExceptionArgument.capacity => "capacity", 
			ExceptionArgument.collection => "collection", 
			ExceptionArgument.item => "item", 
			ExceptionArgument.converter => "converter", 
			ExceptionArgument.match => "match", 
			ExceptionArgument.count => "count", 
			ExceptionArgument.action => "action", 
			ExceptionArgument.comparison => "comparison", 
			ExceptionArgument.exceptions => "exceptions", 
			ExceptionArgument.exception => "exception", 
			ExceptionArgument.pointer => "pointer", 
			ExceptionArgument.start => "start", 
			ExceptionArgument.format => "format", 
			ExceptionArgument.formats => "formats", 
			ExceptionArgument.culture => "culture", 
			ExceptionArgument.comparer => "comparer", 
			ExceptionArgument.comparable => "comparable", 
			ExceptionArgument.source => "source", 
			ExceptionArgument.length => "length", 
			ExceptionArgument.comparisonType => "comparisonType", 
			ExceptionArgument.manager => "manager", 
			ExceptionArgument.sourceBytesToCopy => "sourceBytesToCopy", 
			ExceptionArgument.callBack => "callBack", 
			ExceptionArgument.creationOptions => "creationOptions", 
			ExceptionArgument.function => "function", 
			ExceptionArgument.scheduler => "scheduler", 
			ExceptionArgument.continuation => "continuation", 
			ExceptionArgument.continuationAction => "continuationAction", 
			ExceptionArgument.continuationFunction => "continuationFunction", 
			ExceptionArgument.tasks => "tasks", 
			ExceptionArgument.asyncResult => "asyncResult", 
			ExceptionArgument.beginMethod => "beginMethod", 
			ExceptionArgument.endMethod => "endMethod", 
			ExceptionArgument.endFunction => "endFunction", 
			ExceptionArgument.cancellationToken => "cancellationToken", 
			ExceptionArgument.continuationOptions => "continuationOptions", 
			ExceptionArgument.delay => "delay", 
			ExceptionArgument.millisecondsDelay => "millisecondsDelay", 
			ExceptionArgument.millisecondsTimeout => "millisecondsTimeout", 
			ExceptionArgument.stateMachine => "stateMachine", 
			ExceptionArgument.timeout => "timeout", 
			ExceptionArgument.type => "type", 
			ExceptionArgument.sourceIndex => "sourceIndex", 
			ExceptionArgument.sourceArray => "sourceArray", 
			ExceptionArgument.destinationIndex => "destinationIndex", 
			ExceptionArgument.destinationArray => "destinationArray", 
			ExceptionArgument.pHandle => "pHandle", 
			ExceptionArgument.handle => "handle", 
			ExceptionArgument.other => "other", 
			ExceptionArgument.newSize => "newSize", 
			ExceptionArgument.lowerBounds => "lowerBounds", 
			ExceptionArgument.lengths => "lengths", 
			ExceptionArgument.len => "len", 
			ExceptionArgument.keys => "keys", 
			ExceptionArgument.indices => "indices", 
			ExceptionArgument.index1 => "index1", 
			ExceptionArgument.index2 => "index2", 
			ExceptionArgument.index3 => "index3", 
			ExceptionArgument.length1 => "length1", 
			ExceptionArgument.length2 => "length2", 
			ExceptionArgument.length3 => "length3", 
			ExceptionArgument.endIndex => "endIndex", 
			ExceptionArgument.elementType => "elementType", 
			ExceptionArgument.arrayIndex => "arrayIndex", 
			ExceptionArgument.year => "year", 
			ExceptionArgument.codePoint => "codePoint", 
			ExceptionArgument.str => "str", 
			ExceptionArgument.options => "options", 
			ExceptionArgument.prefix => "prefix", 
			ExceptionArgument.suffix => "suffix", 
			ExceptionArgument.buffer => "buffer", 
			ExceptionArgument.buffers => "buffers", 
			ExceptionArgument.offset => "offset", 
			ExceptionArgument.stream => "stream", 
			ExceptionArgument.anyOf => "anyOf", 
			ExceptionArgument.overlapped => "overlapped", 
			ExceptionArgument.minimumBytes => "minimumBytes", 
			_ => "", 
		};
	}

	private static string GetResourceString(ExceptionResource resource)
	{
		return resource switch
		{
			ExceptionResource.ArgumentOutOfRange_IndexMustBeLessOrEqual => SR.ArgumentOutOfRange_IndexMustBeLessOrEqual, 
			ExceptionResource.ArgumentOutOfRange_IndexMustBeLess => SR.ArgumentOutOfRange_IndexMustBeLess, 
			ExceptionResource.ArgumentOutOfRange_IndexCount => SR.ArgumentOutOfRange_IndexCount, 
			ExceptionResource.ArgumentOutOfRange_IndexCountBuffer => SR.ArgumentOutOfRange_IndexCountBuffer, 
			ExceptionResource.ArgumentOutOfRange_Count => SR.ArgumentOutOfRange_Count, 
			ExceptionResource.ArgumentOutOfRange_Year => SR.ArgumentOutOfRange_Year, 
			ExceptionResource.Arg_ArrayPlusOffTooSmall => SR.Arg_ArrayPlusOffTooSmall, 
			ExceptionResource.Arg_ByteArrayTooSmallForValue => SR.Arg_ByteArrayTooSmallForValue, 
			ExceptionResource.NotSupported_ReadOnlyCollection => SR.NotSupported_ReadOnlyCollection, 
			ExceptionResource.Arg_RankMultiDimNotSupported => SR.Arg_RankMultiDimNotSupported, 
			ExceptionResource.Arg_NonZeroLowerBound => SR.Arg_NonZeroLowerBound, 
			ExceptionResource.ArgumentOutOfRange_GetCharCountOverflow => SR.ArgumentOutOfRange_GetCharCountOverflow, 
			ExceptionResource.ArgumentOutOfRange_ListInsert => SR.ArgumentOutOfRange_ListInsert, 
			ExceptionResource.ArgumentOutOfRange_NeedNonNegNum => SR.ArgumentOutOfRange_NeedNonNegNum, 
			ExceptionResource.ArgumentOutOfRange_SmallCapacity => SR.ArgumentOutOfRange_SmallCapacity, 
			ExceptionResource.Argument_InvalidOffLen => SR.Argument_InvalidOffLen, 
			ExceptionResource.Argument_CannotExtractScalar => SR.Argument_CannotExtractScalar, 
			ExceptionResource.ArgumentOutOfRange_BiggerThanCollection => SR.ArgumentOutOfRange_BiggerThanCollection, 
			ExceptionResource.Serialization_MissingKeys => SR.Serialization_MissingKeys, 
			ExceptionResource.Serialization_NullKey => SR.Serialization_NullKey, 
			ExceptionResource.NotSupported_KeyCollectionSet => SR.NotSupported_KeyCollectionSet, 
			ExceptionResource.NotSupported_ValueCollectionSet => SR.NotSupported_ValueCollectionSet, 
			ExceptionResource.InvalidOperation_NullArray => SR.InvalidOperation_NullArray, 
			ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted => SR.TaskT_TransitionToFinal_AlreadyCompleted, 
			ExceptionResource.TaskCompletionSourceT_TrySetException_NullException => SR.TaskCompletionSourceT_TrySetException_NullException, 
			ExceptionResource.TaskCompletionSourceT_TrySetException_NoExceptions => SR.TaskCompletionSourceT_TrySetException_NoExceptions, 
			ExceptionResource.NotSupported_StringComparison => SR.NotSupported_StringComparison, 
			ExceptionResource.ConcurrentCollection_SyncRoot_NotSupported => SR.ConcurrentCollection_SyncRoot_NotSupported, 
			ExceptionResource.Task_MultiTaskContinuation_NullTask => SR.Task_MultiTaskContinuation_NullTask, 
			ExceptionResource.InvalidOperation_WrongAsyncResultOrEndCalledMultiple => SR.InvalidOperation_WrongAsyncResultOrEndCalledMultiple, 
			ExceptionResource.Task_MultiTaskContinuation_EmptyTaskList => SR.Task_MultiTaskContinuation_EmptyTaskList, 
			ExceptionResource.Task_Start_TaskCompleted => SR.Task_Start_TaskCompleted, 
			ExceptionResource.Task_Start_Promise => SR.Task_Start_Promise, 
			ExceptionResource.Task_Start_ContinuationTask => SR.Task_Start_ContinuationTask, 
			ExceptionResource.Task_Start_AlreadyStarted => SR.Task_Start_AlreadyStarted, 
			ExceptionResource.Task_RunSynchronously_Continuation => SR.Task_RunSynchronously_Continuation, 
			ExceptionResource.Task_RunSynchronously_Promise => SR.Task_RunSynchronously_Promise, 
			ExceptionResource.Task_RunSynchronously_TaskCompleted => SR.Task_RunSynchronously_TaskCompleted, 
			ExceptionResource.Task_RunSynchronously_AlreadyStarted => SR.Task_RunSynchronously_AlreadyStarted, 
			ExceptionResource.AsyncMethodBuilder_InstanceNotInitialized => SR.AsyncMethodBuilder_InstanceNotInitialized, 
			ExceptionResource.Task_ContinueWith_ESandLR => SR.Task_ContinueWith_ESandLR, 
			ExceptionResource.Task_ContinueWith_NotOnAnything => SR.Task_ContinueWith_NotOnAnything, 
			ExceptionResource.Task_InvalidTimerTimeSpan => SR.Task_InvalidTimerTimeSpan, 
			ExceptionResource.Task_Delay_InvalidMillisecondsDelay => SR.Task_Delay_InvalidMillisecondsDelay, 
			ExceptionResource.Task_Dispose_NotCompleted => SR.Task_Dispose_NotCompleted, 
			ExceptionResource.Task_ThrowIfDisposed => SR.Task_ThrowIfDisposed, 
			ExceptionResource.Task_WaitMulti_NullTask => SR.Task_WaitMulti_NullTask, 
			ExceptionResource.ArgumentException_OtherNotArrayOfCorrectLength => SR.ArgumentException_OtherNotArrayOfCorrectLength, 
			ExceptionResource.ArgumentNull_Array => SR.ArgumentNull_Array, 
			ExceptionResource.ArgumentNull_SafeHandle => SR.ArgumentNull_SafeHandle, 
			ExceptionResource.ArgumentOutOfRange_EndIndexStartIndex => SR.ArgumentOutOfRange_EndIndexStartIndex, 
			ExceptionResource.ArgumentOutOfRange_Enum => SR.ArgumentOutOfRange_Enum, 
			ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported => SR.ArgumentOutOfRange_HugeArrayNotSupported, 
			ExceptionResource.Argument_AddingDuplicate => SR.Argument_AddingDuplicate, 
			ExceptionResource.Argument_InvalidArgumentForComparison => SR.Argument_InvalidArgumentForComparison, 
			ExceptionResource.Arg_LowerBoundsMustMatch => SR.Arg_LowerBoundsMustMatch, 
			ExceptionResource.Arg_MustBeType => SR.Arg_MustBeType, 
			ExceptionResource.Arg_Need1DArray => SR.Arg_Need1DArray, 
			ExceptionResource.Arg_Need2DArray => SR.Arg_Need2DArray, 
			ExceptionResource.Arg_Need3DArray => SR.Arg_Need3DArray, 
			ExceptionResource.Arg_NeedAtLeast1Rank => SR.Arg_NeedAtLeast1Rank, 
			ExceptionResource.Arg_RankIndices => SR.Arg_RankIndices, 
			ExceptionResource.Arg_RanksAndBounds => SR.Arg_RanksAndBounds, 
			ExceptionResource.InvalidOperation_IComparerFailed => SR.InvalidOperation_IComparerFailed, 
			ExceptionResource.NotSupported_FixedSizeCollection => SR.NotSupported_FixedSizeCollection, 
			ExceptionResource.Rank_MultiDimNotSupported => SR.Rank_MultiDimNotSupported, 
			ExceptionResource.Arg_TypeNotSupported => SR.Arg_TypeNotSupported, 
			ExceptionResource.Argument_SpansMustHaveSameLength => SR.Argument_SpansMustHaveSameLength, 
			ExceptionResource.Argument_InvalidFlag => SR.Argument_InvalidFlag, 
			ExceptionResource.CancellationTokenSource_Disposed => SR.CancellationTokenSource_Disposed, 
			ExceptionResource.Argument_AlignmentMustBePow2 => SR.Argument_AlignmentMustBePow2, 
			ExceptionResource.ArgumentOutOfRange_NotGreaterThanBufferLength => SR.ArgumentOutOfRange_NotGreaterThanBufferLength, 
			ExceptionResource.InvalidOperation_SpanOverlappedOperation => SR.InvalidOperation_SpanOverlappedOperation, 
			ExceptionResource.InvalidOperation_TimeProviderNullLocalTimeZone => SR.InvalidOperation_TimeProviderNullLocalTimeZone, 
			ExceptionResource.InvalidOperation_TimeProviderInvalidTimestampFrequency => SR.InvalidOperation_TimeProviderInvalidTimestampFrequency, 
			ExceptionResource.Format_UnexpectedClosingBrace => SR.Format_UnexpectedClosingBrace, 
			ExceptionResource.Format_UnclosedFormatItem => SR.Format_UnclosedFormatItem, 
			ExceptionResource.Format_ExpectedAsciiDigit => SR.Format_ExpectedAsciiDigit, 
			_ => "", 
		};
	}
}
