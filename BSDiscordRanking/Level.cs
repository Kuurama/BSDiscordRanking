using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Threading;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats;
using Discord.Commands;
using JsonSerializer = System.Text.Json.JsonSerializer;

// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace BSDiscordRanking
{
    public class Level
    {
        public LevelFormat m_Level;
        public BeatSaverFormat m_BeatSaver;
        private int m_LevelID;
        public const string SUFFIX_NAME = "_Level";
        public bool m_MapAdded;
        public bool m_MapDeleted;

        /// Keep the underscore at the beginning to avoid issue with the controller.
        private const string PATH = @".\Levels\";

        private const int ERROR_LIMIT = 3;
        private int m_ErrorNumber = 0;

        public Level(int p_LevelID)
        {
            m_LevelID = p_LevelID;

            /////////////////////////////// Needed Setup Method ///////////////////////////////////

            CreateDirectory(); /// Make the Level file's directory.
            LoadLevel(); /// Load the Playlist Cache / First Start : Assign needed Playlist Sample.
            ///////////////////////////////////////////////////////////////////////////////////////
        }


        private void LoadLevel()
        {
            /// If First Launch* : Assign a Playlist Sample to m_Level. (mean there isn't any cache file yet)
            /// This Method Load a Playlist from its path and Prefix Name
            /// then Deserialise it to m_Level.
            /// * => If the playlist's file failed to load (or don't exist), it will still load an empty format to m_Level.
            ///
            /// This method will be locked if m_ErrorNumber < m_ErrorLimit to avoid any loop error.


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
                    using (StreamReader l_SR = new StreamReader($"{PATH}{m_LevelID}{SUFFIX_NAME}.bplist"))
                    {
                        m_Level = JsonSerializer.Deserialize<LevelFormat>(l_SR.ReadToEnd());
                        if (m_Level == null) /// json contain "null"
                        {
                            m_Level = new LevelFormat()
                            {
                                songs = new List<SongFormat>(),
                                playlistTitle = new string($"Lvl {m_LevelID}"),
                                playlistAuthor = new string("Kuurama&Julien"),
                                playlistDescription = new string("BSCC Playlist"),
                                syncURL = null,
                                image = new string(""),
                                weighting = 0f
                            };
                            Console.WriteLine($"Level {m_LevelID} Created (Empty Format), contained null");
                        }
                        else if (m_Level.songs != null)
                        {
                            Console.WriteLine($"Level {m_LevelID} Loaded");
                            foreach (var l_Songs in m_Level.songs)
                            {
                                l_Songs.hash = l_Songs.hash.ToUpper();
                            }
                        }
                        else
                        {
                            m_Level.songs = new List<SongFormat>();
                        }
                    }
                }
                catch (Exception) /// file format is wrong / there isn't any file.
                {
                    m_Level = new LevelFormat()
                    {
                        songs = new List<SongFormat>(),
                        playlistTitle = new string($"Lvl {m_LevelID}"),
                        playlistAuthor = new string("Kuurama&Julien"),
                        playlistDescription = new string("BSCC Playlist"),
                        syncURL = null,
                        image = new string(""),
                        weighting = 0f
                    };
                    Console.WriteLine($"{m_LevelID}{SUFFIX_NAME} Created (Empty Format)");
                }
            }
            else
            {
                Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
            }
        }

        public void ResetScoreRequirement()
        {
            foreach (var l_Song in m_Level.songs)
            {
                foreach (var l_Difficulty in l_Song.difficulties)
                {
                    l_Difficulty.minScoreRequirement = new int();
                    l_Difficulty.minScoreRequirement = 0;
                }
            }

            ReWritePlaylist();
        }

        private void CreateDirectory()
        {
            /// This Method Create the Directory needed to save and load the playlist's file from it's Path parameter.
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

        private void ReWritePlaylist()
        {
            /// This Method Serialise the data from m_Level and create a playlist file depending on the path parameter
            /// and it's PrefixName parameter (Prefix is usefull to sort playlist in the game).
            /// Be Aware that it will replace the current Playlist file (if there is any), it shouldn't be an issue
            /// if you Deserialised that playlist to m_Level by using OpenSavedLevel();
            ///
            /// m_ErrorNumber will be increased at every error and lock the method if it exceed m_ErrorLimit

            if (m_ErrorNumber < ERROR_LIMIT)
            {
                try
                {
                    if (m_Level != null)
                    {
                        if (m_Level.songs.Count > 0)
                        {
                            File.WriteAllText($"{PATH}{m_LevelID}{SUFFIX_NAME}.bplist", JsonSerializer.Serialize(m_Level));
                            Console.WriteLine($"{m_LevelID}{SUFFIX_NAME} Updated ({m_Level.songs.Count} maps in Playlist)");
                            new LevelController().FetchLevel(); /// If a new level is created => Update the LevelController Cache.
                        }
                        else
                        {
                            DeleteLevel();
                            Console.WriteLine("No songs in Playlist, Playlist Deleted.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Seems like you forgot to load the Level, Attempting to load..");
                        LoadLevel();
                        ReWritePlaylist();
                    }
                }
                catch
                {
                    Console.WriteLine(
                        "An error occured While attempting to Write the Playlist file. (missing directory?)");
                    Console.WriteLine("Attempting to create the directory..");
                    m_ErrorNumber++;
                    CreateDirectory(); /// m_ErrorNumber will increase again if the directory creation fail.
                    Thread.Sleep(200);
                    ReWritePlaylist();
                }
            }
            else
            {
                Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
            }
        }

        public void DeleteLevel()
        {
            /// This Method Delete the level cache file

            if (m_ErrorNumber < ERROR_LIMIT)
            {
                try
                {
                    if (m_Level != null)
                    {
                        File.Delete($"{PATH}{m_LevelID}{SUFFIX_NAME}.bplist");
                        Console.WriteLine($"{m_LevelID}{SUFFIX_NAME} Deleted");
                        new LevelController().FetchLevel(); /// If a new level is created => Update the LevelController Cache.
                    }
                    else
                    {
                        Console.WriteLine("Seems like the level cache file didn't existed");
                    }
                }
                catch
                {
                    Console.WriteLine(
                        "An error occured While attempting to Delete the Playlist file. (missing directory?)");
                    Console.WriteLine("Attempting to create the directory..");
                    m_ErrorNumber++;
                    CreateDirectory(); /// m_ErrorNumber will increase again if the directory creation fail.
                    Thread.Sleep(200);
                }
            }
            else
            {
                Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
            }
        }

        public void AddMap(string p_Code, string p_SelectedCharacteristic, string p_SelectedDifficultyName, int p_MinScoreRequirement,
            SocketCommandContext p_Context)
        {
            /// <summary>
            /// This Method Add a Map to m_Level.songs (the Playlist), then Call the ReWritePlaylist() Method to update the file.
            /// It use the Hash, the Selected Characteristic (Standard, Lawless, etc)
            /// and the choosed Difficulty Name (Easy, Normal, Hard, Expert, ExpertPlus);
            ///
            /// This method will be locked if m_ErrorNumber < m_ErrorLimit to avoid any loop error.

            if (m_ErrorNumber < ERROR_LIMIT)
            {
                if (m_Level != null)
                {
                    p_Code = p_Code.ToUpper();
                    m_BeatSaver = FetchBeatMap(p_Code, p_Context);
                    if (m_BeatSaver is not null)
                    {
                        bool l_SongAlreadyExist = false;
                        bool l_DifficultyAlreadyExist = false;
                        bool l_ScoreRequirementEdit = false;
                        try
                        {
                            SongFormat l_SongFormat = new SongFormat { hash = m_BeatSaver.versions[0].hash, name = m_BeatSaver.name };

                            InSongFormat l_InSongFormat = new InSongFormat
                            {
                                name = p_SelectedDifficultyName, characteristic = p_SelectedCharacteristic, minScoreRequirement = p_MinScoreRequirement
                            };
                            l_SongFormat.difficulties = new List<InSongFormat> { l_InSongFormat };

                            if (!string.IsNullOrEmpty(l_SongFormat.name))
                            {
                                if (m_Level.songs.Count != 0)
                                {
                                    int l_I;
                                    for (l_I = 0; l_I < m_Level.songs.Count; l_I++) /// check if the map already exist in the playlist.
                                    {
                                        foreach (var l_BeatMapVersion in m_BeatSaver.versions)
                                        {
                                            if (String.Equals(m_Level.songs[l_I].hash, l_BeatMapVersion.hash, StringComparison.CurrentCultureIgnoreCase))
                                            {
                                                l_SongAlreadyExist = true;
                                                break;
                                            }
                                        }

                                        if (l_SongAlreadyExist)
                                            break;
                                    }

                                    if (l_SongAlreadyExist)
                                    {
                                        foreach (var l_Difficulty in m_Level.songs[l_I].difficulties)
                                        {
                                            if (l_InSongFormat.characteristic == l_Difficulty.characteristic && l_InSongFormat.name == l_Difficulty.name)
                                            {
                                                l_DifficultyAlreadyExist = true;
                                                if (l_InSongFormat.minScoreRequirement != l_Difficulty.minScoreRequirement)
                                                {
                                                    l_Difficulty.minScoreRequirement = l_InSongFormat.minScoreRequirement;
                                                    l_ScoreRequirementEdit = true;
                                                }

                                                break;
                                            }
                                        }

                                        if (l_ScoreRequirementEdit)
                                        {
                                            p_Context.Channel.SendMessageAsync($"> :ballot_box_with_check: Min Score Requirement changed to {l_InSongFormat.minScoreRequirement} in Map {l_SongFormat.name} - {p_SelectedDifficultyName} {p_SelectedCharacteristic} ranked in Level {m_LevelID}");
                                            ReWritePlaylist();
                                        }
                                        else if (l_DifficultyAlreadyExist)
                                        {
                                            p_Context.Channel.SendMessageAsync($"> :x: Map {l_SongFormat.name} - {p_SelectedDifficultyName} {p_SelectedCharacteristic} Already Exist In that Playlist");
                                        }
                                        else
                                        {
                                            m_Level.songs[l_I].difficulties.Add(l_InSongFormat);
                                            p_Context.Channel.SendMessageAsync($"> :white_check_mark: Map {l_SongFormat.name} - {p_SelectedDifficultyName} {p_SelectedCharacteristic} added in Level {m_LevelID}");
                                            ReWritePlaylist();
                                        }
                                    }
                                    else
                                    {
                                        m_Level.songs.Add(l_SongFormat);
                                        p_Context.Channel.SendMessageAsync($"> :white_check_mark: Map {l_SongFormat.name} - {p_SelectedDifficultyName} {p_SelectedCharacteristic} added in Level {m_LevelID}");
                                        ReWritePlaylist();
                                    }
                                }
                                else
                                {
                                    m_Level.songs.Add(l_SongFormat);
                                    p_Context.Channel.SendMessageAsync($"> :white_check_mark: Map {l_SongFormat.name} - {p_SelectedDifficultyName} {p_SelectedCharacteristic} added in Level {m_LevelID}");
                                    ReWritePlaylist();
                                }
                            }
                            else
                            {
                                m_ErrorNumber++;
                                p_Context.Channel.SendMessageAsync("> :x: Impossible to get the map name, the key provided could be wrong.");
                            }
                        }
                        catch
                        {
                            m_ErrorNumber++;
                            p_Context.Channel.SendMessageAsync("> :x: Impossible to get the map name, the key provided could be wrong.");
                        }

                        m_MapAdded = l_DifficultyAlreadyExist;
                    }
                }
                else
                {
                    Console.WriteLine("Seems like you forgot to Load the Level, Attempting to load the Level Cache..");
                    m_ErrorNumber++;
                    LoadLevel();
                    Console.WriteLine($"Trying to AddMap {p_Code}");
                    AddMap(p_Code, p_SelectedCharacteristic, p_SelectedDifficultyName, p_MinScoreRequirement, p_Context);
                }
            }
            else
            {
                Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
            }
        }

        public void RemoveMap(string p_Code, string p_SelectedCharacteristic, string p_SelectedDifficultyName,
            SocketCommandContext p_SocketCommandContext)
        {
            /// <summary>
            /// This Method Add a Map to m_Level.songs (the Playlist), then Call the ReWritePlaylist() Method to update the file.
            /// It use the Hash, the Selected Characteristic (Standard, Lawless, etc)
            /// and the choosed Difficulty Name (Easy, Normal, Hard, Expert, ExpertPlus);
            ///
            /// This method will be locked if m_ErrorNumber < m_ErrorLimit to avoid any loop error.

            if (m_ErrorNumber < ERROR_LIMIT)
            {
                if (m_Level != null)
                {
                    p_Code = p_Code.ToUpper();
                    m_BeatSaver = FetchBeatMap(p_Code, p_SocketCommandContext);
                    if (m_BeatSaver is not null)
                    {
                        bool l_SongAlreadyExist = false;
                        bool l_DifficultyAlreadyExist = false;
                        try
                        {
                            SongFormat l_SongFormat = new SongFormat { hash = m_BeatSaver.versions[0].hash, name = m_BeatSaver.name };

                            InSongFormat l_InSongFormat = new InSongFormat
                            {
                                name = p_SelectedDifficultyName, characteristic = p_SelectedCharacteristic, minScoreRequirement = 0
                            };
                            l_SongFormat.difficulties = new List<InSongFormat> { l_InSongFormat };

                            if (!string.IsNullOrEmpty(l_SongFormat.name))
                            {
                                if (m_Level.songs.Count != 0)
                                {
                                    int l_I;
                                    for (l_I = 0; l_I < m_Level.songs.Count; l_I++) /// check if the map already exist in the playlist.
                                    {
                                        foreach (var l_BeatMapVersion in m_BeatSaver.versions)
                                        {
                                            if (String.Equals(m_Level.songs[l_I].hash, l_BeatMapVersion.hash, StringComparison.CurrentCultureIgnoreCase))
                                            {
                                                l_SongAlreadyExist = true;
                                                break;
                                            }
                                        }

                                        if (l_SongAlreadyExist)
                                            break;
                                    }

                                    if (l_SongAlreadyExist)
                                    {
                                        foreach (var l_Difficulty in m_Level.songs[l_I].difficulties)
                                        {
                                            if (l_InSongFormat.characteristic == l_Difficulty.characteristic && l_InSongFormat.name == l_Difficulty.name)
                                            {
                                                l_DifficultyAlreadyExist = true;
                                                l_InSongFormat = l_Difficulty;
                                                break;
                                            }
                                        }

                                        if (l_DifficultyAlreadyExist)
                                        {
                                            m_Level.songs[l_I].difficulties.Remove(l_InSongFormat);
                                            if (m_Level.songs[l_I].difficulties.Count <= 0)
                                            {
                                                m_Level.songs.RemoveAt(l_I);
                                            }

                                            p_SocketCommandContext.Channel.SendMessageAsync($"> :white_check_mark: Map {l_SongFormat.name} - {p_SelectedDifficultyName} {p_SelectedCharacteristic} as been deleted from Level {m_LevelID}");
                                            m_MapDeleted = true;
                                            ReWritePlaylist();
                                        }
                                        else
                                        {
                                            p_SocketCommandContext.Channel.SendMessageAsync($"> :x: Map {l_SongFormat.name} - {p_SelectedDifficultyName} {p_SelectedCharacteristic} doesn't exist in Level {m_LevelID}");
                                            ReWritePlaylist();
                                        }
                                    }
                                    else
                                    {
                                        p_SocketCommandContext.Channel.SendMessageAsync($"> :x: Map {l_SongFormat.name} - {p_SelectedDifficultyName} {p_SelectedCharacteristic} doesn't exist in Level {m_LevelID}");
                                        ReWritePlaylist();
                                    }
                                }
                                else
                                {
                                    p_SocketCommandContext.Channel.SendMessageAsync($"> :x: Map {l_SongFormat.name} - {p_SelectedDifficultyName} {p_SelectedCharacteristic} doesn't exist in Level {m_LevelID}");
                                    ReWritePlaylist();
                                }
                            }
                            else
                            {
                                m_ErrorNumber++;
                                p_SocketCommandContext.Channel.SendMessageAsync("> :x: Impossible to get the map name, the key provided could be wrong.");
                            }
                        }
                        catch
                        {
                            m_ErrorNumber++;
                            p_SocketCommandContext.Channel.SendMessageAsync("> :x: Impossible to get the map name, the key provided could be wrong.");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Seems like you forgot to Load the Level, Attempting to load the Level Cache..");
                    m_ErrorNumber++;
                    LoadLevel();
                    Console.WriteLine($"Trying to RemoveMap {p_Code}");
                    RemoveMap(p_Code, p_SelectedCharacteristic, p_SelectedDifficultyName, p_SocketCommandContext);
                }
            }
            else
            {
                Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
            }
        }


        private void ResetRetryNumber() ///< Concidering the instance is pretty much created for each command, this is useless in most case.
        {
            /// This Method Reset m_ErrorNumber to 0, because if that number exceed m_ErrorLimit, all the "dangerous" method will be locked.    
            m_ErrorNumber = 0;
            Console.WriteLine("RetryNumber set to 0");
        }

        public static string GetPath()
        {
            return PATH;
        }

        public static BeatSaverFormat FetchBeatMap(string p_Code, SocketCommandContext p_SocketCommandContext)
        {
            string l_URL = @$"https://api.beatsaver.com/maps/id/{p_Code}";
            using WebClient l_WebClient = new WebClient();
            try
            {
                Console.WriteLine(l_URL);
                return JsonSerializer.Deserialize<BeatSaverFormat>(l_WebClient.DownloadString(l_URL));
            }
            catch (WebException l_Exception)
            {
                if (l_Exception.Response is HttpWebResponse l_Response)
                {
                    Console.WriteLine("Status Code : {0}", l_Response.StatusCode);
                    if (l_Response.StatusCode == HttpStatusCode.NotFound)
                    {
                        Console.WriteLine($"The Map do not exist");
                        p_SocketCommandContext.Channel.SendMessageAsync("The Map do not exist");
                        return null;
                    }

                    if (l_Response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        Console.WriteLine($"The bot got rate-limited on BeatSaver, Try later");
                        p_SocketCommandContext.Channel.SendMessageAsync("The bot got rate-limited on BeatSaver, Try later");
                        return null;
                    }

                    if (l_Response.StatusCode == HttpStatusCode.BadGateway)
                    {
                        p_SocketCommandContext.Channel.SendMessageAsync("BeatSaver Server BadGateway");
                        Console.WriteLine($"Server BadGateway");
                        return null;
                    }

                    if (l_Response.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        p_SocketCommandContext.Channel.SendMessageAsync("BeatSaver InternalServerError");
                        Console.WriteLine($"InternalServerError");
                        return null;
                    }

                    return null;
                }
                else
                {
                    p_SocketCommandContext.Channel.SendMessageAsync("Internet dead? Something went wrong");
                    Console.WriteLine("OK Internet is Dead?");
                    return null;
                }
            }
        }

        public static BeatSaverFormat FetchBeatMapByHash(string p_Hash, SocketCommandContext p_SocketCommandContext)
        {
            string l_URL = @$"https://api.beatsaver.com/maps/hash/{p_Hash}";
            using WebClient l_WebClient = new WebClient();
            try
            {
                Console.WriteLine(l_URL);
                return JsonSerializer.Deserialize<BeatSaverFormat>(l_WebClient.DownloadString(l_URL));
            }
            catch (WebException l_Exception)
            {
                if (l_Exception.Response is HttpWebResponse l_Response)
                {
                    Console.WriteLine("Status Code : {0}", l_Response.StatusCode);
                    if (l_Response.StatusCode == HttpStatusCode.NotFound)
                    {
                        Console.WriteLine($"The Map do not exist");
                        p_SocketCommandContext.Channel.SendMessageAsync("The Map do not exist");
                        return null;
                    }

                    if (l_Response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        Console.WriteLine($"The bot got rate-limited on BeatSaver, Try later");
                        p_SocketCommandContext.Channel.SendMessageAsync("The bot got rate-limited on BeatSaver, Try later");
                        return null;
                    }

                    if (l_Response.StatusCode == HttpStatusCode.BadGateway)
                    {
                        p_SocketCommandContext.Channel.SendMessageAsync("BeatSaver Server BadGateway");
                        Console.WriteLine($"Server BadGateway");
                        return null;
                    }

                    if (l_Response.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        p_SocketCommandContext.Channel.SendMessageAsync("BeatSaver InternalServerError");
                        Console.WriteLine($"InternalServerError");
                        return null;
                    }

                    return null;
                }
                else
                {
                    p_SocketCommandContext.Channel.SendMessageAsync("Internet dead? Something went wrong");
                    Console.WriteLine("OK Internet is Dead?");
                    return null;
                }
            }
        }
    }
}