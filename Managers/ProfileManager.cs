using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PM = OMineGuard.ProfileManager;

namespace OMineGuard
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

        public static Profile.Config GetConfig(long? id)
        {
            foreach (Profile.Config c in Profile.ConfigsList)
            {
                if (c.ID == id)
                {
                    return c;
                }
            }
            return null;
        }
        public static Profile.Overclock GetClock(long? id)
        {
            foreach (Profile.Overclock c in Profile.ClocksList)
            {
                if (c.ID == id)
                {
                    return c;
                }
            }
            return null;
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
            Digits = 4;
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
        public bool[] GPUsSwitch;
        public List<Config> ConfigsList;
        public List<Overclock> ClocksList;
        public InformManager Informer;
        public double LogTextSize;

        public int TimeoutWachdog;
        public int TimeoutIdle;
        public int TimeoutLH;

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
            public SettingsManager.Miners? Miner;
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
            public uint[] FanSpeed;
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
}