using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Discord;
using BSDiscordRanking.Discord.Modules;
using BSDiscordRanking.Formats.API;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BSDiscordRanking.Controllers
{
    public static class LinkVerificationController
    {
        public static void SendVerificationEmbed(SocketCommandContext p_Context, ApiPlayer p_PlayerFull)
        {
            if (p_Context is null) return;

            /// Ew but i need ya know
            Program.m_TempGlobalGuildID = p_Context.Guild.Id;

            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
            l_EmbedBuilder.WithTitle($"Link Verification: {p_Context.User.Username} => ScoreSaber: {p_PlayerFull.name}?");
            l_EmbedBuilder.WithUrl($"https://scoresaber.com/u/{p_PlayerFull.id}");
            l_EmbedBuilder.WithThumbnailUrl(p_PlayerFull.profilePicture);
            l_EmbedBuilder.WithColor(Color.Gold);
            l_EmbedBuilder.WithDescription("Please make sure the linked account is corresponding to the right player before confirming (click the title to lookup the score saber url).");
            l_EmbedBuilder.AddField("Score Saber Rank", ":earth_africa: #" + p_PlayerFull.rank, true);
            l_EmbedBuilder.AddField("PP", $"{p_PlayerFull.pp}");
            l_EmbedBuilder.WithTimestamp(DateTimeOffset.Now);
            p_Context.Guild.GetTextChannel(ConfigController.GetConfig().LinkVerificationChannel).SendMessageAsync("", false, l_EmbedBuilder.Build(), components: new ComponentBuilder().WithButton(new ButtonBuilder("Accept", $"Accept_{p_Context.User.Id}", ButtonStyle.Success)).WithButton("Deny", $"Deny_{p_Context.User.Id}", ButtonStyle.Danger).Build());
        }

        public static async Task LinkVerificationButtonHandler(SocketMessageComponent p_MessageComponent)
        {
            Embed l_MessageEmbed = p_MessageComponent.Message.Embeds.FirstOrDefault();
            if (l_MessageEmbed is null) return;

            if (l_MessageEmbed.Title.Contains("Link Verification") == false) return;
            string[] l_SplicedCustomID = p_MessageComponent.Data.CustomId.Split("_");

            SocketGuildUser l_User = p_MessageComponent.User as SocketGuildUser;
            List<int> l_PermLevels = PermissionHandler.GetUserPermLevel(l_User);

            if (l_PermLevels.Exists(p_X => p_X != 0) == false) return;

            if (p_MessageComponent.IsValidToken && !p_MessageComponent.HasResponded) await p_MessageComponent.DeferAsync();
            EmbedBuilder l_EmbedBuilder = p_MessageComponent.Message.Embeds.FirstOrDefault().ToEmbedBuilder();

            switch (l_SplicedCustomID[0])
            {
                case "Accept":
                    if (UserController.AcceptVerifyPlayer(l_SplicedCustomID[1]))
                    {
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder
                            .WithDescription("")
                            .WithColor(Color.Green)
                            .WithTimestamp(DateTimeOffset.Now)
                            .AddField("\u200B", "\u200B")
                            .AddField("Accepted", $"By <@{p_MessageComponent.User.Id}>").Build());
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = new ComponentBuilder().Build());
                        await BotHandler.m_Client.GetGuild(Program.m_TempGlobalGuildID).GetTextChannel(ConfigController.GetConfig().AuthorizedChannels.FirstOrDefault()).SendMessageAsync($"> <@{l_SplicedCustomID[1]}>, your bot link request has been **accepted** by \"{p_MessageComponent.User.Username}\", use `{BotHandler.m_Prefix}getstarted` to learn how to use the bot, and do `{BotHandler.m_Prefix}scan` to scan your latest passes.");
                    }
                    else
                    {
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder
                            .WithDescription("")
                            .WithColor(Color.Red)
                            .WithTimestamp(DateTimeOffset.Now)
                            .AddField("\u200B", "\u200B")
                            .AddField("ERROR, Something went wrong..", $"By <@{p_MessageComponent.User.Id}>").Build());
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = new ComponentBuilder().Build());
                    }
                    break;
                case "Deny":
                    await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder
                        .WithDescription("")
                        .WithColor(Color.Red)
                        .WithTimestamp(DateTimeOffset.Now)
                        .AddField("\u200B", "\u200B")
                        .AddField("Refused", $"By <@{p_MessageComponent.User.Id}>").Build());
                    await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = new ComponentBuilder().Build());
                    await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = new ComponentBuilder().Build());
                    await BotHandler.m_Client.GetGuild(Program.m_TempGlobalGuildID).GetTextChannel(ConfigController.GetConfig().AuthorizedChannels.FirstOrDefault()).SendMessageAsync($"> <@{l_SplicedCustomID[1]}>, your bot link request has been **denied** by \"{p_MessageComponent.User.Username}\",\nthere might be some mandatory requirements to be registered, such as being high enough rank or linking to the correct account.\n You can still make an appeal to the moderators if needed.");
                    break;
            }
        }
    }
}
