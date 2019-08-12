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
        public const string Ver = "1.4";
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

            Autostart();

            TCP.ServerStart();
            TCP.INFServerStart();
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
            This.ClocksList.ItemsSource = PM.Profile.ClocksList.Select(W => W.Name);
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
                if (Overclock.Text != "")
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
                Profile.Config PC = PM.Profile.ConfigsList[ConfigsList.SelectedIndex];
                MM.StartMiner(PC);
                TabConroller.SelectedIndex = 2;
            }
        }
        private void ConfigsList_Selected(object sender, RoutedEventArgs e)
        {
            int n = ConfigsList.SelectedIndex;
            if (n == -1)
            {
                MiningConfigName.Text = "";
                Algotitm.SelectedIndex = -1;
                Miner.SelectedIndex = -1;
                Overclock.SelectedIndex = -1;
                Pool.Text = "";
                Port.Text = "";
                Wallet.Text = "";
                Params.Text = "";
                MinHashrate.Text = "";
            }
            else
            {
                if (PM.Profile.ConfigsList[n].Algoritm != "")
                {
                    Algotitm.SelectedItem = PM.Profile.ConfigsList[n].Algoritm;
                }
                else Algotitm.SelectedIndex = -1;

                if (PM.Profile.ConfigsList[n].Miner != null)
                {
                    Miner.SelectedItem = PM.Profile.ConfigsList[n].Miner;
                }
                else Miner.SelectedIndex = -1;

                if (PM.Profile.ConfigsList[n].ClockID != null)
                {
                    Overclock.SelectedItem = PM.GetClock(PM.Profile.ConfigsList[n].ClockID).Name;
                }
                else Overclock.SelectedIndex = -1;

                MiningConfigName.Text = PM.Profile.ConfigsList[n].Name;
                Pool.Text = PM.Profile.ConfigsList[n].Pool;
                Port.Text = PM.Profile.ConfigsList[n].Port;
                Wallet.Text = PM.Profile.ConfigsList[n].Wallet;
                Params.Text = PM.Profile.ConfigsList[n].Params;
                MinHashrate.Text = PM.Profile.ConfigsList[n].MinHashrate.ToString();
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
            int n = ClocksList.SelectedIndex;
            if (n == -1)
            {
                ClockName.IsEnabled = false;
                SwitcherPL.IsEnabled = false;
                SwitcherCC.IsEnabled = false;
                SwitcherMC.IsEnabled = false;
                SwitcherFS.IsEnabled = false;
                SetParam(SwitcherPL, PowLim);
                SetParam(SwitcherCC, CoreClock);
                SetParam(SwitcherMC, MemoryClock);
                SetParam(SwitcherFS, FanSpeed);
                ClockName.Text = "";
            }
            else
            {
                ClockName.Text = PM.Profile.ClocksList[n].Name;
                ClockName.IsEnabled = true;
                SwitcherPL.IsEnabled = true;
                SwitcherCC.IsEnabled = true;
                SwitcherMC.IsEnabled = true;
                SwitcherFS.IsEnabled = true;
                SetParam(SwitcherPL, PowLim, PM.Profile.ClocksList[n].PowLim);
                SetParam(SwitcherCC, CoreClock, PM.Profile.ClocksList[n].CoreClock);
                SetParam(SwitcherMC, MemoryClock, PM.Profile.ClocksList[n].MemoryClock);
                SetParam(SwitcherFS, FanSpeed, PM.Profile.ClocksList[n].FanSpeed);
            }
        }
        private void SetParam(CheckBox CB, TextBox TB, int[] prams)
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
        private void SetParam(CheckBox CB, TextBox TB, uint[] prams)
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
        private void SetParam(CheckBox CB, TextBox TB)
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
                }
                else
                {
                    This.GPUsHashrate.Text = "";
                    This.GPUsHashrate2.Text = "";
                    This.TotalHashrate.Text = "";
                    This.TotalHashrate2.Text = "";
                }
                if (!OCM.OHMisEnabled)
                {
                    if ((int[])((object[])o)[1] != null)
                    {
                        str = "";
                        int[] y = (int[])((object[])o)[1];
                        foreach (double d in y)
                        {
                            str += ToNChar(d.ToString());
                        }
                        This.GPUsTemps.Text = " " + str.TrimStart(',');
                        This.GPUsTemps2.Text = " " + str.TrimStart(',');
                    }
                    else
                    {
                        This.GPUsTemps.Text = "";
                        This.GPUsTemps2.Text = "";
                    }
                }
            }
            else
            {
                This.GPUsHashrate.Text = "";
                This.GPUsHashrate2.Text = "";
                This.TotalHashrate.Text = "";
                This.TotalHashrate2.Text = "";
                if (!OCM.OHMisEnabled)
                {
                    This.GPUsTemps.Text = "";
                    This.GPUsTemps2.Text = "";
                }
            }
        }
        public static void SetMS1(object o)
        {
            string[] MS = (string[])o;
            for (int i = 0; i < MS.Length; i++)
            {
                MS[i] = MS[i] ?? "error";
            }
            This.GPUsPowerLimit.Text = " " + MS[0].TrimStart(',');
            This.GPUsCoreClock.Text = " " + MS[1].TrimStart(',');
            This.GPUsMemoryClocks.Text = " " + MS[2].TrimStart(',');
            This.GPUsFans.Text = " " + MS[3].TrimStart(',');
        }
        public static void SetMS2(object o)
        {
            string[] MS = (string[])o;
            for (int i = 0; i < MS.Length; i++)
            {
                MS[i] = MS[i] ?? "error";
            }
            This.GPUsTemps.Text = " " + MS[0].TrimStart(',');
            This.GPUsTemps2.Text = " " + MS[0].TrimStart(',');
            This.GPUsCoreClockAbs.Text = " " + MS[1].TrimStart(',');
            This.GPUsMemoryClocksAbs.Text = " " + MS[2].TrimStart(',');
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
            if (msg != "")
            {
                This.LowHWachdog.Text = msg;
                This.LowHWachdog.Visibility = Visibility.Visible;
            }
            else
            {
                This.LowHWachdog.Visibility = Visibility.Collapsed;
            }
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
        }
    }
}