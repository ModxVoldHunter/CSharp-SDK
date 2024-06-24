using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;

namespace System.Security.Cryptography;

public struct CngProperty : IEquatable<CngProperty>
{
	private readonly byte[] _value;

	private int? _lazyHashCode;

	public string Name { get; private set; }

	public CngPropertyOptions Options { get; private set; }

	public CngProperty(string name, byte[]? value, CngPropertyOptions options)
	{
		this = default(CngProperty);
		ArgumentNullException.ThrowIfNull(name, "name");
		Name = name;
		Options = options;
		_lazyHashCode = null;
		_value = value.CloneByteArray();
	}

	internal CngProperty(string name, ReadOnlySpan<byte> value, CngPropertyOptions options)
	{
		this = default(CngProperty);
		ArgumentNullException.ThrowIfNull(name, "name");
		Name = name;
		Options = options;
		_lazyHashCode = null;
		_value = value.ToArray();
	}

	public byte[]? GetValue()
	{
		return _value.CloneByteArray();
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is CngProperty)
		{
			return Equals((CngProperty)obj);
		}
		return false;
	}

	public bool Equals(CngProperty other)
	{
		if (!string.Equals(Name, other.Name, StringComparison.Ordinal))
		{
			return false;
		}
		if (Options != other.Options)
		{
			return false;
		}
		if (_value == null)
		{
			return other._value == null;
		}
		if (other._value == null)
		{
			return false;
		}
		return _value.AsSpan().SequenceEqual(other._value);
	}

	public override int GetHashCode()
	{
		if (!_lazyHashCode.HasValue)
		{
			int num = Name.GetHashCode() ^ Options.GetHashCode();
			if (_value != null)
			{
				for (int i = 0; i < _value.Length; i++)
				{
					int num2 = _value[i] << i % 4 * 8;
					num ^= num2;
				}
			}
			_lazyHashCode = num;
		}
		return _lazyHashCode.Value;
	}

	public static bool operator ==(CngProperty left, CngProperty right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(CngProperty left, CngProperty right)
	{
		return !left.Equals(right);
	}

	internal byte[] GetValueWithoutCopying()
	{
		return _value;
	}
}
