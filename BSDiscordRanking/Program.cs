using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using BSDiscordRanking.API;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Discord;

namespace BSDiscordRanking
{
    internal static class Program
    {
        public static ulong TempGlobalGuildID = default(ulong);
        private static void Main(string[] p_Args)
        {
            ApiAccessHandler.InitHandlers();
            new Thread(WebApp.Start).Start(); /// Starts the API

            LevelController.GetLevelControllerCache();
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
}
