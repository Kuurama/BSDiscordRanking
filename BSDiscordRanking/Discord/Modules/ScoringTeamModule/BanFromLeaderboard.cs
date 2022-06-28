using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.ScoringTeamModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class ScoringTeamModule : ModuleBase<SocketCommandContext>
    {
        [Command("ldaccban")]
        [Alias("banldacc")]
        [Summary("Toggle a player ban from the acc leaderboard.")]
        public async Task LDAccBan(string p_DiscordOrScoreSaberID = null)
        {
            if (string.IsNullOrEmpty(p_DiscordOrScoreSaberID))
            {
                await ReplyAsync($"> :x: Seems like you didn't used the command correctly, use: `{BotHandler.m_Prefix}ldaccban [DiscordOrScoreSaberID]`");
                return;
            }

            bool l_IsScoreSaberAccount = UserController.AccountExist(p_DiscordOrScoreSaberID, out _);

            if (UserController.UserExist(p_DiscordOrScoreSaberID))
            {
                p_DiscordOrScoreSaberID = UserController.GetPlayer(p_DiscordOrScoreSaberID);
            }
            else if (!UserController.UserExist(p_DiscordOrScoreSaberID) && !l_IsScoreSaberAccount)
            {
                await ReplyAsync("> :x: Sorry, this Discord User doesn't have any ScoreSaber account linked/isn't a correct ScoreSaberID.");
                return;
            }

            if (ulong.TryParse(p_DiscordOrScoreSaberID, out ulong l_ScoreSaberID))
            {
                EmbedBuilder l_EmbedBuilder = new EmbedBuilder().WithColor(Color.Purple);
                Player l_Player = new Player(p_DiscordOrScoreSaberID);
                AccLeaderboardController l_AccLeaderboardController = new AccLeaderboardController();
                int l_Index = l_AccLeaderboardController.m_Leaderboard.Leaderboard.FindIndex(p_X => p_X.ScoreSaberID == p_DiscordOrScoreSaberID);
                l_AccLeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsBanned = !l_AccLeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsBanned;
                l_AccLeaderboardController.ReWriteLeaderboard();
                if (l_Player.m_PlayerFull != null) await ReplyAsync($"> {l_Player.m_PlayerFull.name}'s {ConfigController.GetConfig().AccPointsName} Ban preference has been changed from **{!l_AccLeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsBanned}** to **{l_AccLeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsBanned}**");
            }
        }

        [Command("ldpassban")]
        [Alias("banldpass")]
        [Summary("Toggle a player ban from the acc leaderboard.")]
        public async Task LDPassBan(string p_DiscordOrScoreSaberID = null)
        {
            if (string.IsNullOrEmpty(p_DiscordOrScoreSaberID))
            {
                await ReplyAsync($"> :x: Seems like you didn't used the command correctly, use: `{BotHandler.m_Prefix}ldpassban [DiscordOrScoreSaberID]`");
                return;
            }

            bool l_IsScoreSaberAccount = UserController.AccountExist(p_DiscordOrScoreSaberID, out _);

            if (UserController.UserExist(p_DiscordOrScoreSaberID))
            {
                p_DiscordOrScoreSaberID = UserController.GetPlayer(p_DiscordOrScoreSaberID);
            }
            else if (!UserController.UserExist(p_DiscordOrScoreSaberID) && !l_IsScoreSaberAccount)
            {
                await ReplyAsync("> :x: Sorry, this Discord User doesn't have any ScoreSaber account linked/isn't a correct ScoreSaberID.");
                return;
            }

            if (ulong.TryParse(p_DiscordOrScoreSaberID, out ulong l_ScoreSaberID))
            {
                EmbedBuilder l_EmbedBuilder = new EmbedBuilder().WithColor(Color.Purple);
                Player l_Player = new Player(p_DiscordOrScoreSaberID);
                PassLeaderboardController l_PassLeaderboardController = new PassLeaderboardController();
                int l_Index = l_PassLeaderboardController.m_Leaderboard.Leaderboard.FindIndex(p_X => p_X.ScoreSaberID == p_DiscordOrScoreSaberID);
                l_PassLeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsBanned = !l_PassLeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsBanned;
                l_PassLeaderboardController.ReWriteLeaderboard();
                if (l_Player.m_PlayerFull != null) await ReplyAsync($"> {l_Player.m_PlayerFull.name}'s {ConfigController.GetConfig().PassPointsName} Ban preference has been changed from **{!l_PassLeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsBanned}** to **{l_PassLeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsBanned}**");
            }
        }

        [Command("ldmapban")]
        [Alias("banldmap", "banmapld")]
        [Summary("Toggle a player ban from the acc leaderboard.")]
        public async Task LDMapBan(string p_DiscordOrScoreSaberID = null)
        {
            if (string.IsNullOrEmpty(p_DiscordOrScoreSaberID))
            {
                await ReplyAsync($"> :x: Seems like you didn't used the command correctly, use: `{BotHandler.m_Prefix}ldmapban [DiscordOrScoreSaberID]`");
                return;
            }

            bool l_IsScoreSaberAccount = UserController.AccountExist(p_DiscordOrScoreSaberID, out _);

            if (UserController.UserExist(p_DiscordOrScoreSaberID))
            {
                p_DiscordOrScoreSaberID = UserController.GetPlayer(p_DiscordOrScoreSaberID);
            }
            else if (!UserController.UserExist(p_DiscordOrScoreSaberID) && !l_IsScoreSaberAccount)
            {
                await ReplyAsync("> :x: Sorry, this Discord User doesn't have any ScoreSaber account linked/isn't a correct ScoreSaberID.");
                return;
            }

            if (ulong.TryParse(p_DiscordOrScoreSaberID, out ulong l_ScoreSaberID))
            {
                EmbedBuilder l_EmbedBuilder = new EmbedBuilder().WithColor(Color.Purple);
                Player l_Player = new Player(p_DiscordOrScoreSaberID);
                l_Player.m_PlayerStats.IsMapLeaderboardBanned = !l_Player.m_PlayerStats.IsMapLeaderboardBanned;
                l_Player.ReWriteStats();
                if (l_Player.m_PlayerFull != null) await ReplyAsync($"> {l_Player.m_PlayerFull.name}'s MapLeaderboard Ban preference has been changed from **{!l_Player.m_PlayerStats.IsMapLeaderboardBanned}** to **{l_Player.m_PlayerStats.IsMapLeaderboardBanned}**");
            }
        }

        [Command("scanban")]
        [Alias("banscan")]
        [Summary("Toggle a player scan ban (prevent from scanning on the bot).")]
        public async Task ScanBan(string p_DiscordOrScoreSaberID = null)
        {
            if (string.IsNullOrEmpty(p_DiscordOrScoreSaberID))
            {
                await ReplyAsync($"> :x: Seems like you didn't used the command correctly, use: `{BotHandler.m_Prefix}scanban [DiscordOrScoreSaberID]`");
                return;
            }

            bool l_IsScoreSaberAccount = UserController.AccountExist(p_DiscordOrScoreSaberID, out _);

            if (UserController.UserExist(p_DiscordOrScoreSaberID))
            {
                p_DiscordOrScoreSaberID = UserController.GetPlayer(p_DiscordOrScoreSaberID);
            }
            else if (!UserController.UserExist(p_DiscordOrScoreSaberID) && !l_IsScoreSaberAccount)
            {
                await ReplyAsync("> :x: Sorry, this Discord User doesn't have any ScoreSaber account linked/isn't a correct ScoreSaberID.");
                return;
            }

            if (ulong.TryParse(p_DiscordOrScoreSaberID, out ulong l_ScoreSaberID))
            {
                EmbedBuilder l_EmbedBuilder = new EmbedBuilder().WithColor(Color.Purple);
                Player l_Player = new Player(p_DiscordOrScoreSaberID);
                l_Player.m_PlayerStats.IsScanBanned = !l_Player.m_PlayerStats.IsScanBanned;
                l_Player.ReWriteStats();
                if (l_Player.m_PlayerFull != null) await ReplyAsync($"> {l_Player.m_PlayerFull.name}'s TotalBan preference has been changed from **{!l_Player.m_PlayerStats.IsScanBanned}** to **{l_Player.m_PlayerStats.IsScanBanned}**");
            }
        }
    }
}
