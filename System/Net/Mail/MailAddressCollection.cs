using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Net.Mail;

public class MailAddressCollection : Collection<MailAddress>
{
	public void Add(string addresses)
	{
		ArgumentException.ThrowIfNullOrEmpty(addresses, "addresses");
		ParseValue(addresses);
	}

	protected override void SetItem(int index, MailAddress item)
	{
		ArgumentNullException.ThrowIfNull(item, "item");
		base.SetItem(index, item);
	}

	protected override void InsertItem(int index, MailAddress item)
	{
		ArgumentNullException.ThrowIfNull(item, "item");
		base.InsertItem(index, item);
	}

	internal void ParseValue(string addresses)
	{
		IList<MailAddress> list = MailAddressParser.ParseMultipleAddresses(addresses);
		for (int i = 0; i < list.Count; i++)
		{
			Add(list[i]);
		}
	}

	public override string ToString()
	{
		return string.Join(", ", this);
	}

	internal string Encode(int charsConsumed, bool allowUnicode)
	{
		string text = string.Empty;
		using IEnumerator<MailAddress> enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			MailAddress current = enumerator.Current;
			text = ((!string.IsNullOrEmpty(text)) ? (text + ", " + current.Encode(1, allowUnicode)) : current.Encode(charsConsumed, allowUnicode));
		}
		return text;
	}
}
