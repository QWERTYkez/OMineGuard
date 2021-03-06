﻿using OMineGuard.Miners;
using OMineGuardControlLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OMineGuard.Backend
{
    public class MainModel : IModel
    {
        public event Action Autostarted;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private static bool CheckArrays(int?[] a, int?[] b)
        {
            if (a == null && b == null) return false;
            if (a == null ^ b == null) return true;
            if (a.Length != b.Length) return true;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return true;
            }
            return false;
        }
        private static bool CheckArrays(int[] a, int[] b)
        {
            if (a == null && b == null) return false;
            if (a == null ^ b == null) return true;
            if (a.Length != b.Length) return true;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return true;
            }
            return false;
        }
        private static bool CheckArrays(double?[] a, double?[] b)
        {
            if (a == null && b == null) return false;
            if (a == null ^ b == null) return true;
            if (a.Length != b.Length) return true;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return true;
            }
            return false;
        }
        private (Func<int> GetVKmsgID, Func<string> GetTGmsgID)? MSGids;
        public void IniModel()
        {
            TCPserver.OMWsent += TCP_OMWsent;
            TCPserver.InitializeTCPserver(this);

            MSGids = Informer.SendMessage("OMineGuard запущен");

            Overclocker.OverclockReceived += (msi, ohm) => 
            { 
                var xx = msi;
                if (xx != null)
                {
                    if (!MSIenable) MSIenable = true;

                    if (CheckArrays(InfPowerLimits, xx.Value.PowerLimits))
                        InfPowerLimits = xx.Value.PowerLimits;
                    if (CheckArrays(InfCoreClocks, xx.Value.CoreClocks))
                        InfCoreClocks = xx.Value.CoreClocks;
                    if (CheckArrays(InfMemoryClocks, xx.Value.MemoryClocks))
                        InfMemoryClocks = xx.Value.MemoryClocks;
                    if (!OHMenable && CheckArrays(InfFanSpeeds, xx.Value.FanSpeeds)) 
                        InfFanSpeeds = xx.Value.FanSpeeds;
                }
                else
                {
                    if (MSIenable) MSIenable = false;

                    if (InfPowerLimits != null) InfPowerLimits = null;
                    if (InfCoreClocks != null) InfCoreClocks = null;
                    if (InfMemoryClocks != null) InfMemoryClocks = null;
                    if (!MIenable && !OHMenable && InfFanSpeeds != null) InfFanSpeeds = null;
                }

                var yy = ohm;
                if (yy != null)
                {
                    if (!OHMenable) OHMenable = true;

                    if (CheckArrays(InfOHMCoreClocks, yy.Value.CoreClocks))
                        InfOHMCoreClocks = yy.Value.CoreClocks;
                    if (CheckArrays(InfOHMMemoryClocks, yy.Value.MemoryClocks))
                        InfOHMMemoryClocks = yy.Value.MemoryClocks;
                    if (CheckArrays(InfTemperatures, yy.Value.Temperatures))
                        InfTemperatures = yy.Value.Temperatures;
                    if (CheckArrays(InfFanSpeeds, yy.Value.FanSpeeds))
                        InfFanSpeeds = yy.Value.FanSpeeds;
                }
                else
                {
                    if (OHMenable) OHMenable = false;

                    if (InfOHMCoreClocks != null) InfOHMCoreClocks = null;
                    if (InfOHMMemoryClocks != null) InfOHMMemoryClocks = null;
                    if (!MIenable && InfTemperatures != null) InfTemperatures = null;
                    if (!MIenable && !MSIenable && InfFanSpeeds != null) InfFanSpeeds = null;
                }
            };
            Overclocker.ConnectedToMSI += def =>
            {
                DefClock = def;
                Logging("соединение с MSI установлено");
            };
            Overclocker.OverclockApplied += () => Logging("Профиль разгона MSI Afterburner применен");
            Overclocker._Overclocker(this);

            Miner.InactivityTimer += n =>
            {
                if (n < 1) IdleWachdog = "";
                else IdleWachdog = $"Бездаействие {n}";
            };
            Miner.InactivityError += () =>
            {
                Logging("Бездействие, перезагрузка", true);
                Process.Start("shutdown", "/r /f /t 0 /c \"OMineGuard перезапуск\"");
                System.Windows.Application.Current.Shutdown();
            };
            Miner.MinerInfoUpdated += mi =>
            {
                var xx = mi;
                if (!MIenable) MIenable = true;

                if (xx.Hashrates != null)
                {
                    if (CheckArrays(InfHashrates, xx.Hashrates))
                        InfHashrates = xx.Hashrates;
                    TotalHashrate = InfHashrates.Sum();
                }
                else
                {
                    InfHashrates = null;
                    TotalHashrate = 0;
                }
                if (!OHMenable && CheckArrays(InfTemperatures, xx.Temperatures))
                    InfTemperatures = xx.Temperatures;
                if (!MSIenable && CheckArrays(InfFanSpeeds, xx.Fanspeeds))
                    InfFanSpeeds = xx.Fanspeeds;
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
            Miner.ZeroHash += () =>
            {
                Logging("Нулевой [Zero] хешрейт, перезапуск майнера", true);
                RestartMiner(false);
            };
            Miner.GPUsfalled += gs =>
            {
                string str = "";
                foreach (int g in gs) str += $"{g},";
                Logging($"Отвал GPUs:[{str.TrimEnd(',')}] перезапуск майнера", true);
                RestartMiner(false);
            };
            Miner.LowHashrateError += () =>
            {
                Logging("Низкий [Low] хешрейт, перезапуск майнера", true);
                RestartMiner(false);
            };
            Miner.MinerStarted += (conf, ethernet) => 
            {
                Profile.StartedID = conf.ID;
                Settings.SetProfile(Profile);
                Indicator = true;
                TCPserver.Indication = true;

                var msg = $"{conf.Name} запущен";
                if (MSGids != null)
                {
                    var xx = MSGids.Value; MSGids = null;

                    Logging(msg);
                    Informer.EditMessage(xx, "OMineGuard запущен, " + msg);
                    return;
                }
                if (ethernet) msg = $"Интернет восстановлен, {conf.Name} запущен";
                Logging(msg, true);
            };
            Miner.LogDataReceived += s => { if (showlog) Logging(s); };
            Miner.MinerStoped += (conf, b) =>
            {
                Indicator = false;
                TCPserver.Indication = false;

                if (MIenable) MIenable = false;

                InfHashrates = null;
                TotalHashrate = null;
                if (!OHMenable) InfTemperatures = null;
                if (!OHMenable && !MSIenable) InfFanSpeeds = null;

                ShAccepted = null;
                ShInvalid = null;
                ShRejected = null;
                ShTotalAccepted = null;
                ShTotalInvalid = null;
                ShTotalRejected = null;

                Logging($"{conf.Name} остановлен");
                if (b) return;
                Thread.Sleep(5000);
                RestartMiner(false);
            };

            InternetConnectionWacher.InternetConnectionLost += () => Task.Run(() =>
            {
                if (InternetMinerSwitch == null) InternetMinerSwitch = false;
                lock (InternetConnectionKey)
                {
                    if (InternetMinerSwitch.Value)
                    {
                        Miner.StopMiner();
                        InternetMinerSwitch = false;
                    }
                }
            });
            InternetConnectionWacher.InternetConnectionRestored += () => Task.Run(() =>
            {
                lock (InternetConnectionKey)
                {
                    if (!InternetMinerSwitch.Value)
                    {
                        if (ConfigToRecovery != null)
                        {
                            StartMiner(ConfigToRecovery, true);
                            InternetMinerSwitch = true;
                        }
                    }
                }
            });

            Profile = Settings.Profile;
            Miners = Miner.Miners;
            Algoritms = Miner.Algoritms;

            if (Profile.Autostart)
            {
                if (Profile.StartedID != null)
                {
                    try
                    {
                        var config = Profile.ConfigsList.
                            Where(c => c.ID == Profile.StartedID.Value).First();

                        if (config.ClockID != null)
                        {
                            while (!Overclocker.MSIconnected)
                                Thread.Sleep(100);
                        }

                        Thread.Sleep(2000);

                        StartMiner(config, false);
                        Task.Run(() => Autostarted?.Invoke());
                    }
                    catch { Profile.StartedID = null; }
                }
            }
        }

        private static bool showlog = false;
        private readonly object InternetConnectionKey = new object();
        private bool? InternetMinerSwitch = null;
        private static IConfig ConfigToRecovery { get; set; }
        private async void StartMiner(IConfig config, bool manually, bool InternetRestored = false)
        {
            Miner.StopMiner(manually);
            await Task.Delay(1000);

            if (config.ClockID != null)
            { Overclocker.ApplyOverclock(Profile.ClocksList.Where(c => c.ID == config.ClockID).First()); }

            Miner.StartMiner(config, InternetRestored);
            ConfigToRecovery = config;
        }
        private void RestartMiner(bool manually) => StartMiner(ConfigToRecovery, manually);

        public IProfile Profile { get; set; }
        public List<string> Miners { get; set; }
        public IDefClock DefClock { get; set; }
        public Dictionary<string, int[]> Algoritms { get; set; }

        public string Loggong { get; set; }
        public void Logging(string msg, bool informer = false)
        {
            msg = msg.
                Replace("\r\n\r\n", "\r\n").
                Replace("\r\r\n", "\r\n").
                Replace("\n\r\n", "\r\n").
                TrimEnd("\r\n".ToArray()).
                TrimStart("\r\n".ToArray());
            Loggong = msg;
            Loggong = Environment.NewLine;
            if (informer) Informer.SendMessage(msg);
        }

        private bool MSIenable = false;
        private bool OHMenable = false;
        private bool MIenable = false;

        public int[] InfPowerLimits { get; set; }
        public int[] InfCoreClocks { get; set; }
        public int[] InfMemoryClocks { get; set; }
        public int?[] InfOHMCoreClocks { get; set; }
        public int?[] InfOHMMemoryClocks { get; set; }
        public int?[] InfFanSpeeds { get; set; }
        public int?[] InfTemperatures { get; set; }
        public double?[] InfHashrates { get; set; }
        public double? TotalHashrate { get; set; }

        public int[] ShAccepted { get; set; }
        public int? ShTotalAccepted { get; set; }
        public int[] ShRejected { get; set; }
        public int? ShTotalRejected { get; set; }
        public int[] ShInvalid { get; set; }
        public int? ShTotalInvalid { get; set; }

        public string WachdogInfo { get; set; }
        public string LowHWachdog { get; set; }
        public string IdleWachdog { get; set; }
        public bool Indicator { get; set; } = false;

        #region Commands
        public void CMD_SaveProfile(IProfile prof)
        {
            Profile = prof;
            Settings.SetProfile(Profile);
        }
        public void CMD_RunProfile(IProfile prof, int index)
        {
            Profile = prof;
            Settings.SetProfile(Profile);
            StartMiner(Profile.ConfigsList[index], true);
        }
        public void CMD_ApplyClock(IProfile prof, int index)
        {
            Profile = prof;
            Settings.SetProfile(Profile);
            Overclocker.ApplyOverclock(Profile.ClocksList[index]);
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
            Miner.StopMiner(true);
            if (!Indicator)
                StartMiner(Profile.ConfigsList.
                     Where(c => c.ID == Profile.StartedID.Value).First(), true);
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
