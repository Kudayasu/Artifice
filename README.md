![logo](https://github.com/Kudayasu/Artifice/assets/17820526/3cee6279-0619-4b2b-a73f-fce06aab2df0)

# Artifice

A custom tool designed to achieve privilege escalation autonomously for Xbox One Developer Mode.

![tool](https://github.com/Kudayasu/Artifice/assets/17820526/45d3d2b8-1e91-4235-a518-b6348bb9e07a)

## Learn more about the project [here](https://kudayasu.github.io/an-autopsy-of-artifice)

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

It should be placed next to `acl.bat` `art.bat` `icacls.exe` `net1.exe`

## Post-Build Action
**XboxWDPDriver**: The default behavior after building is to copy the application's files `WindowsDevicePortalWrapper.dll` `XboxWdpDriver.exe` `XboxWdpDriver.exe.config` to the **Scratch\WDP** folder. You may change this in the project properties under `Build Events`.

## Q&A
- **Q**: What's the purpose of the toggle?
- **A**: If you don't want to use the default set credentials of `admin:admin` for the admin profile, you'll be prompted to enter your own.

- **Q**: I went to add an additional user, and left the toggle off, but the log showed custom credentials. Why?
- **A**: If you're using the uploaded zip under **Releases** and have set custom credentials before, but then decide to skip custom credentials on the next attempt, it will use the custom credentials from the previous run. These are set in `Artifice\Scratch\SSH\elevate.cmd` - watch out for this.

- **Q**: I reached "Process Complete", but was unable to login. Why?
- **A**: There are safeguards in place to prevent this from happening. However, if it does, restart your console and try again. 

- **Q**: The log displays error (0x********) when launching the UWP app, what's happening?
- **A**: Typically a result of corruption (i.e. exiting Artifice as the install step takes place), or having had the app already installed. In most cases, you should be able to uninstall it via DevHome, WDP, or My Games & Apps, and attempt the process again. If the same error occurs on the next attempt (i.e. [0x80270300](https://support.xbox.com/en-US/help/errors/error-code-0x80270300)) - you'll need to exit Developer Mode and ensure you keep the `Delete sideloaded games and apps` checkbox ticked. Optionally, you may also perform a factory reset (keep games and apps is fine).
