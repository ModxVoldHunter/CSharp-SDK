namespace System.ComponentModel;

public abstract class InstanceCreationEditor
{
	public virtual string Text
	{
		get
		{
			if (!System.SR.UsingResourceKeys())
			{
				return System.SR.InstanceCreationEditorDefaultText;
			}
			return "(New...)";
		}
	}

	public abstract object? CreateInstance(ITypeDescriptorContext context, Type instanceType);
}
