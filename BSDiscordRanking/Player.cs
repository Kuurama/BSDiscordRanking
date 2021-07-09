using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace BSDiscordRanking
{
    public class Player
    {
        private string m_PlayerID;
        private string m_Path;
        private ApiPlayerFull m_PlayerFull;
        private ApiScores m_PlayerScore;
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
                    Console.WriteLine("Directory Created, continuing Loading Levels");
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

        public void FetchScores()
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
                            l_URL =
                                @$"https://new.scoresaber.com/api/player/{m_PlayerFull.playerInfo.playerId}/scores/recent/{l_Page.ToString()}";
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
                                                    l_Skip =
                                                        true; ///< One score already exist (will end the While Loop)
                                                    break;
                                                }
                                            }
                                    }

                                    if (l_Result != null)
                                        foreach (var l_NewScore in
                                            l_Result.scores) /// Remove old score and add new score.
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
                                            Console.WriteLine("No more Page to download");
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
                        File.WriteAllText(m_Path + @"\score.json",
                            JsonSerializer.Serialize(m_PlayerScore));
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

        public void FetchPass()
        {
            List<Level> l_levels = Controllers.LevelController.FetchLevels();
            ApiScores l_scores = JsonSerializer.Deserialize<ApiScores>(new StreamReader($"./Players/{m_PlayerID}/score.json").ReadToEnd());
            List<ApiScore> l_pass = new();
            for (int i = 0; i < l_levels.Count; i++)
            {
                foreach (var l_song in l_levels[i].m_Level.songs)
                {
                    foreach (var l_score in l_scores.scores)
                    {
                        if (!l_score.mods.Contains("NF") || !l_score.mods.Contains("NA") || !l_score.mods.Contains("SS"))
                        {
                            if (l_song.hash == l_score.songHash); ///< TODO: Checking if diff is correct
                            { 
                                l_pass.Add(l_score);
                                Console.WriteLine($"Added {l_score.songName}");
                            }
                            
                        }
                    }
                }
            }
        }
    }
}