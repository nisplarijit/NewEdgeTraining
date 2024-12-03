#region Using directives
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.UI;
using System.Collections.Generic;
using FTOptix.HMIProject;
using System;
using System.Linq;
using FTOptix.RAEtherNetIP;
using FTOptix.Alarm;
using FTOptix.SerialPort;
#endregion

public class AlarmFilterLogic : BaseNetLogic
{
    public override void Start()
    {
        alarmFilter = new AlarmFilter(Owner);
    }

    public override void Stop()
    {
        alarmFilter.SaveAll();
    }

    [ExportMethod]
    public void Filter(string filterBrowseName)
    {
        alarmFilter.IsValidFilterBrowseName(filterBrowseName);
        alarmFilter.Refresh();
    }

    [ExportMethod]
    public void Refresh()
    {
        alarmFilter.Refresh();
    }

    [ExportMethod]
    public void ClearAll()
    {
        alarmFilter.ClearFilters();
        alarmFilter.Refresh();
    }

    private sealed class AlarmFilter
    {
        public AlarmFilter(IUANode owner)
        {
            Owner = owner;

            try
            {
                filterConfiguration = AlarmFilterEditModelLogic.GetEditModel(AlarmWidgetEditModel, "FiltersConfiguration");
            }
            catch (CoreConfigurationException ex)
            {
                Log.Warning("Filters configuration in AlarmWidgetEditModel not found: " + ex.Message);
            }

            InitializeAlarmFilterData();

            queryBuilder.Query = AlarmWidget.Get("Layout/AlarmsDataGrid").GetVariable("Query");
            AlarmFilterEditModelLogic.CreateEditModel(AlarmWidgetEditModel, alarmFilterData);

            InitializeCheckBoxes();
            InitializeDateTimePickers();
            InitializeTextBoxes();
        }

        public void SaveAll()
        {
            SaveCheckBoxes();
            SaveDateTimePickers();
            SaveTextBoxes();
        }

        public void IsValidFilterBrowseName(string filterBrowseName)
        {
            if (!alarmFilterData.Filters.Any(x => x.Checkbox.BrowseName == filterBrowseName))
            {
                throw new CoreConfigurationException($"Filter {filterBrowseName} browse name not found");
            }
        }

        public void ClearFilters()
        {
            foreach (var filter in alarmFilterData.Filters)
            {
                filter.Checkbox.Checked = false;
            }
        }

        public void Refresh()
        {
            queryBuilder.BuildQuery(alarmFilterData);
            queryBuilder.RefreshQuery();
        }

        public IUANode AlarmWidget
        {
            get
            {
                var aliasNodeId = Owner.GetVariable("ModelAlias").Value;
                var alarmWidget = InformationModel.Get(aliasNodeId);
                return alarmWidget ?? throw new CoreConfigurationException("ModelAlias node id not found");
            }
        }

        public IUAObject AlarmWidgetEditModel
        {
            get
            {
                var alarmWidgetEditModel = AlarmWidget.GetObject("AlarmWidgetEditModel");
                return alarmWidgetEditModel ?? throw new CoreConfigurationException("AlarmWidgetEditModel object not found");
            }
        }

        private void InitializeAlarmFilterData()
        {
            var baseLayout = Owner.Get("Filters/ScrollView/Layout");

            var fromEventTimePicker = baseLayout.Get("EventTime/Content/VerticalLayout1/HorizontalLayout1/VerticalLayout1").Get<DateTimePicker>(AlarmFilterDataLogic.fromEventTimeDateTimePickerBrowseName);
            var toEventTimePicker = baseLayout.Get("EventTime/Content/VerticalLayout1/HorizontalLayout2/VerticalLayout1").Get<DateTimePicker>(AlarmFilterDataLogic.toEventTimeDateTimePickerBrowseName);
            alarmFilterData.EventTimePickers.Add(AlarmFilterDataLogic.fromEventTimeDateTimePickerBrowseName, fromEventTimePicker);
            alarmFilterData.EventTimePickers.Add(AlarmFilterDataLogic.toEventTimeDateTimePickerBrowseName, toEventTimePicker);
            alarmFilterData.FromSeverityTextBox = baseLayout.Get("Severity/Content/HorizontalLayout1/VerticalLayout1").Get<TextBox>(AlarmFilterDataLogic.fromSeverityBrowseName);
            alarmFilterData.ToSeverityTextBox = baseLayout.Get("Severity/Content/HorizontalLayout1/VerticalLayout2").Get<TextBox>(AlarmFilterDataLogic.toSeverityBrowseName);

            GenerateAccordions(baseLayout);

            ProcessAttribute([.. baseLayout.Children]);
        }

        private void GenerateAccordions(IUANode baseLayout)
        {
            foreach (var child in filterConfiguration.Children)
            {
                if (baseLayout.Children.Any(x => x.BrowseName == child.BrowseName))
                    continue;
                var accordion = GenerateAccordion(child);
                baseLayout.Add(accordion);
            }
        }

        private static Accordion GenerateAccordion(IUANode filterConfigurationNode)
        {
            var accordion = InformationModel.Make<Accordion>(filterConfigurationNode.BrowseName);

            accordion.BrowseName = filterConfigurationNode.BrowseName;
            accordion.HorizontalAlignment = HorizontalAlignment.Stretch;
            accordion.VerticalAlignment = VerticalAlignment.Center;
            accordion.RightMargin = 8;
            accordion.Expanded = false;

            //Header
            var label = InformationModel.Make<Label>("Label");
            label.Text = TranslateFilterName(filterConfigurationNode.BrowseName);
            label.LeftMargin = 8;
            accordion.Header.Add(label);

            //Content
            var columnLayout = GenerateColumnLayout(filterConfigurationNode);
            accordion.Content.Add(columnLayout);

            return accordion;
        }

        private static ColumnLayout GenerateColumnLayout(IUANode node)
        {
            var columnLayout = InformationModel.Make<ColumnLayout>(node.BrowseName);


            foreach (var child in node.Children)
            {
                var checkbox = GenerateCheckbox(node.BrowseName, child.BrowseName);
                columnLayout.Children.Add(checkbox);
            }

            columnLayout.LeftMargin = 8;
            columnLayout.TopMargin = 8;
            columnLayout.RightMargin = 8;

            return columnLayout;
        }

        private static CheckBox GenerateCheckbox(string parentBrowseName, string browseName)
        {
            var checkBox = InformationModel.Make<CheckBox>(parentBrowseName + browseName);
            checkBox.Text = TranslateFilterName(browseName);
            checkBox.BottomMargin = 8;

            return checkBox;
        }

        private static string TranslateFilterName(string textId)
        {
            var translation = InformationModel.LookupTranslation(new LocalizedText(textId));
            if (!translation.IsEmpty())
            {
                return translation.Text;
            }
            return textId;
        }

        private void ProcessAttribute(IEnumerable<IUANode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node == null) 
                    return;

                if (node is Accordion accordion)
                {
                    if (Enum.TryParse(accordion.BrowseName, out AlarmFilterDataLogic.FilterAttribute attribute))
                    {
                        accordion.Visible = GetVisibilityAccordion(accordion.BrowseName);

                        ProcessContent([.. accordion.Get("Content").Children], attribute, accordion);
                    }
                    else
                    {
                        throw new CoreConfigurationException($"Accordion {accordion.BrowseName} browse name is not a valid FilterAttribute.");
                    }
                }
            }
        }

        private void ProcessContent(IEnumerable<IUANode> nodes, AlarmFilterDataLogic.FilterAttribute attribute, Accordion accordion)
        {
            foreach (var node in nodes)
            {
                if (node == null) 
                    return;

                if (node is ColumnLayout columnLayout)
                {
                    ProcessContent([.. columnLayout.Children], attribute, accordion);
                }

                if (node is RowLayout rowLayout)
                {
                    ProcessContent([.. rowLayout.Children], attribute, accordion);
                }

                if (node is CheckBox checkbox)
                {
                    var sqlCondition = alarmFilterData.PresetSqlConditions.GetValueOrDefault(checkbox.BrowseName) ??
                                       GenerateSqlCondition(checkbox.Text, attribute, checkbox.BrowseName);
                    SetVisibilityCheckbox(checkbox, attribute.ToString());
                    alarmFilterData.Filters.Add(new AlarmFilterDataLogic.Filter(checkbox, attribute, sqlCondition, accordion));
                }
            }
        }

        private bool GetVisibilityAccordion(string browseName)
        {
            var config = filterConfiguration.GetVariable(browseName);
            if (config != null)
            {
                return config.Value;
            }
            else
            {
                Log.Warning($"FilterConfiguration not contains configuration for accrodion: {browseName}.");
                return true;
            }
        }

        private void SetVisibilityCheckbox(CheckBox checkbox, string attribute)
        {
            List<string> dataInputs =
            [
                AlarmFilterDataLogic.fromEventTimeBrowseName,
                AlarmFilterDataLogic.toEventTimeBrowseName,
                AlarmFilterDataLogic.severityBrowseName
            ];
            var isVisible = GetVisibilityCheckbox(checkbox.BrowseName, attribute);

            if (dataInputs.Contains(checkbox.BrowseName) && checkbox.Parent is RowLayout rowLayout)
            {
                rowLayout.Visible = isVisible;
            }

            checkbox.Visible = isVisible;
        }

        private bool GetVisibilityCheckbox(string browseName, string attribute)
        {
            var configurationName = browseName.Remove(0, attribute.Length);
            var config = filterConfiguration.Get(attribute).GetVariable(configurationName);
            if (config != null)
            {
                return config.Value;
            }
            else
            {
                Log.Warning($"FilterConfiguration not contains configuration for checkbox: {configurationName} for attribute {attribute}.");
                return true;
            }
        }

        private static string GenerateSqlCondition(string text, AlarmFilterDataLogic.FilterAttribute attribute, string checkboxBrowseName)
        {
            if (attribute == AlarmFilterDataLogic.FilterAttribute.Inhibit)
                return $"ShelvingState.CurrentState = '{text}'";
            if (attribute == AlarmFilterDataLogic.FilterAttribute.Class)
                return $"RAAlarmData.AlarmClass LIKE '%{text}%'";
            if (attribute == AlarmFilterDataLogic.FilterAttribute.Group)
                return $"RAAlarmData.AlarmGroup LIKE '%{text}%'";
            if (attribute == AlarmFilterDataLogic.FilterAttribute.AlarmState)
                return GenerateSqlConditionAlarmState(checkboxBrowseName);

            return $"{attribute} LIKE '%{text}%'";
        }

        private static string GenerateSqlConditionAlarmState(string checkboxBrowseName)
        {
            var highHigh = TranslateFilterName("HighHighState");
            var high = TranslateFilterName("HighState");
            var lowLow = TranslateFilterName("LowLowState");
            var low = TranslateFilterName("LowState");
            var active = TranslateFilterName("Active");
            var inactive = TranslateFilterName("InactiveState");

            if (checkboxBrowseName == "AlarmStateHighHighState")
                return $"CurrentState IN ('{highHigh}','{highHigh} {high}')";
            if (checkboxBrowseName == "AlarmStateHighState")
                return $"CurrentState IN ('{high}','{highHigh} {high}')";
            if (checkboxBrowseName == "AlarmStateLowLowState")
                return $"CurrentState IN ('{lowLow}','{low} {lowLow}')";
            if (checkboxBrowseName == "AlarmStateLowState")
                return $"CurrentState IN ('{low}','{low} {lowLow}')";
            if (checkboxBrowseName == "AlarmStateActiveState")
                return $"CurrentState IN ('{active}')";
            if (checkboxBrowseName == "AlarmStateInactiveState")
                return $"CurrentState IN ('{inactive}')";

            return "";
        }

        private void InitializeDateTimePickers()
        {
            InitializeDateTimePicker(AlarmFilterDataLogic.fromEventTimeDateTimePickerBrowseName, AlarmFilterDataLogic.fromEventTimeBrowseName);
            InitializeDateTimePicker(AlarmFilterDataLogic.toEventTimeDateTimePickerBrowseName, AlarmFilterDataLogic.toEventTimeBrowseName);
        }

        private void InitializeDateTimePicker(string dateTimePickerBrowseName, string checkboxBrowseName)
        {
            var filter = alarmFilterData.Filters.First(x => x.Checkbox.BrowseName == checkboxBrowseName &&
                                             x.Attribute == AlarmFilterDataLogic.FilterAttribute.EventTime);

            if (filter.Checkbox.Checked)
                alarmFilterData.EventTimePickers.GetValueOrDefault(dateTimePickerBrowseName).Value = GetFiltersModelVariable(dateTimePickerBrowseName).Value;
            else
                alarmFilterData.EventTimePickers.GetValueOrDefault(dateTimePickerBrowseName).Value = DateTime.Now;
        }

        private void InitializeTextBoxes()
        {
            var severityFilter = alarmFilterData.Filters.First(x => x.Checkbox.BrowseName == AlarmFilterDataLogic.severityBrowseName &&
                                                    x.Attribute == AlarmFilterDataLogic.FilterAttribute.Severity);

            if (severityFilter.Checkbox.Checked)
            {
                alarmFilterData.FromSeverityTextBox.Text = GetFiltersModelVariable(AlarmFilterDataLogic.fromSeverityBrowseName).Value;
                alarmFilterData.ToSeverityTextBox.Text = GetFiltersModelVariable(AlarmFilterDataLogic.toSeverityBrowseName).Value;
            }
            else
            {
                alarmFilterData.FromSeverityTextBox.Text = "1";
                alarmFilterData.ToSeverityTextBox.Text = "1000";
            }
        }

        private void InitializeCheckBoxes()
        {
            foreach (var (filter, isChecked) in from filter in alarmFilterData.Filters
                                                let isChecked = GetFiltersModelVariable(filter.Checkbox.BrowseName).Value
                                                select (filter, isChecked))
            {
                filter.Checkbox.Checked = isChecked;

                if (isChecked)
                    ExpandAccordion(filter);
            }
        }

        private static void ExpandAccordion(AlarmFilterDataLogic.Filter filter)
        {
            filter.Accordion.Expanded = true;
        }

        private void SaveCheckBoxes()
        {
            var checkboxes = alarmFilterData.Filters.Select(x => x.Checkbox).ToList();
            foreach (var checkbox in checkboxes)
            {
                GetFiltersModelVariable(checkbox.BrowseName).Value = checkbox.Checked;
            }
        }

        private void SaveDateTimePickers()
        {
            foreach (var timePicker in alarmFilterData.EventTimePickers)
            {
                GetFiltersModelVariable(timePicker.Key).Value = timePicker.Value.Value;
            }
        }

        private void SaveTextBoxes()
        {
            GetFiltersModelVariable(AlarmFilterDataLogic.fromSeverityBrowseName).Value = alarmFilterData.FromSeverityTextBox.Text;
            GetFiltersModelVariable(AlarmFilterDataLogic.toSeverityBrowseName).Value = alarmFilterData.ToSeverityTextBox.Text;
        }

        private IUAVariable GetFiltersModelVariable(string browseName)
        {
            var filtersModel = AlarmFilterEditModelLogic.GetEditModel(AlarmWidgetEditModel);
            return filtersModel.GetVariable(browseName) ?? throw new CoreConfigurationException($"FilterModel {browseName} variable not found");
        }

        private readonly AlarmFilterDataLogic alarmFilterData = new();
        private readonly AlarmFilterQueryBuilderLogic queryBuilder = new();
        private readonly IUANode Owner;
        private readonly IUAObject filterConfiguration;
    }

    private AlarmFilter alarmFilter;
}
