using OMineGuard.Backend;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using OMineGuardControlLibrary;

namespace OMineGuard.Miners
{
    public abstract class Miner
    {
        //---------------instance
        //abstract
        private protected abstract string Directory { get; }
        private protected abstract string ProcessName { get; }
        private protected abstract void RunThisMiner(IConfig Config);
        private protected abstract MinerInfo CurrentMinerGetInfo();
        //common
        private readonly object InternetConnectionKey = new object();
        private bool? InternetMinerSwitch = null;
        public Miner()
        {
            InternetConnectionWacher.InternetConnectionLost += () =>
            {
                if (InternetMinerSwitch == null) InternetMinerSwitch = false;
                lock (InternetConnectionKey)
                {
                    if (InternetMinerSwitch.Value)
                    {
                        Task.Run(() => Ending());
                        InternetMinerSwitch = false;
                    }
                }
            };
            InternetConnectionWacher.InternetConnectionRestored += () =>
            {
                lock (InternetConnectionKey)
                {
                    if (!InternetMinerSwitch.Value)
                    {
                        StartMiner(ConfigToRecovery, true);
                        InternetMinerSwitch = true;
                    }  
                }
            };
        }

        public Task RestartMiner()
        {
            return Task.Run(() =>
            {
                if (ConfigToRecovery != null)
                {
                    Ending();
                    StartMiner(ConfigToRecovery);
                }
            });
        }
        public void StopMiner()
        {
            inactivity = false;
            InactivityTimer?.Invoke(-1);
            Ending();
        }
        private void Ending()
        {
            KillMiner();
            Waching = false;
            WachdogDelayTimer?.Invoke(-1);
            LowHashrate = false;
            LowHashrateTimer?.Invoke(-1);
        }
        private MinerInfo GetMinerInfo()
        {
            MinerInfo MI;
            if (!Processing) MI = new MinerInfo();
            MI = CurrentMinerGetInfo();
            Task.Run(() => MinerInfoUpdated?.Invoke(MI));
            return MI;
        }
        private Task StartWaching(double minhash)
        {
            return Task.Run(() =>
            {
                Waching = true;
                double?[] hashes;
                IEnumerable<double?> activehashes;
                for (int i = Settings.Profile.TimeoutWachdog; i > -1; i--)
                {
                    if (!Waching) return;
                    Task.Run(() =>
                    {
                        try
                        {
                            activehashes = GetMinerInfo().Hashrates.Where(h => h != null);
                            if (activehashes.Sum() > minhash)
                            {
                                inactivity = false;
                                InactivityTimer?.Invoke(-1);
                            }
                        }
                        catch { }
                    });
                    Task.Run(() => { if (Processing) WachdogDelayTimer?.Invoke(i); });
                    Thread.Sleep(1000);
                }
                while (Waching)
                {
                    Task.Run(() =>
                    {
                        hashes = GetMinerInfo().Hashrates;
                        if (hashes != null)
                        {
                            activehashes = hashes.Where(h => h != null);
                            //бездаействие
                            if (activehashes.Sum() == 0)
                            {
                                ErrorsCounter++;
                                if (ErrorsCounter > 4)
                                {
                                    Task.Run(() => ZeroHash?.Invoke(this));
                                    WachdogInactivity();
                                    return;
                                }
                            }
                            else
                            {
                                // низкий хешрейт
                                if (activehashes.Sum() < minhash)
                                {
                                    WachdogLowHashrate(this);
                                }
                                else
                                {
                                    // блок хорошего поведения
                                    LowHashrate = false;
                                    Inactivity = false;
                                    ErrorsCounter = 0;
                                }

                                //отвал карт
                                if (hashes.Contains(0))
                                {
                                    ErrorsCounter++;
                                    if (ErrorsCounter > 4)
                                    {
                                        List<int> gpus = new List<int>();
                                        for (int i = 0; i < hashes.Length; i++)
                                        {
                                            if (hashes[i] == 0) gpus.Add(i);
                                        }
                                        Task.Run(() => GPUsfalled?.Invoke(this, gpus.ToArray()));
                                        return;
                                    }
                                }
                            }
                            return;
                        }
                        else
                        {
                            //бездаействие
                            ErrorsCounter++;
                            if (ErrorsCounter > 4)
                            {
                                Task.Run(() => ZeroHash?.Invoke(this));
                                WachdogInactivity();
                                return;
                            }
                        }
                        WachdogInactivity();
                    });
                    Thread.Sleep(1000);
                }
            });
        }
        private bool waching = false;
        private bool Waching
        {
            get { lock (WachingKey) return waching; }
            set { lock (WachingKey) waching = value; }
        }

        //---------------static
        //events
        public static event Action<Miner, IConfig, bool> MinerStarted;
        public static event Action MinerStoped;
        public static event Action<MinerInfo> MinerInfoUpdated;
        public static event Action<int> WachdogDelayTimer;
        public static event Action<int> InactivityTimer;
        public static event Action<Miner> ZeroHash;
        public static event Action InactivityError;
        public static event Action<int> LowHashrateTimer;
        public static event Action<Miner> LowHashrateError;
        public static event Action<Miner, int[]> GPUsfalled;
        public static event Action<string> LogDataReceived;
        //common
        public static bool Processing => process != null ? true : false;
        public static List<bool> GPUs { get; private set; }
        private static protected Process process { get; set; }
        private static IConfig ConfigToRecovery { get; set; }
        private static readonly object WachingKey = new object();
        private static readonly object InactivityKey = new object();
        private static readonly object LowHashrateKey = new object();
        private static bool inactivity = false;
        private static bool lowHashrate = false;
        private static bool Inactivity
        {
            get { lock (InactivityKey) return inactivity; }
            set { lock (InactivityKey) inactivity = value; }
        }
        private static bool LowHashrate
        {
            get { lock (LowHashrateKey) return lowHashrate; }
            set { lock (LowHashrateKey) lowHashrate = value; }
        }
        private static byte ErrorsCounter = 0;
        private static Thread MinerLaunchThread;
        //methods
        public static Task StartMiner(IConfig config, bool InternetRestored = false)
        {
            return Task.Run(() =>
            {
                if (MinerLaunchThread != null) try { MinerLaunchThread.Abort(); } catch { }
                MinerLaunchThread = new Thread(() => 
                {
                    Thread.Sleep(5000);

                    KillMiner();

                    Miner miner;
                    switch (config.Miner.Value)
                    {
                        case 0: miner = new Bminer(); break;
                        case 1: miner = new Claymore(); break;
                        case 2: miner = new Gminer(); break;
                        default: return;
                    }
                    miner.RunThisMiner(config);
                    GPUs = Settings.Profile.GPUsSwitch;
                    Task.Run(() => MinerStarted?.Invoke(miner, config, InternetRestored));
                    ConfigToRecovery = config;

                    ErrorsCounter = 0;
                    WachdogInactivity();
                    miner.StartWaching(config.MinHashrate);

                    process.WaitForExit();
                    process = null;
                    Task.Run(() => MinerStoped?.Invoke());
                });
                MinerLaunchThread.Start();
            });
        }
        private static void KillMiner()
        {
            var ProcessNames = new string[]
            {
                Bminer.CurrentProcessName,
                Gminer.CurrentProcessName,
                Claymore.CurrentProcessName
            };
            var processes = Process.GetProcesses().
                Where(p => ProcessNames.Contains(p.ProcessName));
            foreach (var proc in processes)
                try { proc.Kill(); } catch { }
        }
        private static Task WachdogInactivity()
        {
            if (!Inactivity)
            {
                Inactivity = true;

                return Task.Run(() =>
                {
                    for (int i = Settings.Profile.TimeoutIdle; i > 0; i--)
                    {
                        if (!Inactivity) goto Normal;
                        Task.Run(() => InactivityTimer?.Invoke(i));
                        Thread.Sleep(1000);
                    }
                    if (!Inactivity) goto Normal;
                    Task.Run(() => InactivityTimer?.Invoke(0));
                    Task.Run(() => InactivityError?.Invoke());
                    return;
                Normal:
                    Task.Run(() => InactivityTimer?.Invoke(-1));
                    return;
                });
            }
            return null;
        }
        private static Task WachdogLowHashrate(Miner miner)
        {
            if (!LowHashrate)
            {
                LowHashrate = true;

                return Task.Run(() =>
                {
                    for (int i = Settings.Profile.TimeoutLH; i > 0; i--)
                    {
                        if (!LowHashrate) goto Normal;
                        Task.Run(() => { if (Processing) LowHashrateTimer?.Invoke(i); });
                        Thread.Sleep(1000);
                    }
                    if (!LowHashrate) goto Normal;
                    Task.Run(() => { if (Processing) LowHashrateTimer?.Invoke(0); });
                    Task.Run(() => LowHashrateError?.Invoke(miner));
                    return;
                Normal:
                    Task.Run(() => { if (Processing) LowHashrateTimer?.Invoke(-1); });
                    return;
                });
            }
            return null;
        }
        private protected static void Logging(string log)
        {
            Task.Run(() => LogDataReceived?.Invoke(log));
        }
        //const
        private protected const int port = 3330;
        private protected const string LogFolder = "MinersLogs";
        public static readonly List<string> Miners = new List<string>
        {
            "Bminer",
            "Claymore",
            "Gminer"
        };
        public static readonly Dictionary<string, int[]> Algoritms =
            new Dictionary<string, int[]>
            {
                { "BeamHash II",
                     new int[] { Miners.IndexOf("Gminer") } },
                 { "Ethash",
                     new int[] { Miners.IndexOf("Bminer"), Miners.IndexOf("Claymore") } },
                { "Equihash 96.5",
                     new int[] { Miners.IndexOf("Gminer") } },
                 { "Equihash 144.5",
                     new int[] { Miners.IndexOf("Bminer"), Miners.IndexOf("Gminer") } },
                 { "Equihash 150.5",
                     new int[] { Miners.IndexOf("Bminer"), Miners.IndexOf("Gminer") } },
                 { "Equihash 192.7",
                     new int[] { Miners.IndexOf("Gminer") } },
                 { "Equihash 200.9",
                     new int[] { Miners.IndexOf("Bminer") } },
                 { "Equihash 210.9",
                    new int[] { Miners.IndexOf("Gminer") } },
                { "cuckARoo29",
                    new int[] { Miners.IndexOf("Bminer"), Miners.IndexOf("Gminer") } },
                 { "cuckAToo31",
                     new int[] { Miners.IndexOf("Bminer"), Miners.IndexOf("Gminer") } },
                 { "CuckooCycle",
                     new int[] { Miners.IndexOf("Bminer"), Miners.IndexOf("Gminer") } },
                 { "Tensority",
                     new int[] { Miners.IndexOf("Bminer") } },
                { "Zhash",
                     new int[] { Miners.IndexOf("Bminer") } }
            };
    }

    public struct MinerInfo
    {
        public MinerInfo(List<double?> Hashrates, List<int?> Temperatures, 
            List<int?> Fanspeeds, List<int?> ShAccepted, List<int?> ShRejected, List<int?> ShInvalid)
        {
            this.Hashrates = Hashrates.ToArray();
            this.Temperatures = Temperatures.ToArray();
            this.Fanspeeds = Fanspeeds.ToArray();
            this.ShAccepted = ShAccepted.ToArray();
            this.ShRejected = ShRejected.ToArray();
            this.ShInvalid = ShInvalid.ToArray();
            this.ShTotalAccepted = null;
            this.ShTotalRejected = null;
            this.ShTotalInvalid = null;
        }
        public MinerInfo(List<double?> Hashrates, List<int?> Temperatures,
            List<int?> Fanspeeds, int? ShTotalAccepted, int? ShTotalRejected, int? ShTotalInvalid)
        {
            this.Hashrates = Hashrates.ToArray();
            this.Temperatures = Temperatures.ToArray();
            this.Fanspeeds = Fanspeeds.ToArray();
            this.ShTotalAccepted = ShTotalAccepted;
            this.ShTotalRejected = ShTotalRejected;
            this.ShTotalInvalid = ShTotalInvalid;
            this.ShAccepted = null;
            this.ShRejected = null;
            this.ShInvalid = null;
        }

        public double?[] Hashrates;
        public int?[] Temperatures;
        public int?[] Fanspeeds;
        public int?[] ShAccepted;
        public int? ShTotalAccepted;
        public int?[] ShRejected;
        public int? ShTotalRejected;
        public int?[] ShInvalid;
        public int? ShTotalInvalid;
    }
}
