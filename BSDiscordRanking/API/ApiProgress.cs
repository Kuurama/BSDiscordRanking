using System.Collections.Generic;
using System.Linq;
using System.Net;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Discord.Modules.UserModule;
using BSDiscordRanking.Formats.Player;
using Newtonsoft.Json;

namespace BSDiscordRanking.API
{
    internal static partial class WebApp
    {
        [ApiAccessHandler("Progress", @"\/progress\/{0,1}", @"\/progress\/", 0)]
        public static string GetProgress(HttpListenerResponse p_Response, string p_PlayerID, string p_Category = null)
        {
            if (UserController.UserExist(p_PlayerID))
            {
                p_PlayerID = UserController.GetPlayer(p_PlayerID);
            }
            else if (!UserController.AccountExist(p_PlayerID) && !UserController.UserExist(p_PlayerID))
            {
                return null;
            }

            PlayerStatsFormat l_PlayerStats = Player.GetStaticStats(p_PlayerID);

            List<ApiLevelPassed> l_ApiLevelPassed = new List<ApiLevelPassed>();
            List<string> l_AvailableCategories = new List<string>();
            foreach (PassedLevel l_PlayerLevelStats in l_PlayerStats.Levels)
            {
                if (p_Category == null)
                {
                    if (l_PlayerLevelStats.TotalNumberOfMaps > 0)
                    {
                        foreach (CategoryPassed l_Category in l_PlayerLevelStats.Categories.Where(p_CategoryPassed => l_AvailableCategories.FindIndex(p_Y => p_Y == p_CategoryPassed.Category) < 0))
                        {
                            l_AvailableCategories.Add(l_Category.Category);
                        }
                        
                        l_ApiLevelPassed.Add(new ApiLevelPassed()
                        {
                            LevelID = l_PlayerLevelStats.LevelID,
                            NumberOfPass = l_PlayerLevelStats.NumberOfPass,
                            TotalNumberOfMaps = l_PlayerLevelStats.TotalNumberOfMaps
                        });
                    }
                }
                else
                {
                    
                    p_Category = UserModule.FirstCharacterToUpper(p_Category);
                    foreach (CategoryPassed l_Category in l_PlayerLevelStats.Categories)
                    {
                        if (l_AvailableCategories.FindIndex(p_Y => p_Y == l_Category.Category) < 0) l_AvailableCategories.Add(l_Category.Category);
                        if (l_Category.Category == p_Category && l_Category.TotalNumberOfMaps > 0)
                        {
                            l_ApiLevelPassed.Add(new ApiLevelPassed()
                            {
                                LevelID = l_PlayerLevelStats.LevelID,
                                NumberOfPass = l_Category.NumberOfPass,
                                TotalNumberOfMaps = l_Category.TotalNumberOfMaps
                            });
                        }
                    }
                }
            }
            l_AvailableCategories.RemoveAll(string.IsNullOrEmpty);

            return JsonConvert.SerializeObject(new ApiProgressFormat()
            {
                Levels = l_ApiLevelPassed,
                AvailableCategories = l_AvailableCategories
            });
        }
    }

    public class ApiProgressFormat
    {
        public List<ApiLevelPassed> Levels { get; set; }
        public List<string> AvailableCategories { get; set; }
    }

    public class ApiLevelPassed
    {
        public int LevelID { get; set; }
        public int NumberOfPass { get; set; }
        public int TotalNumberOfMaps { get; set; }
    }
}