using OMineGuard.Managers;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OMineGuard.Miners
{
    class Gminer : Miner
    {
        private protected override string Directory { get; set; } = "Gminer";
        private protected override string ProcessName { get; set; } = "miner";
        private protected override Process miner { get; set; }
        private protected override void RunThisMiner(Config Config)
        {
            Profile prof = Settings.GetProfile();
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
                        $"--api 3333 --user {Config.Wallet}.{prof.RigName} " +
                        $"{Config.Params} --logfile \"{logfile}\"";
            if (prof.GPUsSwitch != null)
            {
                string di = "";
                int n = prof.GPUsSwitch.Count;
                for (int i = 0; i < n; i++)
                {
                    if (prof.GPUsSwitch[i])
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
    }
}
