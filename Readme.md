# OpenTAP TUI (Textual User Interface)
The OpenTAP TUI is a textual based user interface that can be used from a terminal. It gives you a graphical way to create OpenTAP test plans (`.TapPlan`).

It supports running in almost every terminal including in Docker containers.

![](doc/images/TUI.jpg)

## Install
Either build it from source or install it from the OpenTAP package repository ([packages.opentap.io](packages.opentap.io)).

### Install from Repository
- Install: `tap package install TUI --version any`
- Run: `tap tui`

### From source
- Build: `dotnet build`
- Run: `OpenTAP.TUI/bin/Debug/tap tui`


## Documentation
More documentation is available [here](https://opentap.gitlab.io/Plugins/keysight/opentap-tui).


## Known Issues

### Running Linux builds in Windows terminals causes graphical glitches
E.g. when running in an SSH session, or a linux build on WSL.
Launching the TUI with mono instead of dotnet, seems to work: 

`mono OpenTAP.TUI/bin/Debug/tap.dll tui`

## Maintainers
@StefanHolst - stefan.holst@keysight.com.