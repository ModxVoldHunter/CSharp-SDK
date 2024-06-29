using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Text.RegularExpressions;

[Serializable]
public sealed class RegexParseException : ArgumentException
{
	public RegexParseError Error { get; }

	public int Offset { get; }

	internal RegexParseException(RegexParseError error, int offset, string message)
		: base(message)
	{
		Error = error;
		Offset = offset;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.SetType(typeof(ArgumentException));
	}
}
