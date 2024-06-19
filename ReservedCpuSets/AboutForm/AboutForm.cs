using System.Diagnostics;
using System.Windows.Forms;

namespace ReservedCpuSets {
    public partial class AboutForm : Form {
        public AboutForm() {
            InitializeComponent();

            versionText.Text = $"Version {Program.Version.Major}.{Program.Version.Minor}.{Program.Version.Build} 64-Bit";
        }

        private void GitHubLinkLinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            _ = Process.Start("https://github.com/valleyofdoom");
        }
    }
}
