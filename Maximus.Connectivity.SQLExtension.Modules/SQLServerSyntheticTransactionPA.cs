using Maximus.Library.ManagedModuleBase;

using Microsoft.EnterpriseManagement.HealthService;
using Microsoft.EnterpriseManagement.Mom.Modules.DataItems;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Maximus.Connectivity.SQLExtension.Modules
{
  [MonitoringModule(ModuleType.ReadAction)]
  [ModuleOutput(true)]
  public class SQLServerSyntheticTransactionPA : ModuleBaseSimpleAction<PropertyBagDataItem>
  {
    private string FullyQualifiedDomainName, TestDisplayName, ExtraConnectionString, QueryText;
    private int TargetIndex, MinRows, ExplicitHealthSuccessValue, ExplicitHealthWarningValue, ExplicitHealthCriticalValue;
    private SqlCredential sqlCredential;
    private string SQLConnectionString = "";
    private Guid QueryType;

    private Guid ExplicitHealth         = new Guid("26ac9d86-30ee-b828-51b3-1bfde562c828");
    private Guid ExecuteQuerySuccess    = new Guid("6f0fdd5a-ad73-47f0-8736-5b036040deb4");
    private Guid ExecuteNonQuerySuccess = new Guid("2fc810da-d130-92cf-554e-a024ea497605");
    private Guid ExecuteScalarSuccess   = new Guid("c26c9d8e-a414-3a74-d687-a90975f28e7c");

    public SQLServerSyntheticTransactionPA(ModuleHost<PropertyBagDataItem> moduleHost, XmlReader configuration, byte[] previousState) : base(moduleHost, configuration, previousState)
    {
    }

    protected override void PreInitialize(ModuleHost<PropertyBagDataItem> moduleHost, XmlReader configuration, byte[] previousState)
    {
      ModInit.Logger.AddLoggingSource(GetType(), ModInit.evtId_SQLServerSyntheticTransactionPA);
      base.PreInitialize(moduleHost, configuration, previousState);
    }

    protected override PropertyBagDataItem[] GetOutputData(DataItemBase[] inputDataItems)
    {
      SqlConnection sqlConnection = null;
      try
      {
        SQLServerSyntheticTransactionResults results = new SQLServerSyntheticTransactionResults
        {
          ConnectionString = SQLConnectionString,
          Username = sqlCredential?.UserId ?? Environment.UserName
        };

        if (sqlCredential == null)
          sqlConnection = new SqlConnection(SQLConnectionString);
        else
          sqlConnection = new SqlConnection(SQLConnectionString, sqlCredential);

        DateTime start = DateTime.UtcNow;
        try
        {
          sqlConnection.Open();
        }
        catch (Exception e)
        {
          results.Message = e.Message;
          results.SQLConnectStatus = "FAIL";
          return new PropertyBagDataItem[] { results.GetPropertyBagDataItem() };
        }
        results.SQLConnectStatus = "OK";
        results.SQLConnectTime = DateTime.UtcNow.Subtract(start).TotalMilliseconds;

        try
        {
          using (SqlCommand schemaQuery = new SqlCommand("select * from master.sys.schemas", sqlConnection))
          {
            start = DateTime.UtcNow;
            using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(schemaQuery))
            {
              using (DataSet schemaList = new DataSet())
              {
                sqlDataAdapter.Fill(schemaList);
                results.SchemaListStatus = "OK";
                results.SchemaListTime = DateTime.UtcNow.Subtract(start).TotalMilliseconds;
              }
            }
          }
        }
        catch (Exception e)
        {
          results.Message = e.Message;
          results.SchemaListStatus = "FAIL";
        }

        if (string.IsNullOrWhiteSpace(QueryText))
        {
          results.TransactionStatus = "NONE";
        }
        else
          try
          {
            using (SqlCommand sqlCommand = new SqlCommand(QueryText, sqlConnection))
            {
              start = DateTime.UtcNow;
              if (QueryType == ExecuteScalarSuccess || QueryType == ExplicitHealth)
              {
                object rawResult = sqlCommand.ExecuteScalar();
                results.TransactionTime = DateTime.UtcNow.Subtract(start).TotalMilliseconds;
                if (QueryType == ExplicitHealth) 
                {
                  if (rawResult is int queryResponse)
                  {
                    results.Message = $"SQL Query executed successful. Returned value {queryResponse} was decoded as ";
                    if (queryResponse == ExplicitHealthSuccessValue)
                      results.TransactionStatus = "OK";
                    else if (queryResponse == ExplicitHealthWarningValue)
                      results.TransactionStatus = "WARNING";
                    else if (queryResponse == ExplicitHealthCriticalValue)
                      results.TransactionStatus = "FAIL";
                    else results.TransactionStatus = "NONE";
                    results.Message += results.TransactionStatus;
                  }
                  else
                  {
                    results.TransactionStatus = "FAIL";
                    results.Message = "In Explicit Health mode, the query must return an integer result.";
                  }
                  
                }
                else // ExecuteScalarSuccess
                {
                  results.TransactionStatus = "OK";
                }
              }
              if (QueryType == ExecuteQuerySuccess)
              {
                start = DateTime.UtcNow;
                using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand))
                {
                  using (DataSet returnedData = new DataSet())
                  {
                    sqlDataAdapter.Fill(returnedData);
                    results.TransactionTime = DateTime.UtcNow.Subtract(start).TotalMilliseconds;

                    if (MinRows > 0)
                    {
                      if (returnedData.Tables.Count >= 1)
                      {
                        if (returnedData.Tables[0].Rows.Count < MinRows)
                        {
                          results.TransactionStatus = "WARNING";
                          results.Message = $"Query executed OK, but returned number of rows, which is {returnedData.Tables[0].Rows.Count}, is less than minimum required number, which is {MinRows}.";
                        }
                        else
                          results.TransactionStatus = "OK";
                      }
                      else
                      {
                        results.TransactionStatus = "WARNING";
                        results.Message = "No data returned";
                      }
                    }
                    else
                      results.TransactionStatus = "OK";
                  }
                }
              }
              if (QueryType == ExecuteNonQuerySuccess)
              {
                int affectedRows = sqlCommand.ExecuteNonQuery();
                results.TransactionStatus = "OK";
                results.Message = $"Statement executed successful. Rows affected: {affectedRows}";
                results.TransactionTime = DateTime.UtcNow.Subtract(start).TotalMilliseconds;
              }
            }
          }
          catch (Exception e)
          {
            results.Message = e.Message;
            results.TransactionStatus = "FAIL";
            results.TransactionTime = DateTime.UtcNow.Subtract(start).TotalMilliseconds;
          }

        return new PropertyBagDataItem[] { results.GetPropertyBagDataItem() };
      }
      catch (Exception e)
      {
        ModuleErrorSignalReceiver(ModuleErrorSeverity.DataLoss, ModuleErrorCriticality.ThrowAndContinue, e, $"Failed to perform SQL Server synthetic transaction for the {TestDisplayName} test object related to the {FullyQualifiedDomainName}:{TargetIndex} destination object.");
        return null;
      }
      finally
      {
        if (sqlConnection != null)
          sqlConnection.Dispose();
      }
    }

    protected override void ModuleErrorSignalReceiver(ModuleErrorSeverity severity, ModuleErrorCriticality criticality, Exception e, string message, params object[] extaInfo)
    {
      ModInit.ModuleErrorSignalReceiver(severity, criticality, e, message, this);
    }

    protected override void LoadConfiguration(XmlDocument cfgDoc)
    {
      try
      {
        // base class properties
        LoadConfigurationElement(cfgDoc, "TestDisplayName", out TestDisplayName, "<no test name provided>", false); // for logging purposes only
        // parent class property
        LoadConfigurationElement(cfgDoc, "FullyQualifiedDomainName", out FullyQualifiedDomainName);
        LoadConfigurationElement(cfgDoc, "TargetIndex", out TargetIndex);
        // specific class properties -- none
        LoadConfigurationElement(cfgDoc, "UseShortServerName", out bool boolUseShortServerName, false, false);
        LoadConfigurationElement(cfgDoc, "InstanceName", out string strInstanceName, "", false);
        LoadConfigurationElement(cfgDoc, "DatabaseName", out string strDatabaseName, "", false);
        LoadConfigurationElement(cfgDoc, "ServerPort", out int intServerPort, -1, false);
        LoadConfigurationElement(cfgDoc, "ExtraConnectionString", out ExtraConnectionString, "", false);
        LoadConfigurationElement(cfgDoc, "QueryText", out QueryText, "", false);
        LoadConfigurationElement(cfgDoc, "QueryType", out QueryType, "{00000000-0000-0000-0000-000000000000}", false);
        LoadConfigurationElement(cfgDoc, "MinRows", out MinRows, -1, false);
        LoadConfigurationElement(cfgDoc, "ExplicitHealthSuccessValue", out ExplicitHealthSuccessValue, 0, false);
        LoadConfigurationElement(cfgDoc, "ExplicitHealthWarningValue", out ExplicitHealthWarningValue, 1, false);
        LoadConfigurationElement(cfgDoc, "ExplicitHealthCriticalValue", out ExplicitHealthCriticalValue, 2, false);
        LoadConfigurationElement(cfgDoc, "Username", out string strUsername, "", false);
        LoadConfigurationElement(cfgDoc, "Password", out string strPassword, "", false);
        // parse
        if (!string.IsNullOrEmpty(strUsername))
        {
          SecureString password = new SecureString();
          if (!string.IsNullOrEmpty(strPassword))
            foreach (char c in strPassword)
              password.AppendChar(c);
          password.MakeReadOnly();
          sqlCredential = new SqlCredential(strUsername, password);
        }
        string serverName = FullyQualifiedDomainName;
        if (boolUseShortServerName)
          serverName = FullyQualifiedDomainName.Substring(0, FullyQualifiedDomainName.IndexOf('.') < 0 ? FullyQualifiedDomainName.Length : FullyQualifiedDomainName.IndexOf('.'));
        if (!string.IsNullOrWhiteSpace(strInstanceName))
          serverName += $"\\{strInstanceName}";
        else if (intServerPort > 0)
          serverName += $",{intServerPort}";
        if (!string.IsNullOrEmpty(serverName))
          SQLConnectionString += $"Server={serverName};";
        if (!string.IsNullOrWhiteSpace(strDatabaseName))
          SQLConnectionString += $"Database={strDatabaseName};";
        if (string.IsNullOrEmpty(strUsername))
          SQLConnectionString += "Integrated Security=True;";
      }
      catch (Exception e)
      {
        ModuleErrorSignalReceiver(ModuleErrorSeverity.FatalError, ModuleErrorCriticality.Stop, e, "Failed to load module configuration.");
        throw new ModuleException("Failed to load module configuration.", e);
      }
    }
  }
}
