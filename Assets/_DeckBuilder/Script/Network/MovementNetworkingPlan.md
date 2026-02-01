# Movement Networking Plan: Client-Side Prediction & Server Reconciliation

This document outlines the architecture for converting the `Movement` system to use **client-side prediction** and **server reconciliation** for responsive, fast-paced multiplayer gameplay.

---

## Table of Contents

1. [Current Architecture Analysis](#1-current-architecture-analysis)
2. [Target Architecture Overview](#2-target-architecture-overview)
3. [Player Entity Prefab Architecture](#3-player-entity-prefab-architecture)
4. [Client-Side Prediction](#4-client-side-prediction)
5. [Server Reconciliation](#5-server-reconciliation)
6. [Entity Interpolation](#6-entity-interpolation)
7. [Lag Compensation](#7-lag-compensation)
8. [Implementation Plan](#8-implementation-plan)
9. [Data Structures](#9-data-structures)
10. [Network Messages](#10-network-messages)
11. [Edge Cases & Error Handling](#11-edge-cases--error-handling)

---

## 1. Current Architecture Analysis

### Current Implementation (`Movement.cs`)

The current system uses a simple authoritative model:

```
Client Input → ServerRpc → Server processes NavMeshAgent → NetworkTransform syncs position
```

**Issues for Fast-Paced Gameplay:**

| Problem | Impact |
|---------|--------|
| Input latency | 50-200ms delay before seeing movement response |
| No prediction | Player feels "sluggish" controls |
| NavMeshAgent on server only | Client has no local simulation |
| No reconciliation | No correction when prediction diverges |
| NetworkTransform interpolation | Other players appear choppy or delayed |

### Current Code Flow

1. `MoveTo(Vector3 worldTo)` checks `IsOwner`
2. Calls `Move_Internal()` directly (currently bypasses RPC)
3. `NavMeshAgent.SetDestination()` executes
4. Position synced via NetworkTransform (implicit)

---

## 2. Target Architecture Overview

### Design Principles

1. **Authoritative Server**: Server is the single source of truth for all positions
2. **Responsive Client**: Local player sees immediate feedback via prediction
3. **Smooth Visuals**: Other players interpolated smoothly between server snapshots
4. **Cheat Resistant**: Server validates all movement against NavMesh constraints

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CLIENT (Owner)                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│  Input Capture                                                               │
│       ↓                                                                      │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐         │
│  │ Input Buffer    │───→│ Local Prediction │───→│ Visual Position │         │
│  │ (with seq #)    │    │ (NavMeshAgent)   │    │ (Immediate)     │         │
│  └─────────────────┘    └─────────────────┘    └─────────────────┘         │
│       │                        ↑                                            │
│       │                        │ Reconciliation                             │
│       │                        │ (replay unacked inputs)                    │
│       ↓                        │                                            │
│  Send to Server          Server State Received                              │
└─────────────────────────────────────────────────────────────────────────────┘
                    │                        ↑
                    ↓                        │
┌─────────────────────────────────────────────────────────────────────────────┐
│                                 SERVER                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐         │
│  │ Input Queue     │───→│ Authoritative   │───→│ Broadcast State │         │
│  │ (per client)    │    │ Simulation      │    │ (with ack seq)  │         │
│  └─────────────────┘    └─────────────────┘    └─────────────────┘         │
│                         │ NavMeshAgent    │                                 │
│                         │ Validation      │                                 │
└─────────────────────────────────────────────────────────────────────────────┘
                                             │
                                             ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                           CLIENT (Non-Owner)                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐         │
│  │ State Buffer    │───→│ Interpolation   │───→│ Visual Position │         │
│  │ (timestamps)    │    │ (render past)   │    │ (Smooth)        │         │
│  └─────────────────┘    └─────────────────┘    └─────────────────┘         │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Player Entity Prefab Architecture

This section defines the recommended prefab structure for networked player entities that support client-side prediction, visual interpolation, and lag compensation.

### 3.1 Prefab Hierarchy

```
PlayerEntity (Root)
│
├── [Simulation]                    ← Physics/NavMesh simulation (invisible)
│   ├── NavMeshObstacle (optional)  ← For other agents to avoid
│   └── HitboxRoot                  ← Server-authoritative hitboxes
│       ├── Hitbox_Head
│       ├── Hitbox_Body
│       └── Hitbox_Legs
│
├── [Visual]                        ← Rendered representation (interpolated)
│   ├── Model                       ← 3D character model
│   │   ├── Armature               ← Skeleton root
│   │   └── SkinnedMeshRenderer    ← Character mesh
│   ├── VFX_Root                   ← Visual effects attachment point
│   └── Shadow                     ← Blob shadow or projector
│
├── [Audio]                         ← Sound sources
│   ├── FootstepSource
│   └── VoiceSource
│
└── [UI]                            ← World-space UI elements
    ├── Nameplate
    └── HealthBar
```

### 3.2 Root GameObject Components

The root object contains all networking and core logic components:

| Component | Purpose | Network Sync |
|-----------|---------|--------------|
| `NetworkObject` | Netcode identity and ownership | Required |
| `Movement` | Movement prediction/reconciliation | Custom sync |
| `Rigidbody` | Physics body (kinematic for characters) | Via Movement |
| `CapsuleCollider` | Physics collision shape | No |
| `NavMeshAgent` | Pathfinding and steering | Server + Owner |
| `GlobalStatSource` | Stats for speed modifiers | NetworkVariable |
| `StatsModifierManager` | Buff/debuff application | Server authority |
| `AnimationHandler` | Animation state management | Via NetworkAnimator |
| `Health` | Damage and death handling | NetworkVariable |
| `AbilityCaster` | Ability execution | Server authority |

```csharp
// Root GameObject setup
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(NavMeshAgent))]
public class PlayerEntity : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform simulationRoot;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform hitboxRoot;
}
```

### 3.3 Simulation vs Visual Separation

The key architectural principle is **separating simulation from visuals**:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           SIMULATION LAYER                                   │
│  • Root transform position = authoritative/predicted position               │
│  • NavMeshAgent attached here                                               │
│  • Rigidbody attached here                                                  │
│  • Colliders for physics queries                                            │
│  • Invisible (no renderers)                                                 │
│  • Updated by prediction (owner) or server state (non-owner)                │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ Visual follows with offset/interpolation
                                    ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                             VISUAL LAYER                                     │
│  • Child transform with local offset                                        │
│  • Contains all renderers (SkinnedMeshRenderer, etc.)                       │
│  • Contains VFX and UI elements                                             │
│  • Smoothly interpolated position                                           │
│  • Owner: follows simulation + correction smoothing                         │
│  • Non-owner: follows interpolation buffer                                  │
└─────────────────────────────────────────────────────────────────────────────┘
```

#### Why Separate?

| Reason | Explanation |
|--------|-------------|
| Smooth corrections | Visual can lerp while simulation snaps to server position |
| Interpolation | Non-owners render in the past without affecting physics |
| Hit registration | Server uses simulation position for authoritative hits |
| Animation blending | Visual rotation can blend smoothly during fast turns |

### 3.4 NavMeshAgent Configuration

#### Server NavMeshAgent (Primary)

Located on root, handles authoritative movement:

```
NavMeshAgent Settings:
├── Agent Type: Humanoid (or custom)
├── Speed: Driven by GlobalStatSource
├── Angular Speed: 720 (fast turning)
├── Acceleration: 50 (responsive)
├── Stopping Distance: 0.1
├── Auto Braking: true
├── Obstacle Avoidance
│   ├── Radius: 0.3
│   ├── Height: 1.8
│   ├── Quality: High Quality
│   └── Priority: 50
└── Path Finding
    ├── Auto Traverse Off Mesh Link: true
    ├── Auto Repath: true
    └── Area Mask: Walkable
```

#### Owner Client Prediction

For client-side prediction, the owner enables NavMeshAgent locally:

```csharp
public override void OnNetworkSpawn()
{
    base.OnNetworkSpawn();

    if (IsOwner)
    {
        // Enable prediction NavMeshAgent
        _navMeshAgent.enabled = true;
        _navMeshAgent.updatePosition = true;
        _navMeshAgent.updateRotation = true;
    }
    else if (IsServer)
    {
        // Server runs authoritative simulation
        _navMeshAgent.enabled = true;
    }
    else
    {
        // Non-owner clients: disable agent, use interpolation
        _navMeshAgent.enabled = false;
    }
}
```

### 3.5 Rigidbody Configuration

```
Rigidbody Settings:
├── Mass: 1
├── Drag: 0
├── Angular Drag: 0.05
├── Use Gravity: false (NavMeshAgent handles ground)
├── Is Kinematic: true (NavMeshAgent controls position)
├── Interpolate: None (visual layer handles this)
├── Collision Detection: Discrete
└── Constraints:
    ├── Freeze Position: None
    └── Freeze Rotation: X, Z (only Y rotation allowed)
```

### 3.6 Collider Setup

#### Physics Collider (Root)

Used for movement blocking and ground detection:

```csharp
CapsuleCollider:
├── Center: (0, 0.9, 0)
├── Radius: 0.3
├── Height: 1.8
├── Direction: Y-Axis
└── Layer: "Player" (for physics filtering)
```

#### Hitbox Colliders (Hitbox Root)

Used for damage detection with lag compensation:

```
HitboxRoot/
├── Hitbox_Head (SphereCollider)
│   ├── Center: (0, 1.6, 0)
│   ├── Radius: 0.15
│   ├── Is Trigger: true
│   └── Layer: "Hitbox"
│
├── Hitbox_Body (CapsuleCollider)
│   ├── Center: (0, 1.0, 0)
│   ├── Radius: 0.25
│   ├── Height: 0.8
│   ├── Is Trigger: true
│   └── Layer: "Hitbox"
│
└── Hitbox_Legs (CapsuleCollider)
    ├── Center: (0, 0.4, 0)
    ├── Radius: 0.15
    ├── Height: 0.6
    ├── Is Trigger: true
    └── Layer: "Hitbox"
```

### 3.7 Visual Root Setup

The visual child contains all rendered elements:

```csharp
/// <summary>
/// Manages the visual representation separate from simulation.
/// </summary>
public class VisualController : MonoBehaviour
{
    [SerializeField] private Transform modelRoot;
    [SerializeField] private Animator animator;
    [SerializeField] private SkinnedMeshRenderer meshRenderer;

    private Vector3 _targetPosition;
    private Quaternion _targetRotation;
    private float _smoothSpeed = 15f;

    /// <summary>
    /// Updates visual position with smoothing.
    /// </summary>
    public void SetTargetPose(Vector3 position, Quaternion rotation)
    {
        _targetPosition = position;
        _targetRotation = rotation;
    }

    private void LateUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, _targetPosition,
                                           _smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation,
                                               _smoothSpeed * Time.deltaTime);
    }
}
```

### 3.8 Component Activation by Role

Different components activate based on network role:

| Component | Server | Owner Client | Non-Owner Client |
|-----------|--------|--------------|------------------|
| `NetworkObject` | ✓ Active | ✓ Active | ✓ Active |
| `NavMeshAgent` | ✓ Enabled | ✓ Enabled (prediction) | ✗ Disabled |
| `Rigidbody` | ✓ Kinematic | ✓ Kinematic | ✓ Kinematic |
| `Movement` | ✓ Server logic | ✓ Prediction + reconcile | ✓ Interpolation only |
| `CapsuleCollider` | ✓ Active | ✓ Active | ✓ Active |
| `Hitbox` colliders | ✓ Active (lag comp) | ✗ Disabled | ✗ Disabled |
| `VisualController` | ✗ Optional | ✓ Smoothing | ✓ Interpolation |
| `Animator` | ✓ NetworkAnimator | ✓ Local + sync | ✓ Synced state |
| `PlayerInput` | ✗ N/A | ✓ Active | ✗ Disabled |

### 3.9 Layer and Tag Configuration

```
Layers:
├── Player (8)         - Player physics colliders
├── Hitbox (9)         - Damage detection triggers
├── Projectile (10)    - Ability projectiles
├── Ground (11)        - Walkable surfaces
└── Interactable (12)  - Interactive objects

Tags:
├── Player             - Player root objects
├── LocalPlayer        - Owner's player (set at runtime)
└── Hitbox             - Hitbox colliders

Physics Matrix:
├── Player ↔ Ground: Collide
├── Player ↔ Player: Collide (pushing)
├── Hitbox ↔ Projectile: Collide (trigger)
├── Hitbox ↔ Hitbox: Ignore
└── Player ↔ Hitbox: Ignore
```

### 3.10 Complete Prefab Component Checklist

```
PlayerEntity (Root)
├── Components:
│   ├── NetworkObject
│   ├── Rigidbody (Kinematic)
│   ├── CapsuleCollider
│   ├── NavMeshAgent
│   ├── Movement (NetworkBehaviour)
│   ├── GlobalStatSource
│   ├── StatsModifierManager
│   ├── AnimationHandler
│   ├── Health (NetworkBehaviour)
│   ├── AbilityCaster
│   ├── PositionHistory (server lag compensation)
│   └── InterpolationBuffer (client non-owner)
│
├── [Simulation] (Empty GameObject)
│   └── HitboxRoot
│       ├── Hitbox (Script) + SphereCollider (Head)
│       ├── Hitbox (Script) + CapsuleCollider (Body)
│       └── Hitbox (Script) + CapsuleCollider (Legs)
│
├── [Visual] (VisualController Script)
│   ├── Model
│   │   ├── Animator
│   │   ├── NetworkAnimator
│   │   └── SkinnedMeshRenderer
│   └── VFX_Root
│
└── [UI]
    ├── WorldSpaceCanvas
    ├── Nameplate (TextMeshPro)
    └── HealthBar (Slider/Image)
```

### 3.11 Prefab Variant Strategy

Use prefab variants for different character types:

```
Base: PlayerEntity.prefab
├── Variant: PlayerEntity_Warrior.prefab
│   └── Override: Model, Stats, Abilities
├── Variant: PlayerEntity_Mage.prefab
│   └── Override: Model, Stats, Abilities
└── Variant: PlayerEntity_Rogue.prefab
    └── Override: Model, Stats, Abilities
```

---

## 4. Client-Side Prediction

### Concept

The owning client **immediately simulates** movement locally without waiting for server confirmation. This eliminates perceived input lag.

### Implementation Details

#### 3.1 Input Capture & Sequencing

Every input is tagged with a **sequence number** for tracking:

```csharp
public struct MovementInput : INetworkSerializable
{
    public uint SequenceNumber;
    public float Timestamp;
    public Vector3 TargetPosition;      // For click-to-move
    public Vector3 MoveDirection;       // For WASD/joystick
    public MovementInputType InputType; // ClickToMove, Directional, Dash
}
```

#### 3.2 Local Prediction Loop

```
1. Capture input (mouse click / WASD)
2. Assign sequence number (incrementing counter)
3. Store input in pending buffer
4. Apply input to LOCAL NavMeshAgent immediately
5. Send input to server via ServerRpc
6. Render predicted position
```

#### 3.3 Prediction Rules

| Input Type | Prediction Method |
|------------|-------------------|
| Click-to-move | `NavMeshAgent.SetDestination()` locally |
| Directional | `NavMeshAgent.Move(direction * speed * dt)` |
| Dash | Execute full dash routine locally |
| Stop | `NavMeshAgent.ResetPath()` immediately |

#### 3.4 Pending Input Buffer

Maintain a circular buffer of unacknowledged inputs:

```csharp
private Queue<MovementInput> _pendingInputs = new Queue<MovementInput>();
private uint _nextSequenceNumber = 0;
```

---

## 5. Server Reconciliation

### Concept

When the server responds with authoritative state, the client:
1. Snaps to server position
2. Replays all inputs the server hasn't processed yet
3. Arrives at corrected predicted position

### Implementation Details

#### 5.1 Server State Message

```csharp
public struct MovementState : INetworkSerializable
{
    public uint LastProcessedSequence;  // Which input was last processed
    public float ServerTimestamp;
    public Vector3 Position;
    public Vector3 Velocity;
    public Quaternion Rotation;
    public bool IsMoving;
}
```

#### 5.2 Reconciliation Algorithm

```
OnServerStateReceived(state):
    1. Remove all inputs from pendingInputs where seq <= state.LastProcessedSequence
    2. If pendingInputs is empty:
         - Smoothly correct to server position (no replay needed)
    3. Else:
         - Save current visual position
         - Teleport simulation to server position
         - For each remaining input in pendingInputs:
             - Re-apply input to local NavMeshAgent
         - Calculate correction delta
         - Apply smooth visual correction over time
```

#### 5.3 Correction Smoothing

To avoid jarring snaps when prediction error is small:

```csharp
private Vector3 _correctionOffset = Vector3.zero;
private const float CORRECTION_SMOOTHING_RATE = 10f;
private const float SNAP_THRESHOLD = 2f; // Teleport if error > 2m

void ApplyCorrection(Vector3 serverPosition, Vector3 predictedPosition)
{
    Vector3 error = serverPosition - predictedPosition;

    if (error.magnitude > SNAP_THRESHOLD)
    {
        // Large error: snap immediately (likely teleport/respawn)
        transform.position = serverPosition;
        _correctionOffset = Vector3.zero;
    }
    else
    {
        // Small error: smooth correction
        _correctionOffset += error;
    }
}

void Update()
{
    // Gradually reduce correction offset
    _correctionOffset = Vector3.Lerp(_correctionOffset, Vector3.zero,
                                      CORRECTION_SMOOTHING_RATE * Time.deltaTime);

    // Visual position = simulation position + remaining correction
    _visualTransform.position = _simulationPosition + _correctionOffset;
}
```

#### 5.4 Server Processing

```
OnInputReceived(clientId, input):
    1. Validate input (anti-cheat: speed limits, NavMesh bounds)
    2. Apply to server-side NavMeshAgent
    3. Record in input history (for lag compensation)
    4. Broadcast updated state to all clients
```

---

## 6. Entity Interpolation

### Concept

Non-owner clients render other players **in the past** using buffered position snapshots. This provides smooth visuals without prediction artifacts.

### Implementation Details

#### 6.1 Interpolation Buffer

```csharp
public class InterpolationBuffer
{
    private readonly List<PositionSnapshot> _snapshots = new List<PositionSnapshot>();
    private const float INTERPOLATION_DELAY = 0.1f; // 100ms behind

    public struct PositionSnapshot
    {
        public float Timestamp;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
    }
}
```

#### 6.2 Interpolation Algorithm

```
RenderTime = CurrentTime - INTERPOLATION_DELAY

Find two snapshots bracketing RenderTime:
    - snapshot[i].Timestamp <= RenderTime
    - snapshot[i+1].Timestamp > RenderTime

Calculate interpolation factor:
    t = (RenderTime - snapshot[i].Timestamp) /
        (snapshot[i+1].Timestamp - snapshot[i].Timestamp)

Interpolated position:
    position = Vector3.Lerp(snapshot[i].Position, snapshot[i+1].Position, t)
    rotation = Quaternion.Slerp(snapshot[i].Rotation, snapshot[i+1].Rotation, t)
```

#### 6.3 Handling Edge Cases

| Scenario | Solution |
|----------|----------|
| No future snapshot | Extrapolate using last velocity (with limits) |
| Large gap between snapshots | Teleport rather than interpolate |
| Jitter in network | Adaptive delay based on packet variance |

#### 6.4 Visual Separation

Separate **simulation** and **visual** representations:

```csharp
// Simulation (physics, collision)
private Vector3 _simulationPosition;

// Visual (what players see)
[SerializeField] private Transform _visualRoot;

void LateUpdate()
{
    if (IsOwner)
    {
        // Owner: visual follows prediction with correction smoothing
        _visualRoot.position = _simulationPosition + _correctionOffset;
    }
    else
    {
        // Non-owner: visual follows interpolation
        _visualRoot.position = _interpolationBuffer.GetInterpolatedPosition();
    }
}
```

---

## 7. Lag Compensation

### Concept

For hit detection and abilities, the server **rewinds time** to evaluate actions from the player's perspective at the moment they acted.

### Implementation Details

#### 7.1 Server-Side Position History

```csharp
public class PositionHistory
{
    private readonly CircularBuffer<HistoryEntry> _history;
    private const float HISTORY_DURATION = 1f; // Keep 1 second of history

    public struct HistoryEntry
    {
        public float Timestamp;
        public Vector3 Position;
        public Bounds HitboxBounds;
    }

    public HistoryEntry GetStateAtTime(float timestamp)
    {
        // Find and interpolate between history entries
    }
}
```

#### 7.2 Lag Compensation Flow

```
Client fires ability:
    1. Client sends: { AbilityId, TargetPosition, ClientTimestamp }

Server processes:
    1. Calculate client's perceived time = ServerTime - ClientRTT/2
    2. Rewind all relevant entities to that time
    3. Perform hit detection against rewound positions
    4. Apply damage/effects
    5. Restore current positions
    6. Broadcast results
```

#### 7.3 Integration with Ability System

The existing ability system should include timestamps:

```csharp
public struct AbilityCastContext : INetworkSerializable
{
    public ulong CasterNetworkObjectId;
    public Vector3 CastPosition;
    public Vector3 TargetPosition;
    public float ClientTimestamp;  // For lag compensation
    public uint InputSequence;     // Link to movement state
}
```

---

## 8. Implementation Plan

### Phase 1: Core Infrastructure

| Task | Description | Files |
|------|-------------|-------|
| 1.1 | Create `MovementInput` network struct | `Network/MovementInput.cs` |
| 1.2 | Create `MovementState` network struct | `Network/MovementState.cs` |
| 1.3 | Create `InterpolationBuffer` class | `Network/InterpolationBuffer.cs` |
| 1.4 | Create `PositionHistory` for lag comp | `Network/PositionHistory.cs` |

### Phase 2: Client-Side Prediction

| Task | Description | Files |
|------|-------------|-------|
| 2.1 | Add input sequencing to Movement | `Entity/Movement.cs` |
| 2.2 | Implement pending input buffer | `Entity/Movement.cs` |
| 2.3 | Enable local NavMeshAgent for owner | `Entity/Movement.cs` |
| 2.4 | Send inputs via Unreliable RPC | `Entity/Movement.cs` |

### Phase 3: Server Reconciliation

| Task | Description | Files |
|------|-------------|-------|
| 3.1 | Server input processing queue | `Network/MovementServer.cs` |
| 3.2 | Server state broadcast | `Network/MovementServer.cs` |
| 3.3 | Client reconciliation logic | `Entity/Movement.cs` |
| 3.4 | Correction smoothing | `Entity/Movement.cs` |

### Phase 4: Entity Interpolation

| Task | Description | Files |
|------|-------------|-------|
| 4.1 | Separate visual from simulation | `Entity/Movement.cs` |
| 4.2 | Implement interpolation buffer | `Network/InterpolationBuffer.cs` |
| 4.3 | Non-owner rendering pipeline | `Entity/Movement.cs` |
| 4.4 | Adaptive delay calculation | `Network/InterpolationBuffer.cs` |

### Phase 5: Lag Compensation

| Task | Description | Files |
|------|-------------|-------|
| 5.1 | Server position history recording | `Network/PositionHistory.cs` |
| 5.2 | Time rewinding system | `Network/LagCompensation.cs` |
| 5.3 | Integrate with ability system | `Ability/AbilityCaster.cs` |
| 5.4 | Hitbox rewinding | `Hitbox/Hitbox.cs` |

### Phase 6: Polish & Edge Cases

| Task | Description | Files |
|------|-------------|-------|
| 6.1 | Handle dash prediction | `Entity/Movement.cs` |
| 6.2 | Movement speed modifier sync | `Entity/Movement.cs` |
| 6.3 | Disable/Enable movement states | `Entity/Movement.cs` |
| 6.4 | Network diagnostics overlay | `UI/NetworkDebugView.cs` |

---

## 9. Data Structures

### MovementInput.cs

```csharp
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Represents a single movement input sent from client to server.
/// </summary>
public struct MovementInput : INetworkSerializable
{
    public uint SequenceNumber;
    public float Timestamp;
    public Vector3 TargetPosition;
    public Vector3 MoveDirection;
    public MovementInputType InputType;
    public DashInputData DashData;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SequenceNumber);
        serializer.SerializeValue(ref Timestamp);
        serializer.SerializeValue(ref TargetPosition);
        serializer.SerializeValue(ref MoveDirection);
        serializer.SerializeValue(ref InputType);
        serializer.SerializeValue(ref DashData);
    }
}

public enum MovementInputType : byte
{
    None = 0,
    ClickToMove = 1,
    Directional = 2,
    Dash = 3,
    Stop = 4
}

public struct DashInputData : INetworkSerializable
{
    public Vector3 Direction;
    public float Distance;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Direction);
        serializer.SerializeValue(ref Distance);
    }
}
```

### MovementState.cs

```csharp
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Authoritative movement state broadcast from server to clients.
/// </summary>
public struct MovementState : INetworkSerializable
{
    public uint LastProcessedSequence;
    public float ServerTimestamp;
    public Vector3 Position;
    public Vector3 Velocity;
    public Quaternion Rotation;
    public bool IsMoving;
    public bool CanMove;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref LastProcessedSequence);
        serializer.SerializeValue(ref ServerTimestamp);
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref Velocity);
        serializer.SerializeValue(ref Rotation);
        serializer.SerializeValue(ref IsMoving);
        serializer.SerializeValue(ref CanMove);
    }
}
```

---

## 10. Network Messages

### Message Flow Diagram

```
CLIENT (Owner)                         SERVER                         CLIENT (Other)
     │                                    │                                  │
     │──── MovementInputRpc ─────────────→│                                  │
     │     (unreliable, high freq)        │                                  │
     │                                    │                                  │
     │                                    │──── MovementStateRpc ───────────→│
     │←─── MovementStateRpc ──────────────│     (unreliable, tick rate)      │
     │     (unreliable, tick rate)        │                                  │
     │                                    │                                  │
```

### RPC Definitions

```csharp
// Client → Server: Send input (unreliable for speed)
[Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
private void SendMovementInputServerRpc(MovementInput input);

// Server → All Clients: Broadcast state (unreliable)
[Rpc(SendTo.Everyone, Delivery = RpcDelivery.Unreliable)]
private void BroadcastMovementStateClientRpc(MovementState state);

// Server → Specific Client: Force correction (reliable)
[Rpc(SendTo.SpecifiedInParams, Delivery = RpcDelivery.Reliable)]
private void ForcePositionCorrectionClientRpc(Vector3 position, RpcParams rpcParams);
```

### Tick Rate Considerations

| Setting | Recommended Value | Reason |
|---------|-------------------|--------|
| Client input send rate | 60 Hz | Match client frame rate |
| Server simulation tick | 30-60 Hz | Balance accuracy vs. CPU |
| State broadcast rate | 20-30 Hz | Bandwidth optimization |
| Interpolation delay | 100-150 ms | Cover 3-4 state updates |

---

## 11. Edge Cases & Error Handling

### 11.1 Network Disconnection

```csharp
void OnClientDisconnected()
{
    // Clear pending inputs
    _pendingInputs.Clear();

    // Stop all movement
    StopMovement();

    // Disable prediction
    _isPredicting = false;
}
```

### 11.2 Excessive Prediction Error

When server repeatedly corrects by large amounts:

```csharp
private int _consecutiveLargeCorrections = 0;
private const int MAX_LARGE_CORRECTIONS = 5;

void OnReconcile(float errorMagnitude)
{
    if (errorMagnitude > LARGE_ERROR_THRESHOLD)
    {
        _consecutiveLargeCorrections++;

        if (_consecutiveLargeCorrections > MAX_LARGE_CORRECTIONS)
        {
            // Likely cheating or severe desync - force full resync
            RequestFullStateSync();
            _consecutiveLargeCorrections = 0;
        }
    }
    else
    {
        _consecutiveLargeCorrections = 0;
    }
}
```

### 11.3 Sequence Number Overflow

```csharp
// Use uint (4 billion values) and handle wrap-around
private bool IsSequenceNewer(uint a, uint b)
{
    // Handle wrap-around: if difference > half max value, older wrapped
    return (a > b) && (a - b < uint.MaxValue / 2) ||
           (b > a) && (b - a > uint.MaxValue / 2);
}
```

### 11.4 Dash During Reconciliation

```csharp
// Dash must be replayed atomically
void ReplayInput(MovementInput input)
{
    switch (input.InputType)
    {
        case MovementInputType.Dash:
            // Replay dash positions instantly (no coroutine)
            Vector3 dashEnd = CalculateDashEndPosition(input.DashData);
            _simulationPosition = dashEnd;
            break;

        case MovementInputType.ClickToMove:
            // Simulate NavMesh path progress
            SimulatePathProgress(input.TargetPosition, timeSinceInput);
            break;
    }
}
```

### 11.5 Movement State Conflicts

When movement is disabled server-side but client predicted movement:

```csharp
void OnServerStateReceived(MovementState state)
{
    if (!state.CanMove && _localCanMove)
    {
        // Server disabled movement - cancel all pending
        _pendingInputs.Clear();
        StopLocalMovement();
    }

    _localCanMove = state.CanMove;
}
```

---

## References

- [Gabriel Gambetta: Fast-Paced Multiplayer](https://www.gabrielgambetta.com/client-server-game-architecture.html)
- [Unity Netcode for GameObjects Documentation](https://docs-multiplayer.unity3d.com/)
- [Valve: Source Multiplayer Networking](https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking)
- [Overwatch GDC: Netcode](https://www.youtube.com/watch?v=vTH2ZPgYujQ)

---

## Summary

This plan transforms the current simple authoritative model into a responsive client-predicted system while maintaining server authority. Key components:

1. **Client-Side Prediction**: Immediate local response to inputs
2. **Server Reconciliation**: Correction when prediction diverges
3. **Entity Interpolation**: Smooth rendering of other players
4. **Lag Compensation**: Fair hit detection despite latency

The implementation is phased to allow incremental testing and validation at each stage.
