using OMineGuard.Backend;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OMineGuard.Miners
{
    public abstract class Miner
    {
        //abstract
        private protected abstract string Directory { get; set; }
        private protected abstract string ProcessName { get; set; }
        private protected abstract Process miner { get; set; }

        private protected abstract void RunThisMiner(Backend.Config Config);
        private protected abstract MinerInfo CurrentMinerGetInfo();

        //events
        public event Action<long> MinerStarted;
        public event Action MinerStoped;
        public event Action<string> LogDataReceived;
        public event Action<MinerInfo> MinerInfoUpdated;
        public event Action<int> WachdogDelayTimer;
        public event Action GPUsQuantityError;
        public event Action<int> InactivityTimer;
        public event Action InactivityError;
        public event Action<int> LowHashrateTimer;
        public event Action LowHashrateError;
        public event Action<List<int>> GPUsfalled;

        private protected Task MinerStartedInvoke(string log)
        {
            return Task.Run(() => LogDataReceived?.Invoke(log));
        }

        //common
        public bool Processed { get {
                return miner != null ? true : false;
            } }
        private protected List<bool> GPUs;
        private protected void KillMiner()
        {
            if (miner != null)
            {
                miner.Kill();
            }
            foreach (Process proc in Process.GetProcessesByName(ProcessName))
            {
                proc.Kill();
            }
        }
        public Task StartMiner(Config config)
        {
            return Task.Run(() =>
            {
                KillMiner();

                RunThisMiner(config);
                GPUs = Settings.Profile.GPUsSwitch;
                Task.Run(() => MinerStarted?.Invoke(config.ID));

                StartWaching(config);

                miner.WaitForExit();
                miner = null;
                Task.Run(() => MinerStoped?.Invoke());
            });
        }
        public void StopMiner()
        {
            Task.Run(() => 
            {
                KillMiner();
                Waching = false;
                Task.Run(() => MinerStoped?.Invoke());
            });
        }
        public MinerInfo GetMinerInfo()
        {
            MinerInfo MI;
            if (!Processed) MI = new MinerInfo();
            MI = CurrentMinerGetInfo();
            _ = Task.Run(() => MinerInfoUpdated?.Invoke(MI));
            return MI;
        }

        private object WachingKey = new object();
        private object InactivityKey = new object();
        private object LowHashrateKey = new object();
        private bool waching = false;
        private bool inactivity = false;
        private bool lowHashrate = false;
        private bool Waching
        {
            get { lock (WachingKey) return waching; }
            set { lock (WachingKey) waching = value; }
        }
        private bool Inactivity
        {
            get { lock (InactivityKey) return inactivity; }
            set { lock (InactivityKey) inactivity = value; }
        }
        private bool LowHashrate
        {
            get { lock (LowHashrateKey) return lowHashrate; }
            set { lock (LowHashrateKey) lowHashrate = value; }
        }
        private Task StartWaching(Config config)
        {
            return Task.Run(() =>
            {
                Waching = true;
                double[] hashes;
                double[] activehashes;
                for (int i = Settings.Profile.TimeoutWachdog; i > -1; i--)
                {
                    if (!Waching) return;
                    GetMinerInfo();
                    _ = Task.Run(() => WachdogDelayTimer?.Invoke(i));
                    Task.Delay(1000);
                }
                while (Waching)
                {
                    Task.Run(() =>
                    {
                        hashes = GetMinerInfo().Hashrates;
                        activehashes = hashes.Where(h => h > -1).ToArray();
                        if (hashes != null)
                        {
                            if (hashes.Contains(-2)) //неправильное количество карт
                            { Task.Run(() => GPUsQuantityError?.Invoke()); }

                            //бездаействие
                            if (activehashes.Sum() == 0)
                            {
                                WachdogInactivity();
                            }
                            else Inactivity = false;

                            // низкий хешрейт
                            if (activehashes.Sum() < config.MinHashrate)
                            {
                                WachdogLowHashrate();
                            }
                            else LowHashrate = false;

                            //отвал карт
                            if (hashes.Contains(0))
                            {
                                List<int> gpus = new List<int>();
                                for (int i = 0; i < hashes.Length; i++)
                                {
                                    if (hashes[i] == 0) gpus.Add(i);
                                }
                                Task.Run(() => GPUsfalled?.Invoke(gpus));
                            }
                            return;
                        }
                        WachdogInactivity();
                    });
                    Task.Delay(1000);
                }
            });
        }
        private Task WachdogInactivity()
        {
            return Task.Run(() =>
            {
                for (int i = Settings.Profile.TimeoutIdle; i > 0; i--)
                {
                    if (!Inactivity) goto Normal;
                    Task.Run(() => InactivityTimer?.Invoke(i));
                    Task.Delay(1000);
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
        private Task WachdogLowHashrate()
        {
            return Task.Run(() =>
            {
                for (int i = Settings.Profile.TimeoutLH; i > 0; i--)
                {
                    if (!LowHashrate) goto Normal;
                    Task.Run(() => LowHashrateTimer?.Invoke(i));
                    Task.Delay(1000);
                }
                if (!LowHashrate) goto Normal;
                Task.Run(() => LowHashrateTimer?.Invoke(0));
                Task.Run(() => LowHashrateError?.Invoke());
                return;
            Normal:
                Task.Run(() => LowHashrateTimer?.Invoke(-1));
                return;
            });
        }

        //const
        private protected const int port = 3330;
        private protected const string LogFolder = "MinersLogs";
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
        public static readonly List<string> Miners = new List<string>
        {
            "Bminer",
            "Claymore",
            "Gminer"
        };
    }

    public struct MinerInfo
    {
        public MinerInfo(List<double> Hashrates, List<int> Temperatures, 
            List<int> Fanspeeds, List<int> ShAccepted, List<int> ShRejected, List<int> ShInvalid)
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
            TimeStamp = DateTime.Now;
        }
        public MinerInfo(List<double> Hashrates, List<int> Temperatures,
            List<int> Fanspeeds, int? ShTotalAccepted, int? ShTotalRejected, int? ShTotalInvalid)
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
            TimeStamp = DateTime.Now;
        }

        public DateTime TimeStamp { get; private set; }
        public double[] Hashrates;
        public int[] Temperatures;
        public int[] Fanspeeds;
        public int[] ShAccepted;
        public int? ShTotalAccepted;
        public int[] ShRejected;
        public int? ShTotalRejected;
        public int[] ShInvalid;
        public int? ShTotalInvalid;
    }
}
