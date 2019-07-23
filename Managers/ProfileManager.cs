using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OMineManager
{
    public static class ProfileManager
    {
        public static Profile profile;
        public static Profile Profile
        {
            get
            {
                return profile;
            }
            set
            {
                profile = value;
                SaveProfile();
            }
        }
        public static void SaveProfile()
        {
            string JSON = JsonConvert.SerializeObject(profile);

            using (FileStream fstream = new FileStream("Profile.json", FileMode.Create))
            {
                byte[] array = System.Text.Encoding.Default.GetBytes(JSON);
                fstream.Write(array, 0, array.Length);
            }
        }
        public static Profile ReadProfile()
        {
            try
            {
                using (FileStream fstream = File.OpenRead("Profile.json"))
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
        public static void Initialize()
        {
            profile = ReadProfile();
        }
    }

    public class Profile
    {
        public Profile()
        {
            LogTextSize = 0;
            ConfigsList = new List<Config>();
            ClocksList = new List<Overclock>();
            Digits = 4;
        }

        public string RigName;
        public string StartedConfig;
        public string StartedClock;
        public string StartedProcess;
        public int Digits;
        public bool[] GPUsSwitch;
        public List<Config> ConfigsList;
        public List<Overclock> ClocksList;
        public double LogTextSize;

        public class Config
        {
            public Config()
            {
                Name = "Новый конфиг";
                Algoritm = "";
                Miner = null;
                Pool = "";
                Wallet = "";
                Params = "";
                Overclock = "";
            }

            public string Name;
            public string Algoritm;
            public SettingsManager.Miners? Miner;
            public string Pool;
            public string Port;
            public string Wallet;
            public string Params;
            public string Overclock;
        }
        public class Overclock
        {
            public Overclock()
            {
                Name = "Новый разгон";
            }

            public string Name;
            public int[] PowLim;
            public int[] CoreClock;
            public int[] MemoryClock;
            public uint[] FanSpeed;
        }
    }
}