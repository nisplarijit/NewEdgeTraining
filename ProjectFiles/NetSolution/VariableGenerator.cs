#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.HMIProject;
using FTOptix.DataLogger;
using FTOptix.UI;
using FTOptix.Alarm;
using FTOptix.EventLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.Modbus;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.RAEtherNetIP;
using FTOptix.Report;
using FTOptix.Recipe;
using FTOptix.WebUI;
using FTOptix.SerialPort;
using FTOptix.MQTTClient;
using FTOptix.S7TCP;
using FTOptix.ODBCStore;
using FTOptix.InfluxDBStoreLocal;
using FTOptix.MicroController;
#endregion

public class VariableGenerator : BaseNetLogic
{
    private PeriodicTask taskPeriodico;
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        taskPeriodico = new PeriodicTask(randomNum, 1500, LogicObject);
        taskPeriodico.Start();
    }

    public void randomNum()
    {
        Random r = new Random();
        if ((bool)Project.Current.GetVariable("Model/RandomEn").Value)
        {
            Project.Current.GetVariable("Model/Tags/Variable1").Value = r.Next(0, 500);
            Project.Current.GetVariable("Model/Tags/Variable2").Value = r.Next(0, 500);
            Project.Current.GetVariable("Model/Tags/Variable3").Value = r.Next(0, 500);
            Project.Current.GetVariable("Model/Tags/Variable4").Value = r.Next(0, 500);
            Project.Current.GetVariable("Model/Tags/Variable5").Value = r.Next(0, 500);
            Project.Current.GetVariable("Model/Tags/Variable6").Value = r.Next(0, 500);
            
        }
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        taskPeriodico.Dispose();
    }
}
