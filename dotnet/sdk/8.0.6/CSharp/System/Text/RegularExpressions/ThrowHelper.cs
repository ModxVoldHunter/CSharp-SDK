using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions;

internal static class ThrowHelper
{
	[DoesNotReturn]
	internal static void ThrowArgumentNullException(ExceptionArgument arg)
	{
		throw new ArgumentNullException(GetStringForExceptionArgument(arg));
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRangeException(ExceptionArgument arg)
	{
		throw new ArgumentOutOfRangeException(GetStringForExceptionArgument(arg));
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRangeException(ExceptionArgument arg, ExceptionResource resource)
	{
		throw new ArgumentOutOfRangeException(GetStringForExceptionArgument(arg), GetStringForExceptionResource(resource));
	}

	private static string GetStringForExceptionArgument(ExceptionArgument arg)
	{
		return arg switch
		{
			ExceptionArgument.assemblyname => "assemblyname", 
			ExceptionArgument.array => "array", 
			ExceptionArgument.arrayIndex => "arrayIndex", 
			ExceptionArgument.count => "count", 
			ExceptionArgument.evaluator => "evaluator", 
			ExceptionArgument.i => "i", 
			ExceptionArgument.inner => "inner", 
			ExceptionArgument.input => "input", 
			ExceptionArgument.length => "length", 
			ExceptionArgument.matchTimeout => "matchTimeout", 
			ExceptionArgument.name => "name", 
			ExceptionArgument.options => "options", 
			ExceptionArgument.pattern => "pattern", 
			ExceptionArgument.replacement => "replacement", 
			ExceptionArgument.startat => "startat", 
			ExceptionArgument.str => "str", 
			ExceptionArgument.value => "value", 
			_ => null, 
		};
	}

	private static string GetStringForExceptionResource(ExceptionResource resource)
	{
		return resource switch
		{
			ExceptionResource.BeginIndexNotNegative => System.SR.BeginIndexNotNegative, 
			ExceptionResource.CountTooSmall => System.SR.CountTooSmall, 
			ExceptionResource.LengthNotNegative => System.SR.LengthNotNegative, 
			_ => null, 
		};
	}
}
