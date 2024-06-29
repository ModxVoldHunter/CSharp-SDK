using System.Runtime.CompilerServices;
using System.Text;

namespace System.Runtime.InteropServices.Marshalling;

[CLSCompliant(false)]
[CustomMarshaller(typeof(string), MarshalMode.Default, typeof(Utf8StringMarshaller))]
[CustomMarshaller(typeof(string), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanagedIn))]
public static class Utf8StringMarshaller
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
			if (3L * (long)managed.Length >= buffer.Length)
			{
				int num = checked(Encoding.UTF8.GetByteCount(managed) + 1);
				if (num > buffer.Length)
				{
					buffer = new Span<byte>(NativeMemory.Alloc((nuint)num), num);
					_allocated = true;
				}
			}
			_unmanagedValue = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
			int bytes = Encoding.UTF8.GetBytes(managed, buffer);
			buffer[bytes] = 0;
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
		int num = checked(Encoding.UTF8.GetByteCount(managed) + 1);
		byte* ptr = (byte*)Marshal.AllocCoTaskMem(num);
		Span<byte> bytes = new Span<byte>(ptr, num);
		int bytes2 = Encoding.UTF8.GetBytes(managed, bytes);
		bytes[bytes2] = 0;
		return ptr;
	}

	public unsafe static string? ConvertToManaged(byte* unmanaged)
	{
		return Marshal.PtrToStringUTF8((nint)unmanaged);
	}

	public unsafe static void Free(byte* unmanaged)
	{
		Marshal.FreeCoTaskMem((nint)unmanaged);
	}
}
