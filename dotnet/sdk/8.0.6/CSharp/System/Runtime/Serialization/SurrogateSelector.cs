namespace System.Runtime.Serialization;

[Obsolete("Formatter-based serialization is obsolete and should not be used.", DiagnosticId = "SYSLIB0050", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
public class SurrogateSelector : ISurrogateSelector
{
	internal readonly SurrogateHashtable _surrogates = new SurrogateHashtable(32);

	internal ISurrogateSelector _nextSelector;

	public virtual void AddSurrogate(Type type, StreamingContext context, ISerializationSurrogate surrogate)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		ArgumentNullException.ThrowIfNull(surrogate, "surrogate");
		SurrogateKey key = new SurrogateKey(type, context);
		_surrogates.Add(key, surrogate);
	}

	private static bool HasCycle(ISurrogateSelector selector)
	{
		ISurrogateSelector surrogateSelector = selector;
		ISurrogateSelector surrogateSelector2 = selector;
		while (surrogateSelector != null)
		{
			surrogateSelector = surrogateSelector.GetNextSelector();
			if (surrogateSelector == null)
			{
				return true;
			}
			if (surrogateSelector == surrogateSelector2)
			{
				return false;
			}
			surrogateSelector = surrogateSelector.GetNextSelector();
			surrogateSelector2 = surrogateSelector2.GetNextSelector();
			if (surrogateSelector == surrogateSelector2)
			{
				return false;
			}
		}
		return true;
	}

	public virtual void ChainSelector(ISurrogateSelector selector)
	{
		ArgumentNullException.ThrowIfNull(selector, "selector");
		if (selector == this)
		{
			throw new SerializationException(System.SR.Serialization_SurrogateCycle);
		}
		if (!HasCycle(selector))
		{
			throw new ArgumentException(System.SR.Serialization_SurrogateCycleInArgument, "selector");
		}
		ISurrogateSelector nextSelector = selector.GetNextSelector();
		ISurrogateSelector surrogateSelector = selector;
		while (nextSelector != null && nextSelector != this)
		{
			surrogateSelector = nextSelector;
			nextSelector = nextSelector.GetNextSelector();
		}
		if (nextSelector == this)
		{
			throw new ArgumentException(System.SR.Serialization_SurrogateCycle, "selector");
		}
		nextSelector = selector;
		ISurrogateSelector surrogateSelector2 = selector;
		while (nextSelector != null)
		{
			nextSelector = ((nextSelector != surrogateSelector) ? nextSelector.GetNextSelector() : GetNextSelector());
			if (nextSelector == null)
			{
				break;
			}
			if (nextSelector == surrogateSelector2)
			{
				throw new ArgumentException(System.SR.Serialization_SurrogateCycle, "selector");
			}
			nextSelector = ((nextSelector != surrogateSelector) ? nextSelector.GetNextSelector() : GetNextSelector());
			surrogateSelector2 = ((surrogateSelector2 != surrogateSelector) ? surrogateSelector2.GetNextSelector() : GetNextSelector());
			if (nextSelector == surrogateSelector2)
			{
				throw new ArgumentException(System.SR.Serialization_SurrogateCycle, "selector");
			}
		}
		ISurrogateSelector nextSelector2 = _nextSelector;
		_nextSelector = selector;
		if (nextSelector2 != null)
		{
			surrogateSelector.ChainSelector(nextSelector2);
		}
	}

	public virtual ISurrogateSelector? GetNextSelector()
	{
		return _nextSelector;
	}

	public virtual ISerializationSurrogate? GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		selector = this;
		SurrogateKey key = new SurrogateKey(type, context);
		ISerializationSurrogate serializationSurrogate = (ISerializationSurrogate)_surrogates[key];
		if (serializationSurrogate != null)
		{
			return serializationSurrogate;
		}
		if (_nextSelector != null)
		{
			return _nextSelector.GetSurrogate(type, context, out selector);
		}
		return null;
	}

	public virtual void RemoveSurrogate(Type type, StreamingContext context)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		SurrogateKey key = new SurrogateKey(type, context);
		_surrogates.Remove(key);
	}
}
