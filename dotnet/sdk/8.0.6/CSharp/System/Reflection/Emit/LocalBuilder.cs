namespace System.Reflection.Emit;

public sealed class LocalBuilder : LocalVariableInfo
{
	private readonly int m_localIndex;

	private readonly Type m_localType;

	private readonly MethodInfo m_methodBuilder;

	private readonly bool m_isPinned;

	public override bool IsPinned => m_isPinned;

	public override Type LocalType => m_localType;

	public override int LocalIndex => m_localIndex;

	internal LocalBuilder(int localIndex, Type localType, MethodInfo methodBuilder, bool isPinned)
	{
		m_isPinned = isPinned;
		m_localIndex = localIndex;
		m_localType = localType;
		m_methodBuilder = methodBuilder;
	}

	internal int GetLocalIndex()
	{
		return m_localIndex;
	}

	internal MethodInfo GetMethodBuilder()
	{
		return m_methodBuilder;
	}
}
