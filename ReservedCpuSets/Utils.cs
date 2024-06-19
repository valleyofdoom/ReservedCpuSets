using Microsoft.Win32;

using System;

namespace ReservedCpuSets {
    internal class Utils {
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
    }
}
