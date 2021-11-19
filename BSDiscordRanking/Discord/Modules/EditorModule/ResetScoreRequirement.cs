using System.Threading.Tasks;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.EditorModule
{
    [PermissionHandler.RequirePermissionAttribute(Permission)]
    public partial class EditorModule : ModuleBase<SocketCommandContext>
    {
        [Command("resetscorerequirement")]
        [Summary("Sets all maps's score requirement from a level to 0.")]
        public async Task ResetScoreRequirement(int p_Level)
        {
            if (p_Level >= 0)
            {
                new Level(p_Level).ResetScoreRequirement();
                await ReplyAsync($"> :white_check_mark: All maps in playlist {p_Level} have now a score requirement of 0");
            }
        }
    }
}