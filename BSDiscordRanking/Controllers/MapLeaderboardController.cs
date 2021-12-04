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
        private const int ERROR_LIMIT = 3;
        private int m_ErrorNumber = 0;
        public MapLeaderboardFormat m_MapLeaderboard;

        public MapLeaderboardController(int p_LeaderboardID, string p_Key = null, int p_MaxScore = default)
        {
            m_LeaderboardID = p_LeaderboardID;
            m_Key = p_Key;
            LoadMapLeaderboard();
            m_MapLeaderboard.info ??= GetInfos(m_LeaderboardID);
            if (p_MaxScore != default)
            {
                m_MapLeaderboard.info.maxScore = p_MaxScore; /// Solution until Umbra fix his api.
            }

            switch (m_MapLeaderboard.scores)
            {
                case null:
                {
                    m_MapLeaderboard.scores = new List<MapPlayerScore>();
                    List<ApiScore> l_ApiScores = GetLeaderboardScores(p_LeaderboardID, p_MaxScore);
                    if (l_ApiScores != null)
                    {
                        l_ApiScores.RemoveAll(p_X => p_X.baseScore > m_MapLeaderboard.info.maxScore);
                        foreach (ApiScore l_Score in l_ApiScores)
                        {
                            m_MapLeaderboard.scores.Add(new MapPlayerScore()
                            {
                                customData = new LeaderboardCustomData() { isBotRegistered = false },
                                score = l_Score
                            });
                        }
                    }

                    break;
                }
            }
        }

        private static List<ApiScore> GetLeaderboardScores(int p_LeaderboardID, int p_TryLimit = 3)
        {
            /// This Method Get the Scores from a Leaderboard (with it's ID) from the Score Saber API.
            /// It handle most of the exceptions possible and return null if an error happen.
            ///
            /// If it fail to load the Scores, p_TryLimit = 0 => the program will stop trying.
            /// and it can mean the Leaderboard ID is wrong.
            if (p_TryLimit > 0)
            {
                using (WebClient l_WebClient = new WebClient())
                {
                    try
                    {
                        List<ApiScore> l_LeaderboardScores = JsonConvert.DeserializeObject<List<ApiScore>>(
                            l_WebClient.DownloadString(@$"https://scoresaber.com/api/leaderboard/by-id/{p_LeaderboardID}/scores"));
                        return l_LeaderboardScores;
                    }
                    catch (WebException l_Exception)
                    {
                        if (l_Exception.Response is HttpWebResponse
                                l_HttpWebResponse) ///< If the request succeeded (internet OK) but you got an error code.
                        {
                            if (l_HttpWebResponse.StatusCode == HttpStatusCode.TooManyRequests)
                            {
                                Console.WriteLine("RateLimited, Trying again in 50sec");
                                p_TryLimit--;
                                Thread.Sleep(50000);
                                return GetLeaderboardScores(p_LeaderboardID, p_TryLimit);
                            }
                            
                            if (l_HttpWebResponse.StatusCode == HttpStatusCode.BadGateway)
                            {
                                Console.WriteLine("BadGateway, Trying again in 5sec");
                                p_TryLimit--;
                                Thread.Sleep(5000);
                                return GetLeaderboardScores(p_LeaderboardID, p_TryLimit);
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
                                return GetLeaderboardScores(p_TryLimit);
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Too Many Errors => Method Locked (GetLeaderboardScores");
                Console.WriteLine("Please Contact an Administrator.");
                return null;
            }
            Console.WriteLine("Too many try on GetLeaderboardScores.");
            return null;
        }

        private static ApiLeaderboard GetInfos(int p_LeaderboardID, int p_TryLimit = 3)
        {
            /// This Method Get the Leaderboard's Info from the Score Saber API.
            /// It handle most of the exceptions possible and return null if an error happen.
            ///
            /// If it fail to load the Scores, p_TryLimit = 0 => the program will stop trying.
            /// and it can mean the Leaderboard ID is wrong.
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
                                p_TryLimit--;
                                Thread.Sleep(50000);
                                return GetInfos(p_LeaderboardID, p_TryLimit);
                            }

                            if (l_HttpWebResponse.StatusCode == HttpStatusCode.BadGateway)
                            {
                                Console.WriteLine("BadGateway, Trying again in 5sec");
                                p_TryLimit--;
                                Thread.Sleep(5000);
                                return GetInfos(p_LeaderboardID, p_TryLimit);
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
                                return GetInfos(p_LeaderboardID, p_TryLimit);
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
            Console.WriteLine("Too much try on LeaderboardGetInfo.");
            return null;
        }

        public void ReWriteMapLeaderboard()
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
                            m_MapLeaderboard.scores = m_MapLeaderboard.scores.OrderByDescending(p_X => p_X.score.baseScore).ToList();
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
                                forceAutoWeightRecalculation = false,
                                info = null,
                                scores = null
                            };
                            Console.WriteLine($"Map Leaderboard Created (Empty Format), contained null");
                        }
                        else
                        {
                            if (m_MapLeaderboard.scores != null)
                            {
                                /// m_Leaderboard.Leaderboard = m_Leaderboard.Leaderboard.OrderByDescending(p_X => p_X.PassPoints).ToList();
                            }

                            if (m_MapLeaderboard.scores is { Count: >= 2 }) m_MapLeaderboard.scores.RemoveAll(p_X => p_X.score.leaderboardPlayerInfo.id == null);

                            /// Console.WriteLine($"Map Leaderboard Loaded and Sorted");
                        }
                    }
                }
                catch (Exception) /// file format is wrong / there isn't any file.
                {
                    m_MapLeaderboard = new MapLeaderboardFormat()
                    {
                        key = m_Key,
                        forceAutoWeightRecalculation = false,
                        info = null,
                        scores = null
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

        public bool ManagePlayerAndAutoWeightCheck(MapPlayerScore p_PlayerScore, float p_CustomDataAutoWeight)
        {
            /// This function Adds a player score to a map leaderboard, then return true if the autoweight need to be changed.
            if (p_PlayerScore != null)
            {
                if (p_PlayerScore.score.baseScore > m_MapLeaderboard.info.maxScore)
                {
                    Console.WriteLine("Score Above 100%, Cheated scores aren't allowed.");
                    return false;
                }
                bool l_NewPlayer = true;
                bool l_AutoWeightCheck = false;
                int l_SumOfFirstScores = 0;
                int l_NewSumOfFirstScores = 0;

                int l_MinimumNumberOfScore = ConfigController.GetConfig().MinimumNumberOfScoreForAutoWeight;
                if ((m_MapLeaderboard.scores.Count == l_MinimumNumberOfScore - 1) || ((m_MapLeaderboard.scores.Count == l_MinimumNumberOfScore - 1) && (p_CustomDataAutoWeight == 0)))
                {
                    for (int l_Index = 0; l_Index < l_MinimumNumberOfScore - 1; l_Index++)
                    {
                        l_SumOfFirstScores += m_MapLeaderboard.scores[l_Index].score.baseScore;
                    }

                    l_AutoWeightCheck = true;
                }
                else if ((m_MapLeaderboard.scores.Count >= l_MinimumNumberOfScore) || ((m_MapLeaderboard.scores.Count >= l_MinimumNumberOfScore) && (p_CustomDataAutoWeight == 0)))
                {
                    for (int l_Index = 0; l_Index < l_MinimumNumberOfScore; l_Index++)
                    {
                        l_SumOfFirstScores += m_MapLeaderboard.scores[l_Index].score.baseScore;
                    }

                    l_AutoWeightCheck = true;
                }

                for (int l_I = 0; l_I < m_MapLeaderboard.scores.Count; l_I++)
                {
                    if (m_MapLeaderboard.scores[l_I].score is { leaderboardPlayerInfo: { } } && p_PlayerScore.score.leaderboardPlayerInfo.id == m_MapLeaderboard.scores[l_I].score.leaderboardPlayerInfo.id)
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

                m_MapLeaderboard.scores.RemoveAll(p_X => p_X.score.baseScore > m_MapLeaderboard.info.maxScore); /// Removing potentially > 100% scores, cheating isn't allowed.
                
                ReWriteMapLeaderboard();

                if (l_AutoWeightCheck)
                {
                    if (m_MapLeaderboard.scores.Count >= l_MinimumNumberOfScore)
                    {
                        for (int l_Index = 0; l_Index < ConfigController.GetConfig().MinimumNumberOfScoreForAutoWeight; l_Index++)
                        {
                            l_NewSumOfFirstScores += m_MapLeaderboard.scores[l_Index].score.baseScore;
                        }

                        if (l_SumOfFirstScores < l_NewSumOfFirstScores && l_SumOfFirstScores != 0 || p_CustomDataAutoWeight == 0)
                        {
                            return true;
                        }
                    }
                }

                if (m_MapLeaderboard.forceAutoWeightRecalculation)
                {
                    m_MapLeaderboard.forceAutoWeightRecalculation = false;
                    return true;
                }
                
                return false;
            }
            else
            {
                Console.WriteLine("This MapPlayerScore is null. Returning");
                return false;
            }
        }
        public static float RecalculateAutoWeight(int p_LeaderboardID, int p_DifficultlyMultiplier)
        {
            float l_SumOfPercentage = 0;
            ConfigFormat l_ConfigFormat = ConfigController.GetConfig();
            MapLeaderboardController l_MapLeaderboard = new MapLeaderboardController(p_LeaderboardID);
            if (l_MapLeaderboard.m_MapLeaderboard.scores.Count >= l_ConfigFormat.MinimumNumberOfScoreForAutoWeight)
            {
                for (int l_Index = 0; l_Index < l_ConfigFormat.MinimumNumberOfScoreForAutoWeight; l_Index++)
                {
                    if (l_MapLeaderboard.m_MapLeaderboard.info.maxScore > 0)
                    {
                        l_SumOfPercentage += ((float)l_MapLeaderboard.m_MapLeaderboard.scores[l_Index].score.baseScore / l_MapLeaderboard.m_MapLeaderboard.info.maxScore) * 100;
                    }
                    else
                    {
                        Console.WriteLine("Map MaxScore is negative/zero, can't recalculate weight.");
                        return 0;
                    }
                }

                float l_AveragePercentage = (l_SumOfPercentage / l_ConfigFormat.MinimumNumberOfScoreForAutoWeight);
                float l_AverageNeededPercentage = 100f - l_AveragePercentage;
                float l_NewWeight = (l_AverageNeededPercentage * 0.66f * p_DifficultlyMultiplier) / 32;
                return l_NewWeight;
            }
            else
            {
                return 0;
            }
        }
    }
    
}