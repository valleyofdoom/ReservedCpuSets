using Microsoft.Win32;

using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace ReservedCpuSets {
    public partial class MainForm : Form {

        public MainForm() {
            InitializeComponent();
        }

        private bool IsAddedToStartup() {
            using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run")) {
                return key.GetValue("ReservedCpuSets") != null;
            }
        }

        private void AddToStartup(bool isEnabled) {
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

        private bool IsAllCPUsChecked() {
            for (var i = 0; i < cpuListBox.Items.Count; i++) {
                if (!cpuListBox.GetItemChecked(i)) {
                    return false;
                }
            }

            return true;
        }

        private void CheckAllCPUs(bool isChecked) {
            for (var i = 0; i < cpuListBox.Items.Count; i++) {
                cpuListBox.SetItemChecked(i, isChecked);
            }
        }

        private void MainFormLoad(object sender, EventArgs e) {
            for (var i = 0; i < Environment.ProcessorCount; i++) {
                _ = cpuListBox.Items.Add($"CPU {i}");
            }

            // load current configuration into the program

            var bitmask = Utils.GetReservedCpuSets();

            var lastBitIndex = bitmask.Length - 1;

            for (var i = 0; i < Environment.ProcessorCount; i++) {
                cpuListBox.SetItemChecked(i, bitmask[lastBitIndex - i] == '1');
            }

            if (Utils.GetWindowsBuildNumber() > 19044) {
                addToStartup.Enabled = false;
                addToStartup.ToolTipText = "Configuration does not need to be applied\non a per-boot basis on 21H2+";
            }

            // check if program is set to run at startup
            addToStartup.Checked = IsAddedToStartup();

            // 19044 is Windows 10 version 21H2
            if (!IsAddedToStartup() && Utils.GetWindowsBuildNumber() < 19044) {
                _ = MessageBox.Show("On 21H1 and below, the configuration must be applied on a per-boot basis.\nPlace the program somewhere safe and enable \"Add To Startup\" in the File menu", "ReservedCpuSets", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SaveButtonClick(object sender, EventArgs e) {
            if (IsAllCPUsChecked()) {
                _ = MessageBox.Show("At least one CPU must be unreserved", "ReservedCpuSets", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ulong affinity = 0;

            foreach (int i in cpuListBox.CheckedIndices) {
                affinity |= (ulong)1 << i;
            }

            if (affinity == 0) {
                // all CPUs unreserved correspond to the registry key being deleted
                using (var key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel", true)) {
                    try {
                        key.DeleteValue("ReservedCpuSets");
                    } catch (ArgumentException) {
                        // ignore error if the key does not exist
                    }
                }

                // since there are no changes to apply at startup, the program can be removed from starting at boot
                if (IsAddedToStartup()) {
                    AddToStartup(false);
                }
            } else {
                var bytes = BitConverter.GetBytes(affinity);
                var paddedBytes = new byte[8];
                Array.Copy(bytes, 0, paddedBytes, 0, bytes.Length);

                using (var key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel", true)) {
                    key.SetValue("ReservedCpuSets", paddedBytes, RegistryValueKind.Binary);
                }
            }

            Environment.Exit(0);
        }

        private void InvertSelectionClick(object sender, EventArgs e) {
            for (var i = 0; i < cpuListBox.Items.Count; i++) {
                cpuListBox.SetItemChecked(i, !cpuListBox.GetItemChecked(i));
            }
        }

        private void CheckAllClick(object sender, EventArgs e) {
            CheckAllCPUs(true);
        }

        private void UncheckAllClick(object sender, EventArgs e) {
            CheckAllCPUs(false);
        }

        private void ExitToolStripMenuItemClick(object sender, EventArgs e) {
            Application.Exit();
        }

        private void AboutToolStripMenuItemClick(object sender, EventArgs e) {
            var about = new AboutForm {
                StartPosition = FormStartPosition.Manual,
                Location = Location
            };
            _ = about.ShowDialog();
        }

        private void AddToStartupClick(object sender, EventArgs e) {
            AddToStartup(addToStartup.Checked);
        }

        private void DonateToolStripMenuItem_Click(object sender, EventArgs e) {

        }
    }
}
