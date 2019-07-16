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

            using (FileStream fstream = new FileStream("Profile.ini", FileMode.Create))
            {
                byte[] array = System.Text.Encoding.Default.GetBytes(JSON);
                fstream.Write(array, 0, array.Length);
            }
        }
        public static Profile ReadProfile()
        {
            try
            {
                using (FileStream fstream = File.OpenRead("Profile.ini"))
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
        public bool[] GPUsSwitch;
    }
}
