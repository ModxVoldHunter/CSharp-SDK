using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class DebuggerVisualizerAttribute : Attribute
{
	private Type _target;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public string? VisualizerObjectSourceTypeName { get; }

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public string VisualizerTypeName { get; }

	public string? Description { get; set; }

	public Type? Target
	{
		get
		{
			return _target;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			TargetTypeName = value.AssemblyQualifiedName;
			_target = value;
		}
	}

	public string? TargetTypeName { get; set; }

	public DebuggerVisualizerAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string visualizerTypeName)
	{
		VisualizerTypeName = visualizerTypeName;
	}

	public DebuggerVisualizerAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string visualizerTypeName, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string? visualizerObjectSourceTypeName)
	{
		VisualizerTypeName = visualizerTypeName;
		VisualizerObjectSourceTypeName = visualizerObjectSourceTypeName;
	}

	public DebuggerVisualizerAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string visualizerTypeName, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type visualizerObjectSource)
	{
		ArgumentNullException.ThrowIfNull(visualizerObjectSource, "visualizerObjectSource");
		VisualizerTypeName = visualizerTypeName;
		VisualizerObjectSourceTypeName = visualizerObjectSource.AssemblyQualifiedName;
	}

	public DebuggerVisualizerAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type visualizer)
	{
		ArgumentNullException.ThrowIfNull(visualizer, "visualizer");
		VisualizerTypeName = visualizer.AssemblyQualifiedName;
	}

	public DebuggerVisualizerAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type visualizer, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type visualizerObjectSource)
	{
		ArgumentNullException.ThrowIfNull(visualizer, "visualizer");
		ArgumentNullException.ThrowIfNull(visualizerObjectSource, "visualizerObjectSource");
		VisualizerTypeName = visualizer.AssemblyQualifiedName;
		VisualizerObjectSourceTypeName = visualizerObjectSource.AssemblyQualifiedName;
	}

	public DebuggerVisualizerAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type visualizer, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string? visualizerObjectSourceTypeName)
	{
		ArgumentNullException.ThrowIfNull(visualizer, "visualizer");
		VisualizerTypeName = visualizer.AssemblyQualifiedName;
		VisualizerObjectSourceTypeName = visualizerObjectSourceTypeName;
	}
}
