# 100-Node WiFi Network Protocol Design (C#)

## Overview

This project implements a master-slave WiFi network protocol for a 100-node system covering a 1km² area with repeater nodes every 100m, built in C# for .NET with structured node ID allocation.

## Network Architecture

### Key Specifications

- **Coverage Area**: 1km × 1km square
- **Total Nodes**: 100
- **Repeater Spacing**: 100m intervals
- **Master Node**: Single master (first device powered on)
- **Communication**: WiFi-based broadcasts on dedicated network
- **Framework**: .NET 6.0+
- **Platform**: Cross-platform (Windows, Linux, macOS)

### Node ID Allocation System

The network uses a structured 8-bit node ID allocation system for efficient identification and management:

| Category              | Hex Range | Decimal Range | Total Slots | Notes                                    |
| --------------------- | --------- | ------------- | ----------- | ---------------------------------------- |
| **Masters**           | 0x01-0x0F | 1-15          | 15          | Clean separation for network controllers |
| **Repeaters**         | 0x10-0x7F | 16-127        | 112         | Intermediate network devices             |
| **Users/End Devices** | 0x80-0xFF | 128-255       | 128         | Terminal devices and sensors             |

#### Key Benefits

- **Clean hex boundaries** (0x10, 0x80) for easy identification
- **Ample expansion room** in all categories
- **Easy node type identification** by address range
- **Efficient 8-bit address space utilisation**

### Network Topology

```
Grid Layout (10×10):
┌─────┬─────┬─────┬─────┬─────┬─────┬─────┬─────┬─────┬─────┐
│ M01 │ R02 │ R03 │ R04 │ R05 │ R06 │ R07 │ R08 │ R09 │ R10 │
├─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┤
│ R11 │ R12 │ R13 │ R14 │ R15 │ R16 │ R17 │ R18 │ R19 │ R20 │
├─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┤
│ R21 │ R22 │ R23 │ R24 │ R25 │ R26 │ R27 │ R28 │ R29 │ R30 │
├─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┤
│ R31 │ R32 │ R33 │ R34 │ R35 │ R36 │ R37 │ R38 │ R39 │ R40 │
├─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┤
│ R41 │ R42 │ R43 │ R44 │ R45 │ R46 │ R47 │ R48 │ R49 │ R50 │
├─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┤
│ R51 │ R52 │ R53 │ R54 │ R55 │ R56 │ R57 │ R58 │ R59 │ R60 │
├─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┤
│ R61 │ R62 │ R63 │ R64 │ R65 │ R66 │ R67 │ R68 │ R69 │ R70 │
├─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┤
│ R71 │ R72 │ R73 │ R74 │ R75 │ R76 │ R77 │ R78 │ R79 │ R80 │
├─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┤
│ R81 │ R82 │ R83 │ R84 │ R85 │ R86 │ R87 │ R88 │ R89 │ R90 │
├─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┼─────┤
│ R91 │ R92 │ R93 │ R94 │ R95 │ R96 │ R97 │ R98 │ R99 │ R100│
└─────┴─────┴─────┴─────┴─────┴─────┴─────┴─────┴─────┴─────┘

Legend:
- M01: Master Node (0x01) - first powered on
- R##: Repeater Nodes (0x10-0x73)
- U##: User Devices (0x80-0xFF) - not shown in grid
```

## Protocol Components

### 1. Master Election Protocol

- First device powered on becomes master (0x01)
- Master broadcasts presence every 30 seconds
- Backup master election if current master fails (0x02)
- Priority-based election using node type and capabilities

### 2. Node Discovery Protocol

- Nodes broadcast discovery packets on startup with node ID
- Master maintains network topology map with byte node IDs
- Automatic neighbor detection and routing
- Type-specific capabilities based on node ID range

### 3. Communication Protocols

- UDP broadcast for discovery and control
- TCP for reliable data transmission
- JSON-based message format with structured node IDs
- Support for both string and byte node identifiers

### 4. Node ID Management

- Centralized node ID allocation and validation
- Automatic type identification from node ID
- Conflict detection and resolution
- Statistics and monitoring of allocation usage

### 5. Repeater Management

- Automatic signal strength monitoring
- Dynamic routing based on link quality
- Load balancing across multiple paths
- Type-specific routing capabilities

## Solution Structure

```
MultiNodeNetwork.sln
├── src/
│   ├── MultiNodeNetwork.Core/           # Core protocol library
│   │   ├── Protocols/
│   │   │   ├── MasterElection.cs
│   │   │   ├── NodeDiscovery.cs
│   │   │   ├── Communication.cs
│   │   │   └── Topology.cs
│   │   ├── Models/
│   │   │   ├── NetworkMessage.cs
│   │   │   ├── NodeInfo.cs
│   │   │   └── TopologyMap.cs
│   │   ├── Services/
│   │   │   ├── WifiManager.cs
│   │   │   ├── MessageHandler.cs
│   │   │   ├── NodeIdManager.cs
│   │   │   └── Logger.cs
│   │   ├── Configuration/
│   │   │   └── NetworkConfig.cs
│   │   └── NodeIdAllocationTests.cs
│   ├── MultiNodeNetwork.Master/         # Master node application
│   │   ├── Program.cs
│   │   ├── MasterNode.cs
│   │   ├── MasterNodeService.cs
│   │   └── MasterNode.csproj
│   ├── MultiNodeNetwork.Repeater/       # Repeater node application
│   │   ├── Program.cs
│   │   ├── RepeaterNode.cs
│   │   └── RepeaterNode.csproj
│   └── MultiNodeNetwork.Common/         # Shared utilities
│       ├── Extensions/
│       ├── Helpers/
│       └── Constants/
├── tests/
│   ├── MultiNodeNetwork.Tests/          # Unit tests
│   │   ├── ProtocolTests/
│   │   ├── ServiceTests/
│   │   └── IntegrationTests/
│   └── MultiNodeNetwork.Performance/    # Performance tests
└── docs/
    ├── ProtocolSpecification.md
    ├── DeploymentGuide.md
    └── APIReference.md
```

## Getting Started

### Prerequisites

- Visual Studio 2022
- .NET 6.0 SDK or later
- Windows 10/11 (for WiFi functionality)

### Building the Solution

1. Open `MultiNodeNetwork.sln` in Visual Studio 2022
2. Restore NuGet packages
3. Build the solution (Ctrl+Shift+B)

### Running the Applications

1. **Master Node**: Set `MultiNodeNetwork.Master` as startup project and run
2. **Repeater Node**: Set `MultiNodeNetwork.Repeater` as startup project and run

### Configuration

- WiFi SSID: `MultiNode_Network`
- Broadcast Port: `5000`
- TCP Port: `5001`
- Discovery Interval: `30 seconds`
- Heartbeat Interval: `10 seconds`

### Node ID Allocation

The system automatically manages node ID allocation:

- **Master Nodes**: Automatically allocated 0x01 (primary) or 0x02 (backup)
- **Repeater Nodes**: Automatically allocated from 0x10-0x7F range
- **User Devices**: Automatically allocated from 0x80-0xFF range

## Key Features

- **Structured Node IDs**: Clean 8-bit allocation with type identification
- **Async/Await**: Full asynchronous communication
- **Dependency Injection**: Clean architecture with DI container
- **Logging**: Structured logging with Serilog and hex node ID display
- **Configuration**: JSON-based configuration with validation
- **Testing**: Comprehensive unit and integration tests
- **Performance**: Optimized for high-throughput communication
- **Type Safety**: Strong typing with NodeType enums
- **Statistics**: Real-time node ID allocation and network statistics

## Node ID Examples

```csharp
// Node type identification
var nodeType = NodeIdAllocation.GetNodeType(0x01); // Returns NodeType.Master
var nodeType = NodeIdAllocation.GetNodeType(0x10); // Returns NodeType.Repeater
var nodeType = NodeIdAllocation.GetNodeType(0x80); // Returns NodeType.UserDevice

// Node ID formatting
var formatted = NodeIdAllocation.FormatNodeId(0x01); // Returns "0x01 (1)"
var formatted = NodeIdAllocation.FormatNodeId(0xFF); // Returns "0xFF (255)"

// Node ID parsing
var nodeId = NodeIdAllocation.ParseNodeId("0x01"); // Returns 0x01
var nodeId = NodeIdAllocation.ParseNodeId("255");  // Returns 0xFF
```

## Network Monitoring

The system provides comprehensive monitoring:

- **Real-time Statistics**: Node count, online status, topology version
- **Node Type Distribution**: Count of masters, repeaters, and user devices
- **Node ID Allocation**: Usage statistics for each node type range
- **Topology Information**: Connection maps and routing paths
- **Performance Metrics**: Network load, signal strength, uptime
