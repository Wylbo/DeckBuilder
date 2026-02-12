---
name: new-ability
description: Scaffold a new Ability ScriptableObject and optional AbilityBehaviour
---

Create a new ability named `$ARGUMENTS` following the project conventions.

## Steps

1. **Read existing patterns** to match the project style:
   - Read `Assets/_DeckBuilder/Script/Ability/Ability.cs` for the ScriptableObject pattern
   - Read one file in `Assets/_DeckBuilder/Script/Ability/AbilityBehaviours/` for the behaviour pattern

2. **Create the AbilityBehaviour** (if custom logic is needed):
   - Create `Assets/_DeckBuilder/Script/Ability/AbilityBehaviours/{AbilityName}Behaviour.cs`
   - Use `[Serializable]` attribute
   - Inherit from `AbilityBehaviour`
   - Override only the relevant virtual methods
   - Follow all code-style rules from `.claude/rules/code-style.md`

3. **Remind the user** to:
   - Create the `Ability` ScriptableObject asset in the Unity Editor via `Create > Ability`
   - Assign the new behaviour in the Inspector via `[SerializeReference]`
   - Register any new tags in `GTagRegistry` if applicable
