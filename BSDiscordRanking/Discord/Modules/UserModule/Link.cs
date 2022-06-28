using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.API;
using Discord.Commands;
using static System.String;


namespace BSDiscordRanking.Discord.Modules.UserModule
{
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("link")]
        [Alias("register")]
        [Summary("Links your ScoreSaber account to your Discord's one.")]
        public async Task LinkUser(string p_ScoreSaberLink = "")
        {
            if (!IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())))
            {
                await ReplyAsync(
                    $"> :x: Sorry, but your account already has been linked. If you want to unlink it: Please use `{BotHandler.m_Prefix}unlink`.");
            }
            else if (!IsNullOrEmpty(p_ScoreSaberLink))
            {
                p_ScoreSaberLink = Regex.Match(p_ScoreSaberLink, @"\d+").Value;
                if (IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())) && UserController.AccountExist(p_ScoreSaberLink, out ApiPlayer l_PlayerFull) && !UserController.SSIsAlreadyLinked(p_ScoreSaberLink))
                {
                    if (ConfigController.m_ConfigFormat.EnableLinkVerificationSystem)
                    {
                        if (ConfigController.m_ConfigFormat.LinkVerificationChannel != 0)
                        {
                            if (UserController.IsLinkBanned(Context.User.Id.ToString()))
                            {
                                await ReplyAsync("> :x: Sorry, but you are Banned from Linking through the bot.\nYou can still make an appeal to the moderators if needed, they will have to manually link your DiscordID to your ScoreSaberID.");
                                return;
                            }

                            if (!UserController.IsPendingVerification(Context.User.Id.ToString()))
                            {
                                bool l_IsLinkDenied = UserController.IsLinkDenied(Context.User.Id.ToString());
                                /// Make sure player don't duplicate.
                                UserController.RemovePlayer(Context.User.Id.ToString());
                                UserController.AddPlayerNeedVerification(Context.User.Id.ToString(), p_ScoreSaberLink);
                                LinkVerificationController.SendVerificationEmbed(Context, l_PlayerFull, l_IsLinkDenied);
                                await ReplyAsync($"> Your link request had been *submitted for verification*, you will be *notified* once approved or denied.");
                            }
                            else
                            {
                                await ReplyAsync("> :x: Sorry, but you are already pending verification.");
                            }
                        }
                        else
                        {
                            await ReplyAsync("> :x: Sorry, but the link verification channel isn't set. Please set a Link verification channel before attempting to link.");
                        }
                    }
                    else
                    {
                        UserController.AddPlayer(Context.User.Id.ToString(), p_ScoreSaberLink);
                        await ReplyAsync($"> :white_check_mark: Your account has been successfully linked.\nLittle tip: use `{BotHandler.m_Prefix}scan` to scan your latest passes!");
                    }
                }
                else if (!UserController.AccountExist(p_ScoreSaberLink, out _))
                {
                    await ReplyAsync("> :x: Sorry, but please enter a correct ScoreSaber Link/ID.");
                }
                else if (UserController.SSIsAlreadyLinked(p_ScoreSaberLink))
                {
                    await ReplyAsync(
                        "> :x: Sorry but this account is already linked to an other user.\nIf you entered the correct id and you didn't linked it on an other discord account\nPlease Contact an administrator.");
                }
                else
                {
                    await ReplyAsync("> :x: Oopsie, unhandled error.");
                }
            }
            else
            {
                await ReplyAsync("> :x: Please enter a ScoreSaber link/id.");
            }
        }
    }
}
