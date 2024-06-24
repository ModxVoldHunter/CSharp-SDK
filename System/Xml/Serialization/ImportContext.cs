using System.Collections;
using System.Collections.Specialized;

namespace System.Xml.Serialization;

public class ImportContext
{
	private readonly bool _shareTypes;

	private SchemaObjectCache _cache;

	private Hashtable _mappings;

	private Hashtable _elements;

	private CodeIdentifiers _typeIdentifiers;

	internal SchemaObjectCache Cache => _cache ?? (_cache = new SchemaObjectCache());

	internal Hashtable Elements => _elements ?? (_elements = new Hashtable());

	internal Hashtable Mappings => _mappings ?? (_mappings = new Hashtable());

	public CodeIdentifiers TypeIdentifiers => _typeIdentifiers ?? (_typeIdentifiers = new CodeIdentifiers());

	public bool ShareTypes => _shareTypes;

	public StringCollection Warnings => Cache.Warnings;

	public ImportContext(CodeIdentifiers? identifiers, bool shareTypes)
	{
		_typeIdentifiers = identifiers;
		_shareTypes = shareTypes;
	}

	internal ImportContext()
		: this(null, shareTypes: false)
	{
	}
}
