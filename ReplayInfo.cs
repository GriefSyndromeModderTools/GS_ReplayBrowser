using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GS_ReplayBrowser
{
    class ReplayInfo
    {
        public string FilePath;
        public string FileName;
        public int FileSize_KB;

        public TimeSpan Time;
        public int Messages;

        public int Players;
        public int Lap;
        public string Actors;

        public ReplayInfo(string fn)
        {
            var rep = new ReplayFile(fn);

            FilePath = fn;
            FileName = Path.GetFileName(fn);

            FileSize_KB = rep.FileSize / 1024;
            Time = new TimeSpan((long)(rep.InputData.Length / 3 / 60.0f * TimeSpan.TicksPerSecond));
            Messages = rep.ChatMessages.Length;

            ushort p0 = 0, p1 = 0, p2 = 0;
            for (int i = 0; i < rep.InputData.Length; i += 3)
            {
                p0 |= rep.InputData[i];
                p1 |= rep.InputData[i + 1];
                p2 |= rep.InputData[i + 2];
            }
            Players = (p0 == 0 ? 0 : 1) + (p1 == 0 ? 0 : 1) + (p2 == 0 ? 0 : 1);

            var s = new ReplaySimulator(rep);
            try
            {
                s.Simulate();
                Lap = s.SelectedLap;
                Actors = s.SelectedActorName;
            }
            catch
            {
                Lap = 0;
                Actors = "-";
            }
        }

        public string Description
        {
            get
            {
                if (Lap == 0) return "-";
                return Actors + Lap + "周目";
            }
        }
    }
}
