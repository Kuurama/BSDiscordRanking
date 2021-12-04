using System;
using System.IO;
using System.Threading;

namespace BSDiscordRanking.Controllers
{
    public static class JsonDataBaseController
    {
        public static void CreateDirectory(string p_Path, int p_TryLimit = 3, int p_TryTimeout = 200)
        {
            /// This Method Create the p_PathDirectory.
            /// m_ErrorNumber will be increased at every error and lock the method if it exceed m_ErrorLimit

            if (p_TryLimit > 0)
            {
                if (!Directory.Exists(p_Path))
                    try
                    {
                        Directory.CreateDirectory(p_Path);
                        Console.WriteLine($"Directory {p_Path} Created");
                    }
                    catch (Exception l_Exception)
                    {
                        Console.WriteLine($"[Error] Couldn't Create Directory : {l_Exception.Message}");
                        Thread.Sleep(p_TryTimeout);
                        CreateDirectory(p_Path, p_TryLimit - 1, p_TryTimeout);
                    }
            }
            else
            {
                Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
            }
        }
    }
}