﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("getplaylist")]
        [Alias("gpl")]
        [Summary("Sends the desired Level's playlist file. Use `all` instead of the level id to get the whole level folder. It can also sort by Category if you type it.")]
        public async Task GetPlaylist(string p_Level = null, [Remainder] string p_Category = null)
        {
            if (p_Category == null)
            {
                if (int.TryParse(p_Level, out _))
                {
                    int l_LevelInt = int.Parse(p_Level);
                    string l_Path = Level.GetPath() + $"{l_LevelInt:D3}{Level.SUFFIX_NAME}.bplist";
                    if (File.Exists(l_Path))
                    {
                        await Context.Channel.SendFileAsync(l_Path, "> :white_check_mark: Here's the complete playlist! (up to date).\n> All difficulties you need to do are highlighted and the playlist can be updated through you game using the sync button.");
                        if (File.Exists(@"./public/SyncPlaylistMessage.png") == false)
                        {
                            await File.WriteAllBytesAsync(@"./public/SyncPlaylistMessage.png",Convert.FromBase64String(SYNC_PLAYLIST_MESSAGE_IMAGE_B64));
                        }

                        await Context.Channel.SendFileAsync(@"./public/SyncPlaylistMessage.png", "> Please note that the playlists can be **updated** through the game using the **sync** button.");
                    }
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
                        await Context.Channel.SendFileAsync("levels.zip", "> :white_check_mark: Here's a playlist folder containing all the playlist's pools! (up to date).\n> All difficulties you need to do are highlighted and the playlists can be updated through you game using the sync button.");
                        if (File.Exists(@"./public/SyncPlaylistMessage.png") == false)
                        {
                            await File.WriteAllBytesAsync(@"./public/SyncPlaylistMessage.png",Convert.FromBase64String(SYNC_PLAYLIST_MESSAGE_IMAGE_B64));
                        }

                        await Context.Channel.SendFileAsync(@"./public/SyncPlaylistMessage.png", "> Please note that the playlists can be **updated** through the game using the **sync** button.");
                    }
                    catch
                    {
                        await ReplyAsync("> :x: Seems like you forgot to add Levels. Unless you want an empty zip file?");
                    }
                }
                else
                {
                    await ReplyAsync("> :x: Wrong argument, please use \"1,2,3..\" or \"all\".");
                }

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

                string l_FileName = ConfigController.GetConfig().GuildName + "_" + RemoveSpecialCharacters(p_Category);
                string l_Path = ORIGINAL_PATH + l_FileName + "/";
                DeleteAllFolderAndFile(l_Path);
                DeleteFile($"{ORIGINAL_PATH}{l_FileName}.zip");

                if (int.TryParse(p_Level, out int l_LevelInt))
                {
                    if (LevelController.GetLevelControllerCache().LevelID.Contains(int.Parse(p_Level)))
                    {
                        string l_PlaylistName = $"{l_FileName}_{l_LevelInt:D3}{Level.SUFFIX_NAME}";
                        string l_PathFile = l_Path + l_PlaylistName;

                        DeleteFile(l_PathFile); /// Delete playlist file if it already exist.

                        Level l_Level = new Level(l_LevelInt);
                        RemoveCategoriesFormat l_LevelFormat = RemoveOtherCategoriesFromPlaylist(l_Level.m_Level, p_Category);

                        if (l_LevelFormat.LevelFormat.songs.Any()) /// Only create the file if it's not empty.
                        {
                            JsonDataBaseController.CreateDirectory(l_Path);
                            if (p_Category is not null)
                            {
                                l_LevelFormat.LevelFormat.customData.syncURL = ConfigController.GetConfig().ApiURL + "playlist/" + l_LevelInt + "/" + p_Category;
                            }
                            Level.ReWriteStaticPlaylist(l_LevelFormat.LevelFormat, l_Path, l_PlaylistName); /// Write the personal playlist file in the PATH folder.
                        }

                        if (File.Exists($"{l_PathFile}{Level.EXTENSION}"))
                        {
                            await Context.Channel.SendFileAsync($"{l_PathFile}{Level.EXTENSION}", $"> :white_check_mark: Here's the {p_Category}'s playlist! <@{Context.User.Id.ToString()}>");
                            if (File.Exists(@"./public/SyncPlaylistMessage.png") == false)
                            {
                                await File.WriteAllBytesAsync(@"./public/SyncPlaylistMessage.png",Convert.FromBase64String(SYNC_PLAYLIST_MESSAGE_IMAGE_B64));
                            }

                            await Context.Channel.SendFileAsync(@"./public/SyncPlaylistMessage.png", "> Please note that the playlists can be **updated** through the game using the **sync** button.");
                        }
                        else
                        {
                            string l_Message = $":x: Sorry but there isn't any categories (stored in your stats) called `{p_Category}` in Level {p_Level}, here is a list of all the available categories:";
                            foreach (string l_Category in l_LevelFormat.Categories)
                                if (l_Category != null)
                                    if (l_Category != "")
                                        l_Message += $"\n> {l_Category}";

                            if (l_Message.Length <= 1980)
                                await ReplyAsync(l_Message);
                            else
                                await ReplyAsync($"> :x: Sorry but there isn't any categories (stored in your stats) called `{p_Category}` in Level {p_Level},\n+ there is too many categories in that level to send all of them in one message.");
                        }

                        if (l_LevelFormat.LevelFormat.songs.Count > 0) /// Only create the file if it's not empty.
                        {
                            DeleteAllFolderAndFile(l_Path);
                            DeleteFile(l_PathFile);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync("> :x: This level does not exist.");
                    }
                }
                else if (p_Level == "all")
                {
                    List<string> l_AvailableCategories = new List<string>();
                    foreach (int l_LevelID in LevelController.GetLevelControllerCache().LevelID)
                    {
                        string l_PlaylistName = $"{l_FileName}_{l_LevelID:D3}{Level.SUFFIX_NAME}";
                        string l_PathFile = l_Path + l_PlaylistName;

                        if (File.Exists(l_PathFile)) /// Mean there is already a personnal playlist file.
                            File.Delete(l_PathFile);

                        Level l_Level = new Level(l_LevelID);
                        RemoveCategoriesFormat l_LevelFormat = RemoveOtherCategoriesFromPlaylist(l_Level.m_Level, p_Category);

                        foreach (string l_Category in l_LevelFormat.Categories)
                        {
                            int l_FindIndex = l_AvailableCategories.FindIndex(p_X => p_X == l_Category);
                            if (l_FindIndex < 0) l_AvailableCategories.Add(l_Category);
                        }

                        if (l_LevelFormat.LevelFormat.songs.Count > 0) /// Only create the file if it's not empty.
                        {
                            JsonDataBaseController.CreateDirectory(l_Path);
                            if (p_Category is not null)
                            {
                                l_LevelFormat.LevelFormat.customData.syncURL = ConfigController.GetConfig().ApiURL + "playlist/" + l_LevelID + "/" + p_Category;
                            }
                            Level.ReWriteStaticPlaylist(l_LevelFormat.LevelFormat, l_Path, l_PlaylistName); /// Write the personal playlist file in the PATH folder.
                        }
                    }

                    try
                    {
                        if (Directory.GetFiles(l_Path, "*", SearchOption.AllDirectories).Any())
                        {
                            ZipFile.CreateFromDirectory(l_Path, $"{ORIGINAL_PATH}{l_FileName}.zip");
                            await Context.Channel.SendFileAsync($"{ORIGINAL_PATH}{l_FileName}.zip", $"> :white_check_mark: Here's the {p_Category}'s playlist folder!");
                            DeleteAllFolderAndFile(l_Path);
                            DeleteFile($"{ORIGINAL_PATH}{l_FileName}.zip");
                            if (File.Exists(@"./public/SyncPlaylistMessage.png") == false)
                            {
                                await File.WriteAllBytesAsync(@"./public/SyncPlaylistMessage.png",Convert.FromBase64String(SYNC_PLAYLIST_MESSAGE_IMAGE_B64));
                            }

                            await Context.Channel.SendFileAsync(@"./public/SyncPlaylistMessage.png", "> Please note that the playlists can be **updated** through the game using the **sync** button.");
                        }
                        else /// Shouldn't ever happen but actually, i prefer doing it.
                        {
                            string l_Message = $":x: Sorry but there isn't any categories (stored in your stats) called {p_Category}, here is a list of all the available categories:";
                            foreach (string l_Category in l_AvailableCategories)
                                if (l_Category != null)
                                    if (l_Category != "")
                                        l_Message += $"\n> {l_Category}";

                            if (l_Message.Length <= 1980)
                                await ReplyAsync(l_Message);
                            else
                                await ReplyAsync($"> :x: Sorry but there isn't any categories (stored in your stats) called {p_Category},\n+ there is too many categories in that level to send all of them in one message.");
                        }
                    }
                    catch
                    {
                        if (l_AvailableCategories.Count > 0)
                        {
                            string l_Message = $":x: Sorry but there isn't any categories (stored in your stats) called {p_Category}, here is a list of all the available categories:";
                            foreach (string l_Category in l_AvailableCategories)
                                if (l_Category != null)
                                    if (l_Category != "")
                                        l_Message += $"\n> {l_Category}";

                            if (l_Message.Length <= 1980)
                                await ReplyAsync(l_Message);
                            else
                                await ReplyAsync($"> :x: Sorry but there isn't any categories (stored in your stats) called {p_Category},\n+ there is too many categories in all levels to send all of them in one message.");
                        }
                        else
                        {
                            await Context.Channel.SendMessageAsync("> :x: Seems like you forgot to add Levels. Unless you want an empty zip file?");
                        }
                    }
                }
                else
                {
                    await ReplyAsync("> :x: Wrong argument, please use \"1,2,3..\" or \"all\".");
                }
            }
        }
    }
}
