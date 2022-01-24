using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Controller;
using BSDiscordRanking.Formats.Level;
using BSDiscordRanking.Formats.Player;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("ggp")]
        [Alias("getgrindpool")]
        [Summary("Shows the Level's maps while displaying your passes, also work with categories, not giving a specific level or category will display the next available level you might want to grind.")]
        public async Task GetGrindPool(string p_Level = null, [Remainder] string p_Category = null)
        {
            LevelControllerFormat l_LevelControllerFormat = LevelController.GetLevelControllerCache();
            bool l_FullEmbeddedGGP = ConfigController.m_ConfigFormat.FullEmbeddedGGP;

            Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
            try
            {
                if (int.TryParse(p_Level, out int l_Level))
                {
                    p_Category = FirstCharacterToUpper(p_Category);
                    await SendGGP(l_LevelControllerFormat, l_Level, l_Player, p_Category, l_FullEmbeddedGGP, false);
                }
                else
                {
                    int l_PlayerLevel;
                    if (p_Level != null) /// Player maybe used p_Level as the Category
                    {
                        p_Level = FirstCharacterToUpper(p_Level);
                        l_PlayerLevel = l_Player.GetPlayerLevel(false, p_Level);
                        if (l_PlayerLevel < 0) /// Category doesn't even exist
                            l_Level = int.MaxValue;
                        else if (l_PlayerLevel == l_Player.GetPlayerLevel(false, p_Level, true)) l_Level = int.MinValue; /// Max level already reached.
                    }
                    else
                    {
                        l_PlayerLevel = l_Player.GetPlayerLevel();
                    }

                    if (l_Level != int.MinValue)
                    {
                        int l_LevelTemp = int.MaxValue;

                        foreach (int l_ID in l_LevelControllerFormat.LevelID.Where(p_ID => p_ID > l_PlayerLevel && p_ID <= l_LevelTemp)) l_LevelTemp = l_ID;

                        l_Level = l_LevelTemp;
                    }

                    if (p_Level != null && p_Category == null)
                    {
                        p_Category = p_Level; /// Player typed !ggp Category, instead of !ggp LevelID Category.
                        await SendGGP(l_LevelControllerFormat, l_Level, l_Player, p_Category, l_FullEmbeddedGGP, true);
                    }
                    else if (p_Level != null)
                    {
                        await ReplyAsync($"> :x: Sorry but you didn't used the command correctly, either use `{BotHandler.m_Prefix}ggp <LevelID> <Category>`, or `{BotHandler.m_Prefix}ggp <Category>`.");
                    }
                    else
                    {
                        await SendGGP(l_LevelControllerFormat, l_Level, l_Player, p_Category, l_FullEmbeddedGGP, true);
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        private async Task SendGGP(LevelControllerFormat p_LevelControllerFormat, int p_Level, Player p_Player, string p_Category, bool p_FullEmbeddedGGP, bool p_CheckForLastGGP)
        {
            float l_EarnedPoints = 0f;
            ConfigFormat l_Config = ConfigController.GetConfig();
            float l_MaxPassPoints = 0f;
            float l_MaxAccPoints = 0f;
            bool l_IDExist = p_LevelControllerFormat.LevelID.Any(p_X => p_X == p_Level);

            if (l_IDExist)
            {
                Level l_Level = new Level(p_Level);
                try
                {
                    bool l_LevelIsPassed = false;
                    PlayerPassFormat l_PlayerPassFormat = new PlayerPassFormat
                    {
                        SongList = new List<InPlayerSong>()
                    };
                    int l_NumberOfPass = 0;
                    bool l_AlreadyHaveThumbnail = false;

                    p_Player.LoadPass();
                    PlayerStatsFormat l_PlayerStats = p_Player.GetStats();
                    int l_LevelIndex = p_Player.m_PlayerStats.Levels.FindIndex(p_X => p_X.LevelID == p_Level);
                    if (l_LevelIndex < 0)
                    {
                        l_LevelIndex = p_Player.m_PlayerStats.Levels.Count;
                        p_Player.m_PlayerStats.Levels.Add(new PassedLevel
                        {
                            LevelID = p_Level,
                            Passed = false,
                            Trophy = new Trophy
                            {
                                Plastic = 0,
                                Silver = 0,
                                Gold = 0,
                                Diamond = 0,
                                Ruby = 0
                            }
                        });
                    }

                    RemoveCategoriesFormat l_RemoveCategoriesFormat = new RemoveCategoriesFormat { LevelFormat = l_Level.m_Level, Categories = null };

                    if (p_Category != null)
                    {
                        l_RemoveCategoriesFormat = RemoveOtherCategoriesFromPlaylist(l_Level.m_Level, p_Category);

                        if (!l_RemoveCategoriesFormat.LevelFormat.songs.Any())
                        {
                            string l_Message = $":x: Sorry but there isn't any categories (stored in your stats) called `{p_Category}` in Level {p_Level}, here is a list of all the available categories:";
                            foreach (string l_Category in l_RemoveCategoriesFormat.Categories)
                                if (l_Category != null)
                                    if (l_Category != "")
                                        l_Message += $"\n> {l_Category}";

                            if (l_Message.Length <= 1980)
                                await ReplyAsync(l_Message);
                            else
                                await ReplyAsync($"> :x: Sorry but there isn't any categories in Level {p_Level} called {p_Category},\n+ there is too many categories in that level to send all of them in one message.");

                            return;
                        }
                    }


                    foreach (var l_Song in l_RemoveCategoriesFormat.LevelFormat.songs.Select((p_Value, p_Index) => new { value = p_Value, index = p_Index }))
                    {
                        l_PlayerPassFormat.SongList.Add(new InPlayerSong
                        {
                            hash = l_Song.value.hash,
                            key = l_Song.value.key,
                            name = l_Song.value.name,
                            DiffList = new List<InPlayerPassFormat>()
                        });

                        foreach (Difficulty l_SongDifficulty in l_Song.value.difficulties)
                        {
                            l_PlayerPassFormat.SongList[l_Song.index].DiffList.Add(
                                new InPlayerPassFormat
                                {
                                    Difficulty = new Difficulty
                                    {
                                        name = l_SongDifficulty.name,
                                        characteristic = l_SongDifficulty.characteristic,
                                        customData = l_SongDifficulty.customData
                                    },
                                    Score = 0,
                                    Rank = 0
                                });
                            bool l_AccWeightAlreadySet = false;
                            bool l_PassWeightAlreadySet = false;
                            if (l_SongDifficulty.customData.forceManualWeight)
                            {
                                if (!l_Config.OnlyAutoWeightForAccLeaderboard)
                                {
                                    l_MaxAccPoints += 100f * l_Config.AccPointMultiplier * l_SongDifficulty.customData.manualWeight;
                                    l_AccWeightAlreadySet = true;
                                }

                                if (!l_Config.OnlyAutoWeightForPassLeaderboard)
                                {
                                    l_MaxPassPoints += l_Config.PassPointMultiplier * l_SongDifficulty.customData.manualWeight;
                                    l_PassWeightAlreadySet = true;
                                }
                            }

                            if (l_SongDifficulty.customData.AutoWeight > 0 && l_Config.AutomaticWeightCalculation)
                            {
                                if (!l_AccWeightAlreadySet && l_Config.OnlyAutoWeightForAccLeaderboard)
                                {
                                    l_MaxAccPoints += 100f * l_Config.AccPointMultiplier * l_SongDifficulty.customData.AutoWeight;
                                    l_AccWeightAlreadySet = true;
                                }

                                if (!l_PassWeightAlreadySet && l_Config.OnlyAutoWeightForPassLeaderboard)
                                {
                                    l_MaxPassPoints += l_Config.PassPointMultiplier * l_SongDifficulty.customData.AutoWeight;
                                    l_PassWeightAlreadySet = true;
                                }
                            }

                            if (l_Config.PerPlaylistWeighting)
                            {
                                if (!l_Config.OnlyAutoWeightForAccLeaderboard && !l_AccWeightAlreadySet) l_MaxAccPoints += 100f * l_Config.AccPointMultiplier * l_Level.m_Level.customData.weighting;

                                if (!l_Config.OnlyAutoWeightForPassLeaderboard && !l_PassWeightAlreadySet) l_MaxPassPoints += l_Config.PassPointMultiplier * l_Level.m_Level.customData.weighting;
                            }
                        }

                        if (p_Player.m_PlayerPass != null)
                            foreach (InPlayerSong l_PlayerPass in p_Player.m_PlayerPass.SongList)
                                if (l_Song.value.hash == l_PlayerPass.hash)
                                    foreach (Difficulty l_SongDifficulty in l_Song.value.difficulties)
                                    foreach (InPlayerPassFormat l_PlayerPassDifficulty in l_PlayerPass.DiffList
                                                 .Where(p_PlayerPassDifficulty => l_SongDifficulty.characteristic == p_PlayerPassDifficulty.Difficulty.characteristic && l_SongDifficulty.name == p_PlayerPassDifficulty.Difficulty.name))
                                    {
                                        l_LevelIsPassed = true;

                                        Console.WriteLine($"Pass detected on {l_Song.value.name} {l_SongDifficulty.name} - {l_SongDifficulty.characteristic}");
                                        l_NumberOfPass++;

                                        int l_DiffIndex = l_PlayerPassFormat.SongList[l_Song.index].DiffList.FindIndex(p_X => p_X.Difficulty.characteristic == l_SongDifficulty.characteristic && p_X.Difficulty.name == l_SongDifficulty.name);
                                        if (l_DiffIndex >= 0)
                                        {
                                            l_PlayerPassFormat.SongList[l_Song.index].DiffList[l_DiffIndex].Difficulty.customData = l_PlayerPassDifficulty.Difficulty.customData;
                                            l_PlayerPassFormat.SongList[l_Song.index].DiffList[l_DiffIndex].Score = l_PlayerPassDifficulty.Score;
                                            l_PlayerPassFormat.SongList[l_Song.index].DiffList[l_DiffIndex].Rank = l_PlayerPassDifficulty.Rank;
                                            l_EarnedPoints += l_Level.m_Level.customData.weighting * 0.375f;
                                        }

                                        break;
                                    }
                    }


                    int l_NumberOfDifficulties = l_RemoveCategoriesFormat.LevelFormat.songs.SelectMany(p_Song => p_Song.difficulties).Count();

                    string l_PlayerTrophy = "";

                    // ReSharper disable once IntDivisionByZero
                    if (l_NumberOfDifficulties != 0)
                        switch (l_NumberOfPass * 100 / l_NumberOfDifficulties)
                        {
                            case 0:
                            {
                                l_PlayerTrophy = "";
                                break;
                            }
                            case <= 25:
                            {
                                p_Player.m_PlayerStats.Levels[l_LevelIndex].Trophy.Plastic = 1;
                                l_PlayerTrophy = "<:big_plastic:916492151402164314>";
                                break;
                            }
                            case <= 50:
                            {
                                p_Player.m_PlayerStats.Levels[l_LevelIndex].Trophy.Silver = 1;
                                l_PlayerTrophy = "<:big_silver:916492243743932467>: ";
                                break;
                            }
                            case <= 75:
                            {
                                p_Player.m_PlayerStats.Levels[l_LevelIndex].Trophy.Gold = 1;
                                l_PlayerTrophy = "<:big_gold:916492277780709426>: ";
                                break;
                            }

                            case <= 99:
                            {
                                p_Player.m_PlayerStats.Levels[l_LevelIndex].Trophy.Diamond = 1;
                                l_PlayerTrophy = "<:big_diamond:916492304108355685>: ";
                                break;
                            }

                            case 100:
                            {
                                p_Player.m_PlayerStats.Levels[l_LevelIndex].Trophy.Ruby = 1;
                                l_PlayerTrophy = "<:big_ruby:916803316925755473>";
                                break;
                            }
                        }

                    if (l_NumberOfPass > 0)
                    {
                        EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                        l_EmbedBuilder.WithColor(new Color(0, 255, 0));
                        if (!l_AlreadyHaveThumbnail)
                        {
                            l_EmbedBuilder.WithThumbnailUrl(p_Player.m_PlayerFull.profilePicture);
                            l_AlreadyHaveThumbnail = true;
                        }

                        l_EmbedBuilder.WithTitle(p_Category != null
                            ? $"Passed maps in `{p_Category}` level {p_Level} ({ConfigController.GetConfig().PassPointsName} earned: {l_EarnedPoints}/{l_MaxPassPoints:n2}) {l_PlayerTrophy}"
                            : $"Passed maps in level {p_Level} ({ConfigController.GetConfig().PassPointsName} earned: {l_EarnedPoints}/{l_MaxPassPoints:n2}) {l_PlayerTrophy}"
                        );


                        GGPFormat l_GGP = await BuildGGP(l_PlayerPassFormat, l_EmbedBuilder, p_FullEmbeddedGGP, true, false);
                        int l_EmbedIndex = 0;
                        if (l_GGP.Embed != null)
                            foreach (Embed l_Embed in l_GGP.Embed)
                            {
                                if (l_EmbedIndex == 0 && l_GGP.Embed.Count != 1)
                                {
                                    await Context.Channel.SendMessageAsync("", false, l_Embed);
                                }
                                else if (l_EmbedIndex + 1 >= l_GGP.Embed.Count && l_NumberOfDifficulties - l_NumberOfPass <= 0)
                                {
                                    Embed l_LastEmbed = l_Embed.ToEmbedBuilder().WithFooter($"To get the playlist file: use {BotHandler.m_Prefix}getplaylist {p_Level} (or {BotHandler.m_Prefix}getplaylist all) to get all of them.").Build();
                                    await Context.Channel.SendMessageAsync("", false, l_LastEmbed);
                                }
                                else
                                {
                                    await Context.Channel.SendMessageAsync("", false, l_Embed);
                                }

                                l_EmbedIndex++;
                            }

                        p_Player.m_PlayerStats.Levels[l_LevelIndex].Passed = l_LevelIsPassed;
                    }

                    p_Player.ReWriteStats();

                    List<string> l_Messages = new List<string> { "" }; /// Reset the Message between Passed and Unpassed maps

                    if (l_NumberOfDifficulties - l_NumberOfPass > 0)
                    {
                        EmbedBuilder l_EmbedBuilder = new EmbedBuilder();

                        l_EmbedBuilder.WithColor(new Color(255, 0, 0));
                        if (!l_AlreadyHaveThumbnail && p_Player.m_PlayerFull != null)
                        {
                            l_EmbedBuilder.WithThumbnailUrl(p_Player.m_PlayerFull.profilePicture);
                            l_AlreadyHaveThumbnail = true;
                        }

                        l_EmbedBuilder.WithTitle(p_Category != null
                            ? $"Unpassed maps in `{p_Category}` level {p_Level}"
                            : $"Unpassed maps in level {p_Level}");

                        GGPFormat l_GGP = await BuildGGP(l_PlayerPassFormat, l_EmbedBuilder, p_FullEmbeddedGGP, false, true);

                        int l_EmbedIndex = 0;
                        if (l_GGP.Embed != null)
                            foreach (Embed l_Embed in l_GGP.Embed)
                            {
                                if (l_EmbedIndex == 0 && l_GGP.Embed.Count != 1)
                                {
                                    await Context.Channel.SendMessageAsync("", false, l_Embed);
                                }
                                else if (l_EmbedIndex + 1 >= l_GGP.Embed.Count && l_NumberOfPass >= 0)
                                {
                                    Embed l_LastEmbed = l_Embed.ToEmbedBuilder().WithFooter($"To get the playlist file: use {BotHandler.m_Prefix}getplaylist {p_Level} (or {BotHandler.m_Prefix}getplaylist all) to get all of them.").Build();
                                    await Context.Channel.SendMessageAsync("", false, l_LastEmbed);
                                }
                                else
                                {
                                    await Context.Channel.SendMessageAsync("", false, l_Embed);
                                }

                                l_EmbedIndex++;
                            }
                    }
                }
                catch (Exception l_Exception)
                {
                    await ReplyAsync($"> :x: Error occured : {l_Exception.Message}");
                }
            }
            else if (p_CheckForLastGGP)
            {
                if (p_Level == int.MinValue)
                    await ReplyAsync("> :white_check_mark: Seems like there isn't any new level to grind for you right now, good job.");
                else if (p_Level == int.MaxValue)
                    await ReplyAsync($"> :x: Sorry but there isn't any category called `{p_Category}` (stored in your stats) available for category's level grind pool.");
                else
                    await ReplyAsync($"> :x: Weird Behavior occured, Level: {p_Level}, category {p_Category}.");
            }

            else
            {
                await ReplyAsync("> :x: This level does not exist.");
            }
        }

#pragma warning disable 1998
        private static async Task<GGPFormat> BuildGGP(PlayerPassFormat p_PlayerPassFormat, EmbedBuilder p_EmbedBuilder, bool p_FullEmbeddedGGP, bool p_OnlySendPasses, bool p_DisplayCategory)
#pragma warning restore 1998
        {
            int l_Y = 0;
            int l_NumbedOfEmbed = 1;
            string l_LastMessage = null;
            List<Embed> l_EmbedBuiltList = new List<Embed>();
            List<string> l_Messages = new List<string> { "" };

            List<Tuple<InPlayerSong, InPlayerPassFormat, string>> l_MapsTuples = new List<Tuple<InPlayerSong, InPlayerPassFormat, string>>();
            foreach (InPlayerSong l_Map in p_PlayerPassFormat.SongList)
            foreach (InPlayerPassFormat l_Diff in l_Map.DiffList)
                if (l_Diff.Difficulty.customData.category != null && l_Diff.Difficulty.customData.customCategoryInfo != null)
                    l_MapsTuples.Add(new Tuple<InPlayerSong, InPlayerPassFormat, string>(l_Map, l_Diff, $"{l_Diff.Difficulty.customData.category}/{l_Diff.Difficulty.customData.customCategoryInfo}")); /// Category + Info
                else if (l_Diff.Difficulty.customData.category != null)
                    l_MapsTuples.Add(new Tuple<InPlayerSong, InPlayerPassFormat, string>(l_Map, l_Diff, $"{l_Diff.Difficulty.customData.category}")); /// Normal category, no info.
                else if (l_Diff.Difficulty.customData.customCategoryInfo != null)
                    l_MapsTuples.Add(new Tuple<InPlayerSong, InPlayerPassFormat, string>(l_Map, l_Diff, $"{l_Diff.Difficulty.customData.customCategoryInfo}")); /// Fake category.
                else
                    l_MapsTuples.Add(new Tuple<InPlayerSong, InPlayerPassFormat, string>(l_Map, l_Diff, null)); /// No category

            IOrderedEnumerable<Tuple<InPlayerSong, InPlayerPassFormat, string>> l_SortedMapsTuples = from l_Category in l_MapsTuples orderby l_Category.Item3 select l_Category;


            string l_CurrentCategory = null;
            int l_TotalMessageLength = 1700;
            bool l_FirstEmbed = true;

            foreach ((InPlayerSong l_Map, InPlayerPassFormat l_Diff, string l_Category) in l_SortedMapsTuples)
            {
                if (p_OnlySendPasses) /// Sort passed maps from unpassed maps with score.
                {
                    if (l_Diff.Score <= 0)
                        continue;
                }
                else
                {
                    if (l_Diff.Score > 0)
                        continue;
                }

                if (l_CurrentCategory != l_Category && p_DisplayCategory)
                {
                    l_Messages.Add("");
                    if (l_LastMessage is not ("" or null))
                    {
                        p_EmbedBuilder.WithDescription(l_LastMessage);
                        l_EmbedBuiltList.Add(p_EmbedBuilder.Build());
                        p_EmbedBuilder = new EmbedBuilder();
                        p_EmbedBuilder.WithColor(l_Diff.Score != 0 ? new Color(0, 255, 0) : new Color(255, 0, 0));
                        l_LastMessage = "";
                        l_Messages[^1] = l_Messages[^1].Insert(0, $"**{l_Category} maps:**\n\n");
                        l_CurrentCategory = l_Category;
                        l_TotalMessageLength = 1700 - $"**{l_Category} maps:**\n\n".Length;
                    }
                    else if (l_FirstEmbed)
                    {
                        l_FirstEmbed = false;
                        l_Messages[^1] = l_Messages[^1].Insert(0, $"**{l_Category} maps:**\n\n");
                        l_CurrentCategory = l_Category;
                        l_TotalMessageLength = 1700 - $"**{l_Category} maps:**\n\n".Length;
                    }
                }

                string l_ScoreOnMap = l_Diff.Score != 0 ? $"- {Math.Round(l_Diff.Score / l_Diff.Difficulty.customData.maxScore * 100f * 100f) / 100f}%" : null;
                string l_RankOnMap = l_Diff.Rank != 0 ? $"(#{l_Diff.Rank})" : null;
                string l_CustomText = l_Diff.Difficulty.customData.infoOnGGP != null ? $"- {l_Diff.Difficulty.customData.infoOnGGP.Replace("_", " ")}" : "";


                if (p_FullEmbeddedGGP)
                {
                    string l_EmbedValue = $"{l_Diff.Difficulty.name} - {l_Diff.Difficulty.characteristic}";
                    if (l_Diff.Difficulty.customData.minScoreRequirement != 0)
                        l_EmbedValue += $" - MinScore: {l_Diff.Difficulty.customData.minScoreRequirement} ({Math.Round((float)l_Diff.Difficulty.customData.minScoreRequirement / l_Diff.Difficulty.customData.maxScore * 100f * 100f) / 100f}%) {l_CustomText}";
                    else
                        l_EmbedValue += l_CustomText;

                    p_EmbedBuilder.AddField($"{l_Map.name} {l_ScoreOnMap} {l_RankOnMap}", l_EmbedValue, true);

                    if (l_NumbedOfEmbed % 2 != 0)
                    {
                        if (l_Y % 2 == 0)
                            p_EmbedBuilder.AddField("\u200B", "\u200B", true);
                    }
                    else if (l_Y % 2 != 0)
                    {
                        p_EmbedBuilder.AddField("\u200B", "\u200B", true);
                    }

                    l_Y++;
                    if (l_Y % 15 == 0)
                    {
                        l_EmbedBuiltList.Add(p_EmbedBuilder.Build());
                        p_EmbedBuilder = new EmbedBuilder();

                        p_EmbedBuilder.WithColor(l_Diff.Score != 0 ? new Color(0, 255, 0) : new Color(255, 0, 0));
                        l_NumbedOfEmbed++;
                    }
                }
                else
                {
                    if (l_Messages[^1].Length + $"{l_Map.name} - {l_Diff.Difficulty.name} - {l_Diff.Difficulty.characteristic}  - MinScore: {l_Diff.Difficulty.customData.minScoreRequirement} ({Math.Round((float)l_Diff.Difficulty.customData.minScoreRequirement / l_Diff.Difficulty.customData.maxScore * 100f * 100f) / 100f}%) {l_CustomText} {l_ScoreOnMap} {l_RankOnMap}\n"
                            .Length > l_TotalMessageLength) /// Description/Field lenght limit
                    {
                        p_EmbedBuilder.WithDescription(l_Messages[^1]);
                        l_Messages.Add(""); /// Initialize the next used index.
                        l_EmbedBuiltList.Add(p_EmbedBuilder.Build());
                        p_EmbedBuilder = new EmbedBuilder();
                        p_EmbedBuilder.WithColor(l_Diff.Score != 0 ? new Color(0, 255, 0) : new Color(255, 0, 0));
                    }
                    else if (p_EmbedBuilder.Fields.Count >= 22)
                    {
                        p_EmbedBuilder.WithDescription(l_Messages[^1]);
                        l_Messages.Add(""); /// Initialize the next used index.
                        l_EmbedBuiltList.Add(p_EmbedBuilder.Build());
                        p_EmbedBuilder = new EmbedBuilder();
                        p_EmbedBuilder.WithColor(l_Diff.Score != 0 ? new Color(0, 255, 0) : new Color(255, 0, 0));
                    }

                    l_Messages[^1] += $"***`{l_Map.name.Replace("`", @"\`").Replace("*", @"\*")}`***";

                    if (l_Diff.Difficulty.characteristic == "Standard")
                        l_Messages[^1] += $" *`({l_Diff.Difficulty.name})`*";
                    else
                        l_Messages[^1] += $" *`({l_Diff.Difficulty.name} - {l_Diff.Difficulty.characteristic})`*";

                    if (l_Diff.Difficulty.customData.minScoreRequirement != 0)

                        l_Messages[^1] += $" *`- MinScore: {l_Diff.Difficulty.customData.minScoreRequirement} ({Math.Round((float)l_Diff.Difficulty.customData.minScoreRequirement / l_Diff.Difficulty.customData.maxScore * 100f * 100f) / 100f}%)`*";


                    if (l_CustomText != "")
                        l_Messages[^1] += $" *`{l_CustomText}`*";

                    if (l_ScoreOnMap != null) l_Messages[^1] += $" *`{l_ScoreOnMap}`*";

                    if (l_RankOnMap != null) l_Messages[^1] += $" *`{l_RankOnMap}`*";

                    l_Messages[^1] += "\n";

                    l_LastMessage = l_Messages[^1];
                }

                if (l_CurrentCategory != l_Category && p_DisplayCategory && l_LastMessage != null)
                {
                    l_LastMessage = l_LastMessage.Insert(0, $"**{l_Category} maps:**\n\n");
                    l_CurrentCategory = l_Category;
                }
            }

            if (!p_FullEmbeddedGGP && l_LastMessage != "") p_EmbedBuilder.WithDescription(p_EmbedBuilder.Description += l_LastMessage);


            if (p_EmbedBuilder.Fields.Count <= 0 && p_EmbedBuilder.Description == null) p_EmbedBuilder = null;

            if (p_EmbedBuilder != null) l_EmbedBuiltList.Add(p_EmbedBuilder.Build());

            l_EmbedBuiltList.RemoveAll(p_X => p_X.Description == null && p_X.Fields.Length <= 0 && p_X.Title == null);

            return new GGPFormat
            {
                Messages = l_Messages,
                Embed = l_EmbedBuiltList
            };
        }
    }


    public class GGPFormat
    {
        public List<string> Messages { get; set; }
        public List<Embed> Embed { get; set; }
    }
}