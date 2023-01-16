# Installing
Either build it from source or install it from the OpenTAP package repository ([packages.opentap.io](packages.opentap.io)).

## Install from Repository
- Install: `tap package install TUI`
- Run: `tap tui`
- Run Package Manager: `tap tui-pm`
- Run Results Viewer: `tap tui-results`

## From Source
- Build: `dotnet build`
- Run: `OpenTAP.TUI/bin/Debug/tap tui`
- Run Package Manager: `OpenTAP.TUI/bin/Debug/tap tui-pm`
- Run Results Viewer: `OpenTAP.TUI/bin/Debug/tap tui-results`

## Via Docker
- See https://hub.docker.com/r/opentapio/opentap/tags for available OpenTap Docker images, the following example uses `9.15-ubuntu18.04`.
- Run: `docker pull opentapio/opentap:9.18.3-bionic`
- Run: `docker run --name mytui -it opentapio/opentap:9.18.3-bionic`
- Run: `tap package install TUI`
- Run: `tap tui`
