using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling;

[CLSCompliant(false)]
[CustomMarshaller(typeof(Span<>), MarshalMode.Default, typeof(SpanMarshaller<, >))]
[CustomMarshaller(typeof(Span<>), MarshalMode.ManagedToUnmanagedIn, typeof(SpanMarshaller<, >.ManagedToUnmanagedIn))]
[ContiguousCollectionMarshaller]
public static class SpanMarshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
{
	public ref struct ManagedToUnmanagedIn
	{
		private Span<T> _managedArray;

		private unsafe TUnmanagedElement* _allocatedMemory;

		private Span<TUnmanagedElement> _span;

		public unsafe static int BufferSize { get; } = 512 / sizeof(TUnmanagedElement);


		public unsafe void FromManaged(Span<T> managed, Span<TUnmanagedElement> buffer)
		{
			_allocatedMemory = null;
			if (Unsafe.IsNullRef(ref MemoryMarshal.GetReference(managed)))
			{
				_managedArray = null;
				_span = default(Span<TUnmanagedElement>);
				return;
			}
			_managedArray = managed;
			if (managed.Length <= buffer.Length)
			{
				_span = buffer.Slice(0, managed.Length);
				return;
			}
			int num = checked(managed.Length * sizeof(TUnmanagedElement));
			_allocatedMemory = (TUnmanagedElement*)NativeMemory.Alloc((nuint)num);
			_span = new Span<TUnmanagedElement>(_allocatedMemory, managed.Length);
		}

		public ReadOnlySpan<T> GetManagedValuesSource()
		{
			return _managedArray;
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

		public static ref T GetPinnableReference(Span<T> managed)
		{
			return ref MemoryMarshal.GetReference(managed);
		}
	}

	public unsafe static TUnmanagedElement* AllocateContainerForUnmanagedElements(Span<T> managed, out int numElements)
	{
		if (Unsafe.IsNullRef(ref MemoryMarshal.GetReference(managed)))
		{
			numElements = 0;
			return null;
		}
		numElements = managed.Length;
		int cb = Math.Max(checked(sizeof(TUnmanagedElement) * numElements), 1);
		return (TUnmanagedElement*)Marshal.AllocCoTaskMem(cb);
	}

	public static ReadOnlySpan<T> GetManagedValuesSource(Span<T> managed)
	{
		return managed;
	}

	public unsafe static Span<TUnmanagedElement> GetUnmanagedValuesDestination(TUnmanagedElement* unmanaged, int numElements)
	{
		return new Span<TUnmanagedElement>(unmanaged, numElements);
	}

	public unsafe static Span<T> AllocateContainerForManagedElements(TUnmanagedElement* unmanaged, int numElements)
	{
		if (unmanaged == null)
		{
			return null;
		}
		return new T[numElements];
	}

	public static Span<T> GetManagedValuesDestination(Span<T> managed)
	{
		return managed;
	}

	public unsafe static ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(TUnmanagedElement* unmanaged, int numElements)
	{
		return new ReadOnlySpan<TUnmanagedElement>(unmanaged, numElements);
	}

	public unsafe static void Free(TUnmanagedElement* unmanaged)
	{
		Marshal.FreeCoTaskMem((nint)unmanaged);
	}
}
