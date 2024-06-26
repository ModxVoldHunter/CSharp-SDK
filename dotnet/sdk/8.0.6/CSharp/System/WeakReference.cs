using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class WeakReference : ISerializable
{
	private nint _taggedHandle;

	public virtual bool TrackResurrection => IsTrackResurrection();

	internal nint WeakHandle
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			nint taggedHandle = _taggedHandle;
			if ((taggedHandle & 2) != 0)
			{
				return ComAwareWeakReference.GetWeakHandle(taggedHandle);
			}
			return taggedHandle & ~(nint)3;
		}
	}

	public virtual bool IsAlive
	{
		get
		{
			nint weakHandle = WeakHandle;
			if (weakHandle == 0)
			{
				return false;
			}
			bool result = GCHandle.InternalGet(weakHandle) != null;
			GC.KeepAlive(this);
			return result;
		}
	}

	public virtual object? Target
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			nint num = _taggedHandle & ~(nint)1;
			if (num == 0)
			{
				return null;
			}
			object target;
			if ((num & 2) != 0)
			{
				target = ComAwareWeakReference.GetTarget(num);
				GC.KeepAlive(this);
				return target;
			}
			target = GCHandle.InternalGet(num);
			GC.KeepAlive(this);
			return target;
		}
		set
		{
			nint num = _taggedHandle & ~(nint)1;
			if (num == 0)
			{
				throw new InvalidOperationException(SR.InvalidOperation_HandleIsNotInitialized);
			}
			ComAwareWeakReference.ComInfo comInfo = ComAwareWeakReference.ComInfo.FromObject(value);
			if ((num & 2) != 0 || comInfo != null)
			{
				ComAwareWeakReference.SetTarget(ref _taggedHandle, value, comInfo);
				GC.KeepAlive(this);
			}
			else
			{
				GCHandle.InternalSet(num, value);
				GC.KeepAlive(this);
			}
		}
	}

	public WeakReference(object? target)
		: this(target, trackResurrection: false)
	{
	}

	public WeakReference(object? target, bool trackResurrection)
	{
		Create(target, trackResurrection);
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected WeakReference(SerializationInfo info, StreamingContext context)
	{
		ArgumentNullException.ThrowIfNull(info, "info");
		object value = info.GetValue("TrackedObject", typeof(object));
		bool boolean = info.GetBoolean("TrackResurrection");
		Create(value, boolean);
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		ArgumentNullException.ThrowIfNull(info, "info");
		info.AddValue("TrackedObject", Target, typeof(object));
		info.AddValue("TrackResurrection", IsTrackResurrection());
	}

	private void Create(object target, bool trackResurrection)
	{
		nint num = GCHandle.InternalAlloc(target, trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
		_taggedHandle = (trackResurrection ? (num | 1) : num);
		ComAwareWeakReference.ComInfo comInfo = ComAwareWeakReference.ComInfo.FromObject(target);
		if (comInfo != null)
		{
			ComAwareWeakReference.SetComInfoInConstructor(ref _taggedHandle, comInfo);
		}
	}

	private bool IsTrackResurrection()
	{
		return (_taggedHandle & 1) != 0;
	}

	~WeakReference()
	{
		nint num = _taggedHandle & ~(nint)3;
		if (num != 0)
		{
			GCHandle.InternalFree(num);
			_taggedHandle &= 1;
		}
	}
}
[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class WeakReference<T> : ISerializable where T : class?
{
	private nint _taggedHandle;

	private T? Target
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			nint num = _taggedHandle & ~(nint)1;
			if (num == 0)
			{
				return null;
			}
			T result;
			if ((num & 2) != 0)
			{
				result = Unsafe.As<T>(ComAwareWeakReference.GetTarget(num));
				GC.KeepAlive(this);
				return result;
			}
			result = Unsafe.As<T>(GCHandle.InternalGet(num));
			GC.KeepAlive(this);
			return result;
		}
	}

	public WeakReference(T target)
		: this(target, trackResurrection: false)
	{
	}

	public WeakReference(T target, bool trackResurrection)
	{
		Create(target, trackResurrection);
	}

	private WeakReference(SerializationInfo info, StreamingContext context)
	{
		ArgumentNullException.ThrowIfNull(info, "info");
		T target = (T)info.GetValue("TrackedObject", typeof(T));
		bool boolean = info.GetBoolean("TrackResurrection");
		Create(target, boolean);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryGetTarget([MaybeNullWhen(false)][NotNullWhen(true)] out T target)
	{
		return (target = Target) != null;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		ArgumentNullException.ThrowIfNull(info, "info");
		info.AddValue("TrackedObject", Target, typeof(T));
		info.AddValue("TrackResurrection", IsTrackResurrection());
	}

	private bool IsTrackResurrection()
	{
		return (_taggedHandle & 1) != 0;
	}

	private void Create(T target, bool trackResurrection)
	{
		nint num = GCHandle.InternalAlloc(target, trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
		_taggedHandle = (trackResurrection ? (num | 1) : num);
		ComAwareWeakReference.ComInfo comInfo = ComAwareWeakReference.ComInfo.FromObject(target);
		if (comInfo != null)
		{
			ComAwareWeakReference.SetComInfoInConstructor(ref _taggedHandle, comInfo);
		}
	}

	public void SetTarget(T target)
	{
		nint num = _taggedHandle & ~(nint)1;
		if (num == 0)
		{
			throw new InvalidOperationException(SR.InvalidOperation_HandleIsNotInitialized);
		}
		ComAwareWeakReference.ComInfo comInfo = ComAwareWeakReference.ComInfo.FromObject(target);
		if ((num & 2) != 0 || comInfo != null)
		{
			ComAwareWeakReference.SetTarget(ref _taggedHandle, target, comInfo);
			GC.KeepAlive(this);
		}
		else
		{
			GCHandle.InternalSet(num, target);
			GC.KeepAlive(this);
		}
	}

	~WeakReference()
	{
	}
}
