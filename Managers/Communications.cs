using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PM = OMineGuard.ProfileManager;
using IM = OMineGuard.InformManager;
using MW = OMineGuard.MainWindow;
using MM = OMineGuard.MinersManager;
using OCM = OMineGuard.OverclockManager;
using SM = OMineGuard.SettingsManager;
using System.Diagnostics;
using System.Windows;
using System;
using System.Windows.Documents;

namespace OMineGuard
{
    public static class TCPserver
    {
        public static event Action<RootObject> OMWsent;

        private static bool ServerAlive = true;
        private static TcpListener Server1 = new TcpListener(IPAddress.Any, 2111);
        private static TcpListener Server2 = new TcpListener(IPAddress.Any, 2112);
        private static TcpListener Server3 = new TcpListener(IPAddress.Any, 2113);
        public static void ServersStop()
        {
            ServerAlive = false;
            //Server1.Stop();
            Server2.Stop();
            Server3.Stop();
        }

        #region MSG
        public static void ServerStart()
        {
            Task.Run(() =>
            {
                while (ServerAlive)
                {
                    try
                    {
                        Server2.Start();
                        break;
                    }
                    catch { }
                    Thread.Sleep(100);
                }
                while (ServerAlive)
                {
                    try
                    {
                        OMWcontrol(Server2.AcceptTcpClient());
                    }
                    catch { }
                    Thread.Sleep(100);
                }
            });
        }

        static void OMWcontrol(TcpClient client)
        {
            Task.Run(() => 
            {
                using (NetworkStream stream = client.GetStream())
                {
                    try
                    {
                        Sendcontrol(client, stream, 
                            PM.Profile, 
                            OMWcontrolType.Profile);

                        Sendcontrol(client, stream, 
                            SM.MinersD, 
                            OMWcontrolType.Algoritms);

                        Sendcontrol(client, stream, 
                            new string[] { SM.Miners.Bminer.ToString(), SM.Miners.Claymore.ToString(), SM.Miners.Gminer.ToString() }, 
                            OMWcontrolType.Miners);

                        Sendcontrol(client, stream, 
                            (new TextRange(MW.This.MinerLog.Document.ContentStart, MW.This.MinerLog.Document.ContentEnd)).Text, 
                            OMWcontrolType.Log);

                        // Отправление статистики
                        Task.Run(() =>
                        {
                            while (ServerAlive)
                            {
                                try
                                {
                                    Server3.Start();
                                    break;
                                }
                                catch { }
                                Thread.Sleep(100);
                            }
                            while (ServerAlive)
                            {
                                try
                                {
                                    OMWstate(Server3.AcceptTcpClient());
                                }
                                catch { }
                                Thread.Sleep(100);
                            }

                        });
                    }
                    catch { }

                    while (client.Connected && ServerAlive)
                    {
                        RootObject RO;
                        try
                        {
                            RO = JsonConvert.DeserializeObject<RootObject>(OMWreadMSG(stream));
                            OMWsent?.Invoke(RO);
                        }
                        catch { }
                        Thread.Sleep(50);
                    }
                }
            });
        }
        #region send msg
        private static object key1 = new object();
        public static void Sendcontrol(TcpClient client, NetworkStream stream, object body, OMWcontrolType type)
        {
            if (client != null)
            {
                if (client.Connected)
                {
                    lock (key1)
                    {
                        string header = "";
                        string msg = "";
                        switch (type)
                        {
                            case OMWcontrolType.Profile: header = "Profile"; break;
                            case OMWcontrolType.Miners: header = "Miners"; break;
                            case OMWcontrolType.Algoritms: header = "Algoritms"; break;
                            case OMWcontrolType.Log: header = "Logging"; break;
                        }

                        if (header == "Algoritms")
                        {
                            msg = JsonConvert.SerializeObject(body);
                        }
                        else
                        {
                            msg = $"{{\"{header}\":{JsonConvert.SerializeObject(body)}}}";
                        }
                        
                        byte[] Message = Encoding.Default.GetBytes(msg);
                        byte[] Header = BitConverter.GetBytes(Message.Length);

                        stream.Write(Header, 0, Header.Length);

                        byte[] b = new byte[1];
                        stream.Read(b, 0, b.Length);

                        stream.Write(Message, 0, Message.Length);
                    }
                }
            }
        }
        public enum OMWcontrolType
        {
            Profile,
            Miners,
            Algoritms,
            Log
        }
        #endregion
        #region read msg
        private static string OMWreadMSG(NetworkStream stream)
        {
            byte[] msg = new byte[4];
            stream.Read(msg, 0, msg.Length);
            int MSGlength = BitConverter.ToInt32(msg, 0);

            stream.Write(new byte[] { 1 }, 0, 1);

            msg = new byte[MSGlength];
            int count = stream.Read(msg, 0, msg.Length);
            return Encoding.Default.GetString(msg, 0, count);
        }
        public class RootObject
        {
            public Profile Profile { get; set; }
            public long? RunConfig { get; set; }
            public long? ApplyClock { get; set; }
            public bool? StartProcess { get; set; }
            public bool? KillProcess { get; set; }
            public bool? ShowMinerLog { get; set; }
        }
        #endregion
        #region send state
        private static TcpClient OMWstateClient;
        private static NetworkStream OMWstateStream;
        static void OMWstate(TcpClient client)
        {
            Task.Run(() =>
            {
                try
                {
                    OMWstateClient.Close();
                    OMWstateClient.Dispose();
                }
                catch { }
                OMWstateClient = client;
                OMWstateStream = OMWstateClient.GetStream();
            });
        }
        private static object key2 = new object();
        public static void OMWsendState(object body, OMWstateType type)
        {
            if (OMWstateClient != null)
            {
                if (OMWstateClient.Connected)
                {
                    lock (key2)
                    {
                        try
                        {
                            string header = "";
                            switch (type)
                            {
                                case OMWstateType.Hasrates:    header = "Hasrates";   break;
                                case OMWstateType.Overclock:   header = "Overclock";  break;
                                case OMWstateType.Indication:  header = "Indication"; break;
                                case OMWstateType.Logging:     header = "Logging";    break;
                                case OMWstateType.WachdogInfo: header = "WachdogInfo"; break;
                                case OMWstateType.LowHWachdog: header = "LowHWachdog"; break;
                                case OMWstateType.IdleWachdog: header = "IdleWachdog"; break;
                                case OMWstateType.ShowMLogTB: header = "ShowMLogTB"; break;
                            }

                            string msg = $"{{\"{header}\":{JsonConvert.SerializeObject(body)}}}";

                            byte[] Message = Encoding.Default.GetBytes(msg);
                            byte[] Header = BitConverter.GetBytes(Message.Length);

                            OMWstateStream.Write(Header, 0, Header.Length);

                            byte[] b = new byte[1];
                            OMWstateStream.Read(b, 0, b.Length);

                            OMWstateStream.Write(Message, 0, Message.Length);
                        }
                        catch { }
                    }
                }
            }
        }
        public enum OMWstateType
        {
            Hasrates,
            Overclock,
            Indication,
            Logging,
            WachdogInfo,
            LowHWachdog,
            IdleWachdog,
            ShowMLogTB
        }
        #endregion
        #endregion

        #region INF
        public static Thread INFServerThread;
        public static void INFServerStart()
        {
            try
            {
                INFServerThread.Abort();

            }
            catch { }
            INFServerThread = new Thread(INFServerTS);
            //INFServerThread.Start();
        }
        public static ThreadStart INFServerTS = new ThreadStart(() =>
        {
            while (true)
            {
                try
                {
                    Server1.Start();
                    break;
                }
                catch { }
            }
            while (true)
            {
                try
                {
                    INFstreams.Add(Server1.AcceptTcpClient().GetStream());
                }
                catch { }
            }
        });
        static List<NetworkStream> INFstreams = new List<NetworkStream>();
        private static List<NetworkStream> deleteList = new List<NetworkStream>();
        private static IM.AVGMinerInfo inf;
        private static object INFkey = new object();
        private static ThreadStart INFsendTS = new ThreadStart(() =>
        {
            string JS;
            byte[] arrayJS;
            string JSI;
            byte[] arrayJSI;
            string Info;
            string OClock;

            Info = JsonConvert.SerializeObject(inf);

            JSI = JsonConvert.SerializeObject(new object[] { "info", Info });
            arrayJSI = Encoding.Default.GetBytes(JSI);

            JS = JsonConvert.SerializeObject(new object[] { "js", $"{arrayJSI.Length}" });
            arrayJS = Encoding.Default.GetBytes(JS);

            lock (INFkey)
            {
                foreach (NetworkStream ns in INFstreams)
                {
                    try
                    {
                        ns.Write(arrayJS, 0, arrayJS.Length);
                        ns.Write(arrayJSI, 0, arrayJSI.Length);
                    }
                    catch (System.IO.IOException) { deleteList.Add(ns); }
                }
                while (deleteList.Count > 0)
                {
                    INFstreams.Remove(deleteList[0]);
                    deleteList.Remove(deleteList[0]);
                }
            }
            
            Thread.CurrentThread.Abort();
        });

        public static void INFsend(IM.AVGMinerInfo info)
        {
            inf = info;
            Thread th = new Thread(INFsendTS);
            th.Start();
        }
        #endregion
    }
}
