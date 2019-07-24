using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SM = OMineManager.SettingsManager;
using MW = OMineManager.MainWindow;
using xNet;

namespace OMineManager
{
    public static class InformManager
    {
        public static MinerInfo Info;
        public static Thread InformThread;
        private static int MsCycle = 1000;

        public static void StartWaching(SM.Miners? Miner)
        {
            MinerInfo Info = new MinerInfo();
            HttpRequest request;
            string content = "";
            Task.Run(() =>
            {
                InformThread = Thread.CurrentThread;
                switch (Miner)
                {
                    case SM.Miners.Claymore:
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
                    case SM.Miners.Gminer:
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
                                }
                                catch { }
                            }
                            catch { }
                            Thread.Sleep(MsCycle);
                        }
                    case SM.Miners.Bminer:
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
                                }
                                catch { }
                            }
                            catch { }
                            Thread.Sleep(MsCycle);
                        }
                }
            });
        }
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
    }
}