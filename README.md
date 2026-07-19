# MultiNodeNetwork

A C# implementation of a master/repeater/user-device WiFi network protocol for up to 100 nodes covering a 1 km2 area. Nodes self-organise using a structured 8-bit ID allocation scheme, automatic master election, and dynamic topology management.

**Initiated:** 2025-06-22 · **Framework:** .NET 9 · **Solution:** `MultiNodeNetwork.sln`

---

## Network Specifications

| Parameter | Value |
|-----------|-------|
| Coverage area | 1 km x 1 km |
| Total nodes | Up to 100 |
| Repeater spacing | 100 m intervals |
| Node ID width | 8-bit (0x00-0xFF) |
| Discovery transport | UDP broadcast (port 5000) |
| Data transport | TCP (port 5001) |

---

## Node ID Allocation

| Category | Range | Slots |
|----------|-------|-------|
| Masters | 0x01-0x0F | 15 |
| Repeaters | 0x10-0x7F | 112 |
| User Devices | 0x80-0xFF | 128 |

---

## Solution Structure

| Project | Purpose |
|---------|---------|
| `MultiNodeNetwork.Common` | Shared models, events, enums |
| `MultiNodeNetwork.Core` | Protocol library - election, discovery, topology, services |
| `MultiNodeNetwork.Master` | Master node executable |
| `MultiNodeNetwork.Repeater` | Repeater node executable |
| `MultiNodeNetwork.Tests` | Unit, service, and integration tests |

---

## Getting Started

```bash
dotnet build MultiNodeNetwork.sln
dotnet run --project src/MultiNodeNetwork.Master
dotnet run --project src/MultiNodeNetwork.Repeater
```