using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using BSDiscordRanking.Formats.API;
using BSDiscordRanking.Formats.Controller;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BSDiscordRanking.Controllers
{
    public class MapLeaderboardController
    {
        private const string PATH = @"./Leaderboard/Maps/";
        private string m_Key = null;
        private int m_LeaderboardID;
        private ApiLeaderboard m_ApiLeaderboard;
        private const int ERROR_LIMIT = 3;
        private int m_ErrorNumber = 0;
        public MapLeaderboardFormat m_MapLeaderboard;

        public MapLeaderboardController(int p_LeaderboardID, string p_Key = null)
        {
            m_LeaderboardID = p_LeaderboardID;
            m_Key = p_Key;
            LoadMapLeaderboard();
            if (m_MapLeaderboard.info == null)
            {
                m_ApiLeaderboard = GetInfos(m_LeaderboardID);
                m_MapLeaderboard.info = m_ApiLeaderboard;
            }
        }
        
        private static ApiLeaderboard GetInfos(int p_LeaderboardID, int p_TryLimit = 3, int p_TryTimeout = 200)
        {
            /// This Method Get the Player's Info from the api, then Deserialize it to m_PlayerFull for later usage.
            /// It handle most of the exceptions possible
            ///
            /// If it fail to load the Player's Info, m_NumberOfTry = 6 => the program will stop trying. (limit is 5)
            /// and it mean the Score Saber ID is wrong.
            if (p_TryLimit > 0)
            {
                using (WebClient l_WebClient = new WebClient())
                {
                    try
                    {
                        ApiLeaderboard l_MapLeaderboardInfo = JsonConvert.DeserializeObject<ApiLeaderboard>(
                            l_WebClient.DownloadString(@$"https://scoresaber.com/api/leaderboard/by-id/{p_LeaderboardID}/info"));
                        return l_MapLeaderboardInfo;
                    }
                    catch (WebException l_Exception)
                    {
                        if (
                            l_Exception.Response is HttpWebResponse
                                l_HttpWebResponse) ///< If the request succeeded (internet OK) but you got an error code.
                        {
                            if (l_HttpWebResponse.StatusCode == HttpStatusCode.TooManyRequests)
                            {
                                Console.WriteLine("RateLimited, Trying again in 50sec");
                                Thread.Sleep(50000);
                                GetInfos(p_LeaderboardID);
                            }

                            if (l_HttpWebResponse.StatusCode == HttpStatusCode.NotFound)
                            {
                                Console.WriteLine("Wrong Leaderboard ID, Please contact an administrator");
                                return null;
                            }
                        }
                        else ///< Request Error => Internet or ScoreSaber API Down.
                        {
                            if (!Player.CheckScoreSaberAPI_Response("Leaderboard Full")) ///< Checking if ScoreSaber Api Return.
                            {
                                p_TryLimit--;
                                Console.WriteLine($"Retrying to get Leaderboard's Info in 30 sec : {p_TryLimit} try left");
                                Thread.Sleep(30000);
                                GetInfos(p_TryLimit, p_TryTimeout);
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
                return null;
            }
            
            return null;
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
                        if (m_MapLeaderboard.scores.Count > 0)
                        {
                            m_MapLeaderboard.scores = m_MapLeaderboard.scores.OrderByDescending(p_X => p_X.baseScore).ToList();
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
                    JsonDataBaseController.CreateDirectory(PATH); /// m_ErrorNumber will increase again if the directory creation fail.
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
                    JsonDataBaseController.CreateDirectory(PATH);
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
                                key = m_Key,
                                info = m_ApiLeaderboard,
                                scores = new List<MapPlayerScore>()
                                {
                                    new MapPlayerScore()
                                    {
                                        leaderboardPlayerInfo = new ApiLeaderboardPlayerInfo(){name = "PlayerSample"}
                                    }
                                },
                            };
                            Console.WriteLine($"Map Leaderboard Created (Empty Format), contained null");
                        }
                        else
                        {
                            if (m_MapLeaderboard.scores != null)
                            {
                                /// m_Leaderboard.Leaderboard = m_Leaderboard.Leaderboard.OrderByDescending(p_X => p_X.PassPoints).ToList();
                            }

                            if (m_MapLeaderboard.scores is { Count: >= 2 }) m_MapLeaderboard.scores.RemoveAll(p_X => p_X.leaderboardPlayerInfo.id == null);

                            /// Console.WriteLine($"Map Leaderboard Loaded and Sorted");
                        }
                    }
                }
                catch (Exception) /// file format is wrong / there isn't any file.
                {
                    m_MapLeaderboard = new MapLeaderboardFormat()
                    {
                        key = m_Key,
                        info = m_ApiLeaderboard,
                        scores = new List<MapPlayerScore>()
                        {
                            new MapPlayerScore()
                            {
                                leaderboardPlayerInfo = new ApiLeaderboardPlayerInfo(){name = "PlayerSample"}
                            }
                        },
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

        public bool ManagePlayerAndAutoWeightCheck(MapPlayerScore p_PlayerScore)
        {
            /// This function Adds a player score to a map leaderboard, then return true if the autoweight need to be changed.
            if (p_PlayerScore != null)
            {
                bool l_NewPlayer = true;
                bool l_AutoWeightCheck = false;
                LoadMapLeaderboard();
                int l_SumOfFirstScores = 0;
                int l_NewSumOfFirstScores = 0;

                int l_MinimumNumberOfScore = ConfigController.GetConfig().MinimumNumberOfScoreForAutoWeight;
                if (m_MapLeaderboard.scores.Count == l_MinimumNumberOfScore - 1)
                {
                    for (int l_Index = 0; l_Index < l_MinimumNumberOfScore - 1; l_Index++)
                    {
                        l_SumOfFirstScores += m_MapLeaderboard.scores[l_Index].baseScore;
                    }

                    l_AutoWeightCheck = true;
                }
                else if (m_MapLeaderboard.scores.Count >= l_MinimumNumberOfScore)
                {
                    for (int l_Index = 0; l_Index < l_MinimumNumberOfScore; l_Index++)
                    {
                        l_SumOfFirstScores += m_MapLeaderboard.scores[l_Index].baseScore;
                    }

                    l_AutoWeightCheck = true;
                }

                for (int l_I = 0; l_I < m_MapLeaderboard.scores.Count; l_I++)
                {
                    if (p_PlayerScore.leaderboardPlayerInfo.id == m_MapLeaderboard.scores[l_I].leaderboardPlayerInfo.id)
                    {
                        l_NewPlayer = false;
                        m_MapLeaderboard.scores[l_I] = p_PlayerScore;
                        break;
                    }
                }


                if (l_NewPlayer)
                {
                    m_MapLeaderboard.scores.Add(p_PlayerScore);
                }

                ReWriteMapLeaderboard();

                if (l_AutoWeightCheck)
                {
                    if (m_MapLeaderboard.scores.Count >= l_MinimumNumberOfScore)
                    {
                        for (int l_Index = 0; l_Index < ConfigController.GetConfig().MinimumNumberOfScoreForAutoWeight; l_Index++)
                        {
                            l_NewSumOfFirstScores += m_MapLeaderboard.scores[l_Index].baseScore;
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
                Console.WriteLine("This MapPlayerScore is null. Returning");
                return false;
            }
        }
    }
}