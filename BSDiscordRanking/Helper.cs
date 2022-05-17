using System;
using System.Globalization;
using System.Linq;

namespace BSDiscordRanking
{
    /// <summary>
    /// Helper class
    /// </summary>
    public class Helper
    {
        /// <summary>
        /// Time utilities
        /// </summary>
        public class Time
        {
            /// <summary>
            /// Unix Epoch
            /// </summary>
            private static readonly DateTime s_UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

            ////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// Get UnixTimestamp
            /// </summary>
            /// <returns>Unix timestamp</returns>
            public static Int64 UnixTimeNow()
            {
                return (Int64) (DateTime.UtcNow - s_UnixEpoch).TotalSeconds;
            }

            /// <summary>
            /// Convert DateTime to UnixTimestamp
            /// </summary>
            /// <param name="p_DateTime">The DateTime to convert</param>
            /// <returns></returns>
            public static Int64 ToUnixTime(DateTime p_DateTime)
            {
                return (Int64) p_DateTime.ToUniversalTime().Subtract(s_UnixEpoch).TotalSeconds;
            }

            /// <summary>
            /// Convert UnixTimestamp to DateTime
            /// </summary>
            /// <param name="p_TimeStamp"></param>
            /// <returns></returns>
            public static DateTime FromUnixTime(Int64 p_TimeStamp)
            {
                return s_UnixEpoch.AddSeconds(p_TimeStamp).ToLocalTime();
            }

            /// <summary>
            /// Try parse international data
            /// </summary>
            /// <param name="p_Input"></param>
            /// <param name="p_Result"></param>
            /// <returns></returns>
            public static bool TryParseInternational(string p_Input, out DateTime p_Result)
            {
                return DateTime.TryParse(p_Input, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out p_Result);
            }
        }

        /// <summary>
        /// Random utilities
        /// </summary>
        public class Random
        {
            /// <summary>
            /// Avoid to have 2 Random generator with the same seeds (and the same generated number suite)
            /// </summary>
            private static System.Random s_UniqueGenerator = new System.Random();
            /// <summary>
            /// Random lock object
            /// </summary>
            private static object s_RandomLock = new Object();

            ////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// Generate a random string
            /// </summary>
            /// <param name="p_Size">String size</param>
            /// <returns>The random string</returns>
            public static string GenerateString(int p_Size)
            {
                var l_Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                var l_Result = new string(
                    Enumerable.Repeat(l_Characters, p_Size)
                              .Select(s => s[Random.GenerateRandomNumber(s.Length)])
                              .ToArray());

                return l_Result;
            }

            /// <summary>
            /// Generate a random string/number
            /// </summary>
            /// <param name="p_Size">String size</param>
            /// <returns>The random string</returns>
            public static string GenerateStringNumber(int p_Size)
            {
                var l_Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                var l_Result = new string(
                    Enumerable.Repeat(l_Characters, p_Size)
                              .Select(s => s[Random.GenerateRandomNumber(s.Length)])
                              .ToArray());

                return l_Result;
            }

            public static int GenerateRandomNumber(int p_Max)
            {
                lock (s_RandomLock)
                {
                    return s_UniqueGenerator.Next(p_Max);
                }
            }
        }
    }
}
