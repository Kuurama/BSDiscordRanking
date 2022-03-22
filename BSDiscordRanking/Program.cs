using System.Threading;
using BSDiscordRanking.API;

namespace BSDiscordRanking
{
    internal static class Program
    {
        public static ulong m_TempGlobalGuildID = default(ulong);
        private static void Main(string[] p_Args)
        {
            ApiAccessHandler.InitHandlers();
            
            new Thread(WebApp.Start).Start(); /// Starts the API
            
            //LevelController.GetLevelControllerCache();
            //UserController.ReadDB();
            //BotHandler.StartBot(ConfigController.ReadConfig()); /// Starts the Discord Bot (on the main thread)
        }
    }
}