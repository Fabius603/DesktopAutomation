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
