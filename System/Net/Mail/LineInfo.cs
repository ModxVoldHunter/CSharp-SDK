namespace System.Net.Mail;

internal readonly struct LineInfo
{
	internal string Line { get; }

	internal SmtpStatusCode StatusCode { get; }

	internal LineInfo(SmtpStatusCode statusCode, string line)
	{
		StatusCode = statusCode;
		Line = line;
	}
}
