using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maximus.Connectivity.SQLExtension.Modules
{
  class SQLServerSyntheticTransactionResults: PropertyBagObject
  {
    public string SQLConnectStatus { get; set; } = "NONE";
    public string SchemaListStatus { get; set; } = "NONE";
    public string TransactionStatus { get; set; } = "NONE";
    public double SQLConnectTime { get; set; } = -1;
    public double SchemaListTime { get; set; } = -1;
    public double TransactionTime { get; set; } = -1;
    public string Message { get; set; } = "";
    public string ConnectionString { get; set; } = "";
    public string Username { get; set; } = "";
  }
}
