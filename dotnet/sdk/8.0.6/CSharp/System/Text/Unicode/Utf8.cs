using System.Buffers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Unicode;

public static class Utf8
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	[InterpolatedStringHandler]
	public ref struct TryWriteInterpolatedStringHandler
	{
		private readonly Span<byte> _destination;

		private readonly IFormatProvider _provider;

		internal int _pos;

		internal bool _success;

		private readonly bool _hasCustomFormatter;

		public TryWriteInterpolatedStringHandler(int literalLength, int formattedCount, Span<byte> destination, out bool shouldAppend)
		{
			_destination = destination;
			_provider = null;
			_pos = 0;
			_success = (shouldAppend = destination.Length >= literalLength);
			_hasCustomFormatter = false;
		}

		public TryWriteInterpolatedStringHandler(int literalLength, int formattedCount, Span<byte> destination, IFormatProvider? provider, out bool shouldAppend)
		{
			_destination = destination;
			_provider = provider;
			_pos = 0;
			_success = (shouldAppend = destination.Length >= literalLength);
			_hasCustomFormatter = provider != null && DefaultInterpolatedStringHandler.HasCustomFormatter(provider);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool AppendLiteral(string value)
		{
			if (value != null)
			{
				Span<byte> span = _destination.Slice(_pos);
				int num = UTF8Encoding.UTF8EncodingSealed.ReadUtf8(ref value.GetRawStringData(), value.Length, ref MemoryMarshal.GetReference(span), span.Length);
				if (num < 0)
				{
					return Fail();
				}
				_pos += num;
			}
			return true;
		}

		public bool AppendFormatted<T>(T value)
		{
			if (_hasCustomFormatter)
			{
				return AppendCustomFormatter(value, null);
			}
			if (typeof(T).IsEnum)
			{
				return AppendEnum(value, null);
			}
			if (value is IUtf8SpanFormattable)
			{
				if (((IUtf8SpanFormattable)(object)value).TryFormat(_destination.Slice(_pos), out var bytesWritten, default(ReadOnlySpan<char>), _provider))
				{
					_pos += bytesWritten;
					return true;
				}
				return Fail();
			}
			string text;
			if (value is IFormattable)
			{
				if (value is ISpanFormattable)
				{
					return AppendSpanFormattable(value, null);
				}
				text = ((IFormattable)(object)value).ToString(null, _provider);
			}
			else
			{
				text = value?.ToString();
			}
			return AppendFormatted(text.AsSpan());
		}

		public bool AppendFormatted<T>(T value, string? format)
		{
			if (_hasCustomFormatter)
			{
				return AppendCustomFormatter(value, format);
			}
			if (typeof(T).IsEnum)
			{
				return AppendEnum(value, format);
			}
			if (value is IUtf8SpanFormattable)
			{
				if (((IUtf8SpanFormattable)(object)value).TryFormat(_destination.Slice(_pos), out var bytesWritten, format, _provider))
				{
					_pos += bytesWritten;
					return true;
				}
				return Fail();
			}
			string text;
			if (value is IFormattable)
			{
				if (value is ISpanFormattable)
				{
					return AppendSpanFormattable(value, format);
				}
				text = ((IFormattable)(object)value).ToString(format, _provider);
			}
			else
			{
				text = value?.ToString();
			}
			return AppendFormatted(text.AsSpan());
		}

		public bool AppendFormatted<T>(T value, int alignment)
		{
			int pos = _pos;
			if (AppendFormatted(value))
			{
				if (alignment != 0)
				{
					return TryAppendOrInsertAlignmentIfNeeded(pos, alignment);
				}
				return true;
			}
			return Fail();
		}

		public bool AppendFormatted<T>(T value, int alignment, string? format)
		{
			int pos = _pos;
			if (AppendFormatted(value, format))
			{
				if (alignment != 0)
				{
					return TryAppendOrInsertAlignmentIfNeeded(pos, alignment);
				}
				return true;
			}
			return Fail();
		}

		public bool AppendFormatted(scoped ReadOnlySpan<char> value)
		{
			if (Encoding.UTF8.TryGetBytes(value, _destination.Slice(_pos), out var bytesWritten))
			{
				_pos += bytesWritten;
				return true;
			}
			return Fail();
		}

		public bool AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null)
		{
			int pos = _pos;
			if (AppendFormatted(value))
			{
				if (alignment != 0)
				{
					return TryAppendOrInsertAlignmentIfNeeded(pos, alignment);
				}
				return true;
			}
			return Fail();
		}

		public bool AppendFormatted(scoped ReadOnlySpan<byte> utf8Value)
		{
			if (utf8Value.TryCopyTo(_destination.Slice(_pos)))
			{
				_pos += utf8Value.Length;
				return true;
			}
			return Fail();
		}

		public bool AppendFormatted(scoped ReadOnlySpan<byte> utf8Value, int alignment = 0, string? format = null)
		{
			int pos = _pos;
			if (AppendFormatted(utf8Value))
			{
				if (alignment != 0)
				{
					return TryAppendOrInsertAlignmentIfNeeded(pos, alignment);
				}
				return true;
			}
			return Fail();
		}

		public bool AppendFormatted(string? value)
		{
			if (!_hasCustomFormatter)
			{
				return AppendFormatted(value.AsSpan());
			}
			return AppendCustomFormatter(value, null);
		}

		public bool AppendFormatted(string? value, int alignment = 0, string? format = null)
		{
			return this.AppendFormatted<string>(value, alignment, format);
		}

		public bool AppendFormatted(object? value, int alignment = 0, string? format = null)
		{
			return this.AppendFormatted<object>(value, alignment, format);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private bool AppendCustomFormatter<T>(T value, string format)
		{
			ICustomFormatter customFormatter = (ICustomFormatter)_provider.GetFormat(typeof(ICustomFormatter));
			if (customFormatter != null)
			{
				string text = customFormatter.Format(format, value, _provider);
				if (text != null)
				{
					return AppendFormatted(text.AsSpan());
				}
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool AppendSpanFormattable<T>(T value, string format)
		{
			Span<char> destination = stackalloc char[256];
			if (!((ISpanFormattable)(object)value).TryFormat(destination, out var charsWritten2, format, _provider))
			{
				return GrowAndAppendFormatted(ref this, value, destination.Length, out charsWritten2, format);
			}
			return AppendFormatted((ReadOnlySpan<char>)destination.Slice(0, charsWritten2));
			[MethodImpl(MethodImplOptions.NoInlining)]
			static bool GrowAndAppendFormatted(scoped ref TryWriteInterpolatedStringHandler thisRef, T value, int length, out int charsWritten, string format)
			{
				while (true)
				{
					int num = length * 2;
					if ((long)(uint)num > 2147483591L)
					{
						num = ((length == 2147483591) ? (2147483591 + 1) : 2147483591);
					}
					length = num;
					char[] array = ArrayPool<char>.Shared.Rent(length);
					try
					{
						if (((ISpanFormattable)(object)value).TryFormat(array, out charsWritten, format, thisRef._provider))
						{
							return thisRef.AppendFormatted((ReadOnlySpan<char>)array.AsSpan(0, charsWritten));
						}
					}
					finally
					{
						ArrayPool<char>.Shared.Return(array);
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool AppendEnum<T>(T value, string format)
		{
			Span<char> destination = stackalloc char[256];
			if (!Enum.TryFormatUnconstrained(value, destination, out var charsWritten2, format))
			{
				return GrowAndAppendFormatted(ref this, value, destination.Length, out charsWritten2, format);
			}
			return AppendFormatted((ReadOnlySpan<char>)destination.Slice(0, charsWritten2));
			[MethodImpl(MethodImplOptions.NoInlining)]
			static bool GrowAndAppendFormatted(scoped ref TryWriteInterpolatedStringHandler thisRef, T value, int length, out int charsWritten, string format)
			{
				while (true)
				{
					int num = length * 2;
					if ((long)(uint)num > 2147483591L)
					{
						num = ((length == 2147483591) ? (2147483591 + 1) : 2147483591);
					}
					length = num;
					char[] array = ArrayPool<char>.Shared.Rent(length);
					try
					{
						if (Enum.TryFormatUnconstrained(value, array, out charsWritten, format))
						{
							return thisRef.AppendFormatted((ReadOnlySpan<char>)array.AsSpan(0, charsWritten));
						}
					}
					finally
					{
						ArrayPool<char>.Shared.Return(array);
					}
				}
			}
		}

		private bool TryAppendOrInsertAlignmentIfNeeded(int startingPos, int alignment)
		{
			int num = _pos - startingPos;
			bool flag = false;
			if (alignment < 0)
			{
				flag = true;
				alignment = -alignment;
			}
			int num2 = alignment - num;
			if (num2 <= 0)
			{
				return true;
			}
			if (num2 <= _destination.Length - _pos)
			{
				if (flag)
				{
					_destination.Slice(_pos, num2).Fill(32);
				}
				else
				{
					_destination.Slice(startingPos, num).CopyTo(_destination.Slice(startingPos + num2));
					_destination.Slice(startingPos, num2).Fill(32);
				}
				_pos += num2;
				return true;
			}
			return Fail();
		}

		private bool Fail()
		{
			_success = false;
			return false;
		}
	}

	public unsafe static OperationStatus FromUtf16(ReadOnlySpan<char> source, Span<byte> destination, out int charsRead, out int bytesWritten, bool replaceInvalidSequences = true, bool isFinalBlock = true)
	{
		_ = source.Length;
		_ = destination.Length;
		fixed (char* ptr = &MemoryMarshal.GetReference(source))
		{
			fixed (byte* ptr2 = &MemoryMarshal.GetReference(destination))
			{
				OperationStatus operationStatus = OperationStatus.Done;
				char* pInputBufferRemaining = ptr;
				byte* pOutputBufferRemaining = ptr2;
				while (!source.IsEmpty)
				{
					operationStatus = Utf8Utility.TranscodeToUtf8((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source)), source.Length, (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination)), destination.Length, out pInputBufferRemaining, out pOutputBufferRemaining);
					if (operationStatus <= OperationStatus.DestinationTooSmall || (operationStatus == OperationStatus.NeedMoreData && !isFinalBlock))
					{
						break;
					}
					if (!replaceInvalidSequences)
					{
						operationStatus = OperationStatus.InvalidData;
						break;
					}
					destination = destination.Slice((int)(pOutputBufferRemaining - (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination))));
					if (destination.Length <= 2)
					{
						operationStatus = OperationStatus.DestinationTooSmall;
						break;
					}
					destination[0] = 239;
					destination[1] = 191;
					destination[2] = 189;
					destination = destination.Slice(3);
					source = source.Slice((int)(pInputBufferRemaining - (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source))) + 1);
					operationStatus = OperationStatus.Done;
					pInputBufferRemaining = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source));
					pOutputBufferRemaining = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination));
				}
				charsRead = (int)(pInputBufferRemaining - ptr);
				bytesWritten = (int)(pOutputBufferRemaining - ptr2);
				return operationStatus;
			}
		}
	}

	public unsafe static OperationStatus ToUtf16(ReadOnlySpan<byte> source, Span<char> destination, out int bytesRead, out int charsWritten, bool replaceInvalidSequences = true, bool isFinalBlock = true)
	{
		_ = source.Length;
		_ = destination.Length;
		fixed (byte* ptr = &MemoryMarshal.GetReference(source))
		{
			fixed (char* ptr2 = &MemoryMarshal.GetReference(destination))
			{
				OperationStatus operationStatus = OperationStatus.Done;
				byte* pInputBufferRemaining = ptr;
				char* pOutputBufferRemaining = ptr2;
				while (!source.IsEmpty)
				{
					operationStatus = Utf8Utility.TranscodeToUtf16((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source)), source.Length, (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination)), destination.Length, out pInputBufferRemaining, out pOutputBufferRemaining);
					if (operationStatus <= OperationStatus.DestinationTooSmall || (operationStatus == OperationStatus.NeedMoreData && !isFinalBlock))
					{
						break;
					}
					if (!replaceInvalidSequences)
					{
						operationStatus = OperationStatus.InvalidData;
						break;
					}
					destination = destination.Slice((int)(pOutputBufferRemaining - (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination))));
					if (destination.IsEmpty)
					{
						operationStatus = OperationStatus.DestinationTooSmall;
						break;
					}
					destination[0] = '\ufffd';
					destination = destination.Slice(1);
					source = source.Slice((int)(pInputBufferRemaining - (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source))));
					Rune.DecodeFromUtf8(source, out var _, out var bytesConsumed);
					source = source.Slice(bytesConsumed);
					operationStatus = OperationStatus.Done;
					pInputBufferRemaining = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source));
					pOutputBufferRemaining = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination));
				}
				bytesRead = (int)(pInputBufferRemaining - ptr);
				charsWritten = (int)(pOutputBufferRemaining - ptr2);
				return operationStatus;
			}
		}
	}

	internal unsafe static OperationStatus ToUtf16PreservingReplacement(ReadOnlySpan<byte> source, Span<char> destination, out int bytesRead, out int charsWritten, bool replaceInvalidSequences = true, bool isFinalBlock = true)
	{
		_ = source.Length;
		_ = destination.Length;
		fixed (byte* ptr = &MemoryMarshal.GetReference(source))
		{
			fixed (char* ptr2 = &MemoryMarshal.GetReference(destination))
			{
				OperationStatus operationStatus = OperationStatus.Done;
				byte* pInputBufferRemaining = ptr;
				char* pOutputBufferRemaining = ptr2;
				while (!source.IsEmpty)
				{
					operationStatus = Utf8Utility.TranscodeToUtf16((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source)), source.Length, (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination)), destination.Length, out pInputBufferRemaining, out pOutputBufferRemaining);
					if (operationStatus <= OperationStatus.DestinationTooSmall || (operationStatus == OperationStatus.NeedMoreData && !isFinalBlock))
					{
						break;
					}
					if (!replaceInvalidSequences)
					{
						operationStatus = OperationStatus.InvalidData;
						break;
					}
					source = source.Slice((int)(pInputBufferRemaining - (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source))));
					destination = destination.Slice((int)(pOutputBufferRemaining - (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination))));
					Rune.DecodeFromUtf8(source, out var _, out var bytesConsumed);
					if (destination.Length < bytesConsumed)
					{
						operationStatus = OperationStatus.DestinationTooSmall;
						break;
					}
					for (int i = 0; i < bytesConsumed; i++)
					{
						destination[i] = (char)(0xDF00u | source[i]);
					}
					destination = destination.Slice(bytesConsumed);
					source = source.Slice(bytesConsumed);
					operationStatus = OperationStatus.Done;
					pInputBufferRemaining = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source));
					pOutputBufferRemaining = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination));
				}
				bytesRead = (int)(pInputBufferRemaining - ptr);
				charsWritten = (int)(pOutputBufferRemaining - ptr2);
				return operationStatus;
			}
		}
	}

	public static bool TryWrite(Span<byte> destination, [InterpolatedStringHandlerArgument("destination")] ref TryWriteInterpolatedStringHandler handler, out int bytesWritten)
	{
		if (handler._success)
		{
			bytesWritten = handler._pos;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public static bool TryWrite(Span<byte> destination, IFormatProvider? provider, [InterpolatedStringHandlerArgument(new string[] { "destination", "provider" })] ref TryWriteInterpolatedStringHandler handler, out int bytesWritten)
	{
		return TryWrite(destination, ref handler, out bytesWritten);
	}

	public static bool IsValid(ReadOnlySpan<byte> value)
	{
		bool isAscii;
		return Utf8Utility.GetIndexOfFirstInvalidUtf8Sequence(value, out isAscii) < 0;
	}
}
