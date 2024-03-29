﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace OMineGuardControlLibrary
{
    internal class ViewModel : INotifyPropertyChanged
    {
        public event Action Autostarted;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public ViewModel() { }

        public IModel _model;
        public ViewModel(IModel model)
        {
            IniConfigsCommands();
            IniConfigCommands();
            IniOverclockCommands();
            IniLogCommands();
            IniBaseSettingsCommands();

            Task.Run(() => 
            {
                _model = model;
                _model.PropertyChanged += ModelChanged;
                _model.Autostarted += () => Autostarted?.Invoke();
                _model.IniModel();
            });
        }

        public IProfile Profile; //private
        private Dictionary<string, int[]> Algs;
        private List<string> MinersList;

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
                    (InfOHMCoreClocks != null? InfOHMCoreClocks.Length : 0),
                    (InfOHMMemoryClocks != null? InfOHMMemoryClocks.Length : 0),
                    (InfHashrates != null? InfHashrates.Length : 0),
                    Profile.GPUsSwitch.Count
                };
                int m = l.Max();
                if (GPUs != m)
                {
                    GPUs = m;
                }
            }
        }

        private void ModelChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Profile":
                    {
                        if (_model.Profile != null)
                        {
                            Profile = _model.Profile;

                            LogFontSize = Profile.LogTextSize;
                            GPUsSwitch = Profile.GPUsSwitch;
                            GPUsCountSelected = _model.Profile.GPUsSwitch.Count;
                            RigName = Profile.RigName;
                            AutoRun = Profile.Autostart;
                            ConfigsNames = from i in Profile.ConfigsList select i.Name;
                            ConfigOverclocks = new List<string> { "---" };
                            ConfigOverclocks = ConfigOverclocks.Concat(
                                from i in Profile.ClocksList select i.Name);
                            OverclocksNames = from i in Profile.ClocksList select i.Name;
                            WachdogTimer = Profile.TimeoutWachdog;
                            IdleTimeout = Profile.TimeoutIdle;
                            LHTimeout = Profile.TimeoutLH;

                            VKInformer = Profile.VkInform;
                            VKUserID = Profile.VKuserID;
                        }
                        break;
                    }
                case "InfPowerLimits": { InfPowerLimits = _model.InfPowerLimits; ResetGPUs(); break; }
                case "InfCoreClocks": { InfCoreClocks = _model.InfCoreClocks; ResetGPUs(); break; }
                case "InfMemoryClocks": { InfMemoryClocks = _model.InfMemoryClocks; ResetGPUs(); break; }
                case "InfOHMCoreClocks": { InfOHMCoreClocks = _model.InfOHMCoreClocks; ResetGPUs(); break; }
                case "InfOHMMemoryClocks": { InfOHMMemoryClocks = _model.InfOHMMemoryClocks; ResetGPUs(); break; }
                case "InfFanSpeeds": { InfFanSpeeds = _model.InfFanSpeeds; ResetGPUs(); break; }
                case "InfTemperatures": { InfTemperatures = _model.InfTemperatures; ResetGPUs(); break; }
                case "InfHashrates": { InfHashrates = _model.InfHashrates; ResetGPUs(); break; }
                case "TotalHashrate": { TotalHashrate = _model.TotalHashrate; break; }
                case "Miners":
                    {
                        MinersList = _model.Miners;
                        break;
                    }
                case "Algoritms":
                    {
                        Algs = _model.Algoritms;
                        Algoritms = Algs.Keys;
                        break;
                    }
                case "Loggong":
                    {
                        Log += _model.Loggong;
                    }
                    break;
                case "DefClock":
                    {
                        DefPowerLimits = _model.DefClock.PowerLimits;
                        DefCoreClocks = _model.DefClock.CoreClocks;
                        DefMemoryClocks = _model.DefClock.MemoryClocks;
                        DefFanSpeeds = _model.DefClock.FanSpeeds;
                    }
                    break;
                case "WachdogInfo":
                    {
                        WachdogInfo = _model.WachdogInfo;
                    }
                    break;
                case "LowHWachdog":
                    {
                        LowHWachdog = _model.LowHWachdog;
                    }
                    break;
                case "IdleWachdog":
                    {
                        IdleWachdog = _model.IdleWachdog;
                    }
                    break;
                case "Indicator":
                    {
                        Indication = _model.Indicator;
                        if (Indication)
                        {
                            SwitchProcessButtonText = "KillProcess";
                        }
                        else
                        {
                            SwitchProcessButtonText = "RunProcess";
                        }
                    }
                    break;
            }
        }

        public bool Indication { get; set; } = false;
        #region Configs
        public string RigName { get; set; }
        public RelayCommand SetRigName { get; set; }

        public bool AutoRun { get; set; }
        public RelayCommand SetAutoRun { get; set; }

        public List<string> GPUsCounts { get; set; } = new List<string>() { "Auto", "1", "2",
            "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
        public int GPUsCountSelected { get; set; } = -1;
        public List<bool> GPUsSwitch { get; set; }
        public RelayCommand SetGPUsSwitch { get; set; }

        private void IniConfigsCommands()
        {
            SetRigName = new RelayCommand(obj =>
            {
                Profile.RigName = RigName;
                _model.CMD_SaveProfile(Profile);
            });
            SetAutoRun = new RelayCommand(obj =>
            {
                Profile.Autostart = AutoRun;
                _model.CMD_SaveProfile(Profile);
            });
            SetGPUsSwitch = new RelayCommand(obj =>
            {
                var bools = GPUsSwitch.ToArray();
                GPUsSwitch = bools.ToList();
                Profile.GPUsSwitch = GPUsSwitch;
                _model.CMD_SaveProfile(Profile);
            });
        }
        private void SaveProfile()
        {
            Profile.ConfigsList[SelectedConfigIndex].Name = ConfigName;
            Profile.ConfigsList[SelectedConfigIndex].MinHashrate = MinHashrate.GetValueOrDefault();
            Profile.ConfigsList[SelectedConfigIndex].Pool = Pool;
            Profile.ConfigsList[SelectedConfigIndex].Port = Port;
            Profile.ConfigsList[SelectedConfigIndex].Wallet = Wallet;
            Profile.ConfigsList[SelectedConfigIndex].Params = Params;
            if (ConfigSelectedOverclockIndex > 0)
            {
                Profile.ConfigsList[SelectedConfigIndex].ClockID = Profile.ClocksList[ConfigSelectedOverclockIndex - 1].ID;
            }
            else { Profile.ConfigsList[SelectedConfigIndex].ClockID = null; }
            if (SelectedAlgoritmIndex > -1)
            {
                Profile.ConfigsList[SelectedConfigIndex].Algoritm = Algs.Keys.ToList()[SelectedAlgoritmIndex];
            }
            else { Profile.ConfigsList[SelectedConfigIndex].Algoritm = ""; }
            if (SelectedMinerIndex > -1)
            {
                if (SelectedMinerItem == null)
                    Profile.ConfigsList[SelectedConfigIndex].Miner = -1;
                else Profile.ConfigsList[SelectedConfigIndex].Miner = MinersList.IndexOf(SelectedMinerItem.ToString());
            }
            else
            {
                Profile.ConfigsList[SelectedConfigIndex].Miner = null;
            }
            int n = SelectedConfigIndex;
            ConfigsNames = from i in Profile.ConfigsList select i.Name;
            SelectedConfigIndex = n;
        }
        #endregion
        #region Config
        public IEnumerable<string> ConfigsNames { get; set; }
        public int SelectedConfigIndex { get; set; } = -1;
        public RelayCommand SelectConfig { get; set; }

        public string ConfigName { get; set; }
        public IEnumerable<string> ConfigOverclocks { get; set; }
        public int ConfigSelectedOverclockIndex { get; set; } = -1;
        public object ConfigSelectedOcerclockItem { get; set; }
        public IEnumerable<string> Algoritms { get; set; }
        public int SelectedAlgoritmIndex { get; set; } = -1;
        public object SelectedAlgoritmItem { get; set; }
        public RelayCommand SelectAlgoritm { get; set; }
        public IEnumerable<string> Miners { get; set; }
        public int SelectedMinerIndex { get; set; } = -1;
        public object SelectedMinerItem { get; set; }
        public double? MinHashrate { get; set; }
        public string Pool { get; set; }
        public string Port { get; set; }
        public string Wallet { get; set; }
        public string Params { get; set; }

        public RelayCommand PlusConfig { get; set; }
        public RelayCommand MinusConfig { get; set; }
        public RelayCommand SaveConfig { get; set; }
        public RelayCommand StartConfig { get; set; }

        private void IniConfigCommands()
        {
            SelectConfig = new RelayCommand(obj =>
            {
                if (SelectedConfigIndex > -1)
                {
                    ConfigName = Profile.ConfigsList[SelectedConfigIndex].Name;
                    MinHashrate = Profile.ConfigsList[SelectedConfigIndex].MinHashrate;
                    Pool = Profile.ConfigsList[SelectedConfigIndex].Pool;
                    Port = Profile.ConfigsList[SelectedConfigIndex].Port;
                    Wallet = Profile.ConfigsList[SelectedConfigIndex].Wallet;
                    Params = Profile.ConfigsList[SelectedConfigIndex].Params;
                    if (Profile.ConfigsList[SelectedConfigIndex].ClockID != null)
                    {
                        foreach (IOverclock c in Profile.ClocksList)
                        {
                            if (c.ID == Profile.ConfigsList[SelectedConfigIndex].ClockID)
                            {
                                ConfigSelectedOcerclockItem = c.Name;
                            }
                        }
                    }
                    else { ConfigSelectedOverclockIndex = -1; }
                    if (Profile.ConfigsList[SelectedConfigIndex].Algoritm != "")
                    {
                        foreach (string alg in Algoritms)
                        {
                            if (alg == Profile.ConfigsList[SelectedConfigIndex].Algoritm)
                            {
                                SelectedAlgoritmItem = alg;
                            }
                        }
                    }
                    else { SelectedAlgoritmIndex = -1; }
                    if (Profile.ConfigsList[SelectedConfigIndex].Miner != null)
                    {
                        if ((int)Profile.ConfigsList[SelectedConfigIndex].Miner < 0)
                            SelectedMinerItem = 0;
                        else SelectedMinerItem = MinersList[(int)Profile.ConfigsList[SelectedConfigIndex].Miner];
                    }
                    else
                    {
                        SelectedMinerIndex = 0;
                    }
                }
                else
                {
                    ConfigName = null;
                    MinHashrate = null;
                    Pool = null;
                    Port = null;
                    Wallet = null;
                    Params = null;
                    ConfigSelectedOverclockIndex = -1;
                    SelectedAlgoritmIndex = -1;
                    SelectedMinerIndex = -1;
                }
            });
            SelectAlgoritm = new RelayCommand(obj =>
            {
                if (SelectedAlgoritmIndex > -1)
                {
                    int[] x = Algs[Algs.Keys.ToList()[SelectedAlgoritmIndex]];
                    Miners = from m in MinersList where x.Contains(MinersList.IndexOf(m)) select m;
                    SelectedMinerIndex = -1;
                }
                else { Miners = null; }
            });
            PlusConfig = new RelayCommand(obj =>
            {
                Profile.ConfigsList.Add(new Config());
                ConfigsNames = from i in Profile.ConfigsList select i.Name;
                _model.CMD_SaveProfile(Profile);
                SelectedConfigIndex = ConfigsNames.Count() - 1;
            });
            MinusConfig = new RelayCommand(obj =>
            {
                if (SelectedConfigIndex > -1)
                {
                    Profile.ConfigsList.RemoveAt(SelectedConfigIndex);
                    ConfigsNames = from i in Profile.ConfigsList select i.Name;
                    SelectedConfigIndex = -1;
                    _model.CMD_SaveProfile(Profile);
                }
            });
            SaveConfig = new RelayCommand(obj =>
            {
                if (SelectedConfigIndex > -1)
                {
                    SaveProfile();
                    _model.CMD_SaveProfile(Profile);
                }
            });
            StartConfig = new RelayCommand(obj =>
            {
                if (SelectedConfigIndex > -1)
                {
                    SaveProfile();
                    _model.CMD_RunProfile(Profile, SelectedConfigIndex);
                }
            });
        }
        #endregion
        #region Overclock
        public IEnumerable<string> OverclocksNames { get; set; }
        public int SelectedOverclockIndex { get; set; } = -1;
        public RelayCommand SelectOverclock { get; set; }

        public RelayCommand PlusOverclock { get; set; }
        public RelayCommand MinusOverclock { get; set; }
        public RelayCommand SaveOverclock { get; set; }
        public RelayCommand ApplyOverclock { get; set; }

        private int[] DefPowerLimits;
        private int[] DefCoreClocks;
        private int[] DefMemoryClocks;
        private int[] DefFanSpeeds;

        public string OverclockName { get; set; }
        public int[] PowerLimits { get; set; }
        public int[] CoreClocks { get; set; }
        public int[] MemoryClocks { get; set; }
        public int[] FanSpeeds { get; set; }

        public RelayCommand PowerLimitsOn { get; set; }
        public RelayCommand CoreClocksOn { get; set; }
        public RelayCommand MemoryClocksOn { get; set; }
        public RelayCommand FanSpeedsOn { get; set; }

        public RelayCommand PowerLimitsOff { get; set; }
        public RelayCommand CoreClocksOff { get; set; }
        public RelayCommand MemoryClocksOff { get; set; }
        public RelayCommand FanSpeedsOff { get; set; }

        public int[] InfPowerLimits { get; set; }
        public int[] InfCoreClocks { get; set; }
        public int[] InfMemoryClocks { get; set; }
        public int?[] InfOHMCoreClocks { get; set; }
        public int?[] InfOHMMemoryClocks { get; set; }
        public int?[] InfFanSpeeds { get; set; }
        public int?[] InfTemperatures { get; set; }
        public double?[] InfHashrates { get; set; }
        public double? TotalHashrate { get; set; }

        private void IniOverclockCommands()
        {
            SelectOverclock = new RelayCommand(obj =>
            {
                if (SelectedOverclockIndex > -1)
                {
                    OverclockName = Profile.ClocksList[SelectedOverclockIndex].Name;

                    PowerLimits = Profile.ClocksList[SelectedOverclockIndex].PowLim;
                    CoreClocks = Profile.ClocksList[SelectedOverclockIndex].CoreClock;
                    MemoryClocks = Profile.ClocksList[SelectedOverclockIndex].MemoryClock;
                    FanSpeeds = Profile.ClocksList[SelectedOverclockIndex].FanSpeed;
                }
                else
                {
                    OverclockName = "";

                    PowerLimits = null;
                    CoreClocks = null;
                    MemoryClocks = null;
                    FanSpeeds = null;
                }
            });

            PowerLimitsOn = new RelayCommand(obj =>
            {
                if (Profile.ClocksList[SelectedOverclockIndex].PowLim == null)
                    PowerLimits = DefPowerLimits;
                else
                    PowerLimits = Profile.ClocksList[SelectedOverclockIndex].PowLim;
            });
            CoreClocksOn = new RelayCommand(obj =>
            {
                if (Profile.ClocksList[SelectedOverclockIndex].PowLim == null)
                    CoreClocks = DefCoreClocks;
                else
                    CoreClocks = Profile.ClocksList[SelectedOverclockIndex].CoreClock;
            });
            MemoryClocksOn = new RelayCommand(obj =>
            {
                if (Profile.ClocksList[SelectedOverclockIndex].PowLim == null)
                    MemoryClocks = DefMemoryClocks;
                else
                    MemoryClocks = Profile.ClocksList[SelectedOverclockIndex].MemoryClock;
            });
            FanSpeedsOn = new RelayCommand(obj =>
            {
                if (Profile.ClocksList[SelectedOverclockIndex].PowLim == null)
                    FanSpeeds = DefFanSpeeds;
                else
                    FanSpeeds = Profile.ClocksList[SelectedOverclockIndex].FanSpeed;
            });

            PowerLimitsOff = new RelayCommand(obj =>
            {
                PowerLimits = null;
            });
            CoreClocksOff = new RelayCommand(obj =>
            {
                CoreClocks = null;
            });
            MemoryClocksOff = new RelayCommand(obj =>
            {
                MemoryClocks = null;
            });
            FanSpeedsOff = new RelayCommand(obj =>
            {
                FanSpeeds = null;
            });

            PlusOverclock = new RelayCommand(obj =>
            {
                Profile.ClocksList.Add(new Overclock());
                OverclocksNames = from i in Profile.ClocksList select i.Name;
                _model.CMD_SaveProfile(Profile);
                SelectedOverclockIndex = OverclocksNames.Count() - 1;
                ConfigOverclocks = new List<string> { "---" };
                ConfigOverclocks = ConfigOverclocks.Concat(
                    from i in Profile.ClocksList select i.Name);
            });
            MinusOverclock = new RelayCommand(obj =>
            {
                if (SelectedOverclockIndex > -1)
                {
                    foreach (var c in Profile.ConfigsList)
                    {
                        if (c.ClockID == Profile.ClocksList[SelectedOverclockIndex].ID)
                        {
                            c.ClockID = null;
                        }
                    }
                    Profile.ClocksList.RemoveAt(SelectedOverclockIndex);
                    OverclocksNames = from i in Profile.ClocksList select i.Name;
                    _model.CMD_SaveProfile(Profile);
                    SelectedOverclockIndex = -1;
                    ConfigOverclocks = new List<string> { "---" };
                    ConfigOverclocks = ConfigOverclocks.Concat(
                        from i in Profile.ClocksList select i.Name);
                }
            });
            SaveOverclock = new RelayCommand(obj =>
            {
                if (SelectedOverclockIndex > -1)
                {
                    SaveCLock();
                    _model.CMD_SaveProfile(Profile);
                    ConfigOverclocks = new List<string> { "---" };
                    ConfigOverclocks = ConfigOverclocks.Concat(
                        from i in Profile.ClocksList select i.Name);
                }
            });
            ApplyOverclock = new RelayCommand(obj =>
            {
                if (SelectedOverclockIndex > -1)
                {
                    SaveCLock();
                    _model.CMD_ApplyClock(Profile, SelectedOverclockIndex);
                    ConfigOverclocks = new List<string> { "---" };
                    ConfigOverclocks = ConfigOverclocks.Concat(
                        from i in Profile.ClocksList select i.Name);
                }
            });
        }
        private void SaveCLock()
        {
            Profile.ClocksList[SelectedOverclockIndex].Name = OverclockName;
            if (PowerLimits != null)
            {
                Profile.ClocksList[SelectedOverclockIndex].PowLim = PowerLimits.ToArray();
            }
            else { Profile.ClocksList[SelectedOverclockIndex].PowLim = null; }
            if (CoreClocks != null)
            {
                Profile.ClocksList[SelectedOverclockIndex].CoreClock = CoreClocks.ToArray();
            }
            else { Profile.ClocksList[SelectedOverclockIndex].CoreClock = null; }
            if (MemoryClocks != null)
            {
                Profile.ClocksList[SelectedOverclockIndex].MemoryClock = MemoryClocks.ToArray();
            }
            else { Profile.ClocksList[SelectedOverclockIndex].MemoryClock = null; }
            if (FanSpeeds != null)
            {
                Profile.ClocksList[SelectedOverclockIndex].FanSpeed = FanSpeeds.ToArray();
            }
            else { Profile.ClocksList[SelectedOverclockIndex].FanSpeed = null; }
            int n = SelectedOverclockIndex;
            OverclocksNames = from i in Profile.ClocksList select i.Name;
            SelectedOverclockIndex = n;
        }
        #endregion
        #region Log
        public string Log { get; set; }
        public string LogHashrate { get; set; }
        public string LogTemperature { get; set; }
        public string LogTotalHash { get; set; }
        public int LogFontSize { get; set; }
        public RelayCommand SetLogFontSize { get; set; }
        public bool LogAutoscroll { get; set; } = true;
        public RelayCommand MinerLogShow { get; set; }
        public RelayCommand MinerLogHide { get; set; }
        public string SwitchProcessButtonText { get; set; } = "Switch Process";
        public RelayCommand SwitchProcess { get; set; }

        public string WachdogInfo { get; set; }
        public string LowHWachdog { get; set; }
        public string IdleWachdog { get; set; }
        public string ShowMLogTB { get; set; }

        private void IniLogCommands()
        {
            SetLogFontSize = new RelayCommand(obj =>
            {
                Profile.LogTextSize = LogFontSize;
                _model.CMD_SaveProfile(Profile);
            });
            MinerLogShow = new RelayCommand(obj => 
            {
                _model.CMD_MinerLogShow();
            });
            MinerLogHide = new RelayCommand(obj =>
            {
                _model.CMD_MinerLogHide();
            });
            SwitchProcess = new RelayCommand(obj =>
            {
                _model.CMD_SwitchProcess();
            });
        }
        #endregion
        #region BaseSettings
        public int WachdogTimer { get; set; } = 30;
        public int IdleTimeout { get; set; } = 300;
        public int LHTimeout { get; set; } = 30;
        public RelayCommand SetWachdogTimer { get; set; }
        public RelayCommand SetIdleTimeout { get; set; }
        public RelayCommand SetLHTimeout { get; set; }
        
        public bool VKInformer { get; set; }
        public RelayCommand SetVKInformer { get; set; }
        public string VKUserID { get; set; }
        public RelayCommand SetVKUserID { get; set; }
        public bool TelegramInformer { get; set; }
        public RelayCommand SetTelegramInformer { get; set; }
        public string TelegramUserID { get; set; }
        public RelayCommand SetTelegramUserID { get; set; }

        private void IniBaseSettingsCommands()
        {
            SetWachdogTimer = new RelayCommand(obj =>
            {
                Profile.TimeoutWachdog = WachdogTimer;
                if (IdleTimeout < WachdogTimer)
                {
                    IdleTimeout = WachdogTimer;
                }
                _model.CMD_SaveProfile(Profile);
            });
            SetIdleTimeout = new RelayCommand(obj =>
            {
                Profile.TimeoutIdle = IdleTimeout;
                _model.CMD_SaveProfile(Profile);
            });
            SetLHTimeout = new RelayCommand(obj =>
            {
                Profile.TimeoutLH = LHTimeout;
                _model.CMD_SaveProfile(Profile);
            });
            SetVKInformer = new RelayCommand(obj =>
            {
                Profile.VkInform = VKInformer;
                if (!VKInformer) Profile.VKuserID = "";
                _model.CMD_SaveProfile(Profile);
            });
            SetVKUserID = new RelayCommand(obj =>
            {
                Profile.VKuserID = VKUserID;
                _model.CMD_SaveProfile(Profile);
            });
            SetTelegramInformer = new RelayCommand(obj =>
            {
                _model.CMD_SaveProfile(Profile);
            });
            SetTelegramUserID = new RelayCommand(obj =>
            {

                _model.CMD_SaveProfile(Profile);
            });
        }
        #endregion
    }
}