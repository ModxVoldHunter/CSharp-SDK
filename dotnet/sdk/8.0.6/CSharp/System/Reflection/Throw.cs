using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection;

internal static class Throw
{
	[DoesNotReturn]
	internal static void InvalidCast()
	{
		throw new InvalidCastException();
	}

	[DoesNotReturn]
	internal static void InvalidArgument(string message, string parameterName)
	{
		throw new ArgumentException(message, parameterName);
	}

	[DoesNotReturn]
	internal static void InvalidArgument_OffsetForVirtualHeapHandle()
	{
		throw new ArgumentException(System.SR.CantGetOffsetForVirtualHeapHandle, "handle");
	}

	[DoesNotReturn]
	internal static Exception InvalidArgument_UnexpectedHandleKind(HandleKind kind)
	{
		throw new ArgumentException(System.SR.Format(System.SR.UnexpectedHandleKind, kind));
	}

	[DoesNotReturn]
	internal static Exception InvalidArgument_Handle(string parameterName)
	{
		throw new ArgumentException(System.SR.InvalidHandle, parameterName);
	}

	[DoesNotReturn]
	internal static void SignatureNotVarArg()
	{
		throw new InvalidOperationException(System.SR.SignatureNotVarArg);
	}

	[DoesNotReturn]
	internal static void ControlFlowBuilderNotAvailable()
	{
		throw new InvalidOperationException(System.SR.ControlFlowBuilderNotAvailable);
	}

	[DoesNotReturn]
	internal static void InvalidOperationBuilderAlreadyLinked()
	{
		throw new InvalidOperationException(System.SR.BuilderAlreadyLinked);
	}

	[DoesNotReturn]
	internal static void InvalidOperation(string message)
	{
		throw new InvalidOperationException(message);
	}

	[DoesNotReturn]
	internal static void InvalidOperation_LabelNotMarked(int id)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.LabelNotMarked, id));
	}

	[DoesNotReturn]
	internal static void LabelDoesntBelongToBuilder(string parameterName)
	{
		throw new ArgumentException(System.SR.LabelDoesntBelongToBuilder, parameterName);
	}

	[DoesNotReturn]
	internal static void HeapHandleRequired()
	{
		throw new ArgumentException(System.SR.NotMetadataHeapHandle, "handle");
	}

	[DoesNotReturn]
	internal static void EntityOrUserStringHandleRequired()
	{
		throw new ArgumentException(System.SR.NotMetadataTableOrUserStringHandle, "handle");
	}

	[DoesNotReturn]
	internal static void InvalidToken()
	{
		throw new ArgumentException(System.SR.InvalidToken, "token");
	}

	[DoesNotReturn]
	internal static void ArgumentNull(string parameterName)
	{
		throw new ArgumentNullException(parameterName);
	}

	[DoesNotReturn]
	internal static void ArgumentEmptyString(string parameterName)
	{
		throw new ArgumentException(System.SR.ExpectedNonEmptyString, parameterName);
	}

	[DoesNotReturn]
	internal static void ArgumentEmptyArray(string parameterName)
	{
		throw new ArgumentException(System.SR.ExpectedNonEmptyArray, parameterName);
	}

	[DoesNotReturn]
	internal static void ValueArgumentNull()
	{
		throw new ArgumentNullException("value");
	}

	[DoesNotReturn]
	internal static void BuilderArgumentNull()
	{
		throw new ArgumentNullException("builder");
	}

	[DoesNotReturn]
	internal static void ArgumentOutOfRange(string parameterName)
	{
		throw new ArgumentOutOfRangeException(parameterName);
	}

	[DoesNotReturn]
	internal static void ArgumentOutOfRange(string parameterName, string message)
	{
		throw new ArgumentOutOfRangeException(parameterName, message);
	}

	[DoesNotReturn]
	internal static void BlobTooLarge(string parameterName)
	{
		throw new ArgumentOutOfRangeException(parameterName, System.SR.BlobTooLarge);
	}

	[DoesNotReturn]
	internal static void IndexOutOfRange()
	{
		throw new ArgumentOutOfRangeException("index");
	}

	[DoesNotReturn]
	internal static void TableIndexOutOfRange()
	{
		throw new ArgumentOutOfRangeException("tableIndex");
	}

	[DoesNotReturn]
	internal static void ValueArgumentOutOfRange()
	{
		throw new ArgumentOutOfRangeException("value");
	}

	[DoesNotReturn]
	internal static void OutOfBounds()
	{
		throw new BadImageFormatException(System.SR.OutOfBoundsRead);
	}

	[DoesNotReturn]
	internal static void WriteOutOfBounds()
	{
		throw new InvalidOperationException(System.SR.OutOfBoundsWrite);
	}

	[DoesNotReturn]
	internal static void InvalidCodedIndex()
	{
		throw new BadImageFormatException(System.SR.InvalidCodedIndex);
	}

	[DoesNotReturn]
	internal static void InvalidHandle()
	{
		throw new BadImageFormatException(System.SR.InvalidHandle);
	}

	[DoesNotReturn]
	internal static void InvalidCompressedInteger()
	{
		throw new BadImageFormatException(System.SR.InvalidCompressedInteger);
	}

	[DoesNotReturn]
	internal static void InvalidSerializedString()
	{
		throw new BadImageFormatException(System.SR.InvalidSerializedString);
	}

	[DoesNotReturn]
	internal static void ImageTooSmall()
	{
		throw new BadImageFormatException(System.SR.ImageTooSmall);
	}

	[DoesNotReturn]
	internal static void ImageTooSmallOrContainsInvalidOffsetOrCount()
	{
		throw new BadImageFormatException(System.SR.ImageTooSmallOrContainsInvalidOffsetOrCount);
	}

	[DoesNotReturn]
	internal static void ReferenceOverflow()
	{
		throw new BadImageFormatException(System.SR.RowIdOrHeapOffsetTooLarge);
	}

	[DoesNotReturn]
	internal static void TableNotSorted(TableIndex tableIndex)
	{
		throw new BadImageFormatException(System.SR.Format(System.SR.MetadataTableNotSorted, tableIndex));
	}

	[DoesNotReturn]
	internal static void InvalidOperation_TableNotSorted(TableIndex tableIndex)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.MetadataTableNotSorted, tableIndex));
	}

	[DoesNotReturn]
	internal static void InvalidOperation_PEImageNotAvailable()
	{
		throw new InvalidOperationException(System.SR.PEImageNotAvailable);
	}

	[DoesNotReturn]
	internal static void TooManySubnamespaces()
	{
		throw new BadImageFormatException(System.SR.TooManySubnamespaces);
	}

	[DoesNotReturn]
	internal static void ValueOverflow()
	{
		throw new BadImageFormatException(System.SR.ValueTooLarge);
	}

	[DoesNotReturn]
	internal static void SequencePointValueOutOfRange()
	{
		throw new BadImageFormatException(System.SR.SequencePointValueOutOfRange);
	}

	[DoesNotReturn]
	internal static void HeapSizeLimitExceeded(HeapIndex heap)
	{
		throw new ImageFormatLimitationException(System.SR.Format(System.SR.HeapSizeLimitExceeded, heap));
	}

	[DoesNotReturn]
	internal static void PEReaderDisposed()
	{
		throw new ObjectDisposedException("PEReader");
	}
}
