
namespace BSDiscordRanking.DatabaseFramework
{
    /// <summary>
    /// Database Entity object field instance
    /// </summary>
    public class EntityField
    {
        /// <summary>
        /// Field Name
        /// </summary>
        public string Name;
        /// <summary>
        /// Is a primary key
        /// </summary>
        public bool PrimaryKey;
        /// <summary>
        /// Is an auto incremented value
        /// </summary>
        public bool AutoIncrement;
        /// <summary>
        /// Field type info
        /// </summary>
        public System.Type Type;
        /// <summary>
        /// Reflection info
        /// </summary>
        public System.Reflection.FieldInfo Info;
    }
}
