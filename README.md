# Multi-Node

A C# implementation of a master/repeater/user-device WiFi network protocol for up to 100 nodes covering a 1 km2 area. Nodes self-organise using a structured 8-bit ID allocation scheme, automatic master election, and dynamic topology management.

**Source last updated:** 2025-06-22  
**Language:** C#  
**Target:** .NET 9  
**Output:** libraries plus master/repeater/user executables

## Solution structure

| Project | Language | Type | Purpose |
| --- | --- | --- | --- |
| `MultiNodeNetwork.Common` | C# | Library | Shared models, events, enums |
| `MultiNodeNetwork.Core` | C# | Library | Protocol library - election, discovery, topology, services |
| `MultiNodeNetwork.Master` | C# | WinExe | Master node executable |
| `MultiNodeNetwork.Repeater` | C# | WinExe | Repeater node (if present in tree) |
| `MultiNodeNetwork.UserDevice` | C# | WinExe | User device node (if present in tree) |

## How to open

Open `MultiNodeNetwork.sln` in Visual Studio.

## Requirements

- Visual Studio 2022 or 2026
- .NET 9 SDK

## Attribution and provenance

Working copy from my Historical Dev folder. Protocol design and implementation are Dave Robinson / VaderConsulting work in this tree.

## License

MIT. See `LICENSE`.
