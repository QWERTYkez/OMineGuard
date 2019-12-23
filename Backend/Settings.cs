using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace OMineGuard.Backend
{
    public static class Settings
    {
        static Settings()
        {
            Profile = ReadProfile();
        }
        public static Profile Profile { get; private set; }
        public static void SetProfile(Profile prof)
        {
            Profile = prof;
            SaveProfile();
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
        private static Profile ReadProfile()
        {
            lock (ProfileKey)
            {
                try
                {
                    using (FileStream fstream = File.OpenRead("Settings.json"))
                    {
                        byte[] array = new byte[fstream.Length];
                        fstream.Read(array, 0, array.Length);
                        string json = System.Text.Encoding.Default.GetString(array);
                        return JsonConvert.DeserializeObject<Profile>(json);
                    }
                }
                catch
                {
                    return new Profile();
                }
            }
        }
    }

    public class Profile
    {
        public Profile()
        {
            LogTextSize = 0;
            ConfigsList = new List<Config>();
            ClocksList = new List<Overclock>();
            Informer = new InformManager();
            Digits = 5;
            Autostart = false;

            TimeoutWachdog = 60;
            TimeoutIdle = 3 * 60;
            TimeoutLH = 30;
        }

        public string RigName;
        public bool Autostart;
        public long? StartedID;
        public string StartedProcess;
        public int Digits;
        public List<bool> GPUsSwitch;
        public List<Config> ConfigsList;
        public List<Overclock> ClocksList;
        public InformManager Informer;
        public int LogTextSize;

        public int TimeoutWachdog;
        public int TimeoutIdle;
        public int TimeoutLH;
    }
    public class Config
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

        public string Name;
        public string Algoritm;
        public int? Miner;
        public string Pool;
        public string Port;
        public string Wallet;
        public string Params;
        public long? ClockID;
        public double MinHashrate;
        public long ID;
    }
    public class Overclock
    {
        public Overclock()
        {
            Name = "Новый разгон";
            ID = DateTime.UtcNow.ToBinary();
        }

        public string Name;
        public int[] PowLim;
        public int[] CoreClock;
        public int[] MemoryClock;
        public int[] FanSpeed;
        public long ID;
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