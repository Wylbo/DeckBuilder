# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Networked hack-and-slash game built in **Unity 6000.2.13f1** with **Unity Netcode for GameObjects**.
All game code lives under `Assets/_DeckBuilder/Script/`. No custom assembly definitions — everything compiles in the default assembly.

**Do not run `dotnet` CLI commands.** Testing is done directly inside the Unity engine.

## Rules & Tools

- Code style, documentation, naming, script structure: `.claude/rules/code-style.md`
- Anti-patterns: `.claude/rules/anti-patterns.md`
- Skills: `/new-ability [Name]`, `/new-ui-screen [Name]`

---

## Architecture

### Dependency Wiring

No DI container. Dependencies are wired via:
- **Serialized fields** on MonoBehaviours (Inspector references)
- **Constructor injection** for plain C# classes (AbilityExecutor, AbilityStatProvider, handlers)
- **GetComponent<>()** cached in `Reset()` or `Awake()`

### Network Authority Model

Server-authoritative with client prediction. Key pattern:
- **Owner** sends input via `[ServerRpc]` → server validates → broadcasts via `[ClientRpc]`
- **Movement**: client predicts locally, server reconciles (MovementPredictionHandler / MovementReconciliationHandler)
- **Abilities**: client requests cast → server validates resources/cooldowns/tags → broadcasts result
- **Health**: server-only mutation via NetworkVariable<int>
- Non-owners interpolate from server snapshots (InterpolationBuffer, MovementVisualHandler)

### Ability System (end-to-end flow)

```
Input → AbilityCaster.RequestCast_ServerRpc()
      → SpellSlot.Cast() (cooldown, stat resolution)
      → AbilityExecutor.Cast() (plain C# orchestrator)
      → AbilityBehaviour.OnCastStarted() (serialized polymorphic effects)
      → NotifyCastStarted_ClientRpc() (animation, rotation on all clients)
```

- **Ability** (ScriptableObject): holds icon, stats, behaviours list (`[SerializeReference]`), debuffs, tags, animation data
- **AbilityBehaviour** (abstract serializable): lifecycle hooks — `OnCastStarted`, `OnCastUpdated`, `OnCastEnded`, `OnHoldEnded`
- **AbilityExecutor** (plain C#): runs behaviour list, manages cast state via `RequiresUpdate` / `BlocksAbilityEnd` flags
- **AbilityBehaviourContext**: bundles all dependencies (caster, movement, animation, projectiles, modifiers, stats)
- **AbilityCastContext**: runtime cast data (target point, aim, hold state, direction)

### Stat System

Two-layer stat evaluation:
1. **GlobalStatSource** (MonoBehaviour): base character stats + GlobalModifiers → evaluated via `EvaluateGlobalStats()`
2. **AbilityStatProvider** (plain C#): ability base stats (Flat / RatioToGlobal / CopyGlobal) + AbilityModifiers → evaluated via `EvaluateStats()`

StatsModifierManager holds active modifiers and fires `OnAnyModifiersChanged` — Movement listens to refresh speed, abilities re-evaluate on cast.

### Entity / Character Composition

```
Character (NetworkBehaviour)
├── CharacterVisual       # dissolve, visual effects
├── Movement              # NavMeshAgent + client prediction + dash
├── AbilityCaster         # 4 spell slots + dodge slot
├── Health                # NetworkVariable<int>
├── Hurtbox               # IDamageable → Character.TakeDamage() → Health
├── AnimationHandler      # animation state management
└── Faction               # IFactionOwner for hostility checks
```

**Controller** (NetworkBehaviour) owns a **ControlStrategy** (ScriptableObject):
- `PlayerControlStrategy`: composes InputProvider, MouseRaycaster, ClickMoveHandler, AbilityInputHandler, UI toggles
- `BaseEnemyControlStrategy`: drives AI via behaviour tree
- `DummyControlStrategy`: no-op for test entities

### UI Framework

```
IUIManager.Show<TView>()
  → IUIViewFactory.GetOrCreate<TView>() (instantiate + cache)
  → IUILayerController (parent to layer root, manage render order)
  → UIView.ShowInternal() → OnShow() (virtual hook)
```

- **UIView** (abstract MonoBehaviour): base for all screens/popups/HUD. Properties: Layer, IsVisible, Owner (IUIManager)
- **UIViewFactory** (plain C#): type-keyed dictionary of prefabs → instances
- **Layers**: Screen, Popup, HUD — each with configurable show/hide behaviour
- **IUIHistoryTracker**: view stack for back-button navigation
- **IPauseService**: views can auto-pause game on show

### Damage Flow

```
Hitbox trigger → Hurtbox.TakeDamage(DamageInstance)
  → On_DamageReceived event
  → Character.TakeDamage()
  → Health.AddOrRemoveHealth() [ServerRpc]
  → Health.On_Empty → Character.Die()
```

### AI / Behaviour Trees

BT nodes in `Script/BT/Node/`:
- **Actions**: Chase, Patrol, UpdateSensors, UseAbility
- **Conditions**: HasSensedTarget, IsAlive
- **SensorManager**: ticks ISensor list, provides `HasSensedTarget(out GameObject)`

---

## Key Design Patterns

| Pattern | Where | Example |
|---------|-------|---------|
| Strategy | Entity control | ControlStrategy → PlayerControlStrategy / BaseEnemyControlStrategy |
| Composition | Abilities | Ability SO holds list of AbilityBehaviour via `[SerializeReference]` |
| Presenter | Networking | AbilityCastPresenter handles client-side visuals from network events |
| Observer | Cross-system | UnityAction events, NetworkVariable.OnValueChanged |
| Context Object | Ability execution | AbilityBehaviourContext / AbilityCastContext bundle dependencies |

## Key Interfaces (System Boundaries)

| Interface | Purpose |
|-----------|---------|
| IUIManager | Show/hide/query UI views |
| IUIViewFactory | View instantiation and caching |
| IDamageable | Receive damage (Hurtbox) |
| IAbilityMovement | Movement control for abilities |
| IAbilityStatProvider | Stat resolution for abilities |
| IGlobalStatSource | Character-wide stat evaluation |
| IAbilityDebuffService | Debuff application |
| IFactionOwner | Faction-based hostility |
| ISensor | AI sensing contract |

---

## SOLID Principles (Mandatory)

- **Single Responsibility** — One class, one responsibility. Refactor immediately if violated.
- **Open / Closed** — Extend via composition and interfaces, not modification.
- **Liskov Substitution** — Derived types must be fully substitutable.
- **Interface Segregation** — Small, focused interfaces. Never force unused methods.
- **Dependency Inversion** — Depend on abstractions. Constructor injection for plain C#, serialized references for MonoBehaviours.

## Unity Rules

- MonoBehaviours: hold serialized references, receive Unity callbacks, coordinate flow
- Plain C# classes: business rules, calculations, state machines, domain logic
- Cache components during initialization — no runtime object searches
- No allocations in per-frame methods — event-driven over polling
- No singletons unless explicitly approved

---

Call me **Maximilien** if you read everything correctly at the end of each answer.
