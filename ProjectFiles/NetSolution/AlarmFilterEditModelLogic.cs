#region Using directives
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.NetLogic;
using FTOptix.Alarm;
using FTOptix.System;
using FTOptix.RAEtherNetIP;
using FTOptix.SerialPort;
#endregion

public class AlarmFilterEditModelLogic : BaseNetLogic
{
    public static void CreateEditModel(IUAObject parentNode, AlarmFilterDataLogic filtersData, string editModelBrowseName = DefaultEditModelBrowseName)
    {
        FilterEditModel.Create(parentNode, filtersData, editModelBrowseName);
    }

    public static IUAObject GetEditModel(IUAObject parentNode, string editModelBrowseName = DefaultEditModelBrowseName)
    {
        var filterEditModel = parentNode.GetObject(editModelBrowseName);
        return filterEditModel ?? throw new CoreConfigurationException($"Edit model {editModelBrowseName} filters not found");
    }

    public static void DeleteEditModels(IUAObject parentNode)
    {
        FilterEditModel.Delete(parentNode);
    }

    private static class FilterEditModel
    {
        public static void Create(IUAObject parentNode , AlarmFilterDataLogic filtersData, string editModelBrowseName = DefaultEditModelBrowseName)
        {
            var editModelFilters = parentNode.FindObject(editModelBrowseName);
            if (editModelFilters == null)
            {
                editModelFilters = InformationModel.MakeObject(editModelBrowseName);

                // initalize
                foreach (var filter in filtersData.Filters)
                {
                    editModelFilters.Add(InformationModel.MakeVariable(filter.Checkbox.BrowseName, OpcUa.DataTypes.Boolean));
                }
                editModelFilters.Add(InformationModel.MakeVariable(AlarmFilterDataLogic.fromEventTimeDateTimePickerBrowseName, OpcUa.DataTypes.DateTime));
                editModelFilters.Add(InformationModel.MakeVariable(AlarmFilterDataLogic.toEventTimeDateTimePickerBrowseName, OpcUa.DataTypes.DateTime));
                editModelFilters.Add(InformationModel.MakeVariable(AlarmFilterDataLogic.fromSeverityBrowseName, OpcUa.DataTypes.UInt16));
                editModelFilters.Add(InformationModel.MakeVariable(AlarmFilterDataLogic.toSeverityBrowseName, OpcUa.DataTypes.UInt16));
                parentNode.Add(editModelFilters);
            }
        }

        public static void Delete(IUAObject parentNode, string editModelBrowseName = DefaultEditModelBrowseName)
        {
            var editModelNetworkInterfaces = parentNode.GetObject(editModelBrowseName);
            if (editModelNetworkInterfaces != null)
                parentNode.Remove(editModelNetworkInterfaces);
        }
    }

    private const string DefaultEditModelBrowseName = "AlarmFilters";
}

