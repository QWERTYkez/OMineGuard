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

namespace OMineManager
{
    public static class InformManager
    {
        public static MinerInfo Info = new MinerInfo();
        public static Thread InformThread;
        private static int MsCycle = 3000;

        public class MinerInfo
        {
            public double[] Hashrates;
            public int[] Temperatures;
            public int[] Fanspeeds;
            public int[] ShAccepted;
            public int[] ShRejected;
            public int[] ShInvalid;
        }
        public static void StartWaching(SM.Miners? Miner)
        {
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
                        while (true)
                        {

                            Thread.Sleep(MsCycle);
                        }
                    case SM.Miners.Bminer:
                        while (true)
                        {

                            Thread.Sleep(MsCycle);
                        }
                }
            });
        }
        public class ClaymoreInfo
        {
            public int id { get; set; }
            public object error { get; set; }
            public List<string> result { get; set; }
        }
    }

    
}