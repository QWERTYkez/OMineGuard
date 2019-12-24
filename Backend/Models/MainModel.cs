using OMineGuard.Backend;
using OMineGuard.Miners;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OMineGuard.Backend.Models
{
    public class MainModel : INotifyPropertyChanged
    {
        public event Action Autostarted;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private Profile CurrProf;
        public void IniModel()
        {
            TCPserver.OMWsent += TCP_OMWsent;
            TCPserver.InitializeTCPserver(this);

            Informer.SendMessage("OMineGuard запущен");

            Overclocker.OverclockReceived += (msi, ohm) => 
            { 
                var xx = msi;
                if (xx != null)
                {
                    if (!MSIenable) MSIenable = true;

                    InfPowerLimits = xx.Value.PowerLimits;
                    InfCoreClocks = xx.Value.CoreClocks;
                    InfMemoryClocks = xx.Value.MemoryClocks;
                    if (!OHMenable) InfFanSpeeds = xx.Value.FanSpeeds;
                }
                else
                {
                    if (MSIenable) MSIenable = false;

                    InfPowerLimits = null;
                    InfCoreClocks = null;
                    InfMemoryClocks = null;
                    if (!MIenable && !OHMenable) InfFanSpeeds = null;
                }

                var yy = ohm;
                if (yy != null)
                {
                    if (!OHMenable) OHMenable = true;

                    InfOHMCoreClocks = yy.Value.CoreClocks;
                    InfOHMMemoryClocks = yy.Value.MemoryClocks;
                    InfTemperatures = yy.Value.Temperatures;
                    InfFanSpeeds = yy.Value.FanSpeeds;
                }
                else
                {
                    if (OHMenable) OHMenable = false;

                    InfOHMCoreClocks = null;
                    InfOHMMemoryClocks = null;
                    if (!MIenable) InfTemperatures = null;
                    if (!MIenable && !MSIenable) InfFanSpeeds = null;
                }

                ResetGPUs();
            };
            Overclocker.ConnectedToMSI += def =>
            {
                DefClock = def;
                Logging("соединение с MSI установлено");
            };
            Overclocker.OverclockApplied += () => Logging("Профиль разгона MSI Afterburner применен");
            Overclocker._Overclocker();

            CurrProf = Settings.Profile;
            Profile = CurrProf;
            Miners = Miner.Miners;
            Algoritms = Miner.Algoritms;

            Miner.LogDataReceived += s => { if (showlog) Logging(s); };
            Miner.MinerInfoUpdated += mi => 
            {
                var xx = mi;
                if (!MIenable) MIenable = true;

                if (xx.Hashrates != null)
                {
                    InfHashrates = xx.Hashrates;
                    TotalHashrate = InfHashrates.Sum();
                }
                if (!OHMenable) { InfTemperatures = xx.Temperatures; }
                if (!MSIenable) InfFanSpeeds = xx.Fanspeeds;

                ResetGPUs();

                if (mi.ShAccepted != null) ShAccepted = mi.ShAccepted;
                if (mi.ShInvalid != null) ShInvalid = mi.ShInvalid;
                if (mi.ShRejected != null) ShRejected = mi.ShRejected;
                if (mi.ShTotalAccepted != null) ShTotalAccepted = mi.ShTotalAccepted;
                if (mi.ShTotalInvalid != null) ShTotalInvalid = mi.ShTotalInvalid;
                if (mi.ShTotalRejected != null) ShTotalRejected = mi.ShTotalRejected;
            };
            Miner.MinerStarted += (conf, ethernet) =>
            {
                CurrProf.StartedID = conf.ID;
                Settings.SetProfile(CurrProf);
                Indicator = true;
                TCPserver.Indication = true;
                string msg;
                if (!ethernet) msg = $"{conf.Name} запущен";
                else msg = $"Интернет восстановлен, {conf.Name} запущен";
                Logging(msg, true);
            };
            Miner.MinerStoped += () =>
            {
                Indicator = false;
                TCPserver.Indication = false;

                if (MIenable) MIenable = false;

                InfHashrates = null;
                TotalHashrate = null;
                if (!OHMenable) InfTemperatures = null;
                if (!OHMenable && !MSIenable) InfFanSpeeds = null;

                ResetGPUs();

                ShAccepted = null;
                ShInvalid = null;
                ShRejected = null;
                ShTotalAccepted = null;
                ShTotalInvalid = null;
                ShTotalRejected = null;
            };
            Miner.InactivityTimer += n =>
            {
                if (n < 1) IdleWachdog = "";
                else IdleWachdog = $"Бездаействие {n}";
            };
            Miner.LowHashrateTimer += n =>
            {
                if (n < 1) LowHWachdog = "";
                else LowHWachdog = $"Низкий хешрейт {n}";
            };
            Miner.WachdogDelayTimer += n =>
            {
                if (n < 1) WachdogInfo = "";
                else WachdogInfo = $"Активация вачдога {n}";
            };
            Miner.ZeroHash += miner =>
            {
                Logging("Нулевой [Zero] хешрейт, перезапуск майнера", true);
                miner.RestartMiner();
            };
            Miner.GPUsfalled += (miner, gs) =>
            {
                string str = "";
                foreach (int g in gs) str += $"{g},";
                Logging($"Отвал GPUs:[{str.TrimEnd(',')}] перезапуск майнера", true);
                miner.RestartMiner();
            };
            Miner.InactivityError += () =>
            {
                Logging("Бездействие, перезагрузка", true);
                System.Diagnostics.Process.Start("shutdown", "/r /f /t 0 /c \"OMineGuard перезапуск\"");
                System.Windows.Application.Current.Shutdown();
            };
            Miner.LowHashrateError += miner =>
            {
                Logging("Низкий [Low] хешрейт, перезапуск майнера", true);
                miner.RestartMiner();
            };

            if (CurrProf.Autostart)
            {
                if (CurrProf.StartedID != null)
                {
                    try
                    {
                        StartMiner(CurrProf.ConfigsList.
                            Where(c => c.ID == CurrProf.StartedID.Value).First());
                        System.Threading.Tasks.Task.Run(() => Autostarted?.Invoke());
                    }
                    catch { CurrProf.StartedID = null; }
                }
            }
        }

        private static Miner miner;
        private static bool showlog = false;
        private void StartMiner(Config config)
        {
            if (miner != null)
            {
                miner.StopMiner();
                miner = null;
            }

            switch (config.Miner.Value)
            {
                case 0: miner = new Bminer(); break;
                case 1: miner = new Claymore(); break;
                case 2: miner = new Gminer(); break;
            }

            if(config.ClockID != null)
            {
                Overclock oc = CurrProf.ClocksList.
                    Where(c => c.ID == config.ClockID).First();
                Overclocker.ApplyOverclock(oc);
            }
            miner.StartMiner(config);
        }
        private static void StopMiner()
        {
            if (miner != null) 
                miner.StopMiner();
        }
        public static void ExxtraStopMiner()
        {
            if (miner != null)
                miner.ExxtraStopMiner();
        }

        public Profile Profile { get; set; }
        public List<string> Miners { get; set; }
        public DefClock DefClock { get; set; }
        public Dictionary<string, int[]> Algoritms { get; set; }

        public string Loggong { get; set; }
        public string Log = ""; //статика для отправки по TCP
        private void Logging(string msg, bool informer = false)
        {
            msg = msg.
                Replace("\r\n\r\n", "\r\n").
                Replace("\r\r\n", "\r\n").
                Replace("\n\r\n", "\r\n").
                TrimEnd("\r\n".ToArray()).
                TrimStart("\r\n".ToArray());
            Loggong = msg + Environment.NewLine;
            Log += msg + Environment.NewLine;
            if (informer) Informer.SendMessage(msg);
        }

        private bool MSIenable = false;
        private bool OHMenable = false;
        private bool MIenable = false;

        public int GPUs { get; set; }
        private static readonly object gpkey = new object();
        public void ResetGPUs()
        {
            lock (gpkey)
            {
                int[] l = new int[]
                {
                    (InfPowerLimits != null? InfPowerLimits.Length : 0),
                    (InfCoreClocks != null? InfCoreClocks.Length : 0),
                    (InfMemoryClocks != null? InfMemoryClocks.Length : 0),
                    (InfFanSpeeds != null? InfFanSpeeds.Length : 0),
                    (InfTemperatures != null? InfTemperatures.Length : 0),
                    CurrProf.GPUsSwitch.Count
                };
                int m = l.Max();
                if (GPUs != m)
                {
                    GPUs = m;
                }
            }
        }

        public int?[] InfPowerLimits { get; set; }
        public int?[] InfCoreClocks { get; set; }
        public int?[] InfMemoryClocks { get; set; }
        public int?[] InfOHMCoreClocks { get; set; }
        public int?[] InfOHMMemoryClocks { get; set; }
        public int?[] InfFanSpeeds { get; set; }
        public int?[] InfTemperatures { get; set; }
        public double?[] InfHashrates { get; set; }
        public double? TotalHashrate { get; set; }

        public int?[] ShAccepted { get; set; }
        public int? ShTotalAccepted { get; set; }
        public int?[] ShRejected { get; set; }
        public int? ShTotalRejected { get; set; }
        public int?[] ShInvalid { get; set; }
        public int? ShTotalInvalid { get; set; }

        public string WachdogInfo { get; set; }
        public string LowHWachdog { get; set; }
        public string IdleWachdog { get; set; }
        public bool Indicator { get; set; } = false;

        #region Commands
        public void CMD_SaveProfile(Profile prof)
        {
            CurrProf = prof;
            Profile = null;
            Profile = CurrProf;
            Settings.SetProfile(CurrProf);
        }
        public void CMD_RunProfile(Profile prof, int index)
        {
            CurrProf = prof;
            Profile = null;
            Profile = CurrProf;
            Settings.SetProfile(CurrProf);
            StartMiner(CurrProf.ConfigsList[index]);
        }
        public void CMD_ApplyClock(Profile prof, int index)
        {
            CurrProf = prof;
            Profile = null;
            Profile = CurrProf;
            Settings.SetProfile(CurrProf);
            Overclocker.ApplyOverclock(CurrProf.ClocksList[index]);
        }
        public void CMD_MinerLogShow()
        {
            showlog = true;
        }
        public void CMD_MinerLogHide()
        {
            showlog = false;
        }
        public void CMD_SwitchProcess()
        {
            if (Indicator) StopMiner();
            else StartMiner(CurrProf.ConfigsList.
                            Where(c => c.ID == CurrProf.StartedID.Value).First());
        }

        private void TCP_OMWsent(RootObject RO)
        {
            if (RO.Profile != null) CMD_SaveProfile(RO.Profile);
            if (RO.ApplyClock != null) CMD_ApplyClock((Profile)RO.ApplyClock[0], (int)RO.ApplyClock[1]);
            if (RO.RunConfig != null) CMD_RunProfile((Profile)RO.RunConfig[0], (int)RO.RunConfig[1]);
            if (RO.ShowMinerLog != null) showlog = RO.ShowMinerLog.Value;
            if (RO.SwitchProcess != null) CMD_SwitchProcess();
        }
        #endregion
    }
}
