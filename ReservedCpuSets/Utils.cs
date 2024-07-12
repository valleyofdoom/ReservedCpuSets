using Microsoft.Win32;

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ReservedCpuSets {
    internal class Utils {
        private delegate int SetSystemCpuSetDelegate(ulong mask);

        public static int GetWindowsBuildNumber() {
            using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion")) {
                return int.Parse(key.GetValue("CurrentBuildNumber") as string);
            }
        }

        public static string GetReservedCpuSets() {
            string bitmask;

            using (var key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel")) {
                try {
                    var currentConfig = key.GetValue("ReservedCpuSets") as byte[];
                    Array.Reverse(currentConfig); // big to little endian
                    var hexString = BitConverter.ToString(currentConfig).Replace("-", "");
                    // convert to binary
                    bitmask = Convert.ToString(Convert.ToInt64(hexString, 16), 2);
                } catch (ArgumentException) {
                    bitmask = "";
                }
            }
            // sterilize string and ensure it is length of core count
            return bitmask.TrimStart('0').PadLeft(Environment.ProcessorCount, '0');
        }

        public static int LoadCpuSet() {
            var bitmask = GetReservedCpuSets();

            // bitmask must be inverted because the logic is flipped
            var invertedSystemBitmask = "";

            for (var i = 0; i < bitmask.Length; i++) {
                invertedSystemBitmask += bitmask[i] == '0' ? 1 : 0;
            }

            var systemAffinity = Convert.ToUInt64(invertedSystemBitmask, 2);

            var moduleHandle = NativeMethods.LoadLibrary("ReservedCpuSets.dll");

            if (moduleHandle == IntPtr.Zero) {
                _ = MessageBox.Show("Failed to apply changes. Could not load ReservedCpuSets.dll", "ReservedCpuSets", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }

            var funcPtr = NativeMethods.GetProcAddress(moduleHandle, "SetSystemCpuSet");

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

            _ = NativeMethods.FreeLibrary(moduleHandle);

            return 0;
        }

        public static bool IsAddedToStartup() {
            using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run")) {
                return key.GetValue("ReservedCpuSets") != null;
            }
        }

        public static void AddToStartup(bool isEnabled) {
            var entryAssembly = Assembly.GetEntryAssembly();

            using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run", true)) {
                if (isEnabled) {
                    key.SetValue("ReservedCpuSets", $"\"{entryAssembly.Location}\" --load-cpusets --timeout 10");
                } else {
                    try {
                        key.DeleteValue("ReservedCpuSets");
                    } catch (ArgumentException) {
                        // ignore error if the key does not exist
                    }
                }
            }
        }
    }
}
