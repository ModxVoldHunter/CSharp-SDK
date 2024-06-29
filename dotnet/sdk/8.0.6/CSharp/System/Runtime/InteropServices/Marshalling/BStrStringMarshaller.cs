using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling;

[CLSCompliant(false)]
[CustomMarshaller(typeof(string), MarshalMode.Default, typeof(BStrStringMarshaller))]
[CustomMarshaller(typeof(string), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanagedIn))]
public static class BStrStringMarshaller
{
	public ref struct ManagedToUnmanagedIn
	{
		private unsafe ushort* _ptrToFirstChar;

		private bool _allocated;

		public static int BufferSize => 256;

		public unsafe void FromManaged(string? managed, Span<byte> buffer)
		{
			_allocated = false;
			if (managed == null)
			{
				_ptrToFirstChar = null;
				return;
			}
			int num;
			int num2;
			checked
			{
				num = 2 * managed.Length;
				num2 = num + 6;
			}
			ushort* ptr;
			if (num2 > buffer.Length)
			{
				ptr = (ushort*)Marshal.AllocBSTRByteLen((uint)num);
				_allocated = true;
			}
			else
			{
				byte* ptr2 = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
				*(int*)ptr2 = num;
				ptr = (ushort*)(ptr2 + 4);
			}
			managed.CopyTo(new Span<char>(ptr, managed.Length));
			ptr[managed.Length] = 0;
			_ptrToFirstChar = ptr;
		}

		public unsafe ushort* ToUnmanaged()
		{
			return _ptrToFirstChar;
		}

		public unsafe void Free()
		{
			if (_allocated)
			{
				BStrStringMarshaller.Free(_ptrToFirstChar);
			}
		}
	}

	public unsafe static ushort* ConvertToUnmanaged(string? managed)
	{
		return (ushort*)Marshal.StringToBSTR(managed);
	}

	public unsafe static string? ConvertToManaged(ushort* unmanaged)
	{
		if (unmanaged == null)
		{
			return null;
		}
		return Marshal.PtrToStringBSTR((nint)unmanaged);
	}

	public unsafe static void Free(ushort* unmanaged)
	{
		Marshal.FreeBSTR((nint)unmanaged);
	}
}
