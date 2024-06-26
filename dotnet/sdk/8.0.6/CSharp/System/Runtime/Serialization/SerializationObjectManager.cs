using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization;

[Obsolete("Formatter-based serialization is obsolete and should not be used.", DiagnosticId = "SYSLIB0050", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
public sealed class SerializationObjectManager
{
	private readonly Dictionary<object, object> _objectSeenTable;

	private readonly StreamingContext _context;

	private SerializationEventHandler _onSerializedHandler;

	public SerializationObjectManager(StreamingContext context)
	{
		_context = context;
		_objectSeenTable = new Dictionary<object, object>();
	}

	[RequiresUnreferencedCode("SerializationObjectManager is not trim compatible because the type of objects being managed cannot be statically discovered.")]
	public void RegisterObject(object obj)
	{
		SerializationEvents serializationEventsForType = SerializationEventsCache.GetSerializationEventsForType(obj.GetType());
		if (serializationEventsForType.HasOnSerializingEvents && _objectSeenTable.TryAdd(obj, true))
		{
			serializationEventsForType.InvokeOnSerializing(obj, _context);
			AddOnSerialized(obj);
		}
	}

	public void RaiseOnSerializedEvent()
	{
		_onSerializedHandler?.Invoke(_context);
	}

	[RequiresUnreferencedCode("SerializationObjectManager is not trim compatible because the type of objects being managed cannot be statically discovered.")]
	private void AddOnSerialized(object obj)
	{
		SerializationEvents serializationEventsForType = SerializationEventsCache.GetSerializationEventsForType(obj.GetType());
		_onSerializedHandler = serializationEventsForType.AddOnSerialized(obj, _onSerializedHandler);
	}
}
