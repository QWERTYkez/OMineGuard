﻿using OMineGuard.Backend;
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
        private protected abstract string Directory { get; }
        private protected abstract string ProcessName { get; }
        private protected abstract void RunThisMiner(IConfig Config);
        private protected abstract MinerInfo CurrentMinerGetInfo();

        public event Action<IConfig, bool> MinerStarted;
        public event Action MinerStoped;
        public event Action<MinerInfo> MinerInfoUpdated;
        public event Action<int> WachdogDelayTimer;
        public static event Action<int> InactivityTimer;
        public event Action ZeroHash;
        public static event Action InactivityError;
        public event Action<int> LowHashrateTimer;
        public event Action LowHashrateError;
        public event Action<int[]> GPUsfalled;
        public event Action<string> LogDataReceived;

        private protected Process process { get; set; }
        public List<bool> GPUs { get; private set; }
        private byte ErrorsCounter;
        private readonly object WachingKey = new object();
        private static readonly object InactivityKey = new object();
        private readonly object LowHashrateKey = new object();
        private static bool inactivity = false;
        private bool lowHashrate = false;
        private bool waching = false;
        private bool Waching
        {
            get { lock (WachingKey) return waching; }
            set { lock (WachingKey) waching = value; }
        }
        private static bool Inactivity
        {
            get { lock (InactivityKey) return inactivity; }
            set { lock (InactivityKey) inactivity = value; }
        }
        private bool LowHashrate
        {
            get { lock (LowHashrateKey) return lowHashrate; }
            set { lock (LowHashrateKey) lowHashrate = value; }
        }

        public static Miner GetMiner(IConfig config)
        {
            switch (config.Miner.Value)
            {
                case 0: return new Bminer();
                case 1: return new Claymore();
                case 2: return new Gminer();
                case 3: return new PhoenixMiner();
                default: return null;
            }
        }
        public Task StartMiner(IConfig config, bool InternetRestored = false)
        {
            return Task.Run(() =>
            {
                KillMiner();

                RunThisMiner(config);
                GPUs = Settings.Profile.GPUsSwitch;
                Task.Run(() => MinerStarted?.Invoke(config, InternetRestored));

                ErrorsCounter = 0;
                StartWaching(config.MinHashrate);

                process.WaitForExit();
                process = null;
                Task.Run(() => MinerStoped?.Invoke());
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
            {
                try { proc.Kill(); } catch { }
            }
        }
        public Task StopMiner(bool manually = false)
        {
            return Task.Run(() => 
            {
                if (manually)
                {
                    inactivity = false;
                    InactivityTimer?.Invoke(-1);
                }
                
                Waching = false;
                WachdogDelayTimer?.Invoke(-1);
                LowHashrate = false;
                LowHashrateTimer?.Invoke(-1);

                //clear events
                LogDataReceived = null;
                MinerInfoUpdated = null;
                MinerStarted = null;
                MinerStoped = null;
                //InactivityTimer = null;
                LowHashrateTimer = null;
                WachdogDelayTimer = null;
                ZeroHash = null;
                GPUsfalled = null;
                //InactivityError = null;
                LowHashrateError = null;

                try { process?.Kill(); }
                catch
                {
                    try { process?.Kill(); }
                    catch
                    {
                        try { process?.Kill(); } 
                        catch { }
                    }
                }
            });
        }
        private static Task WachdogInactivity(Miner miner)
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
                    miner.Waching = false;
                    return;
                Normal:
                    Task.Run(() => InactivityTimer?.Invoke(-1));
                    return;
                });
            }
            return null;
        }
        private MinerInfo GetMinerInfo()
        {
            var MI = CurrentMinerGetInfo();
            Task.Run(() => MinerInfoUpdated?.Invoke(MI));
            return MI;
        }
        private Task StartWaching(double minhash)
        {
            return Task.Run(() =>
            {
                WachdogInactivity(this);
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
                    Task.Run(() => { WachdogDelayTimer?.Invoke(i); });
                    Thread.Sleep(1000);
                }
                Action WachdogLoop = (() =>
                {
                    hashes = GetMinerInfo().Hashrates;
                    if (hashes != null)
                    {
                        activehashes = hashes.Where(h => h != null);
                        //Zero
                        if (activehashes.Sum() == 0)
                        {
                            ErrorsCounter++;
                            if (ErrorsCounter > 4)
                            {
                                Task.Run(() => ZeroHash?.Invoke());
                                WachdogInactivity(this);
                                Waching = false;
                                return;
                            }
                        }
                        else
                        {
                            // низкий хешрейт
                            if (activehashes.Sum() < minhash)
                            {
                                WachdogLowHashrate();
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
                                    Task.Run(() => GPUsfalled?.Invoke(gpus.ToArray()));
                                    Waching = false;
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
                            Task.Run(() => ZeroHash?.Invoke());
                            WachdogInactivity(this);
                            Waching = false;
                            return;
                        }
                    }
                    WachdogInactivity(this);
                });
                while (Waching)
                {
                    var WachResult = WachdogLoop.BeginInvoke(null, null);
                    Thread.Sleep(1000);
                    WachdogLoop.EndInvoke(WachResult);
                }
            });
        }
        private Task WachdogLowHashrate()
        {
            if (!LowHashrate)
            {
                LowHashrate = true;

                return Task.Run(() =>
                {
                    for (int i = Settings.Profile.TimeoutLH; i > 0; i--)
                    {
                        if (!LowHashrate) goto Normal;
                        Task.Run(() => { LowHashrateTimer?.Invoke(i); });
                        Thread.Sleep(1000);
                    }
                    if (!LowHashrate) goto Normal;
                    Task.Run(() => { LowHashrateTimer?.Invoke(0); });
                    Task.Run(() => LowHashrateError?.Invoke());
                    Waching = false;
                    return;
                Normal:
                    Task.Run(() => { LowHashrateTimer?.Invoke(-1); });
                    return;
                });
            }
            return null;
        }
        private protected void Logging(string log)
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
            "Gminer",
            "PhoenixMiner"
        };
        public static readonly Dictionary<string, int[]> Algoritms =
            new Dictionary<string, int[]>
            {
                { "BeamHash II",
                     new int[] { Miners.IndexOf("Gminer") } },
                 { "Ethash",
                     new int[] { Miners.IndexOf("Bminer"), Miners.IndexOf("Claymore"), Miners.IndexOf("PhoenixMiner") } },
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