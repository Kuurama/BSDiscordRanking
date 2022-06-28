using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("unlink")]
        [Summary("Unlinks a specific discord accounts from your ScoreSaber's one (also work the opposite ID way).")]
        public async Task UnLinkUser(string p_DiscordOrScoreSaberID = "")
        {
            if (!string.IsNullOrEmpty(p_DiscordOrScoreSaberID))
            {
                bool l_IsScoreSaberAccount = UserController.AccountExist(p_DiscordOrScoreSaberID, out _);

                if (l_IsScoreSaberAccount)
                {
                    if (!UserController.SSIsAlreadyLinked(p_DiscordOrScoreSaberID))
                        await ReplyAsync("> :x: Sorry, this Score Saber ID isn't registered on the Bot.");
                    else
                        p_DiscordOrScoreSaberID = UserController.GetDiscordID(p_DiscordOrScoreSaberID);
                }
                else if (!UserController.UserExist(p_DiscordOrScoreSaberID))
                {
                    if (UserController.IsLinkBanned(p_DiscordOrScoreSaberID))
                    {
                        UserController.RemovePlayer(p_DiscordOrScoreSaberID);
                        await ReplyAsync($"> :white_check_mark: This DiscordID link request had been reset to none!");
                        return;
                    }
                    await ReplyAsync("> :x: Sorry but this account isn't registered on the bot/wrong ScoreSaber ID, and as a result, can't be unlinked");
                    return;
                }

                UserController.RemovePlayer(p_DiscordOrScoreSaberID);
                await ReplyAsync($"> :white_check_mark: Player <@{p_DiscordOrScoreSaberID}> was successfully unlinked!");
            }
            else
            {
                if (string.IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())))
                {
                    await ReplyAsync($"> :x: Sorry, you don't have any account linked. Please use `{BotHandler.m_Prefix}link` instead.");
                }
                else
                {
                    UserController.RemovePlayer(Context.User.Id.ToString());
                    await ReplyAsync("> :white_check_mark: Your account was successfully unlinked!");
                }
            }
        }
    }
}
