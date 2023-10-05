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

            var ssh1File = "D:\\SSH1.txt";
            var ssh1Folder = "D:\\DevelopmentFiles\\artssh";
            var ssh1Success = "D:\\SSH1Success.txt";

            var ssh2File = "D:\\SSH2.txt";
            var ssh2Folder = "D:\\DevelopmentFiles\\SSH2";
            var ssh2Success = "D:\\SSH2Success.txt";

            var sshSrcPath = "D:\\DevelopmentFiles\\SSH";
            var sshDestPath = "T:\\ProgramData\\ssh";
            var successSSHPath = "D:\\sshReady.txt";

            // Step 3: First Launch
            if (File.Exists(ssh1File))
            {
                xbox.RecursiveCopyDirectory(sshDestPath, ssh1Folder);
                File.WriteAllText(ssh1Success, "Directory copy successful");
                File.Delete(ssh1File);
            }

            // Step 5: Second Launch
            if (!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
                using (File.Create(Path.Combine(tempDirectory, "test.txt"))) { }
                xbox.RecursiveCopyDirectory(tempDirectory, "S:\\Windows\\System32\\config\\systemprofile\\.ssh");
                File.WriteAllText(successFilePath, "Directory creation successful");
            }

            // Step 6: Third Launch
            if (File.Exists(ssh2File))
            {
                xbox.RecursiveCopyDirectory(ssh2Folder, "S:\\Windows\\System32\\config\\systemprofile\\.ssh");
                File.WriteAllText(ssh2Success, "Directory copy successful");
            }

            // Step 7: Fourth Launch
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
