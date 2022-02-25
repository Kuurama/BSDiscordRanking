using BSDiscordRanking.API;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Discord;

namespace BSDiscordRanking
{
    internal static class Program
    {
        public static ulong m_TempGlobalGuildID = default(ulong);

        private static void Main(string[] p_Args)
        {
            LevelController.GetLevelControllerCache();
            UserController.ReadDB();
            BotHandler.StartBot(ConfigController.ReadConfig());
            //WebApp.Start();
        }
    }
}