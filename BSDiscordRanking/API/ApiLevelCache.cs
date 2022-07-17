using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Controller;
using Newtonsoft.Json;

namespace BSDiscordRanking.API
{
    internal static partial class WebApp
    {
        [ApiAccessHandler("LevelCache", @"\/levelcache\/{0,1}", @"\/levelcache\/", 0)]
        public static string GetLevelCache(HttpListenerResponse p_Response, string p_Category = null)
        {
            LevelControllerFormat l_AvailableLevels = LevelController.GetLevelControllerCache();

            if (string.IsNullOrEmpty(p_Category) || p_Category == "null") return JsonConvert.SerializeObject(l_AvailableLevels);

            LevelControllerFormat l_CategoryAvailableLevels = new LevelControllerFormat()
            {
                LevelID = new List<int>()
            };

            foreach (int l_LevelID in from l_LevelID in l_AvailableLevels.LevelID
                     let l_Level = new Level(l_LevelID)
                     where l_Level.m_Level.songs.Exists(p_X => p_X.difficulties.Exists(p_Y => string.Equals(p_Y.customData.category, p_Category, StringComparison.CurrentCultureIgnoreCase)))
                     select l_LevelID)
                l_CategoryAvailableLevels.LevelID.Add(l_LevelID);

            return JsonConvert.SerializeObject(l_CategoryAvailableLevels);
        }
    }
}
