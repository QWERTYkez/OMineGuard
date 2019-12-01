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
        //private protected abstract
        private protected abstract string Directory { get; set; }
        private protected abstract string ProcessName { get; set; }
        private protected abstract Process miner { get; set; }
        private protected abstract void RunThisMiner(Backend.Config Config);

        //common
        public Miner()
        {
            MinerStarted += l => { Processed = true; };
            MinerStoped += () => { Processed = false; };
        }

        private protected static string LogFolder = "MinersLogs";

        public Action<long> MinerStarted;
        public Action MinerStoped;
        public Action<string> LogDataReceived;

        public bool Processed = false;

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
        public Task StartMiner(Backend.Config Config)
        {
            return Task.Run(async () =>
            {
                await KillMiner();

                RunThisMiner(Config);
                _ = Task.Run(() => MinerStarted?.Invoke(Config.ID));

                //IM.StartWaching(Config.Miner);
                //IM.StartInternetWachdog();

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

        //static
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
}
