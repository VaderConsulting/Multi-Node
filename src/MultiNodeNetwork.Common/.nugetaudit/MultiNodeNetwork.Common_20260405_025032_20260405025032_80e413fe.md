# NuGet Audit Report - MultiNodeNetwork.Common

- Snapshot ID: `20260405025032_80e413fe`
- Captured UTC: `2026-04-05T02:50:32.2916213+00:00`
- Solution: `C:\Users\windo\OneDrive\Documents\Development\Multi Node\src\MultiNodeNetwork.Common\MultiNodeNetwork.Common.csproj`
- Source identity key: `25F5423821EE308F`
- Source identity kind: `disk-path`
- Source machine: `SURFACELAPTOP7`
- Repository root: `C:\Users\windo\OneDrive\Documents\Development\Multi Node\src\MultiNodeNetwork.Common`
- Git remote: `-`
- Analysis status: `Complete`
- Projects analysed: `1`
- Packages discovered: `3`
- Direct packages: `3`
- Transitive packages: `0`
- Dependency edges: `0`
- Error diagnostics: `0`
- Warning diagnostics: `3`
- Delta changed: `False`
- Knowledge snapshot generated: `2026-04-05T02:50:32.2916213+00:00`

## Attention Required

| Severity | Project | Package | Message | TFM |
|---|---|---|---|---|
| Warning | MultiNodeNetwork.Common | System.Text.Json | Package 'System.Text.Json' 9.0.6 is outdated. Latest compatible stable version: 10.0.5. | net9.0 |
| Warning | MultiNodeNetwork.Common | System.Text.Json | Package 'System.Text.Json' 9.0.6 is outdated. Latest compatible stable version: 10.0.5. | net9.0-windows |
| Warning | MultiNodeNetwork.Common | System.Text.Json | Package 'System.Text.Json' 9.0.6 is outdated. Latest compatible stable version: 10.0.5. | net9.0-windows7.0 |

## Change Summary

No previous accepted snapshot was available for comparison.

## Knowledge Changes

| Package | Version | Determined UTC | Previous Health | Current Health | Risk | Alert | Remediation | Summary |
|---|---|---|---|---|---|---|---|---|
| System.Text.Json | 9.0.6 | 2026-04-05T02:50:32.2916213+00:00 | - | outdated->10.0.5 | 32.30 (Medium) | 43.40 (Medium) | Upgrade to 10.0.5. | Attention is now required for this package version. |

## Security and Health Transitions

No package health transitions were detected relative to the previous accepted snapshot.

## Dependency Relationship Changes

No dependency edge changes were detected relative to the previous accepted snapshot.

## Project Details

### MultiNodeNetwork.Common

- Path: `C:\Users\windo\OneDrive\Documents\Development\Multi Node\src\MultiNodeNetwork.Common\MultiNodeNetwork.Common.csproj`
- Style: `PackageReference`
- Load state: `Loaded`
- Analysis status: `Complete`
- Target frameworks: `net9.0;net9.0-windows`
- Diagnostics:
  - [Warning] Package 'System.Text.Json' 9.0.6 is outdated. Latest compatible stable version: 10.0.5.
  - [Warning] Package 'System.Text.Json' 9.0.6 is outdated. Latest compatible stable version: 10.0.5.
  - [Warning] Package 'System.Text.Json' 9.0.6 is outdated. Latest compatible stable version: 10.0.5.

| Package | Kind | Requested | Resolved | Health | Vulnerability Detail | Risk | Alert | Knowledge Determined | Status Changed | Remediation | Central | Parents | Path | TFM |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| System.Text.Json | Direct | 9.0.6 | 9.0.6 | outdated->10.0.5 | - | 32.30 (Medium) | 43.40 (Medium) | 2026-04-05T02:50:32.2916213+00:00 | 2026-04-05T02:50:32.2916213+00:00 | Upgrade to 10.0.5. | No | - | System.Text.Json | net9.0 |
| System.Text.Json | Direct | 9.0.6 | - | outdated->10.0.5 | - | 32.30 (Medium) | 43.40 (Medium) | 2026-04-05T02:50:32.2916213+00:00 | 2026-04-05T02:50:32.2916213+00:00 | Upgrade to 10.0.5. | No | - | - | net9.0-windows |
| System.Text.Json | Direct | - | 9.0.6 | outdated->10.0.5 | - | 32.30 (Medium) | 43.40 (Medium) | 2026-04-05T02:50:32.2916213+00:00 | 2026-04-05T02:50:32.2916213+00:00 | Upgrade to 10.0.5. | No | - | System.Text.Json | net9.0-windows7.0 |

#### Dependency Graph Table

No dependency edges were captured for this project.

#### Dependency Graph Mermaid

```mermaid
graph TD
    Empty["No dependency edges captured"]
```

## Snapshot Warnings

- Attention required: 3 package diagnostics are warnings.
