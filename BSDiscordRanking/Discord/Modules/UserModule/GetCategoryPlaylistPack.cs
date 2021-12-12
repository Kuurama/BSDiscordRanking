using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("getcategoryplaylistpack")]
        [Alias("gcplp")]
        [Summary("Sends the desired Level's playlist file. Use `all` instead of the level id to get the whole level folder. It can also sort by Category if you type it.")]
        public async Task GetCategoryPlaylistPack()
        {
            List<string> l_AvailableCategories = new List<string>();
            const string ORIGINAL_PATH = "./PersonalLevels/";
            string l_UserPath = $"{ORIGINAL_PATH}{RemoveSpecialCharacters(Context.User.Username)}/";
            DeleteAllFolderAndFile(l_UserPath); /// Will attempt folder content deletion if there is.
            DeleteFile($"{l_UserPath}CategoryPlaylistPack.zip"); /// Will attempt archive deletion if it already exist.
            JsonDataBaseController.CreateDirectory(l_UserPath); /// Will attempt folder creation if it doesn't exist.
            
            
            foreach (int l_LevelID in LevelController.GetLevelControllerCache().LevelID)
            {
                Level l_Level = new Level(l_LevelID);
                RemoveCategoriesFormat l_LevelFormat = RemoveOtherCategoriesFromPlaylist(l_Level.m_Level, null); /// Will return available category on those levels.

                foreach (string l_Category in l_LevelFormat.Categories) /// Will create every category file into their respecting folders.
                {
                    int l_FindIndex = l_AvailableCategories.FindIndex(p_X => p_X == l_Category);
                    if (l_FindIndex < 0) l_AvailableCategories.Add(l_Category); /// Just so it can get the final category list.
                    
                    
                    l_Level.LoadLevel(); /// Reset the level.
                    l_LevelFormat = RemoveOtherCategoriesFromPlaylist(l_Level.m_Level, l_Category);
                    string l_FileName = RemoveSpecialCharacters(l_Category);
                    string l_Path = l_UserPath + l_FileName + "/";
                    string l_PlaylistName = $"{l_FileName}_{l_LevelID:D3}{Level.SUFFIX_NAME}";
                    
                    if (l_LevelFormat.LevelFormat.songs.Count > 0) /// Only create the file if it's not empty.
                    {
                        JsonDataBaseController.CreateDirectory(l_Path); /// Will attempt folder creation if it doesn't exist.
                        Level.ReWriteStaticPlaylist(l_LevelFormat.LevelFormat, l_Path, l_PlaylistName); /// Write the personal playlist file in the PATH folder.
                    }
                }
            }

            try
            {
                if (Directory.GetFiles(l_UserPath, "*", SearchOption.AllDirectories).Any())
                {
                    ZipFile.CreateFromDirectory(l_UserPath, $"{ORIGINAL_PATH}{RemoveSpecialCharacters(Context.User.Username)}_CategoryPlaylistPack.zip");
                    await Context.Channel.SendFileAsync($"{ORIGINAL_PATH}{RemoveSpecialCharacters(Context.User.Username)}_CategoryPlaylistPack.zip", $"> :white_check_mark: Here's the CategoryPlaylistPack, happy grinding!");
                    DeleteAllFolderAndFile(l_UserPath);
                    DeleteFile($"{ORIGINAL_PATH}{RemoveSpecialCharacters(Context.User.Username)}_CategoryPlaylistPack.zip");
                }
                else
                {
                    await Context.Channel.SendMessageAsync(":x: Sorry but it seems there isn't any category available on the levels.");
                }
            }
            catch
            {
                /// Don't do anything?
            }
        }
    }
}