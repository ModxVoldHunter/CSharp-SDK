namespace System.Text.Json;

internal static class AppContextSwitchHelper
{
	public static bool IsSourceGenReflectionFallbackEnabled { get; } = AppContext.TryGetSwitch("System.Text.Json.Serialization.EnableSourceGenReflectionFallback", out var isEnabled) && isEnabled;

}
