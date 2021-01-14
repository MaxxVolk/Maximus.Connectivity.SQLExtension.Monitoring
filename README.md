# Maximus.Connectivity.SQLExtension.Monitoring
SQL Server connectivity monitoring extension for Maximus Connectivity Monitoring SCOM management pack

This management pack extends Maximus Connectivity Monitoring SCOM managmeent pack by adding SQL Server connectivity monitoring. See https://github.com/MaxxVolk/Maximus.Connectivity.Monitoring for details.

This management pack adds four monitors:
  - Connect
  - Connect slow
  - List available scema
  - Synthetic transaction
  
And three performance collection rules to collect connect, schema select, and synthetic transaction execution time.

Synthetic transaction moniotirng has four modes:
  - Execute a simple query successfully (and if defined, control output row count).
  - Execute non-query successfully
  - Execute scalar and ensure that a value is returned
  - Execute scalar and compare retuned value against defined health values, then trigger/resolve alerts upon query result comparsion.
  
