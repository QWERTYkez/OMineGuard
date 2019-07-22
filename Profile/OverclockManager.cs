using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MSI.Afterburner;
using MSI.Afterburner.Exceptions;
using OpenHardwareMonitor.Hardware;

namespace OMineManager
{
    public static class OverclockManager
    {
        private static ControlMemory CM;
        private static int GPUsCount;
        private static IHardware[] GPUs;
        private static List<ISensor> gpuTempSensors = new List<ISensor>();
        private static List<ISensor> gpuCoreClockSensors = new List<ISensor>();
        private static List<ISensor> gpuMemoryClockSensors = new List<ISensor>();

        public static void Initialize()
        {
            Task.Run(() => { ConnectToMSI(); });
            Task.Run(() => { ConnectToOHM(); });
        }

        private static void ConnectToMSI()
        {
            if (Process.GetProcessesByName("MSIAfterburner").Length == 0)
            {
                while (Process.GetProcessesByName("MSIAfterburner").Length == 0)
                {
                    Thread.Sleep(50);
                }
            }
            bool b = false;
            while (!b)
            {
                try
                {
                    CM = new ControlMemory();
                    b = true;
                }
                catch { }
            }
            if (CM.GpuEntries[CM.GpuEntries.Length-1].PowerLimitCur == 0)
            { GPUsCount = CM.GpuEntries.Length - 1; }
        }
        private static void ConnectToOHM()
        {
            Computer c = new Computer { GPUEnabled = true };
            c.Open();
            GPUs = c.Hardware;

            foreach (IHardware h in GPUs)
            {
                foreach (ISensor s in h.Sensors)
                {
                    if (s.SensorType == SensorType.Temperature)
                    {
                        gpuTempSensors.Add(s);
                    }
                    if (s.Name == "GPU Core" && s.SensorType == SensorType.Clock)
                    {
                        gpuCoreClockSensors.Add(s);
                    }
                    if (s.Name == "GPU Memory" && s.SensorType == SensorType.Clock)
                    {
                        gpuMemoryClockSensors.Add(s);
                    }
                }
            }

            StartMonitoring();
        }
        private static void StartMonitoring()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        CM.ReloadAll();
                        string[] MS = new string[4];
                        for (int i = 0; i < GPUsCount; i++)
                        {
                            MS[0] += To5Char(CM.GpuEntries[i].PowerLimitCur.ToString() + "%");
                            MS[1] += To5Char((CM.GpuEntries[i].CoreClockBoostCur / 1000).ToString());
                            MS[2] += To5Char((CM.GpuEntries[i].MemoryClockBoostCur / 1000).ToString());
                            MS[3] += To5Char((CM.GpuEntries[i].FanSpeedCur).ToString() + "%");
                        }
                        MainWindow.context.Send((object o) => { MainWindow.This.GPUsPowerLimit.Text = MS[0]; }, null);
                        MainWindow.context.Send((object o) => { MainWindow.This.GPUsCoreClock.Text = MS[1]; }, null);
                        MainWindow.context.Send((object o) => { MainWindow.This.GPUsMemoryClocks.Text = MS[2]; }, null);
                        MainWindow.context.Send((object o) => { MainWindow.This.GPUsFans.Text = MS[3]; }, null);
                    }
                    catch { }
                    try
                    {
                        for (int i = 0; i < GPUsCount; i++)
                        {
                            GPUs[i].Update();
                        }
                        string[] MS = new string[3];
                        for (int i = 0; i < GPUsCount; i++)
                        {
                            MS[0] += To5Char(gpuTempSensors[i].Value.GetValueOrDefault().ToString() + "°C");
                            MS[1] += To5Char(Math.Round(Convert.ToDouble(gpuCoreClockSensors[i].Value), MidpointRounding.AwayFromZero).ToString());
                            MS[2] += To5Char(Math.Round(Convert.ToDouble(gpuMemoryClockSensors[i].Value), MidpointRounding.AwayFromZero).ToString());
                        }
                        MainWindow.context.Send((object o) => { MainWindow.This.GPUsTemps.Text = MS[0]; }, null);
                        MainWindow.context.Send((object o) => { MainWindow.This.GPUsCoreClockAbs.Text = MS[1]; }, null);
                        MainWindow.context.Send((object o) => { MainWindow.This.GPUsMemoryClocksAbs.Text = MS[2]; }, null);
                    }
                    catch { }
                    Thread.Sleep(1000);
                }
            });
        }
        private static string To5Char(string s)
        {
            char[] cc = { ' ', ' ', ' ', ' ', ' ' };
            char[] ch = s.ToCharArray();
            for (int i = ch.Length - 1, j = cc.Length - 1; i > -1; i--, j--)
            { cc[j] = ch[i]; }
            return $"{cc[0]}{cc[1]}{cc[2]}{cc[3]}{cc[4]}";
        }
    }
}
