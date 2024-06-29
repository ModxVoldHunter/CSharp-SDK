using System.Runtime.InteropServices;
using System.Threading;

namespace System.Reflection.Internal;

internal sealed class NativeHeapMemoryBlock : AbstractMemoryBlock
{
	private sealed class DisposableData : CriticalDisposableObject
	{
		private nint _pointer;

		public unsafe byte* Pointer => (byte*)_pointer;

		public DisposableData(int size)
		{
			_pointer = Marshal.AllocHGlobal(size);
		}

		protected override void Release()
		{
			nint num = Interlocked.Exchange(ref _pointer, IntPtr.Zero);
			if (num != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(num);
			}
		}
	}

	private readonly DisposableData _data;

	private readonly int _size;

	public unsafe override byte* Pointer => _data.Pointer;

	public override int Size => _size;

	internal NativeHeapMemoryBlock(int size)
	{
		_data = new DisposableData(size);
		_size = size;
	}

	public override void Dispose()
	{
		_data.Dispose();
	}
}
