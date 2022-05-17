using System;
using MySql.Data.MySqlClient;

namespace BSDiscordRanking.DatabaseFramework
{
    /// <summary>
    /// Database connection instance
    /// </summary>
    public class Connection : IDisposable
    {
        public static int FreeValue     = 1;
        public static int NotFreeValue  = 0;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// MySql Connection
        /// </summary>
        public MySqlConnection DBConnection;
        /// <summary>
        /// Is that connection free for a query
        /// </summary>
        public int IsFree;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Ping thread
        /// </summary>
        private System.Threading.Thread m_PingThread;
        /// <summary>
        /// Ping thread is running ?
        /// </summary>
        private bool m_PingThreadRun;
        /// <summary>
        /// Database connection string
        /// </summary>
        private string m_ConnectionString;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Open a MySQL connection
        /// </summary>
        /// <param name="p_Host">Server host</param>
        /// <param name="p_Port">Server port</param>
        /// <param name="p_User">Username</param>
        /// <param name="p_Pass">Password</param>
        /// <param name="p_Name">Database name</param>
        /// <param name="p_Charset">Charset</param>
        public Connection(string p_Host, int p_Port, string p_User, string p_Pass, string p_Name, string p_Charset)
        {
            IsFree              = FreeValue;
            m_ConnectionString  = @"server=" + p_Host + ";userid=" + p_User + ";password=" + p_Pass + ";database=" + p_Name + ";charset=" + p_Charset + ";port=" + p_Port.ToString() + ";Convert Zero Datetime=True;Allow User Variables=True;Max Pool Size=2000;";

            try
            {
                DBConnection = new MySqlConnection(m_ConnectionString);
                DBConnection.Open();

                MySqlCommand l_Query = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999", DBConnection);
                l_Query.ExecuteNonQuery();
            }
            catch (MySql.Data.MySqlClient.MySqlException l_Exception)
            {
                Console.WriteLine(l_Exception.Message + " : " + l_Exception.Number);

                switch (l_Exception.Number)
                {
                    case 0:
                        Logs.Error.Log("[Database.Connection] Cannot connect to the MySQL server.", l_Exception);
                        break;
                    case 1045:
                    case 1042:
                        Logs.Error.Log("[Database.Connection] Invalid MySQL username/password.", l_Exception);
                        break;
                }

                throw new System.Exception("MySQL Error.");
            }

            m_PingThreadRun = true;

            m_PingThread = new System.Threading.Thread(PingThreatFunc);
            m_PingThread.Start();
        }
        /// <summary>
        /// On Connection release
        /// </summary>
        public void Dispose()
        {
            if (m_PingThread.IsAlive)
            {
                m_PingThreadRun = false;
                m_PingThread.Join();
            }

            DBConnection.Close();
            DBConnection = null;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Check the connection status and reconnect the worker if needed
        /// </summary>
        public bool IsReady()
        {
            switch (DBConnection.State)
            {
                case System.Data.ConnectionState.Closed:
                {
                    try
                    {
                        DBConnection = new MySqlConnection(m_ConnectionString);
                        DBConnection.Open();

                        MySqlCommand l_Query = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999", DBConnection);
                        l_Query.ExecuteNonQuery();

                        return true;
                    }
                    catch (MySql.Data.MySqlClient.MySqlException l_Exception)
                    {
                        switch (l_Exception.Number)
                        {
                            case 0:
                                Logs.Error.Log("[Database.Connection] Cannot connect to the MySQL server.", l_Exception);
                                break;
                            case 1045:
                            case 1042:
                                Logs.Error.Log("[Database.Connection] Invalid MySQL username/password.", l_Exception);
                                break;
                        }

                        return false;
                    }
                }

                case System.Data.ConnectionState.Broken:
                {
                    try
                    {
                        DBConnection.Close();
                        DBConnection.Open();
                        MySqlCommand l_Query = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999", DBConnection);
                        l_Query.ExecuteNonQuery();

                        return true;
                    }
                    catch (MySql.Data.MySqlClient.MySqlException l_Exception)
                    {
                        switch (l_Exception.Number)
                        {
                            case 0:
                                Logs.Error.Log("[Database.Connection] Cannot connect to the MySQL server.", l_Exception);
                                break;
                            case 1045:
                            case 1042:
                                Logs.Error.Log("[Database.Connection] Invalid MySQL username/password.", l_Exception);
                                break;
                        }

                        return false;
                    }
                }

                case System.Data.ConnectionState.Open:
                    return true;

                default:
                    return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Ping thread function
        /// </summary>
        private void PingThreatFunc()
        {
            long l_LastPing = Helper.Time.UnixTimeNow();

            while (m_PingThreadRun)
            {
                try
                {
                    /// Each 30 secs
                    if ((Helper.Time.UnixTimeNow() - l_LastPing) > 30 && System.Threading.Interlocked.Equals(this.IsFree, FreeValue))
                    {
                        if (IsReady())
                            l_LastPing = Helper.Time.UnixTimeNow();
                    }

                    System.Threading.Thread.Sleep(500);
                }
                catch (Exception l_Exception)
                {
                    Logs.Error.Log("[Database.Connection] PingThreatFunc", l_Exception);

                    System.Threading.Thread.Sleep(500);
                }
            }
        }



    }
}