//using Newtonsoft.Json;
//using System.Collections.Generic;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using PM = OMineGuard.ProfileManager;
//using IM = OMineGuard.InformManager;
//using MW = OMineGuard.MainWindow;
//using MM = OMineGuard.MinersManager;
//using OCM = OMineGuard.OverclockManager;
//using SM = OMineGuard.SettingsManager;
//using System;
//using System.Windows.Documents;

//namespace OMineGuard
//{
//    public static class TCPserver
//    {
//        public static event Action<RootObject> OMWsent;

//        private static bool ServerAlive = true;
//        private static TcpListener Server1 = new TcpListener(IPAddress.Any, 2111);
//        private static TcpListener Server2 = new TcpListener(IPAddress.Any, 2112);
//        private static TcpListener Server3 = new TcpListener(IPAddress.Any, 2113);
//        public static void ServersStop()
//        {
//            ServerAlive = false;
//            Server1.Stop();
//            Server2.Stop();
//            Server3.Stop();
//        }

//        #region MSG
//        public static void ServerStart()
//        {
//            Task.Run(() =>
//            {
//                while (ServerAlive)
//                {
//                    try
//                    {
//                        Server2.Start();
//                        break;
//                    }
//                    catch { }
//                    Thread.Sleep(100);
//                }
//                while (ServerAlive)
//                {
//                    try
//                    {
//                        OMWcontrol(Server2.AcceptTcpClient());
//                    }
//                    catch { }
//                    Thread.Sleep(100);
//                }
//            });

//            Task.Run(() =>
//            {
//                while (ServerAlive)
//                {
//                    try
//                    {
//                        Server1.Start();
//                        break;
//                    }
//                    catch { }
//                    Thread.Sleep(100);
//                }
//                while (ServerAlive)
//                {
//                    try
//                    {
//                        OMWinforming(Server1.AcceptTcpClient());
//                    }
//                    catch { }
//                    Thread.Sleep(100);
//                }
//            });
//        }

//        static void OMWcontrol(TcpClient client)
//        {
//            Task.Run(() => 
//            {
//                using (NetworkStream stream = client.GetStream())
//                {
//                    try
//                    {
//                        BaseSendMSG(client, stream, 
//                            PM.Profile, 
//                            OMWcontrolType.Profile);

//                        BaseSendMSG(client, stream, 
//                            SM.MinersD, 
//                            OMWcontrolType.Algoritms);

//                        BaseSendMSG(client, stream, 
//                            new string[] { SM.Miners.Bminer.ToString(), SM.Miners.Claymore.ToString(), SM.Miners.Gminer.ToString() }, 
//                            OMWcontrolType.Miners);

//                        BaseSendMSG(client, stream,
//                            OCM.DC,
//                            OMWcontrolType.DefClock);

//                        BaseSendMSG(client, stream,
//                            MM.Indication,
//                            OMWcontrolType.Indication);

//                        BaseSendMSG(client, stream, 
//                            (new TextRange(MW.This.MinerLog.Document.ContentStart, MW.This.MinerLog.Document.ContentEnd)).Text, 
//                            OMWcontrolType.Log);

//                        // Отправление статистики
//                        Task.Run(() =>
//                        {
//                            while (ServerAlive)
//                            {
//                                try
//                                {
//                                    Server3.Start();
//                                    break;
//                                }
//                                catch { }
//                                Thread.Sleep(100);
//                            }
//                            while (ServerAlive)
//                            {
//                                try
//                                {
//                                    OMWstate(Server3.AcceptTcpClient());
//                                }
//                                catch { }
//                                Thread.Sleep(100);
//                            }

//                        });
//                    }
//                    catch { }

//                    while (client.Connected && ServerAlive)
//                    {
//                        RootObject RO;
//                        try
//                        {
//                            RO = JsonConvert.DeserializeObject<RootObject>(OMWreadMSG(stream));
//                            OMWsent?.Invoke(RO);
//                        }
//                        catch { }
//                        Thread.Sleep(50);
//                    }
//                }
//            });
//        }
//        #region send msg
//        private static object key1 = new object();
//        public static void BaseSendMSG(TcpClient client, NetworkStream stream, object body, OMWcontrolType type)
//        {
//            if (client != null)
//            {
//                if (client.Connected)
//                {
//                    lock (key1)
//                    {
//                        string header = "";
//                        string msg = "";
//                        switch (type)
//                        {
//                            case OMWcontrolType.Profile: header = "Profile"; break;
//                            case OMWcontrolType.Miners: header = "Miners"; break;
//                            case OMWcontrolType.Algoritms: header = "Algoritms"; break;
//                            case OMWcontrolType.Log: header = "Logging"; break;
//                            case OMWcontrolType.DefClock: header = "DefClock"; break;
//                            case OMWcontrolType.Indication: header = "Indication"; break;
//                        }

//                        if (header == "Algoritms")
//                        {
//                            msg = JsonConvert.SerializeObject(body);
//                        }
//                        else
//                        {
//                            msg = $"{{\"{header}\":{JsonConvert.SerializeObject(body)}}}";
//                        }
                        
//                        byte[] Message = Encoding.Default.GetBytes(msg);
//                        byte[] Header = BitConverter.GetBytes(Message.Length);

//                        stream.Write(Header, 0, Header.Length);

//                        byte[] b = new byte[1];
//                        stream.Read(b, 0, b.Length);

//                        stream.Write(Message, 0, Message.Length);
//                    }
//                }
//            }
//        }
//        public enum OMWcontrolType
//        {
//            Profile,
//            Miners,
//            Algoritms,
//            Log,
//            DefClock,
//            Indication
//        }
//        #endregion
//        #region read msg
//        private static string OMWreadMSG(NetworkStream stream)
//        {
//            byte[] msg = new byte[4];
//            stream.Read(msg, 0, msg.Length);
//            int MSGlength = BitConverter.ToInt32(msg, 0);

//            stream.Write(new byte[] { 1 }, 0, 1);

//            msg = new byte[MSGlength];
//            int count = stream.Read(msg, 0, msg.Length);
//            return Encoding.Default.GetString(msg, 0, count);
//        }
//        public class RootObject
//        {
//            public Profile Profile { get; set; }
//            public long? RunConfig { get; set; }
//            public long? ApplyClock { get; set; }
//            public bool? StartProcess { get; set; }
//            public bool? KillProcess { get; set; }
//            public bool? ShowMinerLog { get; set; }
//        }
//        #endregion
//        #region send state
//        private static TcpClient OMWstateClient;
//        private static NetworkStream OMWstateStream;
//        static void OMWstate(TcpClient client)
//        {
//            Task.Run(() =>
//            {
//                try
//                {
//                    OMWstateClient.Close();
//                    OMWstateClient.Dispose();
//                }
//                catch { }
//                OMWstateClient = client;
//                OMWstateStream = OMWstateClient.GetStream();
//            });
//        }
//        private static object key2 = new object();
//        public static void OMWsendState(object body, OMWstateType type)
//        {
//            if (OMWstateClient != null)
//            {
//                if (OMWstateClient.Connected)
//                {
//                    lock (key2)
//                    {
//                        try
//                        {
//                            string header = "";
//                            switch (type)
//                            {
//                                case OMWstateType.Hashrates:    header = "Hashrates";   break;
//                                case OMWstateType.Overclock:   header = "Overclock";  break;
//                                case OMWstateType.Indication:  header = "Indication"; break;
//                                case OMWstateType.Logging:     header = "Logging";    break;
//                                case OMWstateType.WachdogInfo: header = "WachdogInfo"; break;
//                                case OMWstateType.LowHWachdog: header = "LowHWachdog"; break;
//                                case OMWstateType.IdleWachdog: header = "IdleWachdog"; break;
//                                case OMWstateType.ShowMLogTB: header = "ShowMLogTB"; break;
//                                case OMWstateType.DefClock: header = "DefClock"; break;
//                                case OMWstateType.Temperatures: header = "Temperatures"; break;
//                            }

//                            string msg = $"{{\"{header}\":{JsonConvert.SerializeObject(body)}}}";

//                            byte[] Message = Encoding.Default.GetBytes(msg);
//                            byte[] Header = BitConverter.GetBytes(Message.Length);

//                            OMWstateStream.Write(Header, 0, Header.Length);

//                            byte[] b = new byte[1];
//                            OMWstateStream.Read(b, 0, b.Length);

//                            OMWstateStream.Write(Message, 0, Message.Length);
//                        }
//                        catch { }
//                    }
//                }
//            }
//        }
//        public enum OMWstateType
//        {
//            Hashrates,
//            Overclock,
//            Indication,
//            Logging,
//            WachdogInfo,
//            LowHWachdog,
//            IdleWachdog,
//            ShowMLogTB,
//            DefClock,
//            Temperatures
//        }
//        #endregion
//        #endregion


//        private static TcpClient OMWinformingClient;
//        private static NetworkStream OMWinformingStream;
//        static void OMWinforming(TcpClient client)
//        {
//            Task.Run(() =>
//            {
//                try
//                {
//                    OMWinformingClient.Close();
//                    OMWinformingClient.Dispose();
//                }
//                catch { }

//                NetworkStream stream = client.GetStream();

//                //Стартовые сообщения
//                {
//                    OMWsendInform(client, stream,
//                            MM.Indication,
//                            OMWinformType.Indication);
//                }

//                OMWinformingClient = client;
//                OMWinformingStream = stream;
//            });
//        }
//        private static object key3 = new object();
//        public static void OMWsendInform(object body, OMWinformType type)
//        {
//            if (OMWinformingClient != null)
//            {
//                if (OMWinformingClient.Connected)
//                {
//                    lock (key3)
//                    {
//                        try
//                        {
//                            string header = "";
//                            switch (type)
//                            {
//                                case OMWinformType.Hashrates: header = "Hashrates"; break;
//                                //case OMWinformType.Overclock: header = "Overclock"; break;
//                                case OMWinformType.Indication: header = "Indication"; break;
//                                //case OMWinformType.Logging: header = "Logging"; break;
//                                //case OMWinformType.WachdogInfo: header = "WachdogInfo"; break;
//                                //case OMWinformType.LowHWachdog: header = "LowHWachdog"; break;
//                                //case OMWinformType.IdleWachdog: header = "IdleWachdog"; break;
//                                //case OMWinformType.ShowMLogTB: header = "ShowMLogTB"; break;
//                                //case OMWinformType.DefClock: header = "DefClock"; break;
//                                case OMWinformType.Temperatures: header = "Temperatures"; break;
//                            }

//                            string msg = $"{{\"{header}\":{JsonConvert.SerializeObject(body)}}}";

//                            byte[] Message = Encoding.Default.GetBytes(msg);
//                            byte[] Header = BitConverter.GetBytes(Message.Length);

//                            OMWinformingStream.Write(Header, 0, Header.Length);

//                            byte[] b = new byte[1];
//                            OMWinformingStream.Read(b, 0, b.Length);

//                            OMWinformingStream.Write(Message, 0, Message.Length);
//                        }
//                        catch { }
//                    }
//                }
//            }
//        }
//        public static void OMWsendInform(TcpClient client, NetworkStream stream, object body, OMWinformType type)
//        {
//            if (client != null)
//            {
//                if (client.Connected)
//                {
//                    lock (key3)
//                    {
//                        try
//                        {
//                            string header = "";
//                            switch (type)
//                            {
//                                case OMWinformType.Hashrates: header = "Hashrates"; break;
//                                //case OMWinformType.Overclock: header = "Overclock"; break;
//                                case OMWinformType.Indication: header = "Indication"; break;
//                                //case OMWinformType.Logging: header = "Logging"; break;
//                                //case OMWinformType.WachdogInfo: header = "WachdogInfo"; break;
//                                //case OMWinformType.LowHWachdog: header = "LowHWachdog"; break;
//                                //case OMWinformType.IdleWachdog: header = "IdleWachdog"; break;
//                                //case OMWinformType.ShowMLogTB: header = "ShowMLogTB"; break;
//                                //case OMWinformType.DefClock: header = "DefClock"; break;
//                                case OMWinformType.Temperatures: header = "Temperatures"; break;
//                            }

//                            string msg = $"{{\"{header}\":{JsonConvert.SerializeObject(body)}}}";

//                            byte[] Message = Encoding.Default.GetBytes(msg);
//                            byte[] Header = BitConverter.GetBytes(Message.Length);

//                            stream.Write(Header, 0, Header.Length);

//                            byte[] b = new byte[1];
//                            stream.Read(b, 0, b.Length);

//                            stream.Write(Message, 0, Message.Length);
//                        }
//                        catch { }
//                    }
//                }
//            }
//        }
//        public enum OMWinformType
//        {
//            Hashrates,
//            Temperatures,
//            Indication

//            //Overclock,
//            //Logging,
//            //WachdogInfo,
//            //LowHWachdog,
//            //IdleWachdog,
//            //ShowMLogTB,
//            //DefClock
//        }
//    }
//}
