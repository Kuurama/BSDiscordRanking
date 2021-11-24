using Discord.Commands;

// ReSharper disable once CheckNamespace
namespace BSDiscordRanking.Discord.Modules.RankingTeamModule
{
    [PermissionHandler.RequirePermissionAttribute(Permission)]
    public partial class RankingTeamModule : ModuleBase<SocketCommandContext>
    {
        public const int Permission = 1;
    }
}