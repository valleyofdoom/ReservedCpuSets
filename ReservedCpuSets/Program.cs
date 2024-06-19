using CommandLine;

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ReservedCpuSets {
    internal static class Program {
        public static Version Version = Assembly.GetExecutingAssembly().GetName().Version;

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string lpLibFileName);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hLibModule);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        private delegate int SetSystemCpuSetDelegate(ulong mask);

        private static int LoadCpuSet() {
            var bitmask = Utils.GetReservedCpuSets();

            // bitmask must be inverted because the logic is flipped
            var invertedSystemBitmask = "";

            for (var i = 0; i < bitmask.Length; i++) {
                invertedSystemBitmask += bitmask[i] == '0' ? 1 : 0;
            }

            var systemAffinity = Convert.ToUInt64(invertedSystemBitmask, 2);

            var moduleHandle = LoadLibrary("ReservedCpuSets.dll");

            if (moduleHandle == IntPtr.Zero) {
                _ = MessageBox.Show("Failed to apply changes. Could not load ReservedCpuSets.dll", "ReservedCpuSets", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }

            var funcPtr = GetProcAddress(moduleHandle, "SetSystemCpuSet");

            if (funcPtr == IntPtr.Zero) {
                _ = MessageBox.Show("Failed to apply changes. GetProcAddress Failed", "ReservedCpuSets", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }

#pragma warning disable IDE1006 // Naming Styles
            var SetSystemCpuSet = Marshal.GetDelegateForFunctionPointer<SetSystemCpuSetDelegate>(funcPtr);
#pragma warning restore IDE1006 // Naming Styles

            // all CPUs = 0 rather than all bits set to 1
            if (SetSystemCpuSet(Convert.ToUInt64(bitmask) == 0 ? 0 : systemAffinity) != 0) {
                _ = MessageBox.Show("Failed to apply changes. Could not apply system-wide CPU set", "ReservedCpuSets", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }

            _ = FreeLibrary(moduleHandle);

            return 0;
        }

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
                    Environment.Exit(LoadCpuSet());
                }
            });


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
