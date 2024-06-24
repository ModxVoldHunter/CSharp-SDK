namespace System.Security.Cryptography;

internal sealed class RandomNumberGeneratorImplementation : RandomNumberGenerator
{
	internal static readonly RandomNumberGeneratorImplementation s_singleton = new RandomNumberGeneratorImplementation();

	private RandomNumberGeneratorImplementation()
	{
	}

	internal unsafe static void FillSpan(Span<byte> data)
	{
		if (data.Length > 0)
		{
			fixed (byte* pbBuffer = data)
			{
				GetBytes(pbBuffer, data.Length);
			}
		}
	}

	public override void GetBytes(byte[] data)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		GetBytes(new Span<byte>(data));
	}

	public override void GetBytes(byte[] data, int offset, int count)
	{
		RandomNumberGenerator.VerifyGetBytes(data, offset, count);
		GetBytes(new Span<byte>(data, offset, count));
	}

	public unsafe override void GetBytes(Span<byte> data)
	{
		if (data.Length > 0)
		{
			fixed (byte* pbBuffer = data)
			{
				GetBytes(pbBuffer, data.Length);
			}
		}
	}

	public override void GetNonZeroBytes(byte[] data)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		FillNonZeroBytes(data);
	}

	public override void GetNonZeroBytes(Span<byte> data)
	{
		FillNonZeroBytes(data);
	}

	internal static void FillNonZeroBytes(Span<byte> data)
	{
		while (data.Length > 0)
		{
			FillSpan(data);
			int num = data.IndexOf<byte>(0);
			if (num < 0)
			{
				break;
			}
			int num2 = 1;
			Span<byte> span = data.Slice(num + 1);
			while (true)
			{
				int num3 = span.IndexOf<byte>(0);
				if (num3 < 0)
				{
					break;
				}
				span.Slice(0, num3).CopyTo(data.Slice(num));
				num2++;
				num += num3;
				span = span.Slice(num3 + 1);
			}
			span.CopyTo(data.Slice(num));
			data = data.Slice(data.Length - num2);
		}
	}

	private unsafe static void GetBytes(byte* pbBuffer, int count)
	{
		global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptGenRandom(IntPtr.Zero, pbBuffer, count, 2);
		if (nTSTATUS != 0)
		{
			throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
		}
	}
}
