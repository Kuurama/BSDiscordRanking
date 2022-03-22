using System.Collections.Generic;
using System.Linq;
using System.Net;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Discord.Modules.UserModule;
using BSDiscordRanking.Formats.API;
using BSDiscordRanking.Formats.Player;
using BSDiscordRanking.Utils;
using Discord;
using Newtonsoft.Json;

namespace BSDiscordRanking.API
{
    internal static partial class WebApp
    {
        
        [ApiAccessHandler("PlayerInfo",  @"\/player\/0*[1-9][0-9]*", @"\/player\/")]
        public static string GetPlayerInfo(HttpListenerResponse p_Response, string p_PlayerID)
        {
            if (UserController.UserExist(p_PlayerID))
            {
                p_PlayerID = UserController.GetPlayer(p_PlayerID);
            }
            else if (!UserController.AccountExist(p_PlayerID) && !UserController.UserExist(p_PlayerID))
            {
                return null;
            }

            Player l_Player = new Player(p_PlayerID);
            int l_PlayerLevel = l_Player.GetPlayerLevel();

            Trophy l_TotalTrophy = new Trophy
            {
                Plastic = 0,
                Silver = 0,
                Gold = 0,
                Diamond = 0,
                Ruby = 0
            };
            foreach (PassedLevel l_PlayerStatsLevel in l_Player.m_PlayerStats.Levels)
            {
                l_PlayerStatsLevel.Trophy ??= new Trophy();
                l_TotalTrophy.Plastic += l_PlayerStatsLevel.Trophy.Plastic;
                l_TotalTrophy.Silver += l_PlayerStatsLevel.Trophy.Silver;
                l_TotalTrophy.Gold += l_PlayerStatsLevel.Trophy.Gold;
                l_TotalTrophy.Diamond += l_PlayerStatsLevel.Trophy.Diamond;
                l_TotalTrophy.Ruby += l_PlayerStatsLevel.Trophy.Ruby;
            }
            List<CustomApiPlayerCategory> l_ApiPlayerCategories = new List<CustomApiPlayerCategory>();

            foreach (CategoryPassed l_LevelCategory in from l_Level in l_Player.m_PlayerStats.Levels
                     where l_Level.Categories != null
                     from l_LevelCategory
                         in l_Level.Categories
                     select l_LevelCategory)
            {
                int l_CategoryFindIndex = l_ApiPlayerCategories.FindIndex(p_X => p_X.Category == l_LevelCategory.Category);
                if (l_CategoryFindIndex < 0)
                {
                    l_ApiPlayerCategories.Add(new CustomApiPlayerCategory
                    {
                        Category = l_LevelCategory.Category,
                        Level = l_Player.GetPlayerLevel(false, l_LevelCategory.Category),
                        MaxLevel = l_Player.GetPlayerLevel(false, l_LevelCategory.Category, true),
                        NumberOfPass = l_LevelCategory.NumberOfPass,
                        TotalNumberOfMaps = l_LevelCategory.TotalNumberOfMaps,
                        Trophy = l_LevelCategory.Trophy
                    }); /// Fetch the categories and gives the player's level on each of them.
                }
                else
                {
                    l_ApiPlayerCategories[l_CategoryFindIndex].NumberOfPass += l_LevelCategory.NumberOfPass;
                    l_ApiPlayerCategories[l_CategoryFindIndex].TotalNumberOfMaps += l_LevelCategory.TotalNumberOfMaps;
                    l_ApiPlayerCategories[l_CategoryFindIndex].Trophy.Plastic += l_LevelCategory.Trophy.Plastic;
                    l_ApiPlayerCategories[l_CategoryFindIndex].Trophy.Silver += l_LevelCategory.Trophy.Silver;
                    l_ApiPlayerCategories[l_CategoryFindIndex].Trophy.Gold += l_LevelCategory.Trophy.Gold;
                    l_ApiPlayerCategories[l_CategoryFindIndex].Trophy.Diamond += l_LevelCategory.Trophy.Diamond;
                    l_ApiPlayerCategories[l_CategoryFindIndex].Trophy.Ruby += l_LevelCategory.Trophy.Ruby;
                }
            }
            
            int l_PassFindIndex = -1;
            PassLeaderboardController l_PassLeaderboardController = null;
            bool l_IsAccLeaderboardBan = false;
            bool l_IsPassLeaderboardBan = false;
            if (s_Config.EnableAccBasedLeaderboard)
            {
                l_PassLeaderboardController = new PassLeaderboardController();
                l_IsPassLeaderboardBan = l_PassLeaderboardController.m_Leaderboard.Leaderboard.Any(p_X => p_X.ScoreSaberID == p_PlayerID && p_X.IsBanned);
                l_PassLeaderboardController.m_Leaderboard.Leaderboard.RemoveAll(p_X => p_X.IsBanned);
                l_PassFindIndex = l_PassLeaderboardController.m_Leaderboard.Leaderboard.FindIndex(p_X => p_X.ScoreSaberID == p_PlayerID);
            }

            int l_AccFindIndex = -1;
            AccLeaderboardController l_AccLeaderboardController = null;
            if (s_Config.EnableAccBasedLeaderboard)
            {
                l_AccLeaderboardController = new AccLeaderboardController();
                l_IsAccLeaderboardBan = l_AccLeaderboardController.m_Leaderboard.Leaderboard.Any(p_X => p_X.ScoreSaberID == p_PlayerID && p_X.IsBanned);
                l_AccLeaderboardController.m_Leaderboard.Leaderboard.RemoveAll(p_X => p_X.IsBanned);
                l_AccFindIndex = l_AccLeaderboardController.m_Leaderboard.Leaderboard.FindIndex(p_X => p_X.ScoreSaberID == p_PlayerID);
            }
            
            Color l_PlayerColor = UserModule.GetRoleColor(RoleController.ReadRolesDB().Roles, null, l_PlayerLevel);

            int l_PassRank = 0, l_AccRank = 0;

            if (s_Config.EnablePassBasedLeaderboard && l_PassLeaderboardController is not null && !l_IsPassLeaderboardBan)
            {
                if (l_PassFindIndex == -1)
                    l_PassRank = 0;

                else
                    l_PassRank = l_PassFindIndex + 1;
            }
            else if (l_IsPassLeaderboardBan)
            {
                l_PassRank = -1;
            }

            if (s_Config.EnableAccBasedLeaderboard && l_AccLeaderboardController is not null && !l_IsAccLeaderboardBan)
            {
                if (l_AccFindIndex == -1)
                    l_AccRank = 0;

                else
                    l_AccRank = l_AccFindIndex + 1;
            }
            else if (l_IsAccLeaderboardBan)
            {
                l_AccRank = -1;
            }


            l_Player.m_PlayerStats.Levels = null; /// Because those are useless info to send.

            PlayerApiOutput l_PlayerApiOutput = new PlayerApiOutput()
            {
                PlayerFull = l_Player.m_PlayerFull,
                PlayerStats = l_Player.m_PlayerStats,
                CustomData = new CustomApiPlayer()
                {
                    PassPointsName = s_Config.PassPointsName,
                    AccPointsName = s_Config.AccPointsName,
                    Level = l_PlayerLevel,
                    ProfileColor = l_PlayerColor,
                    PassRank = l_PassRank,
                    AccRank = l_AccRank,
                    Trophy = l_TotalTrophy,
                    Categories = l_ApiPlayerCategories
                }
            };

            return JsonConvert.SerializeObject(l_PlayerApiOutput);
        }
    }
}