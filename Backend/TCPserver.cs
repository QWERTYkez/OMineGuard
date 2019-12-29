using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OMineGuardControlLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OMineGuard.Backend
{
    public static class TCPserver
    {
        private class ConfigConverter : CustomCreationConverter<IConfig>
        {
            public override IConfig Create(Type objectType)
            {
                return new Config();
            }
        }
        private class OverclockConverter : CustomCreationConverter<IOverclock>
        {
            public override IOverclock Create(Type objectType)
            {
                return new Overclock();
            }
        }
        private static readonly JsonConverter[] Convs = new JsonConverter[]
        {
            new ConfigConverter(),
            new OverclockConverter()
        };

        public static event Action<RootObject> OMWsent;
        public static bool Indication = false;

        private static bool ServerAlive { get; set; } = true;
        private static TcpListener Server1;
        private static TcpListener Server2;
        private static TcpListener Server3;

        private static MainModel _model;
        public static void InitializeTCPserver(MainModel model)
        {
            _model = model;
            _model.PropertyChanged += ModelChanged;
            Task.Run(() => 
            {
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
                    try
                    {
                        if (ServerAlive)
                        {
                            using (TcpClient client = Server2.AcceptTcpClient())
                            {
                                using (NetworkStream stream = client.GetStream())
                                {
                                    try
                                    {
                                        OMWsendState(client, stream, (new ControlStruct
                                        {
                                            Profile = _model.Profile,
                                            Logging = _model.Loggong,
                                            InfPowerLimits = _model.InfPowerLimits,
                                            InfCoreClocks = _model.InfCoreClocks,
                                            InfMemoryClocks = _model.InfMemoryClocks,
                                            InfOHMCoreClocks = _model.InfOHMCoreClocks,
                                            InfOHMMemoryClocks = _model.InfOHMMemoryClocks,
                                            InfFanSpeeds = _model.InfFanSpeeds,
                                            InfTemperatures = _model.InfTemperatures,
                                            InfHashrates = _model.InfHashrates,
                                            TotalHashrate = _model.TotalHashrate,
                                            WachdogInfo = _model.WachdogInfo,
                                            LowHWachdog = _model.LowHWachdog,
                                            IdleWachdog = _model.IdleWachdog,
                                            Indication = _model.Indicator,
                                            Algoritms = _model.Algoritms,
                                            Miners = _model.Miners,
                                            DefClock = _model.DefClock,
                                        },
                                        ContolStateType.ControlStruct));
                                    }
                                    catch { }
                                    Task.Run(() =>
                                    {
                                        // Отправление статистики
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
                                        try
                                        {
                                            using (var statclient = Server3.AcceptTcpClient())
                                            {
                                                using (var statstream = statclient.GetStream())
                                                {
                                                    StateServerActive = true;
                                                    while (statclient.Connected && ServerAlive)
                                                    {
                                                        if (StateQueue.Count > 0)
                                                        {
                                                            OMWsendState(statclient, statstream, StateQueue.Dequeue());
                                                        }
                                                        Thread.Sleep(100);
                                                    }
                                                    StateServerActive = false;
                                                    StateQueue.Clear();
                                                }
                                            }
                                        }
                                        finally
                                        {
                                            Server3.Stop();
                                        }
                                    });
                                    string message;
                                    while (client.Connected && ServerAlive)
                                    {
                                        RootObject RO;

                                        try
                                        {
                                            message = ReadMessage(stream);
                                            RO = JsonConvert.DeserializeObject<RootObject>(message, Convs);
                                            if (RO != null)
                                            {
                                                Task.Run(() => OMWsent?.Invoke(RO));
                                            }
                                        }
                                        catch { }
                                        Thread.Sleep(100);
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                });
            });
        }

        private static readonly object infkey = new object();
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
                        using (InformQueue qu = new InformQueue())
                        {
                            //Стартовые сообщения
                            OMWsendInform(client, stream, (Indication, InformStateType.Indication));
                            while (client.Connected && ServerAlive)
                            {
                                lock (infkey)
                                {
                                    if (qu.CurrentQueue.Count > 0)
                                    {
                                        OMWsendInform(client, stream, qu.CurrentQueue.Dequeue());
                                    }
                                    Thread.Sleep(100);
                                }
                            }
                            qu.CurrentQueue.Clear();
                        }
                    }
                }
            }
            catch { }
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
        private static readonly object key2 = new object();
        private static readonly object key3 = new object();
        private static void OMWsendState(TcpClient client, NetworkStream stream, (object body, ContolStateType type) o)
        {
            if (client != null)
            {
                if (client.Connected)
                {
                    try
                    {
                        lock (key2)
                        {
                            string header = "";
                            switch (o.type)
                            {
                                case ContolStateType.ControlStruct: header = "ControlStruct"; break;

                                case ContolStateType.Algoritms: header = "Algoritms"; break;
                                case ContolStateType.DefClock: header = "DefClock"; break;
                                case ContolStateType.IdleWachdog: header = "IdleWachdog"; break;
                                case ContolStateType.Indication: header = "Indication"; break;
                                case ContolStateType.Logging: header = "Logging"; break;
                                case ContolStateType.LowHWachdog: header = "LowHWachdog"; break;
                                case ContolStateType.Miners: header = "Miners"; break;
                                case ContolStateType.Profile: header = "Profile"; break;
                                case ContolStateType.WachdogInfo: header = "WachdogInfo"; break;

                                case ContolStateType.GPUs: header = "GPUs"; break;
                                case ContolStateType.InfPowerLimits: header = "InfPowerLimits"; break;
                                case ContolStateType.InfCoreClocks: header = "InfCoreClocks"; break;
                                case ContolStateType.InfMemoryClocks: header = "InfMemoryClocks"; break;
                                case ContolStateType.InfOHMCoreClocks: header = "InfOHMCoreClocks"; break;
                                case ContolStateType.InfOHMMemoryClocks: header = "InfOHMMemoryClocks"; break;
                                case ContolStateType.InfFanSpeeds: header = "InfFanSpeeds"; break;
                                case ContolStateType.InfTemperatures: header = "InfTemperatures"; break;
                                case ContolStateType.InfHashrates: header = "InfHashrates"; break;
                                case ContolStateType.TotalHashrate: header = "TotalHashrate"; break;
                            }

                            string msg = $"{{\"{header}\":{JsonConvert.SerializeObject(o.body)}}}";

                            byte[] Message = Encoding.Default.GetBytes(msg);
                            byte[] Header = BitConverter.GetBytes(Message.Length);

                            stream.Write(Header, 0, Header.Length);

                            byte[] b = new byte[1];
                            stream.Read(b, 0, b.Length);

                            stream.Write(Message, 0, Message.Length);
                        }
                    }
                    catch { }
                }
            }
        }
        private static void OMWsendInform(TcpClient client, NetworkStream stream, (object body, InformStateType type) o)
        {
            if (client != null)
            {
                if (client.Connected)
                {
                    lock (key3)
                    {
                        string header = "";
                        switch (o.type)
                        {
                            case InformStateType.Indication: header = "Indication"; break;
                            case InformStateType.InfHashrates: header = "InfHashrates"; break;
                            case InformStateType.InfTemperatures: header = "InfTemperatures"; break;
                            case InformStateType.ShAccepted: header = "ShAccepted"; break;
                            case InformStateType.ShInvalid: header = "ShInvalid"; break;
                            case InformStateType.ShRejected: header = "ShRejected"; break;
                            case InformStateType.ShTotalAccepted: header = "ShTotalAccepted"; break;
                            case InformStateType.ShTotalInvalid: header = "ShTotalInvalid"; break;
                            case InformStateType.ShTotalRejected: header = "ShTotalRejected"; break;
                        }

                        string msg = $"{{\"{header}\":{JsonConvert.SerializeObject(o.body)}}}";

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

        private static bool StateServerActive = false;
        private static readonly Queue<(object, ContolStateType)> StateQueue = new Queue<(object, ContolStateType)>();
        private class InformQueue : IDisposable
        {
            public InformQueue()
            {
                Queues.Add(CurrentQueue);
            }

            public readonly Queue<(object, InformStateType)> CurrentQueue = new Queue<(object, InformStateType)>();

            private static List<Queue<(object, InformStateType)>> Queues = new List<Queue<(object, InformStateType)>>();
            public static void SendInformState(object body, InformStateType type)
            {
                foreach (var q in Queues)
                {
                    q.Enqueue((body, type));
                }
            }

            public void Dispose()
            {
                if (Queues.Contains(CurrentQueue))
                    Queues.Remove(CurrentQueue);
            }
        }
        private static void SendContolState(object body, ContolStateType type)
        {
            if (StateServerActive)
            {
                StateQueue.Enqueue((body, type));
            }
        }
        public static void SendInformState(object body, InformStateType type)
        {
            InformQueue.SendInformState(body, type);
        }

        private static void ModelChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Profile":
                    {
                        if (_model.Profile != null)
                        {
                            SendContolState(_model.Profile, ContolStateType.Profile);
                        }
                        break;
                    }
                case "Loggong": { SendContolState(_model.Loggong, ContolStateType.Logging); break; }
                case "InfPowerLimits": { SendContolState(_model.InfPowerLimits, ContolStateType.InfPowerLimits); break; }
                case "InfCoreClocks": { SendContolState(_model.InfCoreClocks, ContolStateType.InfCoreClocks); break; }
                case "InfMemoryClocks": { SendContolState(_model.InfMemoryClocks, ContolStateType.InfMemoryClocks); break; }
                case "InfOHMCoreClocks": { SendContolState(_model.InfOHMCoreClocks, ContolStateType.InfOHMCoreClocks); break; }
                case "InfOHMMemoryClocks": { SendContolState(_model.InfOHMMemoryClocks, ContolStateType.InfOHMMemoryClocks); break; }
                case "InfFanSpeeds": { SendContolState(_model.InfFanSpeeds, ContolStateType.InfFanSpeeds); break; }
                case "InfTemperatures":
                    {
                        SendContolState(_model.InfTemperatures, ContolStateType.InfTemperatures);
                        SendInformState(_model.InfTemperatures, InformStateType.InfTemperatures);
                        break;
                    }
                case "InfHashrates":
                    {
                        SendContolState(_model.InfHashrates, ContolStateType.InfHashrates);
                        SendInformState(_model.InfHashrates, InformStateType.InfHashrates);
                        break;
                    }
                case "TotalHashrate": { SendContolState(_model.TotalHashrate, ContolStateType.TotalHashrate); break; }
                case "WachdogInfo": { SendContolState(_model.WachdogInfo, ContolStateType.WachdogInfo); } break;
                case "LowHWachdog": { SendContolState(_model.LowHWachdog, ContolStateType.LowHWachdog); } break;
                case "IdleWachdog": { SendContolState(_model.IdleWachdog, ContolStateType.IdleWachdog); } break;
                case "Indicator":
                    {
                        SendContolState(_model.Indicator, ContolStateType.Indication);
                        SendInformState(_model.Indicator, InformStateType.Indication);
                    }
                    break;
                case "ShAccepted": { SendInformState(_model.ShAccepted, InformStateType.ShAccepted); } break;
                case "ShInvalid": { SendInformState(_model.ShInvalid, InformStateType.ShInvalid); } break;
                case "ShRejected": { SendInformState(_model.ShRejected, InformStateType.ShRejected); } break;
                case "ShTotalAccepted": { SendInformState(_model.ShTotalAccepted, InformStateType.ShTotalAccepted); } break;
                case "ShTotalInvalid": { SendInformState(_model.ShTotalInvalid, InformStateType.ShTotalInvalid); } break;
                case "ShTotalRejected": { SendInformState(_model.ShTotalRejected, InformStateType.ShTotalRejected); } break;
            }
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
    public struct ControlStruct
    {
        public IProfile Profile { get; set; }
        public IDefClock DefClock { get; set; }
        public string Logging { get; set; }
        public bool? Indication { get; set; }
        public List<string> Miners { get; set; }
        public Dictionary<string, int[]> Algoritms { get; set; }
        public string WachdogInfo { get; set; }
        public string LowHWachdog { get; set; }
        public string IdleWachdog { get; set; }

        public int? GPUs { get; set; }
        public int?[] InfPowerLimits { get; set; }
        public int?[] InfCoreClocks { get; set; }
        public int?[] InfMemoryClocks { get; set; }
        public int?[] InfOHMCoreClocks { get; set; }
        public int?[] InfOHMMemoryClocks { get; set; }
        public int?[] InfFanSpeeds { get; set; }
        public int?[] InfTemperatures { get; set; }
        public double?[] InfHashrates { get; set; }
        public double? TotalHashrate { get; set; }
    }
    public enum ContolStateType
    {
        ControlStruct,

        Profile,
        Logging,
        InfPowerLimits,
        InfCoreClocks,
        InfMemoryClocks,
        InfOHMCoreClocks,
        InfOHMMemoryClocks,
        InfFanSpeeds,
        InfTemperatures,
        InfHashrates,
        TotalHashrate,
        WachdogInfo,
        LowHWachdog,
        IdleWachdog,
        Indication,
        Algoritms,
        Miners,
        GPUs,
        DefClock
    }
    public enum InformStateType
    {
        ShAccepted,
        ShInvalid,
        ShRejected,
        ShTotalAccepted,
        ShTotalInvalid,
        ShTotalRejected,

        InfHashrates,
        InfTemperatures,
        Indication
    }
}
