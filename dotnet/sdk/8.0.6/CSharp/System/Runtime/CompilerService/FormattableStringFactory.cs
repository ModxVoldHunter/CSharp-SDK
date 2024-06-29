using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices;

public static class FormattableStringFactory
{
	private sealed class ConcreteFormattableString : FormattableString
	{
		private readonly string _format;

		private readonly object[] _arguments;

		public override string Format => _format;

		public override int ArgumentCount => _arguments.Length;

		internal ConcreteFormattableString(string format, object[] arguments)
		{
			_format = format;
			_arguments = arguments;
		}

		public override object[] GetArguments()
		{
			return _arguments;
		}

		public override object GetArgument(int index)
		{
			return _arguments[index];
		}

		public override string ToString(IFormatProvider formatProvider)
		{
			return string.Format(formatProvider, _format, _arguments);
		}
	}

	public static FormattableString Create([StringSyntax("CompositeFormat")] string format, params object?[] arguments)
	{
		ArgumentNullException.ThrowIfNull(format, "format");
		ArgumentNullException.ThrowIfNull(arguments, "arguments");
		return new ConcreteFormattableString(format, arguments);
	}
}
