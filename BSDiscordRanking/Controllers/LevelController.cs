using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using BSDiscordRanking.Formats.Controller;

namespace BSDiscordRanking.Controllers
{
    public class LevelController
    {
        private const string PATH = @"./";
        private const string FILENAME = "LevelController";
        private const int ERROR_LIMIT = 3;
        private int m_ErrorNumber = 0;
        private LevelControllerFormat m_LevelController;

        public LevelController()
        {
            FetchLevel(); /// Force the Fetch to instanciate m_LevelController.
        }

        public void FetchLevel()
        {
            try
            {
                string[] l_Files = Directory.GetFiles(Level.GetPath());
                m_LevelController = new LevelControllerFormat { LevelID = new List<int>() };
                string l_StringLevelID;
                int l_MyInt;

                foreach (string l_FileName in l_Files)
                {
                    l_StringLevelID = "";
                    for (int l_I = 0; l_I < Path.GetFileName(l_FileName).IndexOf("_", StringComparison.Ordinal); l_I++)
                    {
                        l_StringLevelID += Path.GetFileName(l_FileName)[l_I];
                    }

                    l_MyInt = int.Parse(l_StringLevelID);

                    try
                    {
                        m_LevelController.LevelID.Add(l_MyInt);
                    }
                    catch (Exception l_Exception)
                    {
                        Console.WriteLine($"Error with Level name. {l_Exception.Message}");
                    }
                }

                ReWriteController();
            }
            catch (Exception)
            {
                Console.WriteLine($"Don't forget to add Levels before Fetching, creating an Empty LevelController's config file..");
                m_LevelController = new LevelControllerFormat()
                {
                    LevelID = new List<int>()
                };
                ReWriteController();
            }
        }

        private void ReWriteController()
        {
            /// This Method Serialise the data from m_LevelController and create a cache file depending on the path parameter
            /// and it's PrefixName parameter (Prefix is usefull to sort playlist in the game).
            /// Be Aware that it will replace the current Playlist file (if there is any), it shouldn't be an issue
            /// if you Deserialised that playlist to m_Level by using OpenSavedLevel();
            ///
            /// m_ErrorNumber will be increased at every error and lock the method if it exceed m_ErrorLimit
            if (m_ErrorNumber < ERROR_LIMIT)
            {
                try
                {
                    if (m_LevelController != null)
                    {
                        File.WriteAllText($"{PATH}{FILENAME}.json", JsonSerializer.Serialize(m_LevelController));
                        Console.WriteLine($"Updated LevelController Config File at {PATH}{FILENAME}.json ({m_LevelController.LevelID.Count} Level)");
                    }
                    else
                    {
                        Console.WriteLine("Seems like you forgot to Fetch the Level, Attempting to load..");
                        FetchLevel();
                        ReWriteController();
                    }
                }
                catch
                {
                    Console.WriteLine(
                        "An error occured While attempting to Write the Playlist file. (missing directory?)");
                    Console.WriteLine("Attempting to create the directory..");
                    m_ErrorNumber++;
                    Thread.Sleep(200);
                    ReWriteController();
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
                Console.WriteLine("Seems like you forgot Fetch Levels (LevelController), please Fetch Them before using this command : LevelController's Cache is missing");
                Console.WriteLine("Attempting a Fetch..");
                new LevelController().FetchLevel();
                return null;
            }
            else
            {
                try
                {
                    Console.WriteLine("before Fetch");
                    new LevelController().FetchLevel();
                    Console.WriteLine("After Fetch");
                    using (StreamReader l_SR = new StreamReader($"{PATH}{FILENAME}.json"))
                    {
                        Console.WriteLine("Before deserialize");
                        LevelControllerFormat l_LevelController = JsonSerializer.Deserialize<LevelControllerFormat>(l_SR.ReadToEnd());
                        Console.WriteLine("After deserialize");
                        if (l_LevelController == null) /// json contain "null"
                        {
                            Console.WriteLine("Error LevelControllerCache contain null");
                            return null;
                        }
                        else
                            return l_LevelController;
                    }
                }
                catch (Exception) /// file format is wrong / there isn't any file.
                {
                    Console.WriteLine("Seems like you forgot to Fetch the Levels (LevelController), please Fetch Them before using this command, missing file/wrong file format");
                    return null;
                }
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

        private void ResetRetryNumber() ///< Concidering the instance is pretty much created for each command, this is useless in most case.
        {
            /// This Method Reset m_ErrorNumber to 0, because if that number exceed m_ErrorLimit, all the "dangerous" method will be locked.    
            m_ErrorNumber = 0;
            Console.WriteLine("RetryNumber set to 0");
        }

        public MapExistFormat MapExist_Check(string p_Hash, string p_Difficulty, string p_Characteristic, int p_MinScoreRequirement, string p_Category, string p_InfoOnGGP, string p_CustomPassText, bool p_ForceManualWeight, float p_Weight)
        {
            new LevelController().FetchLevel();
            MapExistFormat l_MapExistFormat = new MapExistFormat
            {
                MapExist = false,
                DifferentMinScore = false,
                Level = -1,
                DifferentPassText = false,
                DifferentInfoOnGGP = false,
                DifferentCategory = false,
                ForceManualWeight = p_ForceManualWeight,
                DifferentForceManualWeight = false,
                Weight = p_Weight,
                DifferentWeight = false
            };
            foreach (var l_LevelID in GetLevelControllerCache().LevelID)
            {
                Level l_Level = new Level(l_LevelID);
                Console.WriteLine(l_LevelID);
                foreach (var l_Map in l_Level.m_Level.songs)
                {
                    if (String.Equals(p_Hash, l_Map.hash, StringComparison.CurrentCultureIgnoreCase))
                    {
                        foreach (var l_Difficulty in l_Map.difficulties)
                        {
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

                                if (l_Difficulty.customData.forceManualWeight != p_ForceManualWeight)
                                {
                                    l_MapExistFormat.ForceManualWeight = p_ForceManualWeight;
                                    l_MapExistFormat.DifferentForceManualWeight = true;
                                }

                                if (Math.Abs(l_Difficulty.customData.manualWeight - p_Weight) > 0.001)
                                {
                                    l_MapExistFormat.Weight = p_Weight;
                                    l_MapExistFormat.DifferentWeight = true;
                                }

                                return l_MapExistFormat;
                            }
                        }
                    }
                }
            }

            return l_MapExistFormat;
        }

        public class MapExistFormat
        {
            public bool MapExist { get; set; }
            public bool DifferentMinScore { get; set; }
            public string Category { get; set; }
            public string InfoOnGGP { get; set; }
            public bool DifferentPassText { get; set; }
            public bool DifferentCategory { get; set; }
            public bool DifferentInfoOnGGP { get; set; }
            public string CustomPassText { get; set; }
            public bool ForceManualWeight { get; set; }
            public bool DifferentForceManualWeight { get; set; }
            public float Weight { get; set; }
            public bool DifferentWeight { get; set; }
            public int Level { get; set; }
        }
    }
}