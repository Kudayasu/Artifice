![logo](https://github.com/Kudayasu/Artifice/assets/17820526/efac32c2-2290-495f-b405-e80ec0784763)

# Artifice

A custom tool designed to achieve privilege escalation autonomously for Xbox One Developer Mode.

![tool](https://github.com/Kudayasu/Artifice/assets/17820526/45729a6c-6a12-49d0-b86d-380ca66f40e2)

## Learn more about the project [here](https://kudayasu.github.io)

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

- **Q**: I reached "Process Complete", but was unable to login. Why?
- **A**: There are safeguards in place to prevent this from happening. However, if it does, restart your console and try again.

- **Q**: The log displays error (0x********) when launching the UWP app, what's happening?
- **A**: Typically a result of corruption (i.e. exiting Artifice as the install step takes place), or having had the app already installed. In most cases, you should be able to uninstall it via DevHome, WDP, or My Games & Apps, and attempt the process again. If the same error occurs on the next attempt (i.e. [0x80270300](https://support.xbox.com/en-US/help/errors/error-code-0x80270300)) - you'll need to exit Developer Mode and ensure you keep the `Delete sideloaded games and apps` checkbox ticked. Optionally, you may also perform a factory reset (keep games and apps is fine).
