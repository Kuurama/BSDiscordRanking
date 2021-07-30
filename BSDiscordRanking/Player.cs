using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;
// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace BSDiscordRanking
{
    public class Player
    {
        private string m_PlayerID;
        private string m_Path;
        private ApiPlayerFull m_PlayerFull;
        private ApiScores m_PlayerScore;
        private PlayerPassFormat m_PlayerPass;
        private LevelControllerFormat m_LevelController;
        private int m_NumberOfTry = 0;
        private bool m_HavePlayerInfo = false;
        private const int ERROR_LIMIT = 3;
        private int m_ErrorNumber = 0;

        public Player(string p_PlayerID)
        {
            m_PlayerID = p_PlayerID;
            m_Path = @$".\Players\{m_PlayerID}\";
            Console.WriteLine($"Selected Path is {m_Path}");

            /////////////////////////////// Needed Setup Method ///////////////////////////////////

            GetInfos(); ///< Get Full Player Info.

            CreateDirectory(); ///< Make the score file's directory.

            OpenSavedScore(); ///< Make the player's instance retrieve all the data from the json file.

            ///////////////////////////////////////////////////////////////////////////////////////
        }

        private void GetInfos()
        {
            /// This Method Get the Player's Info from the api, then Deserialize it to m_PlayerFull for later usage.
            /// It handle most of the exceptions possible
            ///
            /// If it fail to load the Player's Info, m_NumberOfTry = 6 => the program will stop trying. (limit is 5)
            /// and it mean the Score Saber ID is wrong.

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
                                Console.WriteLine("RateLimited, Trying again in 45sec");
                                Thread.Sleep(45000);
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
                Console.WriteLine(
                    "Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
            }
        }

        private void CreateDirectory()
        {
            /// This Method Create the Directory needed to save and load the Player's score cache file from it's Path parameter.
            /// m_ErrorNumber will be increased at every error and lock the method if it exceed m_ErrorLimit

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
                Console.WriteLine(
                    "Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
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
                    using (StreamReader l_SR = new StreamReader(m_Path + @"\score.json"))
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
                Console.WriteLine(
                    "Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
            }
        }

        public void FetchScores(SocketCommandContext p_Context = null)
        {
            /// If First Launch : It get ALL the player's score from the api then cache it to a score file. (mean there isn't any cache file yet)
            /// This Method Get the Player's Scores from the api, then call ReWriteScore() to Serialize them into a cache's file.
            /// This method is smart and know the api rate limit work and all the exception it can have to avoid them.
            /// Technically you can run this method without even running any other method in the constructor as it's
            /// smart enough to try to run all the needed method by itself.
            ///
            /// When this method download ALL the player's Score and check there is already some saved scores to stop the research,
            /// it also delete outdated score from old cached passes.
            /// it know when it should stop => Score already saved, => no more Pages, => internet issue, => api rate limit (retrying after 45sec).
            ///
            /// You need to run this method to Update the Player's Scores's cache (if there is any).
            ///
            /// If there is an issue with the scores's downloading and the cache miss some scores,
            /// run ClearScore() and try again downloading all Scores with that method (will act as "First Launch").

            p_Context.Channel.SendMessageAsync("> <:clock1:868188979411959808> Fetching player scores, this step can take a while!");

            if (m_ErrorNumber < ERROR_LIMIT)
            {
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
                                            if (m_PlayerScore.scores.RemoveAll(x =>
                                                    x.leaderboardId == l_NewScore.leaderboardId &&
                                                    x.score != l_NewScore.score) > 0 ||
                                                !(m_PlayerScore.scores.Any(x =>
                                                    x.leaderboardId == l_NewScore.leaderboardId)))
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
                                            Console.WriteLine($"No more Page to download, Downloaded {l_Page} Page(s)");
                                            break;
                                        }

                                        if (l_Response.StatusCode == HttpStatusCode.TooManyRequests)
                                        {
                                            p_Context.Channel.SendMessageAsync($"> <:clock1:868188979411959808> The bot got rate-limited, it will continue after 45s. (Page {l_Page} out of {m_PlayerFull.scoreStats.totalPlayCount / 8})");
                                            Thread.Sleep(45000);
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
            }
            else
            {
                Console.WriteLine(
                    "Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
            }
        }

        private void ReWriteScore()
        {
            /// This Method Serialise the data from m_PlayerScore and cache it to a file depending on the path parameter
            /// Be Aware that it will replace the current cache file (if there is any), it shouldn't be an issue
            /// as you needed to Deserialised that cache (or set an empty format) to m_PlayerScore by using OpenSavedScore();
            ///
            /// m_ErrorNumber will be increased at every error and lock the method if it exceed m_ErrorLimit

            if (m_ErrorNumber < ERROR_LIMIT)
            {
                try
                {
                    if (m_PlayerScore != null)
                    {
                        File.WriteAllText(m_Path + @"\score.json", JsonSerializer.Serialize(m_PlayerScore));
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
                Console.WriteLine(
                    "Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
            }
        }

        private void ClearScore()
        {
            /// <summary>
            /// This Method Delete the player's scores's cache file.
            /// </summary>

            try
            {
                File.Delete(m_Path + @"\score.json");
            }
            catch (Exception l_Exception)
            {
                Console.WriteLine($"Failed to delete File : {l_Exception.Message}");
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


        public async Task<int> FetchPass(SocketCommandContext p_context = null)
        {
            /// This Method Fetch the passes the Player did by checking all Levels and player's pass and add the matching ones.
            int l_passes = 0;
            string l_message = null;
            LoadLevelControllerCache();
            PlayerPassFormat l_OldPlayerPass = ReturnPass();
            m_PlayerPass = new PlayerPassFormat()
            {
                songs = new List<SongFormat>()
            };
            List<Level> l_Levels = new List<Level>();
            foreach (var l_LevelID in m_LevelController.LevelID)
            {
                l_Levels.Add(new Level(l_LevelID));
            }


            for (int i = 0; i < l_Levels.Count; i++)
            {
                foreach (var l_Song in l_Levels[i].m_Level.songs)
                {
                    foreach (var l_Score in m_PlayerScore.scores)
                    {
                        if (!l_Score.mods.Contains("NF") && !l_Score.mods.Contains("NA") && !l_Score.mods.Contains("SS"))
                        {
                            if (l_Song.hash.ToUpper() == l_Score.songHash.ToUpper())
                            {
                                bool l_MapStored = false;
                                if (l_Song.difficulties is not null)
                                {
                                    foreach (var l_Difficulty in l_Song.difficulties)
                                    {
                                        if (l_Score.difficultyRaw == $"_{l_Difficulty.name}_Solo{l_Difficulty.characteristic}")
                                        {
                                            foreach (var l_CachedPassedSong in m_PlayerPass.songs)
                                            {
                                                bool l_DiffExist = false;
                                                bool l_OldDiffExist = false;
                                                if (l_CachedPassedSong.difficulties != null && String.Equals(l_CachedPassedSong.hash, l_Song.hash, StringComparison.CurrentCultureIgnoreCase))
                                                {
                                                    l_MapStored = true;
                                                    foreach (var l_CachedDifficulty in l_CachedPassedSong.difficulties)
                                                    {
                                                        foreach (var l_OldPassedSong in l_OldPlayerPass.songs)
                                                        {
                                                            if (l_CachedPassedSong.hash == l_OldPassedSong.hash)
                                                            {
                                                                foreach (var l_OldPassedDifficulty in l_OldPassedSong.difficulties)
                                                                {
                                                                    if (l_OldPassedDifficulty.characteristic == l_Difficulty.characteristic && l_OldPassedDifficulty.name == l_Difficulty.name)
                                                                    {
                                                                        l_OldDiffExist = true;
                                                                        break;
                                                                    }
                                                                }
                                                            }
                                                        }

                                                        if (l_CachedDifficulty.characteristic == l_Difficulty.characteristic && l_CachedDifficulty.name == l_Difficulty.name)
                                                        {
                                                            l_DiffExist = true;
                                                            break;
                                                        }
                                                    }

                                                    if (!l_DiffExist)
                                                    {
                                                        l_CachedPassedSong.difficulties.Add(l_Difficulty);
                                                        if (!l_OldDiffExist)
                                                        {
                                                            /// Display new pass (new diff passed while there was already a passed diff) 1/2
                                                            l_message += $"<:clap:868195856560582707> Passed {l_Difficulty.name} {l_Difficulty.characteristic} - {l_Score.songName}\n";
                                                            l_passes++;
                                                        }
                                                    }
                                                }
                                            }

                                            if (!l_MapStored)
                                            {
                                                bool l_WasStored = false;
                                                SongFormat l_PassedSong = new SongFormat();
                                                l_PassedSong.difficulties = new List<InSongFormat>();
                                                l_PassedSong.hash = l_Song.hash.ToUpper();
                                                l_PassedSong.difficulties.Add(l_Difficulty);
                                                m_PlayerPass.songs.Add(l_PassedSong);

                                                foreach (var l_OldPassedSong in l_OldPlayerPass.songs)
                                                {
                                                    if (string.Equals(l_OldPassedSong.hash, l_Song.hash, StringComparison.CurrentCultureIgnoreCase))
                                                    {
                                                        l_WasStored = true;
                                                        break;
                                                    }
                                                }

                                                if (!l_WasStored)
                                                {
                                                    l_message += $"<:clap:868195856560582707> Passed {l_Difficulty.name} {l_Difficulty.characteristic} - {l_Score.songName}\n"; /// Display new pass 2/2
                                                    l_passes++;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            ReWritePass();
            if (l_passes >= 1)
                await p_context.Channel.SendMessageAsync(">>> " + l_message);
            return l_passes;
        }

        private PlayerPassFormat ReturnPass()
        {
            /// This method return the Serialised version of the current saved Player's pass, ruturn an empty on if none.
            PlayerPassFormat l_PlayerPass = new PlayerPassFormat();
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
                    using (StreamReader l_SR = new StreamReader($@"{m_Path}\pass.json"))
                    {
                        l_PlayerPass = JsonSerializer.Deserialize<PlayerPassFormat>(l_SR.ReadToEnd());
                        if (l_PlayerPass == null) /// json contain "null"
                        {
                            l_PlayerPass = new PlayerPassFormat()
                            {
                                songs = new List<SongFormat>()
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
                        songs = new List<SongFormat>()
                    };
                    Console.WriteLine($@"{m_Path}pass.json Created (Empty Format)");
                }
            }

            return l_PlayerPass;
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
                    using (StreamReader l_SR = new StreamReader($@"{LevelController.GetPath()}\{LevelController.GetFileName()}.json "))
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
            try
            {
                if (m_PlayerPass != null)
                {
                    File.WriteAllText($@"{m_Path}\pass.json", JsonSerializer.Serialize(m_PlayerPass));
                    try
                    {
                        Console.WriteLine($"Pass's file of {m_PlayerFull.playerInfo.playerName} Updated, {m_PlayerPass.songs.Count} song(s) are stored (song number <= number of scores : multiple diff)");
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