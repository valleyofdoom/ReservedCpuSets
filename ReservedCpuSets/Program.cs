using CommandLine;

using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace ReservedCpuSets {
    internal static class Program {
        public static Version Version = Assembly.GetExecutingAssembly().GetName().Version;

        [STAThread]
        private static void Main() {
            // 10240 is Windows 10 version 1507
            if (Utils.GetWindowsBuildNumber() < 10240) {
                _ = MessageBox.Show("ReservedCpuSets supports Windows 10 and above only", "ReservedCpuSets", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }

            _ = Parser.Default.ParseArguments<Options>(Environment.GetCommandLineArgs()).WithParsed(o => {
                if (o.LoadCpuSets) {
                    Thread.Sleep(o.Timeout * 1000);
                    Environment.Exit(Utils.LoadCpuSet());
                }
            });


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
