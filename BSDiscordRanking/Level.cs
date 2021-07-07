using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace BSDiscordRanking
{
    public class Level
    {
        private int m_LevelID;
        private LevelFormat m_Level;
        private string m_SuffixName;
        private string m_Path;
        private int m_ErrorLimit = 3;
        private int m_ErrorNumber = 0;

        public Level(int p_LevelID)
        {
            m_LevelID = p_LevelID;
            m_Path = @".\Levels\";
            m_SuffixName = "_Level";

            /////////////////////////////// Needed Setup Method ///////////////////////////////////
            
            CreateDirectory(); /// Make the Level file's directory.
            LoadLevel(); /// Load the Playlist Cache / First Start : Assign needed Playlist Sample.

            ///////////////////////////////////////////////////////////////////////////////////////


            //AddMap("41D7C7B621D397DB0723B55F75AB2EF6BE1891E8", "Standard", "ExpertPlus");
            //AddMap("B76F546A682122155BE11739438FCAE6CFE2C2CF", "Standard", "Easy");

        }

        private void LoadLevel()
        {
            /// <summary>
            /// If First Launch* : Assign a Playlist Sample to m_Level. (mean there isn't any cache file yet)
            /// This Method Load a Playlist from its path and Prefix Name
            /// then Deserialise it to m_Level.
            /// * => If the playlist's file failed to load (or don't exist), it will still load an empty format to m_Level.
            ///
            /// This method will be locked if m_ErrorNumber < m_ErrorLimit to avoid any loop error.
            /// 
            /// </summary>

            if (m_ErrorNumber < m_ErrorLimit)
            {
                if (!Directory.Exists(m_Path))
                {
                    Console.WriteLine("Seems like you forgot to Create the Level Directory, attempting creation..");
                    CreateDirectory();
                    Console.WriteLine("Directory Created, continuing Loading Levels");
                }

                try
                {
                    using (StreamReader l_SR = new StreamReader($"{m_Path}{m_LevelID}{m_SuffixName}.json"))
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
                    Console.WriteLine($"{m_LevelID}{m_SuffixName} Created (Empty Format)");
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
            /// <summary>
            /// This Method Create the Directory needed to save and load the playlist's file from it's Path parameter.
            ///
            /// This method increase m_ErrorNumber on fail.
            /// 
            /// This method will be locked if m_ErrorNumber < m_ErrorLimit to avoid any loop error.
            /// 
            /// </summary>

            if (m_ErrorNumber < m_ErrorLimit)
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

        private void ReWritePlaylist()
        {
            /// <summary>
            /// This Method Serialise the data from m_Level and create a playlist file depending on the path parameter
            /// and it's PrefixName parameter (Prefix is usefull to sort playlist in the game).
            /// Be Aware that it will replace the current Playlist file (if there is any), it shouldn't be an issue
            /// if you Deserialised that playlist to m_Level by using OpenSavedLevel();
            ///
            /// This method increase m_ErrorNumber on fail.
            /// 
            /// This method will be locked if m_ErrorNumber < m_ErrorLimit to avoid any loop error.
            /// 
            /// </summary>

            if (m_ErrorNumber < m_ErrorLimit)
            {
                try
                {
                    if (m_Level != null)
                    {
                        File.WriteAllText($"{m_Path}{m_LevelID}{m_SuffixName}.json", JsonSerializer.Serialize(m_Level));
                                            Console.WriteLine($"{m_LevelID}{m_SuffixName} Updated ({m_Level.songs.Count} maps in Playlist)");
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
            /// 
            /// </summary>

            if (m_ErrorNumber < m_ErrorLimit)
            {
                if (m_Level != null)
                {
                    bool l_SongAlreadyExist = false;
                    SongFormat l_SongFormat = new SongFormat {hash = p_Hash};
                    InSongFormat l_InSongFormat = new InSongFormat
                    {
                        name = p_SelectedDifficultyName, characteristic = p_SelectedCharacteristic
                    };
                    l_SongFormat.difficulties = new List<InSongFormat>();
                    l_SongFormat.difficulties.Add(l_InSongFormat);

                    if (m_Level.songs.Count != 0)
                    {
                        for (int i = 0;
                            i < m_Level.songs.Count;
                            i++) /// check if the map already exist in the playlist.
                        {
                            if (m_Level.songs[i].hash == p_Hash)
                            {
                                l_SongAlreadyExist = true;
                                break;
                            }
                        }

                        if (l_SongAlreadyExist)
                        {
                            Console.WriteLine($"Map {p_Hash} Already Exist In that Playlist");
                        }
                        else
                        {
                            m_Level.songs.Add(l_SongFormat);
                            Console.WriteLine($"Map {p_Hash} Added");
                            ReWritePlaylist();
                        }
                    }
                    else
                    {
                        m_Level.songs.Add(l_SongFormat);
                        Console.WriteLine($"Map {p_Hash} Added");
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

        private void ResetRetryNumber() /// Concidering the instance is pretty much created for each command, this is useless.
        {
            /// <summary>
            /// This Method Reset m_ErrorNumber to 0, because if that number exceed m_ErrorLimit, all the "dangerous" method will be locked.
            /// </summary>

            m_ErrorNumber = 0;
            Console.WriteLine("RetryNumber set to 0");
        }
    }
}