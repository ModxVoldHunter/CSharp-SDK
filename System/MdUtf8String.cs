using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System;

internal readonly struct MdUtf8String
{
	private unsafe readonly byte* m_pStringHeap;

	private readonly int m_StringHeapByteLength;

	[LibraryImport("QCall", EntryPoint = "MdUtf8String_EqualsCaseInsensitive")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private unsafe static bool EqualsCaseInsensitive(void* szLhs, void* szRhs, int cSz)
	{
		int num = __PInvoke(szLhs, szRhs, cSz);
		return num != 0;
		[DllImport("QCall", EntryPoint = "MdUtf8String_EqualsCaseInsensitive", ExactSpelling = true)]
		static extern unsafe int __PInvoke(void* __szLhs_native, void* __szRhs_native, int __cSz_native);
	}

	internal unsafe MdUtf8String(void* pStringHeap)
	{
		if (pStringHeap != null)
		{
			m_StringHeapByteLength = string.strlen((byte*)pStringHeap);
		}
		else
		{
			m_StringHeapByteLength = 0;
		}
		m_pStringHeap = (byte*)pStringHeap;
	}

	internal unsafe MdUtf8String(byte* pUtf8String, int cUtf8String)
	{
		m_pStringHeap = pUtf8String;
		m_StringHeapByteLength = cUtf8String;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe bool Equals(MdUtf8String s)
	{
		if (s.m_StringHeapByteLength != m_StringHeapByteLength)
		{
			return false;
		}
		return SpanHelpers.SequenceEqual(ref *s.m_pStringHeap, ref *m_pStringHeap, (uint)m_StringHeapByteLength);
	}

	internal unsafe bool EqualsCaseInsensitive(MdUtf8String s)
	{
		if (s.m_StringHeapByteLength != m_StringHeapByteLength)
		{
			return false;
		}
		if (m_StringHeapByteLength != 0)
		{
			return EqualsCaseInsensitive(s.m_pStringHeap, m_pStringHeap, m_StringHeapByteLength);
		}
		return true;
	}

	public unsafe override string ToString()
	{
		return Encoding.UTF8.GetString(new ReadOnlySpan<byte>(m_pStringHeap, m_StringHeapByteLength));
	}
}
