using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Net.Mail;

public sealed class AlternateViewCollection : Collection<AlternateView>, IDisposable
{
	private bool _disposed;

	internal AlternateViewCollection()
	{
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		using (IEnumerator<AlternateView> enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				AlternateView current = enumerator.Current;
				current.Dispose();
			}
		}
		Clear();
		_disposed = true;
	}

	protected override void RemoveItem(int index)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		base.RemoveItem(index);
	}

	protected override void ClearItems()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		base.ClearItems();
	}

	protected override void SetItem(int index, AlternateView item)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		ArgumentNullException.ThrowIfNull(item, "item");
		base.SetItem(index, item);
	}

	protected override void InsertItem(int index, AlternateView item)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		ArgumentNullException.ThrowIfNull(item, "item");
		base.InsertItem(index, item);
	}
}
