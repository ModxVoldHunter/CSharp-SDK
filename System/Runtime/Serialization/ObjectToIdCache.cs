using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization;

internal sealed class ObjectToIdCache
{
	internal int m_currentCount;

	internal int[] m_ids;

	internal object[] m_objs;

	internal bool[] m_isWrapped;

	public ObjectToIdCache()
	{
		m_currentCount = 1;
		m_ids = new int[GetPrime(1)];
		m_objs = new object[m_ids.Length];
		m_isWrapped = new bool[m_ids.Length];
	}

	public int GetId(object obj, ref bool newId)
	{
		bool isEmpty;
		bool isWrapped;
		int num = FindElement(obj, out isEmpty, out isWrapped);
		if (!isEmpty)
		{
			newId = false;
			return m_ids[num];
		}
		if (!newId)
		{
			return -1;
		}
		int num2 = m_currentCount++;
		m_objs[num] = obj;
		m_ids[num] = num2;
		m_isWrapped[num] = isWrapped;
		if (m_currentCount >= m_objs.Length - 1)
		{
			Rehash();
		}
		return num2;
	}

	public int ReassignId(int oldObjId, object oldObj, object newObj)
	{
		int num = FindElement(oldObj, out var isEmpty, out var _);
		if (isEmpty)
		{
			return 0;
		}
		int num2 = m_ids[num];
		if (oldObjId > 0)
		{
			m_ids[num] = oldObjId;
		}
		else
		{
			RemoveAt(num);
		}
		num = FindElement(newObj, out isEmpty, out var isWrapped2);
		int result = 0;
		if (!isEmpty)
		{
			result = m_ids[num];
		}
		m_objs[num] = newObj;
		m_ids[num] = num2;
		m_isWrapped[num] = isWrapped2;
		return result;
	}

	private int FindElement(object obj, out bool isEmpty, out bool isWrapped)
	{
		isWrapped = false;
		int num = ComputeStartPosition(obj);
		for (int i = num; i != num - 1; i++)
		{
			if (m_objs[i] == null)
			{
				isEmpty = true;
				return i;
			}
			if (m_objs[i] == obj)
			{
				isEmpty = false;
				return i;
			}
			if (i == m_objs.Length - 1)
			{
				isWrapped = true;
				i = -1;
			}
		}
		throw XmlObjectSerializer.CreateSerializationException(System.SR.ObjectTableOverflow);
	}

	private void RemoveAt(int position)
	{
		int num = m_objs.Length;
		int num2 = position;
		for (int i = ((position != num - 1) ? (position + 1) : 0); i != position; i++)
		{
			if (m_objs[i] == null)
			{
				m_objs[num2] = null;
				m_ids[num2] = 0;
				m_isWrapped[num2] = false;
				return;
			}
			int num3 = ComputeStartPosition(m_objs[i]);
			bool flag = i < position && !m_isWrapped[i];
			bool flag2 = num2 < position;
			if ((num3 <= num2 && (!flag || flag2)) || (flag2 && !flag))
			{
				m_objs[num2] = m_objs[i];
				m_ids[num2] = m_ids[i];
				m_isWrapped[num2] = m_isWrapped[i] && i > num2;
				num2 = i;
			}
			if (i == num - 1)
			{
				i = -1;
			}
		}
		throw XmlObjectSerializer.CreateSerializationException(System.SR.ObjectTableOverflow);
	}

	private int ComputeStartPosition(object o)
	{
		return (RuntimeHelpers.GetHashCode(o) & 0x7FFFFFFF) % m_objs.Length;
	}

	private void Rehash()
	{
		int prime = GetPrime(m_objs.Length + 1);
		int[] ids = m_ids;
		object[] objs = m_objs;
		m_ids = new int[prime];
		m_objs = new object[prime];
		m_isWrapped = new bool[prime];
		for (int i = 0; i < objs.Length; i++)
		{
			object obj = objs[i];
			if (obj != null)
			{
				bool isEmpty;
				bool isWrapped;
				int num = FindElement(obj, out isEmpty, out isWrapped);
				m_objs[num] = obj;
				m_ids[num] = ids[i];
				m_isWrapped[num] = isWrapped;
			}
		}
	}

	private static int GetPrime(int min)
	{
		ReadOnlySpan<int> readOnlySpan = RuntimeHelpers.CreateSpan<int>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		ReadOnlySpan<int> readOnlySpan2 = readOnlySpan;
		for (int i = 0; i < readOnlySpan2.Length; i++)
		{
			int num = readOnlySpan2[i];
			if (num >= min)
			{
				return num;
			}
		}
		return min;
	}
}
