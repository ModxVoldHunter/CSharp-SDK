using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;

namespace System.Security;

public sealed class SecureString : IDisposable
{
	private sealed class UnmanagedBuffer : SafeBuffer
	{
		private int _byteLength;

		private UnmanagedBuffer()
			: base(ownsHandle: true)
		{
		}

		public static UnmanagedBuffer Allocate(int byteLength)
		{
			UnmanagedBuffer unmanagedBuffer = new UnmanagedBuffer();
			unmanagedBuffer.SetHandle(Marshal.AllocHGlobal(byteLength));
			unmanagedBuffer.Initialize((ulong)byteLength);
			unmanagedBuffer._byteLength = byteLength;
			return unmanagedBuffer;
		}

		internal unsafe static void Copy(UnmanagedBuffer source, UnmanagedBuffer destination, ulong bytesLength)
		{
			if (bytesLength == 0L)
			{
				return;
			}
			byte* pointer = null;
			byte* pointer2 = null;
			try
			{
				source.AcquirePointer(ref pointer);
				destination.AcquirePointer(ref pointer2);
				Buffer.MemoryCopy(pointer, pointer2, destination.ByteLength, bytesLength);
			}
			finally
			{
				if (pointer2 != null)
				{
					destination.ReleasePointer();
				}
				if (pointer != null)
				{
					source.ReleasePointer();
				}
			}
		}

		protected unsafe override bool ReleaseHandle()
		{
			new Span<byte>((void*)handle, _byteLength).Clear();
			Marshal.FreeHGlobal(handle);
			return true;
		}
	}

	private readonly object _methodLock = new object();

	private UnmanagedBuffer _buffer;

	private int _decryptedLength;

	private bool _encrypted;

	private bool _readOnly;

	public int Length
	{
		get
		{
			EnsureNotDisposed();
			return Volatile.Read(ref _decryptedLength);
		}
	}

	public SecureString()
	{
		Initialize(ReadOnlySpan<char>.Empty);
	}

	[CLSCompliant(false)]
	public unsafe SecureString(char* value, int length)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		ArgumentOutOfRangeException.ThrowIfNegative(length, "length");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 65536, "length");
		Initialize(new ReadOnlySpan<char>(value, length));
	}

	private void Initialize(ReadOnlySpan<char> value)
	{
		_buffer = UnmanagedBuffer.Allocate(GetAlignedByteSize(value.Length));
		_decryptedLength = value.Length;
		SafeBuffer bufferToRelease = null;
		try
		{
			Span<char> destination = AcquireSpan(ref bufferToRelease);
			value.CopyTo(destination);
		}
		finally
		{
			ProtectMemory();
			bufferToRelease?.DangerousRelease();
		}
	}

	private SecureString(SecureString str)
	{
		_buffer = UnmanagedBuffer.Allocate((int)str._buffer.ByteLength);
		UnmanagedBuffer.Copy(str._buffer, _buffer, str._buffer.ByteLength);
		_decryptedLength = str._decryptedLength;
		_encrypted = str._encrypted;
	}

	private void EnsureCapacity(int capacity)
	{
		if (capacity > 65536)
		{
			throw new ArgumentOutOfRangeException("capacity", SR.ArgumentOutOfRange_Capacity);
		}
		if ((uint)(capacity * 2) > _buffer.ByteLength)
		{
			UnmanagedBuffer buffer = _buffer;
			UnmanagedBuffer unmanagedBuffer = UnmanagedBuffer.Allocate(GetAlignedByteSize(capacity));
			UnmanagedBuffer.Copy(buffer, unmanagedBuffer, (uint)(_decryptedLength * 2));
			_buffer = unmanagedBuffer;
			buffer.Dispose();
		}
	}

	public void AppendChar(char c)
	{
		lock (_methodLock)
		{
			EnsureNotDisposed();
			EnsureNotReadOnly();
			SafeBuffer bufferToRelease = null;
			try
			{
				UnprotectMemory();
				EnsureCapacity(_decryptedLength + 1);
				AcquireSpan(ref bufferToRelease)[_decryptedLength] = c;
				_decryptedLength++;
			}
			finally
			{
				ProtectMemory();
				bufferToRelease?.DangerousRelease();
			}
		}
	}

	public void Clear()
	{
		lock (_methodLock)
		{
			EnsureNotDisposed();
			EnsureNotReadOnly();
			_decryptedLength = 0;
			SafeBuffer bufferToRelease = null;
			try
			{
				AcquireSpan(ref bufferToRelease).Clear();
			}
			finally
			{
				bufferToRelease?.DangerousRelease();
			}
		}
	}

	public SecureString Copy()
	{
		lock (_methodLock)
		{
			EnsureNotDisposed();
			return new SecureString(this);
		}
	}

	public void Dispose()
	{
		lock (_methodLock)
		{
			if (_buffer != null)
			{
				_buffer.Dispose();
				_buffer = null;
			}
		}
	}

	public void InsertAt(int index, char c)
	{
		lock (_methodLock)
		{
			if (index < 0 || index > _decryptedLength)
			{
				throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_IndexString);
			}
			EnsureNotDisposed();
			EnsureNotReadOnly();
			SafeBuffer bufferToRelease = null;
			try
			{
				UnprotectMemory();
				EnsureCapacity(_decryptedLength + 1);
				Span<char> span = AcquireSpan(ref bufferToRelease);
				span.Slice(index, _decryptedLength - index).CopyTo(span.Slice(index + 1));
				span[index] = c;
				_decryptedLength++;
			}
			finally
			{
				ProtectMemory();
				bufferToRelease?.DangerousRelease();
			}
		}
	}

	public bool IsReadOnly()
	{
		EnsureNotDisposed();
		return Volatile.Read(ref _readOnly);
	}

	public void MakeReadOnly()
	{
		EnsureNotDisposed();
		Volatile.Write(ref _readOnly, value: true);
	}

	public void RemoveAt(int index)
	{
		lock (_methodLock)
		{
			if (index < 0 || index >= _decryptedLength)
			{
				throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_IndexString);
			}
			EnsureNotDisposed();
			EnsureNotReadOnly();
			SafeBuffer bufferToRelease = null;
			try
			{
				UnprotectMemory();
				Span<char> span = AcquireSpan(ref bufferToRelease);
				span.Slice(index + 1, _decryptedLength - (index + 1)).CopyTo(span.Slice(index));
				_decryptedLength--;
			}
			finally
			{
				ProtectMemory();
				bufferToRelease?.DangerousRelease();
			}
		}
	}

	public void SetAt(int index, char c)
	{
		lock (_methodLock)
		{
			if (index < 0 || index >= _decryptedLength)
			{
				throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_IndexString);
			}
			EnsureNotDisposed();
			EnsureNotReadOnly();
			SafeBuffer bufferToRelease = null;
			try
			{
				UnprotectMemory();
				AcquireSpan(ref bufferToRelease)[index] = c;
			}
			finally
			{
				ProtectMemory();
				bufferToRelease?.DangerousRelease();
			}
		}
	}

	private unsafe Span<char> AcquireSpan(ref SafeBuffer bufferToRelease)
	{
		SafeBuffer buffer = _buffer;
		bool success = false;
		buffer.DangerousAddRef(ref success);
		bufferToRelease = buffer;
		return new Span<char>((void*)buffer.DangerousGetHandle(), (int)(buffer.ByteLength / 2));
	}

	private void EnsureNotReadOnly()
	{
		if (_readOnly)
		{
			throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
		}
	}

	private void EnsureNotDisposed()
	{
		ObjectDisposedException.ThrowIf(_buffer == null, this);
	}

	internal unsafe nint MarshalToBSTR()
	{
		lock (_methodLock)
		{
			EnsureNotDisposed();
			UnprotectMemory();
			SafeBuffer bufferToRelease = null;
			nint num = IntPtr.Zero;
			int length = 0;
			try
			{
				Span<char> span = AcquireSpan(ref bufferToRelease);
				length = _decryptedLength;
				num = Marshal.AllocBSTR(length);
				span.Slice(0, length).CopyTo(new Span<char>((void*)num, length));
				nint result = num;
				num = IntPtr.Zero;
				return result;
			}
			finally
			{
				if (num != IntPtr.Zero)
				{
					new Span<char>((void*)num, length).Clear();
					Marshal.FreeBSTR(num);
				}
				ProtectMemory();
				bufferToRelease?.DangerousRelease();
			}
		}
	}

	internal unsafe nint MarshalToString(bool globalAlloc, bool unicode)
	{
		lock (_methodLock)
		{
			EnsureNotDisposed();
			UnprotectMemory();
			SafeBuffer bufferToRelease = null;
			nint num = IntPtr.Zero;
			int num2 = 0;
			try
			{
				Span<char> span = AcquireSpan(ref bufferToRelease).Slice(0, _decryptedLength);
				num2 = ((!unicode) ? Marshal.GetAnsiStringByteCount(span) : ((span.Length + 1) * 2));
				num = ((!globalAlloc) ? Marshal.AllocCoTaskMem(num2) : Marshal.AllocHGlobal(num2));
				if (unicode)
				{
					Span<char> destination = new Span<char>((void*)num, num2 / 2);
					span.CopyTo(destination);
					destination[destination.Length - 1] = '\0';
				}
				else
				{
					Marshal.GetAnsiStringBytes(span, new Span<byte>((void*)num, num2));
				}
				nint result = num;
				num = IntPtr.Zero;
				return result;
			}
			finally
			{
				if (num != IntPtr.Zero)
				{
					new Span<byte>((void*)num, num2).Clear();
					if (globalAlloc)
					{
						Marshal.FreeHGlobal(num);
					}
					else
					{
						Marshal.FreeCoTaskMem(num);
					}
				}
				ProtectMemory();
				bufferToRelease?.DangerousRelease();
			}
		}
	}

	private static int GetAlignedByteSize(int length)
	{
		int num = Math.Max(length, 1) * 2;
		return (num + 15) / 16 * 16;
	}

	private void ProtectMemory()
	{
		if (_decryptedLength != 0 && !_encrypted && !Interop.Crypt32.CryptProtectMemory(_buffer, (uint)_buffer.ByteLength, 0u))
		{
			throw new CryptographicException(Marshal.GetLastPInvokeError());
		}
		_encrypted = true;
	}

	private void UnprotectMemory()
	{
		if (_decryptedLength != 0 && _encrypted && !Interop.Crypt32.CryptUnprotectMemory(_buffer, (uint)_buffer.ByteLength, 0u))
		{
			throw new CryptographicException(Marshal.GetLastPInvokeError());
		}
		_encrypted = false;
	}
}
