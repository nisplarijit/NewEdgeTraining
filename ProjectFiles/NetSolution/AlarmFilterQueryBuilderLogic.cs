#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.DataLogger;
using FTOptix.Store;
using FTOptix.Alarm;
using FTOptix.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTOptix.RAEtherNetIP;
using FTOptix.SerialPort;
#endregion

public class AlarmFilterQueryBuilderLogic : BaseNetLogic
{
    public IUAVariable Query { get; set; }

    public void RefreshQuery()
    {
        Query.Value = newQuery.ToString();
    }

    public void BuildQuery(AlarmFilterDataLogic alarmFilterData)
    {
        newQuery.Clear();
        newQuery.Append(mandatorySQLpart);
        var wasWhereAdded = false;
        string groupStatement;

        var activeAttributes = alarmFilterData.Filters.FindAll(x => x.Checkbox.Checked)
                                                      .Select(x => x.Attribute)
                                                      .Distinct()
                                                      .ToList();

        foreach (var attribute in activeAttributes)
        {
            groupStatement = BuildStatementForGroup(attribute, alarmFilterData);

            if (!string.IsNullOrEmpty(groupStatement))
            {
                if (!wasWhereAdded)
                {
                    newQuery.Append(Where);
                    wasWhereAdded = true;
                }

                newQuery.Append(groupStatement);
                newQuery.Append(And);
            }
        }

        // remove trailing " AND "
        if (wasWhereAdded)
            newQuery.Remove(newQuery.Length - And.Length, And.Length);
    }

    private static string BuildStatementForGroup(AlarmFilterDataLogic.FilterAttribute attribute, AlarmFilterDataLogic alarmFilterData)
    {
        StringBuilder result = new();
        var activeGroupFiltersCounter = 0;
        var isFromEventTimeChecked = false;

        var activeFilters = alarmFilterData.Filters.FindAll(x => x.Checkbox.Checked && x.Attribute == attribute);

        foreach (var filter in activeFilters)
        {
            if (!string.IsNullOrEmpty(filter.SqlCondition))
            {
                result.Append(filter.SqlCondition);
            }
            else
            {
                if (filter.Checkbox.BrowseName.Equals(AlarmFilterDataLogic.fromEventTimeBrowseName))
                {
                    isFromEventTimeChecked = true;
                    result.Append("(Time >= \"");
                    result.Append(alarmFilterData.EventTimePickers.GetValueOrDefault(AlarmFilterDataLogic.fromEventTimeDateTimePickerBrowseName).Value.ToUniversalTime().ToString("o"));
                    result.Append("\")");
                }
                else if (filter.Checkbox.BrowseName.Equals(AlarmFilterDataLogic.toEventTimeBrowseName))
                {
                    if (isFromEventTimeChecked)
                    {
                        // replace ") OR " to " AND "
                        result.Remove(result.Length - ClosingBracketOr.Length, ClosingBracketOr.Length);
                        result.Append(And);
                        result.Append("Time < \"");
                        result.Append(alarmFilterData.EventTimePickers.GetValueOrDefault(AlarmFilterDataLogic.toEventTimeDateTimePickerBrowseName).Value.ToUniversalTime().AddSeconds(1).ToString("o"));
                        result.Append("\")");
                    }
                    else
                    {
                        result.Append("(Time < \"");
                        result.Append(alarmFilterData.EventTimePickers.GetValueOrDefault(AlarmFilterDataLogic.toEventTimeDateTimePickerBrowseName).Value.ToUniversalTime().AddSeconds(1).ToString("o"));
                        result.Append("\")");
                    }
                }
                else if (filter.Checkbox.BrowseName.Equals(AlarmFilterDataLogic.severityBrowseName) &&
                    Int32.TryParse(alarmFilterData.FromSeverityTextBox.Text, out int fromSeverity) &&
                    Int32.TryParse(alarmFilterData.ToSeverityTextBox.Text, out int toSeverity))
                {
                    result.Append("(Severity >= ");
                    result.Append(fromSeverity);
                    result.Append(And);
                    result.Append("Severity <= ");
                    result.Append(toSeverity);
                    result.Append(')');
                }
                else
                {
                    throw new CoreConfigurationException("SQL condition cannot be empty.");
                }
            }

            result.Append(Or);
            activeGroupFiltersCounter++;
        }

        // remove trailing " OR "
        if (result.Length > 0)
            result.Remove(result.Length - Or.Length, Or.Length);

        if (activeGroupFiltersCounter >= 2)
        {
            result.Insert(0, "(");
            result.Append(')');
        }

        return result.ToString();
    }

    private readonly StringBuilder newQuery = new StringBuilder(mandatorySQLpart, 1024);
    private static readonly string mandatorySQLpart = "SELECT * FROM Model";
    private static readonly string Or = " OR ";
    private static readonly string ClosingBracketOr = ") OR ";
    private static readonly string And = " AND ";
    private static readonly string Where = " WHERE ";
}
