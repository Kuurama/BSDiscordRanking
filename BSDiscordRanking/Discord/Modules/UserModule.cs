using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using static System.String;

namespace BSDiscordRanking.Discord.Modules
{
    [CheckChannel]
    public class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("getinfo")]
        [Summary("Shows informations about available maps found.")]
        public async Task GetInfo(string p_SearchArg)
        {
            p_SearchArg = p_SearchArg.Replace("_", "");
            if (p_SearchArg.Length < 3)
            {
                await ReplyAsync("> :x: Sorry, the minimum for searching a map is 3 characters (excluding '_').");
                return;
            }

            var l_Maps = new List<Tuple<SongFormat, int>>();
            new LevelController().FetchLevel();
            foreach (var l_LevelID in LevelController.GetLevelControllerCache().LevelID)
            {
                var l_Level = new Level(l_LevelID);
                foreach (var l_Map in l_Level.m_Level.songs)
                {
                    if (l_Map.name.Replace(" ", "").Replace("_", "").Contains(p_SearchArg, StringComparison.OrdinalIgnoreCase))
                    {
                        l_Maps.Add(new Tuple<SongFormat, int>(l_Map, l_LevelID));
                    }
                }
            }

            if (l_Maps.Count == 0)
            {
                await ReplyAsync("> :x: Sorry, no maps was found.");
                return;
            }

            foreach (var l_GroupedMaps in l_Maps.GroupBy(p_Maps => p_Maps.Item1.hash))
            {
                EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                foreach (var (l_SongFormat, l_Item2) in l_GroupedMaps)
                {
                    foreach (var l_MapDifficulty in l_SongFormat.difficulties)
                    {
                        l_EmbedBuilder.AddField(l_MapDifficulty.name, l_MapDifficulty.characteristic, true);
                        l_EmbedBuilder.AddField("Level:", $"Lv.{l_Item2}", true);
                        l_EmbedBuilder.AddField("\u200B", "\u200B", true);
                    }
                }

                var l_Map = l_GroupedMaps.First().Item1;

                l_EmbedBuilder.WithDescription("Ranked difficulties:");
                l_EmbedBuilder.WithTitle(l_Map.name);
                l_EmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_Map.hash.ToLower()}.jpg");
                l_EmbedBuilder.WithUrl($"https://beatsaver.com/maps/{Level.FetchBeatMapByHash(l_Map.hash, Context).versions[^1].key}");
                await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
            }
        }


        [Command("link")]
        [Summary("Links your Discord account to your ScoreSaber's one.")]
        public async Task LinkUser(string p_ScoreSaberLink = "")
        {
            if (!IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())))
                await ReplyAsync(
                    $"> :x: Sorry, but your account already has been linked. Please use `{BotHandler.m_Prefix}unlink`.");
            else if (!IsNullOrEmpty(p_ScoreSaberLink))
            {
                p_ScoreSaberLink = Regex.Match(p_ScoreSaberLink, @"\d+").Value;
                if (IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())) &&
                    UserController.AccountExist(p_ScoreSaberLink) && !UserController.SSIsAlreadyLinked(p_ScoreSaberLink))
                {
                    UserController.AddPlayer(Context.User.Id.ToString(), p_ScoreSaberLink);
                    await ReplyAsync(
                        $"> :white_check_mark: Your account has been successfully linked.\nLittle tip: use `{BotHandler.m_Prefix}scan` to scan your latest passes!");
                }
                else if (!UserController.AccountExist(p_ScoreSaberLink))
                    await ReplyAsync("> :x: Sorry, but please enter a correct ScoreSaber Link/ID.");
                else if (UserController.SSIsAlreadyLinked(p_ScoreSaberLink))
                {
                    await ReplyAsync(
                        $"> :x: Sorry but this account is already linked to an other user.\nIf you entered the correct id and you didn't linked it on an other discord account\nPlease Contact an administrator.");
                }
                else
                    await ReplyAsync("> :x: Oopsie, unhandled error.");
            }
            else
                await ReplyAsync("> :x: Please enter a ScoreSaber link/id.");
        }

        [Command("unlink")]
        [Summary("Unlinks your discord accounts from your ScoreSaber's one.")]
        public async Task UnLinkUser()
        {
            if (IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())))
            {
                await ReplyAsync($"> :x: Sorry, you don't have any account linked. Please use `{BotHandler.m_Prefix}link` instead.");
            }
            else
            {
                UserController.RemovePlayer(Context.User.Id.ToString());
                await ReplyAsync("> :white_check_mark: Your account was successfully unlinked!");
            }
        }

        [Command("scan")]
        [Alias("dc")]
        [Summary("Scans all your scores & passes. Also update your rank.")]
        public async Task Scan_Scores()
        {
            Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
            int l_OldPlayerLevel = l_Player.GetPlayerLevel(); /// By doing so, as a result => loadstats() inside too.
            if (!UserController.UserExist(Context.User.Id.ToString()))
                await ReplyAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link <ScoreSaber link/id>` instead.");
            else
            {
                bool l_FirsScan = l_Player.FetchScores(Context); /// FetchScore Return true if it's the first scan.
                var l_FetchPass = l_Player.FetchPass(Context);
                if (l_FetchPass.Result >= 1)
                    await ReplyAsync($"> 🎉 Congratulations! <@{Context.User.Id.ToString()}>, You passed {l_FetchPass.Result} new maps!\n> To see your profile, try the ``{ConfigController.GetConfig().CommandPrefix[0]}profile`` command.");
                else
                {
                    if (l_FirsScan)
                        await ReplyAsync($"> Oh <@{Context.User.Id.ToString()}>, Seems like you didn't pass any maps from the pools.");
                    else
                        await ReplyAsync($"> :x: Sorry <@{Context.User.Id.ToString()}>, you didn't pass any new maps.");
                }

                int l_NewPlayerLevel = l_Player.GetPlayerLevel();
                if (l_OldPlayerLevel != l_Player.GetPlayerLevel())
                {
                    if (l_OldPlayerLevel < l_NewPlayerLevel)
                        await ReplyAsync($"> <:Stonks:884058036371595294> GG! You are now Level {l_NewPlayerLevel}.\n> To see your new pool, try the ``{ConfigController.GetConfig().CommandPrefix[0]}ggp`` command.");
                    else
                        await ReplyAsync($"> <:NotStonks:884057234886238208> You lost levels. You are now Level {l_NewPlayerLevel}");
                    await ReplyAsync("> :clock1: The bot will now update your roles. This step can take a while. ``(The bot should now be responsive again)``");
                    var l_RoleUpdate = UserController.UpdatePlayerLevel(Context, Context.User.Id, l_NewPlayerLevel);
                }
            }
        }

        [Command("ggp")]
        [Alias("getgrindpool")]
        [Summary("Shows the Level's maps while displaying your passes, not giving a specific level will display the next available level you might want to grind.")]
        public async Task GetGrindPool(int p_Level = -1)
        {
            bool l_CheckForLastGGP = false;
            try
            {
                if (p_Level < 0)
                {
                    int l_PlayerLevel =
                        new Player(UserController.GetPlayer(Context.User.Id.ToString())).GetPlayerLevel();
                    int l_LevelTemp = int.MaxValue;
                    foreach (var l_ID in LevelController.GetLevelControllerCache().LevelID)
                    {
                        if (l_ID > l_PlayerLevel && l_ID <= l_LevelTemp)
                        {
                            l_LevelTemp = l_ID;
                        }
                    }

                    p_Level = l_LevelTemp;
                    l_CheckForLastGGP = true;
                }
            }
            catch
            {
                // ignored
            }

            bool l_IDExist = false;
            foreach (var l_ID in LevelController.GetLevelControllerCache().LevelID)
            {
                if (l_ID == p_Level)
                    l_IDExist = true;
            }

            if (l_IDExist)
            {
                Level l_Level = new Level(p_Level);
                try
                {
                    List<bool> l_Passed = new List<bool>();
                    int l_NumberOfPass = 0;
                    Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
                    PlayerPassFormat l_PlayerPasses = l_Player.GetPass();
                    l_Player.LoadStats();
                    int l_I = 0;
                    if (l_Level.m_Level.songs.Count > 0)
                    {
                        foreach (var l_Song in l_Level.m_Level.songs)
                        {
                            if (l_PlayerPasses != null)
                                foreach (var l_PlayerPass in l_PlayerPasses.songs)
                                {
                                    if (l_Song.hash == l_PlayerPass.hash)
                                    {
                                        foreach (var l_SongDifficulty in l_Song.difficulties)
                                        {
                                            foreach (var l_PlayerPassDifficulty in l_PlayerPass.difficulties)
                                            {
                                                if (l_SongDifficulty.characteristic ==
                                                    l_PlayerPassDifficulty.characteristic && l_SongDifficulty.name ==
                                                    l_PlayerPassDifficulty.name)
                                                {
                                                    Console.WriteLine(
                                                        $"Pass detected on {l_Song.name} {l_SongDifficulty.name}");
                                                    l_NumberOfPass++;
                                                    l_Passed.Insert(l_I, true);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }


                            for (int l_N = 0; l_N < l_Song.difficulties.Count; l_N++)
                            {
                                if (l_Passed.Count <= l_I)
                                {
                                    l_Passed.Insert(l_I, false);
                                }

                                l_I++;
                            }
                        }
                    }

                    int l_NumberOfDifficulties = 0;
                    foreach (var l_Song in l_Level.m_Level.songs)
                    {
                        foreach (var l_Difficulty in l_Song.difficulties)
                        {
                            l_NumberOfDifficulties++;
                        }
                    }

                    string l_PlayerTrophy = "";

                    while (l_Player.m_PlayerStats.Trophy.Count <= p_Level - 1)
                    {
                        l_Player.m_PlayerStats.Trophy.Add(new Trophy
                        {
                            Plastic = 0,
                            Silver = 0,
                            Gold = 0,
                            Diamond = 0
                        });
                    }

                    // ReSharper disable once IntDivisionByZero
                    if (l_NumberOfDifficulties != 0)
                    {
                        switch (l_NumberOfPass * 100 / l_NumberOfDifficulties)
                        {
                            case 0:
                            {
                                l_PlayerTrophy = "";
                                break;
                            }
                            case <= 39:
                            {
                                l_Player.m_PlayerStats.Trophy[p_Level - 1].Plastic = 1;
                                l_PlayerTrophy = "<:plastic:874215132874571787>";
                                break;
                            }
                            case <= 69:
                            {
                                l_Player.m_PlayerStats.Trophy[p_Level - 1].Silver = 1;
                                l_PlayerTrophy = "<:silver:874215133197500446>";
                                break;
                            }
                            case <= 99:
                            {
                                l_Player.m_PlayerStats.Trophy[p_Level - 1].Gold = 1;
                                l_PlayerTrophy = "<:gold:874215133147197460>";
                                break;
                            }

                            case 100:
                            {
                                l_Player.m_PlayerStats.Trophy[p_Level - 1].Diamond = 1;
                                l_PlayerTrophy = "<:diamond:874215133289795584>";
                                break;
                            }
                        }
                    }

                    l_Player.ReWriteStats();

                    EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                    l_EmbedBuilder.WithTitle($"Maps for Level {p_Level} {l_PlayerTrophy}");
                    string l_BigGgp = null;
                    int l_Y = 0;
                    int l_NumbedOfEmbed = 1;

                    if (ConfigController.GetConfig().BigGGP) l_BigGgp = "\n\u200B";
                    foreach (var l_Song in l_Level.m_Level.songs)
                    {
                        foreach (var l_SongDifficulty in l_Song.difficulties)
                        {
                            var l_EmbedValue = $"{l_SongDifficulty.name} - {l_SongDifficulty.characteristic}{l_BigGgp}";
                            if (l_SongDifficulty.minScoreRequirement != 0)
                                l_EmbedValue += $" - MinScore: {l_SongDifficulty.minScoreRequirement}";

                            if (!l_Passed[l_Y])
                                l_EmbedBuilder.AddField(l_Song.name, l_EmbedValue, true);
                            else
                                l_EmbedBuilder.AddField($"~~{l_Song.name}~~", $"~~{l_EmbedValue}~~", true);

                            if (l_NumbedOfEmbed % 2 != 0)
                            {
                                if (l_Y % 2 == 0)
                                    l_EmbedBuilder.AddField("\u200B", "\u200B", true);
                            }
                            else if (l_Y % 2 != 0)
                                l_EmbedBuilder.AddField("\u200B", "\u200B", true);

                            l_Y++;
                            if (l_Y % 15 == 0)
                            {
                                await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
                                l_EmbedBuilder = new EmbedBuilder();
                                l_NumbedOfEmbed++;
                            }
                        }
                    }

                    l_EmbedBuilder.WithFooter(
                        $"To get the playlist file: use {BotHandler.m_Prefix}getplaylist {p_Level}");
                    await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());

                    l_Player.SetGrindInfo(p_Level, l_Passed, -1, l_Player.m_PlayerStats.Trophy[p_Level - 1], -1, -1);
                }
                catch (Exception l_Exception)
                {
                    await ReplyAsync($"> :x: Error occured : {l_Exception.Message}");
                }
            }
            else if (l_CheckForLastGGP)
            {
                await ReplyAsync(
                    "> :white_check_mark: Seems like there isn't any new level to grind for you right now, good job.");
            }
            else
            {
                await ReplyAsync("> :x: This level does not exist.");
            }
        }

        [Command("getplaylist")]
        [Alias("gpl")]
        [Summary("Sends the desired Level's playlist file. Use `all` instead of the level id to get the whole level folder.")]
        public async Task GetPlaylist(string p_Level)
        {
            if (int.TryParse(p_Level, out _))
            {
                string l_Path = Level.GetPath() + $"{p_Level}{Level.SUFFIX_NAME}.bplist";
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

        private void CreateUnpassedPlaylist(string p_ScoreSaberID, int p_Level, string p_Path)
        {
            PlayerPassFormat l_PlayerPass = new Player(p_ScoreSaberID).ReturnPass();
            Level l_Level = new Level(p_Level);
            LevelFormat l_LevelFormat = l_Level.GetLevelData();
            if (l_LevelFormat.songs.Count > 0)
            {
                foreach (var l_PlayerPassSong in l_PlayerPass.songs)
                {
                    for (int l_I = l_LevelFormat.songs.Count - 1; l_I >= 0; l_I--)
                    {
                        if (l_LevelFormat.songs.Count > 0)
                        {
                            if (String.Equals(l_LevelFormat.songs[l_I].hash, l_PlayerPassSong.hash, StringComparison.CurrentCultureIgnoreCase))
                            {
                                foreach (var l_PlayerPassSongDifficulty in l_PlayerPassSong.difficulties)
                                {
                                    if (l_LevelFormat.songs.Count > 0)
                                    {
                                        for (int l_Y = l_LevelFormat.songs[l_I].difficulties.Count - 1; l_Y >= 0; l_Y--)
                                        {
                                            if (l_LevelFormat.songs[l_I].difficulties.Count > 0 && l_LevelFormat.songs.Count > 0)
                                            {
                                                if (l_LevelFormat.songs[l_I].difficulties[l_Y].characteristic == l_PlayerPassSongDifficulty.characteristic && l_LevelFormat.songs[l_I].difficulties[l_Y].name == l_PlayerPassSongDifficulty.name)
                                                {
                                                    /// Here remove diff or map if it's the only ranked diff
                                                    if (l_LevelFormat.songs[l_I].difficulties.Count <= 1)
                                                    {
                                                        l_LevelFormat.songs.Remove(l_LevelFormat.songs[l_I]);
                                                    }
                                                    else
                                                    {
                                                        l_LevelFormat.songs[l_I].difficulties.Remove(l_LevelFormat.songs[l_I].difficulties[l_Y]);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (l_LevelFormat.songs.Count <= 0)
                return; /// Do not create the file if it's empty.
            l_Level.CreateDirectory(p_Path);
            l_Level.ReWritePlaylist(true, p_Path, l_LevelFormat); /// Write the personal playlist file in the PATH folder.
        }

        private static string RemoveSpecialCharacters(string p_Str)
        {
            StringBuilder l_SB = new StringBuilder();
            foreach (char l_C in p_Str)
            {
                if (l_C is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_')
                {
                    l_SB.Append(l_C);
                }
            }

            return l_SB.ToString();
        }

        private void DeleteUnpassedPlaylist(string p_OriginalPath, string p_FileName)
        {
            ///// Delete all personnal files before generating new ones /////////
            string l_Path = p_OriginalPath + p_FileName + "/";
            if (File.Exists(p_OriginalPath + p_FileName + ".zip"))
                File.Delete(p_OriginalPath + p_FileName + ".zip");

            DirectoryInfo l_Directory = new DirectoryInfo(l_Path);
            foreach (FileInfo l_File in l_Directory.EnumerateFiles())
            {
                l_File.Delete();
            }

            foreach (DirectoryInfo l_Dir in l_Directory.EnumerateDirectories())
            {
                l_Dir.Delete(true);
            }

            Directory.Delete(p_OriginalPath + p_FileName + "/");
            ///////////////////////////////////////////////////////////////////////
        }

        [Command("getunpassedplaylist")]
        [Alias("gupl")]
        [Summary("Sends Playlist only containing the maps you didn't pass. Use `all` instead of the level id to get the whole level folder.")]
        public async Task GetUnpassedPlaylist(string p_Level)
        {
            if (!UserController.UserExist(Context.User.Id.ToString()))
                await Context.Channel.SendMessageAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link <ScoreSaber link/id>` instead.");
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

                string l_PlayerName = new Player(UserController.GetPlayer(Context.User.Id.ToString())).m_PlayerFull.playerInfo.playerName;
                string l_FileName = RemoveSpecialCharacters(l_PlayerName);
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
                    if (LevelController.GetLevelControllerCache().LevelID.Contains(int.Parse(p_Level)))
                    {
                        string l_PathFile = l_Path + $"{p_Level}{Level.SUFFIX_NAME}.bplist";

                        if (File.Exists(l_PathFile)) /// Mean there is already a personnal playlist file.
                            File.Delete(l_PathFile);


                        CreateUnpassedPlaylist(UserController.GetPlayer(Context.User.Id.ToString()), int.Parse(p_Level), l_Path);
                        if (File.Exists(l_PathFile))
                            await Context.Channel.SendFileAsync(l_PathFile, $"> :white_check_mark: Here's your personal playlist! <@{Context.User.Id.ToString()}>");
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
                        CreateUnpassedPlaylist(UserController.GetPlayer(Context.User.Id.ToString()), l_LevelID, l_Path);
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

        private async Task SendProfile(string p_DiscordOrScoreSaberID, bool p_IsSomeoneElse)
        {
            if (p_DiscordOrScoreSaberID.Length is 16 or 17)
            {
                if (!UserController.AccountExist(p_DiscordOrScoreSaberID))
                {
                    await ReplyAsync("> :x: Sorry, but please enter a correct ScoreSaber Link/ID.");
                    return;
                }

                if (!UserController.SSIsAlreadyLinked(p_DiscordOrScoreSaberID))
                {
                    bool l_IsAdmin = false;
                    if (Context.User is SocketGuildUser l_User)
                        if (l_User.Roles.Any(p_Role => p_Role.Id == ConfigController.GetConfig().BotManagementRoleID))
                            l_IsAdmin = true;
                    if (!l_IsAdmin)
                    {
                        await ReplyAsync("> Sorry, This Score Saber account isn't registered on the bot.");
                        return;
                    }
                }
            }
            else if (UserController.UserExist(p_DiscordOrScoreSaberID))
            {
                p_DiscordOrScoreSaberID = UserController.GetPlayer(p_DiscordOrScoreSaberID);
            }
            else
            {
                await Context.Channel.SendMessageAsync(p_IsSomeoneElse
                    ? "> :x: Sorry, this person doesn't have any account linked."
                    : $"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link <ScoreSaber link/id>` instead.");
            }

            Player l_Player = new Player(p_DiscordOrScoreSaberID);
            var l_PlayerStats = l_Player.GetStats();

            int l_Plastics = 0;
            int l_Silvers = 0;
            int l_Golds = 0;
            int l_Diamonds = 0;
            foreach (var l_Trophy in l_PlayerStats.Trophy)
            {
                l_Plastics += l_Trophy.Plastic;
                l_Silvers += l_Trophy.Silver;
                l_Golds += l_Trophy.Gold;
                l_Diamonds += l_Trophy.Diamond;
            }

            LeaderboardController l_LeaderboardController = new LeaderboardController();
            int l_FindIndex = l_LeaderboardController.m_Leaderboard.Leaderboard.FindIndex(p_X =>
                p_X.ScoreSaberID == p_DiscordOrScoreSaberID);
            EmbedBuilder l_EmbedBuilder = new();
            l_EmbedBuilder.WithTitle(l_Player.m_PlayerFull.playerInfo.playerName);
            l_EmbedBuilder.WithUrl("https://scoresaber.com/u/" + l_Player.m_PlayerFull.playerInfo.playerId);
            l_EmbedBuilder.WithThumbnailUrl("https://new.scoresaber.com" + l_Player.m_PlayerFull.playerInfo.avatar);
            l_EmbedBuilder.AddField("Global Rank", ":earth_africa: #" + l_Player.m_PlayerFull.playerInfo.rank, true);
            if (l_FindIndex == -1)
                l_EmbedBuilder.AddField("Server Rank", ":medal: #0 - 0 RPL", true);
            else
                l_EmbedBuilder.AddField("Server Rank", ":medal: #" + $"{l_FindIndex + 1} - {l_LeaderboardController.m_Leaderboard.Leaderboard[l_FindIndex].Points} RPL", true);
            l_EmbedBuilder.AddField("\u200B", "\u200B", true);
            l_EmbedBuilder.AddField("Number of passes", ":clap: " + l_PlayerStats.TotalNumberOfPass, true);
            l_EmbedBuilder.AddField("Level", ":trophy: " + l_Player.GetPlayerLevel(), true);
            l_EmbedBuilder.AddField("\u200B", "\u200B", true);
            l_EmbedBuilder.AddField($"Plastic trophies:", $"<:plastic:874215132874571787>: {l_Plastics}", true);
            l_EmbedBuilder.AddField($"Silver trophies:", $"<:silver:874215133197500446>: {l_Silvers}", true);
            l_EmbedBuilder.AddField("\u200B", "\u200B", true);
            l_EmbedBuilder.AddField($"Gold trophies:", $"<:gold:874215133147197460>: {l_Golds}", true);
            l_EmbedBuilder.AddField($"Diamond trophies:", $"<:diamond:874215133289795584>: {l_Diamonds}", true);
            l_EmbedBuilder.AddField("\u200B", "\u200B", true);
            await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
            // UserController.UpdatePlayerLevel(Context); /// Seems too heavy for !profile
        }

        [Command("ping")]
        [Summary("Shows the latency from Discord & ScoreSaber")]
        public async Task Ping()
        {
            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
            l_EmbedBuilder.AddField("Discord: ", new Ping().Send("discord.com").RoundtripTime + "ms");
            l_EmbedBuilder.AddField("ScoreSaber: ", new Ping().Send("scoresaber.com").RoundtripTime + "ms");
            l_EmbedBuilder.WithFooter("#LoveArche",
                "https://images.genius.com/d4b8905048993e652aba3d8e105b5dbf.1000x1000x1.jpg");
            l_EmbedBuilder.WithColor(Color.Blue);
            await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
        }

        [Command("lvl9")]
        [Summary("Sends tips.")]
        public async Task SendTipsLVL9()
        {
            await Context.Channel.SendMessageAsync("Here.. (take it, but it's secret) : ||http://prntscr.com/soylt9||", false);
        }

        private string GenerateProgressBar(int p_Value, int p_MaxValue, int p_Size)
        {
            float l_Percentage = (float)p_Value / p_MaxValue;
            int l_Progress = (int)Math.Round(p_Size * l_Percentage);
            int l_EmptyProgress = p_Size - l_Progress;
            string l_ProgressText = "";
            for (int l_I = 0; l_I < l_Progress; l_I++)
            {
                l_ProgressText += "▇";
            }

            for (int l_I = 0; l_I < l_EmptyProgress; l_I++)
            {
                l_ProgressText += "—";
            }

            return $"[{l_ProgressText}]";
        }

        [Command("progress")]
        [Summary("Shows your progress on a specific map pools.")]
        public async Task Progress(string p_LevelID)
        {
            if (!UserController.UserExist(Context.User.Id.ToString()))
            {
                await ReplyAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link <ScoreSaber link/id>` instead.");
            }
            else
            {
                int l_LevelID;
                try
                {
                    if (p_LevelID == null)
                    {
                        await Context.Channel.SendMessageAsync($"Please enter a Level number: ``{ConfigController.GetConfig().CommandPrefix[0]}progress <Level>``");
                        return;
                    }

                    l_LevelID = int.Parse(p_LevelID);
                }
                catch
                {
                    await Context.Channel.SendMessageAsync($"Please enter a correct Level Number: ``{ConfigController.GetConfig().CommandPrefix[0]}progress <Level>``");
                    return;
                }

                Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
                PlayerPassPerLevelFormat l_PlayerPassPerLevel = l_Player.GetPlayerPassPerLevel();
                if (l_PlayerPassPerLevel == null)
                {
                    Console.WriteLine($"Player {UserController.GetPlayer(Context.User.Id.ToString())} : l_PlayerPassPerLevel is null");
                }
                else
                {
                    foreach (var l_PerLevelFormat in l_PlayerPassPerLevel.Levels)
                    {
                        if (l_LevelID == l_PerLevelFormat.LevelID)
                        {
                            if (l_PerLevelFormat.NumberOfMapDiffInLevel == 0)
                            {
                                await Context.Channel.SendMessageAsync($"Sorry but the level {l_PerLevelFormat.LevelID} doesn't contain any map.");
                                return;
                            }

                            var l_Builder = new EmbedBuilder()
                                .AddField("Pool", $"Level {l_PerLevelFormat.LevelID}", true)
                                .AddField("Progress Bar", GenerateProgressBar(l_PerLevelFormat.NumberOfPass, l_PerLevelFormat.NumberOfMapDiffInLevel, 10), true)
                                .AddField("Progress Amount", $"{Math.Round((float)(l_PerLevelFormat.NumberOfPass / (float)l_PerLevelFormat.NumberOfMapDiffInLevel) * 100.0f)}% ({l_PerLevelFormat.NumberOfPass}/{l_PerLevelFormat.NumberOfMapDiffInLevel})  {l_PerLevelFormat.TrophyString}", true);
                            var l_Embed = l_Builder.Build();
                            await Context.Channel.SendMessageAsync(null, embed: l_Embed).ConfigureAwait(false);
                            return;
                        }
                    }

                    await Context.Channel.SendMessageAsync($"Sorry but the level {p_LevelID} doesn't exist.");
                }
            }
        }

        [Command("progress")]
        [Summary("Shows your progress through the map pools.")]
        public async Task Progress()
        {
            if (!UserController.UserExist(Context.User.Id.ToString()))
            {
                await ReplyAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link <ScoreSaber link/id>` instead.");
            }
            else
            {
                Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
                PlayerPassPerLevelFormat l_PlayerPassPerLevel = l_Player.GetPlayerPassPerLevel();

                if (l_PlayerPassPerLevel == null)
                {
                    Console.WriteLine($"Player {UserController.GetPlayer(Context.User.Id.ToString())} : l_PlayerPassPerLevel is null");
                    return;
                }
                else
                {
                    int l_MessagesIndex = 0;
                    List<string> l_Messages = new List<string> { "" };
                    if (l_PlayerPassPerLevel.Levels != null)
                    {
                        var l_Builder = new EmbedBuilder()
                            .WithTitle($"{l_Player.m_PlayerFull.playerInfo.playerName}'s Progress Tracker")
                            .WithDescription("Here is your current progress through the map pools:")
                            .WithThumbnailUrl("https://new.scoresaber.com" + l_Player.m_PlayerFull.playerInfo.avatar);
                        foreach (var l_PerLevelFormat in l_PlayerPassPerLevel.Levels)
                        {
                            if (l_PerLevelFormat.NumberOfMapDiffInLevel > 0)
                            {
                                if (l_Messages[l_MessagesIndex].Length +
                                    $"Level {l_PerLevelFormat.LevelID}: {GenerateProgressBar(l_PerLevelFormat.NumberOfPass, l_PerLevelFormat.NumberOfMapDiffInLevel, 10)} {Math.Round((float)(l_PerLevelFormat.NumberOfPass / (float)l_PerLevelFormat.NumberOfMapDiffInLevel) * 100.0f)}% ({l_PerLevelFormat.NumberOfPass}/{l_PerLevelFormat.NumberOfMapDiffInLevel})  {l_PerLevelFormat.TrophyString}\n"
                                        .Length > 900)
                                {
                                    l_MessagesIndex++;
                                }

                                if (l_Messages.Count < l_MessagesIndex + 1)
                                {
                                    l_Messages.Add(""); /// Initialize the next used index.
                                }

                                l_Messages[l_MessagesIndex] +=
                                    $"Level {l_PerLevelFormat.LevelID}: {GenerateProgressBar(l_PerLevelFormat.NumberOfPass, l_PerLevelFormat.NumberOfMapDiffInLevel, 10)} {Math.Round((float)(l_PerLevelFormat.NumberOfPass / (float)l_PerLevelFormat.NumberOfMapDiffInLevel) * 100.0f)}% ({l_PerLevelFormat.NumberOfPass}/{l_PerLevelFormat.NumberOfMapDiffInLevel})  {l_PerLevelFormat.TrophyString}" +
                                    Environment.NewLine;
                            }
                        }

                        foreach (var l_Message in l_Messages)
                        {
                            l_Builder.AddField("\u200B", l_Message);
                        }

                        await Context.Channel.SendMessageAsync(null, embed: l_Builder.Build()).ConfigureAwait(false);
                    }
                }
            }
        }

        [Command("trophy")]
        [Summary("Shows your trophy on a Level.")]
        public async Task ShowTrophy(string p_LevelID = null)
        {
            if (!UserController.UserExist(Context.User.Id.ToString()))
            {
                await ReplyAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link <ScoreSaber link/id>` instead.");
            }
            else
            {
                int l_LevelID;
                try
                {
                    if (p_LevelID == null)
                    {
                        await Context.Channel.SendMessageAsync($"Please enter a Level number: ``{ConfigController.GetConfig().CommandPrefix[0]}trophy <Level>``");
                        return;
                    }

                    l_LevelID = int.Parse(p_LevelID);
                }
                catch
                {
                    await Context.Channel.SendMessageAsync($"Please enter a correct Level Number: ``{ConfigController.GetConfig().CommandPrefix[0]}trophy <Level>``");
                    return;
                }

                Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
                PlayerPassPerLevelFormat l_PlayerPassPerLevel = l_Player.GetPlayerPassPerLevel();
                if (l_PlayerPassPerLevel == null)
                {
                    Console.WriteLine($"Player {UserController.GetPlayer(Context.User.Id.ToString())} : l_PlayerPassPerLevel is null");
                }
                else
                {
                    foreach (var l_PerLevelFormat in l_PlayerPassPerLevel.Levels)
                    {
                        if (l_LevelID == l_PerLevelFormat.LevelID)
                        {
                            if (l_PerLevelFormat.NumberOfMapDiffInLevel == 0)
                            {
                                await Context.Channel.SendMessageAsync($"Sorry but the level {l_PerLevelFormat.LevelID} doesn't contain any map.");
                                return;
                            }

                            var l_Builder = new EmbedBuilder().AddField($"Level {l_PerLevelFormat.LevelID} {l_PerLevelFormat.TrophyString}",
                                $"{l_PerLevelFormat.NumberOfPass}/{l_PerLevelFormat.NumberOfMapDiffInLevel} ({Math.Round((float)(l_PerLevelFormat.NumberOfPass / (float)l_PerLevelFormat.NumberOfMapDiffInLevel) * 100.0f)}%)");
                            var l_Embed = l_Builder.Build();
                            await Context.Channel.SendMessageAsync(null, embed: l_Embed).ConfigureAwait(false);
                            return;
                        }
                    }

                    await Context.Channel.SendMessageAsync($"Sorry but the level {p_LevelID} doesn't exist.");
                }
            }
        }

        [Command("getstarted")]
        [Summary("Displays informations about how you could get started using the bot.")]
        public async Task GetStarted()
        {
            var l_Builder = new EmbedBuilder()
                .WithTitle("How to get started with the ranking bot? :thinking:")
                .WithFooter("Prefix: " + Join(", ", ConfigController.GetConfig().CommandPrefix) + " | Bot made by Kuurama#3423 & Julien#1234")
                .AddField("Step 1", $"The first command you wanna use is the link command:\n```{ConfigController.GetConfig().CommandPrefix[0]}link [ScoreSaberLink]```")
                .AddField("Step 2", "Once you account is linked, (that mean the bot registered your score saber ID on the database),\n" +
                                    "You might want to scan your profile first:\n" +
                                    "> Use the scan command to start the download of your scoresaber's infos/scores and check if you already passed maps from the different map pools:\n" +
                                    $"```{ConfigController.GetConfig().CommandPrefix[0]}scan```")
                .AddField("Oh that's it?", $"> Yes, but there is much more to discover!\n\nYou can try the help command to find new command to try!\n```{ConfigController.GetConfig().CommandPrefix[0]}help```")
                .AddField("How to see the map pools?", $"To see the map pool you are at:\n```{ConfigController.GetConfig().CommandPrefix[0]}ggp``` \nor by adding a pool number:\n```{ConfigController.GetConfig().CommandPrefix[0]}ggp [PoolNumber]```to see a specific pool.", true)
                .AddField("How do i get the maps?",
                    $"To get a specific playlist's pool:\n```{ConfigController.GetConfig().CommandPrefix[0]}gpl [MapPoolNumber]```\n(stand for getplaylist) or even:```{ConfigController.GetConfig().CommandPrefix[0]}gpl all``` to get all the playlist pools! The playlist you get are always up to date.",
                    true)
                .AddField("About the 'ranking'?",
                    $"There is a leaderboard using the ``{ConfigController.GetConfig().CommandPrefix[0]}ld`` command! (or use ``{ConfigController.GetConfig().CommandPrefix[0]}leaderboard``)\nEach pass you do give you ``RPL``, those points are used to sort you on the leaderboard, the further you progress in the pools, the harder the maps are, the more points you get!")
                .AddField("To see your progress through the ranking:", $"Type ``{ConfigController.GetConfig().CommandPrefix[0]}progress``")
                .AddField("How do i look at my profile?", $"```{ConfigController.GetConfig().CommandPrefix[0]}profile```");
            var l_Embed = l_Builder.Build();
            await Context.Channel.SendMessageAsync(null, embed: l_Embed).ConfigureAwait(false);
        }

        [Command("leaderboard")]
        [Alias("ld", "leaderboards")]
        [Summary("Shows the leaderboard.")]
        public async Task Leaderboard(int p_Page = default)
        {
            bool l_PageExist = false;
            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
            LeaderboardController l_LeaderboardController = new LeaderboardController();
            if (p_Page == default)
            {
                try
                {
                    p_Page = l_LeaderboardController.m_Leaderboard.Leaderboard.FindIndex(x =>
                        x.ScoreSaberID == UserController.GetPlayer(Context.User.Id.ToString())) / 10 + 1;
                }
                catch
                {
                    p_Page = 1;
                }
            }

            for (var l_Index = (p_Page - 1) * 10; l_Index < (p_Page - 1) * 10 + 10; l_Index++)
            {
                try
                {
                    var l_RankedPlayer = l_LeaderboardController.m_Leaderboard.Leaderboard[l_Index];
                    l_EmbedBuilder.AddField(
                        $"#{l_Index + 1} - {l_RankedPlayer.Name} : {l_RankedPlayer.Points} RPL",
                        $"Level: {l_RankedPlayer.Level}. [ScoreSaber Profile](https://scoresaber.com/u/{l_RankedPlayer.ScoreSaberID})");
                    l_PageExist = true;
                }
                catch
                {
                    // ignored
                }
            }

            if (l_PageExist)
            {
                l_EmbedBuilder.WithTitle("Leaderboard:");
                await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
            }
            else
                await ReplyAsync("> :x: Sorry, this page doesn't exist");
        }

        [Command("help")]
        [Summary("Shows all the commands & their summaries.")]
        public async Task Help()
        {
            bool l_IsAdmin = false;
            if (Context.User is SocketGuildUser l_User)
                if (l_User.Roles.Any(p_Role => p_Role.Id == ConfigController.GetConfig().BotManagementRoleID))
                    l_IsAdmin = true;


            int i = 0;
            foreach (var l_Module in BotHandler.m_Commands.Modules)
            {
                EmbedBuilder l_Builder = new EmbedBuilder();
                l_Builder.WithTitle(l_Module.Name);
                foreach (var l_Command in l_Module.Commands)
                {
                    string l_Title = ConfigController.GetConfig().CommandPrefix.First() + l_Command.Name;
                    foreach (var l_Parameter in l_Command.Parameters)
                    {
                        l_Title += " [" + l_Parameter.Name.Replace("p_", "") + "]";
                    }

                    l_Builder.AddField(l_Title, l_Command.Summary, true);
                    l_Builder.WithFooter("Prefix: " + Join(", ", ConfigController.GetConfig().CommandPrefix) + " | Bot made by Kuurama#3423 & Julien#1234");
                }

                if (l_Module.Name.Contains("Admin"))
                {
                    if (!l_IsAdmin)
                        continue;
                    l_Builder.WithColor(Color.Red);
                    l_Builder.Footer = null;
                }

                await Context.Channel.SendMessageAsync("", false, l_Builder.Build());
                i++;
            }
        }
    }

    class CheckChannelAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext p_Context, CommandInfo p_Command, IServiceProvider p_Services)
        {
            foreach (var l_AuthorizedChannel in ConfigController.GetConfig().AuthorizedChannels)
            {
                if (p_Context.Message.Channel.Id == l_AuthorizedChannel)
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
            }

            return Task.FromResult(PreconditionResult.FromError(ExecuteResult.FromError(new Exception(ErrorMessage = "Forbidden channel"))));
        }
    }
}