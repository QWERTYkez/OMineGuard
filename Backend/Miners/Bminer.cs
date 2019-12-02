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
    class Bminer : Miner
    {
        private protected override string Directory { get; set; } = "Bminer";
        private protected override string ProcessName { get; set; } = "bminer";
        private protected override Process miner { get; set; }

        public override event Action<long> MinerStarted;
        public override event Action MinerStoped;
        public override event Action<string> LogDataReceived;

        private protected override void RunThisMiner(Config Config)
        {
            Profile prof = Settings.GetProfile();
            string DT = DateTime.Now.ToString("HH.mm.ss - dd.MM.yy");
            string logfile = $"MinersLogs/log {DT} {Config.Name}.txt";

            string algo = "";

            switch (Config.Algoritm)
            {
                case "Equihash 144.5":
                    algo = "equihash1445";
                    break;
                case "Equihash 150.5":
                    algo = "beam";
                    break;
                case "Equihash 200.9":
                    algo = "stratum";
                    break;
                case "Ethash":
                    algo = "ethash";
                    break;
                case "Tensority":
                    algo = "tensority";
                    break;
                case "CuckooCycle":
                    algo = "aeternity";
                    break;
                case "cuckARoo29":
                    algo = "cuckaroo29d";
                    break;
                case "cuckAToo31":
                    algo = "cuckatoo31";
                    break;
                case "Zhash":
                    algo = "zhash";
                    break;
            }
            string param = $"-uri {algo}://{Config.Wallet}.{prof.RigName}" +
                $"@{Config.Pool}:{Config.Port} {Config.Params} " +
                $"-logfile \"{logfile}\" -api 127.0.0.1:3333";
            if (algo == "equihash1445")
            {
                param += " -pers Auto";
            }
            if (prof.GPUsSwitch != null)
            {
                string di = "";
                int n = prof.GPUsSwitch.Count;
                for (int i = 0; i < n; i++)
                {
                    if (prof.GPUsSwitch[i])
                    {
                        di += "," + i;
                    }
                    di = di.TrimStart(',');
                }
                if (di != "")
                {
                    param += $" -devices {di}";
                }
            }

            miner = new Process();
            miner.StartInfo = new ProcessStartInfo($"{Directory}/{ProcessName}", param);
            miner.StartInfo.UseShellExecute = false;
            // set up output redirection
            miner.StartInfo.RedirectStandardOutput = true;
            miner.StartInfo.RedirectStandardError = true;
            miner.EnableRaisingEvents = true;
            miner.StartInfo.CreateNoWindow = true;
            // see below for output handler
            miner.ErrorDataReceived += (s, e) => { if (e.Data != "") Task.Run(() => LogDataReceived?.Invoke(e.Data)); };
            miner.OutputDataReceived += (s, e) => { if (e.Data != "") Task.Run(() => LogDataReceived?.Invoke(e.Data)); };

            miner.Start();
            miner.BeginErrorReadLine();
            miner.BeginOutputReadLine();
        }

        private protected override MinerInfo? CurrentMinerGetInfo()
        {
            try
            {
                WebRequest request = WebRequest.Create($"http://localhost:{port}/api/status");
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string content = reader.ReadToEnd();
                            for (int i = 0; i < 20; i++)
                            {
                                content = content.Replace("{\"" + i + "\":", "[");
                            }
                            content = content.Replace("}}}}", "}}}]");

                            BminerInfo INF = JsonConvert.DeserializeObject<BminerInfo>(content);

                            List<double> Hashrates = INF.miners.Select(m => m.solver.solution_rate).ToList();
                            List<int> Temperatures = INF.miners.Select(m => m.device.temperature).ToList();
                            List<int> Fanspeeds = INF.miners.Select(m => m.device.fan_speed).ToList();
                            List<int> ShAccepted = new List<int> { INF.stratum.accepted_shares };
                            List<int> ShRejected = new List<int> { INF.stratum.rejected_shares };

                            if (GPUs != null)
                            {
                                for (int i = 0; i < GPUs.Count; i++)
                                {
                                    if (!GPUs[i])
                                    {
                                        Hashrates.Insert(i, -1);
                                        Temperatures.Insert(i, -1);
                                        Fanspeeds.Insert(i, -1);
                                    }
                                }
                            }

                            return new MinerInfo(Hashrates, Temperatures, Fanspeeds, 
                                INF.stratum.accepted_shares, INF.stratum.rejected_shares, null);
                        }
                    }
                }
            }
            catch { return null; }
        }
        private class BminerInfo
        {
            public BminerStratum stratum { get; set; }
            public List<BminerMiner> miners { get; set; }
        }
        private class BminerStratum
        {
            public int accepted_shares { get; set; }
            public int rejected_shares { get; set; }
        }
        private class BminerMiner
        {
            public BminerSolver solver { get; set; }
            public BminerDevice device { get; set; }
        }
        private class BminerSolver
        {
            public double solution_rate { get; set; }
        }
        private class BminerDevice
        {
            public int temperature { get; set; }
            public int fan_speed { get; set; }
        }
    }
}