using OMineGuard.Backend;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMineGuard.Miners
{
    public abstract class Miner
    {
        //abstract
        private protected abstract string Directory { get; set; }
        private protected abstract string ProcessName { get; set; }
        private protected abstract Process miner { get; set; }

        public abstract event Action<long> MinerStarted;
        public abstract event Action MinerStoped;
        public abstract event Action<string> LogDataReceived;

        private protected abstract void RunThisMiner(Backend.Config Config);
        private protected abstract MinerInfo? CurrentMinerGetInfo();

        //common
        public bool Processed { get { return miner != null ? true : false; } }
        private protected List<bool> GPUs;
        private protected Task KillMiner()
        {
            return Task.Run(() =>
            {
                if (miner != null)
                {
                    miner.Kill();
                }
                foreach (Process proc in Process.GetProcessesByName(ProcessName))
                {
                    proc.Kill();
                }
            });
        }
        public Task StartMiner(Config config)
        {
            return Task.Run(async () =>
            {
                await KillMiner();

                RunThisMiner(config);
                GPUs = Settings.GetProfile().GPUsSwitch;
                _ = Task.Run(() => MinerStarted?.Invoke(config.ID));

                miner.WaitForExit();
                miner = null;
                _ = Task.Run(() => MinerStoped?.Invoke());
            });
        }
        public void StopMiner()
        {
            Task.Run(async () => 
            {
                await KillMiner();
                _ = Task.Run(() => MinerStoped?.Invoke());
            });
        }
        public MinerInfo? GetMinerInfo()
        {
            if (!Processed) return null;
            return CurrentMinerGetInfo();
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
