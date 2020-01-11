# OpenTAP TUI (Textual User Interface)
The OpenTAP TUI is a textual based user interface that can be used from a terminal. It gives you a grafical way to create OpenTAP test plans (`.TapPlan`).

It supports running in almost every terminal including in Docker containers.

![](doc/images/TUI.gif)

## Install
Either build it from source or install it from the OpenTAP package repository ([packages.opentap.io](packages.opentap.io)).

### Install from Repository
- Install: `tap package install TUI`
- Run: `tap tui`

### From source
- Build: `dotnet build`
- Run: `OpenTAP.TUI/bin/Debug/tap tui`

## Navigation

### Keyboard Shortcuts

|Keys|Shortcut|
|-|-|
|CTRL+T|Add new test step|
|CTRL+S|Save the current test plan|
|CTRL+Shift+S|Save the current test plan as a new file|
|CTRL+O|Open a test plan|
|CTRL+X or CTRL+C|Quit the TUI|
|F1|Go to the test plan panel|
|F2|Go to the step settings panel|
|F3|Go to the test setting description panel|
|F4|Go to the log panel|
|F9|Go to menu|
|TAB|Go to the next panel|
|ESC|Go back|


![](doc/images/TUI2.jpg)

## Known issues

### Running Linux builds in Windows terminals causes graphical glitches
E.g. when running in an SSH session, or a linux build on WSL.
Launching the TUI with mono instead of dotnet, seems to work: 

`mono OpenTAP.TUI/bin/Debug/tap.dll tui`
