using System.Threading;

namespace System.Diagnostics.Tracing;

internal sealed class TraceLoggingEventHandleTable
{
	private nint[] m_innerTable;

	internal nint this[int eventID]
	{
		get
		{
			nint result = IntPtr.Zero;
			nint[] array = Volatile.Read(ref m_innerTable);
			if (eventID >= 0 && eventID < array.Length)
			{
				result = array[eventID];
			}
			return result;
		}
	}

	internal TraceLoggingEventHandleTable()
	{
		m_innerTable = new nint[10];
	}

	internal void SetEventHandle(int eventID, nint eventHandle)
	{
		if (eventID >= m_innerTable.Length)
		{
			int num = m_innerTable.Length * 2;
			if (num <= eventID)
			{
				num = eventID + 1;
			}
			nint[] array = new nint[num];
			Array.Copy(m_innerTable, array, m_innerTable.Length);
			Volatile.Write(ref m_innerTable, array);
		}
		m_innerTable[eventID] = eventHandle;
	}
}
