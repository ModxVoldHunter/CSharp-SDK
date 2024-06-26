namespace System.Net.Mail;

internal static class SmtpCommands
{
	internal static ReadOnlySpan<byte> Auth => "AUTH "u8;

	internal static ReadOnlySpan<byte> CRLF => "\r\n"u8;

	internal static ReadOnlySpan<byte> Data => "DATA\r\n"u8;

	internal static ReadOnlySpan<byte> DataStop => "\r\n.\r\n"u8;

	internal static ReadOnlySpan<byte> EHello => "EHLO "u8;

	internal static ReadOnlySpan<byte> Hello => "HELO "u8;

	internal static ReadOnlySpan<byte> Mail => "MAIL FROM:"u8;

	internal static ReadOnlySpan<byte> Quit => "QUIT\r\n"u8;

	internal static ReadOnlySpan<byte> Recipient => "RCPT TO:"u8;

	internal static ReadOnlySpan<byte> StartTls => "STARTTLS"u8;
}
