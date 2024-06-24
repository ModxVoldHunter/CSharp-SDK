namespace System.Reflection;

[Flags]
internal enum InvocationFlags : uint
{
	Unknown = 0u,
	Initialized = 1u,
	NoInvoke = 2u,
	RunClassConstructor = 4u,
	NoConstructorInvoke = 8u,
	IsConstructor = 0x10u,
	IsDelegateConstructor = 0x80u,
	ContainsStackPointers = 0x100u,
	SpecialField = 0x10u,
	FieldSpecialCast = 0x20u
}
