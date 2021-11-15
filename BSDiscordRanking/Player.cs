﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Discord.Modules.UserModule;
using BSDiscordRanking.Formats.API;
using BSDiscordRanking.Formats.Controller;
using BSDiscordRanking.Formats.Player;
using Discord;
using Discord.Commands;

// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace BSDiscordRanking
{
    public class Player
    {
        private const int ERROR_LIMIT = 3;
        private int m_ErrorNumber = 0;
        private bool m_HavePlayerInfo = false;
        private LevelControllerFormat m_LevelController;
        private int m_NumberOfTry = 0;
        private string m_Path;
        public ApiPlayerFull m_PlayerFull;
        private string m_PlayerID;
        private PlayerPassFormat m_PlayerPass;
        private ApiScores m_PlayerScore;
        public PlayerStatsFormat m_PlayerStats;

        public Player(string p_PlayerID)
        {
            m_PlayerID = p_PlayerID;
            if (m_PlayerID != null)
            {
                m_Path = @$"./Players/{m_PlayerID}/";
                Console.WriteLine($"Selected Path is {m_Path}");
            }
            else
            {
                m_Path = null;
            }

            /////////////////////////////// Needed Setup Method ///////////////////////////////////

            GetInfos(); ///< Get Full Player Info.

            CreateDirectory(); ///< Make the score file's directory.

            OpenSavedScore(); ///< Make the player's instance retrieve all the data from the json file.

            ///////////////////////////////////////////////////////////////////////////////////////
        }

        public int GetPlayerLevel()
        {
            if (m_PlayerID != null)
            {
                int l_PlayerLevel = 0;
                PlayerStatsFormat l_PlayerStats = GetStats();
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
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        public string GetPlayerID()
        {
            return m_PlayerID;
        }

        private void GetInfos()
        {
            /// This Method Get the Player's Info from the api, then Deserialize it to m_PlayerFull for later usage.
            /// It handle most of the exceptions possible
            ///
            /// If it fail to load the Player's Info, m_NumberOfTry = 6 => the program will stop trying. (limit is 5)
            /// and it mean the Score Saber ID is wrong.
            if (m_PlayerID != null)
            {
                if (m_ErrorNumber < ERROR_LIMIT)
                {
                    using (WebClient l_WebClient = new WebClient())
                    {
                        try
                        {
                            m_PlayerFull = JsonSerializer.Deserialize<ApiPlayerFull>(
                                l_WebClient.DownloadString(@$"https://new.scoresaber.com/api/player/{m_PlayerID}/full"));
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
                                    m_NumberOfTry = 6;
                                    m_ErrorNumber = 6;
                                }
                            }
                            else ///< Request Error => Internet or ScoreSaber API Down.
                            {
                                if (!CheckScoreSaberAPI_Response("Player Full")) ///< Checking if ScoreSaber Api Return.
                                {
                                    if (m_NumberOfTry <= 5)
                                    {
                                        Console.WriteLine(
                                            $"Retrying to get PLayer's Info in 30 sec : {m_NumberOfTry} out of 5 try");
                                        m_NumberOfTry++;
                                        Thread.Sleep(30000);
                                        GetInfos();
                                    }
                                    else
                                    {
                                        Console.WriteLine("No try left, Canceling GetPLayerInfo()");
                                    }
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

        private void CreateDirectory()
        {
            /// This Method Create the Directory needed to save and load the Player's score cache file from it's Path parameter.
            /// m_ErrorNumber will be increased at every error and lock the method if it exceed m_ErrorLimit
            if (m_PlayerID != null)
            {
                if (m_ErrorNumber < ERROR_LIMIT)
                {
                    if (!Directory.Exists(m_Path))
                    {
                        try
                        {
                            Directory.CreateDirectory(m_Path);
                            Console.WriteLine($"Directory {m_Path} Created");
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
        }

        private void OpenSavedScore()
        {
            /// If First Launch* : Assign a Scores Sample to m_PlayerScore. (mean there isn't any cache file yet)
            /// This Method Load a cache file from its path. (The player's scores)
            /// then Deserialise it to m_PlayerScore.
            /// * => If the Scores's file failed to load (or don't exist), it will still load an empty scores format to m_PlayerScore.
            ///
            /// m_ErrorNumber will be increased at every error and lock the method if it exceed m_ErrorLimit

            if (m_PlayerID != null)
            {
                if (m_ErrorNumber < ERROR_LIMIT)
                {
                    if (!Directory.Exists(m_Path))
                    {
                        Console.WriteLine("Seems like you forgot to Create the Player Directory, attempting creation..");
                        CreateDirectory();
                        Console.WriteLine("Continuing Loading Player's Score(s)");
                    }

                    try
                    {
                        using (StreamReader l_SR = new StreamReader(m_Path + "score.json"))
                        {
                            m_PlayerScore = JsonSerializer.Deserialize<ApiScores>(l_SR.ReadToEnd());
                            Console.WriteLine($"Player {m_PlayerID} Successfully Loaded");
                        }
                    }
                    catch (Exception)
                    {
                        m_PlayerScore = new ApiScores()
                        {
                            scores = new List<ApiScore>()
                        };
                        Console.WriteLine($"Player {m_PlayerID} Created (Empty Format) => (Nothing to load/Wrong Format)");
                    }
                }
                else
                {
                    Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                    Console.WriteLine("Please Contact an Administrator.");
                }
            }
        }

        public bool FetchScores(SocketCommandContext p_Context = null)
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

            if (m_PlayerID != null)
            {
                if (m_ErrorNumber < ERROR_LIMIT)
                {
                    if (m_PlayerStats.IsFirstScan > 0)
                    {
                        if (p_Context != null)
                            p_Context.Channel.SendMessageAsync("> <:clock1:868188979411959808> First Time Fetching player scores (downloading all of your passes once), this step will take a while! The bot will be unresponsive during the process.");
                    }

                    else
                    {
                        if (p_Context != null)
                            p_Context.Channel.SendMessageAsync("> <:clock1:868188979411959808> Fetching player scores, this step can take a while! The bot will be unresponsive during the process.");
                    }

                    if (m_HavePlayerInfo) /// Check if Player have Player's Info
                    {
                        if (m_PlayerScore != null)
                        {
                            ApiScores l_Result; ///< Result From Request but Serialized.
                            string l_URL;
                            int l_Page = 1;
                            int l_NumberOfAddedScore = 0;
                            bool l_Skip = false;
                            /// Avoid doing useless attempt, Check player's number of score (8 score per request).
                            while ((m_PlayerFull.scoreStats.totalPlayCount / 8) + 2 >= l_Page && !l_Skip)
                            {
                                l_URL = @$"https://new.scoresaber.com/api/player/{m_PlayerFull.playerInfo.playerId}/scores/recent/{l_Page.ToString()}";
                                using (WebClient l_WebClient = new WebClient())
                                {
                                    try
                                    {
                                        Console.WriteLine(l_URL);
                                        l_Result = JsonSerializer.Deserialize<ApiScores>(l_WebClient.DownloadString(l_URL));
                                        l_Page++;

                                        int l_Index = 0;

                                        for (int i = 0; i < m_PlayerScore.scores.Count; i++)
                                        {
                                            if (l_Skip)
                                                break; ///< break the for and l_Skip will cause the end of the While Loop.
                                            if (l_Result != null)
                                                for (l_Index = 0; l_Index < l_Result.scores.Count; l_Index++)
                                                {
                                                    if (l_Result.scores[l_Index].timeSet == m_PlayerScore.scores[i].timeSet)
                                                    {
                                                        l_Skip = true; ///< One score already exist (will end the While Loop)
                                                        break;
                                                    }
                                                }
                                        }

                                        if (l_Result != null)
                                            foreach (var l_NewScore in l_Result.scores) /// Remove old score and add new score.
                                            {
                                                if (m_PlayerScore.scores.RemoveAll(p_X => p_X.leaderboardId == l_NewScore.leaderboardId && p_X.score != l_NewScore.score) > 0 || !(m_PlayerScore.scores.Any(p_X => p_X.leaderboardId == l_NewScore.leaderboardId)))
                                                {
                                                    m_PlayerScore.scores.Add(l_NewScore);
                                                    l_NumberOfAddedScore++;
                                                }
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
                                                p_Context.Channel.SendMessageAsync($"> <:clock1:868188979411959808> The bot got rate-limited, it will continue after 50s. (Page {l_Page} out of {m_PlayerFull.scoreStats.totalPlayCount / 8})");
                                                Thread.Sleep(50000);
                                            }
                                        }
                                        else
                                        {
                                            if (!CheckScoreSaberAPI_Response("Player Score"))
                                            {
                                                Console.WriteLine($"But {l_NumberOfAddedScore} new Score(s) will be Added");
                                                if (m_NumberOfTry > 5)
                                                {
                                                    Console.WriteLine("OK Internet is Dead, Stopped Fetching Score.");
                                                    break; /// End the While Loop.
                                                }

                                                Console.WriteLine(
                                                    $"Retrying to Fetch Player's Scores in 30 sec : {m_NumberOfTry} out of 5 try");
                                                m_NumberOfTry++;
                                                Thread.Sleep(30000);
                                            }
                                            else
                                            {
                                                m_NumberOfTry = 6;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            if (l_Skip)
                                Console.WriteLine($"Fetched {l_Page - 1} pages");
                            else
                                Console.WriteLine($"Fetched {l_Page} pages");
                            Console.WriteLine($"{l_NumberOfAddedScore} new Score(s) Added");
                            ReWriteScore(); /// Caching the Score from the player instance.
                        }
                        else
                        {
                            Console.WriteLine("Seems like you forgot to load the Player's Scores, Attempting to load..");
                            OpenSavedScore();
                            FetchScores();
                        }
                    }
                    else /// If Player don't have player's info => Trying to get Player's Info
                    {
                        if (m_NumberOfTry <= 5)
                        {
                            GetInfos();
                            FetchScores();
                        }
                        else
                        {
                            Console.WriteLine("Stopped Getting Player Info, canceling.");
                            m_ErrorNumber = 6;
                        }
                    }

                    if (m_PlayerStats.IsFirstScan > 0)
                        return true;
                    else
                        return false;
                }
                else
                {
                    Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                    Console.WriteLine("Please Contact an Administrator.");
                }
            }

            return false;
        }

        private void ReWriteScore()
        {
            /// This Method Serialise the data from m_PlayerScore and cache it to a file depending on the path parameter
            /// Be Aware that it will replace the current cache file (if there is any), it shouldn't be an issue
            /// as you needed to Deserialised that cache (or set an empty format) to m_PlayerScore by using OpenSavedScore();
            ///
            /// m_ErrorNumber will be increased at every error and lock the method if it exceed m_ErrorLimit
            if (m_PlayerID != null)
            {
                if (m_ErrorNumber < ERROR_LIMIT)
                {
                    try
                    {
                        if (m_PlayerScore != null)
                        {
                            File.WriteAllText(m_Path + "score.json", JsonSerializer.Serialize(m_PlayerScore));
                            try
                            {
                                Console.WriteLine(
                                    $"{m_PlayerFull.playerInfo.playerName} Updated, ({m_PlayerScore.scores.Count} Score stored)");
                            }
                            catch (Exception)
                            {
                                Console.WriteLine(
                                    "Seems Like you forgot to Get Player Info, Attempting to get player's info");
                                GetInfos();
                                ReWriteScore();
                            }
                        }
                        else
                        {
                            Console.WriteLine("Seems like you forgot to load the Player's Scores, Attempting to load..");
                            OpenSavedScore();
                            ReWriteScore();
                        }
                    }
                    catch
                    {
                        Console.WriteLine(
                            "An error occured While attempting to Write the Player's Cache. (missing directory?)");
                        Console.WriteLine("Attempting to create the directory..");
                        m_ErrorNumber++;
                        CreateDirectory(); /// m_ErrorNumber will increase again if the directory creation fail.
                        Thread.Sleep(200);
                        ReWriteScore();
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

        private bool CheckScoreSaberAPI_Response(string p_ApiRequestType) /// Return True if ScoreSaber API is Up.
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
                    Console.WriteLine(
                        l_WebClient.DownloadString("https://new.scoresaber.com/api/").Contains("hey")
                            ? $"Internet OK, Score Saber {p_ApiRequestType} API must have changed, please contact an administrator"
                            : "Internet OK, Score Saber API response is Weird, something must have changed, please contact an administrator");
                    m_NumberOfTry = 6;
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

        private void ResetRetryNumber() ///< Concidering the instance is pretty much created for each command, this is useless.
        {
            /// <summary>
            /// This Method Reset m_RetryNumber to 0, because if that number exceed m_RetryLimit, all the "dangerous" method will be locked.
            /// </summary>

            m_ErrorNumber = 0;
            Console.WriteLine("RetryNumber set to 0");
        }


        public async Task<int> FetchPass(SocketCommandContext p_Context = null)
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
                int l_OldPlayerFirstScanStatus = m_PlayerStats.IsFirstScan;
                bool l_DiffGotLeaderboardID = false;
                bool l_DiffGotNewAutoWeight = false;
                int l_Plastic = 0, l_Silver = 0, l_Gold = 0, l_Diamond = 0;
                Trophy l_Trophy = new Trophy();
                List<string> l_Messages = new List<string> { "" };
                List<int> l_ExistingLevelID = new List<int>();
                int l_BiggerLevelID = Int32.MinValue;
                bool l_AboveLVLFourteenPass = false; /// Funny
                var l_LevelController = new LevelController(); /// Constructor makes levelcontroller FetchLevel()
                int l_OldPlayerLevel = GetPlayerLevel();
                LoadLevelControllerCache();
                PlayerPassFormat l_OldPlayerPass = ReturnPass();
                m_PlayerPass = new PlayerPassFormat
                {
                    SongList = new List<InPlayerSong>()
                };
                ResetTrophy();
                ResetLevels();
                List<Level> l_Levels = new List<Level>();
                foreach (var l_LevelID in m_LevelController.LevelID)
                {
                    l_Levels.Add(new Level(l_LevelID)); /// List of the current existing levels
                    if (l_BiggerLevelID < l_LevelID) l_BiggerLevelID = l_LevelID;
                }

                try
                {
                    foreach (var l_Level in l_Levels.Select((p_Value, p_Index) => new { value = p_Value, index = p_Index }))
                    {
                        l_Weighting = 0f;
                        // l_LevelExist = false;
                        // foreach (var l_ID in l_ExistingLevelID)
                        // {
                        //     if (l_ID - 1 == l_Y)
                        //     {
                        //         l_LevelExist = true;
                        //         break;
                        //     }
                        // }
                        //
                        // if (!l_LevelExist)
                        // {
                        //     SetGrindInfo(l_Y+1, false, -1, null, -1, -1, -1); /// Allow Level unrank on level removing
                        //     continue;
                        // }

                        l_Weighting = l_Level.value.m_Level.customData.weighting;
                        foreach (var l_Song in l_Level.value.m_Level.songs)
                        {
                            foreach (var l_Score in m_PlayerScore.scores)
                            {
                                if (!l_Score.mods.Contains("NF") && !l_Score.mods.Contains("NA") && !l_Score.mods.Contains("SS") && !l_Score.mods.Contains("NB"))
                                {
                                    if (String.Equals(l_Song.hash, l_Score.songHash, StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        bool l_MapStored = false;
                                        if (l_Song.difficulties is not null)
                                        {
                                            foreach (var l_Difficulty in l_Song.difficulties)
                                            {
                                                if (l_Score.difficultyRaw == $"_{l_Difficulty.name}_Solo{l_Difficulty.characteristic}")
                                                {
                                                    bool l_DiffExist = false;
                                                    bool l_OldDiffExist = false;
                                                    bool l_PassWeightAlreadySet = false;
                                                    bool l_AccWeightAlreadySet = false;
                                                    foreach (var l_CachedPassedSong in m_PlayerPass.SongList)
                                                    {
                                                        if (l_CachedPassedSong.DiffList != null && string.Equals(l_CachedPassedSong.hash, l_Song.hash, StringComparison.CurrentCultureIgnoreCase))
                                                        {
                                                            l_MapStored = true;
                                                            foreach (var l_CachedDifficulty in l_CachedPassedSong.DiffList)
                                                            {
                                                                if (l_OldPlayerPass.SongList != null)
                                                                {
                                                                    foreach (var l_OldPassedSong in l_OldPlayerPass.SongList)
                                                                    {
                                                                        if (l_CachedPassedSong.hash == l_OldPassedSong.hash)
                                                                        {
                                                                            foreach (var l_OldPassedDifficulty in l_OldPassedSong.DiffList)
                                                                            {
                                                                                if (l_OldPassedDifficulty.Difficulty.characteristic == l_Difficulty.characteristic && l_OldPassedDifficulty.Difficulty.name == l_Difficulty.name && l_Score.unmodififiedScore >= l_Difficulty.customData.minScoreRequirement)
                                                                                {
                                                                                    l_OldDiffExist = true;
                                                                                    break;
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }

                                                                if (l_CachedDifficulty.Difficulty.characteristic == l_Difficulty.characteristic && l_CachedDifficulty.Difficulty.name == l_Difficulty.name && l_Score.unmodififiedScore >= l_Difficulty.customData.minScoreRequirement)
                                                                {
                                                                    l_DiffExist = true;
                                                                    break;
                                                                }
                                                            }

                                                            if (!l_DiffExist)
                                                            {
                                                                l_Difficulty.customData.leaderboardID = l_Score.leaderboardId;
                                                                l_CachedPassedSong.DiffList.Add(new InPlayerPassFormat()
                                                                {
                                                                    Difficulty = l_Difficulty,
                                                                    Score = l_Score.score,
                                                                    Rank = l_Score.rank
                                                                });
                                                                if (!l_OldDiffExist)
                                                                {
                                                                    l_DifficultyShown = l_Difficulty.characteristic != "Standard" ? $"{l_Difficulty.characteristic} " : "";
                                                                    if (m_PlayerStats.IsFirstScan <= 0)
                                                                    {
                                                                        if (l_Messages[l_MessagesIndex].Length + $":white_check_mark: Passed ***`{l_Difficulty.name} {l_DifficultyShown}- {l_Score.songName.Replace("`", @"\`").Replace("*", @"\*")}`*** in Level **{l_Level.value.m_LevelID}** (+{l_Weighting * 0.375f} {l_Config.PassPointsName}):\n> {l_Difficulty.customData.customPassText}\n\n"
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
                                                                            l_Messages[l_MessagesIndex] += $"\n:white_check_mark: Passed ***`{l_Difficulty.name} {l_DifficultyShown}- {l_Score.songName.Replace("`", @"\`").Replace("*", @"\*")}`*** in Level **{l_Level.value.m_LevelID}** (+{l_Weighting * 0.375f} {l_Config.PassPointsName}):\n> {l_Difficulty.customData.customPassText.Replace("_", " ")}\n\n";
                                                                        else
                                                                            l_Messages[l_MessagesIndex] += $":white_check_mark: Passed ***`{l_Difficulty.name} {l_DifficultyShown}- {l_Score.songName.Replace("`", @"\`").Replace("*", @"\*")}`*** in Level **{l_Level.value.m_LevelID}** (+{l_Weighting * 0.375f} {l_Config.PassPointsName})\n";
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
                                                    }

                                                    if (!l_MapStored && l_Score.unmodififiedScore >= l_Difficulty.customData.minScoreRequirement)
                                                    {
                                                        bool l_WasStored = false;
                                                        InPlayerSong l_PlayerPassFormat = new InPlayerSong()
                                                        {
                                                            DiffList = new List<InPlayerPassFormat>(),
                                                            hash = l_Song.hash.ToUpper(),
                                                            key = l_Song.key,
                                                            name = l_Song.name
                                                        };
                                                        l_Difficulty.customData.leaderboardID = l_Score.leaderboardId;
                                                        l_PlayerPassFormat.DiffList.Add(new InPlayerPassFormat()
                                                        {
                                                            Difficulty = l_Difficulty,
                                                            Score = l_Score.score,
                                                            Rank = l_Score.rank
                                                        });
                                                        l_PlayerPassFormat.name = l_Song.name;
                                                        m_PlayerPass.SongList.Add(l_PlayerPassFormat);

                                                        if (l_OldPlayerPass.SongList != null)
                                                        {
                                                            foreach (var l_OldPassedSong in l_OldPlayerPass.SongList)
                                                            {
                                                                if (string.Equals(l_OldPassedSong.hash, l_Song.hash, StringComparison.CurrentCultureIgnoreCase))
                                                                {
                                                                    l_WasStored = true;
                                                                    break;
                                                                }
                                                            }
                                                        }

                                                        if (!l_WasStored)
                                                        {
                                                            l_DifficultyShown = l_Difficulty.characteristic != "Standard" ? $"{l_Difficulty.characteristic} " : "";
                                                            if (m_PlayerStats.IsFirstScan <= 0)
                                                            {
                                                                if (l_Messages[l_MessagesIndex].Length >
                                                                    1900 -
                                                                    $":white_check_mark: Passed ***`{l_Difficulty.name} {l_DifficultyShown}- {l_Score.songName.Replace("`", @"\`").Replace("*", @"\*")}`*** in Level **{l_Level.value.m_LevelID}** (+{l_Weighting * 0.375f} {l_Config.PassPointsName}):\n> {l_Difficulty.customData.customPassText}\n\n"
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
                                                                        $"\n:white_check_mark: Passed ***`{l_Difficulty.name} {l_DifficultyShown}- {l_Score.songName.Replace("`", @"\`").Replace("*", @"\*")}`*** in Level **{l_Level.value.m_LevelID}** (+{l_Weighting * 0.375f} {l_Config.PassPointsName}):\n> {l_Difficulty.customData.customPassText.Replace("_", " ")}\n\n";
                                                                else
                                                                    l_Messages[l_MessagesIndex] +=
                                                                        $":white_check_mark: Passed ***`{l_Difficulty.name} {l_DifficultyShown}- {l_Score.songName.Replace("`", @"\`").Replace("*", @"\*")}`*** in Level **{l_Level.value.m_LevelID}** (+{l_Weighting * 0.375f} {l_Config.PassPointsName})\n";
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

                                                    if (l_Difficulty.customData.leaderboardID != l_Score.leaderboardId)
                                                    {
                                                        l_Difficulty.customData.leaderboardID = l_Score.leaderboardId;
                                                        l_DiffGotLeaderboardID = true;
                                                    }

                                                    MapLeaderboardController l_MapLeaderboardController = new MapLeaderboardController(l_Score.leaderboardId, l_Song.hash, l_Song.key, l_Song.name, l_Difficulty.customData.maxScore);
                                                    bool l_NeedNewAutoWeight = l_MapLeaderboardController.ManagePlayerAndAutoWeightCheck(m_PlayerFull.playerInfo.playerName, m_PlayerID, l_Score.score);

                                                    if (l_NeedNewAutoWeight)
                                                    {
                                                        l_Difficulty.customData.AutoWeight = l_Level.value.RecalculateAutoWeight(l_Score.leaderboardId);
                                                        Console.WriteLine($"New AutoWeight set on {l_Difficulty.name} {l_DifficultyShown} - {l_Score.songName.Replace("`", @"\`").Replace("*", @"\*")}");
                                                        l_DiffGotNewAutoWeight = true;
                                                    }

                                                    if (l_Difficulty.customData.forceManualWeight)
                                                    {
                                                        if (!l_Config.OnlyAutoWeightForAccLeaderboard)
                                                        {
                                                            l_TotalAccPoints += ((float)l_Score.score / l_Difficulty.customData.maxScore) * 100f * 0.375f * l_Difficulty.customData.manualWeight;
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
                                                            l_TotalAccPoints += ((float)l_Score.score / l_Difficulty.customData.maxScore) * 100f * 0.375f * l_Difficulty.customData.AutoWeight;
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
                                                            l_TotalAccPoints += ((float)l_Score.score / l_Difficulty.customData.maxScore) * 100f * 0.375f * l_Level.value.m_Level.customData.weighting;
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


                        foreach (var l_Song in l_Level.value.m_Level.songs)
                        {
                            foreach (var l_Difficulty in l_Song.difficulties)
                            {
                                l_NumberOfDifficulties++;
                            }
                        }


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

                        if (l_PassesPerLevel > 0 && (m_PlayerStats.IsFirstScan > 0))
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
                                l_Messages[l_MessagesIndex] += $":white_check_mark: You passed `{l_PassesPerLevel}/{l_NumberOfDifficulties}` maps in Level **{l_Level.value}** (+{l_Weighting * 0.375f * l_PassesPerLevel} {l_Config.PassPointsName})\n";
                            }
                        }

                        l_Trophy = new Trophy()
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

                        if (m_PlayerStats.Levels is not null) /// If it is maybe you forgot to LoadStats()?
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

                        ReWriteStats();

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
                    m_PlayerStats.IsFirstScan = 0;
                    m_PlayerStats.TotalNumberOfPass = l_TotalAmountOfPass;
                    ReWriteStats();

                    ReWritePass();

                    Color l_Color = UserModule.GetRoleColor(RoleController.ReadRolesDB().Roles, p_Context.Guild.Roles, l_OldPlayerLevel);

                    bool l_IsFirstMessage = true;
                    if (l_Passes >= 1)
                        foreach (var l_Message in l_Messages)
                        {
                            var l_Builder = new EmbedBuilder();
                            if (l_IsFirstMessage) l_Builder.WithTitle(l_OldPlayerFirstScanStatus > 0 ? "You passed maps in the following levels:" : "You passed the following maps:");

                            l_IsFirstMessage = false;
                            l_Builder.WithDescription(l_Message);
                            l_Builder.WithColor(l_Color);
                            var l_Embed = l_Builder.Build();
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

        public PlayerPassFormat ReturnPass()
        {
            /// This method return the Serialised version of the current saved Player's pass, ruturn an empty on if none.
            PlayerPassFormat l_PlayerPass = new PlayerPassFormat();
            if (m_PlayerID != null)
            {
                if (!Directory.Exists(m_Path))
                {
                    Console.WriteLine("Seems like you forgot to Create the Player Directory, attempting creation..");
                    CreateDirectory();
                    Console.WriteLine("Continuing Loading Player's Pass");
                }
                else
                {
                    try
                    {
                        using (StreamReader l_SR = new StreamReader($@"{m_Path}pass.json"))
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
                                Console.WriteLine($"pass.json of {m_PlayerID} loaded");
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
                        Console.WriteLine($@"{m_Path}pass.json Created (Empty Format)");
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

        private void LoadLevelControllerCache()
        {
            if (!Directory.Exists(LevelController.GetPath()))
            {
                Console.WriteLine("Seems like you forgot to Fetch the Levels (LevelController), please Fetch Them before using this command : LevelController's directory is missing");
            }
            else
            {
                try
                {
                    using (StreamReader l_SR = new StreamReader($"{LevelController.GetPath()}{LevelController.GetFileName()}.json"))
                    {
                        m_LevelController = JsonSerializer.Deserialize<LevelControllerFormat>(l_SR.ReadToEnd());
                        if (m_LevelController == null) /// json contain "null"
                            Console.WriteLine("Error LevelControllerCache contain null");
                        else
                            Console.WriteLine($"LevelControllerCache Loaded with {m_LevelController.LevelID.Count} Level saved");
                    }
                }
                catch (Exception) /// file format is wrong / there isn't any file.
                {
                    Console.WriteLine("Seems like you forgot to Fetch the Levels (LevelController), please Fetch Them before using this command, missing file/wrong file format");
                }
            }
        }

        private void ReWritePass()
        {
            if (m_PlayerID != null)
            {
                if (m_ErrorNumber < ERROR_LIMIT)
                {
                    try
                    {
                        if (m_PlayerPass != null)
                        {
                            File.WriteAllText($@"{m_Path}pass.json", JsonSerializer.Serialize(m_PlayerPass));
                            try
                            {
                                Console.WriteLine($"Pass's file of {m_PlayerFull.playerInfo.playerName} Updated, {m_PlayerPass.SongList.Count} song(s) are stored (song number <= number of scores : multiple diff)");
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
                        Console.WriteLine("An error occured While attempting to Write the PlayerPass's Cache. (missing directory?)");
                        Console.WriteLine("Attempting to create the directory..");
                        m_ErrorNumber++;
                        CreateDirectory(); /// m_ErrorNumber will increase again if the directory creation fail.
                        Thread.Sleep(200);
                        ReWritePass();
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
            if (m_PlayerStats.Levels != null)
            {
                foreach (var l_PlayerStatsLevel in m_PlayerStats.Levels)
                {
                    l_PlayerStatsLevel.Passed = false;
                }
            }
        }

        public void ReWriteStats()
        {
            if (m_PlayerID != null)
            {
                if (m_ErrorNumber < ERROR_LIMIT)
                {
                    try
                    {
                        if (m_PlayerStats != null)
                        {
                            File.WriteAllText($@"{m_Path}stats.json", JsonSerializer.Serialize(m_PlayerStats));

                            Console.WriteLine($"Stats's file of {m_PlayerFull.playerInfo.playerName} Updated");
                        }
                        else
                        {
                            Console.WriteLine("Seems like you forgot to fetch the Player's Stats.");
                        }
                    }
                    catch

                    {
                        Console.WriteLine("An error occured While attempting to Write the PlayerStats's Cache. (missing directory?)");
                        Console.WriteLine("Attempting to create the directory..");
                        m_ErrorNumber++;
                        CreateDirectory(); /// m_ErrorNumber will increase again if the directory creation fail.
                        Thread.Sleep(200);
                        ReWriteStats();
                    }
                }
                else
                {
                    Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                    Console.WriteLine("Please Contact an Administrator.");
                }
            }
        }

        public void LoadStats()
        {
            if (m_PlayerID != null)
            {
                CreateDirectory();
                try
                {
                    using StreamReader l_SR = new StreamReader($@"{m_Path}stats.json");
                    m_PlayerStats = JsonSerializer.Deserialize<PlayerStatsFormat>(l_SR.ReadToEnd());
                }
                catch (Exception) /// file format is wrong / there isn't any file.
                {
                    m_PlayerStats = new PlayerStatsFormat
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
                        IsFirstScan = 1
                    };
                    Console.WriteLine($"This player don't have any stats yet");
                }

                if (m_PlayerStats is { Levels: null })
                {
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
            }
            else
            {
                m_PlayerStats = new PlayerStatsFormat
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
                    IsFirstScan = 1
                };
            }
        }

        public PlayerStatsFormat GetStats()
        {
            LoadStats();
            return m_PlayerStats;
        }

        public PlayerPassFormat GetPass()
        {
            if (m_PlayerID != null)
            {
                CreateDirectory();
                try
                {
                    using StreamReader l_SR = new StreamReader($@"{m_Path}pass.json");
                    m_PlayerPass = JsonSerializer.Deserialize<PlayerPassFormat>(l_SR.ReadToEnd());
                    return m_PlayerPass;
                }
                catch (Exception) /// file format is wrong / there isn't any file.
                {
                    m_PlayerPass = new PlayerPassFormat
                    {
                        SongList = new List<InPlayerSong>()
                    };
                    Console.WriteLine("This player don't have any pass yet");
                    return m_PlayerPass;
                }
            }
            else
            {
                return m_PlayerPass = new PlayerPassFormat
                {
                    SongList = new List<InPlayerSong>()
                };
            }
        }

        public PlayerPassPerLevelFormat GetPlayerPassPerLevel()
        {
            PlayerPassPerLevelFormat l_PlayerPassPerLevelFormat = new PlayerPassPerLevelFormat()
            {
                Levels = new List<InPassPerLevelFormat>()
            };
            int l_BiggerLevelID = 0;
            var l_LevelController = new LevelController(); /// Constructor make levelcontroller FetchLevel()
            LoadLevelControllerCache();
            PlayerPassFormat l_OldPlayerPass = ReturnPass();

            List<Level> l_Levels = new List<Level>();
            foreach (var l_LevelID in m_LevelController.LevelID)
            {
                l_Levels.Add(new Level(l_LevelID)); /// List of the current existing levels
                if (l_BiggerLevelID < l_LevelID) l_BiggerLevelID = l_LevelID;
            }


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
                        foreach (var l_Difficulty in l_Song.difficulties)
                        {
                            l_NumberOfMapDiffInLevel++;
                        }
                    }

                    foreach (var l_OldPlayerScore in l_OldPlayerPass.SongList)
                    {
                        if (String.Equals(l_Song.hash, l_OldPlayerScore.hash, StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (l_Song.difficulties != null && l_OldPlayerScore.DiffList != null)
                            {
                                foreach (var l_Difficulty in l_Song.difficulties)
                                {
                                    foreach (var l_OldPlayerScoreDiff in l_OldPlayerScore.DiffList)
                                    {
                                        if (l_Difficulty.characteristic == l_OldPlayerScoreDiff.Difficulty.characteristic && l_Difficulty.name == l_OldPlayerScoreDiff.Difficulty.name)
                                        {
                                            l_NumberOfPass++;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
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

            if (l_PlayerPassPerLevelFormat.Levels == null)
                return null;

            return l_PlayerPassPerLevelFormat;
        }
    }
}