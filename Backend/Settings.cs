using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OMineGuardControlLibrary;
using System;
using System.Collections.Generic;
using System.IO;

namespace OMineGuard.Backend
{
    public static class Settings
    {
        private static IProfile Prof;
        public static IProfile Profile 
        {
            get 
            {
                if (Prof == null) ReadProfile();
                return Prof;
            }
        }
        public static void SetProfile(IProfile prof)
        {
            if (Prof != prof)
            {
                Prof = prof;
                SaveProfile();
            }
        }

        private static readonly object ProfileKey = new object();
        private static void SaveProfile()
        {
            lock (ProfileKey)
            {
                string JSON = JsonConvert.SerializeObject(Profile);

                using (FileStream fstream = new FileStream("Settings.json", FileMode.Create))
                {
                    byte[] array = System.Text.Encoding.Default.GetBytes(JSON);
                    fstream.Write(array, 0, array.Length);
                }
            }
        }
        public class ConfigConverter : CustomCreationConverter<IConfig>
        {
            public override IConfig Create(Type objectType)
            {
                return new Config();
            }
        }
        public class OverclockConverter : CustomCreationConverter<IOverclock>
        {
            public override IOverclock Create(Type objectType)
            {
                return new Overclock();
            }
        }
        private static void ReadProfile()
        {
            lock (ProfileKey)
            {
                try
                {
                    Prof = JsonConvert.DeserializeObject<Profile>
                    (File.ReadAllText("Settings.json"), new JsonConverter[] 
                    { 
                        new ConfigConverter(), 
                        new OverclockConverter() 
                    });
                }
                catch
                {
                    Prof = new Profile();
                }
            }
        }
    }

    public class Profile : IProfile
    {
        public Profile()
        {
            LogTextSize = 0;
            ConfigsList = new List<IConfig>();
            ClocksList = new List<IOverclock>();
            Digits = 5;
            Autostart = false;

            VkInform = false;
            VKuserID = "";

            TimeoutWachdog = 60;
            TimeoutIdle = 3 * 60;
            TimeoutLH = 30;
        }

        public string RigName { get; set; }
        public bool Autostart { get; set; }
        public long? StartedID { get; set; }
        public string StartedProcess { get; set; }
        public int Digits { get; set; }
        public List<bool> GPUsSwitch { get; set; } = new List<bool>();

        public List<IConfig> ConfigsList { get; set; }
        public List<IOverclock> ClocksList { get; set; }
        public int LogTextSize { get; set; }

        public bool VkInform { get; set; }
        public string VKuserID { get; set; }

        public int TimeoutWachdog { get; set; }
        public int TimeoutIdle { get; set; }
        public int TimeoutLH { get; set; }
    }
    public class Config : IConfig
    {
        public Config()
        {
            Name = "Новый конфиг";
            Algoritm = "";
            Pool = "";
            Wallet = "";
            Params = "";
            MinHashrate = 0;
            ID = DateTime.UtcNow.ToBinary();
        }

        public string Name { get; set; }
        public string Algoritm { get; set; }
        public int? Miner { get; set; }
        public string Pool { get; set; }
        public string Port { get; set; }
        public string Wallet { get; set; }
        public string Params { get; set; }
        public long? ClockID { get; set; }
        public double MinHashrate { get; set; }
        public long ID { get; set; }
    }
    public class Overclock : IOverclock
    {
        public Overclock()
        {
            Name = "Новый разгон";
            ID = DateTime.UtcNow.ToBinary();
        }

        public string Name { get; set; }
        public int[] PowLim { get; set; }
        public int[] CoreClock { get; set; }
        public int[] MemoryClock { get; set; }
        public int[] FanSpeed { get; set; }
        public long ID { get; }
    }
    public class InformManager
    {
        public InformManager()
        {
            VkInform = false;
            VKuserID = "";
        }

        public bool VkInform;
        public string VKuserID;
    }
}