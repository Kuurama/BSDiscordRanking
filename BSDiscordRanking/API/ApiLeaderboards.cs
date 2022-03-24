using System.Collections.Generic;
using System.Net;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Controller;
using Newtonsoft.Json;

namespace BSDiscordRanking.API
{
    internal static partial class WebApp
    {
        [ApiAccessHandler("Leaderboards", @"\/leaderboards\/{0,1}", @"\/leaderboards\/", 0)]
        public static string GetLeaderboards(HttpListenerResponse p_Response, string p_LeaderboardType = null, string p_PageOrFull = null, int p_CountPerPage = default)

        {
            PlayerLeaderboardController l_LeaderboardController;
            switch (p_LeaderboardType)
            {
                case null:
                    return null;
                case "acc":

                    l_LeaderboardController = new AccLeaderboardController();
                    break;
                case "pass":
                    l_LeaderboardController = new PassLeaderboardController();
                    break;
                default:
                    return null;
            }

            l_LeaderboardController.m_Leaderboard.Leaderboard.RemoveAll(p_X => p_X.IsBanned);
            if (p_CountPerPage != default)
            {
                l_LeaderboardController.m_Leaderboard.CountPerPage = p_CountPerPage;
            }

            if (int.TryParse(p_PageOrFull, out int l_Page))
            {
                if (l_Page <= 0) return null;

                List<RankedPlayer> l_LeaderboardByPage = new List<RankedPlayer>();
                bool l_PageExist = false;

                for (int l_Index = (l_Page - 1) * l_LeaderboardController.m_Leaderboard.CountPerPage; l_Index < (l_Page - 1) * l_LeaderboardController.m_Leaderboard.CountPerPage + l_LeaderboardController.m_Leaderboard.CountPerPage; l_Index++)
                    try
                    {
                        if (l_LeaderboardController.m_Leaderboard.Leaderboard.Count <= l_Index) continue;

                        RankedPlayer l_RankedPlayer = l_LeaderboardController.m_Leaderboard.Leaderboard[l_Index];
                        l_LeaderboardByPage.Add(l_RankedPlayer);
                        l_PageExist = true;
                    }
                    catch
                    {
                        // ignored
                    }

                return l_PageExist
                    ? JsonConvert.SerializeObject(new LeaderboardControllerFormat() { Type = l_LeaderboardController.m_Leaderboard.Type, Name = l_LeaderboardController.m_Leaderboard.Name, Page = p_PageOrFull, MaxPage = l_LeaderboardController.m_Leaderboard.MaxPage, CountPerPage = l_LeaderboardController.m_Leaderboard.CountPerPage, Leaderboard = l_LeaderboardByPage})
                    : null;
            }

            return p_PageOrFull is "full" ? JsonConvert.SerializeObject(l_LeaderboardController.m_Leaderboard) : null;
        }
    }
}