using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling;

[CLSCompliant(false)]
[CustomMarshaller(typeof(string), MarshalMode.Default, typeof(AnsiStringMarshaller))]
[CustomMarshaller(typeof(string), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanagedIn))]
public static class AnsiStringMarshaller
{
	public ref struct ManagedToUnmanagedIn
	{
		private unsafe byte* _unmanagedValue;

		private bool _allocated;

		public static int BufferSize => 256;

		public unsafe void FromManaged(string? managed, Span<byte> buffer)
		{
			_allocated = false;
			if (managed == null)
			{
				_unmanagedValue = null;
				return;
			}
			if ((long)Marshal.SystemMaxDBCSCharSize * (long)managed.Length >= buffer.Length)
			{
				int ansiStringByteCount = Marshal.GetAnsiStringByteCount(managed);
				if (ansiStringByteCount > buffer.Length)
				{
					buffer = new Span<byte>(NativeMemory.Alloc((nuint)ansiStringByteCount), ansiStringByteCount);
					_allocated = true;
				}
			}
			_unmanagedValue = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
			Marshal.GetAnsiStringBytes(managed, buffer);
		}

		public unsafe byte* ToUnmanaged()
		{
			return _unmanagedValue;
		}

		public unsafe void Free()
		{
			if (_allocated)
			{
				NativeMemory.Free(_unmanagedValue);
			}
		}
	}

	public unsafe static byte* ConvertToUnmanaged(string? managed)
	{
		if (managed == null)
		{
			return null;
		}
		int ansiStringByteCount = Marshal.GetAnsiStringByteCount(managed);
		byte* ptr = (byte*)Marshal.AllocCoTaskMem(ansiStringByteCount);
		Marshal.GetAnsiStringBytes(bytes: new Span<byte>(ptr, ansiStringByteCount), chars: managed);
		return ptr;
	}

	public unsafe static string? ConvertToManaged(byte* unmanaged)
	{
		return Marshal.PtrToStringAnsi((nint)unmanaged);
	}

	public unsafe static void Free(byte* unmanaged)
	{
		Marshal.FreeCoTaskMem((nint)unmanaged);
	}
}
