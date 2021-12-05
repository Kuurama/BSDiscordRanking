using Discord.Commands;

// ReSharper disable once CheckNamespace
namespace BSDiscordRanking.Discord.Modules.ScoringTeamModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class ScoringTeamModule : ModuleBase<SocketCommandContext>
    {
        private const int PERMISSION = 2;
    }
}