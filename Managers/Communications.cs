using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PM = OMineManager.ProfileManager;
using IM = OMineManager.InformManager;
using MW = OMineManager.MainWindow;
using MM = OMineManager.MinersManager;
using System.Diagnostics;
using System.Windows;
using System;

namespace OMineManager
{
    public static class TCPserver
    {
        #region MSG
        private static TcpListener MSGserver = new TcpListener(IPAddress.Any, 2111);
        public static Thread MSGServerThread;
        public static ThreadStart MSGServerTS = new ThreadStart(() =>
        {
            while (true)
            {
                try
                {
                    MSGserver.Start();
                    break;
                }
                catch { }
            }
            while (true)
            {
                try
                {
                    MSGClientService(MSGserver.AcceptTcpClient());
                }
                catch { }
            }
        });
        public static void ServerStart()
        {
            try
            {
                MSGServerThread.Abort();
            }
            catch { }
            MSGServerThread = new Thread(MSGServerTS);
            MSGServerThread.Start();
        }
        static List<Thread> MSGClientServiceThreads = new List<Thread>();
        static void MSGClientService(TcpClient client)
        {
            Thread ClientThread = new Thread(new ThreadStart(() =>
            {
                int MessageLength = 100;
                string JSON;
                byte[] array;
                using (NetworkStream stream = client.GetStream())
                {
                    stream.Write(new byte[] { 1, 2, 3 }, 0, 3);
                    DateTime DT = DateTime.Now;
                    while ((DateTime.Now - DT).TotalSeconds < 60 * 3)  // пока клиент подключен, ждем приходящие сообщения
                    {
                        byte[] msg = new byte[MessageLength];     // готовим место для принятия сообщения
                        int count = stream.Read(msg, 0, msg.Length);   // читаем сообщение от клиента
                        if (count > 0)
                        {
                            DT = DateTime.Now;
                            string request = Encoding.Default.GetString(msg, 0, count);
                            ClientRequest CR = JsonConvert.DeserializeObject<ClientRequest>(request);
                            switch (CR.header)
                            {
                                case "set message length":
                                    {
                                        MessageLength = JsonConvert.DeserializeObject<int>(CR.body);
                                    }
                                    break;
                                case "get profile":
                                    {
                                        JSON = JsonConvert.SerializeObject(PM.Profile);
                                        array = Encoding.Default.GetBytes(JSON);
                                        stream.Write(array, 0, array.Length);
                                    }
                                    break;
                                case "set profile":
                                    {
                                        PM.Profile = JsonConvert.DeserializeObject<Profile>(CR.body);
                                        PM.SaveProfile();
                                        MW.UpdateProfile();
                                        MessageLength = 100;
                                    }
                                    break;
                                case "get info":
                                    {
                                        JSON = JsonConvert.SerializeObject(IM.Info);
                                        array = Encoding.Default.GetBytes(JSON);
                                        stream.Write(array, 0, array.Length);
                                    }
                                    break;
                                case "restart pc":
                                    {
                                        MW.WriteGeneralLog("Перезагрузка, запрошенная по API");
                                        IM.InformMessage("Перезагрузка, запрошенная по API");
                                        Task.Run(() =>
                                        {
                                            MW.context.Send(MM.KillProcess, null);
                                            Thread.Sleep(5000);
                                            Process.Start("shutdown", "/r /t 0");
                                            Application.Current.Shutdown();
                                        });
                                    }
                                    break;
                                case "close stream":
                                    {
                                        DT -= new TimeSpan(0, 5, 0);
                                    }
                                    break;
                            }
                        }
                        Thread.Sleep(100);
                    }
                    stream.Write(new byte[] { 4, 5, 6 }, 0, 3);
                }  //dispose stream
                MSGClientServiceThreads.Remove(Thread.CurrentThread);
                Thread.CurrentThread.Abort();
            }));
            MSGClientServiceThreads.Add(ClientThread);
            ClientThread.Start();
        }

        public static void AbortTCP()
        {
            try
            {
                MSGserver.Stop();
            }
            catch { }
            try
            {
                MSGServerThread.Abort();
                INFServerThread.Abort();
            }
            catch { }
            foreach (Thread T in MSGClientServiceThreads)
            {
                try
                {
                    T.Abort();
                }
                catch { }
            }
        }

        public class ClientRequest
        {
            public string header;
            public string body;
        }
        #endregion

        #region INF
        private static TcpListener INFserver = new TcpListener(IPAddress.Any, 2112);
        public static Thread INFServerThread;
        public static void INFServerStart()
        {
            try
            {
                INFServerThread.Abort();

            }
            catch { }
            INFServerThread = new Thread(INFServerTS);
            INFServerThread.Start();
        }
        public static ThreadStart INFServerTS = new ThreadStart(() =>
        {
            while (true)
            {
                try
                {
                    INFserver.Start();
                    break;
                }
                catch { }
            }
            while (true)
            {
                try
                {
                    INFstreams.Add(INFserver.AcceptTcpClient().GetStream());
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
