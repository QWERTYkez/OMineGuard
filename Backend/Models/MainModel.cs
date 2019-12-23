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
            Informer.SendMessage("OMineGuard запущен");

            Overclocker.OverclockReceived += (msi, ohm) => { MSI = msi; OHM = ohm; };
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
            Miner.MinerInfoUpdated += mi => { MI = mi; };
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
                MI = null;
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
                Logging("Низкий хешрейт, перезапуск майнера", true);
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

            TCPserver.OMWsent += TCP_OMWsent;
            TCPserver._TCPserver();
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
        public static void StopMiner()
        {
            if (miner != null) 
                miner.StopMiner();
        }

        public Profile Profile { get; set; }
        public List<string> Miners { get; set; }
        public DefClock DefClock { get; set; }
        public Dictionary<string, int[]> Algoritms { get; set; }

        public string Loggong { get; set; }
        public static string Log = ""; //статика для отправки по TCP
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

        public MSIinfo? MSI { get; set; }
        public OHMinfo? OHM { get; set; }
        public MinerInfo? MI { get; set; }

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
