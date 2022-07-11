using System.Collections.Generic;
using System.Net;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Discord.Modules.UserModule;
using BSDiscordRanking.Formats.Controller;
using BSDiscordRanking.Formats.Level;
using Newtonsoft.Json;

namespace BSDiscordRanking.API
{
    internal static partial class WebApp
    {
        [ApiAccessHandler("playlist", @"\/playlist\/{0,1}", @"\/playlist\/", 0)]
        public static string GetPlaylist(HttpListenerResponse p_Response, int p_Level = 0, string p_Category = null)
        {
            Level l_Level = new Level(p_Level);
            if (l_Level.m_Level is null || l_Level.m_Level.songs.Count == 0) return null;

            p_Response.ContentType = "text/plain";

            if (p_Category is null || p_Level == 0)
            {
                l_Level.m_Level.customData.syncURL = ConfigController.GetConfig().ApiURL + "playlist/" + p_Level;
                return JsonConvert.SerializeObject(l_Level.m_Level.songs);
            }
            else
            {
                p_Category = UserModule.FirstCharacterToUpper(p_Category);
                LevelFormat l_LevelFormat = UserModule.RemoveOtherCategoriesFromPlaylist(l_Level.m_Level, p_Category).LevelFormat;
                l_LevelFormat.customData.syncURL = ConfigController.GetConfig().ApiURL + "playlist/" + p_Level + "/" + p_Category;
                return JsonConvert.SerializeObject(l_LevelFormat);
            }
        }
    }
}
