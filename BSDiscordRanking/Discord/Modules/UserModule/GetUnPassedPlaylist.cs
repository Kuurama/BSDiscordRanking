using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Level;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    [CheckChannel]
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("getunpassedplaylist")]
        [Alias("gupl")]
        [Summary("Sends Playlist only containing the maps you didn't pass. Use `all` instead of the level id to get the whole level folder.")]
        public async Task GetUnpassedPlaylist(string p_Level)
        {
            if (!UserController.UserExist(Context.User.Id.ToString()))
                await Context.Channel.SendMessageAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link <ScoreSaber link/id>` instead.\n> (Or to get started with the bot: use the `{BotHandler.m_Prefix}getstarted` command)");
            else
            {
                const string ORIGINAL_PATH = "./PersonalLevels/";
                if (!Directory.Exists(ORIGINAL_PATH))
                {
                    try
                    {
                        Directory.CreateDirectory(ORIGINAL_PATH);
                    }
                    catch
                    {
                        Console.WriteLine($"Exception Occured creating directory : {ORIGINAL_PATH}");
                        return;
                    }
                }

                Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
                l_Player.LoadPass();
                string l_PlayerName = l_Player.m_PlayerFull.name;
                string l_FileName = "Unpassed_"+ RemoveSpecialCharacters(l_PlayerName);
                string l_Path = ORIGINAL_PATH + l_FileName + "/";
                if (!Directory.Exists(l_Path))
                {
                    try
                    {
                        Directory.CreateDirectory(l_Path);
                    }
                    catch
                    {
                        Console.WriteLine($"Exception Occured creating directory : {l_Path}");
                        return;
                    }
                }

                if (int.TryParse(p_Level, out _))
                {
                    int l_LevelInt = int.Parse(p_Level);
                    if (LevelController.GetLevelControllerCache().LevelID.Contains(int.Parse(p_Level)))
                    {
                        string l_PlaylistName = $"{l_FileName}_{l_LevelInt:D3}{Level.SUFFIX_NAME}";
                        string l_PathFile = l_Path + l_PlaylistName;

                        if (File.Exists(l_PathFile)) /// Mean there is already a personnal playlist file.
                            File.Delete(l_PathFile);

                        Level l_Level = new Level(l_LevelInt);
                        LevelFormat l_LevelFormat = RemovePassFromPlaylist(l_Player.ReturnPass(), l_Level.m_Level);
                        
                        if (l_LevelFormat.songs.Count > 0) /// Only create the file if it's not empty.
                        {
                            JsonDataBaseController.CreateDirectory(l_Path);
                            Level.ReWriteStaticPlaylist(l_LevelFormat, l_Path, l_PlaylistName); /// Write the personal playlist file in the PATH folder.
                        }
                        
                        if (File.Exists($"{l_PathFile}{Level.EXTENSION}"))
                            await Context.Channel.SendFileAsync($"{l_PathFile}{Level.EXTENSION}", $"> :white_check_mark: Here's your personal playlist! <@{Context.User.Id.ToString()}>");
                        else
                            await ReplyAsync("> :x: Sorry but you already passed all the maps in that playlist.");

                        DeleteUnpassedPlaylist(ORIGINAL_PATH, l_FileName);
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync("> :x: This level does not exist.");
                    }
                }
                else if (p_Level == "all")
                {
                    foreach (var l_LevelID in LevelController.GetLevelControllerCache().LevelID)
                    {
                        string l_PlaylistName = $"{l_FileName}_{l_LevelID:D3}{Level.SUFFIX_NAME}";
                        string l_PathFile = l_Path + l_PlaylistName;
                        
                        Level l_Level = new Level(l_LevelID);
                        LevelFormat l_LevelFormat = RemovePassFromPlaylist(l_Player.ReturnPass(), l_Level.m_Level);
                        
                        if (l_LevelFormat.songs.Count > 0) /// Only create the file if it's not empty.
                        {
                            JsonDataBaseController.CreateDirectory(l_Path);
                            Level.ReWriteStaticPlaylist(l_LevelFormat, l_Path, l_PlaylistName); /// Write the personal playlist file in the PATH folder.
                        }
                    }

                    try
                    {
                        if (Directory.GetFiles(l_Path, "*", SearchOption.AllDirectories).Length > 0)
                        {
                            ZipFile.CreateFromDirectory(l_Path, $"{ORIGINAL_PATH}{l_FileName}.zip");
                            await Context.Channel.SendFileAsync($"{ORIGINAL_PATH}{l_FileName}.zip", $"> :white_check_mark: Here's your personal playlist folder! <@{Context.User.Id.ToString()}>");
                        }
                        else
                        {
                            await ReplyAsync("Sorry but you already passed all the maps from all pools, good job!");
                        }

                        DeleteUnpassedPlaylist(ORIGINAL_PATH, l_FileName);
                    }
                    catch
                    {
                        await Context.Channel.SendMessageAsync("> :x: Seems like you forgot to add Levels. Unless you want an empty zip file?");
                    }
                }
                else
                    await Context.Channel.SendMessageAsync("> :x: Wrong argument, please use \"1,2,3..\" or \"all\"");
            }
        }
    }
}