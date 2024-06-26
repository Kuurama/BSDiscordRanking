﻿using System;
using System.Linq;
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
        [Command("setlevel")]
        [Summary("Set a specific Level to someone + change his stats/roles and leaderboard's level.")]
        public async Task SetLevelRole(string p_DiscordOrScoreSaberID, int p_Level)
        {
            bool l_IsScoreSaberAccount = UserController.AccountExist(p_DiscordOrScoreSaberID, out _);

            string l_DiscordID = null;
            if (UserController.UserExist(p_DiscordOrScoreSaberID))
            {
                l_DiscordID = p_DiscordOrScoreSaberID;
                p_DiscordOrScoreSaberID = UserController.GetPlayer(p_DiscordOrScoreSaberID);
            }
            else if (!UserController.UserExist(p_DiscordOrScoreSaberID) && !l_IsScoreSaberAccount)
            {
                await ReplyAsync("> :x: Sorry, this Discord User don't have any ScoreSaber account linked/isn't a correct ScoreSaberID.");
                return;
            }

            /// Else => is a correct score saber ID

            Player l_Player = new Player(p_DiscordOrScoreSaberID);
            if (l_Player.m_PlayerStats.IsFirstScan)
            {
                await ReplyAsync("> :x: Sorry, but this ScoreSaber account isn't registered on the bot yet, !scan it first.");
                return;
            }

            if (l_Player.GetPlayerLevel() == p_Level) await ReplyAsync($"> This player is already Level {p_Level} but we will still perform a role check/update.\n");

            foreach (int l_LevelID in  LevelController.GetLevelControllerCache().LevelID)
            {
                if (l_LevelID <= p_Level)
                {
                    int l_Index = l_Player.m_PlayerStats.Levels.FindIndex(p_X => p_X.LevelID == l_LevelID);
                    if (l_Index < 0)
                        l_Player.m_PlayerStats.Levels.Add(new PassedLevel
                        {
                            LevelID = l_LevelID,
                            Passed = true,
                            Trophy = new Trophy
                            {
                                Plastic = 0,
                                Silver = 0,
                                Gold = 0,
                                Diamond = 0,
                                Ruby = 0
                            },
                            TotalNumberOfMaps = new Level(l_LevelID).m_Level.songs.SelectMany(p_X => p_X.difficulties).Count()
                        });
                    else
                        l_Player.m_PlayerStats.Levels[l_Index].Passed = true;
                }
                else
                {
                    int l_Index = l_Player.m_PlayerStats.Levels.FindIndex(p_X => p_X.LevelID == l_LevelID);
                    if (l_Index < 0)
                        l_Player.m_PlayerStats.Levels.Add(new PassedLevel
                        {
                            LevelID = l_LevelID,
                            Passed = false,
                            Trophy = new Trophy
                            {
                                Plastic = 0,
                                Silver = 0,
                                Gold = 0,
                                Diamond = 0,
                                Ruby = 0
                            },
                            TotalNumberOfMaps = new Level(l_LevelID).m_Level.songs.SelectMany(p_X => p_X.difficulties).Count()
                        });
                    else
                        l_Player.m_PlayerStats.Levels[l_Index].Passed = false;
                }
            }

            l_Player.ReWriteStats();
            new PassLeaderboardController().ManagePlayer(l_Player.m_PlayerFull.name, p_DiscordOrScoreSaberID, -1, p_Level, null, false);
            if (l_DiscordID != null)
            {
                SocketGuildUser l_MyUser = Context.Guild.GetUser(Convert.ToUInt64(l_DiscordID));
                if (l_MyUser != null)
                {
                    await ReplyAsync($"> :clock1: The bot will now update {l_MyUser.Username}'s roles. This step can take a while. `(The bot should now be responsive again)`");
                    Task l_RoleUpdate = UserController.UpdateRoleAndSendMessage(Context, l_MyUser.Id, p_Level);
                }

                else
                {
                    await ReplyAsync("> :x: This Discord User isn't accessible yet.");
                }
            }
            else
            {
                await ReplyAsync($"{l_Player.m_PlayerFull.name}'s Level set to Level {p_Level}");
            }
        }
    }
}
