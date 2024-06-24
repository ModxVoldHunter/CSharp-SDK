namespace System.Xml.Serialization;

internal sealed class CodeGeneratorConversionException : Exception
{
	private readonly Type _sourceType;

	private readonly Type _targetType;

	private readonly bool _isAddress;

	private readonly string _reason;

	public CodeGeneratorConversionException(Type sourceType, Type targetType, bool isAddress, string reason)
		: base(System.SR.Format(System.SR.CodeGenConvertError, reason, sourceType.ToString(), targetType.ToString()))
	{
		_sourceType = sourceType;
		_targetType = targetType;
		_isAddress = isAddress;
		_reason = reason;
	}
}
