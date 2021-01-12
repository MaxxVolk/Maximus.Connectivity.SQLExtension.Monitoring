using Maximus.Library.Helpers;
using Maximus.Library.ManagedModuleBase;

using Microsoft.EnterpriseManagement.HealthService;

using System;

namespace Maximus.Connectivity.SQLExtension.Modules
{
  internal static class ModInit
  {
    static ModInit()
    {
    }

    internal static char[] Separators = { ',', ';', '|' };

    const string LogSourceName = "Maximus SQL Connectivity Monitoring Module";
    const int LogBaseEventId = 6240;

    static private LoggingHelper _Logger;
    static internal LoggingHelper Logger => _Logger ?? (_Logger = new LoggingHelper(LogSourceName, LogBaseEventId, EventLoggingLevel.Informational));

    internal const int evtId_SQLServerSyntheticTransactionPA = 0;
    //internal const int evtId_ = 1;
    //internal const int evtId_ = 2;
    //internal const int evtId_ = 3;
    //internal const int evtId_ = 4;
    //internal const int evtId_ = 5;
    //internal const int evtId_ = 6;

    internal static void ModuleErrorSignalReceiver(ModuleErrorSeverity severity, ModuleErrorCriticality criticality, Exception e, string message, object callerInstance)
    {
      Logger.WriteException($"Internal module exception or error.\r\nMessage: {message}\r\nError Severity: {severity}\r\nError Criticality: {criticality}", e ?? new Exception("Unknown exception"), callerInstance);
    }
  }
}
