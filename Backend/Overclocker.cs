using MSI.Afterburner;
using OMineGuardControlLibrary;
using OpenHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace OMineGuard.Backend
{
    public static class Overclocker
    {
        public static event Action<DefClock> ConnectedToMSI;
        public static event Action<MSIinfo?, OHMinfo?> OverclockReceived;
        public static event Action OverclockApplied;

        public static bool MSIconnected { get; private set; } = false;
        public static bool OHMenable { get; private set; } = false;
        public static void ApplyOverclock(IOverclock OC)
        {
            Task.Run(() => 
            {
                while (!MSIconnected) Thread.Sleep(100);

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

                    if (nConf.GpuEntries[i].CoreClockBoostMax - nConf.GpuEntries[i].CoreClockBoostMin != 0)
                    {
                        try
                        {
                            nConf.GpuEntries[i].CoreClockBoostCur = SetParam(i, 1000, OC.CoreClock,
                            nConf.GpuEntries[i].CoreClockBoostMax,
                            nConf.GpuEntries[i].CoreClockBoostMin,
                            nConf.GpuEntries[i].CoreClockBoostDef);
                        }
                        catch { }
                    }
                    else if (nConf.GpuEntries[i].CoreClockMax - nConf.GpuEntries[i].CoreClockMin != 0)
                    {
                        try
                        {
                            nConf.GpuEntries[i].CoreClockCur = SetParam(i, 1000, OC.CoreClock,
                            nConf.GpuEntries[i].CoreClockMax,
                            nConf.GpuEntries[i].CoreClockMin,
                            nConf.GpuEntries[i].CoreClockDef);
                        }
                        catch { }
                    }

                    if (nConf.GpuEntries[i].MemoryClockBoostMax - nConf.GpuEntries[i].MemoryClockBoostMin != 0)
                    {
                        try
                        {
                            nConf.GpuEntries[i].MemoryClockBoostCur = SetParam(i, 1000, OC.MemoryClock,
                            nConf.GpuEntries[i].MemoryClockBoostMax,
                            nConf.GpuEntries[i].MemoryClockBoostMin,
                            nConf.GpuEntries[i].MemoryClockBoostDef);
                        }
                        catch { }
                    }
                    else if (nConf.GpuEntries[i].MemoryClockMax - nConf.GpuEntries[i].MemoryClockMin != 0)
                    {
                        try
                        {
                            nConf.GpuEntries[i].MemoryClockCur = SetParam(i, 1000, OC.MemoryClock,
                            nConf.GpuEntries[i].MemoryClockMax,
                            nConf.GpuEntries[i].MemoryClockMin,
                            nConf.GpuEntries[i].MemoryClockDef);
                        }
                        catch { }
                    }

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
                                    nConf.GpuEntries[i].FanSpeedCur = Convert.ToUInt32(OC.FanSpeed[i]);
                                }
                            }
                            else { nConf.GpuEntries[i].FanFlagsCur = MACM_SHARED_MEMORY_GPU_ENTRY_FAN_FLAG.AUTO; }
                        }
                        else { nConf.GpuEntries[i].FanFlagsCur = MACM_SHARED_MEMORY_GPU_ENTRY_FAN_FLAG.AUTO; }
                    }
                    catch { }
                }

                while (true)
                {
                    try
                    {
                        CM = nConf;
                        CM.CommitChanges();
                        Task.Run(() => OverclockApplied?.Invoke());
                        return;
                    }
                    catch { }
                    Thread.Sleep(50);
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
        private static uint SetParam(int i, int multiply, int[] param, uint max, uint min, uint def)
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
                        return Convert.ToUInt32(param[i] * multiply);
                    }
                }
                else { return def; }
            }
            else { return def; }
        }

        public static void _Overclocker()
        {
            Task.Run(() => 
            {
                MSIconnecting();
                OHMconnecting();
                StartMonitoring();
            });
        }
        public static bool ApplicationLive = true;
        private static readonly List<OHMgpu> OHMgpus = new List<OHMgpu>();
        private static ControlMemory CM;
        private static int GPUsCount;
        private static void MSIconnecting()
        {
            Task.Run(() =>
            {
                while (Process.GetProcessesByName("MSIAfterburner").Length == 0) Thread.Sleep(100);
                while (!MSIconnected)
                {
                    try
                    {
                        CM = new ControlMemory();
                        MSIconnected = true;
                    }
                    catch { }
                }

                GPUsCount = CM.GpuEntries.Length;
                while (CM.GpuEntries[GPUsCount - 1].PowerLimitMax - 
                       CM.GpuEntries[GPUsCount - 1].PowerLimitMin == 0)
                { GPUsCount = CM.GpuEntries.Length - 1; }

                List<int> pl = new List<int>();
                List<int> cc = new List<int>();
                List<int> mc = new List<int>();
                List<int> fs = new List<int>();
                for (int i = 0; i < GPUsCount; i++)
                {
                    int core = 0;
                    if (CM.GpuEntries[i].CoreClockBoostMax - CM.GpuEntries[i].CoreClockBoostMin != 0)
                    { core = CM.GpuEntries[i].CoreClockBoostDef / 1000; }
                    else if (CM.GpuEntries[i].CoreClockMax - CM.GpuEntries[i].CoreClockMin != 0)
                    { core = Convert.ToInt32(CM.GpuEntries[i].CoreClockDef) / 1000; }
                    int memory = 0;
                    if (CM.GpuEntries[i].MemoryClockBoostMax - CM.GpuEntries[i].MemoryClockBoostMin != 0)
                    { memory = CM.GpuEntries[i].MemoryClockBoostDef / 1000; }
                    else if (CM.GpuEntries[i].MemoryClockMax - CM.GpuEntries[i].MemoryClockMin != 0)
                    { memory = Convert.ToInt32(CM.GpuEntries[i].MemoryClockDef) / 1000; }

                    pl.Add(CM.GpuEntries[i].PowerLimitDef);
                    cc.Add(core);
                    mc.Add(memory);
                    fs.Add(Convert.ToInt32(CM.GpuEntries[i].FanSpeedDef));
                }
                Task.Run(() => ConnectedToMSI?.Invoke(new DefClock
                {
                    PowerLimits = pl.ToArray(),
                    CoreClocks = cc.ToArray(),
                    MemoryClocks = mc.ToArray(),
                    FanSpeeds = fs.ToArray()
                }));
            });
        }
        private static void OHMconnecting()
        {
            Task.Run(() => 
            {
                Computer c = new Computer { GPUEnabled = true };
                c.Open();
                foreach (IHardware h in c.Hardware)
                {
                    OHMgpus.Add(new OHMgpu(h));
                }
                if (c.Hardware.Length > 0)
                    OHMenable = true;
            });
        }
        private static void StartMonitoring()
        {
            Task.Run(() =>
            {
                MSIinfo? MSI;
                List<int?> mpl;
                List<int?> mcc;
                List<int?> mmc;
                List<int?> mfs;
                OHMinfo? OHM;
                List<int?> ots;
                List<int?> ofs;
                List<int?> occ;
                List<int?> omc;
                while (ApplicationLive)
                {
                    Task.Run(() => 
                    {
                        // MSIinfo
                        if (MSIconnected)
                        {
                            try
                            {
                                mpl = new List<int?>();
                                mcc = new List<int?>();
                                mmc = new List<int?>();
                                mfs = new List<int?>();
                                CM.ReloadAll();
                                for (int i = 0; i < GPUsCount; i++)
                                {
                                    int core = 0;
                                    if (CM.GpuEntries[i].CoreClockBoostMax - CM.GpuEntries[i].CoreClockBoostMin != 0)
                                    { core = CM.GpuEntries[i].CoreClockBoostCur / 1000; }
                                    else if (CM.GpuEntries[i].CoreClockMax - CM.GpuEntries[i].CoreClockMin != 0)
                                    { core = Convert.ToInt32(CM.GpuEntries[i].CoreClockCur) / 1000; }
                                    int memory = 0;
                                    if (CM.GpuEntries[i].MemoryClockBoostMax - CM.GpuEntries[i].MemoryClockBoostMin != 0)
                                    { memory = CM.GpuEntries[i].MemoryClockBoostCur / 1000; }
                                    else if (CM.GpuEntries[i].MemoryClockMax - CM.GpuEntries[i].MemoryClockMin != 0)
                                    { memory = Convert.ToInt32(CM.GpuEntries[i].MemoryClockCur) / 1000; }

                                    mpl.Add(CM.GpuEntries[i].PowerLimitCur);
                                    mcc.Add(core);
                                    mmc.Add(memory);
                                    mfs.Add(Convert.ToInt32(CM.GpuEntries[i].FanSpeedCur));
                                }
                                MSI = new MSIinfo
                                {
                                    PowerLimits = mpl.ToArray(),
                                    CoreClocks = mcc.ToArray(),
                                    MemoryClocks = mmc.ToArray(),
                                    FanSpeeds = mfs.ToArray()
                                };
                            } catch { MSI = new MSIinfo(); }
                        }
                        else { MSI = null; }
                        //OHMinfo
                        if (OHMenable)
                        {
                            ots = new List<int?>();
                            ofs = new List<int?>();
                            occ = new List<int?>();
                            omc = new List<int?>();
                            foreach (OHMgpu ohm in OHMgpus)
                            {
                                ohm.Update();
                                ots.Add(ohm.Temperature);
                                ofs.Add(ohm.FanSpeed);
                                occ.Add(ohm.CoreClock);
                                omc.Add(ohm.MemoryClock);
                            }
                            OHM = new OHMinfo 
                            {
                                Temperatures = ots.ToArray(),
                                FanSpeeds = ofs.ToArray(),
                                CoreClocks = occ.ToArray(),
                                MemoryClocks = omc.ToArray()
                            };
                        } else { OHM = null; }
                        //event
                        Task.Run(() => OverclockReceived?.Invoke(MSI, OHM));
                    });
                    Thread.Sleep(1000);
                }
            });
        }
        
        private class OHMgpu
        {
            public OHMgpu(IHardware gpu)
            {
                this.gpu = gpu;
                foreach (ISensor s in gpu.Sensors)
                {
                    if (s.SensorType == SensorType.Temperature)
                        TempSensor = s;
                    if (s.Name == "GPU Fan" && s.SensorType == SensorType.Control)
                        FanSpeedSensor = s;
                    if (s.Name == "GPU Core" && s.SensorType == SensorType.Clock)
                        CoreClockSensor = s;
                    if (s.Name == "GPU Memory" && s.SensorType == SensorType.Clock)
                        MemoryClockSensor = s;
                }
            }

            private readonly IHardware gpu;
            private readonly ISensor TempSensor;
            private readonly ISensor CoreClockSensor;
            private readonly ISensor MemoryClockSensor;
            private readonly ISensor FanSpeedSensor;
            public void Update()
            {
                gpu.Update();
            }

            public int? Temperature
            {
                get
                {
                    if (TempSensor.Value != null)
                    {
                        return Convert.ToInt32(TempSensor.Value.GetValueOrDefault());
                    }
                    else { return null; }
                }
            }
            public int? FanSpeed
            {
                get
                {
                    if (FanSpeedSensor.Value != null)
                    {
                        return Convert.ToInt32(FanSpeedSensor.Value.GetValueOrDefault());
                    }
                    else { return null; }
                }
            }
            public int? CoreClock
            {
                get
                {
                    if (CoreClockSensor.Value != null)
                    {
                        return Convert.ToInt32(CoreClockSensor.Value.GetValueOrDefault());
                    }
                    else { return null; }
                }
            }
            public int? MemoryClock
            {
                get
                {
                    if (MemoryClockSensor.Value != null)
                    {
                        return Convert.ToInt32(MemoryClockSensor.Value.GetValueOrDefault());
                    }
                    else { return null; }
                }
            }
        }
    }

    public struct MSIinfo
    {
        public int?[] PowerLimits;
        public int?[] CoreClocks;
        public int?[] MemoryClocks;
        public int?[] FanSpeeds;
    }
    public struct OHMinfo
    {
        public int?[] Temperatures;
        public int?[] FanSpeeds;
        public int?[] CoreClocks;
        public int?[] MemoryClocks;
    }
    public class DefClock : IDefClock
    {
        public int[] PowerLimits { get; set; }
        public int[] CoreClocks { get; set; }
        public int[] MemoryClocks { get; set; }
        public int[] FanSpeeds { get; set; }
    }
}
