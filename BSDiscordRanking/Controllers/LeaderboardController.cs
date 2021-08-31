using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BSDiscordRanking.Formats;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BSDiscordRanking.Controllers
{
    public class LeaderboardController
    {
        private const string PATH = @".\";
        private const string FILENAME = @"\Leaderboard";
        private const int ERROR_LIMIT = 3;
        private int m_ErrorNumber = 0;
        public LeaderboardControllerFormat m_Leaderboard;

        public LeaderboardController()
        {
            LoadLeaderboard();
        }

        public void ManagePlayer(string p_Name, string p_ScoreSaberID, float p_Points, int p_Level, Trophy p_Trophy)
        {
            if (p_ScoreSaberID != null)
            {
                bool l_NewPlayer = true;
                foreach (var l_RankedPlayer in m_Leaderboard.Leaderboard)
                {
                    if (p_ScoreSaberID == l_RankedPlayer.ScoreSaberID)
                    {
                        l_NewPlayer = false;
                        if (p_Name != null)
                        {
                            l_RankedPlayer.Name = p_Name;
                        }

                        if (p_Points >= 0)
                        {
                            l_RankedPlayer.Points = p_Points;
                        }

                        if (p_Level >= 0)
                        {
                            l_RankedPlayer.Level = p_Level;
                        }

                        if (p_Trophy != null)
                        {
                            l_RankedPlayer.Trophy = p_Trophy;
                        }

                        break;
                    }
                }

                if (l_NewPlayer)
                {
                    RankedPlayer l_RankedPlayer = new RankedPlayer
                    {
                        Name = p_Name,
                        ScoreSaberID = p_ScoreSaberID
                    };

                    if (p_Points >= 0)
                        l_RankedPlayer.Points = p_Points;
                    else
                        l_RankedPlayer.Points = 0;

                    l_RankedPlayer.Level = p_Level >= 0 ? p_Level : 0;

                    if (p_Trophy != null)
                        l_RankedPlayer.Trophy = p_Trophy;
                    else
                    {
                        l_RankedPlayer.Trophy = new Trophy()
                        {
                            Plastic = 0,
                            Silver = 0,
                            Gold = 0,
                            Diamond = 0
                        };
                    }

                    m_Leaderboard.Leaderboard.Add(l_RankedPlayer);
                    ReWriteLeaderboard();
                }
                else
                {
                    ReWriteLeaderboard();
                    Console.WriteLine($"Leaderboard's info updated for player {p_ScoreSaberID}.");
                }
            }
            else
            {
                Console.WriteLine("This player is missing his score saber id");
            }
        }

        private void LoadLeaderboard()
        {
            if (m_ErrorNumber < ERROR_LIMIT)
            {
                if (!Directory.Exists(PATH))
                {
                    Console.WriteLine("Seems like you forgot to Create the Level Directory, attempting creation..");
                    CreateDirectory();
                    Console.WriteLine("Directory Created, continuing Loading Levels");
                }

                try
                {
                    using (StreamReader l_SR = new StreamReader($"{PATH}{FILENAME}.json"))
                    {
                        m_Leaderboard = JsonSerializer.Deserialize<LeaderboardControllerFormat>(l_SR.ReadToEnd());
                        if (m_Leaderboard == null) /// json contain "null"
                        {
                            m_Leaderboard = new LeaderboardControllerFormat()
                            {
                                Leaderboard = new List<RankedPlayer>()
                                {
                                    new RankedPlayer() { Name = "PlayerSample" }
                                }
                            };
                            Console.WriteLine($"Leaderboard Created (Empty Format), contained null");
                        }
                        else
                        {
                            if (m_Leaderboard.Leaderboard != null)
                            {
                                m_Leaderboard.Leaderboard = m_Leaderboard.Leaderboard.OrderByDescending(p_X => p_X.Points).ToList();
                            }

                            if (m_Leaderboard.Leaderboard is { Count: >= 2 }) m_Leaderboard.Leaderboard.RemoveAll(p_X => p_X.ScoreSaberID == null);

                            Console.WriteLine($"Leaderboard Loaded and Sorted");
                        }
                    }
                }
                catch (Exception) /// file format is wrong / there isn't any file.
                {
                    m_Leaderboard = new LeaderboardControllerFormat()
                    {
                        Leaderboard = new List<RankedPlayer>()
                        {
                            new RankedPlayer() { Name = "PlayerSample" }
                        }
                    };
                    Console.WriteLine($"Leaderboard Created (Empty Format)");
                }
            }
            else
            {
                Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
            }
        }

        private void CreateDirectory()
        {
            /// This Method Create the Directory needed to save and load the leaderboard's cache file from it's Path parameter.
            /// m_ErrorNumber will be increased at every error and lock the method if it exceed m_ErrorLimit

            if (m_ErrorNumber < ERROR_LIMIT)
            {
                if (!Directory.Exists(PATH))
                {
                    try
                    {
                        Directory.CreateDirectory(PATH);
                        Console.WriteLine($"Directory {PATH} Created");
                    }
                    catch (Exception l_Exception)
                    {
                        Console.WriteLine($"[Error] Couldn't Create Directory : {l_Exception.Message}");
                        m_ErrorNumber++;
                    }
                }
            }
            else
            {
                Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
            }
        }

        private void ReWriteLeaderboard()
        {
            /// This Method Serialise the data from m_Leaderboard and create the Cache.
            /// m_ErrorNumber will be increased at every error and lock the method if it exceed m_ErrorLimit

            if (m_ErrorNumber < ERROR_LIMIT)
            {
                try
                {
                    if (m_Leaderboard != null)
                    {
                        if (m_Leaderboard.Leaderboard.Count > 0)
                        {
                            m_Leaderboard.Leaderboard = m_Leaderboard.Leaderboard.OrderByDescending(p_X => p_X.Points).ToList();
                            File.WriteAllText($"{PATH}{FILENAME}.json", JsonSerializer.Serialize(m_Leaderboard));
                            Console.WriteLine($"{FILENAME}.json Updated and sorted ({m_Leaderboard.Leaderboard.Count} player in the leaderboard)");
                        }
                        else
                        {
                            Console.WriteLine("No Player in Leaderboard.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Seems like you forgot to load the Leaderboard, Attempting to load..");
                        LoadLeaderboard();
                        ReWriteLeaderboard();
                    }
                }
                catch
                {
                    Console.WriteLine(
                        "An error occured While attempting to Write the Leaderboard's cache file. (missing directory?)");
                    Console.WriteLine("Attempting to create the directory..");
                    m_ErrorNumber++;
                    CreateDirectory(); /// m_ErrorNumber will increase again if the directory creation fail.
                    Thread.Sleep(200);
                    ReWriteLeaderboard();
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