﻿using Discord.Commands;

// ReSharper disable once CheckNamespace
namespace BSDiscordRanking.Discord.Modules.EditorModule
{
    [PermissionHandler.RequirePermissionAttribute(Permission)]
    public partial class EditorModule : ModuleBase<SocketCommandContext>
    {
        public const int Permission = 1;
    }
}