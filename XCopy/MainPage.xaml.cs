using Windows.UI.Xaml.Controls;
using System.Diagnostics;
using System.IO;
using Windows.UI.ViewManagement;

namespace recursive_copy
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            RecursiveCopy();
        }

        private void RecursiveCopy()
        {
            var xbox = new comclassactivation.Xbox();

            var tempDirectory = "D:\\Temp";
            var successFilePath = "D:\\success.txt";

            var ssh2File = "D:\\SSH2.txt";
            var ssh2Folder = "D:\\DevelopmentFiles\\SSH2";
            var ssh2Success = "D:\\SSH2Success.txt";

            var sshSrcPath = "D:\\DevelopmentFiles\\SSH";
            var sshDestPath = "T:\\ProgramData\\ssh";
            var successSSHPath = "D:\\sshReady.txt";

            // Step 5
            // First Launch
            if (!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
                using (File.Create(Path.Combine(tempDirectory, "test.txt"))) { }
                xbox.RecursiveCopyDirectory(tempDirectory, "S:\\Windows\\System32\\config\\systemprofile\\.ssh");
                File.WriteAllText(successFilePath, "Directory creation successful");
            }

            // Step 6
            // Second Launch
            if (File.Exists(ssh2File))
            {
                xbox.RecursiveCopyDirectory(ssh2Folder, "S:\\Windows\\System32\\config\\systemprofile\\.ssh");
                File.WriteAllText(ssh2Success, "Directory copy successful");
            }

            // Step 7
            // Third Launch
            if (File.Exists(ssh2Success))
            {
                xbox.RecursiveCopyDirectory(sshSrcPath, sshDestPath);
                File.WriteAllText(successSSHPath, "Directory copy successful");
            }

            // Gracefully terminate the app
            ApplicationView.GetForCurrentView().TryConsolidateAsync();
        }
    }
}
