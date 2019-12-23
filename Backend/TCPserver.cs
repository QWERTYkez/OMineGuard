using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OMineGuard.Backend
{
    public static class TCPserver
    {
        public static event Action<RootObject> OMWsent;
        public static bool Indication = false;
        private static DefClock DC = null;

        private static bool ServerAlive { get; set; } = true;
        private static TcpListener Server1;
        private static TcpListener Server2;
        private static TcpListener Server3;

        public static void _TCPserver()
        {
            Task.Run(() => 
            {
                Overclocker.ConnectedToMSI += def => DC = def;
                Task.Run(() =>
                {
                    while (ServerAlive) 
                    {
                        try 
                        {
                            Server1 = new TcpListener(IPAddress.Any, 2111);
                            Server1.Stop();
                            Server1.Start();
                            break;
                        } 
                        catch { } 
                        Thread.Sleep(100); 
                    }
                    try 
                    { 
                        Server1.BeginAcceptTcpClient(new AsyncCallback(OMWinforming), Server1); 
                    } 
                    catch { }
                });
                Task.Run(() =>
                {
                    while (ServerAlive) 
                    { 
                        try 
                        {
                            Server2 = new TcpListener(IPAddress.Any, 2112);
                            Server2.Stop();
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
                            using (TcpClient client = Server2.AcceptTcpClient())
                            {
                                using (NetworkStream stream = client.GetStream())
                                {
                                    try
                                    {
                                        SendMessage(client, stream, Settings.Profile, MSGtype.Profile);
                                        SendMessage(client, stream, Miners.Miner.Algoritms, MSGtype.Algoritms);
                                        SendMessage(client, stream, Miners.Miner.Miners, MSGtype.Miners);
                                        SendMessage(client, stream, DC, MSGtype.DefClock);
                                        SendMessage(client, stream, Indication, MSGtype.Indication);
                                        SendMessage(client, stream, Models.MainModel.Log, MSGtype.Log);

                                        // Отправление статистики
                                        Task.Run(() =>
                                        {
                                            while (ServerAlive) 
                                            { 
                                                try 
                                                {
                                                    Server3 = new TcpListener(IPAddress.Any, 2113);
                                                    Server3.Stop();
                                                    Server3.Start();
                                                    break; 
                                                } 
                                                catch { } 
                                                Thread.Sleep(100); 
                                            }
                                            using (TcpClient statclient = Server3.AcceptTcpClient())
                                            {
                                                using (NetworkStream statstream = statclient.GetStream())
                                                {
                                                    object[] o;
                                                    while (statclient.Connected && ServerAlive)
                                                    {
                                                        if (StateQueue.Count > 0)
                                                        {
                                                            o = StateQueue.Dequeue();
                                                            OMWsendState(client, stream, o[0], (ContolStateType)o[1]);
                                                        }
                                                        Thread.Sleep(100);
                                                    }
                                                }
                                            }
                                            Server3.Stop();
                                        });
                                    }
                                    catch { }
                                    while (client.Connected && ServerAlive)
                                    {
                                        RootObject RO;
                                        try
                                        {
                                            RO = JsonConvert.DeserializeObject<RootObject>(ReadMessage(stream));
                                            Task.Run(() => OMWsent?.Invoke(RO));
                                        }
                                        catch { }
                                        Thread.Sleep(100);
                                    }
                                }
                            }
                        }
                        catch { }
                        Thread.Sleep(100);
                    }
                });
            });
        }
        private static void OMWinforming(IAsyncResult ar)
        {
            if (ServerAlive)
                Task.Run(() => Server1.BeginAcceptTcpClient(new AsyncCallback(OMWinforming), Server1));
            try
            {
                using (TcpClient client = Server1.EndAcceptTcpClient(ar))
                {
                    using (NetworkStream stream = client.GetStream())
                    {
                        //Стартовые сообщения
                        OMWsendInform(client, stream, Indication, InformStateType.Indication);

                        object[] o;
                        while (client.Connected && ServerAlive)
                        {
                            if (InformQueue.Count > 0)
                            {
                                o = InformQueue.Dequeue();
                                OMWsendState(client, stream, o[0], (ContolStateType)o[1]);
                            }
                            Thread.Sleep(100);
                        }
                    }
                }
            }
            catch { }
        }

        private static readonly Queue<object[]> StateQueue = new Queue<object[]>();
        private static readonly Queue<object[]> InformQueue = new Queue<object[]>();
        public static void SendContolState(object body, ContolStateType type)
        {
            StateQueue.Enqueue(new object[] { body, type });
        }
        public static void SendInformState(object body, InformStateType type)
        {
            InformQueue.Enqueue(new object[] { body, type });
        }
        public static void StopServers()
        {
            ServerAlive = false;
            Server1.Stop();
            Server2.Stop();
            if (Server3 != null)
            {
                Server3.Stop();
            }
        }

        private static string ReadMessage(NetworkStream stream)
        {
            byte[] msg = new byte[4];
            stream.Read(msg, 0, msg.Length);
            int MSGlength = BitConverter.ToInt32(msg, 0);

            stream.Write(new byte[] { 1 }, 0, 1);

            msg = new byte[MSGlength];
            int count = stream.Read(msg, 0, msg.Length);
            return Encoding.Default.GetString(msg, 0, count);
        }
        private static readonly object key = new object();
        private static readonly object key2 = new object();
        private static readonly object key3 = new object();
        private static void SendMessage(TcpClient client, NetworkStream stream, object body, MSGtype type)
        {
            Task.Run(() =>
            {
                if (client != null)
                {
                    if (client.Connected)
                    {
                        lock (key)
                        {
                            string header = "";
                            switch (type)
                            {
                                case MSGtype.Algoritms: header = "Algoritms"; break;
                                case MSGtype.DefClock: header = "DefClock"; break;
                                case MSGtype.Indication: header = "Indication"; break;
                                case MSGtype.Log: header = "Log"; break;
                                case MSGtype.Miners: header = "Miners"; break;
                                case MSGtype.Profile: header = "Profile"; break;
                            }

                            string msg = $"{{\"{header}\":{JsonConvert.SerializeObject(body)}}}";

                            byte[] Message = Encoding.Default.GetBytes(msg);
                            byte[] Header = BitConverter.GetBytes(Message.Length);

                            stream.Write(Header, 0, Header.Length);

                            byte[] b = new byte[1];
                            stream.Read(b, 0, b.Length);

                            stream.Write(Message, 0, Message.Length);
                        }
                    }
                }
            });
        }
        private static void OMWsendState(TcpClient client, NetworkStream stream, object body, ContolStateType type)
        {
            Task.Run(() =>
            {
                if (client != null)
                {
                    if (client.Connected)
                    {
                        lock (key2)
                        {
                            string header = "";
                            switch (type)
                            {
                                case ContolStateType.Hashrates: header = "Hashrates"; break;
                                case ContolStateType.Overclock: header = "Overclock"; break;
                                case ContolStateType.Indication: header = "Indication"; break;
                                case ContolStateType.Logging: header = "Logging"; break;
                                case ContolStateType.WachdogInfo: header = "WachdogInfo"; break;
                                case ContolStateType.LowHWachdog: header = "LowHWachdog"; break;
                                case ContolStateType.IdleWachdog: header = "IdleWachdog"; break;
                                case ContolStateType.ShowMLogTB: header = "ShowMLogTB"; break;
                                case ContolStateType.DefClock: header = "DefClock"; break;
                                case ContolStateType.Temperatures: header = "Temperatures"; break;
                            }

                            string msg = $"{{\"{header}\":{JsonConvert.SerializeObject(body)}}}";

                            byte[] Message = Encoding.Default.GetBytes(msg);
                            byte[] Header = BitConverter.GetBytes(Message.Length);

                            stream.Write(Header, 0, Header.Length);

                            byte[] b = new byte[1];
                            stream.Read(b, 0, b.Length);

                            stream.Write(Message, 0, Message.Length);
                        }
                    }
                }
            });
        }
        private static void OMWsendInform(TcpClient client, NetworkStream stream, object body, InformStateType type)
        {
            Task.Run(() =>
            {
                if (client != null)
                {
                    if (client.Connected)
                    {
                        lock (key3)
                        {
                            string header = "";
                            switch (type)
                            {
                                case InformStateType.Hashrates: header = "Hashrates"; break;
                                //case InformStateType.Overclock: header = "Overclock"; break;
                                case InformStateType.Indication: header = "Indication"; break;
                                //case InformStateType.Logging: header = "Logging"; break;
                                //case InformStateType.WachdogInfo: header = "WachdogInfo"; break;
                                //case InformStateType.LowHWachdog: header = "LowHWachdog"; break;
                                //case InformStateType.IdleWachdog: header = "IdleWachdog"; break;
                                //case InformStateType.ShowMLogTB: header = "ShowMLogTB"; break;
                                //case InformStateType.DefClock: header = "DefClock"; break;
                                case InformStateType.Temperatures: header = "Temperatures"; break;
                            }

                            string msg = $"{{\"{header}\":{JsonConvert.SerializeObject(body)}}}";

                            byte[] Message = Encoding.Default.GetBytes(msg);
                            byte[] Header = BitConverter.GetBytes(Message.Length);

                            stream.Write(Header, 0, Header.Length);

                            byte[] b = new byte[1];
                            stream.Read(b, 0, b.Length);

                            stream.Write(Message, 0, Message.Length);
                        }
                    }
                }
            });
        }
    }
    public class RootObject
    {
        public Profile Profile { get; set; }
        public object[] RunConfig { get; set; }
        public object[] ApplyClock { get; set; }
        public bool? SwitchProcess { get; set; }
        public bool? ShowMinerLog { get; set; }
    }
    public enum MSGtype
    {
        Algoritms,
        DefClock,
        Indication,
        Log,
        Miners,
        Profile
    }
    public enum ContolStateType
    {
        DefClock,
        Hashrates,
        IdleWachdog,
        Indication,
        Logging,
        LowHWachdog,
        Overclock,
        ShowMLogTB,
        Temperatures,
        WachdogInfo
    }
    public enum InformStateType
    {
        Hashrates,
        Indication,
        Temperatures
    }
}
