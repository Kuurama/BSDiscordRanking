using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Xml;
using BSLevelUpDiscordSystem;

namespace BSLevelUpDiscordSystem1._2
{
    public class Player
    {
        private apiPlayerFull m_PlayerFull;
        private apiScores m_PlayerScore;
        private int m_NumberOfTry = 0;
        private bool m_HavePlayerInfo = false;

        public Player(string p_PlayerID)
        {
            var l_Path = @$".\Player\{p_PlayerID}\";

            Console.WriteLine(l_Path);

            GetInfos(p_PlayerID); /// Get Full Player Info.

            CreateDirectoryAndFile(l_Path); /// Make the score file if it don't exist.

            OpenSavedScore(l_Path); /// Make the player's instance retrieve all the data from the json file.

            CheckScore(p_PlayerID); /// Check for New Scores on the recent score api.

            ReWriteScore(l_Path); /// ReWrite the Current Json with the player's instance data.
        }

        private void GetInfos(string p_PlayerID)
        {
            using (WebClient l_WebClient = new WebClient())
            {
                try
                {
                    m_PlayerFull = JsonSerializer.Deserialize<apiPlayerFull>(
                        l_WebClient.DownloadString(@$"https://new.scoresaber.com/api/player/{p_PlayerID}/full"));
                    m_HavePlayerInfo = true;
                }
                catch (WebException l_Exception)
                {
                    if (
                        l_Exception.Response is HttpWebResponse
                            l_HttpWebResponse) // If the request succeeded (internet OK) but you got an error code.
                    {
                        if (l_HttpWebResponse.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            Console.WriteLine("RateLimited, Trying again in 45sec");
                            Thread.Sleep(45000);
                            GetInfos(p_PlayerID);
                        }
                    }
                    else // Internet Error, stop searching for Player's info after more than 5 try
                    {
                        if (m_NumberOfTry <= 5)
                        {
                            Console.WriteLine("Internet Connection Error, Check your Internet Supplier");
                            Console.WriteLine($"Retrying to get PLayer's Info in 30 sec : {m_NumberOfTry} out of 5 try");
                            m_NumberOfTry++;
                            Thread.Sleep(30000);
                            GetInfos(p_PlayerID);
                        }
                    }
                }
            }
        }

        private void CreateDirectoryAndFile(string p_Path)
        {
            if (!Directory.Exists(p_Path))
                Directory.CreateDirectory(p_Path ?? throw new InvalidOperationException());
        }

        private void OpenSavedScore(string p_Path)
        {
            try
            {
                using (StreamReader l_SR = new StreamReader(p_Path + @"\score.json"))
                {
                    m_PlayerScore = JsonSerializer.Deserialize<apiScores>(l_SR.ReadToEnd());
                }
            }
            catch (Exception l_Exception)
            {
                m_PlayerScore = new apiScores()
                {
                    scores = new List<apiScore>()
                };
            }
        }

        private void CheckScore(string p_PlayerID)
        {
            if (m_HavePlayerInfo) /// Check if Player have Player's Info
            {
                apiScores l_Result; /// Result From Request but Serialized.
                string l_URL;
                int l_Page = 1;
                int l_NumberOfAddedScore = 1;
                bool l_Skip = false;
                /// Avoid doing useless attempt, Check player's number of score (8 score per request).
                while ((m_PlayerFull.scoreStats.totalPlayCount / 8) + 2 >= l_Page && !l_Skip)
                {
                    l_URL =
                        @$"https://new.scoresaber.com/api/player/{m_PlayerFull.playerInfo.playerId}/scores/recent/{l_Page.ToString()}";
                    using (WebClient l_WebClient = new WebClient())
                    {
                        try
                        {
                            Console.WriteLine(l_URL);
                            l_Result = JsonSerializer.Deserialize<apiScores>(l_WebClient.DownloadString(l_URL));
                            l_Page++;

                            int l_Index = 0;

                            for (int i = 0; i < m_PlayerScore.scores.Count; i++)
                            {
                                if (l_Skip)
                                    break; /// break the for and l_Skip will cause the end of the While Loop.
                                if (l_Result != null)
                                    for (l_Index = 0; l_Index < l_Result.scores.Count; l_Index++)
                                    {
                                        if (l_Result.scores[l_Index].timeSet == m_PlayerScore.scores[i].timeSet)
                                        {
                                            l_Skip = true; /// One score already exist (will end the While Loop)
                                            break;
                                        }
                                    }
                            }

                            foreach (var l_NewScore in l_Result.scores) /// Remove old score and add new score.
                            {
                                if (m_PlayerScore.scores.RemoveAll(x =>
                                        x.leaderboardId == l_NewScore.leaderboardId && x.score != l_NewScore.score) > 0
                                    || !m_PlayerScore.scores.Any(x => x.leaderboardId == l_NewScore.leaderboardId))
                                {
                                    m_PlayerScore.scores.Add(l_NewScore);
                                    l_NumberOfAddedScore++;
                                }
                            }
                        }
                        catch (WebException e)
                        {
                            if (e.Response is HttpWebResponse l_Response)
                            {
                                Console.WriteLine("Status Code : {0}", l_Response.StatusCode);
                                if (l_Response.StatusCode == HttpStatusCode.NotFound)
                                {
                                    Console.WriteLine("No more Page to download");
                                    Console.WriteLine($"Fetched {l_Page} pages");
                                    Console.WriteLine($"{l_NumberOfAddedScore} new Score(s) Added");
                                    break;
                                }

                                if (l_Response.StatusCode == HttpStatusCode.TooManyRequests)
                                {
                                    Console.WriteLine("RateLimited, Trying again in 45sec");
                                    Thread.Sleep(45000);
                                }
                            }
                            else
                            {
                                Console.WriteLine("Internet Connection Error, Check your Internet Supplier");
                                Console.WriteLine($"But there is still {l_NumberOfAddedScore} new Score(s) Added");
                                if (m_NumberOfTry > 5)
                                {
                                    Console.WriteLine("OK Internet is Dead, Stopped Fetching Score.");
                                    break;
                                }
                                Console.WriteLine($"Retrying to Fetch PLayer's Scores in 30 sec : {m_NumberOfTry} out of 5 try");
                                m_NumberOfTry++;
                                Thread.Sleep(3000);
                            }
                        }
                    }
                }

                if (l_Skip)
                {
                    Console.WriteLine($"{l_NumberOfAddedScore} new Score(s) Added");
                    Console.WriteLine($"Fetched {l_Page - 1} pages");
                }
            }
            else /// If Player don't have player's info => Trying to get Player's Info
            {
                if (m_NumberOfTry <= 5)
                {
                    GetInfos(p_PlayerID);
                    CheckScore(p_PlayerID);
                }
            }
        }

        private void ReWriteScore(string p_Path)
        {
            File.WriteAllText(p_Path + @"\score.json",
                JsonSerializer.Serialize(m_PlayerScore));
        }
        
        private void ClearScore(string p_Path)
        {
            File.Delete(p_Path + @"\score.json");
        }
    }
}