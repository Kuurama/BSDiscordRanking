using BSDiscordRanking.Controllers;
using BSDiscordRanking.Discord;

namespace BSDiscordRanking
{
    internal static class Program
    {
        private static void Main(string[] p_Args)
        {
            new LevelController().FetchLevel();
            UserController.ReadDB();
            BotHandler.StartBot(ConfigController.ReadConfig());
        }
    }
}