using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace BSDiscordRanking.DatabaseFramework{
    /// <summary>
    /// Database entity base class
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class Entity<T> where T : new()
    {
        /// <summary>
        /// Table name
        /// </summary>
        private static string m_TableName = "";
        /// <summary>
        /// Table fields
        /// </summary>
        private static List<EntityField> m_Fields = new List<EntityField>();
        /// <summary>
        /// Table primary fields
        /// </summary>
        private static List<EntityField> m_PrimaryFields = new List<EntityField>();
        /// <summary>
        /// Table auto incremented field
        /// </summary>
        private static EntityField m_AutoIncrementedField = null;

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Database instance
        /// </summary>
        private Instance m_DatabaseInstance;

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_DatabaseInstance">Database instance</param>
        public Entity(Instance p_DatabaseInstance = null)
        {
            m_DatabaseInstance = p_DatabaseInstance;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="p_DatabaseInstance">Linked database</param>
        /// <returns>The new instance</returns>
        public static T Instantiate(Instance p_DatabaseInstance)
        {
            T l_New = new T();
            (l_New as Entity<T>).m_DatabaseInstance = p_DatabaseInstance;

            return l_New;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Find objects in the database by conditions
        /// </summary>
        /// <param name="p_Conditions">List of conditions</param>
        /// <returns>List of found objects</returns>
        public static List<T> Find(Instance p_Connection, params Condition[] p_Conditions)
        {
            try
            {
                List<T> l_ObjectList = new List<T>();

                if (p_Connection == null)
                    return l_ObjectList;

                var l_Fields = GetFieldList();

                string l_Query = BuildSelectQuery();

                MySqlCommand l_Command = new MySqlCommand();

                l_Query += EvalConditions(l_Command, p_Conditions);
                l_Command.CommandText = l_Query;

                var l_Connection = p_Connection.GetFreeConnection();
                l_Command.Connection = l_Connection.DBConnection;

                EntityField l_DebugField = null;

                try
                {
                    l_Command.Prepare();

                    using(MySqlDataReader l_Reader = l_Command.ExecuteReader())
                    {
                        while (l_Reader.Read())
                        {
                            var l_Object = new T();
                            (l_Object as Entity<T>).m_DatabaseInstance = p_Connection;

                            int l_I = 0;
                            foreach (var l_Field in l_Fields)
                            {
                                l_DebugField = l_Field;

                                try
                                {
                                    if (l_Field.Type == typeof(System.String) && l_Reader.GetValue(l_I).GetType() == typeof(System.DBNull))
                                        l_Field.Info.SetValue(l_Object, "");
                                    else if (l_Reader.GetValue(l_I).GetType() == typeof(System.DBNull)
                                            && (l_Field.Type == typeof(uint) || l_Field.Type == typeof(int)
                                            || l_Field.Type == typeof(sbyte) || l_Field.Type == typeof(byte)
                                            || l_Field.Type == typeof(long) || l_Field.Type == typeof(ulong)))
                                        l_Field.Info.SetValue(l_Object, (uint)0);
                                    else
                                        l_Field.Info.SetValue(l_Object, l_Reader.GetValue(l_I));
                                }
                                catch (Exception)
                                {
                                    Logs.Error.Log("[Database.Entity] Find => Field warning : " + GetTableName() + "." + (l_DebugField != null ? l_DebugField.Name : "<Unk>") + " is " + l_Field.Type + " in the API model but the field type in the table schema is different : " + l_Reader.GetValue(l_I).GetType() + ", trying to convert it ...");
                                    l_Field.Info.SetValue(l_Object, 0);
                                }

                                l_I++;
                            }

                            l_ObjectList.Add(l_Object);
                        }
                        l_Reader.Close();
                    }
                }
                catch (MySqlException l_MySQLException)
                {
                    Logs.Error.Log("[Database.Entity] Find => Mysql Exception : " + GetTableName() + "." + (l_DebugField != null ? l_DebugField.Name : "<Unk>"), l_MySQLException);
                    throw new System.Exception("Error : " + l_MySQLException.ToString());
                }
                catch (System.Exception l_Exception)
                {
                    Logs.Error.Log("[Database.Entity] Find => Field error : " + GetTableName() + "." + (l_DebugField != null ? l_DebugField.Name : "<Unk>"), l_Exception);
                    throw new System.Exception("Error : " + l_Exception.ToString());
                }
                finally
                {
                    if (l_Command != null)
                    {
                        l_Command.Dispose();
                        l_Command.Connection = null;
                        l_Command = null;
                    }

                    p_Connection.ReleaseConnection(l_Connection);
                }

                return l_ObjectList;
            }
            catch (System.Exception l_Exception)
            {
                Logs.Error.Log("[Database.Entity] Find => Error in : " + GetTableName(), l_Exception);
            }

            return null;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Count objects in the database by conditions
        /// </summary>
        /// <param name="p_Conditions">List of conditions</param>
        /// <returns>Number of found objects</returns>
        public static UInt32 Count(Instance p_Connection, params Condition[] p_Conditions)
        {
            string l_Query = BuildCountQuery();

            MySqlCommand l_Command = new MySqlCommand();

            l_Query += EvalConditions(l_Command, p_Conditions);
            l_Command.CommandText = l_Query;

            var l_Connection = p_Connection.GetFreeConnection();
            l_Command.Connection = l_Connection.DBConnection;

            UInt32 l_CountResult = 0;

            try
            {
                l_Command.Prepare();

                using(MySqlDataReader l_Reader = l_Command.ExecuteReader())
                {
                    while (l_Reader.Read())
                    {
                        l_CountResult = l_Reader.GetUInt32(0);
                        break;
                    }

                    l_Reader.Close();
                }
            }
            catch (System.Exception l_Exception)
            {
                Logs.Error.Log("[Database.Entity] Count " + GetTableName(), l_Exception);
            }
            finally
            {
                if (l_Command != null)
                {
                    l_Command.Dispose();
                    l_Command.Connection = null;
                    l_Command = null;
                }

                p_Connection.ReleaseConnection(l_Connection);
            }

            return l_CountResult;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Avg objects in the database by conditions
        /// </summary>
        /// <param name="p_Conditions">List of conditions</param>
        /// <returns>Number of found objects</returns>
        public static float Avg(Instance p_Connection, string p_Column, params Condition[] p_Conditions)
        {
            string l_Query = BuildAvgQuery(p_Column);

            MySqlCommand l_Command = new MySqlCommand();

            l_Query += EvalConditions(l_Command, p_Conditions);
            l_Command.CommandText = l_Query;

            var l_Connection = p_Connection.GetFreeConnection();
            l_Command.Connection = l_Connection.DBConnection;

            float l_Average = 0f;

            try
            {
                l_Command.Prepare();

                using (MySqlDataReader l_Reader = l_Command.ExecuteReader())
                {
                    while (l_Reader.Read())
                    {
                        if (l_Reader.GetValue(0).GetType() != typeof(System.DBNull))
                            l_Average = l_Reader.GetFloat(0);

                        break;
                    }

                    l_Reader.Close();
                }
            }
            catch (System.Exception l_Exception)
            {
                Logs.Error.Log("[Database.Entity] Avg " + GetTableName(), l_Exception);
            }
            finally
            {
                if (l_Command != null)
                {
                    l_Command.Dispose();
                    l_Command.Connection = null;
                    l_Command = null;
                }

                p_Connection.ReleaseConnection(l_Connection);
            }

            return l_Average;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Count objects in the database by conditions
        /// </summary>
        /// <param name="p_Conditions">List of conditions</param>
        /// <returns>Number of found objects</returns>
        public static UInt32 CountDistinct(Instance p_Connection, string p_Column, params Condition[] p_Conditions)
        {
            string l_Query = BuildCountDistinctQuery(p_Column);

            MySqlCommand l_Command = new MySqlCommand();

            l_Query += EvalConditions(l_Command, p_Conditions);
            l_Command.CommandText = l_Query;

            var l_Connection = p_Connection.GetFreeConnection();
            l_Command.Connection = l_Connection.DBConnection;

            UInt32 l_CountResult = 0;

            try
            {
                l_Command.Prepare();

                using (MySqlDataReader l_Reader = l_Command.ExecuteReader())
                {
                    while (l_Reader.Read())
                    {
                        l_CountResult = l_Reader.GetUInt32(0);
                        break;
                    }

                    l_Reader.Close();
                }
            }
            catch (System.Exception l_Exception)
            {
                Logs.Error.Log("[Database.Entity] Count " + GetTableName(), l_Exception);
            }
            finally
            {
                if (l_Command != null)
                {
                    l_Command.Dispose();
                    l_Command.Connection = null;
                    l_Command = null;
                }

                p_Connection.ReleaseConnection(l_Connection);
            }

            return l_CountResult;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sync this object in the database
        /// </summary>
        public long Update()
        {
            long l_UpdateReturn = 0;

            var l_Fields    = GetFieldList();
            var l_TableName = GetTableName();

            string l_Query = BuildUpdateQuery();

            int l_KeyI = 1;
            foreach (var l_PrimaryField in GetPrimaryFields())
            {
                if (l_KeyI > 1)
                    l_Query += " AND ";
                else
                    l_Query += " WHERE ";

                l_Query += l_TableName + "." + l_PrimaryField.Name + "=@WhereValue" + (l_KeyI++).ToString();
            }

            MySqlCommand l_Command = new MySqlCommand();

            var l_Connection = m_DatabaseInstance.GetFreeConnection();

            l_Command.Connection = l_Connection.DBConnection;
            l_Command.CommandText = l_Query;

            try
            {
                l_KeyI = 1;
                foreach (var l_PrimaryField in GetPrimaryFields())
                    l_Command.Parameters.AddWithValue("@WhereValue" + (l_KeyI++).ToString(), l_PrimaryField.Info.GetValue(this));

                foreach (var l_Field in l_Fields)
                    l_Command.Parameters.AddWithValue("@" + l_Field.Name, l_Field.Info.GetValue(this));

                l_Command.Prepare();
                l_UpdateReturn = l_Command.ExecuteNonQuery();
            }
            catch (System.Exception l_Exception)
            {
                Logs.Error.Log("[Database.Entity] Update " + GetTableName(), l_Exception);
                l_UpdateReturn = -1;
            }
            finally
            {
                if (l_Command != null)
                {
                    l_Command.Dispose();
                    l_Command.Connection = null;
                    l_Command = null;
                }

                m_DatabaseInstance.ReleaseConnection(l_Connection);
            }

            return l_UpdateReturn;
        }
        /// <summary>
        /// Clone an equivalent entity
        /// </summary>
        /// <param name="p_Other">Entity to clone</param>
        public void UpdateFrom(T p_Other)
        {
            var l_Fields = GetFieldList();

            foreach (var l_Field in l_Fields)
                l_Field.Info.SetValue(this, l_Field.Info.GetValue(p_Other));
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Insert the current object in the database
        /// </summary>
        /// <returns>Inserted ID</returns>
        public long Insert(bool p_WithAutoIncrementedValues = false)
        {
            var l_Fields = GetFieldList();
            string l_Query = BuildInsertQuery(p_WithAutoIncrementedValues);

            MySqlCommand l_Command = new MySqlCommand();

            var l_Connection = m_DatabaseInstance.GetFreeConnection();
            l_Command.Connection    = l_Connection.DBConnection;
            l_Command.CommandText   = l_Query;

            long l_InsertedID = 0;

            try
            {
                foreach (var l_Field in l_Fields)
                {
                    if (l_Field.AutoIncrement && !p_WithAutoIncrementedValues)
                        continue;

                    l_Command.Parameters.AddWithValue("@" + l_Field.Name, l_Field.Info.GetValue(this));
                }

                l_Command.Prepare();
                l_Command.ExecuteNonQuery();

                var l_AutoIncrementedField = GetAutoIncrementedField();
                if (l_AutoIncrementedField != null && l_AutoIncrementedField.Type.IsPrimitive)
                    l_AutoIncrementedField.Info.SetValue(this, Convert.ChangeType(l_Command.LastInsertedId, l_AutoIncrementedField.Type));

                l_InsertedID = l_Command.LastInsertedId;
            }
            catch (System.Exception l_Exception)
            {
                Logs.Error.Log("[Database.Entity] Insert " + GetTableName(), l_Exception);
                l_InsertedID = -1;
            }
            finally
            {
                if (l_Command != null)
                {
                    l_Command.Dispose();
                    l_Command.Connection = null;
                    l_Command = null;
                }

                m_DatabaseInstance.ReleaseConnection(l_Connection);
            }

            return l_InsertedID;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Delete this object
        /// </summary>
        public void Delete()
        {
            List<Condition> l_Conditions = new List<Condition>();

            if (GetPrimaryFields().Count > 0)
            {
                foreach (var l_PrimaryField in GetPrimaryFields())
                    l_Conditions.Add(new Where(l_PrimaryField.Name, Where.Operator.Equal, l_PrimaryField.Info.GetValue(this)));
            }
            else
            {
                foreach (var l_Field in GetFieldList())
                    l_Conditions.Add(new Where(l_Field.Name, Where.Operator.Equal, l_Field.Info.GetValue(this)));
            }

            Delete(m_DatabaseInstance, l_Conditions.ToArray());
        }
        /// <summary>
        /// Delete an object in database
        /// </summary>
        /// <param name="p_Conditions">List of conditions</param>
        public static void Delete(Instance p_Connection, params Condition[] p_Conditions)
        {
            MySqlCommand l_Command = new MySqlCommand();
            var l_Connection = p_Connection.GetFreeConnection();
            l_Command.Connection = l_Connection.DBConnection;

            string l_Query = "DELETE FROM " + GetTableName() + " ";
            l_Query += EvalConditions(l_Command, p_Conditions);

            l_Command.CommandText = l_Query;

            try
            {
                l_Command.Prepare();
                l_Command.ExecuteNonQuery();
            }
            catch (System.Exception l_Exception)
            {
                Logs.Error.Log("[Database.Entity] Delete", l_Exception);
            }
            finally
            {
                if (l_Command != null)
                {
                    l_Command.Dispose();
                    l_Command.Connection = null;
                    l_Command = null;
                }

                p_Connection.ReleaseConnection(l_Connection);
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Build a prepare select query string
        /// </summary>
        /// <returns>Query string</returns>
        public static string BuildSelectQuery()
        {
            var l_Fields       = GetFieldList();
            string l_Query     = "SELECT ";
            string l_TableName = GetTableName();

            int l_I = 0;
            foreach (var l_Field in l_Fields)
            {
                if (l_I != 0)
                    l_Query += ", ";

                l_Query += l_TableName + "." + l_Field.Name;
                l_I++;
            }
            return l_Query + " FROM " + l_TableName + " ";
        }
        /// <summary>
        /// Build a prepare count query string
        /// </summary>
        /// <returns>Query string</returns>
        public static string BuildCountQuery()
        {
            return "SELECT COUNT(*) FROM " + GetTableName() + " ";
        }
        /// <summary>
        /// Build a prepare count query string
        /// </summary>
        /// <returns>Query string</returns>
        public static string BuildAvgQuery(string p_Column)
        {
            return "SELECT AVG(" + p_Column + ") FROM " + GetTableName() + " ";
        }
        /// <summary>
        /// Build a prepare count query string
        /// </summary>
        /// <returns>Query string</returns>
        public static string BuildCountDistinctQuery(string p_ColumnName)
        {
            return "SELECT COUNT(DISTINCT( " + p_ColumnName + " )) FROM " + GetTableName() + " ";
        }
        /// <summary>
        /// Build a prepare update query string
        /// </summary>
        /// <returns>Query string</returns>
        public static string BuildUpdateQuery()
        {
            var l_Fields = GetFieldList();
            string l_Query = "UPDATE " + GetTableName() + " SET ";
            string l_TableName = GetTableName();

            int l_I = 0;
            foreach (var l_Field in l_Fields)
            {
                if (l_I != 0)
                    l_Query += ", ";

                l_Query += l_TableName + "." + l_Field.Name + "=@" + l_Field.Name;

                l_I++;
            }

            return l_Query + " ";
        }
        /// <summary>
        /// Build a prepare insert query string
        /// </summary>
        /// <returns>Query string</returns>
        public static string BuildInsertQuery(bool p_WithAutoIncrementedValues)
        {
            var l_TableName = GetTableName();
            var l_Fields    = GetFieldList();
            string l_Query = "INSERT INTO " + l_TableName + "(";
            string l_Query2 = "";

            int l_I = 0;
            foreach (var l_Field in l_Fields)
            {
                if (l_Field.AutoIncrement && !p_WithAutoIncrementedValues)
                    continue;

                if (l_I != 0)
                {
                    l_Query  += ", ";
                    l_Query2 += ", ";
                }

                l_Query += l_TableName + "." + l_Field.Name;
                l_Query2 += "@" + l_Field.Name;

                l_I++;
            }

            return l_Query + ") VALUES(" + l_Query2 + ") ";
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Get class target table name
        /// </summary>
        /// <returns></returns>
        public static string GetTableName()
        {
            if (m_TableName.Length == 0)
            {
                var l_Attribute = typeof(T).GetCustomAttributes(typeof(Table), true).FirstOrDefault() as Table;

                if (l_Attribute != null)
                    m_TableName = System.Text.Encoding.UTF8.GetString(System.Text.Encoding.UTF8.GetBytes(l_Attribute.TableName));
            }

            return m_TableName;
        }
        /// <summary>
        /// Get table primary key field
        /// </summary>
        /// <returns></returns>
        public static List<EntityField> GetPrimaryFields()
        {
            return m_PrimaryFields;
        }
        /// <summary>
        /// Get table secondary key field
        /// </summary>
        /// <returns></returns>
        public static EntityField GetAutoIncrementedField()
        {
            return m_AutoIncrementedField;
        }
        /// <summary>
        /// Get class present field in database table
        /// </summary>
        /// <returns></returns>
        public static List<EntityField> GetFieldList()
        {
            if (m_Fields.Count == 0)
            {
                System.Reflection.FieldInfo[] l_ClassMembers = typeof(T).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                /// Iterate over each class variable
                foreach (var l_ClassMember in l_ClassMembers)
                {
                    if (!l_ClassMember.IsDefined(typeof(TableField), true))
                        continue;

                    var l_Attribute = (TableField)l_ClassMember.GetCustomAttributes(typeof(TableField), false).SingleOrDefault();
                    var l_Name      = l_ClassMember.Name;

                    EntityField l_Field     = new EntityField();
                    l_Field.Name            = l_Name;
                    l_Field.Type            = l_ClassMember.FieldType;
                    l_Field.Info            = l_ClassMember;
                    l_Field.PrimaryKey      = l_ClassMember.IsDefined(typeof(TablePrimaryKey), true);
                    l_Field.AutoIncrement   = l_ClassMember.IsDefined(typeof(TableAutoIncrement), true);

                    m_Fields.Add(l_Field);

                    if (l_Field.AutoIncrement == true)
                    {
                        if (m_AutoIncrementedField != null)
                            throw new System.Exception(String.Format("Table \"{0}\" contains multiple auto incremented field", GetTableName()));

                        if (!l_Field.Type.IsPrimitive)
                            throw new System.Exception(String.Format("Table \"{0}\" the incremented field need to be a primitive type", GetTableName()));

                        m_AutoIncrementedField = l_Field;
                    }

                    if (l_Field.PrimaryKey == true)
                        m_PrimaryFields.Add(l_Field);
                }
            }

            return m_Fields;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Parse query conditions
        /// </summary>
        /// <param name="p_Command">Query instance</param>
        /// <param name="p_Conditions">List of conditions</param>
        /// <returns>Parsed conditions</returns>
        private static string EvalConditions(MySqlCommand p_Command, params Condition[] p_Conditions)
        {
            string[] l_Layers = new string[(int)Condition.Layer.MaxLayer];

            for (int l_I = 0; l_I < l_Layers.Length; l_I++)
                l_Layers[l_I] = "";

            for (int l_I = 0; l_I < p_Conditions.Length; l_I++)
            {
                string l_Out = "";
                int l_LayerID = (int)p_Conditions[l_I].LayerID;

                if (   p_Conditions[l_I].LayerID == Condition.Layer.Where
                    || p_Conditions[l_I].LayerID == Condition.Layer.WhereIn
                    || p_Conditions[l_I].LayerID == Condition.Layer.MatchAgainst
                    || p_Conditions[l_I].LayerID == Condition.Layer.OrWhere)
                {
                    if (    l_Layers[(int)Condition.Layer.Where]   == "" && l_Layers[(int)Condition.Layer.WhereIn]      == ""
                         && l_Layers[(int)Condition.Layer.OrWhere] == "" && l_Layers[(int)Condition.Layer.MatchAgainst] == "")
                        l_Out += " WHERE ";
                    else
                        l_Out += " AND ";

                    l_Out += p_Conditions[l_I].Value;

                    if (p_Conditions[l_I].LayerID == Condition.Layer.Where || p_Conditions[l_I].LayerID == Condition.Layer.MatchAgainst)
                        p_Command.Parameters.AddWithValue(p_Conditions[l_I].Args.FirstOrDefault().Key, p_Conditions[l_I].Args.FirstOrDefault().Value);
                    else if (p_Conditions[l_I].LayerID == Condition.Layer.WhereIn || p_Conditions[l_I].LayerID == Condition.Layer.OrWhere)
                    {
                        foreach (var l_Pair in p_Conditions[l_I].Args)
                            p_Command.Parameters.AddWithValue(l_Pair.Key, l_Pair.Value);
                    }
                }
                else if (p_Conditions[l_I].LayerID == Condition.Layer.Order
                      || p_Conditions[l_I].LayerID == Condition.Layer.OrderByRand
                      || p_Conditions[l_I].LayerID == Condition.Layer.Limit
                      || p_Conditions[l_I].LayerID == Condition.Layer.GroupBy)
                {
                    l_Out += p_Conditions[l_I].Value;
                }

                l_Layers[l_LayerID] += l_Out + " ";
            }

            string l_Query = "";
            for (int l_I = 0; l_I < l_Layers.Length; l_I++)
                l_Query += l_Layers[l_I];

            return l_Query;
        }
    }
}
