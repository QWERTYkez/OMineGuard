using OMineGuard.Managers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OMineGuard.Models
{
    public class MainModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private Profile CurrProf;
        public MainModel()
        {
            CurrProf = Settings.GetProfile();
            Profile = CurrProf;
            if (CurrProf.Autostart)
            {
                throw new System.Exception();
            }
        }

        public Profile Profile { get; set; }
        public List<string> Miners { get; set; }
        public DC DefClock { get; set; }
        public Dictionary<string, int[]> Algoritms { get; set; }
        public string Loggong { get; set; }
        public OC OC { get; set; }
        public double[] Hashrates { get; set; }
        public int[] Temperatures { get; set; }
        public string WachdogInfo { get; set; }
        public string LowHWachdog { get; set; }
        public string IdleWachdog { get; set; }
        public string ShowMLogTB { get; set; }
        public bool? Indicator { get; set; }

        #region Commands
        public void cmd_SaveProfile(Profile prof)
        {
            CurrProf = prof;
            Settings.SetProfile(CurrProf);
        }
        public void cmd_RunProfile(Profile prof, int index)
        {
            CurrProf = prof;
            Settings.SetProfile(CurrProf);
            OMGcontroller.SendSetting(CurrProf.ConfigsList[index].ID, MSGtype.RunConfig);
        }
        public void cmd_ApplyClock(Profile prof, int index)
        {
            CurrProf = prof;
            Settings.SetProfile(CurrProf);
            OMGcontroller.SendSetting(CurrProf.ClocksList[index].ID, MSGtype.ApplyClock);
        }
        public void cmd_ShowMinerLog()
        {
            OMGcontroller.SendSetting(true, MSGtype.ShowMinerLog);
        }
        public void cmd_SwitchProcess()
        {
            if ((bool)Indicator)
            {
                OMGcontroller.SendSetting(true, MSGtype.KillProcess);
            }
            else
            {
                OMGcontroller.SendSetting(true, MSGtype.StartProcess);
            }
        }
        #endregion
    }
}
