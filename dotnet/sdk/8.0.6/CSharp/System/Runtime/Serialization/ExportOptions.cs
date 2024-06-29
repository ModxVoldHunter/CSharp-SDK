using System.Collections.ObjectModel;

namespace System.Runtime.Serialization;

public class ExportOptions
{
	private Collection<Type> _knownTypes;

	public ISerializationSurrogateProvider? DataContractSurrogate { get; set; }

	public Collection<Type> KnownTypes => _knownTypes ?? (_knownTypes = new Collection<Type>());
}
