using System.Collections.Generic;
using System.Linq;
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
        /// <summary>
        /// Return the corresponding playlist for the given category, if it exists
        /// Giving a ScoreSaberID will return a unPassedPlaylist for the given user
        /// </summary>
        /// <param name="p_Response"></param>
        /// <param name="p_Level"></param>
        /// <param name="p_Category"></param>
        /// <param name="p_ScoreSaberID"></param>
        /// <returns></returns>
        [ApiAccessHandler("playlist", @"\/playlist\/{0,1}", @"\/playlist\/", 0)]
        public static string GetPlaylist(HttpListenerResponse p_Response, int p_Level = 0, string p_Category = null, ulong p_ScoreSaberID = 0)
        {
            Level l_Level = new Level(p_Level);
            if (l_Level.m_Level is null || l_Level.m_Level.songs.Count == 0) return null;

            p_Response.ContentType = "text/plain";

            if (p_Category == "null")
            {
                p_Category = null;
            }
            if (p_Category is null || p_Level == 0)
            {
                if (p_ScoreSaberID == 0)
                {
                    l_Level.m_Level.customData.syncURL = ConfigController.GetConfig().ApiURL + "playlist/" + p_Level;
                    return JsonConvert.SerializeObject(l_Level.m_Level.songs);
                }

                if (UserController.SSIsAlreadyLinked(p_ScoreSaberID.ToString()))
                {
                    Player l_Player = new Player(p_ScoreSaberID.ToString());
                    l_Player.LoadPass();

                    l_Level.m_Level = UserModule.RemovePassFromPlaylist(l_Player.m_PlayerPass, l_Level.m_Level, null, l_Player.GetPlayerID());
                    if ( l_Level.m_Level.songs.Any()) return JsonConvert.SerializeObject(l_Level.m_Level);

                    p_Response.ContentType = null;
                    return null;
                }

                p_Response.ContentType = null;
                return null;
            }
            else
            {
                LevelFormat l_LevelFormat = null;
                p_Category = UserModule.FirstCharacterToUpper(p_Category);
                l_LevelFormat = UserModule.RemoveOtherCategoriesFromPlaylist(l_Level.m_Level, p_Category).LevelFormat;

                if (p_ScoreSaberID != 0 && UserController.SSIsAlreadyLinked(p_ScoreSaberID.ToString()))
                {
                    Player l_Player = new Player(p_ScoreSaberID.ToString());
                    l_Player.LoadPass();
                    l_LevelFormat = UserModule.RemovePassFromPlaylist(l_Player.m_PlayerPass, l_LevelFormat, p_Category, l_Player.GetPlayerID());
                }

                if (l_LevelFormat.songs.Any())
                    return JsonConvert.SerializeObject(l_LevelFormat);

                p_Response.ContentType = null;
                return null;
            }
        }
    }
}
