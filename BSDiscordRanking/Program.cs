using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using BSDiscordRanking.API;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Discord;
using BSDiscordRanking.Formats.Controller;

namespace BSDiscordRanking
{
    internal static class Program
    {
        public static ulong TempGlobalGuildID = default(ulong);
        public static readonly List<MapLeaderboardCacheStruct> s_MapLeaderboardCache = new List<MapLeaderboardCacheStruct>();
        private static void Main(string[] p_Args)
        {
            ApiAccessHandler.InitHandlers();
            WebApp.LoadMapLeaderboardCache();
            new Thread(WebApp.Start).Start(); /// Starts the API

            LevelController.Init();
            UserController.ReadDB();
            BotHandler.StartBot(ConfigController.ReadConfig()); /// Starts the Discord Bot (on the main thread)
        }

        public static void RefreshLevelCover()
        {
            foreach (int l_LevelID in LevelController.GetLevelControllerCache().LevelID)
            {
                byte[] l_ImageBytes = System.IO.File.ReadAllBytes($@"./public/Cover/lvl{l_LevelID}.png");
                string l_Base64String = Convert.ToBase64String(l_ImageBytes);
                Level l_Level = new Level(l_LevelID);

                l_Level.m_Level.image = l_Base64String;
                l_Level.ReWritePlaylist(false);
            }
        }
    }

    public struct MapLeaderboardCacheStruct
    {
        public string Hash { get; set; }
        public int Difficulty { get; set; }
        public string GameMode { get; set; }
        public int ScoreSaberLeaderboardID { get; set; }
        public int MaxScore { get; set; }
        public MapLeaderboardCacheCustomInfoStruct CustomInfo { get; set; }

    }

    public struct MapLeaderboardCacheCustomInfoStruct
    {
        public float ManualWeight { get; set; }
        public float AutoWeight { get; set; }
        public float LevelWeight { get; set; }
        public bool ForceManualWeight { get; set; }
        public int LevelID { get; set; }
        public string Category { get; set; }
    }
}
