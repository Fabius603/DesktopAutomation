# Development scripts

Run these commands from any directory:

```powershell
# Build and start the application in Release mode.
.\scripts\Development\Start-App.ps1

# Build and start the application with current Debug symbols.
.\scripts\Development\Start-Debug.ps1

# Run the complete repository verification, including all tests.
.\scripts\Development\Test.ps1
```

Alternatively, double-click `Run.bat` and select the desired action from the menu.

The scripts use the SDK version pinned in `global.json`. They also recognize a matching
user installation in `%USERPROFILE%\.dotnet`, even when the system `dotnet` appears first
in `PATH`. The start scripts and `eng/verify.ps1` share this SDK selection; it only changes
the environment of the script and its child processes.

`Start-App.ps1 -Configuration Debug` can also be used for a normal start with the Debug
configuration. `Start-Debug.ps1` always performs an explicit Debug build before starting the
application with `--no-build`, so a debugger can be attached to the resulting process without
running stale binaries.

The menu stays open: `1` starts the Debug app, `2` starts the test app (Release),
`3` stops the app, and `4` stops the app and runs repository verification.
Afterwards you can start again in the same console. Starting another configuration
first stops the previous app. `Q` stops the app and closes the menu.
Stopping requests a normal window close, then terminates the process after five
seconds if necessary. Only the app started by this menu is stopped.
Both configurations use the usual application data.

The menu resolves the executable from MSBuild's `TargetPath` for the selected configuration
after a successful build. It must not use a fixed target-framework folder: old build folders
can remain on disk after a framework change and otherwise launch outdated code. If an app
still shows old behavior, inspect its process executable path. Close the old app through
the tray's Exit action and reopen `Run.bat` after updating the launcher; an already running
PowerShell menu retains the script version it loaded at startup.
