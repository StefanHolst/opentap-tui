# Installing
Either build it from source or install it from the OpenTAP package repository ([packages.opentap.io](packages.opentap.io)).

## Install from Repository
- Install: `tap package install TUI --version any`
- Run: `tap tui`
- Run Package Manager: `tap tui-pm`

## From Source
- Build: `dotnet build`
- Run: `OpenTAP.TUI/bin/Debug/tap tui`
- Run Package Manager: `OpenTAP.TUI/bin/Debug/tap tui-pm`

## Via Docker
- See https://hub.docker.com/r/opentapio/opentap/tags for available OpenTap Docker images, the following example uses `9.15-ubuntu18.04`.
- Run: `docker pull opentapio/opentap:9.15-ubuntu18.04`
- Run: `docker run --name mytui -it opentapio/opentap:9.15-ubuntu18.04`
- Run: `tap package install TUI --version any`
- Run: `tap tui`
