using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Level;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("getunpassedplaylist")]
        [Alias("gupl")]
        [Summary("Sends Playlist only containing the maps you didn't pass, it can also sort by Category if you type it. Use `all` instead of the level id to get the whole level folder.")]
        public async Task GetUnpassedPlaylist(string p_Level, [Remainder] string p_Category = null)
        {
            if (!UserController.UserExist(Context.User.Id.ToString()))
            {
                await Context.Channel.SendMessageAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link <ScoreSaber link/id>` instead.\n> (Or to get started with the bot: use the `{BotHandler.m_Prefix}getstarted` command)");
            }
            else
            {
                p_Category = FirstCharacterToUpper(p_Category);
                const string ORIGINAL_PATH = "./PersonalLevels/";
                if (!Directory.Exists(ORIGINAL_PATH))
                    try
                    {
                        Directory.CreateDirectory(ORIGINAL_PATH);
                    }
                    catch
                    {
                        Console.WriteLine($"Exception Occured creating directory : {ORIGINAL_PATH}");
                        return;
                    }

                Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
                l_Player.LoadPass();
                string l_PlayerName = l_Player.m_PlayerFull.name;
                string l_FileName;
                string l_Path;
                if (p_Category != null)
                {
                    l_FileName = $"Unpassed_{RemoveSpecialCharacters(p_Category)}_{RemoveSpecialCharacters(l_PlayerName)}";
                    l_Path = ORIGINAL_PATH + l_FileName + "/";
                }
                else
                {
                    l_FileName = "Unpassed_" + RemoveSpecialCharacters(l_PlayerName);
                    l_Path = ORIGINAL_PATH + l_FileName + "/";
                }

                if (!Directory.Exists(l_Path))
                    try
                    {
                        Directory.CreateDirectory(l_Path);
                    }
                    catch
                    {
                        Console.WriteLine($"Exception Occured creating directory : {l_Path}");
                        return;
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
                        LevelFormat l_LevelFormat;
                        if (p_Category != null)
                        {
                            RemoveCategoriesFormat l_RemoveCategoriesFormat = RemoveOtherCategoriesFromPlaylist(l_Level.m_Level, p_Category);
                            if (!l_RemoveCategoriesFormat.LevelFormat.songs.Any())
                            {
                                string l_Message = $":x: Sorry but there isn't any categories (stored in your stats) called `{p_Category}` in Level {p_Level}, here is a list of all the available categories:";
                                foreach (string l_Category in l_RemoveCategoriesFormat.Categories) if (l_Category != null) if (l_Category != "") l_Message += $"\n> {l_Category}";
                                    

                                if (l_Message.Length <= 1980)
                                    await ReplyAsync(l_Message);
                                else
                                    await ReplyAsync($"> :x: Sorry but there isn't any categories (stored in your stats) called `{p_Category}` in Level {p_Level},\n+ there is too many categories in that level to send all of them in one message.");
                                
                                DeletePlaylistZip(ORIGINAL_PATH, l_FileName);
                                return;
                            }
                            
                            l_LevelFormat = RemovePassFromPlaylist(l_Player.ReturnPass(), l_RemoveCategoriesFormat.LevelFormat);
                            
                        }
                        else
                        {
                            l_LevelFormat = RemovePassFromPlaylist(l_Player.ReturnPass(), l_Level.m_Level);
                        }


                        if ((l_LevelFormat.songs.SelectMany(p_X => p_X.difficulties).Count() == l_Level.m_Level.songs.SelectMany(p_X => p_X.difficulties).Count()) && p_Category == null)
                        {
                            await ReplyAsync($"> It seems like you don't have any pass on this level, i guess you wanted to use the `{BotHandler.m_Prefix}getplaylist {l_LevelInt}` command, here is it:");
                            await GetPlaylist(p_Level);
                            DeletePlaylistZip(ORIGINAL_PATH, l_FileName);
                            return;
                        }

                        if (l_LevelFormat.songs.Any()) /// Only create the file if it's not empty.
                        {
                            JsonDataBaseController.CreateDirectory(l_Path);
                            Level.ReWriteStaticPlaylist(l_LevelFormat, l_Path, l_PlaylistName); /// Write the personal playlist file in the PATH folder.
                        }

                        if (File.Exists($"{l_PathFile}{Level.EXTENSION}"))
                            await Context.Channel.SendFileAsync($"{l_PathFile}{Level.EXTENSION}", $"> :white_check_mark: Here's your personal playlist! <@{Context.User.Id.ToString()}>");
                        else
                        {
                            if (p_Category != null)
                            {
                                await ReplyAsync($"> :x: Sorry but you already passed all the `{p_Category}` maps in that playlist.");
                            }
                            else
                            {
                                await ReplyAsync("> :x: Sorry but you already passed all the maps in that playlist.");
                            }
                        }

                        DeletePlaylistZip(ORIGINAL_PATH, l_FileName);
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync("> :x: This level does not exist.");
                    }
                }
                else if (p_Level == "all")
                {
                    List<string> l_AvailableCategories = new List<string>();
                    bool l_CategoryExist = false;
                    foreach (int l_LevelID in LevelController.GetLevelControllerCache().LevelID)
                    {
                        string l_PlaylistName = $"{l_FileName}_{l_LevelID:D3}{Level.SUFFIX_NAME}";
                        string l_PathFile = l_Path + l_PlaylistName;
                        Level l_Level = new Level(l_LevelID);
                        LevelFormat l_LevelFormat;
                        if (p_Category != null)
                        {
                            RemoveCategoriesFormat l_RemoveCategoriesFormat = RemoveOtherCategoriesFromPlaylist(l_Level.m_Level, p_Category);
                            l_LevelFormat = RemovePassFromPlaylist(l_Player.ReturnPass(), l_RemoveCategoriesFormat.LevelFormat);
                            foreach (string l_Category in l_RemoveCategoriesFormat.Categories)
                            {
                                int l_FindIndex = l_AvailableCategories.FindIndex(p_X => p_X == l_Category);
                                if (l_FindIndex < 0) l_AvailableCategories.Add(l_Category);
                            }

                            if (l_RemoveCategoriesFormat.LevelFormat.songs.Any())
                            {
                                l_CategoryExist = true;
                            }
                        }
                        else
                        {
                            l_LevelFormat = RemovePassFromPlaylist(l_Player.ReturnPass(), l_Level.m_Level);
                        }

                        if (l_LevelFormat.songs.Any()) /// Only create the file if it's not empty.
                        {
                            JsonDataBaseController.CreateDirectory(l_Path);
                            Level.ReWriteStaticPlaylist(l_LevelFormat, l_Path, l_PlaylistName); /// Write the personal playlist file in the PATH folder.
                        }
                    }

                    try
                    {
                        if (Directory.GetFiles(l_Path, "*", SearchOption.AllDirectories).Any())
                        {
                            ZipFile.CreateFromDirectory(l_Path, $"{ORIGINAL_PATH}{l_FileName}.zip");
                            await Context.Channel.SendFileAsync($"{ORIGINAL_PATH}{l_FileName}.zip", $"> :white_check_mark: Here's your personal playlist folder! <@{Context.User.Id.ToString()}>");
                            DeletePlaylistZip(ORIGINAL_PATH, l_FileName);
                        }
                        else
                        {
                            if (l_CategoryExist)
                            {
                                await ReplyAsync($"Sorry but you already passed all the maps from all `{p_Category}` pools, good job!");
                                DeletePlaylistZip(ORIGINAL_PATH, l_FileName);
                            }
                            else
                            {
                                if (p_Category != null)
                                {
                                    string l_Message = $":x: Sorry but there isn't any categories (stored in your stats) called {p_Category}, here is a list of all the available categories:";
                                    foreach (string l_Category in l_AvailableCategories)
                                        if (l_Category != null)
                                            if (l_Category != "")  l_Message += $"\n> {l_Category}";

                                    if (l_Message.Length <= 1980)
                                        await ReplyAsync(l_Message);
                                    else
                                        await ReplyAsync($"> :x: Sorry but there isn't any categories (stored in your stats) called {p_Category},\n+ there is too many different categories in all levels to send all of them in one message.");
                                }
                                else
                                {
                                    await ReplyAsync($"Sorry but you already passed all the maps from all pools, good job!");
                                }
                                DeletePlaylistZip(ORIGINAL_PATH, l_FileName);
                            }
                            
                        }
                    }
                    catch
                    {
                        await Context.Channel.SendMessageAsync("> :x: Seems like you forgot to add Levels. Unless you want an empty zip file?");
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync("> :x: Wrong argument, please use \"1,2,3..\" or \"all\"");
                }
            }
        }
    }
}