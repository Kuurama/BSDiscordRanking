using System;
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
        [Command("getcategoryplaylistpack")]
        [Alias("gcplp","getplaylistcategorypack","gpcp","gcpp")]
        [Summary("Sends the desired Level's playlist file. Use `all` instead of the level id to get the whole level folder. It can also sort by Category if you type it.")]
        public async Task GetCategoryPlaylistPack()
        {
            List<string> l_AvailableCategories = new List<string>();
            const string ORIGINAL_PATH = "./PersonalLevels/";
            string l_UserPath = $"{ORIGINAL_PATH}{RemoveSpecialCharacters(Context.User.Username)}/";
            DeleteAllFolderAndFile(l_UserPath); /// Will attempt folder content deletion if there is.
            DeleteFile($"{l_UserPath}CategoryPlaylistPack.zip"); /// Will attempt archive deletion if it already exist.
            JsonDataBaseController.CreateDirectory(l_UserPath); /// Will attempt folder creation if it doesn't exist.

            await Context.Channel.SendMessageAsync("Sending playlists...");
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
                    l_LevelFormat.LevelFormat.customData.syncURL = ConfigController.GetConfig().ApiURL + "playlist/" + l_LevelID +"/"+ l_Category;
                    string l_FileName = RemoveSpecialCharacters(l_Category);
                    string l_Path = l_UserPath + l_FileName + "/";
                    string l_PlaylistName = $"{l_FileName}_{l_LevelID:D3}{Level.SUFFIX_NAME}";

                    if (l_LevelFormat.LevelFormat.songs.Count > 0) /// Only create the file if it's not empty.
                    {
                        JsonDataBaseController.CreateDirectory(l_Path); /// Will attempt folder creation if it doesn't exist.
                        JsonDataBaseController.CreateDirectory(l_Path + l_FileName + "/"); /// Will attempt folder creation if it doesn't exist (so there is a second folder in the zip.
                        Level.ReWriteStaticPlaylist(l_LevelFormat.LevelFormat, l_Path + l_FileName + "/", l_PlaylistName); /// Write the personal playlist file in the PATH folder.
                    }
                }
            }

            try
            {
                if (Directory.GetFiles(l_UserPath, "*", SearchOption.AllDirectories).Any())
                {
                    string[] l_CategoryDirectory = Directory.GetDirectories(l_UserPath);
                    foreach (string l_CategoryPath in l_CategoryDirectory)
                    {
                        string l_ArchivePath = $"{ORIGINAL_PATH}{RemoveSpecialCharacters(Context.User.Username)}_{Directory.CreateDirectory(l_CategoryPath).Name}_Pack.zip";
                        ZipFile.CreateFromDirectory(l_CategoryPath, l_ArchivePath);
                        await Context.Channel.SendFileAsync(l_ArchivePath);
                        DeleteAllFolderAndFile(l_CategoryPath);
                        DeleteFile(l_ArchivePath);
                    }

                    //await Context.Channel.SendFileAsync($"{ORIGINAL_PATH}{RemoveSpecialCharacters(Context.User.Username)}_CategoryPlaylistPack.zip", "> :white_check_mark: Here's the CategoryPlaylistPack, happy grinding!");
                    DeleteAllFolderAndFile(l_UserPath);
                    //DeleteFile($"{ORIGINAL_PATH}{RemoveSpecialCharacters(Context.User.Username)}_CategoryPlaylistPack.zip");

                    /*string l_Message = "> Which mean a pack containing those category: (Put the folders inside your game's playlist folder)";
                    foreach (string l_Category in l_AvailableCategories)
                        if (l_Category != null)
                            if (l_Category != "")
                                l_Message += $"\n> {l_Category}";

                    if (l_Message.Length <= 1980)
                        await ReplyAsync(l_Message);
                    else
                        await ReplyAsync("> Which mean a pack containing all available categories (Put the folders inside your game's playlist folder),\n+ there is too many categories in all levels to send all of them in one message.");*/
                    if (File.Exists(@"./public/FolderMessage.png") == false)
                    {
                        await File.WriteAllBytesAsync(@"./public/FolderMessage.png",Convert.FromBase64String(FOLDER_MESSAGE_IMAGE_B64));
                    }

                    await Context.Channel.SendFileAsync(@"./public/FolderMessage.png", "> Once the folders are put into your playlist folder, make sure to use the Folder sorting tab on the playlist manager UI,\nThat way you will be able to grind the levels by category without having to manually find which level is which. I suggest you to create first a folder like \"ChallengeSaber\" or \"BSCC\".");
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
