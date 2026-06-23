# WaypointManager Compatibility Integration Plan

## Overview

This document outlines the plan to add compatibility for the [Waypoint Manager](https://github.com/linuxgurugamer/WaypointManager) mod to the LunaCompat (Luna Multiplayer compatibility layer) mod. This will allow waypoint data to be synchronized across all connected players in a Luna Multiplayer session.

---

## 1. Background

### WaypointManager Mod
- **Source:** `D:\Code\LunaMultiplayer\WaypointManager`
- **LMP Source:** `D:\Code\LunaMultiplayer\LunaMultiplayer`
- **Default waypoints:** `GameData/WaypointManager/PluginData/CustomWaypoints.cfg` (not synced - these are just default templates)
- **Important data:** Stored in `ScenarioCustomWaypoints` SFS node within the persistent.sfs
- **Critical:** The default LMP scenario sync does **NOT** sync the `ScenarioCustomWaypoints` scenario - we must do this manually for all waypoints
- **Config format:** KSP `ConfigNode` with `WAYPOINT` nodes
- **Uses KSP's `ScenarioCustomWaypoints` system for waypoint management**

### Existing LunaCompat Patterns
- **SCANsat integration** is the most complex existing example (~627 lines client, ~321 lines server)
- Follows a message-based sync pattern with:
  - Client requests data from server on connect
  - Server sends stored data to requesting client
  - Sync of changes when they occur 
    - Picked up via harmony patches
    - Sent to both other clients and server
    - Server updates its stored data
    - Clients update their state directly

---

## 2. WaypointManager Data Model

### CustomWaypoints.cfg Format
```cfg
CUSTOM_WAYPOINTS
{
    WAYPOINT
    {
        name = My Waypoint
        celestialName = Kerbin
        latitude = 9.123456
        longitude = -45.678901
        navigationId = {guid-here}
        icon = balloon
        altitude = 50.0
        index = 0
        seed = 412
    }
}
```

Note: `navigationId` is a GUID string (not an integer).

### Key Classes to Interact With
| Class | Namespace | Purpose |
|-------|-----------|---------|
| `Waypoint` | `FinePrint` | Individual waypoint object |
| `ScenarioCustomWaypoints` | `KSP` | Stock waypoint manager (OnLoad/OnSave) |
| `CustomWaypoints` | `WaypointManager` | AddWaypoint(), RemoveWaypoint(), Import(), Export() |
| `Config` | `WaypointManager` | Settings persistence |

---

## 3. Files to Create

### 3.1 Messages (LunaCompatCommon)

**File:** `LunaCompatCommon/Messages/ModMessages/WaypointManagerMessages.cs`

Messages use ConfigNode string serialization for forward compatibility - the mod can evolve without breaking message formats.

Waypoints use `navigationId` as their unique identifier for deletion/editing. The `WpManagerSyncWaypointsMessage` and `WpManagerAddWaypointMessage` can be unified into a single message type since they carry the same data structure.

```csharp
namespace LunaCompatCommon.Messages.ModMessages;

// Request all waypoints from server
public class WpManagerRequestWaypointsMessage : IModMessage { }

// Full waypoint list sync OR add/update a single waypoint (sent from server to client or client to server)
// Contains the raw ConfigNode string representation of CUSTOM_WAYPOINTS node or single WAYPOINT node
// Also includes navigationId for quick lookup by the server
public class WpManagerSyncWaypointsMessage : IModMessage
{
    public string ConfigNodeString { get; set; }
    public string NavigationId { get; set; }
}

// Remove a single waypoint (sent from client to server)
// Identifies waypoint by navigationId (unique identifier)
public class WpManagerRemoveWaypointMessage : IModMessage
{
    public string NavigationId { get; set; }
}
```

---

### 3.2 Client-Side Integration

**Directory:** `LunaCompat/Mods/WaypointManager/`

**File:** `LunaCompat/Mods/WaypointManager/WaypointManagerIntegration.cs`

#### Key Responsibilities:
1. **Harmony Patches** - Intercept waypoint add/remove operations:
   - `Postfix on CustomWaypoints.AddWaypoint()` → Send `WpManagerSyncWaypointsMessage` (single waypoint)
   - `Prefix on CustomWaypoints.RemoveWaypoint()` → Send `WpManagerRemoveWaypointMessage` (with navigationId)

2. **Message Handlers**:
   - `WpManagerRequestWaypointsMessage` → Send local custom waypoints to server
   - `WpManagerSyncWaypointsMessage` → Apply received waypoints via `ApplyWaypointsFromConfigNode()`
   - `WpManagerRemoveWaypointMessage` → Remove waypoint by navigationId locally

3. **No Primary Player Selection** - Every player can create/edit waypoints independently

4. **Configuration**:
   - `RequiresServerPlugin = true`
   - `PackageName = "Waypoint Manager"`

#### Structure:
```csharp
internal class WaypointManagerIntegration : ClientModIntegration
{
    // Setup
    public override void Setup()
    {
        // Harmony patches for AddWaypoint/RemoveWaypoint
        // Register message listeners
        // Ignore waypoint scenario messages
    }
    
    // Destroy cleanup
    public override void Destroy() { }
    
    // Message handlers
    private void OnRequestWaypointsMessageReceived(WpManagerRequestWaypointsMessage msg) { }
    private void OnSyncWaypointsMessageReceived(WpManagerSyncWaypointsMessage msg) { }
    private void OnRemoveWaypointMessageReceived(WpManagerRemoveWaypointMessage msg) { }
    
    // Helper methods
    private static string ExportLocalWaypointsAsConfigNode() { }
    private static void ApplyWaypointsFromConfigNode(string configNodeString) { }
}
```

Note: No `_syncInterval` or `_keepAlive` needed - all changes are reacted to via Harmony patches in real-time.

---

### 3.3 Server-Side Integration

**File:** `LunaCompatServerPlugin/Mods/WaypointManagerIntegration.cs`

#### Key Responsibilities:
1. **Store waypoints** in server-side memory (as ConfigNode string for forward compatibility)
2. **Persist each waypoint** to its own CFG file in `LunaCompat/WaypointManager/` in the LMP save directory, named by navigationId
3. **Send waypoint changes** to all connecting clients
4. **Handle requests** from clients on connect
5. **Load waypoints from LMP save directory** when a client connects (not from SFS - the server doesn't have access to SFS files)

#### Structure:
```csharp
internal class WaypointManagerIntegration : ServerModIntegration
{
    private readonly string _basePath;
    private Dictionary<string, ConfigNode> _waypoints;  // Keyed by navigationId
    
    public override void Setup()
    {
        // Register message listeners
        // Load existing waypoint data from LMP save directory (not from SFS)
    }
    
    public override void Destroy()
    {
        // Save waypoints to disk
        // Unregister message listeners
    }
    
    // Message handlers
    private void OnRequestWaypointsMessageReceived(ClientStructure client, WpManagerRequestWaypointsMessage msg)
    {
        // Send all stored waypoints to requesting client (one message per waypoint)
    }
    
    private void OnSyncWaypointsMessageReceived(ClientStructure client, WpManagerSyncWaypointsMessage msg)
    {
        // Persist waypoint to disk and broadcast to all other clients
    }
    
    private void OnRemoveWaypointMessageReceived(ClientStructure client, WpManagerRemoveWaypointMessage msg)
    {
        // Remove waypoint by navigationId and broadcast to all other clients
    }
}
```

---

## 4. Files to Create (No Project File Changes Needed)

Modern .NET projects (SDK-style) automatically include all `.cs` files in the compilation, so no project file modifications are needed. Just create the files in the correct directories:

- `LunaCompat/Mods/WaypointManager/WaypointManagerIntegration.cs`
- `LunaCompatServerPlugin/Mods/WaypointManagerIntegration.cs`

---

## 5. Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     Luna Multiplayer Session                    │
│                                                                 │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐     │
│  │   Client A   │    │   Client B   │    │   Client C   │     │
│  │              │    │              │    │              │     │
│  └──────┬───────┘    └──────┬───────┘    └──────┬───────┘     │
│         │                   │                   │              │
│         ▼                   │                   │              │
│  ┌──────────────┐           │                   │              │
│  │  WM Integration│          │                   │              │
│  │  (Harmony      │          │                   │              │
│  │   Patches)     │          │                   │              │
│  └──────┬───────┘          │                   │              │
│         │                   │                   │              │
│         ▼                   │                   │              │
│  ┌──────────────┐           │                   │              │
│  │  Message      │──────────┼───────────────────┼──────┐      │
│  │  Handler      │   Add/Remove/Sync Messages    │      │      │
│  └──────┬───────┘                                  │      │      │
│         │                                           │      │      │
└─────────┼───────────────────────────────────────────┼──────┼──────┘
          │                                           │      │
          ▼                                           │      │
┌─────────────────────────────────────────────────────┴──────┼──────┐
│                    Luna Compat Server                      │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │          WM Server Integration                      │   │
│  │  - Stores waypoints in memory                       │   │
│  │  - Persists to disk (one CFG per waypoint)          │   │
│  │  - Broadcasts changes to all clients                │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              Message Handler                        │   │
│  │  - Receives from Client A                           │   │
│  │  - Validates & persists                             │   │
│  │  - Broadcasts to Clients B & C                      │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## 6. Sync Flow

### Initial Connection
```
Client connects
    │
    ▼
WpManagerRequestWaypointsMessage → Server
    │
    ▼
Server reads all CFG files from LunaCompat/WaypointManager/ directory
    │
    ▼
For each waypoint:
WpManagerSyncWaypointsMessage (single waypoint) → Client
    │
    ▼
Client applies via ApplyWaypointsFromConfigNode() (same as live updates)
```

### Adding/Updating a Waypoint
```
Player adds/updates waypoint (Harmony patch)
    │
    ▼
Client persists waypoint to local ScenarioCustomWaypoints
    │
    ▼
Parallel actions:
    ├── Client sends WpManagerSyncWaypointsMessage → Server
    │
    ▼
Server persists waypoint to disk
    │
    ▼
Parallel broadcast:
    ├── WpManagerSyncWaypointsMessage → All other clients
    │
    ▼
Other clients apply via ApplyWaypointsFromConfigNode()
```

### Removing a Waypoint
```
Player removes waypoint (Harmony patch)
    │
    ▼
Client removes waypoint locally
    │
    ▼
Parallel actions:
    ├── Client sends WpManagerRemoveWaypointMessage → Server
    │
    ▼
Server removes waypoint by navigationId from disk
    │
    ▼
Parallel broadcast:
    ├── WpManagerRemoveWaypointMessage → All other clients
    │
    ▼
Other clients remove by navigationId
```

---

## 7. Implementation Checklist

- [ ] Create `LunaCompatCommon/Messages/ModMessages/WaypointManagerMessages.cs`
- [ ] Create `LunaCompat/Mods/WaypointManager/WaypointManagerIntegration.cs`
- [ ] Create `LunaCompatServerPlugin/Mods/WaypointManagerIntegration.cs`
- [ ] Test with WaypointManager mod installed
- [ ] Verify waypoint sync across multiple clients
- [ ] Verify persistence across server restarts
- [ ] Add tests for message serialization/deserialization

## 8. Considerations & Edge Cases

### Namespace Considerations
- `FinePrint.WaypointManager` and `FinePrint.Utilities` are in the KSP base assemblies (not external)
- `ScenarioCustomWaypoints` is in the `KSP` namespace
- Integration can directly reference these types (no reflection needed for ScenarioCustomWaypoints)

### Serialization
- Use ConfigNode string serialization for forward compatibility
- The navigationId is a GUID string used as unique identifier

### Performance
- Waypoints are relatively small data (~10-100 entries typically)
- Full sync on connect is acceptable
- One message per waypoint on initial sync

### Conflict Resolution
- Every player can create/edit waypoints (no primary player selection)
- Last-write-wins for simultaneous changes
- Server is the source of truth for persistence

### Stock Waypoints
- Stock waypoints (KSC, landing sites) are managed by KSP's `ScenarioCustomWaypoints`
- Integration only handles **custom** waypoints added by players