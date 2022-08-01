using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Discord.Modules.UserModule;
using BSDiscordRanking.Formats.API;
using BSDiscordRanking.Formats.Controller;
using BSDiscordRanking.Formats.Level;
using BSDiscordRanking.Utils;
using Discord;
using Newtonsoft.Json;

namespace BSDiscordRanking.API
{
    internal static partial class WebApp
    {
        [ApiAccessHandler("MapLeaderboard", @"\/mapleaderboard\/{0,1}", @"\/mapleaderboard\/", 0)]
        public static string GetMapLeaderboard(HttpListenerResponse p_Response, string p_Hash, int p_Difficulty, string p_GameMode = null, int? p_Page = null, UInt64? p_ScoreSaberID = null, string p_Country = null, int p_CountPerPage = 10)
        {
            if (p_GameMode is null or "null" or "") p_GameMode = "Standard";

            MapLeaderboardCacheStruct l_MapCache = Program.s_MapLeaderboardCache.FirstOrDefault(p_X => string.Equals(p_X.Hash, p_Hash, StringComparison.CurrentCultureIgnoreCase) && p_X.Difficulty == p_Difficulty && string.Equals(p_X.GameMode, p_GameMode, StringComparison.CurrentCultureIgnoreCase));
            int l_ScoreSaberLeaderboardID = l_MapCache.ScoreSaberLeaderboardID;
            MapLeaderboardFormat l_MapLeaderboard = new MapLeaderboardController(l_ScoreSaberLeaderboardID).m_MapLeaderboard;
            if (l_MapLeaderboard is null || l_MapLeaderboard.scores is null) return null;

            if (p_Country is null)
            {
                l_MapLeaderboard.scores.RemoveAll(p_X => p_X.customData.isBanned == true || p_X.customData.isBotRegistered == false);
            }
            else
            {
                l_MapLeaderboard.scores.RemoveAll(p_X => p_X.customData.isBanned == true || p_X.customData.isBotRegistered == false || string.Equals(p_X.score.leaderboardPlayerInfo.country, p_Country, StringComparison.CurrentCultureIgnoreCase) == false);
            }

            ApiMapLeaderboardCollectionStruct l_MapLeaderboardCollection = new ApiMapLeaderboardCollectionStruct
            {
                Leaderboards = new List<ApiMapLeaderboardContentStruct>(),
                CustomData = new ApiCustomDataStruct
                {
                    Level = l_MapCache.CustomInfo.LevelID,
                    Category = l_MapCache.CustomInfo.Category,
                    Color = UserModule.GetRoleColor(RoleController.ReadRolesDB().Roles, null, l_MapCache.CustomInfo.LevelID)
                }
            };

            if (p_ScoreSaberID is not null)
            {
                p_Page = l_MapLeaderboard.scores.FindIndex(p_X => p_X.score.leaderboardPlayerInfo.id == p_ScoreSaberID.ToString()) / 10 + 1;
            }

            if (p_CountPerPage < 1)
            {
                p_CountPerPage = 10;
            }

            int l_Count = l_MapLeaderboard.scores.Count;

            if (p_Page is not null)
            {
                if (p_Page <= 0) return null;

                l_MapLeaderboardCollection.Metadata = new ApiPageMetadataStruct
                {
                    Page = p_Page.Value,
                    MaxPage = (int)Math.Ceiling((decimal)l_Count / p_CountPerPage),
                    CountPerPage = p_CountPerPage
                };

                List<ApiMapLeaderboardContentStruct> l_LeaderboardContent = new List<ApiMapLeaderboardContentStruct>();
                bool l_PageExist = false;


                for (int l_Index = (p_Page.Value - 1) * l_MapLeaderboardCollection.Metadata.CountPerPage; l_Index < (p_Page.Value - 1) * l_MapLeaderboardCollection.Metadata.CountPerPage + l_MapLeaderboardCollection.Metadata.CountPerPage; l_Index++)
                    try
                    {
                        if (l_Count <= l_Index) continue;

                        MapPlayerScore l_MapScore = l_MapLeaderboard.scores[l_Index];
                        if (UInt64.TryParse(l_MapScore.score.leaderboardPlayerInfo.id, out UInt64 l_PlayerScoreSaberID) == false) continue;

                        ////////////////////////////////

                        float l_AccPoints = 0;
                        float l_PassPoints = 0;
                        bool l_AccWeightAlreadySet = false;
                        bool l_PassWeightAlreadySet = false;

                        if (l_MapCache.CustomInfo.ForceManualWeight)
                        {
                            if (s_Config.AllowForceManualWeightForAccLeaderboard)
                            {
                                l_AccPoints = (float)l_MapScore.score.baseScore / l_MapCache.MaxScore * 100f * s_Config.AccPointMultiplier * l_MapCache.CustomInfo.ManualWeight;
                                l_AccWeightAlreadySet = true;
                            }

                            if (s_Config.AllowAutoWeightForPassLeaderboard)
                            {
                                l_PassPoints = s_Config.PassPointMultiplier * l_MapCache.CustomInfo.ManualWeight;
                                l_PassWeightAlreadySet = true;
                            }
                        }

                        if (l_MapCache.CustomInfo.AutoWeight > 0 && s_Config.AutomaticWeightCalculation)
                        {
                            if (!l_AccWeightAlreadySet && s_Config.AllowAutoWeightForAccLeaderboard)
                            {
                                l_AccPoints = (float)l_MapScore.score.baseScore / l_MapCache.MaxScore * 100f * s_Config.AccPointMultiplier * l_MapCache.CustomInfo.AutoWeight;
                                l_AccWeightAlreadySet = true;
                            }

                            if (!l_PassWeightAlreadySet && s_Config.AllowAutoWeightForPassLeaderboard)
                            {
                                l_PassPoints = s_Config.PassPointMultiplier * l_MapCache.CustomInfo.AutoWeight;
                                l_PassWeightAlreadySet = true;
                            }
                        }

                        if (s_Config.PerPlaylistWeighting)
                        {
                            if (!s_Config.OnlyAutoWeightForAccLeaderboard && !l_AccWeightAlreadySet) l_AccPoints = (float)l_MapScore.score.baseScore / l_MapCache.MaxScore * 100f * s_Config.AccPointMultiplier * l_MapCache.CustomInfo.LevelWeight;

                            if (!s_Config.OnlyAutoWeightForPassLeaderboard && !l_PassWeightAlreadySet) l_PassPoints = s_Config.PassPointMultiplier * l_MapCache.CustomInfo.LevelWeight;
                        }

                        ////////////////////////////////

                        List<RankData> l_RankData = new List<RankData>();
                        if (s_Config.EnablePassBasedLeaderboard)
                        {
                            l_RankData.Add(new RankData
                            {
                                PointsType = "pass",
                                PointsName = s_Config.PassPointsName,
                                Points = l_PassPoints,
                                Rank = 0
                            });
                        }

                        if (s_Config.EnableAccBasedLeaderboard)
                        {
                            l_RankData.Add(new RankData
                            {
                                PointsType = "acc",
                                PointsName = s_Config.AccPointsName,
                                Points = l_AccPoints,
                                Rank = 0
                            });
                        }

                        l_LeaderboardContent.Add(new ApiMapLeaderboardContentStruct()
                        {
                            ScoreSaberID = l_PlayerScoreSaberID,
                            Rank = l_Index + 1,
                            Name = l_MapScore.score.leaderboardPlayerInfo.name,
                            Country = l_MapScore.score.leaderboardPlayerInfo.country,
                            Avatar = l_MapScore.score.leaderboardPlayerInfo.profilePicture,
                            RankData = l_RankData,
                            Weight = 1.0f,
                            BaseScore = l_MapScore.score.baseScore,
                            ModifiedScore = l_MapScore.score.modifiedScore,
                            Modifiers = l_MapScore.score.modifiers,
                            Multiplier = l_MapScore.score.multiplier,
                            BadCuts = l_MapScore.score.badCuts,
                            MissedNotes = l_MapScore.score.missedNotes,
                            MaxCombo = l_MapScore.score.maxCombo,
                            FullCombo = l_MapScore.score.fullCombo,
                            HMD = l_MapScore.score.hmd,
                            TimeSet = l_MapScore.score.timeSet
                        });

                        l_PageExist = true;
                    }
                    catch
                    {
                        // ignored
                    }

                l_MapLeaderboardCollection.Leaderboards = l_LeaderboardContent;

                return l_PageExist
                    ? JsonConvert.SerializeObject(l_MapLeaderboardCollection)
                    : null;
            }
            else
            {
                List<ApiMapLeaderboardContentStruct> l_LeaderboardContent = new List<ApiMapLeaderboardContentStruct>();
                bool l_PageExist = false;

                l_MapLeaderboardCollection.Metadata = new ApiPageMetadataStruct
                {
                    Page = 0,
                    MaxPage = (int)Math.Ceiling((decimal)l_Count / p_CountPerPage),
                    CountPerPage = p_CountPerPage
                };

                for (int l_Index = 0; l_Index < l_MapLeaderboard.scores.Count; l_Index++)
                {
                    MapPlayerScore l_MapScore = l_MapLeaderboard.scores[l_Index];
                    if (UInt64.TryParse(l_MapScore.score.leaderboardPlayerInfo.id, out UInt64 l_PlayerScoreSaberID) == false) continue;

                    ////////////////////////////////

                    float l_AccPoints = 0;
                    float l_PassPoints = 0;
                    bool l_AccWeightAlreadySet = false;
                    bool l_PassWeightAlreadySet = false;

                    if (l_MapCache.CustomInfo.ForceManualWeight)
                    {
                        if (s_Config.AllowForceManualWeightForAccLeaderboard)
                        {
                            l_AccPoints = (float)l_MapScore.score.baseScore / l_MapCache.MaxScore * 100f * s_Config.AccPointMultiplier * l_MapCache.CustomInfo.ManualWeight;
                            l_AccWeightAlreadySet = true;
                        }

                        if (s_Config.AllowAutoWeightForPassLeaderboard)
                        {
                            l_PassPoints = s_Config.PassPointMultiplier * l_MapCache.CustomInfo.ManualWeight;
                            l_PassWeightAlreadySet = true;
                        }
                    }

                    if (l_MapCache.CustomInfo.AutoWeight > 0 && s_Config.AutomaticWeightCalculation)
                    {
                        if (!l_AccWeightAlreadySet && s_Config.AllowAutoWeightForAccLeaderboard)
                        {
                            l_AccPoints = (float)l_MapScore.score.baseScore / l_MapCache.MaxScore * 100f * s_Config.AccPointMultiplier * l_MapCache.CustomInfo.AutoWeight;
                            l_AccWeightAlreadySet = true;
                        }

                        if (!l_PassWeightAlreadySet && s_Config.AllowAutoWeightForPassLeaderboard)
                        {
                            l_PassPoints = s_Config.PassPointMultiplier * l_MapCache.CustomInfo.AutoWeight;
                            l_PassWeightAlreadySet = true;
                        }
                    }

                    if (s_Config.PerPlaylistWeighting)
                    {
                        if (!s_Config.OnlyAutoWeightForAccLeaderboard && !l_AccWeightAlreadySet) l_AccPoints = (float)l_MapScore.score.baseScore / l_MapCache.MaxScore * 100f * s_Config.AccPointMultiplier * l_MapCache.CustomInfo.LevelWeight;

                        if (!s_Config.OnlyAutoWeightForPassLeaderboard && !l_PassWeightAlreadySet) l_PassPoints = s_Config.PassPointMultiplier * l_MapCache.CustomInfo.LevelWeight;
                    }

                    ////////////////////////////////


                    List<RankData> l_RankData = new List<RankData>();

                    if (s_Config.EnablePassBasedLeaderboard)
                    {
                        l_RankData.Add(new RankData
                        {
                            PointsType = "pass",
                            PointsName = s_Config.PassPointsName,
                            Points = l_PassPoints,
                            Rank = 0
                        });
                    }

                    if (s_Config.EnableAccBasedLeaderboard)
                    {
                        l_RankData.Add(new RankData
                        {
                            PointsType = "acc",
                            PointsName = s_Config.AccPointsName,
                            Points = l_AccPoints,
                            Rank = 0
                        });
                    }

                    l_LeaderboardContent.Add(new ApiMapLeaderboardContentStruct
                    {
                        ScoreSaberID = l_PlayerScoreSaberID,
                        Rank = l_Index + 1,
                        Name = l_MapScore.score.leaderboardPlayerInfo.name,
                        Country = l_MapScore.score.leaderboardPlayerInfo.country,
                        Avatar = l_MapScore.score.leaderboardPlayerInfo.profilePicture,
                        RankData = l_RankData,
                        Weight = 1.0f,
                        BaseScore = l_MapScore.score.baseScore,
                        ModifiedScore = l_MapScore.score.modifiedScore,
                        Modifiers = l_MapScore.score.modifiers,
                        Multiplier = l_MapScore.score.multiplier,
                        BadCuts = l_MapScore.score.badCuts,
                        MissedNotes = l_MapScore.score.missedNotes,
                        MaxCombo = l_MapScore.score.maxCombo,
                        FullCombo = l_MapScore.score.fullCombo,
                        HMD = l_MapScore.score.hmd,
                        TimeSet = l_MapScore.score.timeSet
                    });

                    l_PageExist = true;
                }

                l_MapLeaderboardCollection.Leaderboards = l_LeaderboardContent;

                return l_PageExist
                    ? JsonConvert.SerializeObject(l_MapLeaderboardCollection)
                    : null;
            }
        }

        public static void LoadMapLeaderboardCache()
        {
            foreach (int l_LevelID in LevelController.GetLevelControllerCache().LevelID)
            {
                Level l_Level = new Level(l_LevelID);
                foreach (SongFormat l_Song in l_Level.m_Level.songs)
                {
                    foreach (Difficulty l_Difficulty in l_Song.difficulties)
                    {
                        Program.s_MapLeaderboardCache.Add(new MapLeaderboardCacheStruct()
                        {
                            Hash = l_Song.hash,
                            Difficulty = (int)StringToDifficulty(l_Difficulty.name),
                            GameMode = l_Difficulty.characteristic,
                            MaxScore = l_Difficulty.customData.maxScore,
                            ScoreSaberLeaderboardID = l_Difficulty.customData.leaderboardID,
                            CustomInfo = new MapLeaderboardCacheCustomInfoStruct()
                            {
                                ManualWeight = l_Difficulty.customData.manualWeight,
                                AutoWeight = l_Difficulty.customData.AutoWeight,
                                LevelWeight = l_Level.m_Level.customData.weighting,
                                ForceManualWeight = l_Difficulty.customData.forceManualWeight,
                                LevelID = l_LevelID,
                                Category = l_Difficulty.customData.category
                            },
                        });
                    }
                }
            }
        }

        public static UInt32 StringToDifficulty(string p_Input)
        {
            return p_Input switch
            {
                "Easy" => 1,
                "Normal" => 3,
                "Hard" => 5,
                "Expert" => 7,
                "ExpertPlus" => 9,
                _ => 0
            };
        }

        public static string DifficultyToString(UInt32 p_Input)
        {
            return p_Input switch
            {
                1 => "Easy",
                3 => "Normal",
                5 => "Hard",
                7 => "Expert",
                9 => "ExpertPlus",
                _ => string.Empty
            };
        }
    }
}
