using Newtonsoft.Json;
using System;
using System.Net;
using System.Text;

namespace OMineGuard.Backend
{
    public static class Informer
    {
        public static Random Rand = new Random();

        public static (Func<int> GetVKmsgID, Func<string> GetTGmsgID) SendMessage(string Message)
        {
            string message = $"{Settings.Profile.RigName} >> {Message}";

            Func<int> vkfint = () => SendVKMessage(message);
            Func<string> tgfint = () => SendTelegramMessage(message);

            var VKTres = vkfint.BeginInvoke(null, null);
            var TGTres = tgfint.BeginInvoke(null, null);

            return (() => vkfint.EndInvoke(VKTres), () => tgfint.EndInvoke(TGTres));
        }
        public static void EditMessage((Func<int> GetVKmsgID, Func<string> GetTGmsgID) MSGids, string Message)
        {
            string message = $"{Settings.Profile.RigName} >> {Message}";

            EditVKMessage(MSGids.GetVKmsgID.Invoke(), message);
            EditTelegramMessage(MSGids.GetTGmsgID.Invoke(), message);
        }

        private static int SendVKMessage(string message)
        {
            if (Settings.Profile.VkInform)
            {
                string BaseReq = $"https://api.vk.com/method/messages.send" +
                    $"?user_id={Settings.Profile.VKuserID}" +
                    $"&message={message}" +
                    $"&random_id={Rand.Next(0, int.MaxValue)}" +
                    $"&access_token=6e8b089ad4fa647f95cdf89f4b14d183dc65954485efbfe97fe2ca6aa2f65b1934c80fccf4424d9788929" +
                    $"&v=5.131";

                string resp;
                try { resp = Get(BaseReq); }
                catch 
                {
                    try { resp = Get(BaseReq); }
                    catch
                    {
                        try { resp = Get(BaseReq); }
                        catch
                        {
                            return 0;
                        }
                    }
                }

                try
                {
                    int req = JsonConvert.DeserializeObject<VKResponse>(resp).response;
                    return req;
                }
                catch
                {
                    return 0;
                }
            }
            return 0;
        }
        private static void EditVKMessage(int message_id, string message)
        {
            if (Settings.Profile.VkInform)
            {
                string BaseReq = $"https://api.vk.com/method/messages.edit" +
                    $"?peer_id={Settings.Profile.VKuserID}" +
                    $"&message_id={message_id}" +
                    $"&message={message}" + 
                    $"&group_id=163159897" +
                    $"&access_token=6e8b089ad4fa647f95cdf89f4b14d183dc65954485efbfe97fe2ca6aa2f65b1934c80fccf4424d9788929" +
                    $"&v=5.131";

                string resp;
                try { resp = Get(BaseReq); }
                catch
                {
                    try { resp = Get(BaseReq); }
                    catch
                    {
                        try { resp = Get(BaseReq); }
                        catch { }
                    }
                }
            }
        }
        private static object getkey = new object();
        private static string Get(string req)
        {
            lock (getkey)
            {
                using (var resp = WebRequest.Create(req).GetResponse())
                {
                    using (var stream = resp.GetResponseStream()) 
                    {
                        byte[] msg = new byte[256];
                        var count = stream.Read(msg, 0, msg.Length);
                        return Encoding.Default.GetString(msg, 0, count);
                    }
                }
            }
        }
        public class VKResponse
        {
            public int response { get; set; }
        }

        private static string SendTelegramMessage(string message)
        {
            return "";
        }
        private static void EditTelegramMessage(string message_id, string message)
        {
        }
    }
}
