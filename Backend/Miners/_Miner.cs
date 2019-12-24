using OMineGuard.Backend;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace OMineGuard.Miners
{
    public abstract class Miner
    {
        //abstract
        private protected abstract string Directory { get; set; }
        private protected abstract string ProcessName { get; set; }
        
        private protected abstract void RunThisMiner(Config Config);
        private protected abstract MinerInfo CurrentMinerGetInfo();

        //events
        public static event Action<string> LogDataReceived;
        private protected static void Logging(string log)
        {
            Task.Run(() => LogDataReceived?.Invoke(log));
        }
        public static event Action<Config, bool> MinerStarted;
        public static event Action MinerStoped;
        public static event Action<MinerInfo> MinerInfoUpdated;
        public static event Action<int> WachdogDelayTimer;
        public static event Action<int> InactivityTimer;
        public static event Action<Miner> ZeroHash;
        public static event Action InactivityError;
        public static event Action<int> LowHashrateTimer;
        public static event Action<Miner> LowHashrateError;
        public static event Action<Miner, int[]> GPUsfalled;

        //common
        public Miner()
        {
            InternetConnectionWacher.InternetConnectionLost += () => 
            { Task.Run(() => Ending()); };
            InternetConnectionWacher.InternetConnectionRestored += () =>
            {
                if (ConfigToRecovery != null)
                {
                    StartMiner(ConfigToRecovery, true);
                }
            };
        }
        private Config ConfigToRecovery;
        public static bool Processing => 
            miner != null ? true : false;
        private protected void KillMiner()
        {
            if (miner != null)
            {
                miner.Kill();
                miner = null;
            }
            foreach (Process proc in Process.GetProcessesByName(ProcessName))
            {
                try
                {
                    proc.Kill();
                }
                catch { }
            }
        }
        public void StartMiner(Config config, bool InternetRestored = false)
        {
            Task.Run(() =>
            {
                KillMiner();

                RunThisMiner(config);
                GPUs = Settings.Profile.GPUsSwitch;
                Task.Run(() => MinerStarted?.Invoke(config, InternetRestored));
                ConfigToRecovery = config;

                WachdogInactivity();
                ErrorsCounter = 0;
                StartWaching(config);

                miner.WaitForExit();
                miner = null;
                Task.Run(() => MinerStoped?.Invoke());
            });
        }
        public void RestartMiner()
        {
            Task.Run(() => 
            {
                if (Processing && ConfigToRecovery != null)
                {
                    StopMiner();
                    StartMiner(ConfigToRecovery);
                }
            });
        }
        public void StopMiner()
        {
            Task.Run(() => 
            {
                inactivity = false;
                InactivityTimer?.Invoke(-1);
                Ending();
                ConfigToRecovery = null;
            });
        }
        public void ExxtraStopMiner()
        {
            inactivity = false;
            InactivityTimer?.Invoke(-1);
            if (miner != null)
            {
                miner.Kill();
                miner = null;
            }
            foreach (Process proc in Process.GetProcessesByName(ProcessName))
            {
                try
                {
                    proc.Kill();
                }
                catch { }
            }
            Waching = false;
            WachdogDelayTimer?.Invoke(-1);
            LowHashrate = false;
            LowHashrateTimer?.Invoke(-1);
            ConfigToRecovery = null;
        }
        private void Ending()
        {
            KillMiner();
            Waching = false;
            WachdogDelayTimer?.Invoke(-1);
            LowHashrate = false;
            LowHashrateTimer?.Invoke(-1);
            Task.Run(() => MinerStoped?.Invoke());
        }
        public MinerInfo GetMinerInfo()
        {
            MinerInfo MI;
            if (!Processing) MI = new MinerInfo();
            MI = CurrentMinerGetInfo();
            Task.Run(() => MinerInfoUpdated?.Invoke(MI));
            return MI;
        }

        private static protected Process miner { get; set; }
        private static readonly object WachingKey = new object();
        private static readonly object InactivityKey = new object();
        private static readonly object LowHashrateKey = new object();
        private static bool waching = false;
        private static bool inactivity = false;
        private static bool lowHashrate = false;
        private static bool Waching
        {
            get { lock (WachingKey) return waching; }
            set { lock (WachingKey) waching = value; }
        }
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
        private void StartWaching(Config config)
        {
            Task.Run(() =>
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
                            if (activehashes.Sum() > config.MinHashrate)
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
                        activehashes = hashes.Where(h => h != null);
                        if (hashes != null)
                        {
                            //бездаействие
                            if (activehashes.Sum() == 0)
                            {
                                ErrorsCounter++;
                                if (ErrorsCounter > 4)
                                {
                                    Task.Run(() => ZeroHash?.Invoke(this));
                                    WachdogInactivity();
                                }
                            }
                            else
                            {
                                // низкий хешрейт
                                if (activehashes.Sum() < config.MinHashrate)
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
                                    }
                                }
                            }
                            return;
                        }
                        WachdogInactivity();
                    });
                    Thread.Sleep(1000);
                }
            });
        }
        private static void WachdogInactivity()
        {
            if (!Inactivity)
            {
                Inactivity = true;

                Task.Run(() =>
                {

                    for (int i = Settings.Profile.TimeoutIdle; i > 0; i--)
                    {
                        if (!Inactivity) goto Normal;
                        Task.Run(() => { if (Processing) InactivityTimer?.Invoke(i); });
                        Thread.Sleep(1000);
                    }
                    if (!Inactivity) goto Normal;
                    Task.Run(() => { if (Processing) InactivityTimer?.Invoke(0); });
                    Task.Run(() => InactivityError?.Invoke());
                    return;
                Normal:
                    Task.Run(() => { if (Processing) InactivityTimer?.Invoke(-1); });
                    return;
                });
            }
        }
        private static void WachdogLowHashrate(Miner miner)
        {
            if (!LowHashrate)
            {
                LowHashrate = true;

                Task.Run(() =>
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
        }

        //const
        public static List<bool> GPUs;
        private protected const int port = 3330;
        private protected const string LogFolder = "MinersLogs";
        public static List<string> Miners { get; private set; } = new List<string>
        {
            "Bminer",
            "Claymore",
            "Gminer"
        };
        public static Dictionary<string, int[]> Algoritms { get; private set; } =
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
