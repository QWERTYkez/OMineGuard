using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using xNet;
using MM = OMineGuard.MinersManager;
using MW = OMineGuard.MainWindow;
using PM = OMineGuard.ProfileManager;
using SM = OMineGuard.SettingsManager;
using TCP = OMineGuard.TCPserver;

namespace OMineGuard
{
    public static class InformManager
    {
        #region INFO
        private static MinerInfo currentInfo;
        private static object InfoKey = new object();
        public static MinerInfo Info
        {
            get { lock (InfoKey) return currentInfo; }
            set
            {
                AVGMinerInfo avg = null;
                lock (InfoKey)
                {
                    currentInfo = value;
                    if (value.Hashrates != null)
                    {
                        LInfo.Add(value);
                        avg = AVGInfo;
                    }
                }
                if (avg != null)
                {
                    TCP.INFsend(avg);
                }
            }
        }
        public static AVGMinerInfo AVGInfo
        {
            get
            {
                DateTime DT = LInfo[LInfo.Count - 1].TimeStamp;
                AVGMinerInfo AVG = new AVGMinerInfo(DT, LInfo[LInfo.Count - 1].Hashrates.Length, LInfo[LInfo.Count - 1]);
                LInfo = LInfo.Where(x => (DT - x.TimeStamp).TotalSeconds < 20).ToList();
                int k = LInfo.Count;
                foreach (MinerInfo MI in LInfo)
                {
                    for (int i = 0; i < MI.Hashrates.Length; i++)
                    {
                        AVG.AVGHashrates[i] += MI.Hashrates[i] / k;
                        AVG.AVGTemperatures[i] += Convert.ToDouble(MI.Temperatures[i]) / k;
                        if (MI.Fanspeeds != null)
                        {
                            AVG.AVGFanspeeds[i] += Convert.ToDouble(MI.Fanspeeds[i]) / k;
                        }
                    }
                }
                AVG.ShAccepted = LInfo[LInfo.Count - 1].ShAccepted;
                if (LInfo[LInfo.Count - 1].ShInvalid != null)
                {
                    AVG.ShInvalid = LInfo[LInfo.Count - 1].ShInvalid;
                }
                if (LInfo[LInfo.Count - 1].ShRejected != null)
                {
                    AVG.ShRejected = LInfo[LInfo.Count - 1].ShRejected;
                }
                return AVG;
            }
        }
        public class AVGMinerInfo
        {
            public AVGMinerInfo(DateTime DT, int i, MinerInfo MI)
            {
                AVGHashrates = new double[i];
                AVGTemperatures = new double[i];
                AVGFanspeeds = new double[i];
                ShAccepted = MI.ShAccepted;
                ShRejected = MI.ShRejected;
                ShInvalid = MI.ShInvalid;
                TimeStamp = DT;
            }

            public DateTime TimeStamp { get; private set; }
            public double[] AVGHashrates;
            public double[] AVGTemperatures;
            public double[] AVGFanspeeds;
            public int[] ShAccepted;
            public int[] ShRejected;
            public int[] ShInvalid;
        }
        private static List<MinerInfo> LInfo = new List<MinerInfo>();

        #endregion

        private static int MsCycle = 1000;

        static double[] derr;
        private static void ErrorGethashrate()
        {
            MW.context.Send(MW.Sethashrate, null);
            if (Info != null)
            {
                if (Info.Hashrates != null)
                {
                    Info = new MinerInfo(Info.Hashrates.Length);
                    derr = new double[Info.Hashrates.Length];
                    for (int i = 0; i < Info.Hashrates.Length; i++) derr[i] = -1;
                }
                else
                {
                    Info = new MinerInfo(null, null, null, null, null, null);
                    derr = null;
                }
            }
            else
            {
                Info = new MinerInfo(null, null, null, null, null, null);
                derr = null;
            }
            HashrateWachdog(derr);
        }
        public static Thread WachingThread;
        private static ThreadStart WachingClaymore = new ThreadStart(() =>
        {
            TcpClient client;
            MinerInfo MI;
            double[] Hashrates;
            int[] Temperatures;
            int[] Fanspeeds;
            int[] ShAccepted;
            int[] ShRejected;
            int[] ShInvalid;
            while (true)
            {
                try
                {
                    client = new TcpClient("127.0.0.1", 3333);
                    Byte[] data = Encoding.UTF8.GetBytes("{ \"id\":0,\"jsonrpc\":\"2.0\",\"method\":\"miner_getstat2\"}");
                    using (NetworkStream stream = client.GetStream())
                    {
                        try
                        {
                            // Отправка сообщения
                            stream.Write(data, 0, data.Length);
                            // Получение ответа
                            Byte[] readingData = new Byte[1024];
                            string responseData = string.Empty;
                            StringBuilder completeMessage = new StringBuilder();
                            int numberOfBytesRead = 0;
                            numberOfBytesRead = stream.Read(readingData, 0, readingData.Length);
                            completeMessage.AppendFormat("{0}", Encoding.UTF8.GetString(readingData, 0, numberOfBytesRead));
                            List<string> LS = JsonConvert.DeserializeObject<ClaymoreInfo>(completeMessage.ToString()).result;
                            Hashrates = JsonConvert.DeserializeObject<double[]>($"[{LS[3].Replace(";", ",")}]");
                            for (int i = 0; i < Hashrates.Length; i++)
                            {
                                Hashrates[i] = Hashrates[i] / 1000;
                            }
                            int lt = Hashrates.Length;
                            int[] xx = JsonConvert.DeserializeObject<int[]>($"[{LS[6].Replace(";", ",")}]");
                            Temperatures = new int[lt];
                            Fanspeeds = new int[lt];
                            for (int n = 0; n < xx.Length; n = n + 2)
                            {
                                Temperatures[n / 2] = (byte)xx[n];
                                Fanspeeds[n / 2] = (byte)xx[n + 1];
                            }
                            ShAccepted = JsonConvert.DeserializeObject<int[]>($"[{LS[9].Replace(";", ",")}]");
                            ShRejected = JsonConvert.DeserializeObject<int[]>($"[{LS[10].Replace(";", ",")}]");
                            ShInvalid = JsonConvert.DeserializeObject<int[]>($"[{LS[11].Replace(";", ",")}]");

                            MI = new MinerInfo(Hashrates, Temperatures, Fanspeeds, ShAccepted, ShRejected, ShInvalid);
                            Info = MI;

                            MW.context.Send(MW.Sethashrate, new object[] { MI.Hashrates, MI.Temperatures });
                            HashrateWachdog(MI.Hashrates);
                        }
                        catch
                        {
                            ErrorGethashrate();
                        }
                    }
                }
                catch
                {
                    ErrorGethashrate();
                }
                Thread.Sleep(MsCycle);
            }
        });
        private static ThreadStart WachingGminer = new ThreadStart(() =>
        {
            string content = "";
            HttpRequest request;
            MinerInfo MI;
            double[] Hashrates;
            int[] Temperatures;
            int[] ShAccepted;
            int[] ShRejected;
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

                        Hashrates = GDs.Select(GD => GD.speed).ToArray();
                        Temperatures = GDs.Select(GD => GD.temperature).ToArray();
                        ShAccepted = GDs.Select(GD => GD.accepted_shares).ToArray();
                        ShRejected = GDs.Select(GD => GD.rejected_shares).ToArray();

                        MI = new MinerInfo(Hashrates, Temperatures, null, ShAccepted, ShRejected, null);
                        Info = MI;

                        MW.context.Send(MW.Sethashrate, new object[] { MI.Hashrates, MI.Temperatures });
                        HashrateWachdog(MI.Hashrates);
                    }
                    catch
                    {
                        ErrorGethashrate();
                    }
                }
                catch
                {
                    ErrorGethashrate();
                }
                Thread.Sleep(MsCycle);
            }
        });
        private static ThreadStart WachingBminer = new ThreadStart(() =>
        {
            string content = "";
            HttpRequest request;
            MinerInfo MI;
            double[] Hashrates;
            int[] Temperatures;
            int[] Fanspeeds;
            int[] ShAccepted;
            int[] ShRejected;
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

                        Hashrates = INF.miners.Select(m => m.solver.solution_rate).ToArray();
                        Temperatures = INF.miners.Select(m => m.device.temperature).ToArray();
                        Fanspeeds = INF.miners.Select(m => m.device.fan_speed).ToArray();
                        ShAccepted = new int[] { INF.stratum.accepted_shares };
                        ShRejected = new int[] { INF.stratum.rejected_shares };

                        MI = new MinerInfo(Hashrates, Temperatures, Fanspeeds, ShAccepted, ShRejected, null);
                        Info = MI;

                        MW.context.Send(MW.Sethashrate, new object[] { MI.Hashrates, MI.Temperatures });
                        HashrateWachdog(MI.Hashrates);
                    }
                    catch
                    {
                        ErrorGethashrate();
                    }
                }
                catch
                {
                    ErrorGethashrate();
                }
                Thread.Sleep(MsCycle);
            }
        });
        private static ThreadStart TS = new ThreadStart(() => { });
        public static void StartWaching(SM.Miners? Miner)
        {
            SWT = DateTime.Now;
            if (PM.Profile.GPUsSwitch != null)
            { CardsCount = PM.Profile.GPUsSwitch.Where(x => x == true).Count(); }
            else CardsCount = 0;

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
        private static string GPUs = "";
        private static int CardsCount;
        private static bool wachdog;
        public static void HashrateWachdog(double[] Hashrates)
        {
            double TS = (DateTime.Now - (DateTime)SWT).TotalSeconds;
            if (TS < PM.Profile.TimeoutWachdog)
            {
                int ts = Convert.ToInt32(PM.Profile.TimeoutWachdog - TS);
                MW.WachdogMSG($" Полное включение вачдога через {ts} ");
                wachdog = false;
            }
            else
            {
                MW.WachdogMSG("");
                wachdog = true;
            }

            if (Hashrates == null)
            {
                StartIdleWatchdog();
                return;
            }

            if (Hashrates.Length < CardsCount)  //блок неправильного количества карт
            {
                RebootPC("Ошибка количества GPUs");
                return;
            }

            if (Hashrates.Sum() < 0)  //блок бездействия
            {
                StartIdleWatchdog();
                return;
            }

            // блок отключения вачдогов
            if (Hashrates.Sum() > 0) StopIdleWatchdog();
            if (Hashrates.Sum() > MinHashrate) StopLHWatchdog();

            // Полное включение вачдога
            if (!wachdog) return;

            if (Hashrates.Sum() == 0)  //блок нулевого хешрейта
            {
                MM.RestartMining($"Нулевой хешрейт");
                return;
            }

            {// блок падения карт
                GPUs = "";
                for (int i = 0; i < CardsCount; i++)
                {
                    if (Hashrates[i] == 0)
                    {
                        GPUs += $", {i}";
                    }
                }
                if (GPUs != "")
                {
                    GPUs = GPUs.TrimStart(',');
                    MM.RestartMining($"Отвал GPUs:{GPUs}");
                    return;
                }
            }

            if (Hashrates.Sum() < MinHashrate)  //блок низкого хешрейта
            {
                StartLHWatchdog();
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
                        MW.WriteGeneralLog($"Интернет потерян, остановка работы");
                        MW.context.Send(MM.KillProcess, null);
                        StopIdleWatchdog();
                        StopLHWatchdog();
                    }
                    else
                    {
                        MW.WriteGeneralLog($"Интернет воостановлен, возобновление работы");
                        InformMessage($"Интернет воостановлен, возобновление работы");
                        MW.context.Send(MM.StartLastMiner, null);
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
        private static ThreadStart IdleWatchdogTS = new ThreadStart(() =>
        {
            for (int i = PM.Profile.TimeoutIdle; i > 0; i--)
            {
                MW.IdlewachdogMSG($" Бездействие, перезагрузка через {i} ");
                Thread.Sleep(1000);
            }
            RebootPC("Бездействие");
            Thread.CurrentThread.Abort();
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
            while (true)
            {
                try
                {
                    IdleWatchdogThread.Abort();
                    MW.IdlewachdogMSG("");
                    break;
                }
                catch { }
            }
        }

        private static Thread LowHashrateThread = new Thread(new ThreadStart(() => { }));
        private static ThreadStart LowHashrateTS = new ThreadStart(() =>
        {
            for (int i = PM.Profile.TimeoutLH; i > 0; i--)
            {
                MW.LowHwachdogMSG($" Низкий хешрейт, перезапуск через {i} ");
                Thread.Sleep(1000);
            }
            MM.RestartMining($"Низкий хешрейт");
            MW.LowHwachdogMSG("");
            Thread.CurrentThread.Abort();
        });
        public static void StartLHWatchdog()
        {
            if (LowHashrateThread.IsAlive) return;
            try
            {
                LowHashrateThread.Abort();
            }
            catch { }
            LowHashrateThread = new Thread(LowHashrateTS);
            LowHashrateThread.Start();
        }
        public static void StopLHWatchdog()
        {
            while (true)
            {
                try
                {
                    LowHashrateThread.Abort();
                    MW.LowHwachdogMSG("");
                    break;
                }
                catch { }
            }
        }

        public static void RebootPC(string cause)
        {
            MW.WriteGeneralLog($"{cause}, перезагрузка");
            InformMessage($"{cause}, перезагрузка");
            Thread RebootPCThread = new Thread(new ThreadStart(() =>
            {
                MW.context.Send(MM.KillProcess, null);
                Thread.Sleep(5000);
                Process.Start("shutdown", "/r /t 0");
                Application.Current.Shutdown();
                Thread.CurrentThread.Abort();
            }));
            RebootPCThread.Start();
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
            public MinerInfo(double[] Hashrates, int[] Temperatures, int[] Fanspeeds, int[] ShAccepted, int[] ShRejected, int[] ShInvalid)
            {
                this.Hashrates = Hashrates;
                this.Temperatures = Temperatures;
                this.Fanspeeds = Fanspeeds;
                this.ShAccepted = ShAccepted;
                this.ShRejected = ShRejected;
                this.ShInvalid = ShInvalid;
                TimeStamp = DateTime.Now;
            }
            public MinerInfo(DateTime DT, int i, MinerInfo MI)
            {
                Hashrates = new double[i];
                Temperatures = new int[i];
                Fanspeeds = new int[i];
                ShAccepted = MI.ShAccepted;
                ShRejected = MI.ShRejected;
                ShInvalid = MI.ShInvalid;
                TimeStamp = DT;
            }
            public MinerInfo(int i)
            {
                Hashrates = new double[i];
                Temperatures = new int[i];
                Fanspeeds = new int[i];
                ShAccepted = new int[i];
                ShRejected = new int[i];
                ShInvalid = new int[i];
                TimeStamp = DateTime.Now;
            }

            public DateTime TimeStamp { get; private set; }
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
                    try
                    {
                        pingReply = ping.Send("8.8.8.8");
                    }
                    catch { }
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