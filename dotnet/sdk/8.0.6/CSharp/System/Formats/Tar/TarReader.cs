using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar;

public sealed class TarReader : IDisposable, IAsyncDisposable
{
	private bool _isDisposed;

	private readonly bool _leaveOpen;

	private TarEntry _previouslyReadEntry;

	private List<Stream> _dataStreamsToDispose;

	private bool _reachedEndMarkers;

	internal Stream _archiveStream;

	public TarReader(Stream archiveStream, bool leaveOpen = false)
	{
		ArgumentNullException.ThrowIfNull(archiveStream, "archiveStream");
		if (!archiveStream.CanRead)
		{
			throw new ArgumentException(System.SR.IO_NotSupported_UnreadableStream, "archiveStream");
		}
		_archiveStream = archiveStream;
		_leaveOpen = leaveOpen;
		_previouslyReadEntry = null;
		_isDisposed = false;
		_reachedEndMarkers = false;
	}

	public void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}
		_isDisposed = true;
		if (_leaveOpen)
		{
			return;
		}
		List<Stream> dataStreamsToDispose = _dataStreamsToDispose;
		if (dataStreamsToDispose != null && dataStreamsToDispose.Count > 0)
		{
			foreach (Stream item in _dataStreamsToDispose)
			{
				item.Dispose();
			}
		}
		_archiveStream.Dispose();
	}

	public async ValueTask DisposeAsync()
	{
		if (_isDisposed)
		{
			return;
		}
		_isDisposed = true;
		if (_leaveOpen)
		{
			return;
		}
		List<Stream> dataStreamsToDispose = _dataStreamsToDispose;
		if (dataStreamsToDispose != null && dataStreamsToDispose.Count > 0)
		{
			foreach (Stream item in _dataStreamsToDispose)
			{
				await item.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		await _archiveStream.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
	}

	public TarEntry? GetNextEntry(bool copyData = false)
	{
		if (_reachedEndMarkers)
		{
			return null;
		}
		if (_archiveStream.CanSeek && _archiveStream.Length == 0L)
		{
			return null;
		}
		AdvanceDataStreamIfNeeded();
		TarHeader tarHeader = TryGetNextEntryHeader(copyData);
		if (tarHeader != null)
		{
			TarEntry tarEntry = tarHeader._format switch
			{
				TarEntryFormat.Pax => (tarHeader._typeFlag == TarEntryType.GlobalExtendedAttributes) ? ((PosixTarEntry)new PaxGlobalExtendedAttributesTarEntry(tarHeader, this)) : ((PosixTarEntry)new PaxTarEntry(tarHeader, this)), 
				TarEntryFormat.Gnu => new GnuTarEntry(tarHeader, this), 
				TarEntryFormat.Ustar => new UstarTarEntry(tarHeader, this), 
				_ => new V7TarEntry(tarHeader, this), 
			};
			if (_archiveStream.CanSeek && _archiveStream.Length == _archiveStream.Position)
			{
				_reachedEndMarkers = true;
			}
			_previouslyReadEntry = tarEntry;
			PreserveDataStreamForDisposalIfNeeded(tarEntry);
			return tarEntry;
		}
		_reachedEndMarkers = true;
		return null;
	}

	public ValueTask<TarEntry?> GetNextEntryAsync(bool copyData = false, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<TarEntry>(cancellationToken);
		}
		if (_reachedEndMarkers)
		{
			return ValueTask.FromResult<TarEntry>(null);
		}
		if (_archiveStream.CanSeek && _archiveStream.Length == 0L)
		{
			return ValueTask.FromResult<TarEntry>(null);
		}
		return GetNextEntryInternalAsync(copyData, cancellationToken);
	}

	internal void AdvanceDataStreamIfNeeded()
	{
		if (_previouslyReadEntry == null)
		{
			return;
		}
		if (_archiveStream.CanSeek)
		{
			_archiveStream.Position = _previouslyReadEntry._header._endOfHeaderAndDataAndBlockAlignment;
		}
		else if (_previouslyReadEntry._header._size > 0 && _previouslyReadEntry._header._dataStream is SubReadStream subReadStream)
		{
			if (!subReadStream.HasReachedEnd && subReadStream.Position < _previouslyReadEntry._header._size - 1)
			{
				long bytesToDiscard = _previouslyReadEntry._header._size - subReadStream.Position;
				TarHelpers.AdvanceStream(_archiveStream, bytesToDiscard);
				subReadStream.HasReachedEnd = true;
			}
			TarHelpers.SkipBlockAlignmentPadding(_archiveStream, _previouslyReadEntry._header._size);
		}
	}

	internal async ValueTask AdvanceDataStreamIfNeededAsync(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (_previouslyReadEntry == null)
		{
			return;
		}
		if (_archiveStream.CanSeek)
		{
			_archiveStream.Position = _previouslyReadEntry._header._endOfHeaderAndDataAndBlockAlignment;
		}
		else
		{
			if (_previouslyReadEntry._header._size <= 0)
			{
				return;
			}
			Stream dataStream2 = _previouslyReadEntry._header._dataStream;
			if (dataStream2 is SubReadStream dataStream)
			{
				if (!dataStream.HasReachedEnd && dataStream.Position < _previouslyReadEntry._header._size - 1)
				{
					long bytesToDiscard = _previouslyReadEntry._header._size - dataStream.Position;
					await TarHelpers.AdvanceStreamAsync(_archiveStream, bytesToDiscard, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					dataStream.HasReachedEnd = true;
				}
				await TarHelpers.SkipBlockAlignmentPaddingAsync(_archiveStream, _previouslyReadEntry._header._size, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}

	private async ValueTask<TarEntry> GetNextEntryInternalAsync(bool copyData, CancellationToken cancellationToken)
	{
		await AdvanceDataStreamIfNeededAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		TarHeader tarHeader = await TryGetNextEntryHeaderAsync(copyData, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (tarHeader != null)
		{
			TarEntry tarEntry = tarHeader._format switch
			{
				TarEntryFormat.Pax => (tarHeader._typeFlag == TarEntryType.GlobalExtendedAttributes) ? ((PosixTarEntry)new PaxGlobalExtendedAttributesTarEntry(tarHeader, this)) : ((PosixTarEntry)new PaxTarEntry(tarHeader, this)), 
				TarEntryFormat.Gnu => new GnuTarEntry(tarHeader, this), 
				TarEntryFormat.Ustar => new UstarTarEntry(tarHeader, this), 
				_ => new V7TarEntry(tarHeader, this), 
			};
			if (_archiveStream.CanSeek && _archiveStream.Length == _archiveStream.Position)
			{
				_reachedEndMarkers = true;
			}
			_previouslyReadEntry = tarEntry;
			PreserveDataStreamForDisposalIfNeeded(tarEntry);
			return tarEntry;
		}
		_reachedEndMarkers = true;
		return null;
	}

	private TarHeader TryGetNextEntryHeader(bool copyData)
	{
		TarHeader tarHeader = TarHeader.TryGetNextHeader(_archiveStream, copyData, TarEntryFormat.Unknown, processDataBlock: true);
		if (tarHeader == null)
		{
			return null;
		}
		if (tarHeader._typeFlag == TarEntryType.ExtendedAttributes)
		{
			if (!TryProcessExtendedAttributesHeader(tarHeader, copyData, out var actualHeader))
			{
				return null;
			}
			tarHeader = actualHeader;
		}
		else
		{
			TarEntryType typeFlag = tarHeader._typeFlag;
			if (typeFlag - 75 <= (TarEntryType)1)
			{
				if (!TryProcessGnuMetadataHeader(tarHeader, copyData, out var finalHeader))
				{
					return null;
				}
				tarHeader = finalHeader;
			}
		}
		return tarHeader;
	}

	private async ValueTask<TarHeader> TryGetNextEntryHeaderAsync(bool copyData, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		TarHeader tarHeader = await TarHeader.TryGetNextHeaderAsync(_archiveStream, copyData, TarEntryFormat.Unknown, processDataBlock: true, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (tarHeader == null)
		{
			return null;
		}
		if (tarHeader._typeFlag == TarEntryType.ExtendedAttributes)
		{
			TarHeader tarHeader2 = await TryProcessExtendedAttributesHeaderAsync(tarHeader, copyData, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (tarHeader2 == null)
			{
				return null;
			}
			tarHeader = tarHeader2;
		}
		else
		{
			TarEntryType typeFlag = tarHeader._typeFlag;
			if (typeFlag - 75 <= (TarEntryType)1)
			{
				TarHeader tarHeader3 = await TryProcessGnuMetadataHeaderAsync(tarHeader, copyData, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (tarHeader3 == null)
				{
					return null;
				}
				tarHeader = tarHeader3;
			}
		}
		return tarHeader;
	}

	private bool TryProcessExtendedAttributesHeader(TarHeader extendedAttributesHeader, bool copyData, [NotNullWhen(true)] out TarHeader actualHeader)
	{
		actualHeader = TarHeader.TryGetNextHeader(_archiveStream, copyData, TarEntryFormat.Pax, processDataBlock: false);
		if (actualHeader == null)
		{
			return false;
		}
		TarEntryType typeFlag = actualHeader._typeFlag;
		if ((typeFlag - 75 <= (TarEntryType)1 || typeFlag == TarEntryType.GlobalExtendedAttributes || typeFlag == TarEntryType.ExtendedAttributes) ? true : false)
		{
			throw new InvalidDataException(System.SR.Format(System.SR.TarUnexpectedMetadataEntry, actualHeader._typeFlag, TarEntryType.ExtendedAttributes));
		}
		actualHeader.ReplaceNormalAttributesWithExtended(extendedAttributesHeader.ExtendedAttributes);
		actualHeader.ProcessDataBlock(_archiveStream, copyData);
		return true;
	}

	private async ValueTask<TarHeader> TryProcessExtendedAttributesHeaderAsync(TarHeader extendedAttributesHeader, bool copyData, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		TarHeader tarHeader = await TarHeader.TryGetNextHeaderAsync(_archiveStream, copyData, TarEntryFormat.Pax, processDataBlock: false, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (tarHeader == null)
		{
			return null;
		}
		TarEntryType typeFlag = tarHeader._typeFlag;
		if ((typeFlag - 75 <= (TarEntryType)1 || typeFlag == TarEntryType.GlobalExtendedAttributes || typeFlag == TarEntryType.ExtendedAttributes) ? true : false)
		{
			throw new InvalidDataException(System.SR.Format(System.SR.TarUnexpectedMetadataEntry, tarHeader._typeFlag, TarEntryType.ExtendedAttributes));
		}
		if (tarHeader._typeFlag == TarEntryType.ExtendedAttributes)
		{
			throw new InvalidDataException(System.SR.Format(System.SR.TarUnexpectedMetadataEntry, TarEntryType.ExtendedAttributes, TarEntryType.ExtendedAttributes));
		}
		tarHeader.ReplaceNormalAttributesWithExtended(extendedAttributesHeader.ExtendedAttributes);
		tarHeader.ProcessDataBlock(_archiveStream, copyData);
		return tarHeader;
	}

	private bool TryProcessGnuMetadataHeader(TarHeader header, bool copyData, out TarHeader finalHeader)
	{
		finalHeader = new TarHeader(TarEntryFormat.Gnu);
		TarHeader tarHeader = TarHeader.TryGetNextHeader(_archiveStream, copyData, TarEntryFormat.Gnu, processDataBlock: true);
		if (tarHeader == null)
		{
			return false;
		}
		if (tarHeader._typeFlag == header._typeFlag)
		{
			throw new InvalidDataException(System.SR.Format(System.SR.TarUnexpectedMetadataEntry, tarHeader._typeFlag, header._typeFlag));
		}
		if ((header._typeFlag == TarEntryType.LongLink && tarHeader._typeFlag == TarEntryType.LongPath) || (header._typeFlag == TarEntryType.LongPath && tarHeader._typeFlag == TarEntryType.LongLink))
		{
			TarHeader tarHeader2 = TarHeader.TryGetNextHeader(_archiveStream, copyData, TarEntryFormat.Gnu, processDataBlock: true);
			if (tarHeader2 == null)
			{
				return false;
			}
			TarEntryType typeFlag = tarHeader2._typeFlag;
			if (typeFlag - 75 <= (TarEntryType)1)
			{
				throw new InvalidDataException(System.SR.Format(System.SR.TarUnexpectedMetadataEntry, tarHeader2._typeFlag, tarHeader._typeFlag));
			}
			if (header._typeFlag == TarEntryType.LongLink)
			{
				tarHeader2._linkName = header._linkName;
				tarHeader2._name = tarHeader._name;
			}
			else if (header._typeFlag == TarEntryType.LongPath)
			{
				tarHeader2._name = header._name;
				tarHeader2._linkName = tarHeader._linkName;
			}
			finalHeader = tarHeader2;
		}
		else
		{
			if (header._typeFlag == TarEntryType.LongLink)
			{
				tarHeader._linkName = header._linkName;
			}
			else if (header._typeFlag == TarEntryType.LongPath)
			{
				tarHeader._name = header._name;
			}
			finalHeader = tarHeader;
		}
		return true;
	}

	private async ValueTask<TarHeader> TryProcessGnuMetadataHeaderAsync(TarHeader header, bool copyData, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		TarHeader secondHeader = await TarHeader.TryGetNextHeaderAsync(_archiveStream, copyData, TarEntryFormat.Gnu, processDataBlock: true, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (secondHeader == null)
		{
			return null;
		}
		if (secondHeader._typeFlag == header._typeFlag)
		{
			throw new InvalidDataException(System.SR.Format(System.SR.TarUnexpectedMetadataEntry, secondHeader._typeFlag, header._typeFlag));
		}
		TarHeader result;
		if ((header._typeFlag == TarEntryType.LongLink && secondHeader._typeFlag == TarEntryType.LongPath) || (header._typeFlag == TarEntryType.LongPath && secondHeader._typeFlag == TarEntryType.LongLink))
		{
			TarHeader tarHeader = await TarHeader.TryGetNextHeaderAsync(_archiveStream, copyData, TarEntryFormat.Gnu, processDataBlock: true, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (tarHeader == null)
			{
				return null;
			}
			TarEntryType typeFlag = tarHeader._typeFlag;
			if (typeFlag - 75 <= (TarEntryType)1)
			{
				throw new InvalidDataException(System.SR.Format(System.SR.TarUnexpectedMetadataEntry, tarHeader._typeFlag, secondHeader._typeFlag));
			}
			if (header._typeFlag == TarEntryType.LongLink)
			{
				tarHeader._linkName = header._linkName;
				tarHeader._name = secondHeader._name;
			}
			else if (header._typeFlag == TarEntryType.LongPath)
			{
				tarHeader._name = header._name;
				tarHeader._linkName = secondHeader._linkName;
			}
			result = tarHeader;
		}
		else
		{
			if (header._typeFlag == TarEntryType.LongLink)
			{
				secondHeader._linkName = header._linkName;
			}
			else if (header._typeFlag == TarEntryType.LongPath)
			{
				secondHeader._name = header._name;
			}
			result = secondHeader;
		}
		return result;
	}

	private void PreserveDataStreamForDisposalIfNeeded(TarEntry entry)
	{
		if (entry._header._dataStream is SubReadStream item)
		{
			if (_dataStreamsToDispose == null)
			{
				_dataStreamsToDispose = new List<Stream>();
			}
			_dataStreamsToDispose.Add(item);
		}
	}
}
