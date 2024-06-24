using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics;

public readonly struct ActivityLink : IEquatable<ActivityLink>
{
	private readonly Activity.TagsLinkedList _tags;

	public ActivityContext Context { get; }

	public IEnumerable<KeyValuePair<string, object?>>? Tags => _tags;

	public ActivityLink(ActivityContext context, ActivityTagsCollection? tags = null)
	{
		Context = context;
		_tags = ((tags != null && tags.Count > 0) ? new Activity.TagsLinkedList(tags) : null);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is ActivityLink value)
		{
			return Equals(value);
		}
		return false;
	}

	public bool Equals(ActivityLink value)
	{
		if (Context == value.Context)
		{
			return value.Tags == Tags;
		}
		return false;
	}

	public static bool operator ==(ActivityLink left, ActivityLink right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ActivityLink left, ActivityLink right)
	{
		return !left.Equals(right);
	}

	public Activity.Enumerator<KeyValuePair<string, object?>> EnumerateTagObjects()
	{
		return new Activity.Enumerator<KeyValuePair<string, object>>(_tags?.First);
	}

	public override int GetHashCode()
	{
		HashCode hashCode = default(HashCode);
		hashCode.Add(Context);
		if (Tags != null)
		{
			foreach (KeyValuePair<string, object> tag in Tags)
			{
				hashCode.Add(tag.Key);
				hashCode.Add(tag.Value);
			}
		}
		return hashCode.ToHashCode();
	}
}
