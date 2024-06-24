using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Converters;
using System.Threading;

namespace System.Text.Json.Nodes;

[DebuggerDisplay("JsonObject[{Count}]")]
[DebuggerTypeProxy(typeof(DebugView))]
public sealed class JsonObject : JsonNode, IDictionary<string, JsonNode?>, ICollection<KeyValuePair<string, JsonNode?>>, IEnumerable<KeyValuePair<string, JsonNode?>>, IEnumerable
{
	[ExcludeFromCodeCoverage]
	private sealed class DebugView
	{
		[DebuggerDisplay("{Display,nq}")]
		private struct DebugViewProperty
		{
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public JsonNode Value;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public string PropertyName;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public string Display
			{
				get
				{
					if (Value == null)
					{
						return PropertyName + " = null";
					}
					if (Value is JsonValue)
					{
						return PropertyName + " = " + Value.ToJsonString();
					}
					if (Value is JsonObject jsonObject)
					{
						return $"{PropertyName} = JsonObject[{jsonObject.Count}]";
					}
					JsonArray jsonArray = (JsonArray)Value;
					return $"{PropertyName} = JsonArray[{jsonArray.Count}]";
				}
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly JsonObject _node;

		public string Json => _node.ToJsonString();

		public string Path => _node.GetPath();

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		private DebugViewProperty[] Items
		{
			get
			{
				DebugViewProperty[] array = new DebugViewProperty[_node.Count];
				int num = 0;
				foreach (KeyValuePair<string, JsonNode> item in _node)
				{
					array[num].PropertyName = item.Key;
					array[num].Value = item.Value;
					num++;
				}
				return array;
			}
		}

		public DebugView(JsonObject node)
		{
			_node = node;
		}
	}

	private JsonElement? _jsonElement;

	private JsonPropertyDictionary<JsonNode> _dictionary;

	internal JsonPropertyDictionary<JsonNode?> Dictionary
	{
		get
		{
			JsonPropertyDictionary<JsonNode> dictionary = _dictionary;
			if (dictionary == null)
			{
				return InitializeDictionary();
			}
			return dictionary;
		}
	}

	public int Count => Dictionary.Count;

	ICollection<string> IDictionary<string, JsonNode>.Keys => Dictionary.Keys;

	ICollection<JsonNode?> IDictionary<string, JsonNode>.Values => Dictionary.Values;

	bool ICollection<KeyValuePair<string, JsonNode>>.IsReadOnly => false;

	public JsonObject(JsonNodeOptions? options = null)
		: base(options)
	{
	}

	public JsonObject(IEnumerable<KeyValuePair<string, JsonNode?>> properties, JsonNodeOptions? options = null)
		: this(options)
	{
		foreach (KeyValuePair<string, JsonNode> property in properties)
		{
			Add(property.Key, property.Value);
		}
	}

	public static JsonObject? Create(JsonElement element, JsonNodeOptions? options = null)
	{
		return element.ValueKind switch
		{
			JsonValueKind.Null => null, 
			JsonValueKind.Object => new JsonObject(element, options), 
			_ => throw new InvalidOperationException(System.SR.Format(System.SR.NodeElementWrongType, "Object")), 
		};
	}

	internal JsonObject(JsonElement element, JsonNodeOptions? options = null)
		: this(options)
	{
		_jsonElement = element;
	}

	internal override JsonNode DeepCloneCore()
	{
		GetUnderlyingRepresentation(out var dictionary, out var jsonElement);
		if (dictionary == null)
		{
			if (!jsonElement.HasValue)
			{
				return new JsonObject(base.Options);
			}
			return new JsonObject(jsonElement.Value.Clone(), base.Options);
		}
		bool caseInsensitive = base.Options.HasValue && base.Options.Value.PropertyNameCaseInsensitive;
		JsonObject jsonObject = new JsonObject(base.Options)
		{
			_dictionary = new JsonPropertyDictionary<JsonNode>(caseInsensitive, dictionary.Count)
		};
		foreach (KeyValuePair<string, JsonNode> item in dictionary)
		{
			jsonObject.Add(item.Key, item.Value?.DeepCloneCore());
		}
		return jsonObject;
	}

	internal string GetPropertyName(JsonNode node)
	{
		KeyValuePair<string, JsonNode>? keyValuePair = Dictionary.FindValue(node);
		if (!keyValuePair.HasValue)
		{
			return string.Empty;
		}
		return keyValuePair.Value.Key;
	}

	public bool TryGetPropertyValue(string propertyName, out JsonNode? jsonNode)
	{
		return ((IDictionary<string, JsonNode>)this).TryGetValue(propertyName, out jsonNode);
	}

	public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
	{
		if (writer == null)
		{
			ThrowHelper.ThrowArgumentNullException("writer");
		}
		GetUnderlyingRepresentation(out var dictionary, out var jsonElement);
		if (dictionary == null && jsonElement.HasValue)
		{
			jsonElement.Value.WriteTo(writer);
			return;
		}
		writer.WriteStartObject();
		foreach (KeyValuePair<string, JsonNode> item in Dictionary)
		{
			writer.WritePropertyName(item.Key);
			if (item.Value == null)
			{
				writer.WriteNullValue();
			}
			else
			{
				item.Value.WriteTo(writer, options);
			}
		}
		writer.WriteEndObject();
	}

	internal override JsonValueKind GetValueKindCore()
	{
		return JsonValueKind.Object;
	}

	internal override bool DeepEqualsCore(JsonNode node)
	{
		if (node != null && !(node is JsonArray))
		{
			if (!(node is JsonValue jsonValue))
			{
				if (node is JsonObject jsonObject)
				{
					JsonPropertyDictionary<JsonNode> dictionary = Dictionary;
					JsonPropertyDictionary<JsonNode> dictionary2 = jsonObject.Dictionary;
					if (dictionary.Count != dictionary2.Count)
					{
						return false;
					}
					foreach (KeyValuePair<string, JsonNode> item in dictionary)
					{
						JsonNode node2 = dictionary2[item.Key];
						if (!JsonNode.DeepEquals(item.Value, node2))
						{
							return false;
						}
					}
					return true;
				}
				return false;
			}
			return jsonValue.DeepEqualsCore(this);
		}
		return false;
	}

	internal JsonNode GetItem(string propertyName)
	{
		if (TryGetPropertyValue(propertyName, out JsonNode jsonNode))
		{
			return jsonNode;
		}
		return null;
	}

	internal override void GetPath(List<string> path, JsonNode child)
	{
		if (child != null)
		{
			string key = Dictionary.FindValue(child).Value.Key;
			if (key.AsSpan().ContainsSpecialCharacters())
			{
				path.Add("['" + key + "']");
			}
			else
			{
				path.Add("." + key);
			}
		}
		base.Parent?.GetPath(path, this);
	}

	internal void SetItem(string propertyName, JsonNode value)
	{
		bool valueAlreadyInDictionary;
		JsonNode item = Dictionary.SetValue(propertyName, value, out valueAlreadyInDictionary);
		if (!valueAlreadyInDictionary)
		{
			value?.AssignParent(this);
		}
		DetachParent(item);
	}

	private void DetachParent(JsonNode item)
	{
		if (item != null)
		{
			item.Parent = null;
		}
	}

	public void Add(string propertyName, JsonNode? value)
	{
		Dictionary.Add(propertyName, value);
		value?.AssignParent(this);
	}

	public void Add(KeyValuePair<string, JsonNode?> property)
	{
		Add(property.Key, property.Value);
	}

	public void Clear()
	{
		JsonPropertyDictionary<JsonNode> dictionary = _dictionary;
		if (dictionary == null)
		{
			_jsonElement = null;
			return;
		}
		foreach (JsonNode item in dictionary.GetValueCollection())
		{
			DetachParent(item);
		}
		dictionary.Clear();
	}

	public bool ContainsKey(string propertyName)
	{
		return Dictionary.ContainsKey(propertyName);
	}

	public bool Remove(string propertyName)
	{
		if (propertyName == null)
		{
			ThrowHelper.ThrowArgumentNullException("propertyName");
		}
		JsonNode existing;
		bool flag = Dictionary.TryRemoveProperty(propertyName, out existing);
		if (flag)
		{
			DetachParent(existing);
		}
		return flag;
	}

	bool ICollection<KeyValuePair<string, JsonNode>>.Contains(KeyValuePair<string, JsonNode> item)
	{
		return Dictionary.Contains(item);
	}

	void ICollection<KeyValuePair<string, JsonNode>>.CopyTo(KeyValuePair<string, JsonNode>[] array, int index)
	{
		Dictionary.CopyTo(array, index);
	}

	public IEnumerator<KeyValuePair<string, JsonNode?>> GetEnumerator()
	{
		return Dictionary.GetEnumerator();
	}

	bool ICollection<KeyValuePair<string, JsonNode>>.Remove(KeyValuePair<string, JsonNode> item)
	{
		return Remove(item.Key);
	}

	bool IDictionary<string, JsonNode>.TryGetValue(string propertyName, out JsonNode jsonNode)
	{
		return Dictionary.TryGetValue(propertyName, out jsonNode);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return Dictionary.GetEnumerator();
	}

	private JsonPropertyDictionary<JsonNode> InitializeDictionary()
	{
		GetUnderlyingRepresentation(out var dictionary, out var jsonElement);
		if (dictionary == null)
		{
			bool caseInsensitive = base.Options.HasValue && base.Options.Value.PropertyNameCaseInsensitive;
			dictionary = new JsonPropertyDictionary<JsonNode>(caseInsensitive);
			if (jsonElement.HasValue)
			{
				foreach (JsonProperty item in jsonElement.Value.EnumerateObject())
				{
					JsonNode jsonNode = JsonNodeConverter.Create(item.Value, base.Options);
					if (jsonNode != null)
					{
						jsonNode.Parent = this;
					}
					dictionary.Add(new KeyValuePair<string, JsonNode>(item.Name, jsonNode));
				}
			}
			_dictionary = dictionary;
			Interlocked.MemoryBarrier();
			_jsonElement = null;
		}
		return dictionary;
	}

	private void GetUnderlyingRepresentation(out JsonPropertyDictionary<JsonNode> dictionary, out JsonElement? jsonElement)
	{
		jsonElement = _jsonElement;
		Interlocked.MemoryBarrier();
		dictionary = _dictionary;
	}
}
