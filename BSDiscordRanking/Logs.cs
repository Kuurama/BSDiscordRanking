using System;

namespace BSDiscordRanking
{
    internal static class Logs
    {
        internal static class Error
        {
            public static void Log(string p_Comment, System.Exception p_Exception = null)
            {
                Console.WriteLine($"Error: {p_Comment}, {p_Exception}");
            }
        }

        internal static class Info
        {
            public static void Log(string p_Comment, System.Exception p_Exception = null)
            {
                Console.WriteLine($"Info: {p_Comment}, {p_Exception}");
            }
        }
    }
}
