using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace BSDiscordRanking
{
    public class Level
    {
        public LevelFormat m_Level;
        private int m_LevelID;
        private const string SUFFIX_NAME = "_Level"; /// Keep the underscore at the beginning to avoid issue with the controller.
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


            //AddMap("41D7C7B621D397DB0723B55F75AB2EF6BE1891E8", "Standard", "ExpertPlus");
            //AddMap("B76F546A682122155BE11739438FCAE6CFE2C2CF", "Standard", "Easy");
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
                    using (StreamReader l_SR = new StreamReader($"{PATH}{m_LevelID}{SUFFIX_NAME}.json"))
                    {
                        m_Level = JsonSerializer.Deserialize<LevelFormat>(l_SR.ReadToEnd());
                        if (m_Level == null) /// json contain "null"
                        {
                            m_Level = new LevelFormat()
                            {
                                songs = new List<SongFormat>(),
                                playlistTitle = new string(""),
                                playlistAuthor = new string(""),
                                playlistDescription = new string(""),
                                syncURL = null,
                                image = new string("")
                            };
                            Console.WriteLine($"Level {m_LevelID} Created (Empty Format), contained null");
                        }
                        else
                        {
                            Console.WriteLine($"Level {m_LevelID} Loaded");
                            foreach (var l_Songs in m_Level.songs)
                            {
                                l_Songs.hash = l_Songs.hash.ToUpper();
                            }
                        }
                    }
                }
                catch (Exception) /// file format is wrong / there isn't any file.
                {
                    m_Level = new LevelFormat()
                    {
                        songs = new List<SongFormat>(),
                        playlistTitle = new string(""),
                        playlistAuthor = new string(""),
                        playlistDescription = new string(""),
                        syncURL = null,
                        image = new string("")
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
                        File.WriteAllText($"{PATH}{m_LevelID}{SUFFIX_NAME}.json", JsonSerializer.Serialize(m_Level));
                        Console.WriteLine($"{m_LevelID}{SUFFIX_NAME} Updated ({m_Level.songs.Count} maps in Playlist)");
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

        public void AddMap(string p_Hash, string p_SelectedCharacteristic, string p_SelectedDifficultyName)
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
                    p_Hash = p_Hash.ToUpper();
                    bool l_SongAlreadyExist = false;
                    bool l_DifficultyAlreadyExist = false;
                    SongFormat l_SongFormat = new SongFormat {hash = p_Hash};
                    InSongFormat l_InSongFormat = new InSongFormat
                    {
                        name = p_SelectedDifficultyName, characteristic = p_SelectedCharacteristic
                    };
                    l_SongFormat.difficulties = new List<InSongFormat>();
                    l_SongFormat.difficulties.Add(l_InSongFormat);

                    if (m_Level.songs.Count != 0)
                    {
                        int l_I;
                        for (l_I = 0;
                            l_I < m_Level.songs.Count;
                            l_I++) /// check if the map already exist in the playlist.
                        {
                            if (m_Level.songs[l_I].hash == p_Hash)
                            {
                                l_SongAlreadyExist = true;
                                break;
                            }
                        }

                        if (l_SongAlreadyExist)
                        {
                            foreach (var l_Difficulty in m_Level.songs[l_I].difficulties)
                            {
                                if (l_InSongFormat.characteristic == l_Difficulty.characteristic && l_InSongFormat.name == l_Difficulty.name)
                                    l_DifficultyAlreadyExist = true;
                            }

                            if (l_DifficultyAlreadyExist)
                            {
                                Console.WriteLine($"Map {p_Hash} - {p_SelectedDifficultyName} {p_SelectedCharacteristic} Already Exist In that Playlist");
                            }
                            else
                            {
                                m_Level.songs[l_I].difficulties.Add(l_InSongFormat);
                                Console.WriteLine($"Map {p_Hash} - {p_SelectedDifficultyName} {p_SelectedCharacteristic} Added");
                                ReWritePlaylist();
                            }
                        }
                        else
                        {
                            m_Level.songs.Add(l_SongFormat);
                            Console.WriteLine($"Map {p_Hash} - {p_SelectedDifficultyName} {p_SelectedCharacteristic} Added");
                            ReWritePlaylist();
                        }
                    }
                    else
                    {
                        m_Level.songs.Add(l_SongFormat);
                        Console.WriteLine($"Map {p_Hash} - {p_SelectedDifficultyName} {p_SelectedCharacteristic} Added");
                        ReWritePlaylist();
                    }
                }
                else
                {
                    Console.WriteLine("Seems like you forgot to Load the Level, Attempting to load the Level Cache..");
                    m_ErrorNumber++;
                    LoadLevel();
                    Console.WriteLine($"Trying to AddMap {p_Hash}");
                    AddMap(p_Hash, p_SelectedCharacteristic, p_SelectedDifficultyName);
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
    }
}