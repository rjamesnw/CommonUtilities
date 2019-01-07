// MS Article on filling DataSets: http://support.microsoft.com/kb/314145 
// SQL Server Management Objects: http://davidhayden.com/blog/dave/archive/2006/01/27/2775.aspx

#if USING_MSSQL_SERVER

using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Xml;
using Common;
using Microsoft.SqlServer.Management.Common; // Microsoft.SqlServer.ConnectionInfo.dll
using Microsoft.SqlServer.Management.Smo; // Microsoft.SqlServer.Smo.dll
using SQLSMO = Microsoft.SqlServer.Management.Smo;

namespace Common.Data
{

    /// <summary>
    /// A more convenient way to interact with databases.
    /// </summary>
    public class SQLAdapter
    {
        public static string DefaultDatabase = "";

        protected string fConnectionString = "";
        protected SqlConnection fConn = null;
        public int ConnectCount = 0; // number of connection requests (not disconnection until this reaches 0)

        protected SqlCommand fComm;
        protected SqlDataAdapter fSqlAdapter;
        protected SqlCommandBuilder fSqlBuilder;

        Server fServer;

        public DataSet DSet;
        public string ActiveTableName;
        public string DefaultDateFormatStatements = "SET DATEFORMAT mdy;"; // mdy is the SQL default install format setting (US format).

        /// <summary>
        /// Creates a new SQLAdapter object with the same connection string as the one derived from.
        /// </summary>
        public SQLAdapter Clone()
        {
            SQLAdapter adapter = new SQLAdapter(Database);
            adapter.ConnectionString = fConnectionString;
            return adapter;
        }

        public string ConnectionString
        {
            get { return fConnectionString; }
            protected set { fConnectionString = value; }
        }

        public int CommandTimeout
        {
            get { return fComm.CommandTimeout; }
            set { fComm.CommandTimeout = value; }
        }

        public int ConnectionTimeout
        {
            get { return fConn.ConnectionTimeout; }
            set
            {
                // note: cannot set while connection is open
                if (fConn == null || fConn.State == ConnectionState.Open) return;
                // get connection string and split into an array
                string[] connprops = fConn.ConnectionString.Split(';');
                // search for the timeout property
                string[] name_value;
                bool updated = false;
                for (int i = 0; i < connprops.Length; i++)
                {
                    name_value = connprops[i].Split('=');
                    if (name_value[0].ToLower() == "connection timeout")
                    { connprops[i] = "Connection Timeout=" + value; updated = true; break; }
                }
                // re-join string and add a timeout property if not found
                string newConnStr = string.Join(";", connprops);
                if (!updated) newConnStr += ";Connection Timeout=" + value;
                // update connection string
                fConn.ConnectionString = newConnStr;
            }
        }

        public ConnectionState ConnectionState
        {
            get { return fConn.State; }
        }

        public SqlDataAdapter SqlAdapter
        {
            get { return fSqlAdapter; }
        }

        public DataTable Table
        {
            get { return DSet.Tables[ActiveTableName]; }
        }

        public int RecordCount
        {
            get { return DSet.Tables[ActiveTableName].Rows.Count; }
        }

        public long[] KeyIDs
        {
            // Return a list of table primary key IDs
            // Note: Uses the first primary key found, or else the first field named "id"
            get
            {
                DataTable tbl = Table;
                DataColumn[] columns = tbl.PrimaryKey;
                int i, ord = -1;
                for (i = 0; i < columns.Length; i++)
                    if (columns[i].Unique)
                    { ord = tbl.Columns[i].Ordinal; break; }
                if (ord < 0)
                {   // use any columns named "id" instead
                    i = tbl.Columns.IndexOf("id");
                    if (i >= 0) ord = tbl.Columns[i].Ordinal;
                    else throw new Exception("Table contains no primary keys, nor any field named 'id'.");
                }
                long[] ids = new long[tbl.Rows.Count];
                i = 0;
                foreach (DataRow row in tbl.Rows)
                    ids[i++] = (long)row[ord];
                return ids;
            }
        }

        public Int64 LastInsertID
        {
            get
            {
                string lastCommandText = fComm.CommandText;
                fComm.CommandText = "SELECT @@Identity AS ID";
                Int64 id = Convert.ToInt64(fComm.ExecuteScalar());
                fComm.CommandText = lastCommandText;
                return id;
            }
        }

        public bool Connected // or "is connecting"
        {
            get
            {
                return (fConn.State != ConnectionState.Closed && fConn.State != ConnectionState.Broken);
            }
        }

        public string Database
        {
            get { return (fConn != null) ? (fConn.Database ?? "") : ""; }
            set
            {
                if (fServer != null && fConn != null && fServer.Databases[value] != null)
                    fConn.ChangeDatabase(value); // (exists locally)
                else
                    _SetDatabase(value); // (check for remote connection)
            }
        }

        public SQLAdapter() : this("") { }
        public SQLAdapter(string databaseName)
        {
            if (databaseName == "")
                databaseName = DefaultDatabase;

            _SetDatabase(databaseName.ToLower());

            DSet = new DataSet();

            ConnectionTimeout = 30;
            CommandTimeout = 30;
        }

        private void _SetDatabase(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                // ... this is a default local database connection ...
                if (ConfigurationManager.ConnectionStrings["localhost"] == null)
                    throw new Exception("SQLAdapter(): Default configuration connection string 'localhost' not found.");
                fConnectionString = ConfigurationManager.ConnectionStrings["localhost"].ConnectionString;
            }
            else
            {
                // ... this is a specific database connection ...
                fConnectionString = GetRemoteServerConnectionString(databaseName);

                if (string.IsNullOrEmpty(fConnectionString))
                    throw new InvalidOperationException("SQLAdapter: No connection information exists for database '" + databaseName + "' (it should specified in the web.config file - please asked administration for assistance.).");
            }

            bool wasNotClosed = false;
            if (fConn != null && fConn.State != ConnectionState.Closed)
            { fConn.Close(); wasNotClosed = true; }

            fConn = new SqlConnection(fConnectionString);
            fComm = new SqlCommand("", fConn);
            fServer = new Server(new ServerConnection(fConn));
            fSqlAdapter = new SqlDataAdapter(fComm);

            fSqlAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

            fSqlBuilder = new SqlCommandBuilder(fSqlAdapter);

            if (wasNotClosed)
                fConn.Open();
        }

        public void Connect() //?? rename this
        {
            if (!Connected)
                try { fConn.Open(); }
                catch (Exception ex) { if (fConn.State != ConnectionState.Connecting) throw ex; }
            ConnectCount++;
        }

        public void Disconnect() //?? rename this
        {
            if (ConnectCount > 0) ConnectCount--;
            if (ConnectCount <= 0 && Connected)
            { try { fConn.Close(); } catch { } ConnectCount = 0; }
        }

        public void ForceClosed()
        {
            try { fConn.Close(); }
            catch { }
            ConnectCount = 0;
        }

        public DataTable SelectTable(string tablename)
        {
            ActiveTableName = tablename;
            return DSet.Tables[ActiveTableName];
        }
        public DataTable SelectTable(int i)
        {
            ActiveTableName = DSet.Tables[i].TableName;
            return DSet.Tables[i];
        }

        public static string GetRemoteServerName(string databaseName)
        { return ConfigurationManager.AppSettings[databaseName + "_server"]; }
        private string _GetRemoteServerName() { return GetRemoteServerName(Database); }

        public static string GetRemoteServerConnectionString(string databaseName)
        {
            string server = GetRemoteServerName(databaseName);
            if (string.IsNullOrEmpty(server)) return "";
            string templateConnStr = ConfigurationManager.AppSettings["dbserver_connection_string_template"];
            if (string.IsNullOrEmpty(templateConnStr))
                throw new ConnectionException("The template connection string ('dbserver_connection_string_template') is missing from 'appSettings' within the 'Web.Config' file. When creating the template connection string, put '{server}' (without the quotes) where the server name/IP goes, and '{database}' where the database name goes.");
            templateConnStr = templateConnStr.Replace("{server}", server);
            templateConnStr = templateConnStr.Replace("{database}", databaseName);
            return templateConnStr;
        }
        private string _GetRemoteServerConnectionString() { return GetRemoteServerConnectionString(Database); }

        private string _GetLinkedServerName(string databaseName, bool exceptIfExistsLocally)
        {
            // ... go through linked server list and locate the catalogue, if any ...
            if (exceptIfExistsLocally)
            {
                foreach (Database db in fServer.Databases)
                    if (db.Name == databaseName)
                        return "";
            }

            foreach (LinkedServer lsrv in fServer.LinkedServers)
                if (lsrv.Catalog == databaseName)
                    return lsrv.Name;

            return "";
        }
        private string _GetLinkedServerName(string databaseName)
        { return _GetLinkedServerName(databaseName, true); }

        public string TranslateSQLDatabaseReferences(string sql)
        {
            sql = sql.Trim();

            // ... need to auto-detect cross-database queries by looking for ".dbo." sequences, then insert the required linked server alias.
            string[] parts = sql.Split(new string[] { ".dbo." }, StringSplitOptions.None);
            string part, database, linkedServerAlias;
            bool bracketFound;

            for (int i = 0; i < parts.Length - 1; i++) // (note: process all parts, except the last one)
            {
                part = parts[i];
                int offset = part.Length - 1;
                bracketFound = (part[offset] == ']');

                if (bracketFound)
                {
                    while (offset >= 0 && part[offset] != '[') { offset--; }
                    if (offset > 0 && part[offset - 1] == '.') continue; // (a linked server name already exists)
                    if (offset < 0) continue; // (just in case [not found, must be user entry error?]...)
                    database = part.Substring(offset + 1, part.Length - 2 - offset);
                }
                else
                {
                    while (offset >= 0 && part[offset].IsIdent()) { offset--; }
                    if (offset >= 0 && part[offset] == '.') continue; // (a linked server name already exists)
                    offset++;
                    database = part.Substring(offset, part.Length - offset);
                }

                linkedServerAlias = _GetLinkedServerName(database, false);
                if (!string.IsNullOrEmpty(linkedServerAlias))
                    parts[i] = part.Insert(offset, linkedServerAlias + ".");
            }

            sql = string.Join(".dbo.", parts);

            return sql;
        }

        /// <summary>
        /// Run an insert, update, or delete query.
        /// </summary>
        /// <param name="sql"></param>
        /// <returns>The number of rows affected.</returns>
        public int RunNonQuery(string sql)
        {
            if (fConn.State != ConnectionState.Open)
                return 0;
            fComm.CommandText = TranslateSQLDatabaseReferences(sql);
            return fComm.ExecuteNonQuery();
        }

        /// <summary>
        /// Run a query that returns a single result (first column of the first record returned).
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public object RunQuery(string sql)
        {
            if (fConn.State != ConnectionState.Open)
                return null;

            if (DefaultDateFormatStatements != "")
            {
                fComm.CommandText = DefaultDateFormatStatements;
                fComm.ExecuteNonQuery();
            }

            fComm.CommandText = TranslateSQLDatabaseReferences(sql);

            try
            {
                return fComm.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new Exception("SQLAdapter Error: Error executing query '" + sql + "'.", ex);
            }
        }

        /// <summary>
        /// Execute a SELECT SQL query.
        /// </summary>
        /// <param name="sql">SQL text.</param>
        /// <param name="resetDSet">Reset datasets (tabes) from all previous queries.</param>
        /// <param name="sourceTableName">The name of a previous result table returned.</param>
        /// <param name="targetTableName">After the query this becomes the new name for the resulting table.</param>
        /// <returns>Number of records.</returns>
        public int RunQuery(string sql, bool resetDSet, string sourceTableName, string targetTableName)
        {
            if (fConn.State != ConnectionState.Open)
                throw new Exception("SQLAdapter.RunQuery(): No open connection.");

            try
            {
                if (DefaultDateFormatStatements != "")
                {
                    fComm.CommandText = DefaultDateFormatStatements;
                    fComm.ExecuteNonQuery();
                }

                fComm.CommandText = TranslateSQLDatabaseReferences(sql);

                if (sourceTableName == "") sourceTableName = "Table";
                if (targetTableName == "") targetTableName = sourceTableName;

                // remove all tables if requested
                if (resetDSet)
                {
                    DSet.Clear();
                    DSet.Reset();
                }

                int i, result;

                // clear existing table before loading records (else new records will append)
                i = DSet.Tables.IndexOf(sourceTableName);
                if (i >= 0) { DSet.Tables[i].Clear(); DSet.Tables[i].Reset(); }

                // get schema and fill table with records
                DataTable[] tablesAdded = fSqlAdapter.FillSchema(DSet, SchemaType.Source, sourceTableName);
                result = fSqlAdapter.Fill(0, int.MaxValue, tablesAdded);

                if (targetTableName != sourceTableName)
                {   // target name is different...
                    // delete target table if already exists
                    i = DSet.Tables.IndexOf(targetTableName);
                    if (i >= 0) DSet.Tables.RemoveAt(i);
                    tablesAdded[0].TableName = targetTableName;
                    ActiveTableName = targetTableName;
                }
                else
                {   // target name is the same as the source
                    ActiveTableName = sourceTableName;
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("SQLAdapter Error: Error executing query '" + sql + "'.", ex);
            }
        }
        /// <summary>
        /// Execute a SELECT SQL query.
        /// </summary>
        /// <param name="sql">SQL text.</param>
        /// <param name="resetDSet">Reset datasets (tables) from all previous queries.</param>
        /// <param name="tablename">A name to give the returned results set (table).</param>
        /// <returns>Number of records.</returns>
        public int RunQuery(string sql, bool resetDSet, string tablename)
        {

            return RunQuery(sql, resetDSet, tablename, "");
        }
        /// <summary>
        /// Execute a SELECT SQL query. Previous query datasets (tables) are not cleared.
        /// </summary>
        /// <param name="sql">SQL text.</param>
        /// <param name="tablename">A name to give the returned results set (table).</param>
        /// <returns>Number of records.</returns>
        public int RunQuery(string sql, string tablename)
        {

            return RunQuery(sql, false, tablename);
        }

        public static DataType GetSQLDataType(Type type, int maxLength)
        {
            if (type == Type.GetType("System.Boolean") || type == Type.GetType("System.bool"))
                return DataType.Bit;
            if (type == Type.GetType("System.Byte") || type == Type.GetType("System.byte"))
                return DataType.TinyInt;
            if (type == Type.GetType("System.SByte"))
                return DataType.SmallInt; // TinyInt is actually a byte value! (0-255)
            if (type == Type.GetType("System.Char"))
                return DataType.Char(1);
            if (type == Type.GetType("System.Int8"))
                return DataType.TinyInt;
            if (type == Type.GetType("System.Int16"))
                return DataType.SmallInt;
            else if (type == Type.GetType("System.Int32"))
                return DataType.Int;
            else if (type == Type.GetType("System.Int64"))
                return DataType.BigInt;
            else if (type == Type.GetType("System.float"))
                return DataType.Float;
            else if (type == Type.GetType("System.Single"))
                return DataType.Float;
            else if (type == Type.GetType("System.Double"))
                return DataType.Real;
            else if (type == Type.GetType("System.Decimal"))
                return DataType.Real;
            else if (type == Type.GetType("System.TimeSpan"))
                return DataType.Timestamp;
            else if (type == Type.GetType("System.DateTime"))
                return DataType.DateTime;
            else if (type == Type.GetType("System.String") || type == Type.GetType("System.string"))
            {
                if (maxLength == 0) maxLength = 50;
                if (maxLength <= 255)
                    return DataType.VarChar(maxLength);
                else
                    return DataType.Text;
            }
            else throw new Exception("The data type '" + type.FullName + "' cannot be translated to an SQL data type.");
            //return DataType.Variant;
        }
        public static DataType GetSQLDataType(Type type) { return GetSQLDataType(type, 255); }

        public void UpdateSchema(string tablename, string schemaSelectQuery)
        {
            String databaseName = fConn.Database;

            // check for "database_name.dbo.table_name" format (NOTE: no SMO support here for cross-server queries)
            String[] tableNameParts = tablename.Split('.');
            if (tableNameParts.Length >= 3)
                databaseName = tableNameParts[tableNameParts.Length - 3]; // go from end in case a server name is also included

            if (schemaSelectQuery == "")
            {
                schemaSelectQuery = TranslateSQLDatabaseReferences("SELECT TOP(0) * FROM " + tablename);
                fComm.CommandText = schemaSelectQuery;
                fComm.ExecuteScalar();
            }

            // add any new columns to database table (using SMO - local database support only)
            Database db = fServer.Databases[databaseName];
            //Server remoteServer = new Server(new ServerConnection(
            SQLSMO.Table srcTable = db.Tables[tableNameParts[tableNameParts.Length - 1]];
            Column newColumn = null;
            foreach (DataColumn col in DSet.Tables[tablename].Columns)
            {
                if (!srcTable.Columns.Contains(col.ColumnName))
                {
                    newColumn = new Column(srcTable, col.ColumnName, GetSQLDataType(col.DataType, col.MaxLength));
                    srcTable.Columns.Add(newColumn);
                    newColumn.Create();
                }
            }
        }
        public void UpdateSchema(string tablename) { UpdateSchema(tablename, ""); }
        public void UpdateSchema() { UpdateSchema(ActiveTableName, ""); }

        /// <summary>
        /// Updates table changes to the database.
        /// </summary>
        /// <param name="tablename">Name of a table to update.</param>
        /// <param name="schemaSelectQuery">A query used to select the table schema only, and which doesn't return any records. If empty, a query will be built from the specified table name.</param>
        public void UpdateDB(string tablename, string schemaSelectQuery)
        {
            fSqlBuilder.RefreshSchema(); // note: removes all the DbCommand objects from the SelectCommand property, and Update/Insert as well.
            fSqlAdapter.SelectCommand = fComm; // restore the command object

            if (tablename != "")
            {
                UpdateSchema(tablename, schemaSelectQuery);

                // update records (remove primary key first)
                fSqlAdapter.Update(DSet, tablename);
            }
            else
            {
                if (schemaSelectQuery != "")
                {
                    fComm.CommandText = TranslateSQLDatabaseReferences(schemaSelectQuery);
                    fComm.ExecuteScalar();
                }
                fSqlAdapter.Update(DSet);
            }
        }
        public void UpdateDB(string tablename) { UpdateDB(tablename, ""); }
        public void UpdateDB() { UpdateDB(ActiveTableName, ""); }

        public DataRow NewRow()
        {
            return DSet.Tables[ActiveTableName].NewRow();
        }

        public DataRow AddRow(DataRow drow)
        {
            DSet.Tables[ActiveTableName].Rows.Add(drow);
            return drow;
        }

        public DataColumn NewColumn(string name, Type datatype)
        {
            return new DataColumn(name, datatype);
        }

        public DataColumn AddColumn(DataColumn dcol)
        {
            DSet.Tables[ActiveTableName].Columns.Add(dcol);
            return dcol;
        }

        public void RemoveTable(String tableName)
        {
            if (tableName == "") tableName = ActiveTableName;
            if (tableName == ActiveTableName)
                ActiveTableName = "";
            DataTable table = DSet.Tables[tableName];
            if (table != null && DSet.Tables.CanRemove(table))
                DSet.Tables.Remove(table);
        }
        public void RemoveTable() { RemoveTable(""); }

        public string GetIDSearchString(string dest_id_field_name)
        {
            // When referencing table IDs from other tables, the
            // format "[tablename]_id" is normally used as a field name.
            // 'dest_id_field_name' is that name. If empty, it will be
            // auto-generated based on the currently active Table.
            // Returns "[table]_id=# OR [table]_id=# OR..." for use 
            // in querying. 
            if (dest_id_field_name == "")
                dest_id_field_name = Table.TableName + "_id";
            long[] ids = KeyIDs; // get list of primary key IDs from last query
            string where = "";
            foreach (long val in ids)
            {
                if (where == "")
                    where = dest_id_field_name + "=" + val;
                else
                    where += " OR " + dest_id_field_name + "=" + val;
            }
            return where;
        }

        public bool TableExists(string name)
        {
            //return ((int)RunQuery("IF OBJECT_ID('"+name.Replace("'","''")+"', 'U') IS NOT NULL (select 1) ELSE (select 0);") == 1);
            Database db = fServer.Databases[fConn.Database]; // use current connection database name
            return db.Tables.Contains(name);
        }

        /// <summary>
        /// Attempts to duplicate one table to another. Indexes are also
        /// copied. The primary key index is expected to be in the format
        /// "PK_[source_table_name]". This is detected and converted into
        /// "PK_[destination_table_name]".
        /// </summary>
        /// <param name="sourceTableName"></param>
        /// <param name="newTableName"></param>
        public void CloneTable(string sourceTableName, string newTableName)
        {
            Connect();
            Database db = fServer.Databases[fConn.Database];
            SQLSMO.Table srcTable = db.Tables[sourceTableName];
            //string tableRef = srcTable.ToString(); // format returned: [dbo].[tablename]
            string tsql = Utilities.ToString(srcTable.Script(), "\r\n");
            tsql = tsql.Replace("[" + sourceTableName + "]", "[" + newTableName + "]");
            RunNonQuery(tsql);
            db.Tables.Refresh();
            // Copy source table indexes
            SQLSMO.Table destTable = db.Tables[newTableName];
            string[] tsqlParts;
            int i;
            foreach (Index idx in srcTable.Indexes)
            {
                tsql = Utilities.ToString(idx.Script(), "\r\n");
                tsqlParts = tsql.Split(' ');
                // update table name
                i = Utilities.IndexOf("TABLE", tsqlParts, false);
                if (i >= 0 && i < tsqlParts.Length)
                    tsqlParts[i + 1] = tsqlParts[i + 1].Replace(sourceTableName, newTableName);
                // update constraint name
                i = Utilities.IndexOf("CONSTRAINT", tsqlParts, false);
                if (i >= 0 && i < tsqlParts.Length)
                    tsqlParts[i + 1] = tsqlParts[i + 1].Replace(sourceTableName, newTableName);
                tsql = string.Join(" ", tsqlParts);
                RunNonQuery(tsql);
            }
            db.Tables.Refresh();
            Disconnect();
        }

        public DataTable GetTableSchema(string tableName)
        {
            RunQuery("SELECT TOP(0) * FROM " + tableName, false, tableName);
            return Table;
        }

        public XmlNode Table2XML(XmlDocument xmlDoc, DataTable table, string namespaceURI)
        {
            if (xmlDoc == null) throw new Exception("Table2XML(): 'xmlDoc' is required,");
            if (table == null) table = Table;
            XmlNode rootNode = xmlDoc.CreateNode(XmlNodeType.Element, table.TableName, namespaceURI);
            //??XmlAttribute attrib = xmlDoc.CreateAttribute("name");
            //??attrib.Value = table.TableName;
            //??rootNode.Attributes.Append(attrib);
            //??attrib = xmlDoc.CreateAttribute("recordCount");

            // loop through and store column information
            XmlNode node = null, columsNode = xmlDoc.CreateNode(XmlNodeType.Element, "Columns", namespaceURI);
            foreach (DataColumn col in table.Columns)
            {
                node = xmlDoc.CreateNode(XmlNodeType.Element, "Column", namespaceURI);
                node.Attributes.Append(Utilities.XML_CreateAttribute(xmlDoc, "type", col.DataType.Name));
                node.Attributes.Append(Utilities.XML_CreateAttribute(xmlDoc, "unique", col.Unique));
                node.Attributes.Append(Utilities.XML_CreateAttribute(xmlDoc, "autoIncrement", col.AutoIncrement));
                node.Attributes.Append(Utilities.XML_CreateAttribute(xmlDoc, "allowNull", col.AllowDBNull));
                node.Attributes.Append(Utilities.XML_CreateAttribute(xmlDoc, "index", col.Ordinal));
                node.InnerText = col.ColumnName;
                columsNode.AppendChild(node);
            }

            rootNode.AppendChild(columsNode);

            // loop through records and create record elements
            XmlNode recNode = null, recordsNode = xmlDoc.CreateNode(XmlNodeType.Element, "Records", namespaceURI);
            recordsNode.Attributes.Append(Utilities.XML_CreateAttribute(xmlDoc, "count", table.Rows.Count));
            foreach (DataRow row in table.Rows)
            {
                recNode = xmlDoc.CreateNode(XmlNodeType.Element, "Record", namespaceURI);
                // loop through fields and create field elements
                foreach (DataColumn col in table.Columns)
                {
                    node = xmlDoc.CreateNode(XmlNodeType.Element, col.ColumnName, namespaceURI);
                    // ... need to format dates into the ISO standard format before sending to the client (i.e. yyyyMMddHHmmss) - default is "mdy" style ...
                    if (col.DataType != typeof(DateTime))
                        node.InnerText = Utilities.ND(row[col.Ordinal], "");
                    else
                        node.InnerText = Utilities.FormatDateTime(row[col.Ordinal]);
                    recNode.AppendChild(node);
                }
                recordsNode.AppendChild(recNode);
            }

            rootNode.AppendChild(recordsNode);

            return rootNode;
        }
        public XmlNode Table2XML(XmlDocument xmlDoc, string namespaceURI) { return Table2XML(xmlDoc, null, namespaceURI); }
    }
}

#endif
