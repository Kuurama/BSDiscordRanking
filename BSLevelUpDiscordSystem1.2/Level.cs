using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BSLevelUpDiscordSystem1._2
{
    public class Level
    {
        private int m_LevelID;
        private LevelFormat m_Level;
        private int m_LevelPrefix;

        public Level(int p_LevelID)
        {
            m_LevelID = p_LevelID;
            string l_Path = @$".\Levels\";
            string l_PrefixName = "_Level";

            CreateDirectory(l_Path); /// Make the Level file's directory.

            OpenSavedLevel(l_Path, l_PrefixName);

            AddMap("41D7C7B621D397DB0723B55F75AB2EF6BE1891E8", "Standard", "ExpertPlus");
            AddMap("B76F546A682122155BE11739438FCAE6CFE2C2CF", "Standard", "Easy");

            ReWritePlaylist(l_Path, l_PrefixName);
        }

        private void OpenSavedLevel(string p_Path, string p_PrefixName)
        {
            try
            {
                using (StreamReader l_SR = new StreamReader($"{p_Path}{m_LevelID}{p_PrefixName}.json"))
                {
                    m_Level = JsonSerializer.Deserialize<LevelFormat>(l_SR.ReadToEnd());
                }
            }
            catch (Exception l_Exception)
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
            }
        }

        private void CreateDirectory(string p_Path)
        {
            if (!Directory.Exists(p_Path))
                Directory.CreateDirectory(p_Path ?? throw new InvalidOperationException());
        }

        private void ReWritePlaylist(string p_Path, string p_PrefixName)
        {
            File.WriteAllText($"{p_Path}{m_LevelID}{p_PrefixName}.json",
                JsonSerializer.Serialize(m_Level));
        }
        private void AddMap(string p_Hash, string p_SelectedCharacteristic, string p_SelectedDifficultyName)
        {
            bool l_SongAlreadyExist = false;
            SongFormat l_SongFormat = new SongFormat {hash = p_Hash};
            InSongFormat l_InSongFormat = new InSongFormat
            {
                name = p_SelectedDifficultyName, characteristic = p_SelectedCharacteristic
            };
            l_SongFormat.difficulties = new List<InSongFormat>();
            l_SongFormat.difficulties.Add(l_InSongFormat);

            Console.WriteLine(m_Level.songs.Count);
            if (m_Level.songs.Count != 0)
            {
                for (int i = 0; i < m_Level.songs.Count; i++) /// check if the map already exist in the playlist.
                {
                    if (m_Level.songs[i].hash == p_Hash)
                    {
                        l_SongAlreadyExist = true;
                        break;
                    }
                }

                if (l_SongAlreadyExist)
                {
                    Console.WriteLine("Song Already Exist In that Playlist");
                }
                else
                {
                    m_Level.songs.Add(l_SongFormat);
                    Console.WriteLine("Song Added");
                }
            }
            else
            {
                m_Level.songs.Add(l_SongFormat);
                Console.WriteLine("Song Added");
            }
        }
    }
}