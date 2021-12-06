using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using BSDiscordRanking.Formats.Controller;
using BSDiscordRanking.Formats.Level;

namespace BSDiscordRanking.Controllers
{
    public static class LevelController
    {
        private const string PATH = @"./";
        private const string FILENAME = "LevelController";

        static LevelController()
        {
            LevelControllerFormat l_LevelControllerFormat = FetchAndGetLevel(); /// Force the Fetch to instanciate m_LevelController.
            l_LevelControllerFormat.LevelID.Sort();
            ReWriteController(l_LevelControllerFormat);
        }

        public static LevelControllerFormat FetchAndGetLevel()
        {
            try
            {
                string[] l_Files = Directory.GetFiles(Level.GetPath());
                LevelControllerFormat l_LevelController = new LevelControllerFormat { LevelID = new List<int>() };

                foreach (string l_FileName in l_Files)
                {
                    string l_StringLevelID = "";
                    for (int l_I = 0; l_I < Path.GetFileName(l_FileName).IndexOf("_", StringComparison.Ordinal); l_I++) l_StringLevelID += Path.GetFileName(l_FileName)[l_I];

                    int l_MyInt = int.Parse(l_StringLevelID);

                    try
                    {
                        l_LevelController.LevelID.Add(l_MyInt);
                    }
                    catch (Exception l_Exception)
                    {
                        Console.WriteLine($"Error with Level name. {l_Exception.Message}");
                    }
                }

                l_LevelController.LevelID.Sort();
                return l_LevelController;
            }
            catch (Exception)
            {
                Console.WriteLine("Don't forget to add Levels before Fetching, creating an Empty LevelController's config file..");
                return new LevelControllerFormat
                {
                    LevelID = new List<int>()
                };
            }
        }

        public static void ReWriteController(LevelControllerFormat p_LevelControllerFormat, int p_TryLimit = 3, int p_TryTimeout = 200)
        {
            /// This Method Serialise the data from m_LevelController and create a cache file depending on the path parameter
            /// and it's PrefixName parameter (Prefix is usefull to sort playlist in the game).
            /// Be Aware that it will replace the current Playlist file (if there is any), it shouldn't be an issue
            /// if you Deserialised that playlist to m_Level by using OpenSavedLevel();
            ///
            /// m_ErrorNumber will be increased at every error and lock the method if it exceed m_ErrorLimit
            if (p_TryLimit > 0)
            {
                try
                {
                    if (p_LevelControllerFormat != null)
                    {
                        p_LevelControllerFormat.LevelID.Sort();
                        File.WriteAllText($"{PATH}{FILENAME}.json", JsonSerializer.Serialize(p_LevelControllerFormat));
                        Console.WriteLine($"Updated LevelController Config File at {PATH}{FILENAME}.json ({p_LevelControllerFormat.LevelID.Count} Level)");
                    }
                    else
                    {
                        Console.WriteLine("Seems like you forgot to Fetch the Level, Returning.");
                    }
                }
                catch
                {
                    Console.WriteLine(
                        "An error occured While attempting to Write the Playlist file. (missing directory? or missing permission?)");
                    p_TryLimit--;
                    Thread.Sleep(p_TryTimeout);
                    ReWriteController(p_LevelControllerFormat, p_TryLimit, p_TryTimeout);
                }
            }
            else
            {
                Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
            }
        }

        public static LevelControllerFormat GetLevelControllerCache()
        {
            if (!File.Exists($"{PATH}{FILENAME}.json"))
            {
                Console.WriteLine($"{PATH}{FILENAME}.json");
                Console.WriteLine("Seems like you forgot Fetch Levels (LevelController), please Fetch Them before using this command : LevelController's Cache is missing, Returning.");
                return null;
            }

            try
            {
                using (StreamReader l_SR = new StreamReader($"{PATH}{FILENAME}.json"))
                {
                    LevelControllerFormat l_LevelController = JsonSerializer.Deserialize<LevelControllerFormat>(l_SR.ReadToEnd());
                    if (l_LevelController == null) /// json contain "null"
                    {
                        Console.WriteLine("Error LevelControllerCache contain null");
                        return null;
                    }

                    l_LevelController.LevelID.Sort();
                    return l_LevelController;
                }
            }
            catch (Exception) /// file format is wrong / there isn't any file.
            {
                Console.WriteLine("Seems like you forgot to Fetch the Levels (LevelController), please Fetch Them before using this command, missing file/wrong file format");
                return null;
            }
        }

        public static string GetPath()
        {
            return PATH;
        }

        public static string GetFileName()
        {
            return FILENAME;
        }

        public static MapExistFormat MapExist_Check(string p_Hash, string p_Difficulty, string p_Characteristic, int p_MinScoreRequirement, string p_Category, string p_CustomCategoryInfo, string p_InfoOnGGP, string p_CustomPassText, bool p_ForceManualWeight, float p_Weight, bool p_AdmingPingOnPass, string p_Key = null)
        {
            LevelControllerFormat l_LevelControllerFormat = GetLevelControllerCache();
            MapExistFormat l_MapExistFormat = new MapExistFormat
            {
                MapExist = false,
                DifferentMinScore = false,
                Level = -1,
                DifferentPassText = false,
                DifferentInfoOnGGP = false,
                DifferentCategory = false,
                DifferentCustomCategoryInfo = false,
                ForceManualWeight = p_ForceManualWeight,
                DifferentForceManualWeight = false,
                adminConfirmationOnPass = p_AdmingPingOnPass,
                DifferentAdminConfirmationOnPass = false,
                Weight = p_Weight,
                DifferentWeight = false,
                Name = null
            };
            foreach (int l_LevelID in l_LevelControllerFormat.LevelID)
            {
                Level l_Level = new Level(l_LevelID);
                Console.WriteLine(l_LevelID);
                foreach (SongFormat l_Map in l_Level.m_Level.songs)
                    if (string.Equals(p_Hash, l_Map.hash, StringComparison.CurrentCultureIgnoreCase) || string.Equals(p_Key, l_Map.key, StringComparison.CurrentCultureIgnoreCase))
                        foreach (Difficulty l_Difficulty in l_Map.difficulties)
                            if (l_Difficulty.name == p_Difficulty && l_Difficulty.characteristic == p_Characteristic)
                            {
                                Console.WriteLine($"Map already exist in level {l_Level}");

                                l_MapExistFormat.MapExist = true;
                                l_MapExistFormat.Level = l_LevelID;

                                if (l_Difficulty.customData.minScoreRequirement != p_MinScoreRequirement)
                                    l_MapExistFormat.DifferentMinScore = true;

                                if (l_Difficulty.customData.customPassText != p_CustomPassText)
                                {
                                    l_MapExistFormat.CustomPassText = p_CustomPassText;
                                    l_MapExistFormat.DifferentPassText = true;
                                }

                                if (l_Difficulty.customData.infoOnGGP != p_InfoOnGGP)
                                {
                                    l_MapExistFormat.InfoOnGGP = p_InfoOnGGP;
                                    l_MapExistFormat.DifferentInfoOnGGP = true;
                                }

                                if (l_Difficulty.customData.category != p_Category)
                                {
                                    l_MapExistFormat.Category = p_Category;
                                    l_MapExistFormat.DifferentCategory = true;
                                }
                                
                                if (l_Difficulty.customData.customCategoryInfo != p_CustomCategoryInfo)
                                {
                                    l_MapExistFormat.CustomCategoryInfo = p_CustomCategoryInfo;
                                    l_MapExistFormat.DifferentCustomCategoryInfo = true;
                                }

                                if (l_Difficulty.customData.forceManualWeight != p_ForceManualWeight)
                                {
                                    l_MapExistFormat.ForceManualWeight = p_ForceManualWeight;
                                    l_MapExistFormat.DifferentForceManualWeight = true;
                                }

                                if (l_Difficulty.customData.adminConfirmationOnPass != p_AdmingPingOnPass)
                                {
                                    l_MapExistFormat.adminConfirmationOnPass = p_AdmingPingOnPass;
                                    l_MapExistFormat.DifferentAdminConfirmationOnPass = true;
                                }

                                if (Math.Abs(l_Difficulty.customData.manualWeight - p_Weight) > 0.001)
                                {
                                    l_MapExistFormat.Weight = p_Weight;
                                    l_MapExistFormat.DifferentWeight = true;
                                }

                                l_MapExistFormat.Name = l_Map.name;

                                return l_MapExistFormat;
                            }
            }

            return l_MapExistFormat;
        }

        public class MapExistFormat
        {
            public bool MapExist { get; set; }
            public bool DifferentMinScore { get; set; }
            public string Category { get; set; }
            public string CustomCategoryInfo { get; set; }
            public string InfoOnGGP { get; set; }
            public bool DifferentPassText { get; set; }
            public bool DifferentCategory { get; set; }
            public bool DifferentCustomCategoryInfo { get; set; }
            public bool DifferentInfoOnGGP { get; set; }
            public string CustomPassText { get; set; }
            public bool ForceManualWeight { get; set; }
            public bool DifferentForceManualWeight { get; set; }
            public bool adminConfirmationOnPass { get; set; }
            public bool DifferentAdminConfirmationOnPass { get; set; }
            public float Weight { get; set; }
            public bool DifferentWeight { get; set; }
            public int Level { get; set; }
            public string Name { get; set; }
        }
    }
}