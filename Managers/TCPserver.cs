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
        static Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


        private static TcpListener server = new TcpListener(IPAddress.Any, 2111);
        public static Thread ServerThread;
        public static ThreadStart ServerTS = new ThreadStart(() =>
        {
            while (true)
            {
                try
                {
                    server.Start();
                    break;
                }
                catch { }
            }
            while (true)
            {
                try
                {
                    ClientService(server.AcceptTcpClient());
                }
                catch { }
            }
        });
        public static void ServerStart()
        {
            try
            {
                ServerThread.Abort();
            }
            catch { }
            ServerThread = new Thread(ServerTS);
            ServerThread.Start();
        }
        static List<Thread> ClientServiceThreads = new List<Thread>();
        static void ClientService(TcpClient client)
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
                ClientServiceThreads.Remove(Thread.CurrentThread);
                Thread.CurrentThread.Abort();
            }));
            ClientServiceThreads.Add(ClientThread);
            ClientThread.Start();
        }

        public static void AbortTCP()
        {
            try
            {
                server.Stop();
            }
            catch { }
            try
            {
                ServerThread.Abort();
            }
            catch { }
            foreach (Thread T in ClientServiceThreads)
            {
                try
                {
                    T.Abort();
                }
                catch { }
            }
        }
    }

    public class ClientRequest
    {
        public string header;
        public string body;
    }
}
