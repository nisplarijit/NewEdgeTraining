#region Using directives
using FTOptix.NetLogic;
using FTOptix.UI;
using System.Collections.Generic;
using FTOptix.RAEtherNetIP;
using FTOptix.Alarm;
using FTOptix.SerialPort;
#endregion

public class AlarmFilterDataLogic : BaseNetLogic
{
    public enum FilterAttribute
    {
        AlarmState,
        BrowseName,
        Class,
        EventTime,
        Group,
        Inhibit,
        Message,
        Priority,
        Severity,
        Status
    }

    public struct Filter(CheckBox checkbox, FilterAttribute attribute, string sqlCondition, Accordion accordion)
    {
        public CheckBox Checkbox = checkbox;
        public FilterAttribute Attribute = attribute;
        public string SqlCondition = sqlCondition;
        public Accordion Accordion = accordion;
    }

    public List<Filter> Filters { get => filters; set => filters = value; }
    public Dictionary<string, DateTimePicker> EventTimePickers { get => eventTimePickers; set => eventTimePickers = value; }
    public Dictionary<string, string> PresetSqlConditions { get => presetSqlConditions; }
    public TextBox FromSeverityTextBox { get; set; }
    public TextBox ToSeverityTextBox { get; set; }

    public static readonly string fromEventTimeBrowseName = "EventTimeFromEventTime";
    public static readonly string toEventTimeBrowseName = "EventTimeToEventTime";
    public static readonly string fromEventTimeDateTimePickerBrowseName = "FromEventTimeDateTimePicker";
    public static readonly string toEventTimeDateTimePickerBrowseName = "ToEventTimeDateTimePicker";
    public static readonly string severityBrowseName = "SeveritySeverity";
    public static readonly string fromSeverityBrowseName = "FromSeverity";
    public static readonly string toSeverityBrowseName = "ToSeverity";
    public static readonly string noFilterBrowseName = "NoFilter";

    private Dictionary<string, DateTimePicker> eventTimePickers = [];
    private List<Filter> filters = [];
    private static readonly Dictionary<string, string> presetSqlConditions = new()
    {
        { "PriorityUrgent", "(Severity >= 751 AND Severity <= 1000)" },
        { "PriorityHigh", "(Severity >= 501 AND Severity <= 750)" },
        { "PriorityMedium", "(Severity >= 251 AND Severity <= 500)" },
        { "PriorityLow", "(Severity >= 1 AND Severity <= 250)" },
        { "StatusNormalUnacked", "(ActiveState.Id = 0 AND AckedState.Id = 0)" },
        { "StatusInAlarm", "ActiveState.Id = 1" },
        { "StatusInAlarmAcked", "(ActiveState.Id = 1 AND AckedState.Id = 1)" },
        { "StatusInAlarmUnacked", "(ActiveState.Id = 1 AND AckedState.Id = 0)" },
        { "StatusInAlarmConfirmed", "(ActiveState.Id = 1 AND ConfirmedState.Id = 1)" },
        { "StatusInAlarmUnconfirmed", "(ActiveState.Id = 1 AND ConfirmedState.Id = 0)" },
        { "InhibitEnabled", "EnabledState.Id = 1" },
        { "InhibitDisabled", "EnabledState.Id = 0" },
        { "InhibitSuppressed", "SuppressedState.Id = 1" },
        { "InhibitUnsuppressed", "SuppressedState.Id = 0" },
        { "SeveritySeverity", ""},
        { "EventTimeFromEventTime", ""},
        { "EventTimeToEventTime", ""}
    };
}
