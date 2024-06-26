﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("botinfo")]
        [Alias("botstats")]
        [Summary("Shows information about the bot E.G: Uptime or ScoreCount.")]
        public async Task BotInfo()
        {
            TimeSpan l_Uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;
            int l_ScoreCount = 0;

            foreach (UserFormat l_User in UserController.m_Users)
            {
                Player l_Player = new Player(l_User.ScoreSaberID);
                l_Player.LoadPass();
                l_ScoreCount += l_Player.m_PlayerPass.SongList.Count;
            }

            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
            l_EmbedBuilder.WithTitle("BotInfos");
            l_EmbedBuilder.AddField("Uptime", $"{l_Uptime.Days}d, {l_Uptime.Hours}h, {l_Uptime.Minutes}m");
            l_EmbedBuilder.AddField("Player count", $"{UserController.m_Users.Count} players");
            l_EmbedBuilder.AddField("Pass count", $"{l_ScoreCount} total scores");
            await Context.Channel.SendMessageAsync(null, false, l_EmbedBuilder.Build());
        }
    }
}