using CommandLine;

namespace ReservedCpuSets {
    public class Options {
        [Option("timeout")]
        public int Timeout { get; set; }

        [Option("load-cpusets")]
        public bool LoadCpuSets { get; set; }
    }
}
