using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling;

[CLSCompliant(false)]
[CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder*[]), MarshalMode.Default, typeof(PointerArrayMarshaller<, >))]
[CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder*[]), MarshalMode.ManagedToUnmanagedIn, typeof(PointerArrayMarshaller<, >.ManagedToUnmanagedIn))]
[ContiguousCollectionMarshaller]
public unsafe static class PointerArrayMarshaller<T, TUnmanagedElement> where T : unmanaged where TUnmanagedElement : unmanaged
{
	public ref struct ManagedToUnmanagedIn
	{
		private unsafe T*[] _managedArray;

		private unsafe TUnmanagedElement* _allocatedMemory;

		private Span<TUnmanagedElement> _span;

		public unsafe static int BufferSize => 512 / sizeof(TUnmanagedElement);

		public unsafe void FromManaged(T*[]? array, Span<TUnmanagedElement> buffer)
		{
			_allocatedMemory = null;
			if (array == null)
			{
				_managedArray = null;
				_span = default(Span<TUnmanagedElement>);
				return;
			}
			_managedArray = array;
			if (array.Length <= buffer.Length)
			{
				_span = buffer.Slice(0, array.Length);
				return;
			}
			int val = checked(array.Length * sizeof(TUnmanagedElement));
			int num = Math.Max(val, 1);
			_allocatedMemory = (TUnmanagedElement*)NativeMemory.Alloc((nuint)num);
			_span = new Span<TUnmanagedElement>(_allocatedMemory, array.Length);
		}

		public unsafe ReadOnlySpan<nint> GetManagedValuesSource()
		{
			return Unsafe.As<nint[]>(_managedArray);
		}

		public Span<TUnmanagedElement> GetUnmanagedValuesDestination()
		{
			return _span;
		}

		public ref TUnmanagedElement GetPinnableReference()
		{
			return ref MemoryMarshal.GetReference(_span);
		}

		public unsafe TUnmanagedElement* ToUnmanaged()
		{
			return (TUnmanagedElement*)Unsafe.AsPointer(ref GetPinnableReference());
		}

		public unsafe void Free()
		{
			NativeMemory.Free(_allocatedMemory);
		}

		public unsafe static ref byte GetPinnableReference(T*[]? array)
		{
			if (array == null)
			{
				return ref Unsafe.NullRef<byte>();
			}
			return ref MemoryMarshal.GetArrayDataReference((Array)array);
		}
	}

	public unsafe static TUnmanagedElement* AllocateContainerForUnmanagedElements(T*[]? managed, out int numElements)
	{
		if (managed == null)
		{
			numElements = 0;
			return null;
		}
		numElements = managed.Length;
		int cb = Math.Max(checked(sizeof(TUnmanagedElement) * numElements), 1);
		return (TUnmanagedElement*)Marshal.AllocCoTaskMem(cb);
	}

	public unsafe static ReadOnlySpan<nint> GetManagedValuesSource(T*[]? managed)
	{
		return Unsafe.As<nint[]>(managed);
	}

	public unsafe static Span<TUnmanagedElement> GetUnmanagedValuesDestination(TUnmanagedElement* unmanaged, int numElements)
	{
		return new Span<TUnmanagedElement>(unmanaged, numElements);
	}

	public unsafe static T*[]? AllocateContainerForManagedElements(TUnmanagedElement* unmanaged, int numElements)
	{
		if (unmanaged == null)
		{
			return null;
		}
		return new T*[numElements];
	}

	public unsafe static Span<nint> GetManagedValuesDestination(T*[]? managed)
	{
		return Unsafe.As<nint[]>(managed);
	}

	public unsafe static ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(TUnmanagedElement* unmanagedValue, int numElements)
	{
		return new ReadOnlySpan<TUnmanagedElement>(unmanagedValue, numElements);
	}

	public unsafe static void Free(TUnmanagedElement* unmanaged)
	{
		Marshal.FreeCoTaskMem((nint)unmanaged);
	}
}
