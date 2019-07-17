using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OMineManager
{
    public static class SettingsManager
    {
        
        public static Dictionary<string, Dictionary<string, string>> Miners;

        public static void Initialize()
        {
            string[,] jss;
            try
            {
                using (FileStream fstream = File.OpenRead("Settings.json"))
                {
                    byte[] array = new byte[fstream.Length];
                    fstream.Read(array, 0, array.Length);
                    string json = System.Text.Encoding.Default.GetString(array);
                    jss = JsonConvert.DeserializeObject<string[,]>(json);
                }
            }
            catch
            {
                jss = new string[,] { };
            }

            Miners = new Dictionary<string, Dictionary<string, string>>();
            int n = (jss.Length / 3);
            for (int i = 0; i < n; i++)
            {
                if (Miners.ContainsKey(jss[i, 0]))
                {
                    Miners[jss[i, 0]].Add(jss[i,1], jss[i,2]);
                }
                else
                {
                    Miners.Add(jss[i, 0], new Dictionary<string, string> { { jss[i, 1], jss[i, 2] } });
                }
            }
        }
    }

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
        public string RigName;
        public bool[] GPUsSwitch;
        public List<Config> ConfigsList;

        public class Config
        {
            public Config()
            {
                Name = "Новый конфиг";
                Algoritm = "";
                Miner = "";
                Pool = "";
                Wallet = "";
                Params = "";
            }

            public string Name;
            public string Algoritm;
            public string Miner;
            public string Pool;
            public string Wallet;
            public string Params;
        }
    }
}
