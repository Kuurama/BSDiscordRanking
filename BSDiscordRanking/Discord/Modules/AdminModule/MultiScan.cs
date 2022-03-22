using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats;
using BSDiscordRanking.Formats.Player;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("multiscan")]
        [Summary("Scans a specific number of player.")]
        public async Task MultiScan(int p_NumberOfUser = int.MinValue, bool p_DownloadScore = false)
        {
            int l_NumberOfPlayer = 0;
            foreach (UserFormat l_User in UserController.m_Users)
            {
                if (l_NumberOfPlayer < p_NumberOfUser || (p_NumberOfUser == int.MinValue))
                {
                    Player l_Player = new Player(l_User.ScoreSaberID);
                    l_Player.LoadPass();
                    if (p_DownloadScore)
                    {
                        l_Player.FetchScores();
                    }
                    await l_Player.FetchPass();
                    l_NumberOfPlayer++;
                    
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

                    int l_NewPlayerLevel = l_Player.GetPlayerLevel();
                    new PassLeaderboardController().ManagePlayer(l_Player.m_PlayerFull.name, l_Player.GetPlayerID(), l_Player.m_PlayerStats.PassPoints, l_NewPlayerLevel, l_TotalTrophy, false);
                    new AccLeaderboardController().ManagePlayer(l_Player.m_PlayerFull.name, l_Player.GetPlayerID(), l_Player.m_PlayerStats.AccPoints, l_NewPlayerLevel, l_TotalTrophy, false);
                }
            }

            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
            l_EmbedBuilder.WithTitle($"MultiScan");
            l_EmbedBuilder.AddField("Fetched", $"{l_NumberOfPlayer} Player(s)");
            
            await Context.Channel.SendMessageAsync(null, false, l_EmbedBuilder.Build());
        }
    }
}