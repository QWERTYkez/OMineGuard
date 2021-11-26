using MSI.Afterburner;
using OMineGuardControlLibrary;
using OpenHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        public static string MSIpath { get; private set; } = "";
        public static bool OHMenable { get; private set; } = false;
        private static MSIinfo? MSICurrent;
        public static void ApplyOverclock(IOverclock OC)
        {
            Task.Run(() =>
            {
                while (!MSIconnected) Thread.Sleep(100);

                Process.Start(MSIpath);

            tryApplyClock:

                CM = new ControlMemory();
                ControlMemory nConf;
                List<ControlMemoryGpuEntry> gpuEnries = new List<ControlMemoryGpuEntry>();

                do
                {
                    Thread.Sleep(50);
                    CM.ReloadAll();
                    nConf = CM;
                    if (nConf.GpuEntries.Length > 0)
                    {
                        var vals = ValidGPUS.ToArray();
                        for (int i = 0; i < vals.Length; i++)
                        {
                            if (vals[i])
                                gpuEnries.Add(nConf.GpuEntries[i]);
                        }
                    }
                }
                while (gpuEnries[0].PowerLimitMax == 0);

                for (int i = 0; i < gpuEnries.Count; i++)
                {
                    try
                    {
                        gpuEnries[i].PowerLimitCur = SetParam(i, 1, OC.PowLim,
                        gpuEnries[i].PowerLimitMax,
                        gpuEnries[i].PowerLimitMin,
                        gpuEnries[i].PowerLimitDef);
                    }
                    catch { }

                    if (gpuEnries[i].CoreClockBoostMax - gpuEnries[i].CoreClockBoostMin != 0)
                    {
                        try
                        {
                            gpuEnries[i].CoreClockBoostCur = SetParam(i, 1000, OC.CoreClock,
                            gpuEnries[i].CoreClockBoostMax,
                            gpuEnries[i].CoreClockBoostMin,
                            gpuEnries[i].CoreClockBoostDef);
                        }
                        catch { }
                    }
                    else if (gpuEnries[i].CoreClockMax - gpuEnries[i].CoreClockMin != 0)
                    {
                        try
                        {
                            gpuEnries[i].CoreClockCur = SetParam(i, 1000, OC.CoreClock,
                            gpuEnries[i].CoreClockMax,
                            gpuEnries[i].CoreClockMin,
                            gpuEnries[i].CoreClockDef);
                        }
                        catch { }
                    }

                    if (gpuEnries[i].MemoryClockBoostMax - gpuEnries[i].MemoryClockBoostMin != 0)
                    {
                        try
                        {
                            gpuEnries[i].MemoryClockBoostCur = SetParam(i, 1000, OC.MemoryClock,
                            gpuEnries[i].MemoryClockBoostMax,
                            gpuEnries[i].MemoryClockBoostMin,
                            gpuEnries[i].MemoryClockBoostDef);
                        }
                        catch { }
                    }
                    else if (gpuEnries[i].MemoryClockMax - gpuEnries[i].MemoryClockMin != 0)
                    {
                        try
                        {
                            gpuEnries[i].MemoryClockCur = SetParam(i, 1000, OC.MemoryClock,
                            gpuEnries[i].MemoryClockMax,
                            gpuEnries[i].MemoryClockMin,
                            gpuEnries[i].MemoryClockDef);
                        }
                        catch { }
                    }

                    try
                    {
                        if (OC.FanSpeed != null)
                        {
                            if (OC.FanSpeed.Length > i)
                            {
                                gpuEnries[i].FanFlagsCur = MACM_SHARED_MEMORY_GPU_ENTRY_FAN_FLAG.None;
                                if (OC.FanSpeed[i] > gpuEnries[i].FanSpeedMax)
                                {
                                    gpuEnries[i].FanSpeedCur = gpuEnries[i].FanSpeedMax;
                                }
                                else if (OC.FanSpeed[i] < gpuEnries[i].FanSpeedMin)
                                {
                                    gpuEnries[i].FanSpeedCur = gpuEnries[i].FanSpeedMin;
                                }
                                else
                                {
                                    gpuEnries[i].FanSpeedCur = Convert.ToUInt32(OC.FanSpeed[i]);
                                }
                            }
                            else { gpuEnries[i].FanFlagsCur = MACM_SHARED_MEMORY_GPU_ENTRY_FAN_FLAG.AUTO; }
                        }
                        else { gpuEnries[i].FanFlagsCur = MACM_SHARED_MEMORY_GPU_ENTRY_FAN_FLAG.AUTO; }
                    }
                    catch { }
                }

                while (true)
                {
                    try
                    {
                        CM = nConf;
                        CM.CommitChanges();

                        Thread.Sleep(3000);

                        if (MSICurrent != null)
                        {
                            var msi = MSICurrent.Value;

                            for (int i = 0; i < OC.PowLim.Length; i++)
                            {
                                if (OC.PowLim != null)
                                    if (msi.PowerLimits[i] != OC.PowLim[i]) goto tryApplyClock;

                                if (OC.CoreClock != null)
                                    if (msi.CoreClocks[i] != OC.CoreClock[i]) goto tryApplyClock;

                                if (OC.MemoryClock != null)
                                    if (msi.MemoryClocks[i] != OC.MemoryClock[i]) goto tryApplyClock;
                            }
                        }
                        else goto tryApplyClock;


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

        public static void _Overclocker(MainModel mm)
        {
            Task.Run(async () =>
            {
                await MSIconnecting(mm);
                OHMconnecting();
                StartMonitoring();
            });
        }
        public static bool ApplicationLive = true;
        private static readonly List<OHMgpu> OHMgpus = new List<OHMgpu>();
        private static ControlMemory CM;
        private static IEnumerable<bool> ValidGPUS;

        private static Task MSIconnecting(MainModel mm)
        {
            return Task.Run(() =>
            {
                while (Process.GetProcessesByName("MSIAfterburner").Length == 0) Thread.Sleep(100);
                try
                {
                    MSIpath = Process.GetProcessesByName("MSIAfterburner").First().MainModule.FileName;
                }
                catch { mm.Logging("Добавьте права администратора", true); }

                IEnumerable<ControlMemoryGpuEntry> GEs = new List<ControlMemoryGpuEntry>();
                while (GEs.Count() == 0)
                {
                    try
                    {
                        CM = new ControlMemory();
                        CM.ReloadAll();
                        GEs = CM.GpuEntries.Where(e => e.PowerLimitMax - e.PowerLimitMin != 0);
                        ValidGPUS = CM.GpuEntries.Select(e => e.PowerLimitMax - e.PowerLimitMin != 0);
                        Thread.Sleep(1000);
                    }
                    catch { Thread.Sleep(1000); }
                }

                MSIconnected = true;

                Task.Run(() => ConnectedToMSI?.Invoke(new DefClock
                {
                    PowerLimits = GEs.Select(e => e.PowerLimitDef).ToArray(),
                    CoreClocks = GEs.Select(e => (e.CoreClockBoostMin != 0 ? e.CoreClockBoostDef :
                        (e.CoreClockMin != 0 ? Convert.ToInt32(e.CoreClockDef) : 0)) / 1000).ToArray(),
                    MemoryClocks = GEs.Select(e => (e.MemoryClockBoostMin != 0 ? e.MemoryClockBoostDef :
                        (e.MemoryClockMin != 0 ? Convert.ToInt32(e.MemoryClockDef) : 0)) / 1000).ToArray(),
                    FanSpeeds = GEs.Select(e => Convert.ToInt32(e.FanSpeedDef)).ToArray()
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
                    if (h.Sensors.Length > 1)
                        OHMgpus.Add(new OHMgpu(h));
                }
                if (OHMgpus.Count > 0)
                    OHMenable = true;
            });
        }

        private static IEnumerable<int?> OHMtemp = OHMgpus.Select(e => e.Temperature);
        private static IEnumerable<int?> OHMfs = OHMgpus.Select(e => e.FanSpeed);
        private static IEnumerable<int?> OHMcc = OHMgpus.Select(e => e.CoreClock);
        private static IEnumerable<int?> OHMmc = OHMgpus.Select(e => e.MemoryClock);

        private static void StartMonitoring()
        {
            Task.Run(() =>
            {
                MSIinfo? MSI; OHMinfo? OHM;
                IEnumerable<ControlMemoryGpuEntry> GEs;
                while (ApplicationLive)
                {
                    Task.Run(() =>
                    {
                        // MSIinfo
                        if (MSIconnected)
                        {
                            try
                            {
                                CM.ReloadAll();
                                GEs = CM.GpuEntries.Where(e => e.PowerLimitMax - e.PowerLimitMin != 0);
                                MSI = new MSIinfo
                                {
                                    PowerLimits = GEs.Select(e => e.PowerLimitCur).ToArray(),
                                    CoreClocks = GEs.Select(e => (e.CoreClockBoostMin != 0 ? e.CoreClockBoostCur :
                                        (e.CoreClockMin != 0 ? Convert.ToInt32(e.CoreClockCur) : 0)) / 1000).ToArray(),
                                    MemoryClocks = GEs.Select(e => (e.MemoryClockBoostMin != 0 ? e.MemoryClockBoostCur :
                                        (e.MemoryClockMin != 0 ? Convert.ToInt32(e.MemoryClockCur) : 0)) / 1000).ToArray(),
                                    FanSpeeds = GEs.Select(e => new int?(Convert.ToInt32(e.FanSpeedCur))).ToArray()
                                };
                            }
                            catch { MSI = new MSIinfo(); }
                        }
                        else { MSI = null; }
                        MSICurrent = MSI;
                        //OHMinfo
                        if (OHMenable)
                        {
                            foreach (OHMgpu ohm in OHMgpus) ohm.Update();
                            OHM = new OHMinfo
                            {
                                Temperatures = OHMtemp.ToArray(),
                                FanSpeeds = OHMfs.ToArray(),
                                CoreClocks = OHMcc.ToArray(),
                                MemoryClocks = OHMmc.ToArray()
                            };
                        }
                        else { OHM = null; }
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
            public void Update() => gpu.Update();

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
        public int[] PowerLimits;
        public int[] CoreClocks;
        public int[] MemoryClocks;
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