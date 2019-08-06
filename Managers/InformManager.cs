using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using SM = OMineManager.SettingsManager;
using MW = OMineManager.MainWindow;
using MM = OMineManager.MinersManager;
using PM = OMineManager.ProfileManager;
using xNet;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows;
using System.Net.NetworkInformation;

namespace OMineManager
{
    public static class InformManager
    {
        private static MinerInfo info;
        private static object InfoKey = new object();
        public static MinerInfo Info
        {
            get { lock (InfoKey) return info; }
            set { lock (InfoKey) info = value; }
        }
        private static int MsCycle = 1000;

        public static Thread WachingThread;
        private static ThreadStart WachingClaymore = new ThreadStart(() =>
        {
            TcpClient client;
            while (true)
            {
                try
                {
                    client = new TcpClient("127.0.0.1", 3333);
                    Byte[] data = Encoding.UTF8.GetBytes("{ \"id\":0,\"jsonrpc\":\"2.0\",\"method\":\"miner_getstat2\"}");
                    NetworkStream stream = client.GetStream();
                    try
                    {
                        // Отправка сообщения
                        stream.Write(data, 0, data.Length);
                        // Получение ответа
                        Byte[] readingData = new Byte[256];
                        String responseData = String.Empty;
                        StringBuilder completeMessage = new StringBuilder();
                        int numberOfBytesRead = 0;
                        do
                        {
                            numberOfBytesRead = stream.Read(readingData, 0, readingData.Length);
                            completeMessage.AppendFormat("{0}", Encoding.UTF8.GetString(readingData, 0, numberOfBytesRead));
                        }
                        while (stream.DataAvailable);
                        try
                        {
                            List<string> LS = JsonConvert.DeserializeObject<ClaymoreInfo>(completeMessage.ToString()).result;
                            Info.Hashrates = JsonConvert.DeserializeObject<double[]>($"[{LS[3].Replace(";", ",")}]");
                            for (int i = 0; i < Info.Hashrates.Length; i++)
                            {
                                Info.Hashrates[i] = Info.Hashrates[i] / 1000;
                            }
                            int lt = Info.Hashrates.Length;
                            int[] xx = JsonConvert.DeserializeObject<int[]>($"[{LS[6].Replace(";", ",")}]");
                            Info.Temperatures = new int[lt];
                            Info.Fanspeeds = new int[lt];
                            for (int n = 0; n < xx.Length; n = n + 2)
                            {
                                Info.Temperatures[n / 2] = (byte)xx[n];
                                Info.Fanspeeds[n / 2] = (byte)xx[n + 1];
                            }
                            Info.ShAccepted = JsonConvert.DeserializeObject<int[]>($"[{LS[9].Replace(";", ",")}]");
                            Info.ShRejected = JsonConvert.DeserializeObject<int[]>($"[{LS[10].Replace(";", ",")}]");
                            Info.ShInvalid = JsonConvert.DeserializeObject<int[]>($"[{LS[11].Replace(";", ",")}]");
                        }
                        catch { }

                        MW.context.Send(MW.Sethashrate, new object[] { Info.Hashrates, Info.Temperatures });
                        if (SWT == null) SWT = DateTime.Now;
                        if ((DateTime.Now - (DateTime)SWT).TotalSeconds > StartingInterval) HashrateWachdog(Info);
                    }
                    finally
                    {
                        stream.Close();
                        client.Close();
                    }
                }
                catch { }
                Thread.Sleep(MsCycle);
            }
        });
        private static ThreadStart WachingGminer = new ThreadStart(() =>
        {
            string content = "";
            HttpRequest request;
            Info.ShInvalid = null;
            Info.Fanspeeds = null;
            while (true)
            {
                try
                {
                    using (request = new HttpRequest())
                    {
                        request.UserAgent = Http.ChromeUserAgent();

                        // Отправляем запрос.
                        HttpResponse response = request.Get("http://localhost:3333/stat");
                        // Принимаем тело сообщения в виде строки.
                        content = response.ToString();
                    }
                    try
                    {
                        GminerDevice[] GDs = JsonConvert.DeserializeObject<GminerInfo>(content).
                                devices.OrderBy(GD => GD.gpu_id).ToArray();

                        Info.Hashrates = GDs.Select(GD => GD.speed).ToArray();
                        Info.Temperatures = GDs.Select(GD => GD.temperature).ToArray();
                        Info.ShAccepted = GDs.Select(GD => GD.accepted_shares).ToArray();
                        Info.ShRejected = GDs.Select(GD => GD.rejected_shares).ToArray();

                        MW.context.Send(MW.Sethashrate, new object[] { Info.Hashrates, Info.Temperatures });
                        if (SWT == null) SWT = DateTime.Now;
                        if ((DateTime.Now - (DateTime)SWT).TotalSeconds > StartingInterval) HashrateWachdog(Info);
                    }
                    catch { }
                }
                catch { }
                Thread.Sleep(MsCycle);
            }
        });
        private static ThreadStart WachingBminer = new ThreadStart(() =>
        {
            string content = "";
            HttpRequest request;
            while (true)
            {
                try
                {
                    using (request = new HttpRequest())
                    {
                        request.UserAgent = Http.ChromeUserAgent();

                        // Отправляем запрос.
                        HttpResponse response = request.Get("http://localhost:3333/api/status");
                        // Принимаем тело сообщения в виде строки.
                        content = response.ToString();
                        for (int i = 0; i < 20; i++)
                        {
                            content = content.Replace("{\"" + i + "\":", "[");
                        }
                        content = content.Replace("}}}}", "}}}]");
                    }
                    try
                    {
                        BminerInfo INF = JsonConvert.DeserializeObject<BminerInfo>(content);

                        Info.Hashrates = INF.miners.Select(m => m.solver.solution_rate).ToArray();
                        Info.Temperatures = INF.miners.Select(m => m.device.temperature).ToArray();
                        Info.Fanspeeds = INF.miners.Select(m => m.device.fan_speed).ToArray();
                        Info.ShAccepted = new int[] { INF.stratum.accepted_shares };
                        Info.ShAccepted = new int[] { INF.stratum.rejected_shares };

                        MW.context.Send(MW.Sethashrate, new object[] { Info.Hashrates, Info.Temperatures });
                        if (SWT == null) SWT = DateTime.Now;
                        if ((DateTime.Now - (DateTime)SWT).TotalSeconds > StartingInterval) HashrateWachdog(Info);
                    }
                    catch { }
                }
                catch { }
                Thread.Sleep(MsCycle);
            }
        });
        private static ThreadStart TS = new ThreadStart(() => { });
        public static void StartWaching(SM.Miners? Miner)
        {
            Info = new MinerInfo();
            SWT = null;
            if (PM.Profile.GPUsSwitch != null)
            { CardsCount = PM.Profile.GPUsSwitch.Where(x => x == true).Count(); }
            else CardsCount = 0;
            SecurityMode1 = false;
            SecurityMode2 = false;

            try
            {
                WachingThread.Abort();
            }
            catch { }
            
            switch (Miner)
            {
                case SM.Miners.Claymore:
                    TS = WachingClaymore;
                    break;
                case SM.Miners.Gminer:
                    TS = WachingGminer;
                    break;
                case SM.Miners.Bminer:
                    TS = WachingBminer;
                    break;
            }
            WachingThread = new Thread(TS);
            WachingThread.Start();
        }
        #region InformMessage
        public static void InformMessage(string message)
        {
            if (PM.Profile.Informer.VkInform && PM.Profile.Informer.VKuserID != null)
            {
                Thread Thr = new Thread(new ThreadStart(() =>
                {
                    using (var request = new HttpRequest())
                    {
                        var urlParams = new RequestParams();

                        urlParams["user_id"] = PM.Profile.Informer.VKuserID;
                        urlParams["message"] = $"{PM.Profile.RigName} >> {message}{Environment.NewLine}[ver {MW.Ver}]";
                        urlParams["access_token"] = "6e8b089ad4fa647f95cdf89f4b14d183dc65954485efbfe97fe2ca6aa2f65b1934c80fccf4424d9788929";
                        urlParams["v"] = "5.73";

                        string content = request.Post("https://api.vk.com/method/messages.send", urlParams).ToString();
                    }
                    Thread.CurrentThread.Abort();
                }));
                Thr.Start();
            }
        }
        #endregion
        #region Wachdog
        public static double MinHashrate;
        private static DateTime? SWT;
        private static DateTime WDT1;
        private static DateTime WDT2;
        private static bool SecurityMode1;
        private static bool SecurityMode2;
        private static int GK;
        private static int WachdogInterval = 30;
        private static int StartingInterval = 30;
        private static int CardsCount;
        public static void HashrateWachdog(MinerInfo Info)
        {
            if (Info.Hashrates.Length < CardsCount)
            {
                MW.WriteGeneralLog("Перезагрузка из-за отвала карты");
                InformMessage("Перезагрузка из-за отвала карты");
                Thread Thr = new Thread(new ThreadStart(() => 
                {
                    MW.context.Send(MM.KillProcess, null);
                    Thread.Sleep(5000);
                    Process.Start("shutdown", "/r /t 0");
                    Application.Current.Shutdown();
                    Thread.CurrentThread.Abort();
                }));
                Thr.Start();
                return;
            }

            if (Info.Hashrates.Sum() == 0)
            {
                MW.WriteGeneralLog("Перезапуск майнера из-за нулевого хешрейта");
                InformMessage("нулевой хешрейт, перезапуск майнера");
                MM.RestartMining();
                return;
            }

            if (Info.Hashrates.Sum() < MinHashrate)
            {
                if (!SecurityMode1)
                {
                    SecurityMode1 = true;
                    WDT1 = DateTime.Now;
                }
                else
                {
                    if ((DateTime.Now - WDT1).TotalSeconds > WachdogInterval)
                    {
                        MW.WriteGeneralLog("Перезапуск майнера из-за низкого хешрейта");
                        InformMessage("низкий хешрейт, перезапуск майнера");
                        MM.RestartMining();
                        return;
                    }
                }
            }
            else if (SecurityMode1)
            {
                SecurityMode1 = false;
            }

        back:
            if (!SecurityMode2)
            {
                for (int i = 0; i < Info.Hashrates.Length; i++)
                {
                    if (Info.Hashrates[i] == 0)
                    {
                        GK = i;
                        SecurityMode2 = true;
                        WDT2 = DateTime.Now;
                    }
                }
            }
            else
            {
                if (Info.Hashrates[GK] == 0 && (DateTime.Now - WDT2).TotalSeconds > WachdogInterval)
                {
                    MW.WriteGeneralLog($"Перезапуск майнера из-за отвала GPU{GK}");
                    InformMessage($"Перезапуск майнера из-за отвала GPU{GK}");
                    MM.RestartMining();
                    return;
                }
                else
                {
                    SecurityMode2 = false;
                    goto back;
                }
            }

            if (Info.Hashrates.Sum() > MinHashrate)
            {
                StopIdleWatchdog();
                return;
            }
        }

        private static Thread InternetWachdogThread;
        private static ThreadStart InternetWachdogTS = new ThreadStart(() =>
        {
            InternetConnectionState = true;
            bool ICS;
            while (true)
            {
                ICS = InternetConnetction();
                if (InternetConnectionState != ICS)
                {
                    if (InternetConnectionState == true)
                    {
                        MW.WriteGeneralLog($"Остановка работы из-за потери интернет соединения");
                        MW.context.Send(MM.KillProcess, null);
                        TCPserver.AbortTCP();
                    }
                    else
                    {
                        MW.WriteGeneralLog($"Возобновление работы");
                        InformMessage($"Возобновление работы после сбоя интернет соединения");
                        MW.context.Send(MM.StartLastMiner, null);
                        TCPserver.ServerStart();
                    }
                    InternetConnectionState = ICS;
                }

                Thread.Sleep(1000);
            }
        });
        private static bool InternetConnectionState;
        public static void StartInternetWachdog()
        {
            try
            {
                InternetWachdogThread.Abort();
            }
            catch { }
            InternetWachdogThread = new Thread(InternetWachdogTS);
            InternetWachdogThread.Start();
        }
        public static void StopWachdog()
        {
            try
            {
                InternetWachdogThread.Abort();
            }
            catch { }
        }

        private static Thread IdleWatchdogThread = new Thread(new ThreadStart(() => { }));
        private static int IdleWatchdogTimeout = 3 * 60; //sec
        private static ThreadStart IdleWatchdogTS = new ThreadStart(() =>
        {
            Thread.Sleep(1000 * IdleWatchdogTimeout);
            MW.WriteGeneralLog("Перезагрузка из-за простоя");
            InformMessage("Перезагрузка из-за простоя");
            MW.context.Send(MM.KillProcess, null);
            Thread.Sleep(5000);
            Process.Start("shutdown", "/r /t 0");
            Application.Current.Shutdown();
        });
        public static void StartIdleWatchdog()
        {
            if (IdleWatchdogThread.IsAlive) return;
            try
            {
                IdleWatchdogThread.Abort();
            }
            catch { }
            IdleWatchdogThread = new Thread(IdleWatchdogTS);
            IdleWatchdogThread.Start();
        }
        public static void StopIdleWatchdog()
        {
            try
            {
                IdleWatchdogThread.Abort();
            }
            catch { }
        }
        #endregion
        #region Claymore
        public class ClaymoreInfo
        {
            public int id { get; set; }
            public object error { get; set; }
            public List<string> result { get; set; }
        }
        #endregion
        #region Gminer
        public class GminerInfo
        {
            public GminerDevice[] devices { get; set; }
        }
        public class GminerDevice
        {
            public int gpu_id { get; set; }
            public double speed { get; set; }
            public int accepted_shares { get; set; }
            public int rejected_shares { get; set; }
            public int temperature { get; set; }
        }
        #endregion
        #region Bminer
        public class BminerInfo
        {
            public BminerStratum stratum { get; set; }
            public List<BminerMiner> miners { get; set; }

        }
        public class BminerStratum
        {
            public int accepted_shares { get; set; }
            public int rejected_shares { get; set; }
        }
        public class BminerMiner
        {
            public BminerSolver solver { get; set; }
            public BminerDevice device { get; set; }
        }
        public class BminerSolver
        {
            public double solution_rate { get; set; }
        }
        public class BminerDevice
        {
            public int temperature { get; set; }
            public int fan_speed { get; set; }
        }
        #endregion
        public class MinerInfo
        {
            public double[] Hashrates;
            public int[] Temperatures;
            public int[] Fanspeeds;
            public int[] ShAccepted;
            public int[] ShRejected;
            public int[] ShInvalid;
        }
        #region Internet
        private static Ping ping = new Ping();
        private static PingReply pingReply;
        public static bool InternetConnetction()
        {
            InternetConnectionState_e cs = new InternetConnectionState_e();
            InternetGetConnectedState(ref cs, 0);

            IC = new bool[] 
            {
                (cs & InternetConnectionState_e.INTERNET_CONNECTION_LAN) == InternetConnectionState_e.INTERNET_CONNECTION_LAN,
                (cs & InternetConnectionState_e.INTERNET_CONNECTION_MODEM) == InternetConnectionState_e.INTERNET_CONNECTION_MODEM,
                (cs & InternetConnectionState_e.INTERNET_CONNECTION_PROXY) == InternetConnectionState_e.INTERNET_CONNECTION_PROXY
            };
            if (IC[0] || IC[1] || IC[2])
            {
                for (byte i = 0; i < 4; i++)
                {
                    pingReply = ping.Send("8.8.8.8");
                    if (pingReply.Status == IPStatus.Success) return true;
                }
            }

            return false;
        }
        public static bool[] IC;
        #endregion
        #region DLLimport
        [DllImport("wininet.dll", CharSet = CharSet.Auto)]
        private extern static bool InternetGetConnectedState(ref InternetConnectionState_e lpdwFlags, int dwReserved);

        [Flags]
        enum InternetConnectionState_e : int
        {
            INTERNET_CONNECTION_MODEM = 0x01,      // true
            INTERNET_CONNECTION_LAN = 0x02,     // true 
            INTERNET_CONNECTION_PROXY = 0x04,       // true
            INTERNET_RAS_INSTALLED = 0x10,
            INTERNET_CONNECTION_OFFLINE = 0x20,
            INTERNET_CONNECTION_CONFIGURED = 0x40
        }
        #endregion
    }
} 