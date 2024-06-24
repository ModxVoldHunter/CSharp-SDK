using System.Collections.Generic;

namespace System.Diagnostics;

public readonly struct ActivityEvent
{
	private static readonly IEnumerable<KeyValuePair<string, object>> s_emptyTags = Array.Empty<KeyValuePair<string, object>>();

	private readonly Activity.TagsLinkedList _tags;

	public string Name { get; }

	public DateTimeOffset Timestamp { get; }

	public IEnumerable<KeyValuePair<string, object?>> Tags
	{
		get
		{
			IEnumerable<KeyValuePair<string, object>> tags = _tags;
			return tags ?? s_emptyTags;
		}
	}

	public ActivityEvent(string name)
		: this(name, DateTimeOffset.UtcNow)
	{
	}

	public ActivityEvent(string name, DateTimeOffset timestamp = default(DateTimeOffset), ActivityTagsCollection? tags = null)
	{
		Name = name ?? string.Empty;
		Timestamp = ((timestamp != default(DateTimeOffset)) ? timestamp : DateTimeOffset.UtcNow);
		_tags = ((tags != null && tags.Count > 0) ? new Activity.TagsLinkedList(tags) : null);
	}

	public Activity.Enumerator<KeyValuePair<string, object?>> EnumerateTagObjects()
	{
		return new Activity.Enumerator<KeyValuePair<string, object>>(_tags?.First);
	}
}
