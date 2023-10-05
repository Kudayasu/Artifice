![logo](https://github.com/Kudayasu/Artifice/assets/17820526/3cee6279-0619-4b2b-a73f-fce06aab2df0)

# Artifice

A custom tool designed to achieve privilege escalation autonomously for Xbox One Developer Mode.

![tool](https://github.com/Kudayasu/Artifice/assets/17820526/2c33722c-7d6b-496a-a155-268a8836dc48)

## Learn more about the project [here](https://kudayasu.github.io/an-autopsy-of-artifice)
## Anything else Xbox related, check out the [wiki](https://xboxresearch.com/wiki)

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
- **A**: Fundamentally, this should work for any generation. Personal testing was done on an original Durango on the latest GA [10.0.25398.2258](https://support.xbox.com/en-US/help/hardware-network/settings-updates/whats-new-xbox-one-system-updates) `Durango 25398.2258.amd64fre.xb_flt_2309zn.230918-2000 GitEnlistment(ba01) Green wave4langs`

- **Q**: What's the purpose of the toggles?
- **A**: The first toggle allows you to choose custom credentials. By default it's set to `admin:admin` for the account. The second toggle is for disabling various telemetry services via the `veil.bat` batch file. 

- **Q**: I reached "Process Complete", but was unable to login. Why?
- **A**: There are safeguards in place to prevent this from happening. However, if it does, restart your console and try again. 

- **Q**: The log displays error (0x********) when launching the UWP app, what's happening?
- **A**: Typically a result of corruption (i.e. exiting Artifice as the install step takes place), or having had the app already installed. In most cases, you should be able to uninstall it via DevHome, WDP, or My Games & Apps. If the app fails to launch in StepFive, you'll be prompted to uninstall and restart your console. If the same error occurs on the next attempt (i.e. [0x80270300](https://support.xbox.com/en-US/help/errors/error-code-0x80270300)) - you'll need to exit Developer Mode and ensure you keep the `Delete sideloaded games and apps` checkbox ticked. Optionally, you may also perform a factory reset (keep games and apps is fine).