# Debugging
The TUI is created using Microsoft .NET Core 2.1. Which can be debugged with any IDE that support .NET Core.

## Visual Studio Code
Open the project in Visual Studio Code, and launch the appropriate debug configuration. Either `Launch Clr` for windows or `Launch CoreClr` for linux.

## JetBrains Rider
Create a `.NET Executable` configuration with the following args:

![](./images/RiderConfig.jpg)