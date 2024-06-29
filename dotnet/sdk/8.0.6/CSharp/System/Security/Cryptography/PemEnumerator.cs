namespace System.Security.Cryptography;

internal readonly ref struct PemEnumerator
{
	internal ref struct Enumerator
	{
		internal readonly ref struct PemFieldItem
		{
			private readonly ReadOnlySpan<char> _contents;

			private readonly PemFields _pemFields;

			public PemFieldItem(ReadOnlySpan<char> contents, PemFields pemFields)
			{
				_contents = contents;
				_pemFields = pemFields;
			}

			public void Deconstruct(out ReadOnlySpan<char> contents, out PemFields pemFields)
			{
				contents = _contents;
				pemFields = _pemFields;
			}
		}

		private ReadOnlySpan<char> _contents;

		private PemFields _pemFields;

		public PemFieldItem Current => new PemFieldItem(_contents, _pemFields);

		public Enumerator(ReadOnlySpan<char> contents)
		{
			_contents = contents;
			_pemFields = default(PemFields);
		}

		public bool MoveNext()
		{
			ref ReadOnlySpan<char> contents = ref _contents;
			Index end = _pemFields.Location.End;
			int length = contents.Length;
			int offset = end.GetOffset(length);
			_contents = contents.Slice(offset, length - offset);
			return PemEncoding.TryFind(_contents, out _pemFields);
		}
	}

	private readonly ReadOnlySpan<char> _contents;

	public PemEnumerator(ReadOnlySpan<char> contents)
	{
		_contents = contents;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_contents);
	}
}
