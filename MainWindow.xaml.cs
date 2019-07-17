using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SM = OMineManager.SettingsManager;
using PM = OMineManager.ProfileManager;

namespace OMineManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            IniProfile();
        }

        #region InitializeProfile
        private void IniProfile()
        {
            SM.Initialize();
            PM.Initialize();
            Algotitm.ItemsSource = SM.Miners.Keys;
            GPUsCB.ItemsSource = new string[] { "Auto", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
            if (PM.Profile.GPUsSwitch != null)
            { GPUsCB.SelectedIndex = PM.Profile.GPUsSwitch.Length; }
            else { GPUsCB.SelectedIndex = 0; }
            if (PM.Profile.RigName != null)
            { RigName.Text = PM.Profile.RigName; }
            if (PM.Profile.ConfigsList == null)
            { PM.Profile.ConfigsList = new List<Profile.Config>(); }
            ConfigsList.ItemsSource = PM.Profile.ConfigsList.Select(W => W.Name);
        }
        #endregion
        #region RigName
        private void RigName_TextChanged(object sender, TextChangedEventArgs e)
        {
            PM.Profile.RigName = RigName.Text;
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
                PM.Profile.GPUsSwitch = null;
            }
            else
            {
                GPUsSwitchHeader.Visibility = Visibility.Visible;
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
                    WrapPanel WP = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) };
                    {
                        WP.Children.Add(new TextBlock { Foreground = Brushes.White, Text = "GPU" + n });
                        CheckBox CB = new CheckBox { Name = "g" + n.ToString(), IsChecked = PM.Profile.GPUsSwitch[n] };
                        CB.Checked += GPUCB_Checked;
                        CB.Unchecked += GPUCB_Unchecked;
                        WP.Children.Add(CB);
                    }
                    GPUswitchSP.Children.Add(WP);
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
                PM.Profile.ConfigsList.RemoveAt(n);
                ConfigsList.ItemsSource = PM.Profile.ConfigsList.Select(W => W.Name);
                ConfigsList.SelectedIndex = -1;
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

        }
        private void StartConfig_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ConfigsList_Selected(object sender, RoutedEventArgs e)
        {
            int n = ConfigsList.SelectedIndex;
            if (n == -1)
            {
                Algotitm.SelectedIndex = -1;
                Miner.SelectedIndex = -1;
                Pool.Text = "";
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
                if (PM.Profile.ConfigsList[n].Miner != "")
                {
                    Miner.SelectedItem = PM.Profile.ConfigsList[n].Miner;
                }
                else
                {
                    Miner.SelectedIndex = -1;
                }
                Pool.Text = PM.Profile.ConfigsList[n].Pool;
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
                Miner.ItemsSource = SM.Miners[(string)Algotitm.SelectedItem].Keys;
            }
        }
        #endregion


    }
}
