using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Controller;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BSDiscordRanking.Discord.Modules.EditorModule
{
    [PermissionHandler.RequirePermissionAttribute(Permission)]
    public partial class EditorModule : ModuleBase<SocketCommandContext>
    {
        [Command("editmap")]
        [Alias("rankedit")]
        [Summary("Open the Edit-Map management menu.")]
        public async Task EditMap(string p_BSRCode = "", string p_DifficultyName = "ExpertPlus", string p_Characteristic = "Standard")
        {
            if (string.IsNullOrEmpty(p_BSRCode))
            {
                await ReplyAsync($"> :x: Seems like you didn't used the command correctly, use: `{BotHandler.m_Prefix}editmap [level] [key] [ExpertPlus/Hard..] (Standard/Lawless..)`");
            }
            else
            {
                await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder().WithTitle("Welcome to the Edit-Map management menu.").Build(),component: new ComponentBuilder()
                    .WithButton(new ButtonBuilder("Change Map Level", "LevelIDChange"))
                    .Build());
                BotHandler.m_Client.ButtonExecuted += LevelEditButtonHandler;
            }
        }

        public async Task LevelEditButtonHandler(SocketMessageComponent p_MessageComponent)
        {
            switch (p_MessageComponent.Data.CustomId)
            {
                case "LevelIDChange":
                    List<SelectMenuOptionBuilder> l_SelectMenuOptionBuilders = new List<SelectMenuOptionBuilder>();
                    List<List<SelectMenuOptionBuilder>> l_ListListMenuOptionBuilder = new List<List<SelectMenuOptionBuilder>>(){new List<SelectMenuOptionBuilder>()};
                    ComponentBuilder l_ComponentBuilder = new ComponentBuilder();
                    LevelControllerFormat l_LevelControllerFormat = LevelController.FetchAndGetLevel();
                    LevelController.ReWriteController(l_LevelControllerFormat);

                    foreach (var l_LevelID in l_LevelControllerFormat.LevelID)
                    {
                        l_SelectMenuOptionBuilders.Add(new SelectMenuOptionBuilder($"Level {l_LevelID}", l_LevelID.ToString()));
                    }

                    int l_Index = 0;
                    int l_ListListMenuIndex = 0;
                    foreach (var l_SelectMenuOptionBuilder in l_SelectMenuOptionBuilders)
                    {
                        
                        if (l_Index > 23)
                        {
                            l_ListListMenuOptionBuilder.Add(new List<SelectMenuOptionBuilder>()); 
                            l_ListListMenuIndex++;
                            l_Index = 0;
                        }
                        l_ListListMenuOptionBuilder[l_ListListMenuIndex].Add(l_SelectMenuOptionBuilder);
                        l_Index++;
                    }

                    l_Index = 0;
                    foreach (var l_ListMenuOptionBuilder in l_ListListMenuOptionBuilder)
                    {
                        l_ComponentBuilder.WithSelectMenu($"SelectLevelMenu_{l_Index}", options:l_ListMenuOptionBuilder);
                        l_Index++;
                    }
                    await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = new EmbedBuilder().WithTitle("Level-Edit").WithDescription("Please choose the level you want this difficulty to be in.").Build());
                    await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = l_ComponentBuilder.Build());
                    BotHandler.m_Client.InteractionCreated += LevelEditSelect;
                    break;
            }
        }

        public async Task LevelEditSelect(SocketInteraction p_Interaction)
        {
            switch (p_Interaction.Type)
            {
                case InteractionType.MessageComponent:
                    SocketMessageComponent l_Interaction = (SocketMessageComponent)p_Interaction;
                    if (l_Interaction.Data.CustomId.Contains("SelectLevelMenu"))
                    {
                        string l_NewLevelID = l_Interaction.Data.Values.FirstOrDefault();
                        await l_Interaction.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = new EmbedBuilder().WithTitle("Level-Edit Done").WithDescription($"You choice have been registered: Level {l_NewLevelID}.").Build());
                        await l_Interaction.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = new ComponentBuilder().Build());
                    }
                    break;
            }
        }
    }
}