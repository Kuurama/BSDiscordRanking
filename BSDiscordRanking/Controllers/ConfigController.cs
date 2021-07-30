using System;
using System.IO;
using Newtonsoft.Json;

namespace BSDiscordRanking.Controllers
{
    public class ConfigController
    {
        public static void CreateConfig()
        {
            string l_Config = JsonConvert.SerializeObject(new ConfigFormat());
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
                        ConfigFormat l_ConfigFormat = JsonConvert.DeserializeObject<ConfigFormat>(l_StreamReader.ReadToEnd());
                        return l_ConfigFormat;
                    }
                }
                catch (Exception e)
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
        
    }
}