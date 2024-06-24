using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json;

[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal struct ReadStack
{
	public ReadStackFrame Current;

	private ReadStackFrame[] _stack;

	private int _count;

	private int _continuationCount;

	public long BytesConsumed;

	public bool ReadAhead;

	public ReferenceResolver ReferenceResolver;

	public bool SupportContinuation;

	public string ReferenceId;

	public object PolymorphicTypeDiscriminator;

	public bool PreserveReferences;

	public readonly ref ReadStackFrame Parent => ref _stack[_count - 2];

	public readonly JsonPropertyInfo ParentProperty
	{
		get
		{
			if (!Current.HasParentObject)
			{
				return null;
			}
			return Parent.JsonPropertyInfo;
		}
	}

	public bool IsContinuation => _continuationCount != 0;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private string DebuggerDisplay => $"Path = {JsonPath()}, Current = ConverterStrategy.{Current.JsonTypeInfo?.Converter.ConverterStrategy}, {Current.JsonTypeInfo?.Type.Name}";

	private void EnsurePushCapacity()
	{
		if (_stack == null)
		{
			_stack = new ReadStackFrame[4];
		}
		else if (_count - 1 == _stack.Length)
		{
			Array.Resize(ref _stack, 2 * _stack.Length);
		}
	}

	internal void Initialize(JsonTypeInfo jsonTypeInfo, bool supportContinuation = false)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve)
		{
			ReferenceResolver = options.ReferenceHandler.CreateResolver(writing: false);
			PreserveReferences = true;
		}
		Current.JsonTypeInfo = jsonTypeInfo;
		Current.JsonPropertyInfo = jsonTypeInfo.PropertyInfoForTypeInfo;
		Current.NumberHandling = Current.JsonPropertyInfo.EffectiveNumberHandling;
		Current.CanContainMetadata = PreserveReferences || (jsonTypeInfo.PolymorphicTypeResolver?.UsesTypeDiscriminators ?? false);
		SupportContinuation = supportContinuation;
	}

	public void Push()
	{
		if (_continuationCount == 0)
		{
			if (_count == 0)
			{
				_count = 1;
			}
			else
			{
				JsonTypeInfo jsonTypeInfo = Current.JsonPropertyInfo?.JsonTypeInfo ?? Current.CtorArgumentState.JsonParameterInfo.JsonTypeInfo;
				JsonNumberHandling? numberHandling = Current.NumberHandling;
				EnsurePushCapacity();
				_stack[_count - 1] = Current;
				Current = default(ReadStackFrame);
				_count++;
				Current.JsonTypeInfo = jsonTypeInfo;
				Current.JsonPropertyInfo = jsonTypeInfo.PropertyInfoForTypeInfo;
				Current.NumberHandling = numberHandling ?? Current.JsonPropertyInfo.EffectiveNumberHandling;
				Current.CanContainMetadata = PreserveReferences || (jsonTypeInfo.PolymorphicTypeResolver?.UsesTypeDiscriminators ?? false);
			}
		}
		else
		{
			if (_count++ > 0)
			{
				_stack[_count - 2] = Current;
				Current = _stack[_count - 1];
			}
			if (_continuationCount == _count)
			{
				_continuationCount = 0;
			}
		}
		SetConstructorArgumentState();
	}

	public void Pop(bool success)
	{
		if (!success)
		{
			if (_continuationCount == 0)
			{
				if (_count == 1)
				{
					_continuationCount = 1;
					_count = 0;
					return;
				}
				EnsurePushCapacity();
				_continuationCount = _count--;
			}
			else if (--_count == 0)
			{
				return;
			}
			_stack[_count] = Current;
			Current = _stack[_count - 1];
		}
		else if (--_count > 0)
		{
			Current = _stack[_count - 1];
		}
	}

	public JsonConverter InitializePolymorphicReEntry(JsonTypeInfo derivedJsonTypeInfo)
	{
		Current.PolymorphicJsonTypeInfo = Current.JsonTypeInfo;
		Current.JsonTypeInfo = derivedJsonTypeInfo;
		Current.JsonPropertyInfo = derivedJsonTypeInfo.PropertyInfoForTypeInfo;
		ref JsonNumberHandling? numberHandling = ref Current.NumberHandling;
		JsonNumberHandling? jsonNumberHandling = numberHandling;
		if (!jsonNumberHandling.HasValue)
		{
			numberHandling = Current.JsonPropertyInfo.NumberHandling;
		}
		Current.PolymorphicSerializationState = PolymorphicSerializationState.PolymorphicReEntryStarted;
		SetConstructorArgumentState();
		return derivedJsonTypeInfo.Converter;
	}

	public JsonConverter ResumePolymorphicReEntry()
	{
		ref JsonTypeInfo jsonTypeInfo = ref Current.JsonTypeInfo;
		ref JsonTypeInfo polymorphicJsonTypeInfo = ref Current.PolymorphicJsonTypeInfo;
		JsonTypeInfo polymorphicJsonTypeInfo2 = Current.PolymorphicJsonTypeInfo;
		JsonTypeInfo jsonTypeInfo2 = Current.JsonTypeInfo;
		jsonTypeInfo = polymorphicJsonTypeInfo2;
		polymorphicJsonTypeInfo = jsonTypeInfo2;
		Current.PolymorphicSerializationState = PolymorphicSerializationState.PolymorphicReEntryStarted;
		return Current.JsonTypeInfo.Converter;
	}

	public void ExitPolymorphicConverter(bool success)
	{
		ref JsonTypeInfo jsonTypeInfo = ref Current.JsonTypeInfo;
		ref JsonTypeInfo polymorphicJsonTypeInfo = ref Current.PolymorphicJsonTypeInfo;
		JsonTypeInfo polymorphicJsonTypeInfo2 = Current.PolymorphicJsonTypeInfo;
		JsonTypeInfo jsonTypeInfo2 = Current.JsonTypeInfo;
		jsonTypeInfo = polymorphicJsonTypeInfo2;
		polymorphicJsonTypeInfo = jsonTypeInfo2;
		Current.PolymorphicSerializationState = ((!success) ? PolymorphicSerializationState.PolymorphicReEntrySuspended : PolymorphicSerializationState.None);
	}

	public string JsonPath()
	{
		StringBuilder stringBuilder = new StringBuilder("$");
		int continuationCount = _continuationCount;
		(int, bool) tuple = continuationCount switch
		{
			0 => (_count - 1, true), 
			1 => (0, true), 
			_ => (continuationCount, false), 
		};
		int item = tuple.Item1;
		bool item2 = tuple.Item2;
		for (int i = 0; i < item; i++)
		{
			AppendStackFrame(stringBuilder, ref _stack[i]);
		}
		if (item2)
		{
			AppendStackFrame(stringBuilder, ref Current);
		}
		return stringBuilder.ToString();
		static void AppendPropertyName(StringBuilder sb, string propertyName)
		{
			if (propertyName != null)
			{
				if (propertyName.AsSpan().ContainsSpecialCharacters())
				{
					sb.Append("['");
					sb.Append(propertyName);
					sb.Append("']");
				}
				else
				{
					sb.Append('.');
					sb.Append(propertyName);
				}
			}
		}
		static void AppendStackFrame(StringBuilder sb, ref ReadStackFrame frame)
		{
			string propertyName2 = GetPropertyName(ref frame);
			AppendPropertyName(sb, propertyName2);
			if (frame.JsonTypeInfo != null && frame.IsProcessingEnumerable() && frame.ReturnValue is IEnumerable enumerable2 && (frame.ObjectState == StackFrameObjectState.None || frame.ObjectState == StackFrameObjectState.CreatedObject || frame.ObjectState == StackFrameObjectState.ReadElements))
			{
				sb.Append('[');
				sb.Append(GetCount(enumerable2));
				sb.Append(']');
			}
		}
		static int GetCount(IEnumerable enumerable)
		{
			if (enumerable is ICollection collection)
			{
				return collection.Count;
			}
			int num = 0;
			IEnumerator enumerator = enumerable.GetEnumerator();
			while (enumerator.MoveNext())
			{
				num++;
			}
			return num;
		}
		static string GetPropertyName(ref ReadStackFrame frame)
		{
			string result = null;
			byte[] array = frame.JsonPropertyName;
			if (array == null)
			{
				if (frame.JsonPropertyNameAsString != null)
				{
					result = frame.JsonPropertyNameAsString;
				}
				else
				{
					array = frame.JsonPropertyInfo?.NameAsUtf8Bytes ?? frame.CtorArgumentState?.JsonParameterInfo?.NameAsUtf8Bytes;
				}
			}
			if (array != null)
			{
				result = JsonHelpers.Utf8GetString(array);
			}
			return result;
		}
	}

	public JsonTypeInfo GetTopJsonTypeInfoWithParameterizedConstructor()
	{
		for (int i = 0; i < _count - 1; i++)
		{
			if (_stack[i].JsonTypeInfo.UsesParameterizedConstructor)
			{
				return _stack[i].JsonTypeInfo;
			}
		}
		return Current.JsonTypeInfo;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetConstructorArgumentState()
	{
		if (Current.JsonTypeInfo.UsesParameterizedConstructor)
		{
			ref ArgumentState ctorArgumentState = ref Current.CtorArgumentState;
			if (ctorArgumentState == null)
			{
				ctorArgumentState = new ArgumentState();
			}
		}
	}
}
