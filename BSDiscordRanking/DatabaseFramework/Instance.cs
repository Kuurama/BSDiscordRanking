using System.Collections.Concurrent;

namespace BSDiscordRanking.DatabaseFramework
{
    /// <summary>
    /// Database instance
    /// </summary>
    public class Instance
    {
        /// <summary>
        /// List of connections
        /// </summary>
        private ConcurrentQueue<Connection> m_Connections = new ConcurrentQueue<Connection>();

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_Host">Server host</param>
        /// <param name="p_Port">Server port</param>
        /// <param name="p_User">Username</param>
        /// <param name="p_Pass">Password</param>
        /// <param name="p_Name">Database name</param>
        /// <param name="p_Charset">Charset</param>
        /// <param name="p_Pool">Pool size</param>
        public Instance(string p_Host, int p_Port, string p_User, string p_Pass, string p_Name, string p_Charset, int p_Pool)
        {
            for (int l_I = 0; l_I < p_Pool; ++l_I)
                m_Connections.Enqueue(new Connection(p_Host, p_Port, p_User, p_Pass, p_Name, p_Charset));
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Get a free connection for a query
        /// </summary>
        /// <returns>A free MySQL Connection</returns>
        public Connection GetFreeConnection()
        {
            Connection l_Connection = null;
            bool l_IsReady = false;
            while (!l_IsReady)
            {
                while (!m_Connections.TryDequeue(out l_Connection) || l_Connection == null)
                    System.Threading.Thread.Sleep(1);

                l_IsReady = l_Connection.IsReady();

                /// It's a bad connection but need re-enqueue to fix it
                if (!l_IsReady)
                    m_Connections.Enqueue(l_Connection);
            }

            System.Threading.Interlocked.Exchange(ref l_Connection.IsFree, Connection.NotFreeValue);
            return l_Connection;
        }
        /// <summary>
        /// Release a connection
        /// </summary>
        /// <param name="p_Connection">Connection instance</param>
        public void ReleaseConnection(Connection p_Connection)
        {
            while (p_Connection.DBConnection.State == System.Data.ConnectionState.Fetching || p_Connection.DBConnection.State == System.Data.ConnectionState.Executing)
                System.Threading.Thread.Sleep(1);

            System.Threading.Interlocked.Exchange(ref p_Connection.IsFree, Connection.FreeValue);
            m_Connections.Enqueue(p_Connection);
        }
    }
}
