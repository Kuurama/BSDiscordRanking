using System;
using System.Collections.Generic;
using System.IO;
using BSDiscordRanking.Formats;
using Newtonsoft.Json;

namespace BSDiscordRanking.Controllers
{
    public class ConfigController
    {
        public static ConfigFormat m_ConfigFormat = new ConfigFormat(){ AuthorizedChannels = new List<ulong>()};
        
        public static void CreateConfig()
        {
            string l_Config = JsonConvert.SerializeObject(new ConfigFormat() { AuthorizedChannels = new List<ulong>()});
            File.WriteAllText("./config.json", l_Config);
            Console.WriteLine("Blank config file created");
            Environment.Exit(0);
        }

        public static ConfigFormat ReadConfig()
        {
            if (File.Exists("./config.json"))
            {
                try
                {   
                    using (StreamReader l_StreamReader = new StreamReader("./config.json"))
                    {
                        m_ConfigFormat  = JsonConvert.DeserializeObject<ConfigFormat>(l_StreamReader.ReadToEnd());
                        return m_ConfigFormat;
                    }
                }
                catch
                {
                    Console.WriteLine("An error occured while reading the config. Creating a new one");
                    CreateConfig();
                }
            }
            else
            {
                Console.WriteLine("Config does not exist, creating a new one.");
                CreateConfig();
            }
            return null;
        }

        public static void ReWriteConfig()
        {
            string l_Config = JsonConvert.SerializeObject(m_ConfigFormat);
            File.WriteAllText("./config.json", l_Config);
        }
        
    }
}