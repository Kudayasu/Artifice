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
            var sshSrcPath = "D:\\DevelopmentFiles\\SSH";
            var sshDestPath = "T:\\ProgramData\\ssh";
            var successSSHPath = "D:\\sshReady.txt";

            if (!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
            }

            if (!File.Exists(successFilePath))
            {
                using (File.Create(Path.Combine(tempDirectory, "test.txt"))) { }
                xbox.RecursiveCopyDirectory(tempDirectory, "S:\\Windows\\System32\\config\\systemprofile\\.ssh");
                File.WriteAllText(successFilePath, "Directory creation successful");
            }

            else
            {
                xbox.RecursiveCopyDirectory(sshSrcPath, sshDestPath);
                File.WriteAllText(successSSHPath, "Directory copy successful");
            }

            // Gracefully terminate the app
            ApplicationView.GetForCurrentView().TryConsolidateAsync();
        }
    }
}
