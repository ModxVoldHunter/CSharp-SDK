using System.Collections.Specialized;
using System.Net.Mail;
using System.Text;

namespace System.Net.Mime;

internal sealed class HeaderCollection : NameValueCollection
{
	internal HeaderCollection()
		: base(StringComparer.OrdinalIgnoreCase)
	{
	}

	public override void Remove(string name)
	{
		ArgumentException.ThrowIfNullOrEmpty(name, "name");
		base.Remove(name);
	}

	public override string Get(string name)
	{
		ArgumentException.ThrowIfNullOrEmpty(name, "name");
		return base.Get(name);
	}

	public override string[] GetValues(string name)
	{
		ArgumentException.ThrowIfNullOrEmpty(name, "name");
		return base.GetValues(name);
	}

	internal void InternalRemove(string name)
	{
		base.Remove(name);
	}

	internal void InternalSet(string name, string value)
	{
		base.Set(name, value);
	}

	internal void InternalAdd(string name, string value)
	{
		if (MailHeaderInfo.IsSingleton(name))
		{
			base.Set(name, value);
		}
		else
		{
			base.Add(name, value);
		}
	}

	public override void Set(string name, string value)
	{
		ArgumentException.ThrowIfNullOrEmpty(name, "name");
		ArgumentException.ThrowIfNullOrEmpty(value, "value");
		if (!MimeBasePart.IsAscii(name, permitCROrLF: false))
		{
			throw new FormatException(System.SR.InvalidHeaderName);
		}
		name = MailHeaderInfo.NormalizeCase(name);
		value = value.Normalize(NormalizationForm.FormC);
		base.Set(name, value);
	}

	public override void Add(string name, string value)
	{
		ArgumentException.ThrowIfNullOrEmpty(name, "name");
		ArgumentException.ThrowIfNullOrEmpty(value, "value");
		MailBnfHelper.ValidateHeaderName(name);
		name = MailHeaderInfo.NormalizeCase(name);
		value = value.Normalize(NormalizationForm.FormC);
		InternalAdd(name, value);
	}
}
