using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json;

internal static class ThrowHelper
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowOutOfMemoryException_BufferMaximumSizeExceeded(uint capacity)
	{
		throw new OutOfMemoryException(System.SR.Format(System.SR.BufferMaximumSizeExceeded, capacity));
	}

	[DoesNotReturn]
	public static void ThrowArgumentNullException(string parameterName)
	{
		throw new ArgumentNullException(parameterName);
	}

	[DoesNotReturn]
	public static void ThrowArgumentOutOfRangeException_MaxDepthMustBePositive(string parameterName)
	{
		throw GetArgumentOutOfRangeException(parameterName, System.SR.MaxDepthMustBePositive);
	}

	private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(string parameterName, string message)
	{
		return new ArgumentOutOfRangeException(parameterName, message);
	}

	[DoesNotReturn]
	public static void ThrowArgumentOutOfRangeException_CommentEnumMustBeInRange(string parameterName)
	{
		throw GetArgumentOutOfRangeException(parameterName, System.SR.CommentHandlingMustBeValid);
	}

	[DoesNotReturn]
	public static void ThrowArgumentOutOfRangeException_ArrayIndexNegative(string paramName)
	{
		throw new ArgumentOutOfRangeException(paramName, System.SR.ArrayIndexNegative);
	}

	[DoesNotReturn]
	public static void ThrowArgumentOutOfRangeException_JsonConverterFactory_TypeNotSupported(Type typeToConvert)
	{
		throw new ArgumentOutOfRangeException("typeToConvert", System.SR.Format(System.SR.SerializerConverterFactoryInvalidArgument, typeToConvert.FullName));
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_ArrayTooSmall(string paramName)
	{
		throw new ArgumentException(System.SR.ArrayTooSmall, paramName);
	}

	private static ArgumentException GetArgumentException(string message)
	{
		return new ArgumentException(message);
	}

	[DoesNotReturn]
	public static void ThrowArgumentException(string message)
	{
		throw GetArgumentException(message);
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_DestinationTooShort()
	{
		throw GetArgumentException(System.SR.DestinationTooShort);
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_PropertyNameTooLarge(int tokenLength)
	{
		throw GetArgumentException(System.SR.Format(System.SR.PropertyNameTooLarge, tokenLength));
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_ValueTooLarge(long tokenLength)
	{
		throw GetArgumentException(System.SR.Format(System.SR.ValueTooLarge, tokenLength));
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_ValueNotSupported()
	{
		throw GetArgumentException(System.SR.SpecialNumberValuesNotSupported);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_NeedLargerSpan()
	{
		throw GetInvalidOperationException(System.SR.FailedToGetLargerSpan);
	}

	[DoesNotReturn]
	public static void ThrowPropertyNameTooLargeArgumentException(int length)
	{
		throw GetArgumentException(System.SR.Format(System.SR.PropertyNameTooLarge, length));
	}

	[DoesNotReturn]
	public static void ThrowArgumentException(ReadOnlySpan<byte> propertyName, ReadOnlySpan<byte> value)
	{
		if (propertyName.Length > 166666666)
		{
			ThrowArgumentException(System.SR.Format(System.SR.PropertyNameTooLarge, propertyName.Length));
		}
		else
		{
			ThrowArgumentException(System.SR.Format(System.SR.ValueTooLarge, value.Length));
		}
	}

	[DoesNotReturn]
	public static void ThrowArgumentException(ReadOnlySpan<byte> propertyName, ReadOnlySpan<char> value)
	{
		if (propertyName.Length > 166666666)
		{
			ThrowArgumentException(System.SR.Format(System.SR.PropertyNameTooLarge, propertyName.Length));
		}
		else
		{
			ThrowArgumentException(System.SR.Format(System.SR.ValueTooLarge, value.Length));
		}
	}

	[DoesNotReturn]
	public static void ThrowArgumentException(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> value)
	{
		if (propertyName.Length > 166666666)
		{
			ThrowArgumentException(System.SR.Format(System.SR.PropertyNameTooLarge, propertyName.Length));
		}
		else
		{
			ThrowArgumentException(System.SR.Format(System.SR.ValueTooLarge, value.Length));
		}
	}

	[DoesNotReturn]
	public static void ThrowArgumentException(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> value)
	{
		if (propertyName.Length > 166666666)
		{
			ThrowArgumentException(System.SR.Format(System.SR.PropertyNameTooLarge, propertyName.Length));
		}
		else
		{
			ThrowArgumentException(System.SR.Format(System.SR.ValueTooLarge, value.Length));
		}
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationOrArgumentException(ReadOnlySpan<byte> propertyName, int currentDepth, int maxDepth)
	{
		currentDepth &= 0x7FFFFFFF;
		if (currentDepth >= maxDepth)
		{
			ThrowInvalidOperationException(System.SR.Format(System.SR.DepthTooLarge, currentDepth, maxDepth));
		}
		else
		{
			ThrowArgumentException(System.SR.Format(System.SR.PropertyNameTooLarge, propertyName.Length));
		}
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException(int currentDepth, int maxDepth)
	{
		currentDepth &= 0x7FFFFFFF;
		ThrowInvalidOperationException(System.SR.Format(System.SR.DepthTooLarge, currentDepth, maxDepth));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException(string message)
	{
		throw GetInvalidOperationException(message);
	}

	private static InvalidOperationException GetInvalidOperationException(string message)
	{
		return new InvalidOperationException(message)
		{
			Source = "System.Text.Json.Rethrowable"
		};
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationOrArgumentException(ReadOnlySpan<char> propertyName, int currentDepth, int maxDepth)
	{
		currentDepth &= 0x7FFFFFFF;
		if (currentDepth >= maxDepth)
		{
			ThrowInvalidOperationException(System.SR.Format(System.SR.DepthTooLarge, currentDepth, maxDepth));
		}
		else
		{
			ThrowArgumentException(System.SR.Format(System.SR.PropertyNameTooLarge, propertyName.Length));
		}
	}

	public static InvalidOperationException GetInvalidOperationException_ExpectedArray(JsonTokenType tokenType)
	{
		return GetInvalidOperationException("array", tokenType);
	}

	public static InvalidOperationException GetInvalidOperationException_ExpectedObject(JsonTokenType tokenType)
	{
		return GetInvalidOperationException("object", tokenType);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ExpectedNumber(JsonTokenType tokenType)
	{
		throw GetInvalidOperationException("number", tokenType);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ExpectedBoolean(JsonTokenType tokenType)
	{
		throw GetInvalidOperationException("boolean", tokenType);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ExpectedString(JsonTokenType tokenType)
	{
		throw GetInvalidOperationException("string", tokenType);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ExpectedPropertyName(JsonTokenType tokenType)
	{
		throw GetInvalidOperationException("propertyName", tokenType);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ExpectedStringComparison(JsonTokenType tokenType)
	{
		throw GetInvalidOperationException(tokenType);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ExpectedComment(JsonTokenType tokenType)
	{
		throw GetInvalidOperationException("comment", tokenType);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_CannotSkipOnPartial()
	{
		throw GetInvalidOperationException(System.SR.CannotSkip);
	}

	private static InvalidOperationException GetInvalidOperationException(string message, JsonTokenType tokenType)
	{
		return GetInvalidOperationException(System.SR.Format(System.SR.InvalidCast, tokenType, message));
	}

	private static InvalidOperationException GetInvalidOperationException(JsonTokenType tokenType)
	{
		return GetInvalidOperationException(System.SR.Format(System.SR.InvalidComparison, tokenType));
	}

	[DoesNotReturn]
	internal static void ThrowJsonElementWrongTypeException(JsonTokenType expectedType, JsonTokenType actualType)
	{
		throw GetJsonElementWrongTypeException(expectedType.ToValueKind(), actualType.ToValueKind());
	}

	internal static InvalidOperationException GetJsonElementWrongTypeException(JsonValueKind expectedType, JsonValueKind actualType)
	{
		return GetInvalidOperationException(System.SR.Format(System.SR.JsonElementHasWrongType, expectedType, actualType));
	}

	internal static InvalidOperationException GetJsonElementWrongTypeException(string expectedTypeName, JsonValueKind actualType)
	{
		return GetInvalidOperationException(System.SR.Format(System.SR.JsonElementHasWrongType, expectedTypeName, actualType));
	}

	[DoesNotReturn]
	public static void ThrowJsonReaderException(ref Utf8JsonReader json, ExceptionResource resource, byte nextByte = 0, ReadOnlySpan<byte> bytes = default(ReadOnlySpan<byte>))
	{
		throw GetJsonReaderException(ref json, resource, nextByte, bytes);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static JsonException GetJsonReaderException(ref Utf8JsonReader json, ExceptionResource resource, byte nextByte, ReadOnlySpan<byte> bytes)
	{
		string resourceString = GetResourceString(ref json, resource, nextByte, JsonHelpers.Utf8GetString(bytes));
		long lineNumber = json.CurrentState._lineNumber;
		long bytePositionInLine = json.CurrentState._bytePositionInLine;
		resourceString += $" LineNumber: {lineNumber} | BytePositionInLine: {bytePositionInLine}.";
		return new JsonReaderException(resourceString, lineNumber, bytePositionInLine);
	}

	private static bool IsPrintable(byte value)
	{
		if (value >= 32)
		{
			return value < 127;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static string GetPrintableString(byte value)
	{
		if (!IsPrintable(value))
		{
			return $"0x{value:X2}";
		}
		char c = (char)value;
		return c.ToString();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string GetResourceString(ref Utf8JsonReader json, ExceptionResource resource, byte nextByte, string characters)
	{
		string printableString = GetPrintableString(nextByte);
		string result = "";
		switch (resource)
		{
		case ExceptionResource.ArrayDepthTooLarge:
			result = System.SR.Format(System.SR.ArrayDepthTooLarge, json.CurrentState.Options.MaxDepth);
			break;
		case ExceptionResource.MismatchedObjectArray:
			result = System.SR.Format(System.SR.MismatchedObjectArray, printableString);
			break;
		case ExceptionResource.TrailingCommaNotAllowedBeforeArrayEnd:
			result = System.SR.TrailingCommaNotAllowedBeforeArrayEnd;
			break;
		case ExceptionResource.TrailingCommaNotAllowedBeforeObjectEnd:
			result = System.SR.TrailingCommaNotAllowedBeforeObjectEnd;
			break;
		case ExceptionResource.EndOfStringNotFound:
			result = System.SR.EndOfStringNotFound;
			break;
		case ExceptionResource.RequiredDigitNotFoundAfterSign:
			result = System.SR.Format(System.SR.RequiredDigitNotFoundAfterSign, printableString);
			break;
		case ExceptionResource.RequiredDigitNotFoundAfterDecimal:
			result = System.SR.Format(System.SR.RequiredDigitNotFoundAfterDecimal, printableString);
			break;
		case ExceptionResource.RequiredDigitNotFoundEndOfData:
			result = System.SR.RequiredDigitNotFoundEndOfData;
			break;
		case ExceptionResource.ExpectedEndAfterSingleJson:
			result = System.SR.Format(System.SR.ExpectedEndAfterSingleJson, printableString);
			break;
		case ExceptionResource.ExpectedEndOfDigitNotFound:
			result = System.SR.Format(System.SR.ExpectedEndOfDigitNotFound, printableString);
			break;
		case ExceptionResource.ExpectedNextDigitEValueNotFound:
			result = System.SR.Format(System.SR.ExpectedNextDigitEValueNotFound, printableString);
			break;
		case ExceptionResource.ExpectedSeparatorAfterPropertyNameNotFound:
			result = System.SR.Format(System.SR.ExpectedSeparatorAfterPropertyNameNotFound, printableString);
			break;
		case ExceptionResource.ExpectedStartOfPropertyNotFound:
			result = System.SR.Format(System.SR.ExpectedStartOfPropertyNotFound, printableString);
			break;
		case ExceptionResource.ExpectedStartOfPropertyOrValueNotFound:
			result = System.SR.ExpectedStartOfPropertyOrValueNotFound;
			break;
		case ExceptionResource.ExpectedStartOfPropertyOrValueAfterComment:
			result = System.SR.Format(System.SR.ExpectedStartOfPropertyOrValueAfterComment, printableString);
			break;
		case ExceptionResource.ExpectedStartOfValueNotFound:
			result = System.SR.Format(System.SR.ExpectedStartOfValueNotFound, printableString);
			break;
		case ExceptionResource.ExpectedValueAfterPropertyNameNotFound:
			result = System.SR.ExpectedValueAfterPropertyNameNotFound;
			break;
		case ExceptionResource.FoundInvalidCharacter:
			result = System.SR.Format(System.SR.FoundInvalidCharacter, printableString);
			break;
		case ExceptionResource.InvalidEndOfJsonNonPrimitive:
			result = System.SR.Format(System.SR.InvalidEndOfJsonNonPrimitive, json.TokenType);
			break;
		case ExceptionResource.ObjectDepthTooLarge:
			result = System.SR.Format(System.SR.ObjectDepthTooLarge, json.CurrentState.Options.MaxDepth);
			break;
		case ExceptionResource.ExpectedFalse:
			result = System.SR.Format(System.SR.ExpectedFalse, characters);
			break;
		case ExceptionResource.ExpectedNull:
			result = System.SR.Format(System.SR.ExpectedNull, characters);
			break;
		case ExceptionResource.ExpectedTrue:
			result = System.SR.Format(System.SR.ExpectedTrue, characters);
			break;
		case ExceptionResource.InvalidCharacterWithinString:
			result = System.SR.Format(System.SR.InvalidCharacterWithinString, printableString);
			break;
		case ExceptionResource.InvalidCharacterAfterEscapeWithinString:
			result = System.SR.Format(System.SR.InvalidCharacterAfterEscapeWithinString, printableString);
			break;
		case ExceptionResource.InvalidHexCharacterWithinString:
			result = System.SR.Format(System.SR.InvalidHexCharacterWithinString, printableString);
			break;
		case ExceptionResource.EndOfCommentNotFound:
			result = System.SR.EndOfCommentNotFound;
			break;
		case ExceptionResource.ZeroDepthAtEnd:
			result = System.SR.Format(System.SR.ZeroDepthAtEnd);
			break;
		case ExceptionResource.ExpectedJsonTokens:
			result = System.SR.ExpectedJsonTokens;
			break;
		case ExceptionResource.NotEnoughData:
			result = System.SR.NotEnoughData;
			break;
		case ExceptionResource.ExpectedOneCompleteToken:
			result = System.SR.ExpectedOneCompleteToken;
			break;
		case ExceptionResource.InvalidCharacterAtStartOfComment:
			result = System.SR.Format(System.SR.InvalidCharacterAtStartOfComment, printableString);
			break;
		case ExceptionResource.UnexpectedEndOfDataWhileReadingComment:
			result = System.SR.Format(System.SR.UnexpectedEndOfDataWhileReadingComment);
			break;
		case ExceptionResource.UnexpectedEndOfLineSeparator:
			result = System.SR.Format(System.SR.UnexpectedEndOfLineSeparator);
			break;
		case ExceptionResource.InvalidLeadingZeroInNumber:
			result = System.SR.Format(System.SR.InvalidLeadingZeroInNumber, printableString);
			break;
		}
		return result;
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException(ExceptionResource resource, int currentDepth, int maxDepth, byte token, JsonTokenType tokenType)
	{
		throw GetInvalidOperationException(resource, currentDepth, maxDepth, token, tokenType);
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_InvalidCommentValue()
	{
		throw new ArgumentException(System.SR.CannotWriteCommentWithEmbeddedDelimiter);
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_InvalidUTF8(ReadOnlySpan<byte> value)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = Math.Min(value.Length, 10);
		for (int i = 0; i < num; i++)
		{
			byte value2 = value[i];
			if (IsPrintable(value2))
			{
				stringBuilder.Append((char)value2);
				continue;
			}
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(2, 1, stringBuilder2);
			handler.AppendLiteral("0x");
			handler.AppendFormatted(value2, "X2");
			stringBuilder2.Append(ref handler);
		}
		if (num < value.Length)
		{
			stringBuilder.Append("...");
		}
		throw new ArgumentException(System.SR.Format(System.SR.CannotEncodeInvalidUTF8, stringBuilder));
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_InvalidUTF16(int charAsInt)
	{
		throw new ArgumentException(System.SR.Format(System.SR.CannotEncodeInvalidUTF16, $"0x{charAsInt:X2}"));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ReadInvalidUTF16(int charAsInt)
	{
		throw GetInvalidOperationException(System.SR.Format(System.SR.CannotReadInvalidUTF16, $"0x{charAsInt:X2}"));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ReadIncompleteUTF16()
	{
		throw GetInvalidOperationException(System.SR.CannotReadIncompleteUTF16);
	}

	public static InvalidOperationException GetInvalidOperationException_ReadInvalidUTF8(DecoderFallbackException innerException = null)
	{
		return GetInvalidOperationException(System.SR.CannotTranscodeInvalidUtf8, innerException);
	}

	public static ArgumentException GetArgumentException_ReadInvalidUTF16(EncoderFallbackException innerException)
	{
		return new ArgumentException(System.SR.CannotTranscodeInvalidUtf16, innerException);
	}

	public static InvalidOperationException GetInvalidOperationException(string message, Exception innerException)
	{
		InvalidOperationException ex = new InvalidOperationException(message, innerException);
		ex.Source = "System.Text.Json.Rethrowable";
		return ex;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static InvalidOperationException GetInvalidOperationException(ExceptionResource resource, int currentDepth, int maxDepth, byte token, JsonTokenType tokenType)
	{
		string resourceString = GetResourceString(resource, currentDepth, maxDepth, token, tokenType);
		InvalidOperationException invalidOperationException = GetInvalidOperationException(resourceString);
		invalidOperationException.Source = "System.Text.Json.Rethrowable";
		return invalidOperationException;
	}

	[DoesNotReturn]
	public static void ThrowOutOfMemoryException(uint capacity)
	{
		throw new OutOfMemoryException(System.SR.Format(System.SR.BufferMaximumSizeExceeded, capacity));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string GetResourceString(ExceptionResource resource, int currentDepth, int maxDepth, byte token, JsonTokenType tokenType)
	{
		string result = "";
		switch (resource)
		{
		case ExceptionResource.MismatchedObjectArray:
			result = ((tokenType == JsonTokenType.PropertyName) ? System.SR.Format(System.SR.CannotWriteEndAfterProperty, (char)token) : System.SR.Format(System.SR.MismatchedObjectArray, (char)token));
			break;
		case ExceptionResource.DepthTooLarge:
			result = System.SR.Format(System.SR.DepthTooLarge, currentDepth & 0x7FFFFFFF, maxDepth);
			break;
		case ExceptionResource.CannotStartObjectArrayWithoutProperty:
			result = System.SR.Format(System.SR.CannotStartObjectArrayWithoutProperty, tokenType);
			break;
		case ExceptionResource.CannotStartObjectArrayAfterPrimitiveOrClose:
			result = System.SR.Format(System.SR.CannotStartObjectArrayAfterPrimitiveOrClose, tokenType);
			break;
		case ExceptionResource.CannotWriteValueWithinObject:
			result = System.SR.Format(System.SR.CannotWriteValueWithinObject, tokenType);
			break;
		case ExceptionResource.CannotWritePropertyWithinArray:
			result = ((tokenType == JsonTokenType.PropertyName) ? System.SR.Format(System.SR.CannotWritePropertyAfterProperty) : System.SR.Format(System.SR.CannotWritePropertyWithinArray, tokenType));
			break;
		case ExceptionResource.CannotWriteValueAfterPrimitiveOrClose:
			result = System.SR.Format(System.SR.CannotWriteValueAfterPrimitiveOrClose, tokenType);
			break;
		}
		return result;
	}

	[DoesNotReturn]
	public static void ThrowFormatException()
	{
		throw new FormatException
		{
			Source = "System.Text.Json.Rethrowable"
		};
	}

	public static void ThrowFormatException(NumericType numericType)
	{
		string message = "";
		switch (numericType)
		{
		case NumericType.Byte:
			message = System.SR.FormatByte;
			break;
		case NumericType.SByte:
			message = System.SR.FormatSByte;
			break;
		case NumericType.Int16:
			message = System.SR.FormatInt16;
			break;
		case NumericType.Int32:
			message = System.SR.FormatInt32;
			break;
		case NumericType.Int64:
			message = System.SR.FormatInt64;
			break;
		case NumericType.Int128:
			message = System.SR.FormatInt128;
			break;
		case NumericType.UInt16:
			message = System.SR.FormatUInt16;
			break;
		case NumericType.UInt32:
			message = System.SR.FormatUInt32;
			break;
		case NumericType.UInt64:
			message = System.SR.FormatUInt64;
			break;
		case NumericType.UInt128:
			message = System.SR.FormatUInt128;
			break;
		case NumericType.Half:
			message = System.SR.FormatHalf;
			break;
		case NumericType.Single:
			message = System.SR.FormatSingle;
			break;
		case NumericType.Double:
			message = System.SR.FormatDouble;
			break;
		case NumericType.Decimal:
			message = System.SR.FormatDecimal;
			break;
		}
		throw new FormatException(message)
		{
			Source = "System.Text.Json.Rethrowable"
		};
	}

	[DoesNotReturn]
	public static void ThrowFormatException(DataType dataType)
	{
		string message = "";
		switch (dataType)
		{
		case DataType.Boolean:
		case DataType.DateOnly:
		case DataType.DateTime:
		case DataType.DateTimeOffset:
		case DataType.TimeOnly:
		case DataType.TimeSpan:
		case DataType.Guid:
		case DataType.Version:
			message = System.SR.Format(System.SR.UnsupportedFormat, dataType);
			break;
		case DataType.Base64String:
			message = System.SR.CannotDecodeInvalidBase64;
			break;
		}
		throw new FormatException(message)
		{
			Source = "System.Text.Json.Rethrowable"
		};
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ExpectedChar(JsonTokenType tokenType)
	{
		throw GetInvalidOperationException("char", tokenType);
	}

	[DoesNotReturn]
	public static void ThrowObjectDisposedException_Utf8JsonWriter()
	{
		throw new ObjectDisposedException("Utf8JsonWriter");
	}

	[DoesNotReturn]
	public static void ThrowObjectDisposedException_JsonDocument()
	{
		throw new ObjectDisposedException("JsonDocument");
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_NodeValueNotAllowed(string paramName)
	{
		throw new ArgumentException(System.SR.NodeValueNotAllowed, paramName);
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_DuplicateKey(string paramName, string propertyName)
	{
		throw new ArgumentException(System.SR.Format(System.SR.NodeDuplicateKey, propertyName), paramName);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_NodeAlreadyHasParent()
	{
		throw new InvalidOperationException(System.SR.NodeAlreadyHasParent);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_NodeCycleDetected()
	{
		throw new InvalidOperationException(System.SR.NodeCycleDetected);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_NodeElementCannotBeObjectOrArray()
	{
		throw new InvalidOperationException(System.SR.NodeElementCannotBeObjectOrArray);
	}

	[DoesNotReturn]
	public static void ThrowNotSupportedException_CollectionIsReadOnly()
	{
		throw GetNotSupportedException_CollectionIsReadOnly();
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_NodeWrongType(string typeName)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.NodeWrongType, typeName));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_NodeParentWrongType(string typeName)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.NodeParentWrongType, typeName));
	}

	public static NotSupportedException GetNotSupportedException_CollectionIsReadOnly()
	{
		return new NotSupportedException(System.SR.CollectionIsReadOnly);
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_DeserializeWrongType(Type type, object value)
	{
		throw new ArgumentException(System.SR.Format(System.SR.DeserializeWrongType, type, value.GetType()));
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_SerializerDoesNotSupportComments(string paramName)
	{
		throw new ArgumentException(System.SR.JsonSerializerDoesNotSupportComments, paramName);
	}

	[DoesNotReturn]
	public static void ThrowNotSupportedException_SerializationNotSupported(Type propertyType)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.SerializationNotSupportedType, propertyType));
	}

	[DoesNotReturn]
	public static void ThrowNotSupportedException_TypeRequiresAsyncSerialization(Type propertyType)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.TypeRequiresAsyncSerialization, propertyType));
	}

	[DoesNotReturn]
	public static void ThrowNotSupportedException_DictionaryKeyTypeNotSupported(Type keyType, JsonConverter converter)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.DictionaryKeyTypeNotSupported, keyType, converter.GetType()));
	}

	[DoesNotReturn]
	public static void ThrowJsonException_DeserializeUnableToConvertValue(Type propertyType)
	{
		throw new JsonException(System.SR.Format(System.SR.DeserializeUnableToConvertValue, propertyType))
		{
			AppendPathInformation = true
		};
	}

	[DoesNotReturn]
	public static void ThrowInvalidCastException_DeserializeUnableToAssignValue(Type typeOfValue, Type declaredType)
	{
		throw new InvalidCastException(System.SR.Format(System.SR.DeserializeUnableToAssignValue, typeOfValue, declaredType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_DeserializeUnableToAssignNull(Type declaredType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.DeserializeUnableToAssignNull, declaredType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ObjectCreationHandlingPopulateNotSupportedByConverter(JsonPropertyInfo propertyInfo)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.ObjectCreationHandlingPopulateNotSupportedByConverter, propertyInfo.Name, propertyInfo.DeclaringType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ObjectCreationHandlingPropertyMustHaveAGetter(JsonPropertyInfo propertyInfo)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.ObjectCreationHandlingPropertyMustHaveAGetter, propertyInfo.Name, propertyInfo.DeclaringType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ObjectCreationHandlingPropertyValueTypeMustHaveASetter(JsonPropertyInfo propertyInfo)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.ObjectCreationHandlingPropertyValueTypeMustHaveASetter, propertyInfo.Name, propertyInfo.DeclaringType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ObjectCreationHandlingPropertyCannotAllowPolymorphicDeserialization(JsonPropertyInfo propertyInfo)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.ObjectCreationHandlingPropertyCannotAllowPolymorphicDeserialization, propertyInfo.Name, propertyInfo.DeclaringType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ObjectCreationHandlingPropertyCannotAllowReadOnlyMember(JsonPropertyInfo propertyInfo)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.ObjectCreationHandlingPropertyCannotAllowReadOnlyMember, propertyInfo.Name, propertyInfo.DeclaringType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ObjectCreationHandlingPropertyCannotAllowReferenceHandling()
	{
		throw new InvalidOperationException(System.SR.ObjectCreationHandlingPropertyCannotAllowReferenceHandling);
	}

	[DoesNotReturn]
	public static void ThrowNotSupportedException_ObjectCreationHandlingPropertyDoesNotSupportParameterizedConstructors()
	{
		throw new NotSupportedException(System.SR.ObjectCreationHandlingPropertyDoesNotSupportParameterizedConstructors);
	}

	[DoesNotReturn]
	public static void ThrowJsonException_SerializationConverterRead(JsonConverter converter)
	{
		throw new JsonException(System.SR.Format(System.SR.SerializationConverterRead, converter))
		{
			AppendPathInformation = true
		};
	}

	[DoesNotReturn]
	public static void ThrowJsonException_SerializationConverterWrite(JsonConverter converter)
	{
		throw new JsonException(System.SR.Format(System.SR.SerializationConverterWrite, converter))
		{
			AppendPathInformation = true
		};
	}

	[DoesNotReturn]
	public static void ThrowJsonException_SerializerCycleDetected(int maxDepth)
	{
		throw new JsonException(System.SR.Format(System.SR.SerializerCycleDetected, maxDepth))
		{
			AppendPathInformation = true
		};
	}

	[DoesNotReturn]
	public static void ThrowJsonException(string message = null)
	{
		throw new JsonException(message)
		{
			AppendPathInformation = true
		};
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_CannotSerializeInvalidType(string paramName, Type typeToConvert, Type declaringType, string propertyName)
	{
		if (declaringType == null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.CannotSerializeInvalidType, typeToConvert), paramName);
		}
		throw new ArgumentException(System.SR.Format(System.SR.CannotSerializeInvalidMember, typeToConvert, propertyName, declaringType), paramName);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_CannotSerializeInvalidType(Type typeToConvert, Type declaringType, MemberInfo memberInfo)
	{
		if (declaringType == null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.CannotSerializeInvalidType, typeToConvert));
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.CannotSerializeInvalidMember, typeToConvert, memberInfo.Name, declaringType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializationConverterNotCompatible(Type converterType, Type type)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializationConverterNotCompatible, converterType, type));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ResolverTypeNotCompatible(Type requestedType, Type actualType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.ResolverTypeNotCompatible, actualType, requestedType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ResolverTypeInfoOptionsNotCompatible()
	{
		throw new InvalidOperationException(System.SR.ResolverTypeInfoOptionsNotCompatible);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_JsonSerializerOptionsNoTypeInfoResolverSpecified()
	{
		throw new InvalidOperationException(System.SR.JsonSerializerOptionsNoTypeInfoResolverSpecified);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_JsonSerializerIsReflectionDisabled()
	{
		throw new InvalidOperationException(System.SR.JsonSerializerIsReflectionDisabled);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializationConverterOnAttributeInvalid(Type classType, MemberInfo memberInfo)
	{
		string text = classType.ToString();
		if (memberInfo != null)
		{
			text = text + "." + memberInfo.Name;
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializationConverterOnAttributeInvalid, text));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializationConverterOnAttributeNotCompatible(Type classTypeAttributeIsOn, MemberInfo memberInfo, Type typeToConvert)
	{
		string text = classTypeAttributeIsOn.ToString();
		if (memberInfo != null)
		{
			text = text + "." + memberInfo.Name;
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializationConverterOnAttributeNotCompatible, text, typeToConvert));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializerOptionsReadOnly(JsonSerializerContext context)
	{
		string message = ((context == null) ? System.SR.SerializerOptionsReadOnly : System.SR.SerializerContextOptionsReadOnly);
		throw new InvalidOperationException(message);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_DefaultTypeInfoResolverImmutable()
	{
		throw new InvalidOperationException(System.SR.DefaultTypeInfoResolverImmutable);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_TypeInfoResolverChainImmutable()
	{
		throw new InvalidOperationException(System.SR.TypeInfoResolverChainImmutable);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_TypeInfoImmutable()
	{
		throw new InvalidOperationException(System.SR.TypeInfoImmutable);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_InvalidChainedResolver()
	{
		throw new InvalidOperationException(System.SR.SerializerOptions_InvalidChainedResolver);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializerPropertyNameConflict(Type type, string propertyName)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializerPropertyNameConflict, type, propertyName));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializerPropertyNameNull(JsonPropertyInfo jsonPropertyInfo)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializerPropertyNameNull, jsonPropertyInfo.DeclaringType, jsonPropertyInfo.MemberName));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_JsonPropertyRequiredAndNotDeserializable(JsonPropertyInfo jsonPropertyInfo)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.JsonPropertyRequiredAndNotDeserializable, jsonPropertyInfo.Name, jsonPropertyInfo.DeclaringType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_JsonPropertyRequiredAndExtensionData(JsonPropertyInfo jsonPropertyInfo)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.JsonPropertyRequiredAndExtensionData, jsonPropertyInfo.Name, jsonPropertyInfo.DeclaringType));
	}

	[DoesNotReturn]
	public static void ThrowJsonException_JsonRequiredPropertyMissing(JsonTypeInfo parent, BitArray requiredPropertiesSet)
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = true;
		foreach (KeyValuePair<string, JsonPropertyInfo> item in parent.PropertyCache.List)
		{
			JsonPropertyInfo value = item.Value;
			if (value.IsRequired && !requiredPropertiesSet[value.RequiredPropertyIndex])
			{
				if (!flag)
				{
					stringBuilder.Append(CultureInfo.CurrentUICulture.TextInfo.ListSeparator);
					stringBuilder.Append(' ');
				}
				stringBuilder.Append(value.Name);
				flag = false;
				if (stringBuilder.Length >= 50)
				{
					break;
				}
			}
		}
		throw new JsonException(System.SR.Format(System.SR.JsonRequiredPropertiesMissing, parent.Type, stringBuilder.ToString()));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_NamingPolicyReturnNull(JsonNamingPolicy namingPolicy)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.NamingPolicyReturnNull, namingPolicy));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializerConverterFactoryReturnsNull(Type converterType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializerConverterFactoryReturnsNull, converterType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializerConverterFactoryReturnsJsonConverterFactorty(Type converterType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializerConverterFactoryReturnsJsonConverterFactory, converterType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_MultiplePropertiesBindToConstructorParameters(Type parentType, string parameterName, string firstMatchName, string secondMatchName)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.MultipleMembersBindWithConstructorParameter, firstMatchName, secondMatchName, parentType, parameterName));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ConstructorParameterIncompleteBinding(Type parentType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.ConstructorParamIncompleteBinding, parentType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ExtensionDataCannotBindToCtorParam(string propertyName, JsonPropertyInfo jsonPropertyInfo)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.ExtensionDataCannotBindToCtorParam, propertyName, jsonPropertyInfo.DeclaringType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_JsonIncludeOnInaccessibleProperty(string memberName, Type declaringType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.JsonIncludeOnInaccessibleProperty, memberName, declaringType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_IgnoreConditionOnValueTypeInvalid(string clrPropertyName, Type propertyDeclaringType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.IgnoreConditionOnValueTypeInvalid, clrPropertyName, propertyDeclaringType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_NumberHandlingOnPropertyInvalid(JsonPropertyInfo jsonPropertyInfo)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.NumberHandlingOnPropertyInvalid, jsonPropertyInfo.MemberName, jsonPropertyInfo.DeclaringType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ConverterCanConvertMultipleTypes(Type runtimePropertyType, JsonConverter jsonConverter)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.ConverterCanConvertMultipleTypes, jsonConverter.GetType(), jsonConverter.Type, runtimePropertyType));
	}

	[DoesNotReturn]
	public static void ThrowNotSupportedException_ObjectWithParameterizedCtorRefMetadataNotSupported(ReadOnlySpan<byte> propertyName, ref Utf8JsonReader reader, scoped ref ReadStack state)
	{
		JsonTypeInfo topJsonTypeInfoWithParameterizedConstructor = state.GetTopJsonTypeInfoWithParameterizedConstructor();
		state.Current.JsonPropertyName = propertyName.ToArray();
		NotSupportedException ex = new NotSupportedException(System.SR.Format(System.SR.ObjectWithParameterizedCtorRefMetadataNotSupported, topJsonTypeInfoWithParameterizedConstructor.Type));
		ThrowNotSupportedException(ref state, in reader, ex);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_JsonTypeInfoOperationNotPossibleForKind(JsonTypeInfoKind kind)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.InvalidJsonTypeInfoOperationForKind, kind));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_CreateObjectConverterNotCompatible(Type type)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.CreateObjectConverterNotCompatible, type));
	}

	[DoesNotReturn]
	public static void ReThrowWithPath(scoped ref ReadStack state, JsonReaderException ex)
	{
		string text = state.JsonPath();
		string message = ex.Message;
		int num = message.AsSpan().LastIndexOf(" LineNumber: ");
		message = ((num < 0) ? (message + " Path: " + text + ".") : $"{message.Substring(0, num)} Path: {text} |{message.Substring(num)}");
		throw new JsonException(message, text, ex.LineNumber, ex.BytePositionInLine, ex);
	}

	[DoesNotReturn]
	public static void ReThrowWithPath(scoped ref ReadStack state, in Utf8JsonReader reader, Exception ex)
	{
		JsonException ex2 = new JsonException(null, ex);
		AddJsonExceptionInformation(ref state, in reader, ex2);
		throw ex2;
	}

	public static void AddJsonExceptionInformation(scoped ref ReadStack state, in Utf8JsonReader reader, JsonException ex)
	{
		long lineNumber = reader.CurrentState._lineNumber;
		ex.LineNumber = lineNumber;
		long bytePositionInLine = reader.CurrentState._bytePositionInLine;
		ex.BytePositionInLine = bytePositionInLine;
		string value = (ex.Path = state.JsonPath());
		string text2 = ex._message;
		if (string.IsNullOrEmpty(text2))
		{
			Type p = state.Current.JsonPropertyInfo?.PropertyType ?? state.Current.JsonTypeInfo.Type;
			text2 = System.SR.Format(System.SR.DeserializeUnableToConvertValue, p);
			ex.AppendPathInformation = true;
		}
		if (ex.AppendPathInformation)
		{
			text2 += $" Path: {value} | LineNumber: {lineNumber} | BytePositionInLine: {bytePositionInLine}.";
			ex.SetMessage(text2);
		}
	}

	[DoesNotReturn]
	public static void ReThrowWithPath(ref WriteStack state, Exception ex)
	{
		JsonException ex2 = new JsonException(null, ex);
		AddJsonExceptionInformation(ref state, ex2);
		throw ex2;
	}

	public static void AddJsonExceptionInformation(ref WriteStack state, JsonException ex)
	{
		string text2 = (ex.Path = state.PropertyPath());
		string text3 = ex._message;
		if (string.IsNullOrEmpty(text3))
		{
			text3 = System.SR.Format(System.SR.SerializeUnableToSerialize);
			ex.AppendPathInformation = true;
		}
		if (ex.AppendPathInformation)
		{
			text3 = text3 + " Path: " + text2 + ".";
			ex.SetMessage(text3);
		}
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializationDuplicateAttribute(Type attribute, MemberInfo memberInfo)
	{
		string p = ((memberInfo is Type type) ? type.ToString() : $"{memberInfo.DeclaringType}.{memberInfo.Name}");
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializationDuplicateAttribute, attribute, p));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializationDuplicateTypeAttribute(Type classType, Type attribute)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializationDuplicateTypeAttribute, classType, attribute));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializationDuplicateTypeAttribute<TAttribute>(Type classType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializationDuplicateTypeAttribute, classType, typeof(TAttribute)));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ExtensionDataConflictsWithUnmappedMemberHandling(Type classType, JsonPropertyInfo jsonPropertyInfo)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.ExtensionDataConflictsWithUnmappedMemberHandling, classType, jsonPropertyInfo.MemberName));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializationDataExtensionPropertyInvalid(JsonPropertyInfo jsonPropertyInfo)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializationDataExtensionPropertyInvalid, jsonPropertyInfo.PropertyType, jsonPropertyInfo.MemberName));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_NodeJsonObjectCustomConverterNotAllowedOnExtensionProperty()
	{
		throw new InvalidOperationException(System.SR.NodeJsonObjectCustomConverterNotAllowedOnExtensionProperty);
	}

	[DoesNotReturn]
	public static void ThrowNotSupportedException(scoped ref ReadStack state, in Utf8JsonReader reader, NotSupportedException ex)
	{
		string text = ex.Message;
		Type type = state.Current.JsonPropertyInfo?.PropertyType ?? state.Current.JsonTypeInfo.Type;
		if (!text.Contains(type.ToString()))
		{
			if (text.Length > 0)
			{
				text += " ";
			}
			text += System.SR.Format(System.SR.SerializationNotSupportedParentType, type);
		}
		long lineNumber = reader.CurrentState._lineNumber;
		long bytePositionInLine = reader.CurrentState._bytePositionInLine;
		text += $" Path: {state.JsonPath()} | LineNumber: {lineNumber} | BytePositionInLine: {bytePositionInLine}.";
		throw new NotSupportedException(text, ex);
	}

	[DoesNotReturn]
	public static void ThrowNotSupportedException(ref WriteStack state, NotSupportedException ex)
	{
		string text = ex.Message;
		Type type = state.Current.JsonPropertyInfo?.PropertyType ?? state.Current.JsonTypeInfo.Type;
		if (!text.Contains(type.ToString()))
		{
			if (text.Length > 0)
			{
				text += " ";
			}
			text += System.SR.Format(System.SR.SerializationNotSupportedParentType, type);
		}
		text = text + " Path: " + state.PropertyPath() + ".";
		throw new NotSupportedException(text, ex);
	}

	[DoesNotReturn]
	public static void ThrowNotSupportedException_DeserializeNoConstructor(Type type, ref Utf8JsonReader reader, scoped ref ReadStack state)
	{
		string message = ((!type.IsInterface) ? System.SR.Format(System.SR.DeserializeNoConstructor, "JsonConstructorAttribute", type) : System.SR.Format(System.SR.DeserializePolymorphicInterface, type));
		ThrowNotSupportedException(ref state, in reader, new NotSupportedException(message));
	}

	[DoesNotReturn]
	public static void ThrowNotSupportedException_CannotPopulateCollection(Type type, ref Utf8JsonReader reader, scoped ref ReadStack state)
	{
		ThrowNotSupportedException(ref state, in reader, new NotSupportedException(System.SR.Format(System.SR.CannotPopulateCollection, type)));
	}

	[DoesNotReturn]
	public static void ThrowJsonException_MetadataValuesInvalidToken(JsonTokenType tokenType)
	{
		ThrowJsonException(System.SR.Format(System.SR.MetadataInvalidTokenAfterValues, tokenType));
	}

	[DoesNotReturn]
	public static void ThrowJsonException_MetadataReferenceNotFound(string id)
	{
		ThrowJsonException(System.SR.Format(System.SR.MetadataReferenceNotFound, id));
	}

	[DoesNotReturn]
	public static void ThrowJsonException_MetadataValueWasNotString(JsonTokenType tokenType)
	{
		ThrowJsonException(System.SR.Format(System.SR.MetadataValueWasNotString, tokenType));
	}

	[DoesNotReturn]
	public static void ThrowJsonException_MetadataValueWasNotString(JsonValueKind valueKind)
	{
		ThrowJsonException(System.SR.Format(System.SR.MetadataValueWasNotString, valueKind));
	}

	[DoesNotReturn]
	public static void ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties(ReadOnlySpan<byte> propertyName, scoped ref ReadStack state)
	{
		state.Current.JsonPropertyName = propertyName.ToArray();
		ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
	}

	[DoesNotReturn]
	public static void ThrowJsonException_MetadataUnexpectedProperty(ReadOnlySpan<byte> propertyName, scoped ref ReadStack state)
	{
		state.Current.JsonPropertyName = propertyName.ToArray();
		ThrowJsonException(System.SR.Format(System.SR.MetadataUnexpectedProperty));
	}

	[DoesNotReturn]
	public static void ThrowJsonException_UnmappedJsonProperty(Type type, string unmappedPropertyName)
	{
		throw new JsonException(System.SR.Format(System.SR.UnmappedJsonProperty, unmappedPropertyName, type));
	}

	[DoesNotReturn]
	public static void ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties()
	{
		ThrowJsonException(System.SR.MetadataReferenceCannotContainOtherProperties);
	}

	[DoesNotReturn]
	public static void ThrowJsonException_MetadataIdIsNotFirstProperty(ReadOnlySpan<byte> propertyName, scoped ref ReadStack state)
	{
		state.Current.JsonPropertyName = propertyName.ToArray();
		ThrowJsonException(System.SR.MetadataIdIsNotFirstProperty);
	}

	[DoesNotReturn]
	public static void ThrowJsonException_MetadataStandaloneValuesProperty(scoped ref ReadStack state, ReadOnlySpan<byte> propertyName)
	{
		state.Current.JsonPropertyName = propertyName.ToArray();
		ThrowJsonException(System.SR.MetadataStandaloneValuesProperty);
	}

	[DoesNotReturn]
	public static void ThrowJsonException_MetadataInvalidPropertyWithLeadingDollarSign(ReadOnlySpan<byte> propertyName, scoped ref ReadStack state, in Utf8JsonReader reader)
	{
		if (state.Current.IsProcessingDictionary())
		{
			state.Current.JsonPropertyNameAsString = reader.GetString();
		}
		else
		{
			state.Current.JsonPropertyName = propertyName.ToArray();
		}
		ThrowJsonException(System.SR.MetadataInvalidPropertyWithLeadingDollarSign);
	}

	[DoesNotReturn]
	public static void ThrowJsonException_MetadataDuplicateIdFound(string id)
	{
		ThrowJsonException(System.SR.Format(System.SR.MetadataDuplicateIdFound, id));
	}

	[DoesNotReturn]
	public static void ThrowJsonException_MetadataDuplicateTypeProperty()
	{
		ThrowJsonException(System.SR.MetadataDuplicateTypeProperty);
	}

	[DoesNotReturn]
	public static void ThrowJsonException_MetadataInvalidReferenceToValueType(Type propertyType)
	{
		ThrowJsonException(System.SR.Format(System.SR.MetadataInvalidReferenceToValueType, propertyType));
	}

	[DoesNotReturn]
	public static void ThrowJsonException_MetadataInvalidPropertyInArrayMetadata(scoped ref ReadStack state, Type propertyType, in Utf8JsonReader reader)
	{
		ref ReadStackFrame current = ref state.Current;
		byte[] jsonPropertyName;
		if (!reader.HasValueSequence)
		{
			jsonPropertyName = reader.ValueSpan.ToArray();
		}
		else
		{
			ReadOnlySequence<byte> sequence = reader.ValueSequence;
			jsonPropertyName = BuffersExtensions.ToArray(in sequence);
		}
		current.JsonPropertyName = jsonPropertyName;
		string @string = reader.GetString();
		ThrowJsonException(System.SR.Format(System.SR.MetadataPreservedArrayFailed, System.SR.Format(System.SR.MetadataInvalidPropertyInArrayMetadata, @string), System.SR.Format(System.SR.DeserializeUnableToConvertValue, propertyType)));
	}

	[DoesNotReturn]
	public static void ThrowJsonException_MetadataPreservedArrayValuesNotFound(scoped ref ReadStack state, Type propertyType)
	{
		state.Current.JsonPropertyName = null;
		ThrowJsonException(System.SR.Format(System.SR.MetadataPreservedArrayFailed, System.SR.MetadataStandaloneValuesProperty, System.SR.Format(System.SR.DeserializeUnableToConvertValue, propertyType)));
	}

	[DoesNotReturn]
	public static void ThrowJsonException_MetadataCannotParsePreservedObjectIntoImmutable(Type propertyType)
	{
		ThrowJsonException(System.SR.Format(System.SR.MetadataCannotParsePreservedObjectToImmutable, propertyType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_MetadataReferenceOfTypeCannotBeAssignedToType(string referenceId, Type currentType, Type typeToConvert)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.MetadataReferenceOfTypeCannotBeAssignedToType, referenceId, currentType, typeToConvert));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_JsonPropertyInfoIsBoundToDifferentJsonTypeInfo(JsonPropertyInfo propertyInfo)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.JsonPropertyInfoBoundToDifferentParent, propertyInfo.Name, propertyInfo.ParentTypeInfo.Type.FullName));
	}

	[DoesNotReturn]
	internal static void ThrowUnexpectedMetadataException(ReadOnlySpan<byte> propertyName, ref Utf8JsonReader reader, scoped ref ReadStack state)
	{
		if (JsonSerializer.GetMetadataPropertyName(propertyName, state.Current.BaseJsonTypeInfo.PolymorphicTypeResolver) != 0)
		{
			ThrowJsonException_MetadataUnexpectedProperty(propertyName, ref state);
		}
		else
		{
			ThrowJsonException_MetadataInvalidPropertyWithLeadingDollarSign(propertyName, ref state, in reader);
		}
	}

	[DoesNotReturn]
	public static void ThrowNotSupportedException_NoMetadataForType(Type type, IJsonTypeInfoResolver resolver)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.NoMetadataForType, type, resolver?.ToString() ?? "<null>"));
	}

	public static NotSupportedException GetNotSupportedException_AmbiguousMetadataForType(Type type, Type match1, Type match2)
	{
		return new NotSupportedException(System.SR.Format(System.SR.AmbiguousMetadataForType, type, match1, match2));
	}

	[DoesNotReturn]
	public static void ThrowNotSupportedException_ConstructorContainsNullParameterNames(Type declaringType)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.ConstructorContainsNullParameterNames, declaringType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_NoMetadataForType(Type type, IJsonTypeInfoResolver resolver)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.NoMetadataForType, type, resolver?.ToString() ?? "<null>"));
	}

	public static Exception GetInvalidOperationException_NoMetadataForTypeProperties(IJsonTypeInfoResolver resolver, Type type)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.NoMetadataForTypeProperties, resolver?.ToString() ?? "<null>", type));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_NoMetadataForTypeProperties(IJsonTypeInfoResolver resolver, Type type)
	{
		throw GetInvalidOperationException_NoMetadataForTypeProperties(resolver, type);
	}

	[DoesNotReturn]
	public static void ThrowMissingMemberException_MissingFSharpCoreMember(string missingFsharpCoreMember)
	{
		throw new MissingMemberException(System.SR.Format(System.SR.MissingFSharpCoreMember, missingFsharpCoreMember));
	}

	[DoesNotReturn]
	public static void ThrowNotSupportedException_BaseConverterDoesNotSupportMetadata(Type derivedType)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.Polymorphism_DerivedConverterDoesNotSupportMetadata, derivedType));
	}

	[DoesNotReturn]
	public static void ThrowNotSupportedException_DerivedConverterDoesNotSupportMetadata(Type derivedType)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.Polymorphism_DerivedConverterDoesNotSupportMetadata, derivedType));
	}

	[DoesNotReturn]
	public static void ThrowNotSupportedException_RuntimeTypeNotSupported(Type baseType, Type runtimeType)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.Polymorphism_RuntimeTypeNotSupported, runtimeType, baseType));
	}

	[DoesNotReturn]
	public static void ThrowNotSupportedException_RuntimeTypeDiamondAmbiguity(Type baseType, Type runtimeType, Type derivedType1, Type derivedType2)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.Polymorphism_RuntimeTypeDiamondAmbiguity, runtimeType, derivedType1, derivedType2, baseType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_TypeDoesNotSupportPolymorphism(Type baseType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.Polymorphism_TypeDoesNotSupportPolymorphism, baseType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_DerivedTypeNotSupported(Type baseType, Type derivedType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.Polymorphism_DerivedTypeIsNotSupported, derivedType, baseType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_DerivedTypeIsAlreadySpecified(Type baseType, Type derivedType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.Polymorphism_DerivedTypeIsAlreadySpecified, baseType, derivedType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_TypeDicriminatorIdIsAlreadySpecified(Type baseType, object typeDiscriminator)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.Polymorphism_TypeDicriminatorIdIsAlreadySpecified, baseType, typeDiscriminator));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_InvalidCustomTypeDiscriminatorPropertyName()
	{
		throw new InvalidOperationException(System.SR.Polymorphism_InvalidCustomTypeDiscriminatorPropertyName);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_PolymorphicTypeConfigurationDoesNotSpecifyDerivedTypes(Type baseType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.Polymorphism_ConfigurationDoesNotSpecifyDerivedTypes, baseType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_InvalidEnumTypeWithSpecialChar(Type enumType, string enumName)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.InvalidEnumTypeWithSpecialChar, enumType.Name, enumName));
	}

	[DoesNotReturn]
	public static void ThrowJsonException_UnrecognizedTypeDiscriminator(object typeDiscriminator)
	{
		ThrowJsonException(System.SR.Format(System.SR.Polymorphism_UnrecognizedTypeDiscriminator, typeDiscriminator));
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_JsonPolymorphismOptionsAssociatedWithDifferentJsonTypeInfo(string parameterName)
	{
		throw new ArgumentException(System.SR.JsonPolymorphismOptionsAssociatedWithDifferentJsonTypeInfo, parameterName);
	}
}
