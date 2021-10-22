using System;
using BSDiscordRanking.Controllers;

namespace BSDiscordRanking
{
    internal static class Program
    {
        private static void Main(string[] p_Args)
        {
            new LevelController().FetchLevel();
            UserController.ReadDB();
            Discord.BotHandler.StartBot(ConfigController.ReadConfig());
        }
    }
}