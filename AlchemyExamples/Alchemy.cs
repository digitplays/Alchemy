using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data;
using System.Security.Cryptography;
using System.Collections;
using System.Text;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Text.Json;
namespace Tome
{
    public class Alchemy
    {

        public static bool Production = false;
        public bool SQL = true;
        public string connectionString = @"";
        public string currentSQLSTRING;
        private readonly string Encryption_Key = "";
        private readonly string TheoreticalSpace = "__0__";

        public bool AutoManageDatabaseStructure = false;


        public async Task Log(string toLog, string FileName = "DefaultLogName")
        {
            Directory.CreateDirectory((AppDomain.CurrentDomain.BaseDirectory + @"Logs\"));
            using (StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + @"Logs\" + FileName + ".Log"))
            {
                await sw.WriteLineAsync(toLog);
            }
        }

        public async Task<T> Select<T>(object input, string TableName)
        {

            ObjectDictionary OD = GetDictionary(input);

            object returnItem = new object();
            string Post_SQL = " Select * From " + TableName + " Where ";
            string Appended_Value = ParamValues(OD);
            if (Appended_Value == "")
            {
                Post_SQL = " Select * From " + TableName;
            }
            string SQL = Post_SQL + Appended_Value;
            Dictionary<string, object> y = new Dictionary<string, object>();
            try
            {
                SqlConnection conn = new SqlConnection(connectionString);
                SqlCommand cmd = new SqlCommand(SQL, conn);
                for (int index = 0; index < OD.ObjectCount; index++)
                {
                    if (OD.ObjectValues[index] != null)
                    {
                        cmd.Parameters.AddWithValue("@" + OD.ObjectParameters[index], OD.ObjectValues[index]);
                    }
                }
                conn.Open();
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.HasRows)
                    {
                        while (rdr.Read())
                        {
                            for (int index = 0; index < OD.ObjectCount; index++)
                            {
                                OD = await ReadRow(rdr, index, OD, TableName);
                            }
                        }
                    }
                    else
                    {
                        conn.Close();
                        OD.ObjectValues = null;
                    }
                }
                conn.Close();

                returnItem = (T)ConvertDictionaryToObject(OD);
            }
            catch (Exception ex)
            {
                await Log(ex.ToString(), sqlLog);
                if (AutoManageDatabaseStructure)
                {
                    await ValidateTable(TableName: TableName, OD: OD, Exception: ex.ToString());
                }
                returnItem = null;
            }
            return (T)returnItem;

        }
        public string sqlLog = "SQLLog";
        public async Task<ObjectDictionary> ReadRow(SqlDataReader rdr, int index, ObjectDictionary OD, string TableName = "")
        {
            try
            {
                if (rdr[OD.ObjectParameters[index]] == null || rdr[OD.ObjectParameters[index]] == System.DBNull.Value)
                {
                    OD.ObjectValues[index] = null;
                }
                else
                {
                    if (OD.ObjectTypes[index] == typeof(DateTime?))
                    {
                        object D_A_T_E = rdr.IsDBNull(rdr.GetOrdinal((OD.ObjectParameters[index]))) ? null : (DateTime?)rdr.GetDateTime(rdr.GetOrdinal((OD.ObjectParameters[index])));
                        if (D_A_T_E != null)
                        {
                            OD.ObjectValues[index] = (DateTime?)D_A_T_E;
                        }
                        else
                        {
                            OD.ObjectValues[index] = null;
                        }
                    }
                    else if (OD.ObjectTypes[index] == typeof(byte[]))
                    {
                        OD.ObjectValues[index] = Array.ConvertAll<byte, byte?>(rdr[OD.ObjectParameters[index]] as byte[],
                        delegate (byte b)
                        {
                            return b;
                        });
                    }
                    else
                    {

                        if (MSSQLMapping.MSSQL_Types_ToCSharp.FirstOrDefault(X => X.TMCSharpType == OD.ObjectTypes[index]) != null)
                        {
                            var t = OD.ObjectTypes[index];


                            if (rdr[OD.ObjectParameters[index]] == null || rdr[OD.ObjectParameters[index]] == System.DBNull.Value)
                            {
                                OD.ObjectValues[index] = null;
                            }

                            t = Nullable.GetUnderlyingType(t);
                            if (t == null)
                            {
                                t = typeof(string);
                            }

                            dynamic changedObj = Convert.ChangeType(rdr[OD.ObjectParameters[index]], t);

                            OD.ObjectValues[index] = changedObj;


                        }
                        else
                        {
                            var vale = Activator.CreateInstance(OD.ObjectTypes[index], true);

                            vale = JsonSerializer.Deserialize(rdr[OD.ObjectParameters[index]].ToString(), OD.ObjectTypes[index]);

                            OD.ObjectValues[index] = vale;
                        }
                    }
                }
                return OD;
            }
            catch (Exception ex)
            {
                await Log(ex.ToString(), sqlLog);
                if (AutoManageDatabaseStructure)
                {
                    await ValidateTable(TableName, OD, Exception: ex.ToString());
                }
                return null;
            }
        }


        public async Task<List<T>> SelectMany<T>(List<T> input, string TableName, bool SelectAll = false, string OverrideSQL = "", SqlCommand OverrideCMD = null)
        {
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            List<T> ReturnItem = new List<T>();
            ObjectDictionary current = null;
            string SQL = "";
            var H = new object();
            try
            {
                for (int dex = 0; dex < input.Count; dex++)
                {
                    H = input[dex];
                    ObjectDictionary OD = new ObjectDictionary();
                    if (current == null)
                    {
                        OD = GetDictionary(H);
                    }
                    else
                    {
                        OD = GetDictionary(H, current);
                    }
                    current = OD;
                    string Post_SQL = "";
                    string Appended_Value = "";
                    SqlCommand cmd = new SqlCommand();
                    if (OverrideCMD == null)
                    {
                        if (!SelectAll)
                        {
                            if (OverrideSQL == "")
                            {
                                Post_SQL = " Select * From " + TableName + " Where ";
                                Appended_Value = ParamValues(OD);
                                if (Appended_Value == "")
                                {
                                    Post_SQL = " Select * From " + TableName;
                                }
                                SQL = Post_SQL + Appended_Value;
                                cmd = new SqlCommand(SQL, conn);
                                for (int index = 0; index < OD.ObjectCount; index++)
                                {
                                    if (OD.ObjectValues[index] != null)
                                    {
                                        cmd.Parameters.AddWithValue("@" + OD.ObjectParameters[index], OD.ObjectValues[index]);
                                    }
                                }
                            }

                            else
                            {
                                SQL = OverrideSQL;
                                cmd = new SqlCommand(SQL, conn);
                            }

                        }
                        else
                        {
                            SQL = " Select * From " + TableName;
                            cmd = new SqlCommand(SQL, conn);
                        }

                    }
                    if (OverrideCMD != null)
                    {
                        cmd = OverrideCMD;
                        cmd.Connection = conn;
                    }


                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            for (int index = 0; index < OD.ObjectCount; index++)
                            {
                                OD = await ReadRow(rdr, index, OD, TableName);
                            }
                            ReturnItem.Add((T)ConvertDictionaryToObject(OD));
                        }
                    }
                }
                conn.Close();
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(System.Exception))
                {

                }
                await Log(ex.ToString(), sqlLog);
                if (AutoManageDatabaseStructure)
                {
                    await ValidateTable(TableName: TableName, OD: current, Exception: ex.ToString());
                }

                ReturnItem = null;
            }
            return (List<T>)(ReturnItem);
        }

        public async Task<bool> Update<T>(T OriginalValue, T NewValue, string TableName, string IdentifierColumn = null)
        {
            try
            {
                ObjectDictionary OD1 = GetDictionary<T>(OriginalValue);
                ObjectDictionary OD2 = GetDictionary<T>(NewValue);
                if (OriginalValue == null || NewValue == null || OriginalValue.GetType() != NewValue.GetType())
                {
                    return false;
                }
                string Pre_SQL = " UPDATE [" + TableName + "]";
                string NEW_SQL = " SET ";
                string OG_SQL = " WHERE ";

                string post = ParamValues(OD1, suffix: "1");
                string pre = ParamValues(OD2, true, IdentifierColumn, "2");

                NEW_SQL += pre;
                OG_SQL += post;


                string SQL = Pre_SQL + NEW_SQL + OG_SQL;

                SqlConnection conn = new SqlConnection(connectionString);
                SqlCommand dbCommand = new SqlCommand(SQL, conn);
                string tmp = SQL;
                for (int index = 0; index < OD1.ObjectCount; index++)
                {
                    if (OD1.ObjectValues[index] != null)
                    {
                        SQL = SQL.Replace("@" + OD1.ObjectParameters[index] + "1".ToString(), OD1.ObjectValues[index].ToString());
                        dbCommand.Parameters.AddWithValue("@" + OD1.ObjectParameters[index] + "1", OD1.ObjectValues[index]);
                    }
                    if (OD2.ObjectValues[index] != null)
                    {
                        SQL = SQL.Replace("@" + OD2.ObjectParameters[index] + "2".ToString(), OD2.ObjectValues[index].ToString());
                        dbCommand.Parameters.AddWithValue("@" + OD2.ObjectParameters[index] + "2", OD2.ObjectValues[index]);
                    }
                }
                tmp = SQL;

                try
                {
                    conn.Open();
                    int p = dbCommand.ExecuteNonQuery();
                    conn.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    await Log(ex.ToString(), sqlLog);
                    await ValidateTable(TableName: TableName, OD: OD1, FailedSQL: SQL);
                    return false;
                }
            }
            catch (Exception ex)
            {
                await Log(ex.ToString(), sqlLog);
                return false;
            }
        }
        public async Task Delete(object input, string TableName)
        {
            ObjectDictionary OD = GetDictionary(input);
            if (OD != null)
            {
                string sql = "Delete From " + TableName + " Where " + ParamValues(OD);
                SqlConnection conn = new SqlConnection(connectionString);

                SqlCommand dbCommand = new SqlCommand(sql, conn);
                for (int index = 0; index < OD.ObjectCount; index++)
                {
                    if (OD.ObjectValues[index] != null)
                    {
                        dbCommand.Parameters.AddWithValue("@" + OD.ObjectParameters[index], OD.ObjectValues[index]);
                    }

                }

                try
                {
                    conn.Open();
                    int p = dbCommand.ExecuteNonQuery();
                    conn.Close();
                }
                catch (Exception ex)
                {
                    await Log(ex.ToString(), sqlLog);
                    if (AutoManageDatabaseStructure)
                    {
                        await ValidateTable(TableName: TableName, OD: OD, Exception: ex.ToString());
                    }
                }
            }
        }

        public async Task DeleteMany<T>(List<T> input, string TableName)
        {
            for (int index = 0; index < input.Count; index++)
            {
                await Delete(input[index], TableName);
            }
        }

        public async Task<object> RunSproc(string StoredProcedure, List<object> paramas = null, List<object> passparamas = null)
        {
            try
            {
                SqlConnection sqlconn = new SqlConnection(connectionString);
                SqlDataAdapter sda = new SqlDataAdapter();
                SqlCommand sqlcmd = new SqlCommand(StoredProcedure, sqlconn)
                { CommandType = CommandType.StoredProcedure };


                int z = 0;
                if (paramas != null)
                {
                    foreach (string e in paramas)
                    {
                        sqlcmd.Parameters.AddWithValue(e, passparamas[z]);
                        z++;
                    }
                }
                sqlconn.Open();


                object val = sqlcmd.ExecuteScalar(); ;

                sqlconn.Close();
                if (val == null)
                {
                    val = "";
                }
                return val;
            }
            catch (Exception ex)
            {
                await Log(ex.ToString(), sqlLog);
                return null;
            }
        }


        public async Task<int> Insert<T>(T input, string TableName, string IdentifierColumn = "id")
        {
            if (typeof(IEnumerable).IsAssignableFrom(input.GetType()))
            {
                IList objList = (IList)input;
                ObjectDictionary PlaceHolder = null;
                for (int index = 0; index < objList.Count; index++)
                {
                    ObjectDictionary OD = new ObjectDictionary();
                    if (PlaceHolder == null)
                    {
                        PlaceHolder = GetDictionary(objList[index]);
                        OD = PlaceHolder;
                    }
                    else
                    {
                        OD = GetDictionary(objList[index], PlaceHolder);
                    }
                    return await Write(OD, TableName, IdentifierColumn);
                }
            }
            else
            {
                ObjectDictionary OD = GetDictionary(input);
                return await Write(OD, TableName, IdentifierColumn);
            }
            return 0;


        }


        public object ConvertDictionaryToObject(ObjectDictionary OD)
        {
            var obj = Activator.CreateInstance(OD.ObjectType);
            if (OD.ObjectValues == null)
            {
                return null;
            }
            for (int index = 0; index < OD.ObjectCount; index++)
            {
                try
                {
                    OD.ObjectType.GetProperty(OD.ObjectParameters[index]).SetValue(obj, OD.ObjectValues[index], null);
                }
                catch
                {
                    OD.ObjectType.GetProperty(OD.ObjectParameters[index]).SetValue(obj, null);
                }
            }
            return obj;

        }

        private async Task<int> Write(ObjectDictionary OD, string TableName, string IdentifierColumn = "id")
        {
            int MaxID = 0;
            string Post_SQL = "";
            string Values = "Values(";

            for (int index = 0; index < OD.ObjectCount; index++)
            {
                if (OD.ObjectParameters[index].ToLower() == IdentifierColumn.ToLower() && OD.ObjectTypes[index] == typeof(int?))
                {
                    try
                    {
                        MaxID = await (NextSQLID(TableName, IdentifierColumn));
                        OD.ObjectValues[index] = MaxID;
                    }
                    catch (Exception ex)
                    {
                        await Log(ex.ToString(), sqlLog);
                        if (AutoManageDatabaseStructure)
                        {
                            await ValidateTable(TableName: TableName, OD: OD, Exception: ex.ToString());
                        }
                        MaxID = await (NextSQLID(TableName, IdentifierColumn));
                        OD.ObjectValues[index] = MaxID;

                    }
                }

                if (index != OD.ObjectCount - 1)
                {
                    Post_SQL += "[" + OD.ObjectParameters[index] + "],";
                    Values += "@" + OD.ObjectParameters[index] + ",";
                }
                else
                {
                    Post_SQL += "[" + OD.ObjectParameters[index] + "])";
                    Values += "@" + OD.ObjectParameters[index] + ")";
                }
            }
            string SQL = "Insert INTO " + TableName + "(" + Post_SQL + Values;
            SqlConnection conn = new SqlConnection(connectionString);
            SqlCommand cmd = new SqlCommand(SQL, conn);
            try
            {


                for (int index = 0; index < OD.ObjectCount; index++)
                {
                    if (OD.ObjectValues[index] != null)
                    {

                        cmd.Parameters.AddWithValue("@" + OD.ObjectParameters[index], OD.ObjectValues[index]);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@" + OD.ObjectParameters[index], System.DBNull.Value);
                    }
                }
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();

            }
            catch (Exception ex)
            {
                await Log(ex.ToString(), sqlLog);
                if (AutoManageDatabaseStructure)
                {
                    await ValidateTable(TableName: TableName, OD: OD, CMD: cmd, Exception: ex.ToString());
                }
            }
            return MaxID;
        }







        private bool IsNullOrDefault<T>(T value)
        {

            if (EqualityComparer<T>.Default.Equals(value, default(T)))
            {
                return true;
            }
            else
            {
                return object.Equals(value, default(T));
            }
        }
        private object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }

        private async void MakeDatabaseSafe(string TableName)
        {
            string CreateDatabase = "IF DB_ID('T_A_B_L_E_N_A_M_E') Is NULL CREATE DATABASE T_A_B_L_E_N_A_M_E;".Replace("T_A_B_L_E_N_A_M_E", TableName);
            await RunSQL(CreateDatabase);
        }



        public async Task ValidateTable(string TableName, ObjectDictionary OD, string FailedSQL = "", SqlCommand CMD = null, string Exception = "")
        {
            bool ColumnsSized_Properly = true;
            bool TableCreated = false;
            if (Exception.Contains("String or binary data would be truncated.\r\nThe statement has been terminated."))
            {
                ColumnsSized_Properly = false;
            }
            if (Exception.Contains("System.IndexOutOfRangeException: "))
            {
                Exception = "Invalid column name";
            }
            if (Exception.Contains("Invalid object name") || Exception.Contains("Cannot find the object"))
            {
                try
                {
                    await RunSQL("CREATE TABLE " + TableName + " (" + OD.ObjectParameters[0] + " " + CType_To_SQLType(OD.ObjectTypes[0]) + " );");
                }
                catch (Exception ex)
                {
                    await Log(ex.ToString(), sqlLog);
                }
                Exception = "Invalid column name";
                TableCreated = true;
            }
            if (Exception.Contains("Reason: The password of the account must be changed."))
            {

            }
            if (Exception.Contains("Invalid column name"))
            {
                DBColumn genericColumn = new DBColumn() { TABLE_NAME = TableName };
                List<DBColumn> Columns = new List<DBColumn>() { genericColumn };

                string GetAllColumnsQuery = "select * from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME='@TableName'";
                GetAllColumnsQuery = GetAllColumnsQuery.Replace("@TableName", TableName);


                Columns = await SelectMany(Columns, "INFORMATION_SCHEMA.COLUMNS", OverrideSQL: GetAllColumnsQuery);

                string addColumn = "ALTER TABLE @TableName ADD @column_name @datatype;";
                for (int index = 0; index < OD.ObjectCount; index++)
                {
                    addColumn = "ALTER TABLE @TableName ADD @column_name @datatype;";
                    //field is improperly sized
                    if (!ColumnsSized_Properly)
                    {
                        addColumn = "ALTER TABLE [@TableName] ALTER COLUMN [@column_name] @datatype;";
                    }
                    addColumn = addColumn.Replace("@TableName", TableName);
                    if (Columns.FirstOrDefault(X => X.COLUMN_NAME == OD.ObjectParameters[index]) != null && ColumnsSized_Properly)
                    {
                        continue;
                    }
                    else
                    {
                        addColumn = addColumn.Replace("@column_name", "[" + OD.ObjectParameters[index] + "]");
                        addColumn = addColumn.Replace("@datatype", CType_To_SQLType(OD.ObjectTypes[index]));
                    }
                    await RunSQL(addColumn);
                }
            }
            if (FailedSQL != "")
            {
                await RunSQL(FailedSQL);
            }
            if (CMD != null)
            {
                try
                {
                    SqlConnection conn = new SqlConnection(connectionString);
                    CMD.Connection = conn;
                    conn.Open();
                    CMD.ExecuteNonQuery();
                    conn.Close();
                }
                catch (Exception ex)
                {
                    await Log(ex.ToString());
                }
            }
        }


        private string CType_To_SQLType(Type ItemType)
        {
            TypeMapper TM = MSSQLMapping.MSSQL_Types_ToCSharp.FirstOrDefault(X => X.TMCSharpType == ItemType);
            if (TM == null)
            {
                return MSSQLMapping.MSSQL_Types_ToCSharp.FirstOrDefault(X => X.TMCSharpType == typeof(object)).TMSQLType;
            }
            else
            {
                return TM.TMSQLType;
            }
        }



        private async Task<object> RunSQL(string SQLScript)
        {
            try
            {
                SqlConnection sqlconn = new SqlConnection(connectionString);
                SqlCommand sqlcmd = new SqlCommand(SQLScript, sqlconn);
                sqlconn.Open();
                sqlcmd.ExecuteNonQuery();
                sqlconn.Close();
                return true;
            }
            catch (Exception ex)
            {
                await Log(ex.ToString(), sqlLog);

                throw;
            }

        }
        private async Task<int> NextSQLID(string TableName, string NonGenericColumn = "ID")
        {
            string SQLScript = "Select ISNULL(MAX(" + NonGenericColumn + "), 0) From " + TableName;
            try
            {
                using (var connection1 = new SqlConnection(connectionString))
                using (var cmd = new SqlDataAdapter())
                using (var insertCommand = new SqlCommand(SQLScript))
                {
                    insertCommand.Connection = connection1;
                    cmd.InsertCommand = insertCommand;
                    connection1.Open();
                    int returnInt = 0;
                    var h = insertCommand.ExecuteScalar();
                    if (h != System.DBNull.Value)
                    {
                        returnInt = Convert.ToInt32(h);
                    }
                    return returnInt + 1;
                }
            }
            catch (Exception ex)
            {
                await Log(ex.ToString(), sqlLog);
                throw;
            }
        }
        private string ParamValues(ObjectDictionary OD, bool Updater = false, string IdentifierColumn = null, string suffix = "")
        {
            if (OD == null)
            {
                return "";
            }
            string LinkerOf = " AND ";
            if (Updater)
            {
                LinkerOf = " , ";
            }

            string Appended_Value = "";
            int TempCount = OD.ObjectCount;
            int added = 0;
            for (int index = 0; index < TempCount; index++)
            {
                if (OD.ObjectValues[index] == null || OD.ObjectValues[index] == System.DBNull.Value || OD.ObjectParameters[index] == IdentifierColumn)
                {
                    continue;
                }
                else
                {
                    if (index == OD.ObjectCount)
                    {
                        Appended_Value += "[" + OD.ObjectParameters[index] + "]" + " = @" + OD.ObjectParameters[index] + suffix + " ";
                    }
                    else
                    {
                        if (added == 0)
                        {
                            Appended_Value += "[" + OD.ObjectParameters[index] + "]" + " = @" + OD.ObjectParameters[index] + suffix + " ";
                            added++;
                        }
                        else
                        {
                            Appended_Value += LinkerOf + "[" + OD.ObjectParameters[index] + "]" + " = @" + OD.ObjectParameters[index] + suffix + " ";
                        }
                    }
                }
            }

            return Appended_Value;

        }

        public ObjectDictionary GetDictionary<T>(T input, ObjectDictionary lastDictionary = null)
        {
            if (input == null)
            {
                return null;
            }
            if (lastDictionary == null)
            {
                List<string> Parameters = new List<string>();
                List<object> Parameters_Values = new List<object>();
                List<Type> Parameter_Types = new List<Type>();

                var properties = input.GetType().GetProperties();
                int Property_Count = properties.Count();
                
                for (int index = 0; index < Property_Count; index++)
                {
                    string Ovalue = properties[index].Name;

                    object Property_Value = GetPropValue(input, Ovalue);
                    Type Property_Type = properties[index].PropertyType;
                    bool isGenericType = properties[index].PropertyType.IsGenericType;
                    TypeMapper TM = MSSQLMapping.MSSQL_Types_ToCSharp.FirstOrDefault(X => X.TMCSharpType == Property_Type);
                    if (Property_Type.IsGenericType && (Property_Type.GetGenericTypeDefinition().Equals(typeof(Nullable<>))) || Property_Type == typeof(string) || Property_Type == typeof(byte?[]))
                    {
                        
                        if (Property_Type == typeof(byte?[]))
                        {
                            Property_Type = typeof(byte[]);

                            if (Property_Value != null)
                            {
                                byte[] target = ((byte?[])Property_Value).Select(b => b.GetValueOrDefault()).ToArray();
                                Property_Value = target;
                            }
                        }

                        TM = MSSQLMapping.MSSQL_Types_ToCSharp.FirstOrDefault(X => X.TMCSharpType == Property_Type);
                        if (TM == null)
                        {
                            TM = MSSQLMapping.MSSQL_Types_ToCSharp.FirstOrDefault(X => X.TMCSharpType == typeof(object));
                        }
                        if (TM.TMCSharpType == typeof(object) && Property_Value != null)
                        {
                            Property_Value = System.Text.Json.JsonSerializer.Serialize(Property_Value);
                        }
                        Parameter_Types.Add(Property_Type);
                    }
                    else if(TM == null && isGenericType || Property_Type == typeof(bool))
                    {
                        continue;
                    }
                    else
                    {
                        try
                        {
                            if (!(IsNullOrDefault(Property_Value)))
                            {
                                Property_Value = (System.Text.Json.JsonSerializer.Serialize(Property_Value));
                                Parameter_Types.Add(typeof(object));
                            }
                            else
                            {
                                Parameter_Types.Add(typeof(object));
                                Property_Value = (null);
                            }
                            
                        }
                        catch (Exception ex)
                        {
                            Parameter_Types.Add(typeof(object));
                            Property_Value = (null);
                        }
                    }
                    
                    while (Ovalue.Contains(TheoreticalSpace))
                    {
                        Ovalue = Ovalue.Replace(TheoreticalSpace, " ");
                    }
                    Parameters.Add(Ovalue);
                    //name of property
                    if (!(IsNullOrDefault(Property_Value)))
                    {
                        Parameters_Values.Add(Property_Value);
                    }
                    else
                    {
                        Parameters_Values.Add(null);
                    }
                }
                return new ObjectDictionary() { ObjectParameters = Parameters, ObjectValues = Parameters_Values, ObjectTypes = Parameter_Types, ObjectCount = Parameters_Values.Count, ObjectType = input.GetType() };
            }
            else
            {
                lastDictionary.ObjectValues = new List<object>();
                for (int index = 0; index < lastDictionary.ObjectCount; index++)
                {

                    string Ovalue = lastDictionary.ObjectParameters[index];
                    //value of property
                    object Property_Value = GetPropValue(input, Ovalue);


                    //name of property
                    if (!(IsNullOrDefault(Property_Value)))
                    {
                        lastDictionary.ObjectValues.Add(Property_Value);
                    }
                    else
                    {
                        lastDictionary.ObjectValues.Add(null);
                    }
                }
                return lastDictionary;
            }

        }




        public async Task<bool> TestConnection()
        {
            try
            {
                await RunSQL("Select 1");
                return true;
            }

            catch (System.Exception e)
            {
                throw;
            }

        }
    }

    public class ObjectDictionary
    {
        public Type ObjectType { get; set; }
        public List<string> ObjectParameters { get; set; }

        public List<object> ObjectValues { get; set; }

        public List<Type> ObjectTypes { get; set; }

        public int ObjectCount { get; set; }
    }

    public class DBColumn
    {
        public string TABLE_NAME { get; set; }
        public string COLUMN_NAME { get; set; }
        public string DATA_TYPE { get; set; }
    }




    public static class MSSQLMapping
    {
        ///structured like [C# Type, SQL Type
        public static TypeMapper[] MSSQL_Types_ToCSharp = new TypeMapper[]
        {
            new TypeMapper{TMCSharpType = typeof(bool?), TMSQLType = "bit" },
            new TypeMapper {TMCSharpType = typeof(byte[]), TMSQLType = "binary(100)" },
            new TypeMapper {TMCSharpType = typeof(char?), TMSQLType = "char" },
            new TypeMapper {TMCSharpType = typeof(double?), TMSQLType = "float" },
            new TypeMapper {TMCSharpType = typeof(float?), TMSQLType = "float" },
            new TypeMapper {TMCSharpType = typeof(int?), TMSQLType = "int" },
            new TypeMapper {TMCSharpType = typeof(uint?), TMSQLType = "int" },
            new TypeMapper {TMCSharpType = typeof(Int64?), TMSQLType = "bigint" },
            new TypeMapper {TMCSharpType = typeof(long?), TMSQLType = "bigint" },
            new TypeMapper {TMCSharpType = typeof(Int32?), TMSQLType = "int" },
            new TypeMapper {TMCSharpType = typeof(Int16?), TMSQLType = "tinyint" },
            new TypeMapper {TMCSharpType = typeof(decimal?), TMSQLType = "decimal" },
            new TypeMapper {TMCSharpType = typeof(DateTime?), TMSQLType = "datetime" },
            new TypeMapper {TMCSharpType = typeof(string), TMSQLType = "varchar(max)" },
            new TypeMapper {TMCSharpType = typeof(Guid?), TMSQLType = "uniqueidentifier" },
            new TypeMapper { TMCSharpType = typeof(object), TMSQLType = "varchar(max)" },
        };
    }

    public class TypeMapper
    {
        public Type TMCSharpType { get; set; }
        public string TMSQLType { get; set; }
    }
}
