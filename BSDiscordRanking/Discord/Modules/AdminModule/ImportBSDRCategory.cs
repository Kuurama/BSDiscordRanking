using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Controller;
using BSDiscordRanking.Formats.Level;
using Discord.Commands;
using Newtonsoft.Json;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("importbsdrcategory")]
        [Summary("Set this channel as the log messages sending channel (Map Added/Deleted/Edited etc).")]
        public async Task ImportBSDRCategory(string p_ApiURL, bool p_ImportCustomCategoryInfo = false, bool p_ImportCurrentJob = true)
        {
            await ReplyAsync("Processing to do your job using someone else job because it took too long yall know what i mean..");
            HttpClient l_HttpClient = new HttpClient
            {
                BaseAddress = new Uri(p_ApiURL)
            };

            LevelControllerFormat l_LevelIDList;
            using (l_HttpClient)
            {
                try
                {
                    Task<LevelControllerFormat> l_Response = l_HttpClient.GetFromJsonAsync<LevelControllerFormat>("levelcache/");
                    l_Response.Wait();
                    l_LevelIDList = l_Response.Result;
                }
                catch (Exception l_E)
                {
                    await ReplyAsync("Something went wrong while downloading the level cache");
                    Console.WriteLine(l_E);
                    return;
                }
            }

            await ReplyAsync("Doing a level Backup..");
            if (File.Exists("levels.zip"))
                File.Delete("levels.zip");
            try
            {
                ZipFile.CreateFromDirectory(Level.GetPath(), "levels.zip");
                await Context.Channel.SendFileAsync("levels.zip", "> :white_check_mark: Here's the backup files.");
            }
            catch
            {
                await ReplyAsync("> :x: Seems like you forgot to add Levels. Unless you want an empty zip file?");
                return;
            }

            int l_Count = 0;

            Level l_GlobalApiLevelGrouped = null;

            foreach (int l_LevelID in l_LevelIDList.LevelID)
            {
                LevelFormat l_BSDRLevel = FetchPlaylist($"{p_ApiURL}playlist/{l_LevelID}");
                if (l_BSDRLevel is null) continue;

                if (l_GlobalApiLevelGrouped is null)
                {
                    l_GlobalApiLevelGrouped = new Level(0)
                    {
                        m_Level = l_BSDRLevel
                    };
                    continue;
                }

                l_GlobalApiLevelGrouped.m_Level.songs.AddRange(l_BSDRLevel.songs);
                foreach (SongFormat l_ApiDownloadedSong in l_BSDRLevel.songs)
                {
                    SongFormat l_PresentGlobalSong = l_GlobalApiLevelGrouped.m_Level.songs.Find(p_X => string.Equals(p_X.hash, l_ApiDownloadedSong.hash, StringComparison.CurrentCultureIgnoreCase));
                    if (l_PresentGlobalSong is null)
                    {
                        l_GlobalApiLevelGrouped.m_Level.songs.Add(l_ApiDownloadedSong);
                    }
                    else
                    {
                        foreach (Difficulty l_DownloadedDifficulty in l_ApiDownloadedSong.difficulties)
                        {
                            Difficulty l_GlobalDifficulty = l_PresentGlobalSong.difficulties.Find(p_X => p_X.characteristic == l_DownloadedDifficulty.characteristic && p_X.name == l_DownloadedDifficulty.name);
                            if (l_GlobalDifficulty is null)
                            {
                                l_PresentGlobalSong.difficulties.Add(l_DownloadedDifficulty);
                            }
                        }
                    }
                }
            }

            foreach (int l_LevelID in LevelController.GetLevelControllerCache().LevelID)
            {
                l_Count++;

                bool l_LevelShouldUpdate = false;
                Level l_Level = new Level(l_LevelID);
                foreach (SongFormat l_Song in l_Level.m_Level.songs)
                {
                    if (l_GlobalApiLevelGrouped is null)
                        break;

                    var l_GlobalLevelSong = l_GlobalApiLevelGrouped.m_Level.songs.Find(p_X => string.Equals(p_X.hash, l_Song.hash, StringComparison.CurrentCultureIgnoreCase));
                    if (l_GlobalLevelSong is null) continue;

                    foreach (Difficulty l_SongDifficulty in l_Song.difficulties)
                    {
                        Difficulty l_OtherBSDRDiff = l_GlobalLevelSong.difficulties.Find(p_X => p_X.characteristic == l_SongDifficulty.characteristic && p_X.name == l_SongDifficulty.name);
                        if (l_OtherBSDRDiff is null) continue;

                        l_LevelShouldUpdate = true;

                        // Process to import current work
                        if (p_ImportCurrentJob)
                        {
                            if (l_SongDifficulty.customData.customCategoryInfo is "Tech" or "Streams" or "Jumps" or "Vibro")
                            {
                                l_SongDifficulty.customData.category = l_SongDifficulty.customData.customCategoryInfo;
                                if (p_ImportCustomCategoryInfo)
                                {
                                    l_SongDifficulty.customData.customCategoryInfo = l_OtherBSDRDiff.customData.customCategoryInfo;
                                }

                                continue;
                            }
                        }

                        if (l_OtherBSDRDiff.customData.category != "Shitpost")
                        {
                            l_SongDifficulty.customData.category = l_OtherBSDRDiff.customData.category;
                        }

                        if (p_ImportCustomCategoryInfo)
                        {
                            l_SongDifficulty.customData.customCategoryInfo = l_OtherBSDRDiff.customData.customCategoryInfo;
                        }
                    }
                }

                if (l_LevelShouldUpdate)
                {
                    l_Level.ReWritePlaylist(false);
                }

                if (l_Count == 1 || l_Count % 4 == 0)
                {
                    await ReplyAsync($"> {l_Count}/{LevelController.GetLevelControllerCache().LevelID.Count} Level Updated..");
                }
            }

            await ReplyAsync("Yo? Job is done? Are ya happy guys? I hope you are happy because i codded this because of yall lack of job ^^");
        }

        public static LevelFormat FetchPlaylist(string p_URL)
        {
            LevelFormat l_BSDRLevel = null;
            using HttpClient l_HttpClient = new HttpClient();
            try
            {
                Task<string> l_Response = l_HttpClient.GetStringAsync(p_URL);
                l_Response.Wait();
                l_BSDRLevel = JsonConvert.DeserializeObject<LevelFormat>(l_Response.Result);
            }
            catch (AggregateException l_AggregateException)
            {
                if (l_AggregateException.InnerException is HttpRequestException l_HttpRequestException)
                {
                    switch (l_HttpRequestException.StatusCode)
                    {
                        case HttpStatusCode.NotFound:
                            Console.WriteLine("[BSDRPlaylistHandler] The Level do not exist");
                            return null;
                        case HttpStatusCode.TooManyRequests:
                            Console.WriteLine("[BSDRPlaylistHandler] The bot got rate-limited on this URL, Try later");
                            return null;
                        case HttpStatusCode.BadGateway:
                            Console.WriteLine("[BSDRPlaylistHandler] Server BadGateway");
                            return null;
                        case HttpStatusCode.InternalServerError:
                            Console.WriteLine("[BSDRPlaylistHandler] InternalServerError");
                            return null;
                    }
                }
                else
                {
                    Console.WriteLine($"Error: [BSDRPlaylistHandler] FetchPlaylist: Unhandled exception)", l_AggregateException.InnerException);
                }
            }
            return l_BSDRLevel;
        }
    }
}
