using System.Threading.Tasks;
using BSDiscordRanking.API;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.RankingTeamModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class RankingTeamModule : ModuleBase<SocketCommandContext>
    {
        [Command("refreshmapcache")]
        [Summary("Refresh the Map Cache used by the Api.")]
        public async Task RefreshMapCache()
        {
            await ReplyAsync("> Doing..");
            Program.s_MapLeaderboardCache.Clear();
            WebApp.LoadMapLeaderboardCache();
            await ReplyAsync("> Done!");
        }
    }
}
