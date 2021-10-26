using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    [CheckChannel]
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {

        [Command("getplaylist")]
        [Alias("gpl")]
        [Summary("Sends the desired Level's playlist file. Use `all` instead of the level id to get the whole level folder.")]
        public async Task GetPlaylist(string p_Level)
        {
            if (int.TryParse(p_Level, out _))
            {
                int l_LevelInt = int.Parse(p_Level);
                string l_Path = Level.GetPath() + $"{l_LevelInt:D3}{Level.SUFFIX_NAME}.bplist";
                if (File.Exists(l_Path))

                    await Context.Channel.SendFileAsync(l_Path, "> :white_check_mark: Here's the complete playlist! (up to date)");
                else

                    await Context.Channel.SendMessageAsync("> :x: This level does not exist.");
            }
            else if (p_Level == "all")
            {
                if (File.Exists("levels.zip"))
                    File.Delete("levels.zip");
                try
                {
                    ZipFile.CreateFromDirectory(Level.GetPath(), "levels.zip");
                    await Context.Channel.SendFileAsync("levels.zip", "> :white_check_mark: Here's a playlist folder containing all the playlist's pools! (up to date)");
                }
                catch
                {
                    await ReplyAsync("> :x: Seems like you forgot to add Levels. Unless you want an empty zip file?");
                }
            }
            else
                await ReplyAsync("> :x: Wrong argument, please use \"1,2,3..\" or \"all\"");
        }
    }
}