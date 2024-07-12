using System;
using System.Runtime.InteropServices;

namespace ReservedCpuSets {
    internal class NativeMethods {
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string lpLibFileName);

        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hLibModule);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
    }
}
