using System.Collections.Generic;
using System.Linq;
using System.Net;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Discord.Modules.UserModule;
using BSDiscordRanking.Formats.API;
using BSDiscordRanking.Formats.Player;
using Discord;
using Newtonsoft.Json;

namespace BSDiscordRanking.API
{
    internal static partial class WebApp
    {
        [ApiAccessHandler("PlayerInfo", @"\/player/data\/0*[1-9][0-9]*", @"\/player/data\/", 0)]
        public static string GetPlayerInfo(HttpListenerResponse p_Response, string p_PlayerID)
        {
            if (UserController.UserExist(p_PlayerID))
            {
                p_PlayerID = UserController.GetPlayer(p_PlayerID);
            }
            else if (!UserController.AccountExist(p_PlayerID, out _) && !UserController.UserExist(p_PlayerID))
            {
                return null;
            }

            Player l_Player = new Player(p_PlayerID, false);

            int l_PlayerLevel = l_Player.GetPlayerLevel();
            Trophy l_TotalTrophy = GetTotalTrophy(l_Player.m_PlayerStats.Levels);
            List<RankData> l_RankData = GetRankData(p_PlayerID, l_Player.m_PlayerStats.PassPoints, l_Player.m_PlayerStats.AccPoints);
            List<CustomApiPlayerCategory> l_ApiPlayerCategories = GetPlayerCategoriesInfo(l_Player);
            Color l_PlayerColor = UserModule.GetRoleColor(RoleController.ReadRolesDB().Roles, null, l_PlayerLevel);

            PlayerApiReworkOutput l_ApiReworkOutput = new PlayerApiReworkOutput()
            {
                Id = p_PlayerID,
                Name = l_Player.m_PlayerFull.name,
                Country = l_Player.m_PlayerFull.country,
                ProfilePicture = l_Player.m_PlayerFull.profilePicture,
                ProfileColor = l_PlayerColor,
                Badges = l_Player.m_PlayerFull.badges, // ScoreSaber Badges for now.
                Trophy = l_TotalTrophy,
                Level = l_PlayerLevel,
                PassCount = l_Player.m_PlayerStats.TotalNumberOfPass,
                IsMapLeaderboardBanned = l_Player.m_PlayerStats.IsMapLeaderboardBanned,
                IsScanBanned = l_Player.m_PlayerStats.IsScanBanned,
                RankData = l_RankData,
                CategoryData = l_ApiPlayerCategories
            };

            return JsonConvert.SerializeObject(l_ApiReworkOutput);
        }

        private static List<CustomApiPlayerCategory> GetPlayerCategoriesInfo(Player p_Player)
        {
            List<CustomApiPlayerCategory> l_ApiPlayerCategories = new List<CustomApiPlayerCategory>();

            foreach (CategoryPassed l_LevelCategory in from l_Level in p_Player.m_PlayerStats.Levels
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
                        Level = p_Player.GetPlayerLevel(false, l_LevelCategory.Category),
                        MaxLevel = p_Player.GetPlayerLevel(false, l_LevelCategory.Category, true),
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
            l_ApiPlayerCategories.RemoveAll(p_X => string.IsNullOrEmpty(p_X.Category)); /// Small HardCodding of the "OnlyRankingByCategory".
            return l_ApiPlayerCategories;
        }

        private static Trophy GetTotalTrophy(List<PassedLevel> p_PassedLevels)
        {
            Trophy l_TotalTrophy = new Trophy
            {
                Plastic = 0,
                Silver = 0,
                Gold = 0,
                Diamond = 0,
                Ruby = 0
            };
            if (p_PassedLevels is null) return l_TotalTrophy;

            foreach (CategoryPassed l_Category in p_PassedLevels.SelectMany(p_PlayerStatsLevel => p_PlayerStatsLevel.Categories))
            {
                l_Category.Trophy ??= new Trophy();
                l_TotalTrophy.Plastic += l_Category.Trophy.Plastic;
                l_TotalTrophy.Silver += l_Category.Trophy.Silver;
                l_TotalTrophy.Gold += l_Category.Trophy.Gold;
                l_TotalTrophy.Diamond += l_Category.Trophy.Diamond;
                l_TotalTrophy.Ruby += l_Category.Trophy.Ruby;
            }
            return l_TotalTrophy;
        }

        private static List<RankData> GetRankData(string p_PlayerID, float p_PassPoints, float p_AccPoints)
        {
            List<RankData> l_RankData = new List<RankData>();

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

            if (s_Config.EnablePassBasedLeaderboard && l_PassLeaderboardController is not null && !l_IsPassLeaderboardBan)
            {
                if (l_PassFindIndex == -1)
                {
                    l_RankData.Add(new RankData()
                    {
                        PointsType = "pass",
                        PointsName = s_Config.PassPointsName,
                        Points = 0,
                        Rank = 0
                    });
                }
                else
                {
                    l_RankData.Add(new RankData()
                    {
                        PointsType = "pass",
                        PointsName = s_Config.PassPointsName,
                        Points = p_PassPoints,
                        Rank = l_PassFindIndex + 1
                    });
                }
            }

            if (s_Config.EnableAccBasedLeaderboard && l_AccLeaderboardController is not null && !l_IsAccLeaderboardBan)
            {
                if (l_AccFindIndex == -1)
                {
                    l_RankData.Add(new RankData()
                    {
                        PointsType = "acc",
                        PointsName = s_Config.AccPointsName,
                        Points = 0,
                        Rank = 0
                    });
                }
                else
                {
                    l_RankData.Add(new RankData()
                    {
                        PointsType = "acc",
                        PointsName = s_Config.AccPointsName,
                        Points = p_AccPoints,
                        Rank = l_AccFindIndex + 1
                    });
                }
            }
            return l_RankData;
        }
    }
}
