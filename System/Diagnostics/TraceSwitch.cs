#define TRACE
namespace System.Diagnostics;

[SwitchLevel(typeof(TraceLevel))]
public class TraceSwitch : Switch
{
	public TraceLevel Level
	{
		get
		{
			return (TraceLevel)base.SwitchSetting;
		}
		set
		{
			if (value < TraceLevel.Off || value > TraceLevel.Verbose)
			{
				throw new ArgumentException(System.SR.TraceSwitchInvalidLevel);
			}
			SetSwitchValues((int)value, value.ToString());
		}
	}

	public bool TraceError => Level >= TraceLevel.Error;

	public bool TraceWarning => Level >= TraceLevel.Warning;

	public bool TraceInfo => Level >= TraceLevel.Info;

	public bool TraceVerbose => Level == TraceLevel.Verbose;

	public TraceSwitch(string displayName, string? description)
		: base(displayName, description)
	{
	}

	public TraceSwitch(string displayName, string? description, string defaultSwitchValue)
		: base(displayName, description, defaultSwitchValue)
	{
	}

	protected override void OnSwitchSettingChanged()
	{
		int switchSetting = base.SwitchSetting;
		if (switchSetting < 0)
		{
			Trace.WriteLine(System.SR.Format(System.SR.TraceSwitchLevelTooLow, base.DisplayName));
			base.SwitchSetting = 0;
		}
		else if (switchSetting > 4)
		{
			Trace.WriteLine(System.SR.Format(System.SR.TraceSwitchLevelTooHigh, base.DisplayName));
			base.SwitchSetting = 4;
		}
	}

	protected override void OnValueChanged()
	{
		TraceLevel traceLevel = Enum.Parse<TraceLevel>(base.Value, ignoreCase: true);
		if (traceLevel < TraceLevel.Off)
		{
			traceLevel = TraceLevel.Off;
		}
		else if (traceLevel > TraceLevel.Verbose)
		{
			traceLevel = TraceLevel.Verbose;
		}
		SetSwitchValues((int)traceLevel, traceLevel.ToString());
	}
}
