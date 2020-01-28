# Known Issues

### Running Linux builds in Windows terminals causes graphical glitches
E.g. when running in an SSH session, or a linux build on WSL.
Launching the TUI with mono instead of dotnet, seems to work: 

`mono OpenTAP.TUI/bin/Debug/tap.dll tui`