using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Converters;
using System.Threading;

namespace System.Text.Json.Nodes;

[DebuggerDisplay("JsonArray[{List.Count}]")]
[DebuggerTypeProxy(typeof(DebugView))]
public sealed class JsonArray : JsonNode, IList<JsonNode?>, ICollection<JsonNode?>, IEnumerable<JsonNode?>, IEnumerable
{
	[ExcludeFromCodeCoverage]
	private sealed class DebugView
	{
		[DebuggerDisplay("{Display,nq}")]
		private struct DebugViewItem
		{
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public JsonNode Value;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public string Display
			{
				get
				{
					if (Value == null)
					{
						return "null";
					}
					if (Value is JsonValue)
					{
						return Value.ToJsonString();
					}
					if (Value is JsonObject jsonObject)
					{
						return $"JsonObject[{jsonObject.Count}]";
					}
					JsonArray jsonArray = (JsonArray)Value;
					return $"JsonArray[{jsonArray.List.Count}]";
				}
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly JsonArray _node;

		public string Json => _node.ToJsonString();

		public string Path => _node.GetPath();

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		private DebugViewItem[] Items
		{
			get
			{
				DebugViewItem[] array = new DebugViewItem[_node.List.Count];
				for (int i = 0; i < _node.List.Count; i++)
				{
					array[i].Value = _node.List[i];
				}
				return array;
			}
		}

		public DebugView(JsonArray node)
		{
			_node = node;
		}
	}

	private JsonElement? _jsonElement;

	private List<JsonNode> _list;

	internal List<JsonNode?> List
	{
		get
		{
			List<JsonNode> list = _list;
			if (list == null)
			{
				return InitializeList();
			}
			return list;
		}
	}

	public int Count => List.Count;

	bool ICollection<JsonNode>.IsReadOnly => false;

	public JsonArray(JsonNodeOptions? options = null)
		: base(options)
	{
	}

	public JsonArray(JsonNodeOptions options, params JsonNode?[] items)
		: base(options)
	{
		InitializeFromArray(items);
	}

	public JsonArray(params JsonNode?[] items)
	{
		InitializeFromArray(items);
	}

	internal override JsonValueKind GetValueKindCore()
	{
		return JsonValueKind.Array;
	}

	internal override JsonNode DeepCloneCore()
	{
		GetUnderlyingRepresentation(out var list, out var jsonElement);
		if (list == null)
		{
			if (!jsonElement.HasValue)
			{
				return new JsonArray(base.Options);
			}
			return new JsonArray(jsonElement.Value.Clone(), base.Options);
		}
		JsonArray jsonArray = new JsonArray(base.Options)
		{
			_list = new List<JsonNode>(list.Count)
		};
		for (int i = 0; i < list.Count; i++)
		{
			jsonArray.Add(list[i]?.DeepCloneCore());
		}
		return jsonArray;
	}

	internal override bool DeepEqualsCore(JsonNode node)
	{
		if (node != null && !(node is JsonObject))
		{
			if (!(node is JsonValue jsonValue))
			{
				if (node is JsonArray jsonArray)
				{
					List<JsonNode> list = List;
					List<JsonNode> list2 = jsonArray.List;
					if (list.Count != list2.Count)
					{
						return false;
					}
					for (int i = 0; i < list.Count; i++)
					{
						if (!JsonNode.DeepEquals(list[i], list2[i]))
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

	internal int GetElementIndex(JsonNode node)
	{
		return List.IndexOf(node);
	}

	public IEnumerable<T> GetValues<T>()
	{
		foreach (JsonNode item in List)
		{
			yield return (item == null) ? ((T)(object)null) : item.GetValue<T>();
		}
	}

	private void InitializeFromArray(JsonNode[] items)
	{
		List<JsonNode> list = new List<JsonNode>(items);
		for (int i = 0; i < items.Length; i++)
		{
			items[i]?.AssignParent(this);
		}
		_list = list;
	}

	public static JsonArray? Create(JsonElement element, JsonNodeOptions? options = null)
	{
		return element.ValueKind switch
		{
			JsonValueKind.Null => null, 
			JsonValueKind.Array => new JsonArray(element, options), 
			_ => throw new InvalidOperationException(System.SR.Format(System.SR.NodeElementWrongType, "Array")), 
		};
	}

	internal JsonArray(JsonElement element, JsonNodeOptions? options = null)
		: base(options)
	{
		_jsonElement = element;
	}

	[RequiresUnreferencedCode("Creating JsonValue instances with non-primitive types is not compatible with trimming. It can result in non-primitive types being serialized, which may have their members trimmed.")]
	[RequiresDynamicCode("Creating JsonValue instances with non-primitive types requires generating code at runtime.")]
	public void Add<T>(T? value)
	{
		JsonNode item = JsonNode.ConvertFromValue(value, base.Options);
		Add(item);
	}

	internal JsonNode GetItem(int index)
	{
		return List[index];
	}

	internal void SetItem(int index, JsonNode value)
	{
		value?.AssignParent(this);
		DetachParent(List[index]);
		List[index] = value;
	}

	internal override void GetPath(List<string> path, JsonNode child)
	{
		if (child != null)
		{
			int value = List.IndexOf(child);
			path.Add($"[{value}]");
		}
		base.Parent?.GetPath(path, this);
	}

	public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
	{
		if (writer == null)
		{
			ThrowHelper.ThrowArgumentNullException("writer");
		}
		GetUnderlyingRepresentation(out var list, out var jsonElement);
		if (list == null && jsonElement.HasValue)
		{
			jsonElement.Value.WriteTo(writer);
			return;
		}
		writer.WriteStartArray();
		foreach (JsonNode item in List)
		{
			if (item == null)
			{
				writer.WriteNullValue();
			}
			else
			{
				item.WriteTo(writer, options);
			}
		}
		writer.WriteEndArray();
	}

	private List<JsonNode> InitializeList()
	{
		GetUnderlyingRepresentation(out var list, out var jsonElement);
		if (list == null)
		{
			if (jsonElement.HasValue)
			{
				JsonElement value = jsonElement.Value;
				list = new List<JsonNode>(value.GetArrayLength());
				foreach (JsonElement item in value.EnumerateArray())
				{
					JsonNode jsonNode = JsonNodeConverter.Create(item, base.Options);
					jsonNode?.AssignParent(this);
					list.Add(jsonNode);
				}
			}
			else
			{
				list = new List<JsonNode>();
			}
			_list = list;
			Interlocked.MemoryBarrier();
			_jsonElement = null;
		}
		return list;
	}

	private void GetUnderlyingRepresentation(out List<JsonNode> list, out JsonElement? jsonElement)
	{
		jsonElement = _jsonElement;
		Interlocked.MemoryBarrier();
		list = _list;
	}

	public void Add(JsonNode? item)
	{
		item?.AssignParent(this);
		List.Add(item);
	}

	public void Clear()
	{
		List<JsonNode> list = _list;
		if (list == null)
		{
			_jsonElement = null;
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			DetachParent(list[i]);
		}
		list.Clear();
	}

	public bool Contains(JsonNode? item)
	{
		return List.Contains(item);
	}

	public int IndexOf(JsonNode? item)
	{
		return List.IndexOf(item);
	}

	public void Insert(int index, JsonNode? item)
	{
		item?.AssignParent(this);
		List.Insert(index, item);
	}

	public bool Remove(JsonNode? item)
	{
		if (List.Remove(item))
		{
			DetachParent(item);
			return true;
		}
		return false;
	}

	public void RemoveAt(int index)
	{
		JsonNode item = List[index];
		List.RemoveAt(index);
		DetachParent(item);
	}

	void ICollection<JsonNode>.CopyTo(JsonNode[] array, int index)
	{
		List.CopyTo(array, index);
	}

	public IEnumerator<JsonNode?> GetEnumerator()
	{
		return List.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)List).GetEnumerator();
	}

	private static void DetachParent(JsonNode item)
	{
		if (item != null)
		{
			item.Parent = null;
		}
	}
}
