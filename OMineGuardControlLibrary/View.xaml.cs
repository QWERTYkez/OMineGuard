﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace OMineGuardControlLibrary
{
    public partial class View : UserControl
    {
        public View(IModel model)
        {
            InitializeComponent();
            OMGindication();

            DataContext = _ViewModel = new ViewModel(model);
            _ViewModel.PropertyChanged += MainWindow_PropertyChanged;
            _ViewModel.Autostarted += () => StartProfile(null, null);
        }

        private readonly ViewModel _ViewModel;

        public bool GPUsCanSelect { get; set; } = false;
        private void GPUsCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int countCurrent = GPUswitchSP.Children.Count;
            int countNew = _ViewModel.GPUsCountSelected;
            if (GPUsCanSelect)
            {
                while (countCurrent != countNew)
                {
                    if (countCurrent < countNew)
                    {
                        if (_ViewModel.GPUsSwitch.Count < countCurrent + 1)
                        {
                            _ViewModel.GPUsSwitch.Add(true);
                        }

                        Grid GR = new Grid { Width = 60 };
                        GR.Children.Add(new TextBlock { Text = "GPU" + countCurrent, Effect = null, Foreground = Brushes.White });
                        CheckBox CB = new CheckBox
                        {
                            Name = "g" + countCurrent.ToString(),
                            Margin = new Thickness(0, 0, 7, 0),
                            IsChecked = _ViewModel.GPUsSwitch[countCurrent],
                            HorizontalAlignment = HorizontalAlignment.Right
                        };
                        CB.Checked += SwitchGPU;
                        CB.Unchecked += SwitchGPU;
                        GR.Children.Add(CB);
                        GPUswitchSP.Children.Add(GR);
                        countCurrent++;
                    }
                    else
                    {
                        while (_ViewModel.GPUsSwitch.Count > countCurrent - 1)
                        {
                            _ViewModel.GPUsSwitch.RemoveAt(_ViewModel.GPUsSwitch.Count - 1);
                        }
                        GPUswitchSP.Children.RemoveAt(countCurrent - 1);
                        countCurrent--;
                    }
                }
                _ViewModel.ResetGPUs();
                _ViewModel.SetGPUsSwitch.Execute(null);
            }
            else
            {
                for (byte n = 0; n < countNew; n++)
                {
                    Grid GR = new Grid { Width = 60 };
                    GR.Children.Add(new TextBlock { Text = "GPU" + n, Effect = null, Foreground = Brushes.White });
                    CheckBox CB = new CheckBox
                    {
                        Name = "g" + n.ToString(),
                        Margin = new Thickness(0, 0, 7, 0),
                        IsChecked = _ViewModel.GPUsSwitch[n],
                        HorizontalAlignment = HorizontalAlignment.Right
                    };
                    CB.Checked += SwitchGPU;
                    CB.Unchecked += SwitchGPU;
                    GR.Children.Add(CB);
                    GPUswitchSP.Children.Add(GR);
                }
                GPUsCanSelect = true;
            }
        }
        private void SwitchGPU(object sender, RoutedEventArgs e)
        {
            int i = Convert.ToInt32(((CheckBox)sender).Name.Replace("g", ""));
            if (((CheckBox)sender).IsChecked == true)
            {
                _ViewModel.GPUsSwitch[i] = true;
            }
            else
            {
                _ViewModel.GPUsSwitch[i] = false;
            }
            _ViewModel.SetGPUsSwitch.Execute(null);
        }

        private void MainWindow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    switch (e.PropertyName)
                    {
                        case "GPUs":
                            {
                                int n = _ViewModel.GPUs;
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
                                SetTextBoxes(PowerLimits, "PowerLimits", n);
                                SetLabels(InfPowerLimits, "InfPowerLimits", PLsLengthA, PLsLengthB, n);
                                SetLengths(ArrPowerLimits, PLsLengthA, PLsLengthB);

                                SetTextBoxes(CoreClocks, "CoreClocks", n);
                                SetLabels(InfCoreClocks, "InfCoreClocks", CoresLengthA, CoresLengthB, n);
                                SetLengths(ArrCoreClocks, CoresLengthA, CoresLengthB);

                                SetLabels(InfOHMCoreClocks, "InfOHMCoreClocks", null, null, n);

                                SetTextBoxes(MemoryClocks, "MemoryClocks", n);
                                SetLabels(InfMemoryClocks, "InfMemoryClocks", MemorysLengthA, MemorysLengthB, n);
                                SetLengths(ArrMemoryClocks, MemorysLengthA, MemorysLengthB);

                                SetLabels(InfOHMMemoryClocks, "InfOHMMemoryClocks", null, null, n);

                                SetTextBoxes(FanSpeeds, "FanSpeeds", n);
                                SetLabels(InfoFanSpeeds, "InfFanSpeeds", FansLengthA, FansLengthB, n);
                                SetLengths(ArrFanSpeeds, FansLengthA, FansLengthB);

                                SetLabels(InfTemps, "InfTemperatures", TempsLengthA, TempsLengthB, n);
                                SetLengths(ArrTemperatures, TempsLengthA, TempsLengthB);

                                SetLabels(InfHashrates, "InfHashrates", HashesLengthA, HashesLengthB, n, "0.00");
                                SetLengths(ArrHashrates, HashesLengthA, HashesLengthB);

                                SetLabels(LogTemperatures, "InfTemperatures", n, "0°C");
                                SetLabels(LogHashrates, "InfHashrates", n, "0.00");
                            }
                            break;
                        case "Indication":
                            OMGworking = _ViewModel.Indication;
                            break;
                        case "Log":
                            if (_ViewModel.LogAutoscroll) LogScroller.ScrollToEnd();
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
                        case "InfOHMCoreClocks":
                            {
                                if (ArrOHMCoreClocks != _ViewModel.InfOHMCoreClocks)
                                {
                                    ArrOHMCoreClocks = _ViewModel.InfOHMCoreClocks;
                                }
                            }
                            break;
                        case "InfOHMMemoryClocks":
                            {
                                if (ArrOHMMemoryClocks != _ViewModel.InfOHMMemoryClocks)
                                {
                                    ArrOHMMemoryClocks = _ViewModel.InfOHMMemoryClocks;
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
                });
            }
            catch { }
            
        }
        private static void SetLengths(double?[] vals, List<ColumnDefinition> cdA, List<ColumnDefinition> cdB)
        {
            if (vals != null)
            {
                if (vals.Length > 0)
                {
                    double? mx = vals.Max().Value;
                    if (mx != null)
                    {
                        double? cr;
                        for (int i = 0; i < cdA.Count; i++)
                        {
                            try
                            {
                                cr = vals[i];
                                if (cr != null)
                                {
                                    try
                                    {
                                        cdA[i].Width = new GridLength(cr.Value / mx.Value, GridUnitType.Star);
                                        cdB[i].Width = new GridLength((mx.Value - cr.Value) / mx.Value, GridUnitType.Star);
                                    }
                                    catch { }
                                }
                                else
                                {
                                    try
                                    {
                                        cdA[i].Width = new GridLength(0, GridUnitType.Star);
                                        cdB[i].Width = new GridLength(1, GridUnitType.Star);
                                    }
                                    catch { }
                                }
                            }
                            catch
                            {
                                try
                                {
                                    cdA[i].Width = new GridLength(0, GridUnitType.Star);
                                    cdB[i].Width = new GridLength(1, GridUnitType.Star);
                                }
                                catch { }
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < cdA.Count; i++)
                        {
                            try
                            {
                                cdA[i].Width = new GridLength(0, GridUnitType.Star);
                                cdB[i].Width = new GridLength(1, GridUnitType.Star);
                            }
                            catch { }
                        }
                    }
                }
            }
        }
        private static void SetLengths(int?[] vals, List<ColumnDefinition> cdA, List<ColumnDefinition> cdB)
        {
            if (vals != null)
            {
                if (vals.Length > 0)
                {
                    double? mx = vals.Max().Value;
                    if (mx != null)
                    {
                        double? cr;
                        for (int i = 0; i < cdA.Count; i++)
                        {
                            try
                            {
                                cr = vals[i];
                                if (cr != null)
                                {
                                    try
                                    {
                                        cdA[i].Width = new GridLength(cr.Value / mx.Value, GridUnitType.Star);
                                        cdB[i].Width = new GridLength((mx.Value - cr.Value) / mx.Value, GridUnitType.Star);
                                    }
                                    catch { }
                                }
                                else
                                {
                                    try
                                    {
                                        cdA[i].Width = new GridLength(0, GridUnitType.Star);
                                        cdB[i].Width = new GridLength(1, GridUnitType.Star);
                                    }
                                    catch { }
                                }
                            }
                            catch
                            {
                                try
                                {
                                    cdA[i].Width = new GridLength(0, GridUnitType.Star);
                                    cdB[i].Width = new GridLength(1, GridUnitType.Star);
                                }
                                catch { }
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < cdA.Count; i++)
                        {
                            try
                            {
                                cdA[i].Width = new GridLength(0, GridUnitType.Star);
                                cdB[i].Width = new GridLength(1, GridUnitType.Star);
                            }
                            catch { }
                        }
                    }
                }
            }
        }
        private static void SetLengths(int[] vals, List<ColumnDefinition> cdA, List<ColumnDefinition> cdB)
        {
            if (vals != null)
            {
                if (vals.Length > 0)
                {
                    double? mx = vals.Max();
                    if (mx != null)
                    {
                        double? cr;
                        for (int i = 0; i < cdA.Count; i++)
                        {
                            try
                            {
                                cr = vals[i];
                                if (cr != null)
                                {
                                    try
                                    {
                                        cdA[i].Width = new GridLength(cr.Value / mx.Value, GridUnitType.Star);
                                        cdB[i].Width = new GridLength((mx.Value - cr.Value) / mx.Value, GridUnitType.Star);
                                    }
                                    catch { }
                                }
                                else
                                {
                                    try
                                    {
                                        cdA[i].Width = new GridLength(0, GridUnitType.Star);
                                        cdB[i].Width = new GridLength(1, GridUnitType.Star);
                                    }
                                    catch { }
                                }
                            }
                            catch
                            {
                                try
                                {
                                    cdA[i].Width = new GridLength(0, GridUnitType.Star);
                                    cdB[i].Width = new GridLength(1, GridUnitType.Star);
                                }
                                catch { }
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < cdA.Count; i++)
                        {
                            try
                            {
                                cdA[i].Width = new GridLength(0, GridUnitType.Star);
                                cdB[i].Width = new GridLength(1, GridUnitType.Star);
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        private int[] ArrPowerLimits;
        private readonly List<ColumnDefinition> PLsLengthA = new List<ColumnDefinition>();
        private readonly List<ColumnDefinition> PLsLengthB = new List<ColumnDefinition>();
        private int[] ArrCoreClocks;
        private readonly List<ColumnDefinition> CoresLengthA = new List<ColumnDefinition>();
        private readonly List<ColumnDefinition> CoresLengthB = new List<ColumnDefinition>();
        private int?[] ArrOHMCoreClocks;
        private int[] ArrMemoryClocks;
        private readonly List<ColumnDefinition> MemorysLengthA = new List<ColumnDefinition>();
        private readonly List<ColumnDefinition> MemorysLengthB = new List<ColumnDefinition>();
        private int?[] ArrOHMMemoryClocks;
        private int?[] ArrFanSpeeds;
        private readonly List<ColumnDefinition> FansLengthA = new List<ColumnDefinition>();
        private readonly List<ColumnDefinition> FansLengthB = new List<ColumnDefinition>();
        private int?[] ArrTemperatures;
        private readonly List<ColumnDefinition> TempsLengthA = new List<ColumnDefinition>();
        private readonly List<ColumnDefinition> TempsLengthB = new List<ColumnDefinition>();
        private double?[] ArrHashrates;
        private readonly List<ColumnDefinition> HashesLengthA = new List<ColumnDefinition>();
        private readonly List<ColumnDefinition> HashesLengthB = new List<ColumnDefinition>();

        private void SetLabels(StackPanel SP, string prop,
            List<ColumnDefinition> LengthsA, List<ColumnDefinition> LengthsB, int length, string format = null)
        {
            SP.Children.Clear();
            if (LengthsA != null && LengthsB != null)
            {
                LengthsA.Clear();
                LengthsB.Clear();
            }
            for (int i = 0; i < length; i++)
            {
                Grid GRD = new Grid();
                {
                    if (LengthsA != null && LengthsB != null)
                    {
                        Grid GRDD = new Grid();
                        {
                            ColumnDefinition cdA = new ColumnDefinition { Width = new GridLength(0, GridUnitType.Star) };
                            LengthsA.Add(cdA);
                            GRDD.ColumnDefinitions.Add(cdA);

                            ColumnDefinition cdB = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
                            LengthsB.Add(cdB);
                            GRDD.ColumnDefinitions.Add(cdB);
                        }
                        {
                            Grid grd = new Grid { Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)) };
                            Grid.SetColumn(grd, 0);

                            GRDD.Children.Add(grd);
                        }
                        GRD.Children.Add(GRDD);
                    }

                    Label Lb = new Label
                    {
                        Height = 26,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 14,
                    };
                    if (format != null) Lb.ContentStringFormat = format;
                    Lb.SetBinding(Label.ContentProperty, $"{prop}[{i}]");

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
                Binding b = new Binding($"{prop}") { Converter = new Styles.ListIntExistToBool() };
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
                        try
                        {
                            Dispatcher.Invoke(() =>
                            {
                                IndicatorEl.Fill = Brushes.Lime;
                                IndicatorEl2.Fill = Brushes.Lime;
                            });
                            Thread.Sleep(700);
                            Dispatcher.Invoke(() =>
                            {
                                IndicatorEl.Fill = null;
                                IndicatorEl2.Fill = null;
                            });
                            Thread.Sleep(300);
                        }
                        catch { }
                    }
                    while (OMGworking == false)
                    {
                        try
                        {
                            Dispatcher.Invoke(() =>
                            {
                                IndicatorEl.Fill = Brushes.Red;
                                IndicatorEl2.Fill = Brushes.Red;
                            });
                            Thread.Sleep(200);
                        }
                        catch { }
                    }
                }
            });
        }

        private void StartProfile(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                TABCON.SelectedIndex = 2;
            });
        }
    }
}
