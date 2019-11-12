using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PM = OMineGuard.ProfileManager;
using SM = OMineGuard.SettingsManager;
using MM = OMineGuard.MinersManager;
using OCM = OMineGuard.OverclockManager;
using IM = OMineGuard.InformManager;
using TCP = OMineGuard.TCPserver;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows.Documents;
using System.IO;
using System.Reflection;

namespace OMineGuard
{
    public partial class MainWindow : Window
    {
        public const string Ver = "1.8";
        public static string Version;
        public static MainWindow This;
        public static bool AutoScroll = true;
        public static SynchronizationContext context = SynchronizationContext.Current;

        public MainWindow()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            InitializeComponent();
            Title += $" v.{Ver}";
            This = this;
            IniProfile();
            OCM.Initialize();
            TCP.OMWsent += TCP_OMWsent;
        }

        #region InitializeProfile
        private void IniProfile()
        {
            PM.Initialize();
            CreateDirectories();
            Algotitm.ItemsSource = SM.MinersD.Keys;
            AutoStart.IsChecked = PM.Profile.Autostart;
            AutoStart.Checked += AutoStart_Checked;
            AutoStart.Unchecked += AutoStart_Checked;
            Overclock.ItemsSource = (new string[] { "" }).Concat(PM.Profile.ClocksList.Select(W => W.Name));
            GPUsCB.ItemsSource = new string[] { "Auto", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
            if (PM.Profile.GPUsSwitch != null)
            { GPUsCB.SelectedIndex = PM.Profile.GPUsSwitch.Length; }
            else { GPUsCB.SelectedIndex = 0; }
            if (PM.Profile.RigName != null)
            { RigName.Text = PM.Profile.RigName; }
            if (PM.Profile.ConfigsList == null)
            { PM.Profile.ConfigsList = new List<Profile.Config>(); }
            ConfigsList.ItemsSource = PM.Profile.ConfigsList.Select(W => W.Name);
            ClocksList.ItemsSource = PM.Profile.ClocksList.Select(W => W.Name);
            Profile.Config Conf = PM.GetConfig(PM.Profile.StartedID);
            if (Conf != null)
            {
                ConfigsList.SelectedItem = Conf.Name;
                Profile.Overclock clock = PM.GetClock(PM.GetConfig(PM.Profile.StartedID).ID);
                if (clock != null)
                {
                    ClocksList.SelectedItem = clock.Name;
                }
            }
            if (PM.Profile.LogTextSize != 0)
            {
                MinerLog.FontSize = PM.Profile.LogTextSize;
                TextSizeTB.Text = PM.Profile.LogTextSize.ToString();
            }
            TextSizeSlider.Value = PM.Profile.LogTextSize;
            TextSizeSlider.ValueChanged += TextSizeSlider_ValueChanged;
            DigitsSlider.Value = PM.Profile.Digits;
            Dig = PM.Profile.Digits;
            Digits.Text = PM.Profile.Digits.ToString();
            DigitsSlider.ValueChanged += DigitsSlider_ValueChanged;
            if (!PM.Profile.Informer.VkInform)
            {
                VKuserID.IsEnabled = false;
                VKInformerToggle.IsChecked = false;
            }
            else
            {
                VKuserID.IsEnabled = true;
                VKInformerToggle.IsChecked = true;
            }
            VKuserID.Text = PM.Profile.Informer.VKuserID;
            VKInformerToggle.Checked += VKInformerToggle_Click;
            VKInformerToggle.Unchecked += VKInformerToggle_Click;
            VKuserID.TextChanged += VKuserID_TextChanged;

            {// timeout sliders
                WachdogTimerSlider.Value = PM.Profile.TimeoutWachdog;
                WachdogTimerSec.Text = PM.Profile.TimeoutWachdog.ToString();

                IdleTimeoutSlider.Minimum = PM.Profile.TimeoutWachdog;
                IdleTimeoutSlider.Value = PM.Profile.TimeoutIdle;
                IdleTimeoutSec.Text = PM.Profile.TimeoutIdle.ToString();

                LHTimeoutSlider.Value = PM.Profile.TimeoutLH;
                LHTimeoutSec.Text = PM.Profile.TimeoutLH.ToString();

                WachdogTimerSlider.ValueChanged += WachdogTimerSlider_ValueChanged;
                IdleTimeoutSlider.ValueChanged += IdleTimeoutSlider_ValueChanged;
                LHTimeoutSlider.ValueChanged += LHTimeoutSlider_ValueChanged;
            }
            


            Autostart();

            TCP.ServerStart();
        }
        public static void UpdateProfile()
        {
            This.Algotitm.ItemsSource = SM.MinersD.Keys;
            This.AutoStart.Checked -= This.AutoStart_Checked;
            This.AutoStart.Unchecked -= This.AutoStart_Checked;
            This.AutoStart.IsChecked = PM.Profile.Autostart;
            This.AutoStart.Checked += This.AutoStart_Checked;
            This.AutoStart.Unchecked += This.AutoStart_Checked;
            This.Overclock.ItemsSource = (new string[] { "" }).Concat(PM.Profile.ClocksList.Select(W => W.Name));
            if (PM.Profile.GPUsSwitch != null)
            { This.GPUsCB.SelectedIndex = PM.Profile.GPUsSwitch.Length; }
            else { This.GPUsCB.SelectedIndex = 0; }
            if (PM.Profile.RigName != null)
            { This.RigName.Text = PM.Profile.RigName; }
            if (PM.Profile.ConfigsList == null)
            { PM.Profile.ConfigsList = new List<Profile.Config>(); }
            This.ConfigsList.ItemsSource = PM.Profile.ConfigsList.Select(W => W.Name);
            This.ConfigsList_Selected(null, null);
            This.ClocksList.ItemsSource = PM.Profile.ClocksList.Select(W => W.Name);
            This.ClocksList_SelectionChanged(null, null);
            Profile.Config Conf = PM.GetConfig(PM.Profile.StartedID);
            if (Conf != null)
            {
                This.ClocksList.SelectedItem = Conf.Name;
                Profile.Overclock clock = PM.GetClock(PM.GetConfig(PM.Profile.StartedID).ID);
                if (clock != null)
                {
                    This.ClocksList.SelectedItem = clock.Name;
                }
            }
            if (PM.Profile.LogTextSize != 0)
            {
                This.MinerLog.FontSize = PM.Profile.LogTextSize;
                This.TextSizeTB.Text = PM.Profile.LogTextSize.ToString();
            }
            This.TextSizeSlider.ValueChanged -= This.TextSizeSlider_ValueChanged;
            This.TextSizeSlider.Value = PM.Profile.LogTextSize;
            This.TextSizeSlider.ValueChanged += This.TextSizeSlider_ValueChanged;
            This.DigitsSlider.Value = PM.Profile.Digits;
            Dig = PM.Profile.Digits;
            This.DigitsSlider.ValueChanged -= This.DigitsSlider_ValueChanged;
            This.Digits.Text = PM.Profile.Digits.ToString();
            This.DigitsSlider.ValueChanged += This.DigitsSlider_ValueChanged;

            This.VKInformerToggle.Checked -= This.VKInformerToggle_Click;
            This.VKInformerToggle.Unchecked -= This.VKInformerToggle_Click;
            This.VKuserID.TextChanged -= This.VKuserID_TextChanged;
            if (!PM.Profile.Informer.VkInform)
            {
                This.VKuserID.IsEnabled = false;
                This.VKInformerToggle.IsChecked = false;
            }
            else
            {
                This.VKuserID.IsEnabled = true;
                This.VKInformerToggle.IsChecked = true;
            }
            This.VKuserID.Text = PM.Profile.Informer.VKuserID;
            This.VKInformerToggle.Checked += This.VKInformerToggle_Click;
            This.VKInformerToggle.Unchecked += This.VKInformerToggle_Click;
            This.VKuserID.TextChanged += This.VKuserID_TextChanged;

            {// timeout sliders
                This.WachdogTimerSlider.ValueChanged -= This.WachdogTimerSlider_ValueChanged;
                This.IdleTimeoutSlider.ValueChanged -= This.IdleTimeoutSlider_ValueChanged;
                This.LHTimeoutSlider.ValueChanged -= This.LHTimeoutSlider_ValueChanged;

                This.WachdogTimerSlider.Value = PM.Profile.TimeoutWachdog;
                This.WachdogTimerSec.Text = PM.Profile.TimeoutWachdog.ToString();

                This.IdleTimeoutSlider.Minimum = PM.Profile.TimeoutWachdog;
                This.IdleTimeoutSlider.Value = PM.Profile.TimeoutIdle;
                This.IdleTimeoutSec.Text = PM.Profile.TimeoutIdle.ToString();

                This.LHTimeoutSlider.Value = PM.Profile.TimeoutLH;
                This.LHTimeoutSec.Text = PM.Profile.TimeoutLH.ToString();

                This.WachdogTimerSlider.ValueChanged += This.WachdogTimerSlider_ValueChanged;
                This.IdleTimeoutSlider.ValueChanged += This.IdleTimeoutSlider_ValueChanged;
                This.LHTimeoutSlider.ValueChanged += This.LHTimeoutSlider_ValueChanged;
            }
        }
        private void CreateDirectories()
        {
            DirectoryInfo dirInfo;
            string[] dirs = new string[] { "MinersLogs", "Claymore's Dual Miner", "Gminer", "Bminer" };
            foreach (string dir in dirs)
            {
                dirInfo = new DirectoryInfo(dir);
                if (!dirInfo.Exists)
                {
                    dirInfo.Create();
                }
            }
        }
        private void Autostart()
        {
            if (PM.Profile.Autostart) // && PM.Profile.StartedID != null
            {
                if (IM.InternetConnetction())
                {
                    IM.InformMessage("OMineGuard запущен");
                    MM.StartLastMiner(null);
                    TabConroller.SelectedIndex = 2;
                }
                else
                {
                    SystemMessage("Ожидание подключения к интернету");
                    while (true)
                    {
                        if (IM.InternetConnetction())
                        {
                            SystemMessage("Соединение с интернетом установлено");
                            IM.InformMessage("OMineGuard запущен, после установления интернет соединения");
                            MM.StartLastMiner(null);
                            TabConroller.SelectedIndex = 2;
                            break;
                        }
                        Thread.Sleep(1000);
                    }
                }
            }
        }
        #endregion
        #region RigName
        private void RigName_TextChanged(object sender, TextChangedEventArgs e)
        {
            PM.Profile.RigName = RigName.Text;
            PM.SaveProfile();
        }
        #endregion
        #region GPUs
        private void GPUsCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GPUswitchSP.Children.Clear();

            string Selected = (string)GPUsCB.SelectedItem;
            if (Selected == "Auto")
            {
                GPUsSwitchHeader.Visibility = Visibility.Hidden;
                GPUswitchB.Visibility = Visibility.Hidden;
                PM.Profile.GPUsSwitch = null;
            }
            else
            {
                GPUsSwitchHeader.Visibility = Visibility.Visible;
                GPUswitchB.Visibility = Visibility.Visible;
                byte k = Convert.ToByte(Selected);
                if (PM.Profile.GPUsSwitch == null)
                {
                    PM.Profile.GPUsSwitch = new bool[k];
                    for (int i = 0; i < k; i++)
                    {
                        PM.Profile.GPUsSwitch[i] = true;
                    }
                }
                else if (PM.Profile.GPUsSwitch.Length != k)
                {
                    PM.Profile.GPUsSwitch = new bool[k];
                    for (int i = 0; i < k; i++)
                    {
                        PM.Profile.GPUsSwitch[i] = true;
                    }
                }
                for (byte n = 0; n < k; n++)
                {
                    GPUswitchSP.Children.Add(new TextBlock { Foreground = Brushes.White, Text = "GPU" + n, FontSize = 12 });
                    CheckBox CB = new CheckBox { Name = "g" + n.ToString(), IsChecked = PM.Profile.GPUsSwitch[n], Margin = new Thickness(0, 0, 7, 0) };
                    CB.Checked += GPUCB_Checked;
                    CB.Unchecked += GPUCB_Unchecked;
                    GPUswitchSP.Children.Add(CB);
                }
            }
            PM.SaveProfile();
        }
        private void GPUCB_Checked(object sender, RoutedEventArgs e)
        {
            PM.Profile.GPUsSwitch[Convert.ToInt32(((CheckBox)sender).Name.ToCharArray()[1].ToString())] = true;
            PM.SaveProfile();
        }
        private void GPUCB_Unchecked(object sender, RoutedEventArgs e)
        {
            PM.Profile.GPUsSwitch[Convert.ToInt32(((CheckBox)sender).Name.ToCharArray()[1].ToString())] = false;
            PM.SaveProfile();
        }
        #endregion
        #region Miner
        private void MinusConfig_Click(object sender, RoutedEventArgs e)
        {
            int n = ConfigsList.SelectedIndex;
            if (n != -1)
            {
                if ((string)ConfigsList.SelectedItem == PM.GetConfig(PM.Profile.StartedID).Name)
                {
                    PM.Profile.StartedID = null;
                }
                PM.Profile.ConfigsList.RemoveAt(n);
                ConfigsList.ItemsSource = PM.Profile.ConfigsList.Select(W => W.Name);
                ConfigsList.SelectedIndex = -1;
                PM.SaveProfile();
            }
        }
        private void PlusConfig_Click(object sender, RoutedEventArgs e)
        {
            PM.Profile.ConfigsList.Add(new Profile.Config());
            ConfigsList.ItemsSource = PM.Profile.ConfigsList.Select(W => W.Name);
            ConfigsList.SelectedIndex = PM.Profile.ConfigsList.Count - 1;
        }
        private void ApplyConfig_Click(object sender, RoutedEventArgs e)
        {
            ApplyConfigM();
        }
        private bool ApplyConfigM()
        {
            int n = ConfigsList.SelectedIndex;

            if (n != -1)
            {
                PM.Profile.ConfigsList[n].Name = MiningConfigName.Text;
                PM.Profile.ConfigsList[n].Algoritm = Algotitm.Text;
                PM.Profile.ConfigsList[n].Miner = (SM.Miners?)Miner.SelectedItem;
                PM.Profile.ConfigsList[n].Pool = Pool.Text;
                PM.Profile.ConfigsList[n].Port = Port.Text;
                PM.Profile.ConfigsList[n].Wallet = Wallet.Text;
                PM.Profile.ConfigsList[n].Params = Params.Text;
                if (Overclock.SelectedIndex > 0)
                {
                    PM.Profile.ConfigsList[n].ClockID = PM.Profile.ClocksList.Where(w => w.Name == Overclock.Text).ToList()[0].ID;
                }
                else
                {
                    PM.Profile.ConfigsList[n].ClockID = null;
                }
                
                try
                {
                    PM.Profile.ConfigsList[n].MinHashrate = Convert.ToDouble(MinHashrate.Text);
                }
                catch
                {
                    PM.Profile.ConfigsList[n].MinHashrate = 0;
                }

                PM.SaveProfile();
                ConfigsList.ItemsSource = PM.Profile.ConfigsList.Select(W => W.Name);
                ConfigsList.SelectedIndex = n;
                return true;
            }
            else { return false; }
        }
        private void StartConfig_Click(object sender, RoutedEventArgs e)
        {
            if (ApplyConfigM())
            {
                StartConfig(PM.Profile.ConfigsList[ConfigsList.SelectedIndex].ID);
            }
        }
        public static void StartConfig(long ConfigId)
        {
            Profile.Config PC = PM.GetConfig(ConfigId);
            MM.StartMiner(PC);
            This.TabConroller.SelectedIndex = 2;
        }

        private void ConfigsList_Selected(object sender, RoutedEventArgs e)
        {
            int n = This.ConfigsList.SelectedIndex;
            if (n == -1)
            {
                This.MiningConfigName.Text = "";
                This.Algotitm.SelectedIndex = -1;
                This.Miner.SelectedIndex = -1;
                This.Overclock.SelectedIndex = -1;
                This.Pool.Text = "";
                This.Port.Text = "";
                This.Wallet.Text = "";
                This.Params.Text = "";
                This.MinHashrate.Text = "";
            }
            else
            {
                if (PM.Profile.ConfigsList[n].Algoritm != "")
                {
                    This.Algotitm.SelectedItem = PM.Profile.ConfigsList[n].Algoritm;
                }
                else This.Algotitm.SelectedIndex = -1;

                if (PM.Profile.ConfigsList[n].Miner != null)
                {
                    This.Miner.SelectedItem = PM.Profile.ConfigsList[n].Miner;
                }
                else This.Miner.SelectedIndex = -1;

                if (PM.Profile.ConfigsList[n].ClockID != null)
                {
                    This.Overclock.SelectedItem = PM.GetClock(PM.Profile.ConfigsList[n].ClockID).Name;
                }
                else This.Overclock.SelectedIndex = -1;

                This.MiningConfigName.Text = PM.Profile.ConfigsList[n].Name;
                This.Pool.Text = PM.Profile.ConfigsList[n].Pool;
                This.Port.Text = PM.Profile.ConfigsList[n].Port;
                This.Wallet.Text = PM.Profile.ConfigsList[n].Wallet;
                This.Params.Text = PM.Profile.ConfigsList[n].Params;
                This.MinHashrate.Text = PM.Profile.ConfigsList[n].MinHashrate.ToString();
            }
        }
        private void Algotitm_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Algotitm.SelectedIndex == -1)
            {
                Miner.SelectedIndex = -1;
                Miner.IsEnabled = false;
            }
            else
            {
                Miner.IsEnabled = true;
                Miner.ItemsSource = SM.MinersD[(string)Algotitm.SelectedItem];
            }
        }
        #endregion
        #region MinerLog
        private void TextSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextSizeTB != null)
            {
                MinerLog.FontSize = TextSizeSlider.Value;
                TextSizeTB.Text = TextSizeSlider.Value.ToString();
                PM.Profile.LogTextSize = TextSizeSlider.Value;
                PM.SaveProfile();
            }
        }
        private void Autoscroll_Checked(object sender, RoutedEventArgs e)
        { AutoScroll = ((bool)Autoscroll.IsChecked); }
        private void KillProcess_Click(object sender, RoutedEventArgs e)
        {
            string str = (string)KillProcess.Content;
            if (str == "Завершить процесс")
            {
                IM.StopWachdog();
                IM.StopLHWatchdog();
                IM.StopIdleWatchdog();
                MM.KillProcess();
                IM.ProcessСompleted = true;
                MM.Indication = false;
                context.Send(LowHwachdogmsg, "");
                context.Send(Idlewachdogmsg, "");
                context.Send(Wachdogmsg, "");
                context.Send(ShowMinerLogmsg, "");
            }
            if (str == "Запустить процесс")
            {
                MM.StartMiner(PM.GetConfig(PM.Profile.StartedID));
            }
        }
        #endregion
        #region Clock
        private void MinuClock_Click(object sender, RoutedEventArgs e)
        {
            int n = ClocksList.SelectedIndex;

            if (n != -1)
            {
                long id = PM.Profile.ClocksList[n].ID;
                foreach (Profile.Config c in PM.Profile.ConfigsList)
                {
                    if (c.ClockID == id)
                    {
                        c.ClockID = null;
                    }
                }

                PM.Profile.ClocksList.RemoveAt(n);
                ClocksList.ItemsSource = PM.Profile.ClocksList.Select(W => W.Name);
                ClocksList.SelectedIndex = -1;
                PM.SaveProfile();
                Overclock.ItemsSource = (new string[] { "" }).Concat(PM.Profile.ClocksList.Select(W => W.Name));
            }
        }
        private void PlusClock_Click(object sender, RoutedEventArgs e)
        {
            PM.Profile.ClocksList.Add(new Profile.Overclock());
            ClocksList.ItemsSource = PM.Profile.ClocksList.Select(W => W.Name);
            ClocksList.SelectedIndex = PM.Profile.ClocksList.Count - 1;
            Overclock.ItemsSource = (new string[] { "" }).Concat(PM.Profile.ClocksList.Select(W => W.Name));
            Overclock.SelectedIndex = PM.Profile.ClocksList.Count - 1;
        }
        private void SaveClock_Click(object sender, RoutedEventArgs e)
        {
            SaveClock();
        }
        private bool SaveClock()
        {
            int n = ClocksList.SelectedIndex;
            if (n != -1)
            {
                PM.Profile.ClocksList[n].Name = ClockName.Text;
                try
                {
                    if (PowLim.Text != "" && !(bool)SwitcherPL.IsChecked)
                    {
                        PM.Profile.ClocksList[n].PowLim =
                            JsonConvert.DeserializeObject<int[]>($"[{PowLim.Text}]");
                    }
                    else
                    {
                        PM.Profile.ClocksList[n].PowLim = null;
                    }
                }
                catch
                {
                    PowLim.Text = "Неправильный формат";
                    return false;
                }
                try
                {
                    if (CoreClock.Text != "" && !(bool)SwitcherCC.IsChecked)
                    {
                        PM.Profile.ClocksList[n].CoreClock =
                            JsonConvert.DeserializeObject<int[]>($"[{CoreClock.Text}]");
                    }
                    else
                    {
                        PM.Profile.ClocksList[n].CoreClock = null;
                    }
                }
                catch
                {
                    CoreClock.Text = "Неправильный формат";
                    return false;
                }
                try
                {
                    if (MemoryClock.Text != "" && !(bool)SwitcherMC.IsChecked)
                    {
                        PM.Profile.ClocksList[n].MemoryClock =
                            JsonConvert.DeserializeObject<int[]>($"[{MemoryClock.Text}]");
                    }
                    else
                    {
                        PM.Profile.ClocksList[n].MemoryClock = null;
                    }
                }
                catch
                {
                    MemoryClock.Text = "Неправильный формат";
                    return false;
                }
                try
                {
                    if (FanSpeed.Text != "" && !(bool)SwitcherFS.IsChecked)
                    {
                        PM.Profile.ClocksList[n].FanSpeed =
                            JsonConvert.DeserializeObject<uint[]>($"[{FanSpeed.Text}]");
                    }
                    else
                    {
                        PM.Profile.ClocksList[n].FanSpeed = null;
                    }
                }
                catch
                {
                    FanSpeed.Text = "Неправильный формат";
                    return false;
                }
                PM.SaveProfile();
                ClocksList.ItemsSource = PM.Profile.ClocksList.Select(W => W.Name);
                ClocksList.SelectedIndex = n;
                Overclock.ItemsSource = (new string[] { "" }).Concat(PM.Profile.ClocksList.Select(W => W.Name));

                int k = ConfigsList.SelectedIndex;
                if (k != -1)
                {
                    ConfigsList.SelectedIndex = -1;
                    ConfigsList.SelectedIndex = k;
                }
                return true;
            }
            else { return false; }
        }
        private void ApplyClock_Click(object sender, RoutedEventArgs e)
        {
            if (SaveClock())
            {
                int n = ClocksList.SelectedIndex;
                if (n != -1)
                {
                    OCM.ApplyOverclock(PM.Profile.ClocksList[ClocksList.SelectedIndex]);
                }
            }
        }
        private void ClocksList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int n = This.ClocksList.SelectedIndex;
            if (n == -1)
            {
                This.ClockName.IsEnabled = false;
                This.SwitcherPL.IsEnabled = false;
                This.SwitcherCC.IsEnabled = false;
                This.SwitcherMC.IsEnabled = false;
                This.SwitcherFS.IsEnabled = false;
                SetParam(This.SwitcherPL, This.PowLim);
                SetParam(This.SwitcherCC, This.CoreClock);
                SetParam(This.SwitcherMC, This.MemoryClock);
                SetParam(This.SwitcherFS, This.FanSpeed);
                This.ClockName.Text = "";
            }
            else
            {
                This.ClockName.Text = PM.Profile.ClocksList[n].Name;
                This.ClockName.IsEnabled = true;
                This.SwitcherPL.IsEnabled = true;
                This.SwitcherCC.IsEnabled = true;
                This.SwitcherMC.IsEnabled = true;
                This.SwitcherFS.IsEnabled = true;
                SetParam(This.SwitcherPL, This.PowLim, PM.Profile.ClocksList[n].PowLim);
                SetParam(This.SwitcherCC, This.CoreClock, PM.Profile.ClocksList[n].CoreClock);
                SetParam(This.SwitcherMC, This.MemoryClock, PM.Profile.ClocksList[n].MemoryClock);
                SetParam(This.SwitcherFS, This.FanSpeed, PM.Profile.ClocksList[n].FanSpeed);
            }
        }
        private static void SetParam(CheckBox CB, TextBox TB, int[] prams)
        {
            string str = "";
            if (prams == null)
            {
                CB.IsChecked = true;
                TB.Text = "";
            }
            else
            {
                CB.IsChecked = false;
                foreach (int x in prams)
                {
                    str += ToNChar(x.ToString());
                }
                TB.Text = " " + str.TrimStart(',');
            }
        }
        private static void SetParam(CheckBox CB, TextBox TB, uint[] prams)
        {
            string str = "";
            if (prams == null)
            {
                CB.IsChecked = true;
                TB.Text = "";
            }
            else
            {
                CB.IsChecked = false;
                foreach (uint x in prams)
                {
                    str += ToNChar(x.ToString());
                }
                TB.Text = " " + str.TrimStart(',');
            }
        }
        private static void SetParam(CheckBox CB, TextBox TB)
        {
            CB.IsChecked = true;
            TB.Text = "";
        }
        private void OCswitch_Checked(object sender, RoutedEventArgs e)
        {
            string nm = ((CheckBox)sender).Name;
            bool b = !((bool)((CheckBox)sender).IsChecked);
            switch (nm)
            {
                case "SwitcherPL":
                    PowLim.IsEnabled = b;
                    break;
                case "SwitcherCC":
                    CoreClock.IsEnabled = b;
                    break;
                case "SwitcherMC":
                    MemoryClock.IsEnabled = b;
                    break;
                case "SwitcherFS":
                    FanSpeed.IsEnabled = b;
                    break;
            }
        }
        #endregion
        #region ContextSends
        public static void Sethashrate(object o)
        {
            if (o != null)
            {
                string str = "";
                if ((double[])((object[])o)[0] != null)
                {
                    double[] x = (double[])((object[])o)[0];
                    foreach (double d in x)
                    {
                        str += ToNChar(d.ToString());
                    }
                    This.GPUsHashrate.Text = " " + str.TrimStart(',');
                    This.GPUsHashrate2.Text = " " + str.TrimStart(',');
                    This.TotalHashrate.Text = x.Sum().ToString().Replace(',', '.');
                    This.TotalHashrate2.Text = x.Sum().ToString().Replace(',', '.');
                    TCP.OMWsendState(x, TCP.OMWstateType.Hashrates);
                    TCP.OMWsendInform(x, TCP.OMWinformType.Hashrates);
                }
                else
                {
                    This.GPUsHashrate.Text = "";
                    This.GPUsHashrate2.Text = "";
                    This.TotalHashrate.Text = "";
                    This.TotalHashrate2.Text = "";
                    TCP.OMWsendState(null, TCP.OMWstateType.Hashrates);
                    TCP.OMWsendInform(null, TCP.OMWinformType.Hashrates);
                }
                if ((int[])((object[])o)[1] != null && !OCM.OHMisEnabled)
                {
                    int[] x = (int[])((object[])o)[1];

                    TCP.OMWsendState(x, TCP.OMWstateType.Temperatures);
                    TCP.OMWsendInform(x, TCP.OMWinformType.Temperatures);
                }
            }
            else
            {
                This.GPUsHashrate.Text = "";
                This.GPUsHashrate2.Text = "";
                This.TotalHashrate.Text = "";
                This.TotalHashrate2.Text = "";
                TCP.OMWsendState(null, TCP.OMWstateType.Hashrates);
                TCP.OMWsendInform(null, TCP.OMWinformType.Hashrates);
            }
        }
        public static void Setoverclock(object o)
        {
            OCM.Overclock Clock = (OCM.Overclock)o;

        }

        public static void SetMS(object o)
        {
            string[] MS = (string[])o;
            for (int i = 0; i < MS.Length; i++)
            {
                MS[i] = MS[i] ?? "null";
            }
            This.GPUsPowerLimit.Text = " " + MS[0].TrimStart(',');
            This.GPUsCoreClock.Text = " " + MS[1].TrimStart(',');
            This.GPUsMemoryClocks.Text = " " + MS[2].TrimStart(',');
            This.GPUsFans.Text = " " + MS[3].TrimStart(',');
            This.GPUsTemps.Text = " " + MS[4].TrimStart(',');
            This.GPUsTemps2.Text = " " + MS[4].TrimStart(',');
        }
        #endregion
        #region WachdogTimers
        private void WachdogTimerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            PM.Profile.TimeoutWachdog = Convert.ToInt32(WachdogTimerSlider.Value);
            int x = Convert.ToInt32(WachdogTimerSlider.Value);
            if (IdleTimeoutSlider.Value < x)
            {
                IdleTimeoutSlider.Value = x;
                IdleTimeoutSlider.Minimum = x;
            }
            else
            {
                IdleTimeoutSlider.Minimum = x;
            }
            WachdogTimerSec.Text = WachdogTimerSlider.Value.ToString();
            PM.SaveProfile();
        }
        private void IdleTimeoutSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            PM.Profile.TimeoutIdle = Convert.ToInt32(IdleTimeoutSlider.Value);
            IdleTimeoutSec.Text = IdleTimeoutSlider.Value.ToString();
            PM.SaveProfile();
        }
        private void LHTimeoutSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            PM.Profile.TimeoutLH = Convert.ToInt32(LHTimeoutSlider.Value);
            LHTimeoutSec.Text = LHTimeoutSlider.Value.ToString();
            PM.SaveProfile();
        }
        #endregion

        private static int Dig;
        public static string ToNChar(string s)
        {
            string ext = "";
            char[] ch = s.ToCharArray();
            Queue<char> st = new Queue<char>();
            for (int i = 0; i < Dig - 1; i++)
            { st.Enqueue(' '); }
            for (int i = 0; i < Dig - 1 && i < ch.Length; i++)
            {
                st.Enqueue(ch[i]);
                st.Dequeue();
            }
            ch = st.ToArray();
            for (int i = 0; i < Dig - 1; i++)
            { ext += ch[i]; }
            return "," + ext.Replace(',', '.');
        }
        private void DigitsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int n = Convert.ToInt32(DigitsSlider.Value);
            Dig = n; Digits.Text = n.ToString();
            PM.Profile.Digits = n;
            PM.SaveProfile();
        }
        public static void SystemMessage(string str)
        { context.Send(systemMessage, str); }
        private static void systemMessage(object o)
        {
            string str = (string)o;
            Brush br = Brushes.Yellow;

            TextRange tr = new TextRange(MainWindow.This.MinerLog.Document.ContentEnd,
                MainWindow.This.MinerLog.Document.ContentEnd);
            tr.Text = $"{DateTime.Now.ToString("dd MMMM - HH.mm.ss")} >> {str}";

            TCP.OMWsendState(tr.Text + Environment.NewLine, TCP.OMWstateType.Logging);

            tr.ApplyPropertyValue(TextElement.ForegroundProperty, br);
            This.MinerLog.AppendText(Environment.NewLine);
        }
        private void AutoStart_Checked(object sender, RoutedEventArgs e)
        {
            if (AutoStart.IsChecked == true)
            {
                PM.Profile.Autostart = true;
                PM.SaveProfile();
            }
            if (AutoStart.IsChecked == false)
            {
                PM.Profile.Autostart = false;
                PM.SaveProfile();
            }
        }
        public static void WriteGeneralLog(string str)
        {
            Task.Run(() =>
            {
                using (FileStream fstream = new FileStream("GeneralLogfile.txt", FileMode.Append))
                {
                    string DT = DateTime.Now.ToString("HH:mm:ss - dd.MM.yy");
                    byte[] array = System.Text.Encoding.Default.GetBytes($"{DT} | >> {str}");
                    fstream.Write(array, 0, array.Length);
                }
            });
        }
        private void VKInformerToggle_Click(object sender, RoutedEventArgs e)
        {
            if (VKInformerToggle.IsChecked == true)
            {
                VKuserID.IsEnabled = true;
                PM.Profile.Informer.VkInform = true;
            }
            else
            {
                VKuserID.IsEnabled = false;
                PM.Profile.Informer.VkInform = false;
            }
        }
        private void VKuserID_TextChanged(object sender, TextChangedEventArgs e)
        {
            PM.Profile.Informer.VKuserID = VKuserID.Text;
            PM.SaveProfile();
        }
        public static void LowHwachdogMSG(string msg) { context.Send(LowHwachdogmsg, msg); }
        private static void LowHwachdogmsg(object o)
        {
            string msg = (string)o;
            if (IM.ProcessСompleted) return;
            if (msg != "")
            {
                This.LowHWachdog.Text = msg;
                This.LowHWachdog.Visibility = Visibility.Visible;
            }
            else
            {
                This.LowHWachdog.Visibility = Visibility.Collapsed;
            }
            TCP.OMWsendState(msg, TCP.OMWstateType.LowHWachdog);
        }
        public static void IdlewachdogMSG(string msg) { context.Send(Idlewachdogmsg, msg); }
        private static void Idlewachdogmsg(object o)
        {
            string msg = (string)o;
            if(msg != "")
            {
                This.IdleWachdog.Text = msg;
                This.IdleWachdog.Visibility = Visibility.Visible;
            }
            else
            {
                This.IdleWachdog.Visibility = Visibility.Collapsed;
            }
            TCP.OMWsendState(msg, TCP.OMWstateType.IdleWachdog);
        }
        public static void WachdogMSG(string msg) { context.Send(Wachdogmsg, msg); }
        private static void Wachdogmsg(object o)
        {
            string msg = (string)o;
            if (msg != "")
            {
                This.WachdogINFO.Text = msg;
                This.WachdogINFO.Visibility = Visibility.Visible;
            }
            else
            {
                This.WachdogINFO.Visibility = Visibility.Collapsed;
            }
            TCP.OMWsendState(msg, TCP.OMWstateType.WachdogInfo);
        }

        public static Thread ShowMinerLogThread;
        private static ThreadStart ShowMinerLogTS = new ThreadStart(() => 
        {
            MM.ShowMinerLog = true;
            for (int i = 30; i > 0; i--)
            {
                ShowMinerLogMSG($"Просмотр сообщений майнера {i}");
                Thread.Sleep(1000);
            }
            ShowMinerLogMSG("");
            MM.ShowMinerLog = false;
        });
        private void ShowMinerLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowMinerLogThread.Abort();
            }
            catch { }
            ShowMinerLogThread = new Thread(ShowMinerLogTS);
            ShowMinerLogThread.Start();
        }
        public static void ShowMinerLogMSG(string msg) { context.Send(ShowMinerLogmsg, msg); }
        private static void ShowMinerLogmsg(object o)
        {
            string msg = (string)o;
            if (msg != "")
            {
                This.ShowMLogTB.Text = msg;
                This.ShowMLogTB.Visibility = Visibility.Visible;
            }
            else
            {
                This.ShowMLogTB.Visibility = Visibility.Collapsed;
            }
            TCP.OMWsendState(msg, TCP.OMWstateType.ShowMLogTB);
        }


        private void TCP_OMWsent(TCP.RootObject RO)
        {
            Task.Run(() => 
            {
                if (RO.Profile != null)
                {
                    PM.Profile = RO.Profile;
                    PM.SaveProfile();
                    context.Send((object o) => UpdateProfile(), null);
                }
                if (RO.ApplyClock != null)
                    OCM.ApplyOverclock(PM.GetClock(RO.ApplyClock));
                if (RO.RunConfig != null)
                    context.Send((object o) => StartConfig((long)RO.RunConfig), null);
                if (RO.StartProcess != null)
                    context.Send((object o) => MM.StartMiner(PM.GetConfig(PM.Profile.StartedID)), null);
                if (RO.KillProcess != null)
                {
                    IM.StopWachdog();
                    IM.StopLHWatchdog();
                    IM.StopIdleWatchdog();
                    MM.KillProcess();
                }
                if (RO.ShowMinerLog != null) ShowMinerLog_Click(null, null);
            });
        }
    }
}