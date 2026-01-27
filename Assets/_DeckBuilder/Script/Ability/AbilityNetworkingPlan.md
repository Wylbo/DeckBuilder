# Ability Networking Plan

## Milestone 1: Establish Server Authority for Casting
- Goal: Make ability casting originate from the server and prevent client-side cheats.
- Actions: Promote `AbilityCaster` to a network-aware orchestrator; add RPC entrypoints for cast/start-hold/end-hold; validate slot ownership, cooldown, resources, and tags on the server before executing.

## Milestone 2: Sync Slot State to Clients
- Goal: Ensure UI reflects server-truth for cooldowns and equipped abilities.
- Actions: Track per-slot cooldown end times and casting flags in network variables; replicate equipped ability identifiers; update `SpellSlotUI` to read networked state instead of local timers.

## Milestone 3: Split Gameplay vs Presentation
- Goal: Keep authoritative simulation server-side while clients drive visuals only.
- Actions: Run damage, debuffs, movement locks, and projectile spawning on the server; emit client RPCs for VFX/animation cues; create a client-side presenter that mirrors cast start/end and aim without mutating gameplay.

## Milestone 4: Network-Friendly Cast Context
- Goal: Remove non-serializable references from cast data.
- Actions: Replace direct component refs (`Targetable`, `Component[]`) with network IDs/struct payloads; send aim/target positions from the requesting client; avoid camera/input usage on the server.

## Milestone 5: Networked Projectiles and Damage
- Goal: Resolve hits authoritatively and keep visuals in sync.
- Actions: Spawn gameplay projectiles as `NetworkObject`s (or server-only physics) and use `NetworkObjectPool`; have `Hitbox`/damage resolution run server-side; broadcast damage results or health deltas to clients.

## Milestone 6: Debuffs and Modifiers Replication
- Goal: Keep stat effects consistent across clients.
- Actions: Apply debuffs/modifiers on the server; expose debuff state and global/ability stat deltas through `NetworkList`/`NetworkVariable` for HUD; reevaluate cooldowns/stats using server values.

## Milestone 7: Animations and Movement Locks
- Goal: Align character presentation with server decisions.
- Actions: When server starts/ends casts, send animation clip ids and rotation/lock data via RPC; unlock movement on cast end; allow client-side prediction of VFX with reconciliation hooks.

## Milestone 8: Testing and Rollout
- Goal: De-risk the migration.
- Actions: Start with a single ability end-to-end; add debug overlays for server vs client cooldown and cast state; progressively convert behaviours; validate in Unity multiplayer playmode sessions.
