# DeckBuilder Module Organization Proposal

This plan groups the DeckBuilder scripts into modular assemblies with clear feature ownership, predictable dependencies, and separate editor/test code. Use it as a map for relocating assets and creating `.asmdef` files.

## Goals
- Faster iteration by compiling only the systems you touch.
- Safer dependencies via feature-scoped assemblies and explicit references.
- Clear separation of runtime, editor, and test-only code.

## Runtime assemblies (layered)
- **DeckBuilder.Core** – Common primitives, constants, math helpers, serialization utilities, generic services, and shared ScriptableObjects.
- **DeckBuilder.Framework** – Game state flow, save/load pipeline, scene bootstrap, service locator/context, and application-level events. Depends on `DeckBuilder.Core`.
- **DeckBuilder.Entity** – Actors/units, stats, team/faction data, health/shield, movement, hitboxes, and targeting helpers. Depends on `DeckBuilder.Framework`.
- **DeckBuilder.Ability** – Ability definitions, behaviours, modifiers, cooldowns, targeting rules, projectile launchers, and range indicators. Depends on `DeckBuilder.Entity`.
- **DeckBuilder.Combat** – Turn/round management, sequencing, combat resolution, and damage application that orchestrates entities and abilities. Depends on `DeckBuilder.Ability`.
- **DeckBuilder.Input** – Player input bindings, input handlers, and gameplay command routing. Depends on `DeckBuilder.Framework`.
- **DeckBuilder.UI.Core** – UI service layer (screen stack, navigation, modal flow), shared presenters/views, UI utilities, and common styling hooks. Depends on `DeckBuilder.Framework`.
- **DeckBuilder.UI.HUD** – In-game HUD widgets (health bars, ability tray, mana/energy display, tooltips). Depends on `DeckBuilder.UI.Core` and `DeckBuilder.Combat` for data.
- **DeckBuilder.UI.Screens** – Menus, deckbuilder screen, loadouts, meta-progression panels, and dialogs. Depends on `DeckBuilder.UI.Core` and whichever feature assemblies supply data (e.g., `DeckBuilder.Ability`).
- **DeckBuilder.UI.Tooltips** – Tooltip models/renderers, hover detectors, and rich text formatters. Depends on `DeckBuilder.UI.Core`.
- **DeckBuilder.Utils** – One-off helpers that are feature-neutral (debug aids, extension methods). Keep references minimal; only higher layers should depend on this if unavoidable.

## Editor assemblies
- Mirror runtime naming per feature (e.g., `DeckBuilder.Ability.Editor`, `DeckBuilder.UI.Core.Editor`). Place them under `Assets/_DeckBuilder/Editor/<Feature>/`.
- Restrict platforms to **Editor** in `.asmdef` and reference only the paired runtime assembly (and Unity editor packages as needed).

## Test assemblies
- Place play-mode/unit tests beside the feature under `Assets/_DeckBuilder/Tests/<Feature>/` with `.asmdef` named `DeckBuilder.<Feature>.Tests` that reference the corresponding runtime assembly.
- Keep editor tests under `Assets/_DeckBuilder/EditorTests/<Feature>/` to avoid leaking editor-only dependencies into runtime.

## Suggested folder layout
```
Assets/
  _DeckBuilder/
    Script/
      Core/
      Framework/
      Entity/
      Ability/
      Combat/
      Input/
      UI/
        Core/
        HUD/
        Screens/
        Tooltips/
      Utils/
    Editor/
      Core/
      Framework/
      Entity/
      Ability/
      Combat/
      Input/
      UI/
        Core/
        HUD/
        Screens/
        Tooltips/
      Utils/
    Tests/
      Core/
      Framework/
      Entity/
      Ability/
      Combat/
      Input/
      UI/
        Core/
        HUD/
        Screens/
        Tooltips/
      Utils/
```

## Dependency rules
- Lower-level assemblies (`Core`, `Framework`) **never** depend on feature layers (Ability, UI, Combat).
- UI assemblies depend on data-only surfaces of gameplay assemblies (e.g., read-only interfaces or DTOs) to avoid circular references.
- Keep `Utils` isolated; if something gains feature meaning, promote it into the owning module instead of keeping it generic.

## Implementation task
Follow this task to realize the module split with `.asmdef` boundaries and aligned code dependencies.

1) Create runtime `.asmdef` files for each feature folder (`Core`, `Framework`, `Entity`, `Ability`, `Combat`, `Input`, `UI/Core`, `UI/HUD`, `UI/Screens`, `UI/Tooltips`, `Utils`). Point references downward only (e.g., `Framework` → `Core`, `Ability` → `Entity`, `UI.HUD` → `UI.Core` + data suppliers).
2) Create matching editor assemblies under `Assets/_DeckBuilder/Editor/<Feature>/` (e.g., `DeckBuilder.Ability.Editor`) that depend solely on their runtime counterparts and are limited to the Editor platform.
3) Create matching test assemblies under `Assets/_DeckBuilder/Tests/<Feature>/` (e.g., `DeckBuilder.Combat.Tests`) that reference only the runtime feature and required third-party test libraries.
4) Move scripts into the new feature folders, updating namespaces to mirror folder names. Refactor shared utilities into `DeckBuilder.Core` if they are cross-cutting; move feature-specific helpers into their owning module to avoid upward dependencies.
5) Update inter-feature calls to respect the dependency rules (e.g., UI reads from data interfaces exposed by gameplay assemblies; no gameplay code depends on UI; `Core` never references higher layers). Add adapter interfaces if needed to keep references pointing downward.
6) Delete or archive the monolithic `DeckBuilder.asmdef` after all files are associated with new assemblies, then fix any remaining assembly reference warnings.

## Migration notes
- Move existing `.asmdef` files into their matching folders and update assembly references accordingly.
- Adjust namespaces to mirror folder structure (e.g., `DeckBuilder.Ability.Projectiles`).
- When in doubt, place shared interfaces in `Core` and concrete gameplay in the relevant feature module.
