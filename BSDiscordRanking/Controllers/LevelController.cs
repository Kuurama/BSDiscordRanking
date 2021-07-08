using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BSDiscordRanking.Controllers
{
    public class LevelController
    {
        public static List<Level> FetchLevels()
        {
            int l_i = 1;
            List<Level> l_list = new List<Level>();
            string[] l_files = Directory.GetFiles(@".\Levels\");
            foreach (var l_file in l_files)
            {
                Level l_level = new Level(l_i);
                l_level.m_Level = JsonSerializer.Deserialize<LevelFormat>(new StreamReader(l_file).ReadToEnd());
                l_list.Add(l_level);
                l_i++;
            }
            return l_list;
        }
    }
}