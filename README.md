![logo](https://github.com/Kudayasu/Artifice/assets/17820526/3cee6279-0619-4b2b-a73f-fce06aab2df0)

# Artifice

A custom tool designed to achieve privilege escalation autonomously for Xbox One Developer Mode.

![tool](https://github.com/user-attachments/assets/b882ff1b-d43d-4e0d-b6a5-0613e2a23f04)

Learn more about the project [here](https://kudayasu.github.io/an-autopsy-of-artifice)\
Anything else Xbox related, check out the [wiki](https://xboxoneresearch.github.io/wiki)

## Project Composition
### Artifice (Project)
- Main WPF application.

### Universal (Project)
- UWP application utilized for accessing the `Microsoft.Xbox.Development` namespace.

### WDP (Folder)
- **Samples\XboxWdpDriver**: Primary helper application allowing for an easy way to interface with Windows Device Portal.
- **WindowsDevicePortalWrapper**: Core portion of the wrapper.
- The code for both you can find [here](https://github.com/microsoft/WindowsDevicePortalWrapper).

### XCopy (Folder)
- **ComClassActivation**: C++ Universal project to supplement the main UWP app. Utilizes COM/WinRT to allow access to the `Microsoft.Xbox.Development` namespace.

## Building
The main project that needs to be built is `Universal`. After running the [Create App Packages](https://learn.microsoft.com/en-us/windows/msix/package/packaging-uwp-apps#create-an-app-package-using-the-packaging-wizard) command, copy the .msix from `XCopy\AppPackages\Universal_1.0.0.0_x64_Test\Universal_1.0.0.0_x64.msix` to the root **Scratch** folder `Artifice\Scratch` as `art.msix` and you're set.

It should be placed next to `icacls.exe` `net1.exe`

After you've built the project and copied over the msix, ensure you've also copied the `x64` dependencies to your `Dependencies` folder in the Scratch root.

If you get stuck on **Setting up SSH** ensure you set `Copy to Output Directory` to `Copy Always` for every item within your `Dependencies` folder and your `Scratch` folder.

## Post-Build Action
**XboxWDPDriver**: The default behavior after building is to copy the application's files `WindowsDevicePortalWrapper.dll` `XboxWdpDriver.exe` `XboxWdpDriver.exe.config` to the **Scratch\WDP** folder. You may change this in the project properties under `Build Events`.

## Q&A
- **Q**: What SKU does this work for?
- **A**: Up to **10.0.25398.4911** on any SKU with an OS version that has the respective PRs [478](https://github.com/PowerShell/openssh-portable/pull/478)+[479](https://github.com/PowerShell/openssh-portable/pull/479) integrated into OpenSSH. Otherwise, this is now patched on the latest GA [10.0.26100.1968](https://support.xbox.com/en-US/help/hardware-network/settings-updates/whats-new-xbox-one-system-updates) `Durango 26100.1968.amd64fre.xb_flt_2408ge.240821-1830 GitEnlistment(ba01) Green wave4langs`

- **Q**: Is there still an easy way to escalate privs?
- **A**: [VSProfilingAccount](https://xboxoneresearch.github.io/wiki/exploits/devmode-priv-escalation-vsprofiling/) still retains SeImpersonatePrivilege + HighIL on **10.0.26100.1968**, so you can utilize something such as the [following](https://github.com/PN-Tester/AppxPotato) with several changes.

  - Migrate/upgrade the dotnet version
  - Manually set the ACL on the namedpipe
  - Edit the process creation to utilize CreateProcessAsUserW
  - Assign SeAssignPrimaryTokenPrivilege for the previous step: see why [here](https://github.com/PowerShellMafia/PowerSploit/blob/master/Exfiltration/Invoke-TokenManipulation.ps1#L1600)
  - Manually parse for other vulnerable methods within the [RPC server](https://googleprojectzero.blogspot.com/2019/12/calling-local-windows-rpc-servers-from.html)

- **Q**: What's the purpose of the toggles?
- **A**: The first toggle allows you to choose custom credentials. By default it's set to `admin:admin` for the account. The second toggle is for disabling various telemetry services via the `veil.bat` batch file. 

- **Q**: I reached "Process Complete", but was unable to login. Why?
- **A**: There are safeguards in place to prevent this from happening. However, if it does, restart your console and try again. 

- **Q**: The log displays error (0x********) when launching the UWP app, what's happening?
- **A**: Typically a result of corruption (i.e. exiting Artifice as the install step takes place), or having had the app already installed. In most cases, you should be able to uninstall it via DevHome, WDP, or My Games & Apps. If the app fails to launch in StepFive, you'll be prompted to uninstall and restart your console. If the same error occurs on the next attempt (i.e. [0x80270300](https://support.xbox.com/en-US/help/errors/error-code-0x80270300)) - you'll need to exit Developer Mode and ensure you keep the `Delete sideloaded games and apps` checkbox ticked. Optionally, you may also perform a factory reset (keep games and apps is fine).
