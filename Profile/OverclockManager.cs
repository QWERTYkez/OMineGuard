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
        private static IHardware[] GPUs;
        private static List<ISensor> gpuTempSensors = new List<ISensor>();
        private static List<ISensor> gpuFanSensors = new List<ISensor>();
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

            /////////////////////

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
                    if(s.Name == "GPU Fan" && s.SensorType == SensorType.Control)
                    {
                        gpuFanSensors.Add(s);
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
                        foreach (IHardware h in GPUs)
                        {
                            h.Update();
                        }
                        SetGPUsTemps();
                        SetGPUsFans();
                        SetGPUsCoreClock();
                        SetGPUsMemoryClock();
                    }
                    catch { }
                    Thread.Sleep(1000);
                }
            });
        }
        private static void SetGPUsTemps()
        {
            string Temps = "";
            foreach (ISensor s in gpuTempSensors)
            {
                Temps += $", {s.Value.GetValueOrDefault().ToString()}";
            }
            if (Temps != "")
            {
                Temps = Temps.TrimStart(',', ' ');
            }
            MainWindow.context.Send((object o) => { MainWindow.This.GPUsTemps.Text = Temps; }, null);
        }
        private static void SetGPUsFans()
        {
            string Temps = "";
            foreach (ISensor s in gpuFanSensors)
            {
                Temps += $", {s.Value.GetValueOrDefault().ToString()}";
            }
            if (Temps != "")
            {
                Temps = Temps.TrimStart(',', ' ');
            }
            MainWindow.context.Send((object o) => { MainWindow.This.GPUsFans.Text = Temps; }, null);
        }
        private static void SetGPUsCoreClock()
        {
            string Temps = "";
            foreach (ISensor s in gpuCoreClockSensors)
            {
                double x = Math.Round(Convert.ToDouble(s.Value), MidpointRounding.AwayFromZero);
                Temps += $", {x.ToString()}";
            }
            if (Temps != "")
            {
                Temps = Temps.TrimStart(',', ' ');
            }
            MainWindow.context.Send((object o) => { MainWindow.This.GPUsCoreClock.Text = Temps; }, null);
        }
        private static void SetGPUsMemoryClock()
        {
            string Temps = "";
            foreach (ISensor s in gpuMemoryClockSensors)
            {
                double x = Math.Round(Convert.ToDouble(s.Value), MidpointRounding.AwayFromZero);
                Temps += $", {x.ToString()}";
            }
            if (Temps != "")
            {
                Temps = Temps.TrimStart(',', ' ');
            }
            MainWindow.context.Send((object o) => { MainWindow.This.GPUsMemoryClocks.Text = Temps; }, null);
        }
    }
}
