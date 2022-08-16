using System;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Level;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("resetautoweight")]
        [Summary("Reset every maps autoweight value (trigger a reweight).")]
        public async Task ResetAutoWeight()
        {
            await ReplyAsync("Doing the stuff..");
            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();

            int l_Count = 0;
            foreach (int l_LevelID in LevelController.GetLevelControllerCache().LevelID)
            {
                Level l_Level = new Level(l_LevelID);
                foreach (SongFormat l_Song in l_Level.m_Level.songs)
                {
                    foreach (Difficulty l_Difficulty in l_Song.difficulties)
                    {
                        try
                        {
                            if (l_Difficulty.customData.leaderboardID == 0) continue;

                            l_Difficulty.customData.AutoWeight = MapLeaderboardController.RecalculateAutoWeight(l_Difficulty.customData.leaderboardID, l_Level.m_Level.customData.autoWeightDifficultyMultiplier, true);
                            l_Count++;
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }

                l_Level.ReWritePlaylist(false);
            }

            l_EmbedBuilder.WithTitle("Auto Weight Reset");
            l_EmbedBuilder.WithDescription($"The autoweight has been reset on {l_Count} maps.");
            await ReplyAsync(embed: l_EmbedBuilder.Build());
        }

        [Command("copyweighttoautoweight")]
        [Summary("yeye you know what it is doing.")]
        public async Task CopyWeightToAutoWeight()
        {
            await ReplyAsync("Doing the stuff..");
            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();

            int l_Count = 0;
            foreach (int l_LevelID in LevelController.GetLevelControllerCache().LevelID)
            {
                Level l_Level = new Level(l_LevelID);
                l_Level.m_Level.customData.autoWeightDifficultyMultiplier = l_Level.m_Level.customData.weighting;
                l_Level.ReWritePlaylist(false);
                l_Count++;
            }

            l_EmbedBuilder.WithTitle("Auto Weight multiplier copy");
            l_EmbedBuilder.WithDescription($"The autoweight difficulty multiplier has been copied on {l_Count} levels.");
            await ReplyAsync(embed: l_EmbedBuilder.Build());
        }
    }
}
