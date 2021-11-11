using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using BSDiscordRanking.Formats.Controller;

namespace BSDiscordRanking.Controllers
{
    public class MapLeaderboardController
    {
        private const string PATH = @"./Leaderboard/Maps/";
        private string m_Hash = null, m_Key = null, m_Name = null;
        private int m_MaxScore = 0;
        private int m_LeaderboardID;
        private const int ERROR_LIMIT = 3;
        private int m_ErrorNumber = 0;
        public MapLeaderboardFormat m_MapLeaderboard;

        public MapLeaderboardController(int p_LeaderboardID, string p_Hash = null, string p_Key = null, string p_Name = null, int p_MaxScore = 0)
        {
            m_LeaderboardID = p_LeaderboardID;
            m_Hash = p_Hash;
            m_Key = p_Key;
            m_Name = p_Name;
            m_MaxScore = p_MaxScore;
            LoadMapLeaderboard();
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

        private void ReWriteMapLeaderboard()
        {
            /// This Method Serialise the data from m_Leaderboard and create the Cache.
            /// m_ErrorNumber will be increased at every error and lock the method if it exceed m_ErrorLimit

            if (m_ErrorNumber < ERROR_LIMIT)
            {
                try
                {
                    if (m_MapLeaderboard != null)
                    {
                        if (m_MapLeaderboard.Leaderboard.Count > 0)
                        {
                            m_MapLeaderboard.Leaderboard = m_MapLeaderboard.Leaderboard.OrderByDescending(p_X => p_X.Score).ToList();
                            File.WriteAllText($"{PATH}{m_LeaderboardID.ToString()}.json", JsonSerializer.Serialize(m_MapLeaderboard));
                            // Console.WriteLine($"{m_LeaderboardID.ToString()}.json Updated and sorted ({m_MapLeaderboard.Leaderboard.Count} player in the leaderboard)");
                        }
                        else
                        {
                            Console.WriteLine("No Player in Leaderboard.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Seems like you forgot to load the Leaderboard, Attempting to load..");
                        LoadMapLeaderboard();
                        ReWriteMapLeaderboard();
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
                    ReWriteMapLeaderboard();
                }
            }
            else
            {
                Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
            }
        }

        private void LoadMapLeaderboard()
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
                    using (StreamReader l_SR = new StreamReader($"{PATH}{m_LeaderboardID.ToString()}.json"))
                    {
                        m_MapLeaderboard = JsonSerializer.Deserialize<MapLeaderboardFormat>(l_SR.ReadToEnd());
                        if (m_MapLeaderboard == null) /// json contain "null"
                        {
                            m_MapLeaderboard = new MapLeaderboardFormat()
                            {
                                hash = m_Hash,
                                key = m_Key,
                                name = m_Name,
                                MaxScore = m_MaxScore,
                                Leaderboard = new List<MapPlayerPass>()
                                {
                                    new MapPlayerPass()
                                    {
                                        Name = "PlayerSample"
                                    }
                                }
                            };
                            Console.WriteLine($"Leaderboard Created (Empty Format), contained null");
                        }
                        else
                        {
                            if (m_MapLeaderboard.Leaderboard != null)
                            {
                                /// m_Leaderboard.Leaderboard = m_Leaderboard.Leaderboard.OrderByDescending(p_X => p_X.PassPoints).ToList();
                            }

                            if (m_MapLeaderboard.Leaderboard is { Count: >= 2 }) m_MapLeaderboard.Leaderboard.RemoveAll(p_X => p_X.ScoreSaberID == null);

                            /// Console.WriteLine($"Map Leaderboard Loaded and Sorted");
                        }
                    }
                }
                catch (Exception) /// file format is wrong / there isn't any file.
                {
                    m_MapLeaderboard = new MapLeaderboardFormat()
                    {
                        hash = m_Hash,
                        key = m_Key,
                        name = m_Name,
                        MaxScore = m_MaxScore,
                        Leaderboard = new List<MapPlayerPass>()
                        {
                            new MapPlayerPass()
                            {
                                Name = "PlayerSample"
                            }
                        }
                    };
                    Console.WriteLine($"Map Leaderboard Created (Empty Format)");
                }
            }
            else
            {
                Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
            }
        }

        public bool ManagePlayerAndAutoWeightCheck(string p_Name, string p_ScoreSaberID, int p_Score)
        {
            /// This function Adds a player score to a map leaderboard, then return true if the autoweight need to be changed.
            if (p_ScoreSaberID != null)
            {
                bool l_NewPlayer = true;
                bool l_AutoWeightCheck = false;
                LoadMapLeaderboard();
                int l_SumOfFirstScores = 0;
                int l_NewSumOfFirstScores = 0;

                int l_MinimumNumberOfScore = ConfigController.GetConfig().minimumNumberOfScoreForAutoWeight;
                if (m_MapLeaderboard.Leaderboard.Count == l_MinimumNumberOfScore - 1)
                {
                    for (int l_Index = 0; l_Index < l_MinimumNumberOfScore - 1; l_Index++)
                    {
                        l_SumOfFirstScores += m_MapLeaderboard.Leaderboard[l_Index].Score;
                    }

                    l_AutoWeightCheck = true;
                }
                else if (m_MapLeaderboard.Leaderboard.Count >= l_MinimumNumberOfScore)
                {
                    for (int l_Index = 0; l_Index < l_MinimumNumberOfScore; l_Index++)
                    {
                        l_SumOfFirstScores += m_MapLeaderboard.Leaderboard[l_Index].Score;
                    }
                    
                    l_AutoWeightCheck = true;
                }

                for (int l_I = 0; l_I < m_MapLeaderboard.Leaderboard.Count; l_I++)
                {
                    if (p_ScoreSaberID == m_MapLeaderboard.Leaderboard[l_I].ScoreSaberID)
                    {
                        l_NewPlayer = false;
                        if (p_Name != null)
                        {
                            m_MapLeaderboard.Leaderboard[l_I].Name = p_Name;
                        }

                        if (p_Score >= 0)
                        {
                            m_MapLeaderboard.Leaderboard[l_I].Score = p_Score;
                        }
                        
                        break;
                    }
                }


                if (l_NewPlayer)
                {
                    MapPlayerPass l_MapPlayerPass = new MapPlayerPass()
                    {
                        Name = p_Name,
                        ScoreSaberID = p_ScoreSaberID,
                        Score = 0
                    };

                    if (p_Score >= 0)
                        l_MapPlayerPass.Score = p_Score;

                    m_MapLeaderboard.Leaderboard.Add(l_MapPlayerPass);
                    
                }
                
                ReWriteMapLeaderboard();
                 
                if (l_AutoWeightCheck)
                {
                    if (m_MapLeaderboard.Leaderboard.Count >= l_MinimumNumberOfScore)
                    {
                        for (int l_Index = 0; l_Index < ConfigController.GetConfig().minimumNumberOfScoreForAutoWeight; l_Index++)
                        {
                            l_NewSumOfFirstScores += m_MapLeaderboard.Leaderboard[l_Index].Score;
                        }

                        if (l_SumOfFirstScores < l_NewSumOfFirstScores && l_SumOfFirstScores != 0)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            else
            {
                Console.WriteLine("This player is missing his score saber id");
                return false;
            }
        }
    }
}