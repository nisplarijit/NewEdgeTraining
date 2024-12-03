#region Using directives
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.Alarm;
using FTOptix.RAEtherNetIP;
using FTOptix.SerialPort;
#endregion

public class AlarmWidgetLogic : BaseNetLogic
{
    public override void Start()
    {
        alarmsDataGridModel = Owner.GetVariable("Layout/AlarmsDataGrid/Model");

        var currentSession = LogicObject.Context.Sessions.CurrentSessionInfo;
        actualLanguageVariable = currentSession.SessionObject.Get<IUAVariable>("ActualLanguage");
        actualLanguageVariable.VariableChange += OnSessionActualLanguageChange;
    }

    public override void Stop()
    {
        actualLanguageVariable.VariableChange -= OnSessionActualLanguageChange;
    }

    public void OnSessionActualLanguageChange(object sender, VariableChangeEventArgs e)
    {
        var dynamicLink = alarmsDataGridModel.GetVariable("DynamicLink");
        if (dynamicLink == null)
            return;

        // Restart the data bind on the data grid model variable to refresh data
        dynamicLink.Stop();
        dynamicLink.Start();
    }

    private IUAVariable alarmsDataGridModel;
    private IUAVariable actualLanguageVariable;
}
