using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading;

namespace System.Diagnostics;

public abstract class Switch
{
	private readonly string _description;

	private readonly string _displayName;

	private int _switchSetting;

	private volatile bool _initialized;

	private bool _initializing;

	private volatile string _switchValueString = string.Empty;

	private readonly string _defaultValue;

	private object _initializedLock;

	private static readonly List<WeakReference<Switch>> s_switches = new List<WeakReference<Switch>>();

	private static int s_LastCollectionCount;

	private StringDictionary _attributes;

	private object InitializedLock
	{
		get
		{
			if (_initializedLock == null)
			{
				object value = new object();
				Interlocked.CompareExchange<object>(ref _initializedLock, value, (object)null);
			}
			return _initializedLock;
		}
	}

	public string DisplayName => _displayName;

	public string Description => _description ?? string.Empty;

	public StringDictionary Attributes
	{
		get
		{
			Initialize();
			return _attributes ?? (_attributes = new StringDictionary());
		}
	}

	protected int SwitchSetting
	{
		get
		{
			if (!_initialized && InitializeWithStatus())
			{
				OnSwitchSettingChanged();
			}
			return _switchSetting;
		}
		set
		{
			bool flag = false;
			lock (InitializedLock)
			{
				_initialized = true;
				if (_switchSetting != value)
				{
					_switchSetting = value;
					flag = true;
				}
			}
			if (flag)
			{
				OnSwitchSettingChanged();
			}
		}
	}

	public string DefaultValue => _defaultValue;

	public string Value
	{
		get
		{
			Initialize();
			return _switchValueString;
		}
		set
		{
			Initialize();
			_switchValueString = value;
			OnValueChanged();
		}
	}

	public static event EventHandler<InitializingSwitchEventArgs>? Initializing;

	protected Switch(string displayName, string? description)
		: this(displayName, description, "0")
	{
	}

	protected Switch(string displayName, string? description, string defaultSwitchValue)
	{
		_displayName = displayName ?? string.Empty;
		_description = description;
		lock (s_switches)
		{
			_pruneCachedSwitches();
			s_switches.Add(new WeakReference<Switch>(this));
		}
		_defaultValue = defaultSwitchValue;
	}

	private static void _pruneCachedSwitches()
	{
		lock (s_switches)
		{
			if (s_LastCollectionCount == GC.CollectionCount(2))
			{
				return;
			}
			List<WeakReference<Switch>> list = new List<WeakReference<Switch>>(s_switches.Count);
			for (int i = 0; i < s_switches.Count; i++)
			{
				if (s_switches[i].TryGetTarget(out var _))
				{
					list.Add(s_switches[i]);
				}
			}
			if (list.Count < s_switches.Count)
			{
				s_switches.Clear();
				s_switches.AddRange(list);
				s_switches.TrimExcess();
			}
			s_LastCollectionCount = GC.CollectionCount(2);
		}
	}

	protected internal virtual string[]? GetSupportedAttributes()
	{
		return null;
	}

	internal void SetSwitchValues(int switchSetting, string switchValueString)
	{
		Initialize();
		lock (InitializedLock)
		{
			_switchSetting = switchSetting;
			_switchValueString = switchValueString;
		}
	}

	internal void OnInitializing()
	{
		Switch.Initializing?.Invoke(null, new InitializingSwitchEventArgs(this));
		TraceUtils.VerifyAttributes(Attributes, GetSupportedAttributes(), this);
	}

	private void Initialize()
	{
		InitializeWithStatus();
	}

	private bool InitializeWithStatus()
	{
		if (!_initialized)
		{
			lock (InitializedLock)
			{
				if (_initialized || _initializing)
				{
					return false;
				}
				_initializing = true;
				_switchValueString = null;
				try
				{
					OnInitializing();
				}
				catch (Exception)
				{
					_initialized = false;
					_initializing = false;
					throw;
				}
				if (_switchValueString == null)
				{
					_switchValueString = _defaultValue;
					OnValueChanged();
				}
				_initialized = true;
				_initializing = false;
			}
		}
		return true;
	}

	protected virtual void OnSwitchSettingChanged()
	{
	}

	protected virtual void OnValueChanged()
	{
		SwitchSetting = int.Parse(Value, CultureInfo.InvariantCulture);
	}

	internal static void RefreshAll()
	{
		lock (s_switches)
		{
			_pruneCachedSwitches();
			for (int i = 0; i < s_switches.Count; i++)
			{
				if (s_switches[i].TryGetTarget(out var target))
				{
					target.Refresh();
				}
			}
		}
	}

	public void Refresh()
	{
		lock (InitializedLock)
		{
			_initialized = false;
			Initialize();
		}
	}
}
