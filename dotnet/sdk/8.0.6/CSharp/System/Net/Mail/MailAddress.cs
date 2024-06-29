using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Mime;
using System.Text;

namespace System.Net.Mail;

public class MailAddress
{
	private readonly Encoding _displayNameEncoding;

	private readonly string _displayName;

	private readonly string _userName;

	private readonly string _host;

	public string DisplayName => _displayName;

	public string User => _userName;

	public string Host => _host;

	public string Address => _userName + "@" + _host;

	private string SmtpAddress => "<" + Address + ">";

	internal MailAddress(string displayName, string userName, string domain, Encoding displayNameEncoding)
	{
		_host = domain;
		_userName = userName;
		_displayName = displayName;
		_displayNameEncoding = displayNameEncoding ?? Encoding.GetEncoding("utf-8");
	}

	public MailAddress(string address)
		: this(address, null, null)
	{
	}

	public MailAddress(string address, string? displayName)
		: this(address, displayName, null)
	{
	}

	public MailAddress(string address, string? displayName, Encoding? displayNameEncoding)
	{
		(string, string, string, Encoding) parsedData;
		bool flag = TryParse(address, displayName, displayNameEncoding, out parsedData, throwExceptionIfFail: true);
		(_displayName, _userName, _host, _displayNameEncoding) = parsedData;
	}

	public static bool TryCreate([NotNullWhen(true)] string? address, [NotNullWhen(true)] out MailAddress? result)
	{
		return TryCreate(address, null, out result);
	}

	public static bool TryCreate([NotNullWhen(true)] string? address, string? displayName, [NotNullWhen(true)] out MailAddress? result)
	{
		return TryCreate(address, displayName, null, out result);
	}

	public static bool TryCreate([NotNullWhen(true)] string? address, string? displayName, Encoding? displayNameEncoding, [NotNullWhen(true)] out MailAddress? result)
	{
		if (TryParse(address, displayName, displayNameEncoding, out (string, string, string, Encoding) parsedData, throwExceptionIfFail: false))
		{
			result = new MailAddress(parsedData.Item1, parsedData.Item2, parsedData.Item3, parsedData.Item4);
			return true;
		}
		result = null;
		return false;
	}

	private static bool TryParse([NotNullWhen(true)] string address, string displayName, Encoding displayNameEncoding, out (string displayName, string user, string host, Encoding displayNameEncoding) parsedData, bool throwExceptionIfFail)
	{
		if (throwExceptionIfFail)
		{
			ArgumentException.ThrowIfNullOrEmpty(address, "address");
		}
		else if (string.IsNullOrEmpty(address))
		{
			parsedData = default((string, string, string, Encoding));
			return false;
		}
		if (displayNameEncoding == null)
		{
			displayNameEncoding = Encoding.GetEncoding("utf-8");
		}
		if (displayName == null)
		{
			displayName = string.Empty;
		}
		if (!string.IsNullOrEmpty(displayName))
		{
			if (!MailAddressParser.TryNormalizeOrThrow(displayName, out displayName, throwExceptionIfFail))
			{
				parsedData = default((string, string, string, Encoding));
				return false;
			}
			if (displayName.Length >= 2 && displayName.StartsWith('"') && displayName.EndsWith('"'))
			{
				displayName = displayName.Substring(1, displayName.Length - 2);
			}
		}
		if (!MailAddressParser.TryParseAddress(address, out var parsedAddress, throwExceptionIfFail))
		{
			parsedData = default((string, string, string, Encoding));
			return false;
		}
		if (string.IsNullOrEmpty(displayName))
		{
			displayName = parsedAddress.DisplayName;
		}
		parsedData = (displayName: displayName, user: parsedAddress.User, host: parsedAddress.Host, displayNameEncoding: displayNameEncoding);
		return true;
	}

	private string GetUser(bool allowUnicode)
	{
		if (!allowUnicode && !MimeBasePart.IsAscii(_userName, permitCROrLF: true))
		{
			throw new SmtpException(System.SR.Format(System.SR.SmtpNonAsciiUserNotSupported, Address));
		}
		return _userName;
	}

	private string GetHost(bool allowUnicode)
	{
		string text = _host;
		if (!allowUnicode && !MimeBasePart.IsAscii(text, permitCROrLF: true))
		{
			IdnMapping idnMapping = new IdnMapping();
			try
			{
				text = idnMapping.GetAscii(text);
			}
			catch (ArgumentException innerException)
			{
				throw new SmtpException(System.SR.Format(System.SR.SmtpInvalidHostName, Address), innerException);
			}
		}
		return text;
	}

	private string GetAddress(bool allowUnicode)
	{
		return GetUser(allowUnicode) + "@" + GetHost(allowUnicode);
	}

	internal string GetSmtpAddress(bool allowUnicode)
	{
		return "<" + GetAddress(allowUnicode) + ">";
	}

	public override string ToString()
	{
		if (string.IsNullOrEmpty(DisplayName))
		{
			return Address;
		}
		return "\"" + DisplayName.Replace("\"", "\\\"") + "\" " + SmtpAddress;
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value == null)
		{
			return false;
		}
		return ToString().Equals(value.ToString(), StringComparison.InvariantCultureIgnoreCase);
	}

	public override int GetHashCode()
	{
		return StringComparer.InvariantCultureIgnoreCase.GetHashCode(ToString());
	}

	internal string Encode(int charsConsumed, bool allowUnicode)
	{
		if (!string.IsNullOrEmpty(_displayName))
		{
			string text;
			if (MimeBasePart.IsAscii(_displayName, permitCROrLF: false) || allowUnicode)
			{
				text = "\"" + _displayName + "\"";
			}
			else
			{
				IEncodableStream encoderForHeader = EncodedStreamFactory.GetEncoderForHeader(_displayNameEncoding, useBase64Encoding: false, charsConsumed);
				encoderForHeader.EncodeString(_displayName, _displayNameEncoding);
				text = encoderForHeader.GetEncodedString();
			}
			return text + " " + GetSmtpAddress(allowUnicode);
		}
		return GetAddress(allowUnicode);
	}
}
