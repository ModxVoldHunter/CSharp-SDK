namespace System.Security.Cryptography;

internal static class PemKeyHelpers
{
	public delegate bool TryExportKeyAction<T>(T arg, Span<byte> destination, out int bytesWritten);

	public delegate bool TryExportEncryptedKeyAction<T, TPassword>(T arg, ReadOnlySpan<TPassword> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten);

	public delegate void ImportKeyAction(ReadOnlySpan<byte> source, out int bytesRead);

	public delegate ImportKeyAction FindImportActionFunc(ReadOnlySpan<char> label);

	public delegate void ImportEncryptedKeyAction<TPass>(ReadOnlySpan<TPass> password, ReadOnlySpan<byte> source, out int bytesRead);

	public unsafe static bool TryExportToEncryptedPem<T, TPassword>(T arg, ReadOnlySpan<TPassword> password, PbeParameters pbeParameters, TryExportEncryptedKeyAction<T, TPassword> exporter, Span<char> destination, out int charsWritten)
	{
		int minimumLength = 4096;
		while (true)
		{
			byte[] array = System.Security.Cryptography.CryptoPool.Rent(minimumLength);
			int bytesWritten = 0;
			minimumLength = array.Length;
			fixed (byte* ptr = array)
			{
				try
				{
					if (exporter(arg, password, pbeParameters, array, out bytesWritten))
					{
						return PemEncoding.TryWrite(data: new Span<byte>(array, 0, bytesWritten), label: "ENCRYPTED PRIVATE KEY", destination: destination, charsWritten: out charsWritten);
					}
				}
				finally
				{
					System.Security.Cryptography.CryptoPool.Return(array, bytesWritten);
				}
				minimumLength = checked(minimumLength * 2);
			}
		}
	}

	public unsafe static bool TryExportToPem<T>(T arg, string label, TryExportKeyAction<T> exporter, Span<char> destination, out int charsWritten)
	{
		int minimumLength = 4096;
		while (true)
		{
			byte[] array = System.Security.Cryptography.CryptoPool.Rent(minimumLength);
			int bytesWritten = 0;
			minimumLength = array.Length;
			fixed (byte* ptr = array)
			{
				try
				{
					if (exporter(arg, array, out bytesWritten))
					{
						return PemEncoding.TryWrite(data: new Span<byte>(array, 0, bytesWritten), label: label, destination: destination, charsWritten: out charsWritten);
					}
				}
				finally
				{
					System.Security.Cryptography.CryptoPool.Return(array, bytesWritten);
				}
				minimumLength = checked(minimumLength * 2);
			}
		}
	}

	public static void ImportEncryptedPem<TPass>(ReadOnlySpan<char> input, ReadOnlySpan<TPass> password, ImportEncryptedKeyAction<TPass> importAction)
	{
		bool flag = false;
		PemFields pemFields = default(PemFields);
		ReadOnlySpan<char> readOnlySpan = default(ReadOnlySpan<char>);
		ReadOnlySpan<char> readOnlySpan2 = input;
		PemFields fields;
		Range label;
		int offset;
		int length;
		while (PemEncoding.TryFind(readOnlySpan2, out fields))
		{
			label = fields.Label;
			ReadOnlySpan<char> span = readOnlySpan2[label.Start..label.End];
			if (span.SequenceEqual("ENCRYPTED PRIVATE KEY"))
			{
				if (flag)
				{
					throw new ArgumentException(System.SR.Argument_PemImport_AmbiguousPem, "input");
				}
				flag = true;
				pemFields = fields;
				readOnlySpan = readOnlySpan2;
			}
			Index end = fields.Location.End;
			Index index = end;
			length = readOnlySpan2.Length;
			offset = index.GetOffset(length);
			readOnlySpan2 = readOnlySpan2.Slice(offset, length - offset);
		}
		if (!flag)
		{
			throw new ArgumentException(System.SR.Argument_PemImport_NoPemFound, "input");
		}
		label = pemFields.Base64Data;
		offset = readOnlySpan.Length;
		length = label.Start.GetOffset(offset);
		int bytesRead = label.End.GetOffset(offset) - length;
		ReadOnlySpan<char> chars = readOnlySpan.Slice(length, bytesRead);
		int decodedDataLength = pemFields.DecodedDataLength;
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(decodedDataLength);
		int bytesWritten = 0;
		try
		{
			if (!Convert.TryFromBase64Chars(chars, array, out bytesWritten))
			{
				throw new ArgumentException();
			}
			Span<byte> span2 = array.AsSpan(0, bytesWritten);
			importAction(password, span2, out bytesRead);
		}
		finally
		{
			System.Security.Cryptography.CryptoPool.Return(array, bytesWritten);
		}
	}

	public static void ImportPem(ReadOnlySpan<char> input, FindImportActionFunc callback)
	{
		ImportKeyAction importKeyAction = null;
		PemFields pemFields = default(PemFields);
		ReadOnlySpan<char> readOnlySpan = default(ReadOnlySpan<char>);
		bool flag = false;
		ReadOnlySpan<char> readOnlySpan2 = input;
		PemFields fields;
		Range label;
		int offset;
		int length;
		while (PemEncoding.TryFind(readOnlySpan2, out fields))
		{
			label = fields.Label;
			ReadOnlySpan<char> readOnlySpan3 = readOnlySpan2[label.Start..label.End];
			ImportKeyAction importKeyAction2 = callback(readOnlySpan3);
			if (importKeyAction2 != null)
			{
				if (importKeyAction != null || flag)
				{
					throw new ArgumentException(System.SR.Argument_PemImport_AmbiguousPem, "input");
				}
				importKeyAction = importKeyAction2;
				pemFields = fields;
				readOnlySpan = readOnlySpan2;
			}
			else if (readOnlySpan3.SequenceEqual("ENCRYPTED PRIVATE KEY"))
			{
				if (importKeyAction != null || flag)
				{
					throw new ArgumentException(System.SR.Argument_PemImport_AmbiguousPem, "input");
				}
				flag = true;
			}
			Index end = fields.Location.End;
			Index index = end;
			length = readOnlySpan2.Length;
			offset = index.GetOffset(length);
			readOnlySpan2 = readOnlySpan2.Slice(offset, length - offset);
		}
		if (flag)
		{
			throw new ArgumentException(System.SR.Argument_PemImport_EncryptedPem, "input");
		}
		if (importKeyAction == null)
		{
			throw new ArgumentException(System.SR.Argument_PemImport_NoPemFound, "input");
		}
		label = pemFields.Base64Data;
		offset = readOnlySpan.Length;
		length = label.Start.GetOffset(offset);
		int bytesRead = label.End.GetOffset(offset) - length;
		ReadOnlySpan<char> chars = readOnlySpan.Slice(length, bytesRead);
		int decodedDataLength = pemFields.DecodedDataLength;
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(decodedDataLength);
		int bytesWritten = 0;
		try
		{
			if (!Convert.TryFromBase64Chars(chars, array, out bytesWritten))
			{
				throw new ArgumentException();
			}
			Span<byte> span = array.AsSpan(0, bytesWritten);
			importKeyAction(span, out bytesRead);
		}
		finally
		{
			System.Security.Cryptography.CryptoPool.Return(array, bytesWritten);
		}
	}
}
