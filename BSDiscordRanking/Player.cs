﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Discord.Modules.UserModule;
using BSDiscordRanking.Formats.API;
using BSDiscordRanking.Formats.Controller;
using BSDiscordRanking.Formats.Level;
using BSDiscordRanking.Formats.Player;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace BSDiscordRanking
{
    public class Player
    {
        private bool m_HavePlayerInfo = false;
        private LevelControllerFormat m_LevelController;
        private static string m_FolderPath = @"./Players/";
        private string m_Path;
        public ApiPlayerFull m_PlayerFull;
        private string m_PlayerID;
        public PlayerPassFormat m_PlayerPass;
        private List<ApiScores> m_PlayerScore;
        public PlayerStatsFormat m_PlayerStats;

        public Player(string p_PlayerID)
        {
            m_PlayerID = p_PlayerID;
            if (m_PlayerID != null)
            {
                m_Path = $"{m_FolderPath}{m_PlayerID}/";
                Console.WriteLine($"Selected Path is {m_Path}");
            }
            else
            {
                m_Path = null;
            }

            /////////////////////////////// Needed Setup Method ///////////////////////////////////

            GetInfos(); ///< Get Full Player Info.

            LoadStats();

            JsonDataBaseController.CreateDirectory(m_Path); ///< Make the player's scores file's directory.

            LoadSavedScore(); ///< Make the player's instance retrieve all the data from the json file.

            m_LevelController = LevelController.GetLevelControllerCache(); /// Load The LevelController Cache as we need the available level's list.

            ///////////////////////////////////////////////////////////////////////////////////////
        }

        public int GetPlayerLevel()
        {
            if (m_PlayerID != null)
            {
                int l_PlayerLevel = 0;
                if (m_PlayerStats is null)
                    return 0;

                if (m_PlayerStats.Levels is not null)
                {
                    m_PlayerStats.Levels = m_PlayerStats.Levels.OrderBy(p_X => p_X.LevelID).ToList();

                    foreach (var l_Level in m_PlayerStats.Levels)
                    {
                        if (l_Level.LevelID >= 0)
                        {
                            if (l_Level.Passed && l_Level.LevelID >= l_PlayerLevel || l_Level.LevelID == 0)
                                l_PlayerLevel = l_Level.LevelID;
                            else
                                break;
                        }
                    }

                    return l_PlayerLevel;
                }
            }

            return 0;
        }

        public static int GetStaticPlayerLevel(string p_PlayerID)
        {
            if (p_PlayerID != null)
            {
                int l_PlayerLevel = 0;
                PlayerStatsFormat l_PlayerStats = GetStaticStats(p_PlayerID);
                if (l_PlayerStats.Levels is not null)
                {
                    l_PlayerStats.Levels = l_PlayerStats.Levels.OrderBy(p_X => p_X.LevelID).ToList();

                    foreach (var l_Level in l_PlayerStats.Levels)
                    {
                        if (l_Level.LevelID >= 0)
                        {
                            if (l_Level.Passed && l_Level.LevelID >= l_PlayerLevel || l_Level.LevelID == 0)
                                l_PlayerLevel = l_Level.LevelID;
                            else
                                break;
                        }
                    }

                    return l_PlayerLevel;
                }
            }

            return 0;
        }

        public string GetPlayerID()
        {
            return m_PlayerID;
        }

        private void GetInfos(int p_TryLimit = 3, int p_TryTimeout = 200)
        {
            /// This Method Get the Player's Info from the api, then Deserialize it to m_PlayerFull for later usage.
            /// It handle most of the exceptions possible
            ///
            /// If it fail to load the Player's Info, m_NumberOfTry = 6 => the program will stop trying. (limit is 5)
            /// and it mean the Score Saber ID is wrong.
            if (m_PlayerID != null)
            {
                if (p_TryLimit > 0)
                {
                    using (WebClient l_WebClient = new WebClient())
                    {
                        try
                        {
                            m_PlayerFull = JsonSerializer.Deserialize<ApiPlayerFull>(
                                l_WebClient.DownloadString(@$"https://scoresaber.com/api/player/{m_PlayerID}/full"));
                            m_HavePlayerInfo = true;
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
                                    GetInfos();
                                }

                                if (l_HttpWebResponse.StatusCode == HttpStatusCode.NotFound)
                                {
                                    Console.WriteLine("Wrong Profile ID, Please contact an administrator");
                                    return;
                                }
                            }
                            else ///< Request Error => Internet or ScoreSaber API Down.
                            {
                                if (!CheckScoreSaberAPI_Response("Player Full")) ///< Checking if ScoreSaber Api Return.
                                {
                                    p_TryLimit--;
                                    Console.WriteLine($"Retrying to get PLayer's Info in 30 sec : {p_TryLimit} try left");
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
                }
            }
        }

        private void LoadSavedScore()
        {
            /// If First Launch* : Assign a Scores Sample to m_PlayerScore. (mean there isn't any cache file yet)
            /// This Method Load a cache file from its path. (The player's scores)
            /// then Deserialise it to m_PlayerScore.
            /// * => If the Scores's file failed to load (or don't exist), it will still load an empty scores format to m_PlayerScore.
            ///
            /// m_ErrorNumber will be increased at every error and lock the method if it exceed m_ErrorLimit

            if (m_PlayerID != null)
            {
                if (!Directory.Exists(m_Path))
                {
                    Console.WriteLine("Seems like you forgot to Create the Player Directory, returned");
                    return;
                }

                try
                {
                    using (StreamReader l_SR = new StreamReader(m_Path + "score.json"))
                    {
                        m_PlayerScore = JsonSerializer.Deserialize<List<ApiScores>>(l_SR.ReadToEnd());
                        Console.WriteLine($"Player {m_PlayerID} Successfully Loaded");
                    }
                }
                catch (Exception)
                {
                    m_PlayerScore = new List<ApiScores>();
                    Console.WriteLine($"Player {m_PlayerID} Created (Empty Format) => (Nothing to load/Wrong Format)");
                }
            }
        }

        public bool FetchScores(SocketCommandContext p_Context = null, int p_TryLimit = 3, int p_TryTimeout = 200)
        {
            /// If First Launch : It get ALL the player's score from the api then cache it to a score file. (mean there isn't any cache file yet)
            /// This Method Get the Player's Scores from the api, then call ReWriteScore() to Serialize them into a cache's file.
            /// This method is smart and know the api rate limit work and all the exception it can have to avoid them.
            /// Technically you can run this method without even running any other method in the constructor as it's
            /// smart enough to try to run all the needed method by itself.
            ///
            /// When this method download ALL the player's Score and check there is already some saved scores to stop the research,
            /// it also delete outdated score from old cached passes.
            /// it know when it should stop => Score already saved, => no more Pages, => internet issue, => api rate limit (retrying after 50sec).
            ///
            /// You need to run this method to Update the Player's Scores's cache (if there is any).
            ///
            /// If there is an issue with the scores's downloading and the cache miss some scores,
            /// run ClearScore() and try again downloading all Scores with that method (will act as "First Launch").

            if (m_PlayerID == null) return false;

            if (p_TryLimit > 0)
            {
                if (m_PlayerStats.IsFirstScan)
                {
                    p_Context?.Channel.SendMessageAsync("> <:clock1:868188979411959808> First Time Fetching player scores (downloading all of your passes once), this step will take a while! The bot will be unresponsive during the process.");
                }

                else
                {
                    p_Context?.Channel.SendMessageAsync("> <:clock1:868188979411959808> Fetching player scores, this step can take a while! The bot will be unresponsive during the process.");
                }

                if (m_HavePlayerInfo) /// Check if Player have Player's Info
                {
                    if (m_PlayerScore != null)
                    {
                        List<ApiScores> l_Result; ///< Result From Request but Serialized.
                        string l_URL;
                        int l_Page = 1;
                        int l_NumberOfAddedScore = 0;
                        bool l_Skip = false;
                        /// Avoid doing useless attempt, Check player's number of score (8 score per request).
                        while ((m_PlayerFull.scoreStats.totalPlayCount / 8) + 2 >= l_Page && !l_Skip)
                        {
                            l_URL = @$"https://scoresaber.com/api/player/{m_PlayerFull.id}/scores?sort=recent&page={l_Page.ToString()}";
                            l_URL = m_PlayerStats.IsFirstScan
                                ? @$"https://scoresaber.com/api/player/{m_PlayerID}/scores?limit=100&sort=recent&page={l_Page.ToString()}"
                                : @$"https://scoresaber.com/api/player/{m_PlayerFull.id}/scores?sort=recent&page={l_Page.ToString()}";

                            using (WebClient l_WebClient = new WebClient())
                            {
                                try
                                {
                                    Console.WriteLine(l_URL);
                                    l_Result = JsonConvert.DeserializeObject<List<ApiScores>>(l_WebClient.DownloadString(l_URL));
                                    l_Page++;

                                    // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                                    foreach (var l_PlayerScore in m_PlayerScore)
                                    {
                                        if (l_Skip)
                                            break; ///< break the for and l_Skip will cause the end of the While Loop.
                                        if (l_Result == null) continue;

                                        if (l_Result.Any(p_ResultScore => p_ResultScore.score.timeSet == l_PlayerScore.score.timeSet))
                                        {
                                            l_Skip = true; ///< One score already exist (will end the While Loop)
                                        }
                                    }

                                    if (l_Result != null)
                                        foreach (ApiScores l_NewScore in l_Result.Where(p_NewScore =>
                                            m_PlayerScore.RemoveAll(p_X => p_X.leaderboard.id == p_NewScore.leaderboard.id && p_X.score != p_NewScore.score) > 0
                                            || !(m_PlayerScore.Any(p_X => p_X.leaderboard.id == p_NewScore.leaderboard.id))))
                                        {
                                            m_PlayerScore.Add(l_NewScore);
                                            l_NumberOfAddedScore++;
                                        }
                                }
                                catch (WebException l_E)
                                {
                                    if (l_E.Response is HttpWebResponse l_Response)
                                    {
                                        Console.WriteLine("Status Code : {0}", l_Response.StatusCode);
                                        if (l_Response.StatusCode == HttpStatusCode.NotFound)
                                        {
                                            Console.WriteLine($"No more Page to download, Downloaded {l_Page} Page(s)");
                                            break;
                                        }

                                        if (l_Response.StatusCode == HttpStatusCode.TooManyRequests)
                                        {
                                            p_Context?.Channel.SendMessageAsync($"> <:clock1:868188979411959808> The bot got rate-limited, it will continue after 50s. (Page {l_Page} out of {m_PlayerFull.scoreStats.totalPlayCount / 8})");
                                            Thread.Sleep(50000);
                                        }
                                    }
                                    else
                                    {
                                        if (!CheckScoreSaberAPI_Response("Player Score"))
                                        {
                                            Console.WriteLine($"But {l_NumberOfAddedScore} new Score(s) will be Added");
                                            if (p_TryLimit <= 0)
                                            {
                                                Console.WriteLine("OK Internet is Dead, Stopped Fetching Score.");
                                                break; /// End the While Loop.
                                            }

                                            p_TryLimit--;
                                            Console.WriteLine($"Retrying to Fetch Player's Scores in 30 sec : {p_TryLimit} try left.");
                                            Thread.Sleep(30000);
                                        }
                                        else
                                        {
                                            p_TryLimit = 0;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        Console.WriteLine(l_Skip ? $"Fetched {l_Page - 1} pages" : $"Fetched {l_Page} pages");
                        Console.WriteLine($"{l_NumberOfAddedScore} new Score(s) Added");
                        ReWriteScore(); /// Caching the Score from the player instance.
                    }
                    else
                    {
                        Console.WriteLine("Seems like you forgot to load the Player's Scores, Returning.");
                    }
                }
                else /// If Player don't have player's info => Trying to get Player's Info
                {
                    Console.WriteLine("Player Is Missing player's info. Returned");
                }

                return m_PlayerStats.IsFirstScan;
            }

            Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
            Console.WriteLine("Please Contact an Administrator.");

            return false;
        }

        private void ReWriteScore(int p_TryLimit = 3, int p_TryTimeout = 200)
        {
            /// This Method Serialise the data from m_PlayerScore and cache it to a file depending on the path parameter
            /// Be Aware that it will replace the current cache file (if there is any), it shouldn't be an issue
            /// as you needed to Deserialised that cache (or set an empty format) to m_PlayerScore by using LoadSavedScore();
            ///
            /// m_ErrorNumber will be increased at every error and lock the method if it exceed m_ErrorLimit
            if (m_PlayerID != null)
            {
                if (p_TryLimit > 0)
                {
                    try
                    {
                        if (m_PlayerScore != null)
                        {
                            File.WriteAllText(m_Path + "score.json", JsonSerializer.Serialize(m_PlayerScore));
                            try
                            {
                                Console.WriteLine(
                                    $"{m_PlayerFull.name} Updated, ({m_PlayerScore.Count} Scores stored)");
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Seems Like you forgot to Get Player Info, Attempting to get player's info");
                                GetInfos();
                                ReWriteScore();
                            }
                        }
                        else
                        {
                            Console.WriteLine("Seems like you forgot to load the Player's Scores, Attempting to load..");
                            LoadSavedScore();
                            ReWriteScore();
                        }
                    }
                    catch
                    {
                        p_TryLimit--;
                        Console.WriteLine($"An error occured While attempting to Write the Player's Cache. (missing directory?), {p_TryLimit} try left.");
                        Thread.Sleep(p_TryTimeout);
                        ReWriteScore(p_TryLimit);
                    }
                }
                else
                {
                    Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                    Console.WriteLine("Please Contact an Administrator.");
                }
            }
        }

        private void ClearScore()
        {
            /// <summary>
            /// This Method Delete the player's scores's cache file.
            /// </summary>

            if (m_PlayerID != null)
            {
                try
                {
                    File.Delete(m_Path + "score.json");
                }
                catch (Exception l_Exception)
                {
                    Console.WriteLine($"Failed to delete File : {l_Exception.Message}");
                }
            }
        }

        public static bool CheckScoreSaberAPI_Response(string p_ApiRequestType) /// Return True if ScoreSaber API is Up.
        {
            /// <summary>
            /// This Method Check if the Score Saber's API return "hey",
            ///
            /// It return True is the Global API is UP, and false if it's Down
            /// 
            /// It is usefull as we can tell if score saber changed it's API => that Method is called
            /// When there is a request error from the checkScore method, if score saber API is up => API thingy we use changed,
            /// if it's down => internet is prob dead (or API total shutdown).
            /// 
            /// </summary>

            using (WebClient l_WebClient = new WebClient())
            {
                try /// Work if ScoreSaber Global API is up => API maybe Changed, Contact an administrator
                {
                    ApiCheck l_ApiCheck = JsonSerializer.Deserialize<ApiCheck>(l_WebClient.DownloadString("https://scoresaber.com/api/"));
                    if (ConfigController.GetConfig().m_ScoreSaberApiVersion != l_ApiCheck.version)
                    {
                        Console.WriteLine($"Api version changed from {ConfigController.GetConfig().m_ScoreSaberApiVersion} to {l_ApiCheck.version}, error occured.");
                    }
                    return true;
                }
                catch (WebException l_WebException) /// Score Saber Global API Down or Internet Error.
                {
                    Console.WriteLine($"{l_WebException.Message} (ScoreSaber API)");
                    Console.WriteLine("Internet Connection Error, Check your Internet Supplier");
                    return false;
                }
            }
        }

        public async Task<int> FetchPass(SocketCommandContext p_Context = null, bool p_IsBotRegistered = false)
        {
            if (m_PlayerID != null)
            {
                /// This Method Fetch the passes the Player did by checking all Levels and player's pass and add the matching ones.
                int l_Passes = 0;
                int l_PassesPerLevel = 0;
                int l_TotalAmountOfPass = 0;
                float l_TotalAccPoints = 0;
                float l_TotalPassPoints = 0;
                int l_NumberOfDifficulties = 0;
                int l_MessagesIndex = 0;
                float l_Weighting = 0f;
                string l_DifficultyShown = "";
                ConfigFormat l_Config = ConfigController.GetConfig();
                bool l_OldPlayerFirstScanStatus = m_PlayerStats.IsFirstScan;
                bool l_DiffGotLeaderboardID = false;
                bool l_DiffGotNewAutoWeight = false;
                int l_Plastic = 0, l_Silver = 0, l_Gold = 0, l_Diamond = 0;
                List<string> l_Messages = new List<string> { "" };
                int l_BiggerLevelID = Int32.MinValue;
                bool l_AboveLVLFourteenPass = false; /// Funny
                int l_OldPlayerLevel = GetPlayerLevel();
                PlayerPassFormat l_TempPlayerPass = ReturnPass();
                m_PlayerPass = new PlayerPassFormat
                {
                    SongList = new List<InPlayerSong>()
                };
                ResetTrophy();
                ResetLevels();
                List<Level> l_Levels = new List<Level>();
                foreach (int l_LevelID in m_LevelController.LevelID)
                {
                    l_Levels.Add(new Level(l_LevelID)); /// List of the current existing levels
                    if (l_BiggerLevelID < l_LevelID) l_BiggerLevelID = l_LevelID;
                }

                Console.WriteLine($"All {l_Levels.Count} Levels loaded.");

                try
                {
                    foreach (var l_Level in l_Levels.Select((p_Value, p_Index) => new { value = p_Value, index = p_Index }))
                    {
                        l_Weighting = l_Level.value.m_Level.customData.weighting;
                        foreach (SongFormat l_Song in l_Level.value.m_Level.songs)
                        {
                            foreach (ApiScores l_Score in m_PlayerScore)
                            {
                                if (!l_Score.score.modifiers.Contains("NF") && !l_Score.score.modifiers.Contains("NA") && !l_Score.score.modifiers.Contains("SS") && !l_Score.score.modifiers.Contains("NB"))
                                {
                                    if (string.Equals(l_Song.hash, l_Score.leaderboard.songHash, StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        bool l_MapStored = false;
                                        if (l_Song.difficulties is not null)
                                        {
                                            foreach (Difficulty l_Difficulty in l_Song.difficulties)
                                            {
                                                if (l_Score.leaderboard.difficultyRaw == $"_{l_Difficulty.name}_Solo{l_Difficulty.characteristic}")
                                                {
                                                    bool l_DiffExist = false;
                                                    bool l_TempDiffExist = false;
                                                    bool l_PassWeightAlreadySet = false;
                                                    bool l_AccWeightAlreadySet = false;
                                                    foreach (InPlayerSong l_CachedPassedSong in m_PlayerPass.SongList
                                                        .Where(p_CachedPassedSong => p_CachedPassedSong.DiffList != null && string.Equals(p_CachedPassedSong.hash, l_Song.hash, StringComparison.CurrentCultureIgnoreCase)))
                                                    {
                                                        l_MapStored = true;
                                                        foreach (InPlayerPassFormat l_CachedDifficulty in l_CachedPassedSong.DiffList)
                                                        {
                                                            if (l_TempPlayerPass.SongList != null)
                                                            {
                                                                foreach (InPlayerSong l_OldPassedSong in l_TempPlayerPass.SongList
                                                                    .Where(p_TempPassedSong => l_CachedPassedSong.hash == p_TempPassedSong.hash)
                                                                    .Where(p_TempPassedSong => p_TempPassedSong.DiffList.Any(p_TempPassedDifficulty => p_TempPassedDifficulty.Difficulty.characteristic == l_Difficulty.characteristic && p_TempPassedDifficulty.Difficulty.name == l_Difficulty.name && l_Score.score.baseScore >= l_Difficulty.customData.minScoreRequirement)))
                                                                {
                                                                    l_TempDiffExist = true;
                                                                }
                                                            }

                                                            if (l_CachedDifficulty.Difficulty.characteristic == l_Difficulty.characteristic && l_CachedDifficulty.Difficulty.name == l_Difficulty.name && l_Score.score.baseScore >= l_Difficulty.customData.minScoreRequirement)
                                                            {
                                                                l_DiffExist = true;
                                                                break;
                                                            }
                                                        }

                                                        if (!l_DiffExist)
                                                        {
                                                            l_Difficulty.customData.leaderboardID = l_Score.leaderboard.id;
                                                            l_CachedPassedSong.DiffList.Add(new InPlayerPassFormat()
                                                            {
                                                                Difficulty = l_Difficulty,
                                                                Score = l_Score.score.baseScore,
                                                                Rank = l_Score.score.rank
                                                            });
                                                            if (!l_TempDiffExist)
                                                            {
                                                                l_DifficultyShown = l_Difficulty.characteristic != "Standard" ? $"{l_Difficulty.characteristic} " : "";
                                                                if (!m_PlayerStats.IsFirstScan)
                                                                {
                                                                    if (l_Messages[l_MessagesIndex].Length + $":white_check_mark: Passed ***`{l_Difficulty.name} {l_DifficultyShown}- {l_Score.leaderboard.songName.Replace("`", @"\`").Replace("*", @"\*")}`*** in Level **{l_Level.value.m_LevelID}** (+{l_Weighting * 0.375f} {l_Config.PassPointsName}):\n> {l_Difficulty.customData.customPassText}\n\n"
                                                                            .Length
                                                                        > 1900)
                                                                    {
                                                                        l_MessagesIndex++;
                                                                    }

                                                                    if (l_Messages.Count < l_MessagesIndex + 1)
                                                                    {
                                                                        l_Messages.Add(""); /// Initialize the next used index.
                                                                    }

                                                                    /// Display new pass (new diff passed while there was already a passed diff) 1/2

                                                                    if (l_Difficulty.customData.customPassText != null)
                                                                        l_Messages[l_MessagesIndex] += $"\n:white_check_mark: Passed ***`{l_Difficulty.name} {l_DifficultyShown}- {l_Score.leaderboard.songName.Replace("`", @"\`").Replace("*", @"\*")}`*** in Level **{l_Level.value.m_LevelID}** (+{l_Weighting * 0.375f} {l_Config.PassPointsName}):\n> {l_Difficulty.customData.customPassText.Replace("_", " ")}\n\n";
                                                                    else
                                                                        l_Messages[l_MessagesIndex] += $":white_check_mark: Passed ***`{l_Difficulty.name} {l_DifficultyShown}- {l_Score.leaderboard.songName.Replace("`", @"\`").Replace("*", @"\*")}`*** in Level **{l_Level.value.m_LevelID}** (+{l_Weighting * 0.375f} {l_Config.PassPointsName})\n";
                                                                    if (l_Level.value.m_LevelID >= 14)
                                                                        l_AboveLVLFourteenPass = true; /// Funny 1/2
                                                                }

                                                                l_Passes++;
                                                                l_PassesPerLevel++;
                                                            }
                                                            else
                                                            {
                                                                l_PassesPerLevel++;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            l_PassesPerLevel++;
                                                        }
                                                    }

                                                    if (!l_MapStored && l_Score.score.baseScore >= l_Difficulty.customData.minScoreRequirement)
                                                    {
                                                        bool l_WasStored = false;
                                                        InPlayerSong l_PlayerPassFormat = new InPlayerSong()
                                                        {
                                                            DiffList = new List<InPlayerPassFormat>(),
                                                            hash = l_Song.hash.ToUpper(),
                                                            key = l_Song.key,
                                                            name = l_Song.name
                                                        };
                                                        l_Difficulty.customData.leaderboardID = l_Score.leaderboard.id;
                                                        l_PlayerPassFormat.DiffList.Add(new InPlayerPassFormat()
                                                        {
                                                            Difficulty = l_Difficulty,
                                                            Score = l_Score.score.baseScore,
                                                            Rank = l_Score.score.rank
                                                        });
                                                        l_PlayerPassFormat.name = l_Song.name;
                                                        m_PlayerPass.SongList.Add(l_PlayerPassFormat);

                                                        if (l_TempPlayerPass.SongList != null)
                                                        {
                                                            if (l_TempPlayerPass.SongList.Any(p_TempPassedSong => string.Equals(p_TempPassedSong.hash, l_Song.hash, StringComparison.CurrentCultureIgnoreCase)))
                                                            {
                                                                l_WasStored = true;
                                                            }
                                                        }

                                                        if (!l_WasStored)
                                                        {
                                                            l_DifficultyShown = l_Difficulty.characteristic != "Standard" ? $"{l_Difficulty.characteristic} " : "";
                                                            if (!m_PlayerStats.IsFirstScan)
                                                            {
                                                                if (l_Messages[l_MessagesIndex].Length >
                                                                    1900 -
                                                                    $":white_check_mark: Passed ***`{l_Difficulty.name} {l_DifficultyShown}- {l_Score.leaderboard.songName.Replace("`", @"\`").Replace("*", @"\*")}`*** in Level **{l_Level.value.m_LevelID}** (+{l_Weighting * 0.375f} {l_Config.PassPointsName}):\n> {l_Difficulty.customData.customPassText}\n\n"
                                                                        .Length)
                                                                {
                                                                    l_MessagesIndex++;
                                                                }

                                                                if (l_Messages.Count < l_MessagesIndex + 1)
                                                                {
                                                                    l_Messages.Add(""); /// Initialize the next used index.
                                                                }

                                                                /// Display new pass (new diff passed while there was already a passed diff) 2/2
                                                                if (l_Difficulty.customData.customPassText != null)
                                                                    l_Messages[l_MessagesIndex] +=
                                                                        $"\n:white_check_mark: Passed ***`{l_Difficulty.name} {l_DifficultyShown}- {l_Score.leaderboard.songName.Replace("`", @"\`").Replace("*", @"\*")}`*** in Level **{l_Level.value.m_LevelID}** (+{l_Weighting * 0.375f} {l_Config.PassPointsName}):\n> {l_Difficulty.customData.customPassText.Replace("_", " ")}\n\n";
                                                                else
                                                                    l_Messages[l_MessagesIndex] +=
                                                                        $":white_check_mark: Passed ***`{l_Difficulty.name} {l_DifficultyShown}- {l_Score.leaderboard.songName.Replace("`", @"\`").Replace("*", @"\*")}`*** in Level **{l_Level.value.m_LevelID}** (+{l_Weighting * 0.375f} {l_Config.PassPointsName})\n";
                                                                if (l_Level.value.m_LevelID >= 14)
                                                                    l_AboveLVLFourteenPass = true; /// Funny 2/2
                                                            }

                                                            l_Passes++;
                                                            l_PassesPerLevel++;
                                                        }
                                                        else
                                                        {
                                                            l_PassesPerLevel++;
                                                        }
                                                    }

                                                    if (l_Difficulty.customData.leaderboardID != l_Score.leaderboard.id)
                                                    {
                                                        l_Difficulty.customData.leaderboardID = l_Score.leaderboard.id;
                                                        l_DiffGotLeaderboardID = true;
                                                    }
                                                    
                                                    
                                                    MapLeaderboardController l_MapLeaderboardController = new MapLeaderboardController(l_Score.leaderboard.id, l_Song.key);
                                                    ApiLeaderboardPlayerInfo l_LeaderboardPlayerInfo = new ApiLeaderboardPlayerInfo()
                                                    {
                                                        country = m_PlayerFull.country,
                                                        id = m_PlayerFull.id,
                                                        name = m_PlayerFull.name,
                                                        profilePicture = m_PlayerFull.profilePicture,
                                                        permissions = m_PlayerFull.permissions,
                                                        role = m_PlayerFull.role
                                                    };
                                                    bool l_NeedNewAutoWeight = l_MapLeaderboardController.ManagePlayerAndAutoWeightCheck(new MapPlayerScore()
                                                    {
                                                        id = l_Score.score.id,
                                                        rank = l_Score.score.rank,
                                                        isBotRegistered = p_IsBotRegistered,
                                                        leaderboardPlayerInfo = l_LeaderboardPlayerInfo,
                                                        badCuts = l_Score.score.badCuts,
                                                        baseScore = l_Score.score.baseScore,
                                                        fullCombo = l_Score.score.fullCombo,
                                                        hasReplay = l_Score.score.hasReplay,
                                                        hmd = l_Score.score.hmd,
                                                        maxCombo = l_Score.score.maxCombo,
                                                        timeSet = l_Score.score.timeSet,
                                                        weight = l_Score.score.weight,
                                                        modifiers = l_Score.score.modifiers,
                                                        multiplier = l_Score.score.multiplier,
                                                        missedNotes = l_Score.score.missedNotes,
                                                        modifiedScore = l_Score.score.modifiedScore,
                                                        pp = l_Score.score.pp
                                                    });

                                                    if (l_NeedNewAutoWeight)
                                                    {
                                                        l_Difficulty.customData.AutoWeight = l_Level.value.RecalculateAutoWeight(l_Score.leaderboard.id);
                                                        Console.WriteLine($"New AutoWeight set on {l_Difficulty.name} {l_DifficultyShown} - {l_Score.leaderboard.songName.Replace("`", @"\`").Replace("*", @"\*")}");
                                                        l_DiffGotNewAutoWeight = true;
                                                    }

                                                    if (l_Difficulty.customData.forceManualWeight)
                                                    {
                                                        if (!l_Config.OnlyAutoWeightForAccLeaderboard)
                                                        {
                                                            l_TotalAccPoints += ((float)l_Score.score.baseScore / l_Difficulty.customData.maxScore) * 100f * 0.375f * l_Difficulty.customData.manualWeight;
                                                            l_AccWeightAlreadySet = true;
                                                        }

                                                        if (!l_Config.OnlyAutoWeightForPassLeaderboard)
                                                        {
                                                            l_TotalPassPoints += 0.375f * l_Difficulty.customData.manualWeight;
                                                            l_PassWeightAlreadySet = true;
                                                        }
                                                    }

                                                    if (l_Difficulty.customData.AutoWeight > 0 && l_Config.AutomaticWeightCalculation)
                                                    {
                                                        if (!l_AccWeightAlreadySet && l_Config.OnlyAutoWeightForAccLeaderboard)
                                                        {
                                                            l_TotalAccPoints += ((float)l_Score.score.baseScore / l_Difficulty.customData.maxScore) * 100f * 0.375f * l_Difficulty.customData.AutoWeight;
                                                            l_AccWeightAlreadySet = true;
                                                        }

                                                        if (!l_PassWeightAlreadySet && l_Config.OnlyAutoWeightForPassLeaderboard)
                                                        {
                                                            l_TotalPassPoints += 0.375f * l_Difficulty.customData.AutoWeight;
                                                            l_PassWeightAlreadySet = true;
                                                        }
                                                    }

                                                    if (l_Config.PerPlaylistWeighting)
                                                    {
                                                        if (!l_Config.OnlyAutoWeightForAccLeaderboard && !l_AccWeightAlreadySet)
                                                        {
                                                            l_TotalAccPoints += ((float)l_Score.score.baseScore / l_Difficulty.customData.maxScore) * 100f * 0.375f * l_Level.value.m_Level.customData.weighting;
                                                        }

                                                        if (!l_Config.OnlyAutoWeightForPassLeaderboard && !l_PassWeightAlreadySet)
                                                        {
                                                            l_TotalPassPoints += 0.375f * l_Level.value.m_Level.customData.weighting;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }


                        l_NumberOfDifficulties += l_Level.value.m_Level.songs.SelectMany(p_Song => p_Song.difficulties).Count();


                        if (l_NumberOfDifficulties > 0)
                        {
                            switch (l_PassesPerLevel * 100 / l_NumberOfDifficulties)
                            {
                                case 0:
                                {
                                    break;
                                }
                                case <= 39:
                                {
                                    l_Plastic = 1;
                                    break;
                                }
                                case <= 69:
                                {
                                    l_Silver = 1;
                                    break;
                                }
                                case <= 99:
                                {
                                    l_Gold = 1;
                                    break;
                                }

                                case 100:
                                {
                                    l_Diamond = 1;
                                    break;
                                }
                            }
                        }

                        if (l_PassesPerLevel > 0 && (m_PlayerStats.IsFirstScan))
                        {
                            if (l_Messages[l_MessagesIndex].Length > 1900 - $"".Length)
                            {
                                l_MessagesIndex++;
                            }

                            if (l_Messages.Count < l_MessagesIndex + 1)
                            {
                                l_Messages.Add(""); /// Initialize the next used index.
                            }


                            /// Display new pass on first scan message.
                            if (l_Config.PerPlaylistWeighting)
                            {
                                l_Messages[l_MessagesIndex] += $":white_check_mark: You passed `{l_PassesPerLevel}/{l_NumberOfDifficulties}` maps in Level **{l_Level.value.m_LevelID}** (+{l_Weighting * 0.375f * l_PassesPerLevel} {l_Config.PassPointsName})\n";
                            }
                        }

                        Trophy l_Trophy = new Trophy()
                        {
                            Plastic = l_Plastic,
                            Silver = l_Silver,
                            Gold = l_Gold,
                            Diamond = l_Diamond,
                        };
                        l_Plastic = 0;
                        l_Silver = 0;
                        l_Gold = 0;
                        l_Diamond = 0;

                        if (m_PlayerStats.Levels is not null) /// If it is maybe you forgot to GetStats()?
                        {
                            // ReSharper disable once SuggestVarOrType_BuiltInTypes
                            int l_PlayerLevelIndex = m_PlayerStats.Levels.FindIndex(p_X => p_X.LevelID == l_Level.value.m_LevelID);
                            if (l_PlayerLevelIndex >= 0)
                            {
                                if (l_PassesPerLevel > 0)
                                    m_PlayerStats.Levels[l_PlayerLevelIndex].Passed = true;

                                m_PlayerStats.Levels[l_PlayerLevelIndex].Trophy = l_Trophy;
                            }
                            else
                            {
                                if (l_PassesPerLevel > 0)
                                {
                                    m_PlayerStats.Levels.Add(new PassedLevel()
                                    {
                                        LevelID = l_Level.value.m_LevelID,
                                        Passed = true,
                                        Trophy = l_Trophy
                                    });
                                }
                                else
                                {
                                    m_PlayerStats.Levels.Add(new PassedLevel()
                                    {
                                        LevelID = l_Level.value.m_LevelID,
                                        Passed = false,
                                        Trophy = l_Trophy
                                    });
                                }
                            }
                        }

                        l_TotalAmountOfPass += l_PassesPerLevel;

                        /*if (ConfigController.GetConfig().PerPlaylistWeighting)
                        {
                            
                            l_TotalPoints += l_Weighting * 0.375f * l_PassesPerLevel; /// Current RPL formula from BSCC
                        }*/
                        l_PassesPerLevel = 0;
                        l_NumberOfDifficulties = 0;

                        if (l_DiffGotLeaderboardID)
                        {
                            l_Level.value.ReWritePlaylist(false);
                            l_DiffGotLeaderboardID = false;
                        }

                        if (l_DiffGotNewAutoWeight)
                        {
                            l_Level.value.ReWritePlaylist(false);
                            l_DiffGotNewAutoWeight = false;
                        }
                    }

                    m_PlayerStats.PassPoints = l_TotalPassPoints;
                    m_PlayerStats.AccPoints = l_TotalAccPoints;
                    m_PlayerStats.IsFirstScan = false;
                    m_PlayerStats.TotalNumberOfPass = l_TotalAmountOfPass;
                    ReWriteStats();

                    ReWritePass();

                    Color l_Color = UserModule.GetRoleColor(RoleController.ReadRolesDB().Roles, p_Context.Guild.Roles, l_OldPlayerLevel);

                    bool l_IsFirstMessage = true;
                    if (l_Passes >= 1)
                        foreach (string l_Message in l_Messages)
                        {
                            EmbedBuilder l_Builder = new EmbedBuilder();
                            if (l_IsFirstMessage)
                            {
                                if (l_OldPlayerFirstScanStatus)
                                {
                                    l_Builder.WithTitle("You passed maps in the following levels:");
                                    l_Builder.WithTitle("You passed the following maps:");
                                }
                            }

                            l_IsFirstMessage = false;
                            l_Builder.WithDescription(l_Message);
                            l_Builder.WithColor(l_Color);
                            Embed l_Embed = l_Builder.Build();
                            await p_Context.Channel.SendMessageAsync(null, embed: l_Embed).ConfigureAwait(false);
                        }

                    if (GetPlayerLevel() == 8 && l_AboveLVLFourteenPass)
                    {
                        await p_Context.Channel.SendMessageAsync($"Ohh that's quite pog, but `{ConfigController.GetConfig().CommandPrefix[0]}lvl9` when <a:KekBoom:905995426786856971>");
                    }

                    return l_Passes;
                }
                catch (Exception l_Exception)
                {
                    Console.WriteLine($"error : {l_Exception.Data}");
                    await p_Context.Channel.SendMessageAsync($"Error : {l_Exception.Message}");
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        public static PlayerPassFormat ReturnStaticPass(string p_PlayerID)
        {
            /// This method return the Serialised version of the current saved Player's pass, ruturn an empty on if none.
            PlayerPassFormat l_PlayerPass = new PlayerPassFormat();
            if (p_PlayerID != null)
            {
                if (!Directory.Exists($"{m_FolderPath}{p_PlayerID}/"))
                {
                    Console.WriteLine("Seems like you forgot to Create the Player Directory, Returning empty pass.");
                }
                else
                {
                    try
                    {
                        using (StreamReader l_SR = new StreamReader($@"{$"{m_FolderPath}{p_PlayerID}/"}pass.json"))
                        {
                            l_PlayerPass = JsonSerializer.Deserialize<PlayerPassFormat>(l_SR.ReadToEnd());
                            if (l_PlayerPass == null) /// json contain "null"
                            {
                                l_PlayerPass = new PlayerPassFormat()
                                {
                                    SongList = new List<InPlayerSong>()
                                    {
                                        new InPlayerSong()
                                    }
                                };
                                Console.WriteLine($"PlayerPass Created (Empty Format), file contained null");
                            }
                            else
                            {
                                Console.WriteLine($"pass.json of {p_PlayerID} loaded");
                            }
                        }
                    }
                    catch (Exception) /// file format is wrong / there isn't any file.
                    {
                        l_PlayerPass = new PlayerPassFormat()
                        {
                            SongList = new List<InPlayerSong>()
                            {
                                new InPlayerSong()
                            }
                        };
                        Console.WriteLine($@"{$"{m_FolderPath}{p_PlayerID}/"}pass.json Created (Empty Format)");
                    }
                }

                return l_PlayerPass;
            }

            else
            {
                return l_PlayerPass = new PlayerPassFormat()
                {
                    SongList = new List<InPlayerSong>()
                    {
                        new InPlayerSong()
                    }
                };
            }
        }

        public PlayerPassFormat ReturnPass()
        {
            // ReSharper disable once InvertIf
            if (m_PlayerID != null)
            {
                if (m_PlayerPass is not null)
                {
                    return m_PlayerPass;
                }
            }

            return new PlayerPassFormat()
            {
                SongList = new List<InPlayerSong>()
            };
        }

        public void LoadPass()
        {
            m_PlayerPass = GetStaticPass(m_PlayerID);
        }

        public static PlayerPassFormat GetStaticPass(string p_PlayerID)
        {
            PlayerPassFormat l_PlayerPass = new PlayerPassFormat
            {
                SongList = new List<InPlayerSong>()
            };

            if (p_PlayerID != null)
            {
                try
                {
                    using StreamReader l_SR = new StreamReader($"{m_FolderPath}{p_PlayerID}/pass.json");
                    l_PlayerPass = JsonSerializer.Deserialize<PlayerPassFormat>(l_SR.ReadToEnd());
                    return l_PlayerPass;
                }
                catch (Exception) /// file format is wrong / there isn't any file.
                {
                    l_PlayerPass = new PlayerPassFormat
                    {
                        SongList = new List<InPlayerSong>()
                    };
                    Console.WriteLine("This player don't have any pass yet");
                    return l_PlayerPass;
                }
            }

            return l_PlayerPass;
        }

        private void ReWritePass(int p_TryLimit = 3, int p_TryTimeout = 200)
        {
            if (m_PlayerID != null)
            {
                if (p_TryLimit > 0)
                {
                    try
                    {
                        if (m_PlayerPass != null)
                        {
                            File.WriteAllText($@"{m_Path}pass.json", JsonSerializer.Serialize(m_PlayerPass));
                            try
                            {
                                Console.WriteLine($"Pass's file of {m_PlayerFull.name} Updated, {m_PlayerPass.SongList.Count} song(s) are stored (song number <= number of scores : multiple diff)");
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Seems Like you forgot to Get Player Info, Attempting to get player's info");
                                GetInfos();
                                ReWritePass();
                            }
                        }

                        else
                        {
                            Console.WriteLine("Seems like you forgot to fetch the Player's Passes, Attempting to fetch..");
                            FetchScores();
                        }
                    }
                    catch
                    {
                        Console.WriteLine("An error occured While attempting to Write the PlayerPass's Cache. (missing directory? or permission?)");
                        p_TryLimit--;
                        Thread.Sleep(p_TryTimeout);
                        ReWritePass(p_TryLimit, p_TryTimeout);
                    }
                }
            }
        }

        private void ResetTrophy()
        {
            if (m_PlayerStats.Levels != null)
            {
                foreach (var l_PlayerStatsLevel in m_PlayerStats.Levels)
                {
                    l_PlayerStatsLevel.Trophy ??= new Trophy();
                    l_PlayerStatsLevel.Trophy.Plastic = 0;
                    l_PlayerStatsLevel.Trophy.Silver = 0;
                    l_PlayerStatsLevel.Trophy.Gold = 0;
                    l_PlayerStatsLevel.Trophy.Diamond = 0;
                }
            }
        }

        public void ResetLevels()
        {
            m_PlayerStats.Levels?.Clear();
            m_PlayerStats.Levels = new List<PassedLevel>()
            {
                new PassedLevel()
                {
                    LevelID = 0,
                    Passed = false,
                    Trophy = new Trophy()
                    {
                        Plastic = 0,
                        Silver = 0,
                        Gold = 0,
                        Diamond = 0
                    }
                }
            };
        }

        public void ReWriteStats(int p_TryLimit = 3, int p_TryTimeout = 200)
        {
            if (m_PlayerID != null)
            {
                if (p_TryLimit > 0)
                {
                    try
                    {
                        if (m_PlayerStats != null)
                        {
                            File.WriteAllText($@"{m_Path}stats.json", JsonSerializer.Serialize(m_PlayerStats));

                            Console.WriteLine($"Stats's file of {m_PlayerFull.name} Updated");
                        }
                        else
                        {
                            Console.WriteLine("Seems like you forgot to fetch the Player's Stats.");
                        }
                    }
                    catch

                    {
                        Console.WriteLine("An error occured While attempting to Write the PlayerStats's Cache. (missing directory? or permission?)");
                        p_TryLimit--;
                        Thread.Sleep(p_TryTimeout);
                        ReWriteStats(p_TryLimit, p_TryTimeout);
                    }
                }
                else
                {
                    Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                    Console.WriteLine("Please Contact an Administrator.");
                }
            }
        }

        public PlayerStatsFormat GetStats()
        {
            if (m_PlayerID != null)
            {
                if (m_PlayerStats is not null)
                {
                    return m_PlayerStats;
                }
            }

            return new PlayerStatsFormat()
            {
                Levels = new List<PassedLevel>(),
                AccPoints = 0,
                IsFirstScan = true,
                PassPoints = 0,
                TotalNumberOfPass = 0
            };
        }

        public static PlayerStatsFormat GetStaticStats(string p_PlayerID)
        {
            PlayerStatsFormat l_PlayerStats;
            if (p_PlayerID != null)
            {
                try
                {
                    using StreamReader l_SR = new StreamReader($@"{m_FolderPath}{p_PlayerID}/stats.json");
                    l_PlayerStats = JsonSerializer.Deserialize<PlayerStatsFormat>(l_SR.ReadToEnd());
                }
                catch (Exception) /// file format is wrong / there isn't any file.
                {
                    l_PlayerStats = new PlayerStatsFormat
                    {
                        Levels = new List<PassedLevel>()
                        {
                            new PassedLevel()
                            {
                                LevelID = 0,
                                Passed = false,
                                Trophy = new Trophy()
                                {
                                    Plastic = 0,
                                    Silver = 0,
                                    Gold = 0,
                                    Diamond = 0
                                }
                            }
                        },
                        TotalNumberOfPass = new int(),
                        PassPoints = new int(),
                        IsFirstScan = true
                    };
                    Console.WriteLine($"This player don't have any stats yet");
                }

                if (l_PlayerStats is { Levels: null })
                {
                    l_PlayerStats.Levels = new List<PassedLevel>()
                    {
                        new PassedLevel()
                        {
                            LevelID = 0,
                            Passed = false,
                            Trophy = new Trophy()
                            {
                                Plastic = 0,
                                Silver = 0,
                                Gold = 0,
                                Diamond = 0
                            }
                        }
                    };
                }
            }
            else
            {
                l_PlayerStats = new PlayerStatsFormat
                {
                    Levels = new List<PassedLevel>()
                    {
                        new PassedLevel()
                        {
                            LevelID = 0,
                            Passed = false,
                            Trophy = new Trophy()
                            {
                                Plastic = 0,
                                Silver = 0,
                                Gold = 0,
                                Diamond = 0
                            }
                        }
                    },
                    TotalNumberOfPass = new int(),
                    PassPoints = new int(),
                    IsFirstScan = true
                };
            }

            return l_PlayerStats;
        }

        public void LoadStats()
        {
            m_PlayerStats = GetStaticStats(m_PlayerID);
        }

        public PlayerPassPerLevelFormat GetPlayerPassPerLevel()
        {
            PlayerPassPerLevelFormat l_PlayerPassPerLevelFormat = new PlayerPassPerLevelFormat()
            {
                Levels = new List<InPassPerLevelFormat>()
            };

            List<Level> l_Levels = m_LevelController.LevelID.Select(p_LevelID => new Level(p_LevelID)).ToList();

            Console.WriteLine($"All {l_Levels.Count} Levels loaded.");

            foreach (var l_Level in l_Levels.Select((p_Value, p_Index) => new { value = p_Value, index = p_Index }))
            {
                int l_NumberOfPass = 0;
                int l_NumberOfMapDiffInLevel = 0;
                int l_Plastic = 0;
                int l_Silver = 0;
                int l_Gold = 0;
                int l_Diamond = 0;
                string l_TrophyString = "";
                foreach (var l_Song in l_Level.value.m_Level.songs)
                {
                    if (l_Song.difficulties != null)
                    {
                        l_NumberOfMapDiffInLevel += l_Song.difficulties.Count;
                    }

                    if (m_PlayerPass?.SongList != null)
                    {
                        l_NumberOfPass += (from l_PlayerPassedSong in m_PlayerPass.SongList
                            where string.Equals(l_Song.hash, l_PlayerPassedSong.hash, StringComparison.CurrentCultureIgnoreCase)
                            where l_Song.difficulties != null && l_PlayerPassedSong.DiffList != null
                            from l_SongDifficulty in l_Song.difficulties
                            where l_PlayerPassedSong.DiffList.Any(p_PlayerPassedDiff => l_SongDifficulty.characteristic == p_PlayerPassedDiff.Difficulty.characteristic && l_SongDifficulty.name == p_PlayerPassedDiff.Difficulty.name)
                            select l_PlayerPassedSong).Count();
                    }
                }

                // ReSharper disable once IntDivisionByZero
                if (l_NumberOfMapDiffInLevel != 0)
                {
                    switch (l_NumberOfPass * 100 / l_NumberOfMapDiffInLevel)
                    {
                        case 0:
                        {
                            l_TrophyString = "";
                            break;
                        }
                        case <= 39:
                        {
                            l_Plastic = 1;
                            l_TrophyString = "<:plastic:874215132874571787>";
                            break;
                        }
                        case <= 69:
                        {
                            l_Silver = 1;
                            l_TrophyString = "<:silver:874215133197500446>";
                            break;
                        }
                        case <= 99:
                        {
                            l_Gold = 1;
                            l_TrophyString = "<:gold:874215133147197460>";
                            break;
                        }

                        case 100:
                        {
                            l_Diamond = 1;
                            l_TrophyString = "<:diamond:874215133289795584>";
                            break;
                        }
                    }
                }

                l_PlayerPassPerLevelFormat.Levels.Add(new InPassPerLevelFormat
                    { LevelID = l_Level.value.m_LevelID, NumberOfPass = l_NumberOfPass, NumberOfMapDiffInLevel = l_NumberOfMapDiffInLevel, Trophy = new Trophy { Plastic = l_Plastic, Silver = l_Silver, Gold = l_Gold, Diamond = l_Diamond }, TrophyString = l_TrophyString });
            }

            return l_PlayerPassPerLevelFormat.Levels == null ? null : l_PlayerPassPerLevelFormat;
        }
    }
}