using System;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Player;
using Discord.Commands;
using Discord.WebSocket;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("scan")]
        [Alias("dc")]
        [Summary("Scans a specific score saber account and add it to the database.")]
        public async Task Scan_Scores(string p_DiscordOrScoreSaberID)
        {
            bool l_IsDiscordLinked = false;
            string l_ScoreSaberOrDiscordName = "";
            string l_DiscordID = "";
            SocketGuildUser l_User = null;

            bool l_IsScoreSaberAccount = UserController.AccountExist(p_DiscordOrScoreSaberID);

            if (UserController.UserExist(p_DiscordOrScoreSaberID))
            {
                l_User = Context.Guild.GetUser(Convert.ToUInt64(p_DiscordOrScoreSaberID));
                p_DiscordOrScoreSaberID = UserController.GetPlayer(p_DiscordOrScoreSaberID);
                if (l_User != null)
                {
                    l_IsDiscordLinked = true;
                    l_DiscordID = l_User.Id.ToString();
                    l_ScoreSaberOrDiscordName = l_User.Username;
                }
                else
                {
                    await ReplyAsync("> :x: This Discord User isn't accessible yet.");
                    return;
                }
            }
            else if (l_IsScoreSaberAccount)
            {
                if (!UserController.SSIsAlreadyLinked(p_DiscordOrScoreSaberID))
                {
                    l_User = Context.Guild.GetUser(Convert.ToUInt64(UserController.GetDiscordID(p_DiscordOrScoreSaberID)));
                    if (l_User != null)
                    {
                        l_IsDiscordLinked = true;
                        l_DiscordID = l_User.Id.ToString();
                        l_ScoreSaberOrDiscordName = l_User.Username;
                    }
                }
            }
            else if (!UserController.UserExist(p_DiscordOrScoreSaberID))
            {
                await ReplyAsync("> :x: Sorry, this Discord User doesn't have any ScoreSaber account linked/isn't a correct ScoreSaberID.");
                return;
            }

            Player l_Player = new Player(p_DiscordOrScoreSaberID);
            if (!l_IsDiscordLinked) l_ScoreSaberOrDiscordName = l_Player.m_PlayerFull.name;

            l_Player.LoadPass();
            int l_OldPlayerLevel = l_Player.GetPlayerLevel(); /// By doing so, as a result => loadstats() inside too.

            bool l_FirsScan = l_Player.FetchScores(Context); /// FetchScore Return true if it's the first scan.
            Task<Player.NumberOfPassTypeFormat> l_FetchPass = l_Player.FetchPass(Context);
            if (l_FetchPass.Result.newPass >= 1 || l_FetchPass.Result.updatedPass >= 1)
            {
                if (l_IsDiscordLinked)
                {
                    if (l_FetchPass.Result.newPass >= 1 && l_FetchPass.Result.updatedPass < 1)
                        await ReplyAsync($"> 🎉 {l_ScoreSaberOrDiscordName} passed {l_FetchPass.Result.newPass} new maps!\n");
                    else if (l_FetchPass.Result.newPass < 1 && l_FetchPass.Result.updatedPass >= 1)
                        await ReplyAsync($"> 🎉 {l_ScoreSaberOrDiscordName} updated {l_FetchPass.Result.updatedPass} scores on maps!\n");
                    else
                        await ReplyAsync($"> 🎉 {l_ScoreSaberOrDiscordName} passed {l_FetchPass.Result.newPass} new maps, and updated their scores on {l_FetchPass.Result.updatedPass} maps!\n");
                }
            }
            else
            {
                if (l_FirsScan)
                    await ReplyAsync($"> Oh, it seems like {l_ScoreSaberOrDiscordName} didn't pass any maps from the pools.");
                else
                    await ReplyAsync($"> :x: Sorry but {l_ScoreSaberOrDiscordName} didn't pass/updated you score on any new maps.");
            }

            int l_NewPlayerLevel = l_Player.GetPlayerLevel();
            if (l_OldPlayerLevel != l_NewPlayerLevel)
            {
                if (l_OldPlayerLevel < l_NewPlayerLevel)
                    if (l_IsDiscordLinked)
                        await ReplyAsync($"> <:Stonks:884058036371595294> GG! <@{l_DiscordID}>, You are now Level {l_NewPlayerLevel}.\n> To see your new pool, try the `{ConfigController.GetConfig().CommandPrefix[0]}ggp` command.");
                    else
                        await ReplyAsync($"> <:Stonks:884058036371595294> {l_ScoreSaberOrDiscordName} is now Level {l_NewPlayerLevel}.\n");
                else if (l_IsDiscordLinked)
                    await ReplyAsync($"> <:NotStonks:884057234886238208> <@{l_DiscordID}>, You lost levels. You are now Level {l_NewPlayerLevel}");
                else
                    await ReplyAsync($"> <:NotStonks:884057234886238208> {l_ScoreSaberOrDiscordName} lost levels. they are now Level {l_NewPlayerLevel}");
                if (l_IsDiscordLinked)
                {
                    await ReplyAsync($"> :clock1: The bot will now update {l_ScoreSaberOrDiscordName}'s roles. This step can take a while. `(The bot should now be responsive again)`");
                    Task l_RoleUpdate = UserController.UpdateRoleAndSendMessage(Context, l_User.Id, l_NewPlayerLevel);
                }
            }

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
            /// This will Update the leaderboard (the ManagePlayer, then depending on the Player's decision, ping them for snipe///////////////////////////

            await PassLeaderboardController.SendSnipeMessage(Context, new PassLeaderboardController().ManagePlayer(l_Player.m_PlayerFull.name, l_Player.GetPlayerID(), l_Player.m_PlayerStats.PassPoints, l_NewPlayerLevel, l_TotalTrophy, false)); /// Manage the PassLeaderboard
            await AccLeaderboardController.SendSnipeMessage(Context, new AccLeaderboardController().ManagePlayer(l_Player.m_PlayerFull.name, l_Player.GetPlayerID(), l_Player.m_PlayerStats.AccPoints, l_NewPlayerLevel, l_TotalTrophy, false)); /// Manage the PassLeaderboard

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            if (!l_IsDiscordLinked)
                await ReplyAsync($"> :white_check_mark: {l_ScoreSaberOrDiscordName}'s info added/updated");
        }
    }
}