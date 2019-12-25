using Newtonsoft.Json;
using OMineGuard.Backend;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace OMineGuard.Miners
{
    class Gminer : Miner
    {
        private protected override string Directory { get; } = "Gminer";
        private protected override string ProcessName { get => CurrentProcessName; }
        public static string CurrentProcessName { get; } = "miner";

        private protected override void RunThisMiner(Config Config)
        {
            string DT = DateTime.Now.ToString("HH.mm.ss - dd.MM.yy");
            string logfile = $"MinersLogs/log {DT} {Config.Name}.txt";

            string algo = "";
            switch (Config.Algoritm)
            {
                case "BeamHash II":
                    algo = "beamhash";
                    break;
                case "Equihash 96.5":
                    algo = "equihash96_5";
                    break;
                case "Equihash 144.5":
                    algo = "equihash144_5";
                    break;
                case "Equihash 150.5":
                    algo = "equihash150_5";
                    break;
                case "Equihash 192.7":
                    algo = "equihash192_7";
                    break;
                case "Equihash 210.9":
                    algo = "equihash210_9";
                    break;
                case "cuckARoo29":
                    algo = "cuckaroo29";
                    break;
                case "cuckAToo31":
                    algo = "cuckatoo31";
                    break;
                case "CuckooCycle":
                    algo = "cuckoo";
                    break;
            }
            string param = $"--algo {algo} --server {Config.Pool} --port {Config.Port} " +
                        $"--api {port} --user {Config.Wallet}.{Settings.Profile.RigName} " +
                        $"{Config.Params} --logfile \"{logfile}\"";
            if (Settings.Profile.GPUsSwitch != null)
            {
                string di = "";
                int n = Settings.Profile.GPUsSwitch.Count;
                for (int i = 0; i < n; i++)
                {
                    if (Settings.Profile.GPUsSwitch[i])
                    {
                        di += " " + i;
                    }
                    di = di.TrimStart(' ');
                }
                if (di != "")
                {
                    param += $" --devices {di}";
                }
            }

            process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo($"{Directory}/{ProcessName}", param)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.ErrorDataReceived += (s, e) => { if (e.Data != "") Logging(e.Data); };
            process.OutputDataReceived += (s, e) => { if (e.Data != "") Logging(e.Data); };

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
        }

        private protected override MinerInfo CurrentMinerGetInfo()
        {
            try
            {
                WebRequest request = WebRequest.Create($"http://localhost:{port}/stat");
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            GminerDevice[] GDs = JsonConvert.DeserializeObject<GminerInfo>(reader.ReadToEnd()).
                                devices.OrderBy(GD => GD.gpu_id).ToArray();

                            List<double?> Hashrates = GDs.Select(GD => GD.speed).ToList();
                            List<int?> Temperatures = GDs.Select(GD => GD.temperature).ToList();
                            List<int?> ShAccepted = GDs.Select(GD => GD.accepted_shares).ToList();
                            List<int?> ShRejected = GDs.Select(GD => GD.rejected_shares).ToList();

                            if (GPUs != null)
                            {
                                for (int i = 0; i < GPUs.Count; i++)
                                {
                                    if (!GPUs[i])
                                    {
                                        Hashrates.Insert(i, null);
                                        Temperatures.Insert(i, null);
                                        ShAccepted.Insert(i, null);
                                        ShRejected.Insert(i, null);
                                    }
                                }
                            }

                            return new MinerInfo(Hashrates, Temperatures, null, ShAccepted, ShRejected, null);
                        }
                    }
                }
            }
            catch { return new MinerInfo(); }
        }
        private class GminerInfo
        {
#pragma warning disable IDE1006 // Стили именования
            public GminerDevice[] devices { get; set; }
#pragma warning restore IDE1006 // Стили именования
        }
        private class GminerDevice
        {
#pragma warning disable IDE1006 // Стили именования
            public int gpu_id { get; set; }
            public double? speed { get; set; }
            public int? accepted_shares { get; set; }
            public int? rejected_shares { get; set; }
            public int? temperature { get; set; }
#pragma warning restore IDE1006 // Стили именования
        }
    }
}