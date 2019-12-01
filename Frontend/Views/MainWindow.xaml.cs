using OMineGuard.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace OMineGuard.Views
{
    public partial class MainWindow : Window
    {
        public const string Ver = "1.8";

        public MainWindow()
        {
            InitializeComponent();
            _context = SynchronizationContext.Current;
            OMGindication();

            _ViewModel = (MainViewModel)DataContext;
            _ViewModel.PropertyChanged += MainWindow_PropertyChanged;
        }

        private SynchronizationContext _context;
        private MainViewModel _ViewModel;

        private void GPUsCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int k = ((MainViewModel)DataContext).GPUsCountSelected;
            ((MainViewModel)DataContext).ResetGPUs();
            ComboBox ComB = (ComboBox)sender;
            if (((MainViewModel)DataContext).GPUsCanSelect)
            {
                GPUswitchSP.Children.Clear();

                if (k < 1)
                {
                    ((MainViewModel)DataContext).GPUsSwitch = null;
                }
                else
                {
                    ((MainViewModel)DataContext).GPUsSwitch = new List<bool>();
                    for (int i = 0; i < k; i++)
                    {
                        ((MainViewModel)DataContext).GPUsSwitch.Add(true);
                    }
                    for (byte n = 0; n < k; n++)
                    {
                        Grid GR = new Grid { Width = 60 };
                        GR.Children.Add(new TextBlock { Text = "GPU" + n, Effect = null, Foreground = Brushes.White });
                        CheckBox CB = new CheckBox
                        {
                            Name = "g" + n.ToString(),
                            Margin = new Thickness(0, 0, 7, 0),
                            IsChecked = true,
                            HorizontalAlignment = HorizontalAlignment.Right
                        };
                        CB.Checked += SwitchGPU;
                        CB.Unchecked += SwitchGPU;
                        GR.Children.Add(CB);
                        GPUswitchSP.Children.Add(GR);
                    }
                    ((MainViewModel)DataContext).SetGPUsSwitch.Execute(null);
                }
            }
            else
            {
                for (byte n = 0; n < k; n++)
                {
                    Grid GR = new Grid { Width = 60 };
                    GR.Children.Add(new TextBlock { Text = "GPU" + n, Effect = null, Foreground = Brushes.White });
                    CheckBox CB = new CheckBox
                    {
                        Name = "g" + n.ToString(),
                        Margin = new Thickness(0, 0, 7, 0),
                        IsChecked = ((MainViewModel)DataContext).GPUsSwitch[n],
                        HorizontalAlignment = HorizontalAlignment.Right
                    };
                    CB.Checked += SwitchGPU;
                    CB.Unchecked += SwitchGPU;
                    GR.Children.Add(CB);
                    GPUswitchSP.Children.Add(GR);
                }
                ((MainViewModel)DataContext).GPUsCanSelect = true;
            }
        }
        private void SwitchGPU(object sender, RoutedEventArgs e)
        {
            int i = Convert.ToInt32(((CheckBox)sender).Name.Replace("g", ""));
            if (((CheckBox)sender).IsChecked == true)
            {
                ((MainViewModel)DataContext).GPUsSwitch[i] = true;
            }
            else
            {
                ((MainViewModel)DataContext).GPUsSwitch[i] = false;
            }
            ((MainViewModel)DataContext).SetGPUsSwitch.Execute(null);
        }

        private void MainWindow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _context.Send((object o) =>
            {
                switch (e.PropertyName)
                {
                    case "GPUs":
                        {
                            int n = ((MainViewModel)DataContext).GPUs;
                            /*SetGPUs*/
                            {
                                GPUs.Children.Clear();
                                for (int i = 0; i < n; i++)
                                {
                                    Label Lb = new Label
                                    {
                                        Height = 26,
                                        HorizontalContentAlignment = HorizontalAlignment.Center,
                                        VerticalContentAlignment = VerticalAlignment.Center,
                                        FontFamily = new FontFamily("Consolas"),
                                        FontSize = 14,
                                        Content = $"GPU{i}"
                                    };
                                    GPUs.Children.Add(Lb);
                                }
                            }
                            SetLabels(InfPowerLimits, "InfPowerLimits", PLsLengthA, PLsLengthB, n);
                            SetTextBoxes(PowerLimits, "PowerLimits", n);
                            SetLabels(InfCoreClocks, "InfCoreClocks", CoresLengthA, CoresLengthB, n);
                            SetTextBoxes(CoreClocks, "CoreClocks", n);
                            SetLabels(InfMemoryClocks, "InfMemoryClocks", MemorysLengthA, MemorysLengthB, n);
                            SetTextBoxes(MemoryClocks, "MemoryClocks", n);
                            SetLabels(InfoFanSpeeds, "InfFanSpeeds", FansLengthA, FansLengthB, n);
                            SetTextBoxes(FanSpeeds, "FanSpeeds", n);
                            SetLabels(InfTemps, "InfTemperatures", TempsLengthA, TempsLengthB, n);
                            SetLabels(LogTemperatures, "InfTemperatures", n, "0°C");
                            SetLabels(InfHashrates, "InfHashrates", HashesLengthA, HashesLengthB, n, "0.00");
                            SetLabels(LogHashrates, "InfHashrates", n, "0.00");
                        }
                        break;
                    case "Indication":
                        OMGworking = ((MainViewModel)DataContext).Indication;
                        break;
                    case "Log":
                        if (((MainViewModel)DataContext).LogAutoscroll) LogScroller.ScrollToEnd();
                        break;
                    case "InfPowerLimits":
                        {
                            if (ArrPowerLimits != _ViewModel.InfPowerLimits)
                            {
                                ArrPowerLimits = _ViewModel.InfPowerLimits;
                                SetLengths(ArrPowerLimits, PLsLengthA, PLsLengthB);
                            }
                        }
                        break;
                    case "InfCoreClocks":
                        {
                            if (ArrCoreClocks != _ViewModel.InfCoreClocks)
                            {
                                ArrCoreClocks = _ViewModel.InfCoreClocks;
                                SetLengths(ArrCoreClocks, CoresLengthA, CoresLengthB);
                            }
                        }
                        break;
                    case "InfMemoryClocks":
                        {
                            if (ArrMemoryClocks != _ViewModel.InfMemoryClocks)
                            {
                                ArrMemoryClocks = _ViewModel.InfMemoryClocks;
                                SetLengths(ArrMemoryClocks, MemorysLengthA, MemorysLengthB);
                            }
                        }
                        break;
                    case "InfFanSpeeds":
                        {
                            if (ArrFanSpeeds != _ViewModel.InfFanSpeeds)
                            {
                                ArrFanSpeeds = _ViewModel.InfFanSpeeds;
                                SetLengths(ArrFanSpeeds, FansLengthA, FansLengthB);
                            }
                        }
                        break;
                    case "InfTemperatures":
                        {
                            if (ArrTemperatures != _ViewModel.InfTemperatures)
                            {
                                ArrTemperatures = _ViewModel.InfTemperatures;
                                SetLengths(ArrTemperatures, TempsLengthA, TempsLengthB);
                            }
                        }
                        break;
                    case "InfHashrates":
                        {
                            if (ArrHashrates != _ViewModel.InfHashrates)
                            {
                                ArrHashrates = _ViewModel.InfHashrates;
                                SetLengths(ArrHashrates, HashesLengthA, HashesLengthB);
                            }
                        }
                        break;
                }
            },
            null);
        }
        private static void SetLengths(int[] vals, List<ColumnDefinition> cdA, List<ColumnDefinition> cdB)
        {
            if (vals.Length > 0)
            {
                double mx = vals.Max();
                for (int i = 0; i < vals.Length; i++)
                {
                    double cr = vals[i];
                    cdA[i].Width = new GridLength(cr / mx, GridUnitType.Star);
                    cdB[i].Width = new GridLength((mx - cr) / mx, GridUnitType.Star);
                }
            }
        }
        private static void SetLengths(double[] vals, List<ColumnDefinition> cdA, List<ColumnDefinition> cdB)
        {
            if (vals.Length > 0)
            {
                double mx = vals.Max();
                for (int i = 0; i < vals.Length; i++)
                {
                    double cr = vals[i];
                    cdA[i].Width = new GridLength(cr / mx, GridUnitType.Star);
                    cdB[i].Width = new GridLength((mx - cr) / mx, GridUnitType.Star);
                }
            }
        }

        private int[] ArrPowerLimits;
        private List<ColumnDefinition> PLsLengthA = new List<ColumnDefinition>();
        private List<ColumnDefinition> PLsLengthB = new List<ColumnDefinition>();
        private int[] ArrCoreClocks;
        private List<ColumnDefinition> CoresLengthA = new List<ColumnDefinition>();
        private List<ColumnDefinition> CoresLengthB = new List<ColumnDefinition>();
        private int[] ArrMemoryClocks;
        private List<ColumnDefinition> MemorysLengthA = new List<ColumnDefinition>();
        private List<ColumnDefinition> MemorysLengthB = new List<ColumnDefinition>();
        private int[] ArrFanSpeeds;
        private List<ColumnDefinition> FansLengthA = new List<ColumnDefinition>();
        private List<ColumnDefinition> FansLengthB = new List<ColumnDefinition>();
        private int[] ArrTemperatures;
        private List<ColumnDefinition> TempsLengthA = new List<ColumnDefinition>();
        private List<ColumnDefinition> TempsLengthB = new List<ColumnDefinition>();
        private double[] ArrHashrates;
        private List<ColumnDefinition> HashesLengthA = new List<ColumnDefinition>();
        private List<ColumnDefinition> HashesLengthB = new List<ColumnDefinition>();

        private void SetLabels(StackPanel SP, string prop,
            List<ColumnDefinition> LengthsA, List<ColumnDefinition> LengthsB, int length)
        {
            SP.Children.Clear();
            LengthsA.Clear();
            LengthsB.Clear();
            for (int i = 0; i < length; i++)
            {
                Grid GRD = new Grid();
                {
                    Grid GRDD = new Grid();
                    {
                        ColumnDefinition cdA = new ColumnDefinition();
                        LengthsA.Add(cdA);
                        GRDD.ColumnDefinitions.Add(cdA);

                        ColumnDefinition cdB = new ColumnDefinition();
                        LengthsB.Add(cdB);
                        GRDD.ColumnDefinitions.Add(cdB);
                    }
                    {
                        Grid grd = new Grid { Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)) };
                        Grid.SetColumn(grd, 0);

                        GRDD.Children.Add(grd);
                    }

                    Label Lb = new Label
                    {
                        Height = 26,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 14
                    };
                    Lb.SetBinding(Label.ContentProperty, $"{prop}[{i}]");

                    GRD.Children.Add(GRDD);
                    GRD.Children.Add(Lb);
                }
                SP.Children.Add(GRD);
            }
        }
        private void SetLabels(StackPanel SP, string prop,
            List<ColumnDefinition> LengthsA, List<ColumnDefinition> LengthsB, int length, string format)
        {
            SP.Children.Clear();
            LengthsA.Clear();
            LengthsB.Clear();
            for (int i = 0; i < length; i++)
            {
                Grid GRD = new Grid();
                {
                    Grid GRDD = new Grid();
                    {
                        ColumnDefinition cdA = new ColumnDefinition();
                        LengthsA.Add(cdA);
                        GRDD.ColumnDefinitions.Add(cdA);

                        ColumnDefinition cdB = new ColumnDefinition();
                        LengthsB.Add(cdB);
                        GRDD.ColumnDefinitions.Add(cdB);
                    }
                    {
                        Grid grd = new Grid { Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)) };
                        Grid.SetColumn(grd, 0);

                        GRDD.Children.Add(grd);
                    }

                    Label Lb = new Label
                    {
                        Height = 26,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 14,
                        ContentStringFormat = format
                    };
                    Lb.SetBinding(Label.ContentProperty, $"{prop}[{i}]");

                    GRD.Children.Add(GRDD);
                    GRD.Children.Add(Lb);
                }
                SP.Children.Add(GRD);
            }
        }
        private void SetLabels(WrapPanel WP, string prop, int length, string format)
        {
            WP.Children.Clear();
            for (int i = 0; i < length; i++)
            {
                Label Lb = new Label
                {
                    Width = 65,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    ContentStringFormat = format
                };
                Lb.SetBinding(Label.ContentProperty, $"{prop}[{i}]");
                WP.Children.Add(Lb);
            }
        }
        private void SetTextBoxes(StackPanel SP, string prop, int length)
        {
            SP.Children.Clear();
            for (int i = 0; i < length; i++)
            {
                TextBox Tb = new TextBox
                {
                    Height = 26,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14
                };
                Tb.SetBinding(TextBox.TextProperty, $"{prop}[{i}]");
                Binding b = new Binding($"{prop}");
                b.Converter = new Styles.ListIntExistToBool();
                Tb.SetBinding(TextBox.IsEnabledProperty, b);
                SP.Children.Add(Tb);
            }
        }

        private static bool? OMGworking;
        private void OMGindication()
        {
            Task.Run(() =>
            {
                OMGworking = false;
                while (OMGworking != null)
                {
                    while (OMGworking == true)
                    {
                        _context.Send((object o) =>
                        {
                            IndicatorEl.Fill = Brushes.Lime;
                            IndicatorEl2.Fill = Brushes.Lime;
                        },
                        null);
                        Thread.Sleep(700);
                        _context.Send((object o) =>
                        {
                            IndicatorEl.Fill = null;
                            IndicatorEl2.Fill = null;
                        },
                        null);
                        Thread.Sleep(300);
                    }
                    while (OMGworking == false)
                    {
                        _context.Send((object o) =>
                        {
                            IndicatorEl.Fill = Brushes.Red;
                            IndicatorEl2.Fill = Brushes.Red;
                        },
                        null);
                        Thread.Sleep(200);
                    }
                }
            });
        }
    }
}
