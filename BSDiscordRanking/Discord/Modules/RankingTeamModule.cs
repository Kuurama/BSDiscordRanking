using Discord.Commands;

// ReSharper disable once CheckNamespace
namespace BSDiscordRanking.Discord.Modules.RankingTeamModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class RankingTeamModule : ModuleBase<SocketCommandContext>
    {
        private const int PERMISSION = 1;
    }
}