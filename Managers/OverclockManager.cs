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
using PM = OMineGuard.ProfileManager;

namespace OMineGuard
{
    public static class OverclockManager
    {
        private static ControlMemory CM; 
        private static int GPUsCount;
        private static IHardware[] GPUs;
        private static List<ISensor> gpuTempSensors = new List<ISensor>();
        private static List<ISensor> gpuCoreClockSensors = new List<ISensor>();
        private static List<ISensor> gpuMemoryClockSensors = new List<ISensor>();
        public static bool OHMisEnabled = false;
        public static bool MSIconnecting = false;

        public static void Initialize()
        {
            Task.Run(() => { ConnectToMSI(); });
        }

        private static void ConnectToMSI()
        {
            while (Process.GetProcessesByName("MSIAfterburner").Length == 0)
            {
                Thread.Sleep(100);
            }
            while (!MSIconnecting)
            {
                try
                {
                    CM = new ControlMemory();
                    MSIconnecting = true;
                    ConnectToOHM();
                }
                catch { }
            }
            if (CM.GpuEntries[CM.GpuEntries.Length - 1].PowerLimitCur == 0)
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
            if (gpuTempSensors.Count > 0) OHMisEnabled = true;

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
                            MS[0] += MainWindow.ToNChar(CM.GpuEntries[i].PowerLimitCur.ToString() + "%");
                            MS[1] += MainWindow.ToNChar((CM.GpuEntries[i].CoreClockBoostCur / 1000).ToString());
                            MS[2] += MainWindow.ToNChar((CM.GpuEntries[i].MemoryClockBoostCur / 1000).ToString());
                            MS[3] += MainWindow.ToNChar((CM.GpuEntries[i].FanSpeedCur).ToString() + "%");
                        }
                        MainWindow.context.Send(MainWindow.SetMS1, MS);
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
                            MS[0] += MainWindow.ToNChar(gpuTempSensors[i].Value.GetValueOrDefault().ToString() + "°C");
                            MS[1] += MainWindow.ToNChar(Math.Round(Convert.ToDouble(gpuCoreClockSensors[i].Value), MidpointRounding.AwayFromZero).ToString());
                            MS[2] += MainWindow.ToNChar(Math.Round(Convert.ToDouble(gpuMemoryClockSensors[i].Value), MidpointRounding.AwayFromZero).ToString());
                        }
                        if (MS[0] != null)
                        {
                            MainWindow.context.Send(MainWindow.SetMS2, MS);
                        }
                    }
                    catch { }
                    Thread.Sleep(1000);
                }
            });
        }
        private static int SetParam(int i, int multiply, int[] param, int max, int min, int def)
        {
            if (param != null)
            {
                if (param.Length > i)
                {
                    if (param[i] * multiply > max)
                    {
                        return max;
                    }
                    else if (param[i] * multiply < min)
                    {
                        return min;
                    }
                    else
                    {
                        return param[i] * multiply;
                    }
                }
                else { return def; }
            }
            else { return def; }
        }
        public static void ApplyOverclock(Profile.Overclock OC)
        {
            MainWindow.context.Send((object o) => 
            {
                MainWindow.This.ClocksList.SelectedIndex =
                PM.Profile.ClocksList.Select(W => W.Name).ToList().IndexOf(OC.Name);
            }, null);
            Task.Run(() => 
            {
                CM.ReloadAll();
                ControlMemory nConf = CM;
                if (nConf.GpuEntries.Length > 0)
                {
                    while (nConf.GpuEntries[0].PowerLimitMax == 0)
                    {
                        Thread.Sleep(50);
                        CM.ReloadAll();
                        nConf = CM;
                    }
                }

                for (int i = 0; i < nConf.GpuEntries.Length; i++)
                {
                    try
                    {
                        nConf.GpuEntries[i].PowerLimitCur = SetParam(i, 1, OC.PowLim,
                        nConf.GpuEntries[i].PowerLimitMax,
                        nConf.GpuEntries[i].PowerLimitMin,
                        nConf.GpuEntries[i].PowerLimitDef);
                    }
                    catch { }
                    try
                    {
                        nConf.GpuEntries[i].CoreClockBoostCur = SetParam(i, 1000, OC.CoreClock,
                        nConf.GpuEntries[i].CoreClockBoostMax,
                        nConf.GpuEntries[i].CoreClockBoostMin,
                        nConf.GpuEntries[i].CoreClockBoostDef);
                    }
                    catch { }
                    try
                    {
                        nConf.GpuEntries[i].MemoryClockBoostCur = SetParam(i, 1000, OC.MemoryClock,
                        nConf.GpuEntries[i].MemoryClockBoostMax,
                        nConf.GpuEntries[i].MemoryClockBoostMin,
                        nConf.GpuEntries[i].MemoryClockBoostDef);
                    }
                    catch { }
                    
                    try
                    {
                        if (OC.FanSpeed != null)
                        {
                            if (OC.FanSpeed.Length > i)
                            {
                                nConf.GpuEntries[i].FanFlagsCur = MACM_SHARED_MEMORY_GPU_ENTRY_FAN_FLAG.None;
                                if (OC.FanSpeed[i] > nConf.GpuEntries[i].FanSpeedMax)
                                {
                                    nConf.GpuEntries[i].FanSpeedCur = nConf.GpuEntries[i].FanSpeedMax;
                                }
                                else if (OC.FanSpeed[i] < nConf.GpuEntries[i].FanSpeedMin)
                                {
                                    nConf.GpuEntries[i].FanSpeedCur = nConf.GpuEntries[i].FanSpeedMin;
                                }
                                else
                                {
                                    nConf.GpuEntries[i].FanSpeedCur = OC.FanSpeed[i];
                                }
                            }
                            else { nConf.GpuEntries[i].FanFlagsCur = MACM_SHARED_MEMORY_GPU_ENTRY_FAN_FLAG.AUTO; }
                        }
                        else { nConf.GpuEntries[i].FanFlagsCur = MACM_SHARED_MEMORY_GPU_ENTRY_FAN_FLAG.AUTO; }
                    }
                    catch { }
                }

                bool b = false;
                while (!b)
                {
                    try
                    {
                        CM = nConf;
                        CM.CommitChanges();
                        b = true;
                        MainWindow.SystemMessage("Параметры разгона установлены");
                    }
                    catch { }
                }
            });
        }
    }
}