# Floating Damage Numbers — Implementation Plan

## Context

We need screen-space floating damage numbers displayed when any `IDamageable` takes damage. A **ScriptableObject Event Bus** will serve as reusable infrastructure for future features (damage recaps, combat logs, analytics, etc.). **Screen-space rendering** (not world-space) is chosen for performance with many simultaneous numbers. The game is multiplayer (Unity Netcode) — damage numbers display on all clients via the already-synced `Health.On_Change` event.

## Data Flow

```
Health.On_Change (fires on ALL clients after NetworkVariable sync)
  -> DamageEventRaiser (per-character component, computes delta + position)
    -> DamageEventChannel (ScriptableObject asset, broadcasts to all subscribers)
      -> DamageNumberSpawner (singleton, screen-space canvas)
        -> DamageNumber (pooled via PoolManager, DOTween animation, auto-release)
```

---

## Phase 1 — ScriptableObject Event Bus Infrastructure

### File 1: `Script/Events/GameEventChannel.cs` (NEW)

Generic reusable SO event channel base class.

- `abstract class GameEventChannel<T> : ScriptableObject`
- `List<UnityAction<T>>` listener list
- `Subscribe(UnityAction<T>)` / `Unsubscribe(UnityAction<T>)` / `Raise(T)`
- Clears listeners in `OnDisable()` to prevent leaks across play sessions

### File 2: `Script/Events/DamageEventData.cs` (NEW)

Immutable data struct for damage events.

```
struct DamageEventData
  int DamageAmount
  Vector3 WorldPosition
  Character Target
  float Timestamp        <- for future damage recap/analytics
  bool IsHeal            <- true when health increases
```

Struct (no GC pressure). Constructor for all fields.

### File 3: `Script/Events/DamageEventChannel.cs` (NEW)

Concrete channel: `DamageEventChannel : GameEventChannel<DamageEventData>` with `[CreateAssetMenu]`. No extra logic — just type specialization. A SO asset will be created in the project for wiring.

---

## Phase 2 — Raising Damage Events

### File 4: `Script/Hitbox/DamageEventRaiser.cs` (NEW)

Component placed on each Character prefab alongside `Health`.

- Serialized fields: `Health health`, `Character character`, `DamageEventChannel channel`
- `OnEnable`: subscribes to `Health.On_Change`
- `OnDisable`: unsubscribes
- On change: computes `delta = previousValue - newValue`, builds `DamageEventData` with `character.transform.position`, raises on channel
- Handles both damage (delta > 0) and healing (delta < 0)

---

## Phase 3 — Screen-Space Damage Number Display

### File 5: `Script/UI/Floating/DamageNumberSpawner.cs` (NEW)

Lives in the scene as a child RectTransform under the UIManager's canvas (sibling to ScreenLayer, PopupLayer, etc. — sorted last so it renders on top).

- Serialized: `DamageEventChannel channel`, `GameObject damageNumberPrefab`, `RectTransform canvasRect`, `Camera mainCamera`
- `OnEnable`/`OnDisable`: subscribe/unsubscribe to channel
- On event: world-to-screen via `RectTransformUtility.WorldToScreenPoint` then `ScreenPointToLocalPointInRectangle` (same pattern as existing `TooltipManager`)
- Adds small random offset to prevent stacking
- Spawns via `PoolManager.Provide<DamageNumber>(prefab, Vector3.zero, Quaternion.identity, canvasRect)` then sets `anchoredPosition` (PoolManager positions in world space, we reposition in canvas space after)

### File 6: `Script/UI/Floating/DamageNumber.cs` (NEW)

Pooled UI element (prefab: RectTransform + CanvasGroup + TMP_Text + DamageNumber).

- Serialized: `TMP_Text text`, `CanvasGroup canvasGroup`, animation settings (floatDistance, duration, curves, colors for damage/heal/crit)
- `Initialize(DamageEventData, Vector2 canvasPosition)`: sets text, color, position, starts DOTween Sequence
- Animation: float up (anchoredPosition.y += floatDistance), fade out (canvasGroup.alpha -> 0), scale punch
- `OnComplete` -> kills tweens -> `PoolManager.Release(gameObject)`

---

## Files Summary

| # | Path | Action |
|---|------|--------|
| 1 | `Script/Events/GameEventChannel.cs` | Create |
| 2 | `Script/Events/DamageEventData.cs` | Create |
| 3 | `Script/Events/DamageEventChannel.cs` | Create |
| 4 | `Script/Hitbox/DamageEventRaiser.cs` | Create |
| 5 | `Script/UI/Floating/DamageNumberSpawner.cs` | Create |
| 6 | `Script/UI/Floating/DamageNumber.cs` | Create |

No existing files need modification — all integration is via SO asset wiring in the Inspector.

## Unity Editor Setup (Manual, after code)

1. Create SO asset: Right-click -> Create -> Events -> Damage Event Channel
2. Create DamageNumber prefab: RectTransform + CanvasGroup + TMP_Text + DamageNumber component
3. Add `DamageNumberSpawner` as a child RectTransform under the main UI canvas (sibling to layer roots, last in hierarchy)
4. Add `DamageEventRaiser` component to Character prefab, wire Health + Character + Channel SO
5. Wire Channel SO + prefab + canvas references on DamageNumberSpawner

## Verification

- Enter Play mode, deal damage to a character
- Damage numbers should appear at the character's screen-projected position
- Numbers float up, fade out, then disappear (returned to pool)
- Healing should show in a different color
- Multiple simultaneous hits should show separate numbers with slight random offsets
- Check PoolManager hierarchy to verify objects are being recycled
