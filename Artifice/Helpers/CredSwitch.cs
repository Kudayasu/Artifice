using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace Artifice
{
    public class CredSwitch
    {
        public async void ShowCredentialDialog(ToggleButton switchButton)
        {
            if (switchButton.IsChecked == true)
            {
                var dialog = new CredentialDialog
                {
                    MainInstruction = "Please enter your credentials",
                    Content = "Enter the username and password for your admin account",
                    Target = "Artifice_Custom"
                };

                if (dialog.ShowDialog() == true)
                {
                    string username = dialog.UserName;
                    string password = dialog.Password;

                    await UpdateCmdFileAsync(username, password);
                }
            }

            else
            {
                // If the toggle button is not checked, revert to default credentials
                await UpdateCmdFileAsync("admin", "admin");
            }
        }

        public async Task UpdateCmdFileAsync(string newUsername, string newPassword)
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string path = Path.Combine(currentDirectory, @"Scratch\SSH\elevate.cmd");
            string[] lines = await File.ReadAllLinesAsync(path);

            // Parse the second line to extract the current username and password
            string[] parts = lines[1].Split(' ');
            string oldUsername = parts[2];
            string oldPassword = parts[3];

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains($"net1.exe user {oldUsername}"))
                {
                    lines[i] = lines[i].Replace(oldUsername, newUsername).Replace(oldPassword, newPassword);
                }
                else if (lines[i].Contains($"net1.exe localgroup") && lines[i].EndsWith($"{oldUsername} /add"))
                {
                    // Use regex to replace the old username only when it appears as a whole word
                    lines[i] = Regex.Replace(lines[i], $@"\b{oldUsername}\b", newUsername);
                }
            }

            await File.WriteAllLinesAsync(path, lines);
        }
    }
}
