using Newtonsoft.Json;
using OMineGuard.Backend.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
                                        OMWsendState(client, stream, (_model.Profile, ContolStateType.Profile));
                                        OMWsendState(client, stream, (_model.Algoritms, ContolStateType.Algoritms));
                                        OMWsendState(client, stream, (_model.Miners, ContolStateType.Miners));
                                        OMWsendState(client, stream, (_model.DefClock, ContolStateType.DefClock));
                                        OMWsendState(client, stream, (_model.Indicator, ContolStateType.Indication));
                                        OMWsendState(client, stream, (_model.Log, ContolStateType.Logging));

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
                                                    StateServerActive = true;
                                                    while (statclient.Connected && ServerAlive)
                                                    {
                                                        if (StateQueue.Count > 0)
                                                        {
                                                            OMWsendState(client, stream, StateQueue.Dequeue());
                                                        }
                                                        Thread.Sleep(100);
                                                    }
                                                    StateServerActive = false;
                                                    StateQueue.Clear();
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
        private static void OMWsendState(TcpClient client, NetworkStream stream, (object, ContolStateType) o)
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
                            switch (o.Item2)
                            {
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

                            string msg = $"{{\"{header}\":{JsonConvert.SerializeObject(o.Item2)}}}";

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
        private static void OMWsendInform(TcpClient client, NetworkStream stream, (object, InformStateType) o)
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
                            switch (o.Item2)
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

                            string msg = $"{{\"{header}\":{JsonConvert.SerializeObject(o.Item1)}}}";

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
                case "Loggong": { SendContolState(_model.Loggong, ContolStateType.Logging); } break;
                case "GPUs": { SendContolState(_model.GPUs, ContolStateType.GPUs); break; }
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
    public enum ContolStateType
    {
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
