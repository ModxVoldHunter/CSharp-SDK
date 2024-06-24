using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System;

internal sealed class SZArrayHelper
{
	private SZArrayHelper()
	{
	}

	internal IEnumerator<T> GetEnumerator<T>()
	{
		T[] array = Unsafe.As<T[]>(this);
		int num = array.Length;
		if (num != 0)
		{
			return new SZGenericArrayEnumerator<T>(array, num);
		}
		return SZGenericArrayEnumerator<T>.Empty;
	}

	private void CopyTo<T>(T[] array, int index)
	{
		T[] array2 = Unsafe.As<T[]>(this);
		Array.Copy(array2, 0, array, index, array2.Length);
	}

	internal int get_Count<T>()
	{
		T[] array = Unsafe.As<T[]>(this);
		return array.Length;
	}

	internal T get_Item<T>(int index)
	{
		T[] array = Unsafe.As<T[]>(this);
		if ((uint)index >= (uint)array.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();
		}
		return array[index];
	}

	internal void set_Item<T>(int index, T value)
	{
		T[] array = Unsafe.As<T[]>(this);
		if ((uint)index >= (uint)array.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();
		}
		array[index] = value;
	}

	private void Add<T>(T _)
	{
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
	}

	private bool Contains<T>(T value)
	{
		T[] array = Unsafe.As<T[]>(this);
		return Array.IndexOf(array, value, 0, array.Length) >= 0;
	}

	private bool get_IsReadOnly<T>()
	{
		return true;
	}

	private void Clear<T>()
	{
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
	}

	private int IndexOf<T>(T value)
	{
		T[] array = Unsafe.As<T[]>(this);
		return Array.IndexOf(array, value, 0, array.Length);
	}

	private void Insert<T>(int _, T _1)
	{
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
	}

	private bool Remove<T>(T _)
	{
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
		return false;
	}

	private void RemoveAt<T>(int _)
	{
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
	}
}
