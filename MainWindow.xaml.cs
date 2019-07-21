using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PM = OMineManager.ProfileManager;
using SM = OMineManager.SettingsManager;
using MM = OMineManager.MinersManager;
using OCM = OMineManager.OverclockManager;
using System.Threading.Tasks;
using System.Diagnostics;

namespace OMineManager
{
    public partial class MainWindow : Window
    {
        public static MainWindow This;
        public static bool AutoScroll = true;
        public static SynchronizationContext context = SynchronizationContext.Current;

        public MainWindow()
        {
            InitializeComponent();
            This = this;
            IniProfile();
            OCM.Initialize();

        }

        #region InitializeProfile
        private void IniProfile()
        {
            PM.Initialize();
            Algotitm.ItemsSource = SM.MinersD.Keys;
            GPUsCB.ItemsSource = new string[] { "Auto", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
            if (PM.Profile.GPUsSwitch != null)
            { GPUsCB.SelectedIndex = PM.Profile.GPUsSwitch.Length; }
            else { GPUsCB.SelectedIndex = 0; }
            if (PM.Profile.RigName != null)
            { RigName.Text = PM.Profile.RigName; }
            if (PM.Profile.ConfigsList == null)
            { PM.Profile.ConfigsList = new List<Profile.Config>(); }
            ConfigsList.ItemsSource = PM.Profile.ConfigsList.Select(W => W.Name);
            ConfigsList.SelectedItem = PM.Profile.StartedConfig;
            if(PM.Profile.LogTextSize != 0)
            {
                MinerLog.FontSize = PM.Profile.LogTextSize;
                TextSizeTB.Text = PM.Profile.LogTextSize.ToString();
            }
            TextSizeSlider.Value = PM.Profile.LogTextSize;
            TextSizeSlider.ValueChanged += TextSizeSlider_ValueChanged;
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
                    GPUswitchSP.Children.Add(new TextBlock { Foreground = Brushes.White, Text = "GPU" + n, FontSize=12 });
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
                if ((string)ConfigsList.SelectedItem == PM.Profile.StartedConfig)
                {
                    PM.Profile.StartedConfig = null;
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

            if (PM.Profile.ConfigsList[n].Name == MiningConfigName.Text)
            {
                if (PM.Profile.StartedConfig == PM.Profile.ConfigsList[n].Name)
                {
                    PM.Profile.StartedConfig = MiningConfigName.Text;
                }
                PM.Profile.ConfigsList[n].Name = MiningConfigName.Text;
                PM.Profile.ConfigsList[n].Algoritm = Algotitm.Text;
                PM.Profile.ConfigsList[n].Miner = (SM.Miners)Miner.SelectedItem;
                PM.Profile.ConfigsList[n].Pool = Pool.Text;
                PM.Profile.ConfigsList[n].Port = Port.Text;
                PM.Profile.ConfigsList[n].Wallet = Wallet.Text;
                PM.Profile.ConfigsList[n].Params = Params.Text;
                PM.SaveProfile();
                ConfigsList.ItemsSource = PM.Profile.ConfigsList.Select(W => W.Name);
                ConfigsList.SelectedIndex = n;
                return true;
            }
            else if (!PM.Profile.ConfigsList.Select(W => W.Name).Contains(MiningConfigName.Text))
            {
                if (PM.Profile.StartedConfig == PM.Profile.ConfigsList[n].Name)
                {
                    PM.Profile.StartedConfig = MiningConfigName.Text;
                }
                PM.Profile.ConfigsList[n].Name = MiningConfigName.Text;
                PM.Profile.ConfigsList[n].Algoritm = Algotitm.Text;
                PM.Profile.ConfigsList[n].Miner = (SM.Miners)Miner.SelectedItem;
                PM.Profile.ConfigsList[n].Pool = Pool.Text;
                PM.Profile.ConfigsList[n].Port = Port.Text;
                PM.Profile.ConfigsList[n].Wallet = Wallet.Text;
                PM.Profile.ConfigsList[n].Params = Params.Text;
                PM.SaveProfile();
                ConfigsList.ItemsSource = PM.Profile.ConfigsList.Select(W => W.Name);
                ConfigsList.SelectedIndex = n;
                return true;
            }
            else
            {
                try
                {
                    MSGThread.Abort();
                }
                catch { }
                MSGtextBox.Visibility = Visibility.Visible;
                Task.Run(() =>
                {
                    MSGThread = Thread.CurrentThread;
                    Thread.Sleep(2000);
                    context.Send(msgmethod, null);
                });
                return false;
            }
        }
        Thread MSGThread;
        private void msgmethod(object o)
        {
            MSGtextBox.Visibility = Visibility.Collapsed;
        }
        private void StartConfig_Click(object sender, RoutedEventArgs e)
        {
            if (ApplyConfigM())
            {
                MinersManager.StartMiner(PM.Profile.ConfigsList[ConfigsList.SelectedIndex]);
                PM.Profile.StartedConfig = (string)ConfigsList.SelectedItem;
                PM.SaveProfile();
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
                Pool.Text = "";
                Port.Text = "";
                Wallet.Text = "";
                Params.Text = "";
            }
            else
            {
                if(PM.Profile.ConfigsList[n].Algoritm != "")
                {
                    Algotitm.SelectedItem = PM.Profile.ConfigsList[n].Algoritm;
                }
                else
                {
                    Algotitm.SelectedIndex = -1;
                }
                if (PM.Profile.ConfigsList[n].Miner != null)
                {
                    Miner.SelectedItem = PM.Profile.ConfigsList[n].Miner;
                }
                else
                {
                    Miner.SelectedIndex = -1;
                }
                MiningConfigName.Text = PM.Profile.ConfigsList[n].Name;
                Pool.Text = PM.Profile.ConfigsList[n].Pool;
                Port.Text = PM.Profile.ConfigsList[n].Port;
                Wallet.Text = PM.Profile.ConfigsList[n].Wallet;
                Params.Text = PM.Profile.ConfigsList[n].Params;
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
            if(str == "Завершить процесс")
            {
                MM.KillProcess();
            }
            if(str == "Запустить процесс")
            {
                MinersManager.StartMiner(PM.Profile.ConfigsList.Single(p => p.Name == PM.profile.StartedConfig));
            }
        }
        #endregion
    }
}