using InSimDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LFSAutoPilot2
{
    public class Paths
    {
        public List<LFSPath> paths;

        public Paths()
        {
            paths = new List<LFSPath>();
            var pfiles = Directory.GetFiles(@"D:\LFS\paths", "*.txt");
            foreach(var pf in pfiles)
            {
                var vecs = new List<Vector>();
                var lines = File.ReadAllLines(pf);
                foreach(var l in lines)
                {
                    var spl = l.Split(' ');
                    if(spl.Length == 3)
                    {
                        vecs.Add(new Vector(float.Parse(spl[0]), float.Parse(spl[1]), float.Parse(spl[2])));
                    }
                }
                var fn = Path.GetFileNameWithoutExtension(pf);
                paths.Add(new LFSPath() { Name = fn, Waypoints = vecs });
            }
        }
    }

    public struct LFSPath
    {
        public string Name { get; set; }
        public List<Vector> Waypoints{ get; set; }
    }
}
