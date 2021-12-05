using System.Threading.Tasks;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("profile")]
        [Alias("stats")]
        [Summary("Sends your profile's informations (Level, Passes, Trophies etc).")]
        public async Task Profile()
        {
            await SendProfile(Context.User.Id.ToString(), false);
        }

        [Command("profile")]
        [Alias("stats")]
        [Summary("Sends someone else profile's informations (Level, Passes, Trophies etc).")]
        public async Task Profile(string p_DiscordOrScoreSaberID)
        {
            await SendProfile(p_DiscordOrScoreSaberID, true);
        }
    }
}