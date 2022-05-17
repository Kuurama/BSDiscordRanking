using System;

namespace BSDiscordRanking.DatabaseFramework
{
    /// <summary>
    /// Table Database entity object class attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    class Table : Attribute
    {
        /// <summary>
        /// Table name
        /// </summary>
        public string TableName;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_TableName">Table name</param>
        public Table(string p_TableName)
        {
            TableName = p_TableName;
        }
    }

    /// <summary>
    /// Primary Key table field attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    class TablePrimaryKey : Attribute
    {

    }

    /// <summary>
    /// Auto increment table field attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    class TableAutoIncrement : Attribute
    {

    }

    /// <summary>
    /// Table field attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    public class TableField : Attribute
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public TableField()
        {

        }
    }
}
