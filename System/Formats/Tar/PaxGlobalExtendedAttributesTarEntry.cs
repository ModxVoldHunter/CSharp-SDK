using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Formats.Tar;

public sealed class PaxGlobalExtendedAttributesTarEntry : PosixTarEntry
{
	private ReadOnlyDictionary<string, string> _readOnlyGlobalExtendedAttributes;

	public IReadOnlyDictionary<string, string> GlobalExtendedAttributes => _readOnlyGlobalExtendedAttributes ?? (_readOnlyGlobalExtendedAttributes = _header.ExtendedAttributes.AsReadOnly());

	internal PaxGlobalExtendedAttributesTarEntry(TarHeader header, TarReader readerOfOrigin)
		: base(header, readerOfOrigin, TarEntryFormat.Pax)
	{
	}

	public PaxGlobalExtendedAttributesTarEntry(IEnumerable<KeyValuePair<string, string>> globalExtendedAttributes)
		: base(TarEntryType.GlobalExtendedAttributes, "PaxGlobalExtendedAttributesTarEntry", TarEntryFormat.Pax, isGea: true)
	{
		ArgumentNullException.ThrowIfNull(globalExtendedAttributes, "globalExtendedAttributes");
		_header.InitializeExtendedAttributesWithExisting(globalExtendedAttributes);
	}

	internal override bool IsDataStreamSetterSupported()
	{
		return false;
	}
}
