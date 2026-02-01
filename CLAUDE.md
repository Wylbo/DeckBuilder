# Agent.md — C# & Unity Development Guidelines

This document defines mandatory coding standards, architectural rules, and best practices for Unity projects written in C#.

---

## Project Structure

```
Assets/_DeckBuilder/
├── Scenes/           # Unity scenes
└── Script/
    ├── Ability/      # Ability system, behaviours, modifiers, projectiles
    ├── BT/           # Behaviour tree nodes for AI
    ├── Camera/       # Camera controller and effects
    ├── Entity/       # Characters, movement, controllers, AI sensors
    ├── Framework/    # Game state management
    ├── Hitbox/       # Damage system (Health, Hitbox, Hurtbox)
    ├── Input/        # Input rebinding and player input
    ├── Network/      # Session management, networking components
    ├── UI/           # UI framework, views, HUD, tooltips
    └── Utils/        # Pooling, timers, helpers
```

**Networking:** Unity Netcode for GameObjects
**Key Patterns:** Behaviour trees for AI, Component-based abilities, UI framework with views/layers

---

## Common Workflows

### Adding a New Ability
1. Create `ScriptableObject` ability data in `Ability/`
2. Implement `AbilityBehaviour` if custom logic is needed
3. Add any required `AbilityModifier` components
4. Register tags in `GTagRegistry` if applicable

### Adding a New UI Screen
1. Create view class inheriting from `UIView` in `UI/Screen/`
2. Register in `UIViewFactory`
3. Use `IUIManager` to show/hide

### Adding Networked Behaviour
1. Inherit from `NetworkBehaviour`
2. Use `NetworkVariable<T>` for synced state
3. Use `[ServerRpc]` and `[ClientRpc]` for RPCs
4. Test with host and client builds

### Refactoring a Class
1. Identify all responsibilities
2. Extract each to a new class with single responsibility
3. Use interfaces for dependencies
4. Update consumers to use new APIs
5. Validate serialized references in `Awake()` or `OnEnable()`

---

## Anti-Patterns (Never Do)

- ❌ `var` — Always use explicit types
- ❌ Multiple responsibilities in one class
- ❌ Missing XML documentation on public members
- ❌ `FindObjectOfType` or `GameObject.Find` in production code
- ❌ Allocations in `Update()`, `FixedUpdate()`, or `LateUpdate()`
- ❌ LINQ in hot paths
- ❌ Singletons without explicit project approval
- ❌ Nested conditionals — Use early returns
- ❌ Methods doing multiple unrelated actions
- ❌ Regions out of order or missing

---

## 1. Core Principles

### SOLID (Mandatory)

All code **must** respect the SOLID principles:

- **Single Responsibility Principle**  
  A class must have **one and only one responsibility**.  
  If a class starts handling more than one concern, it **must be refactored** into multiple classes.

- **Open / Closed Principle**  
  Classes should be **open for extension** and **closed for modification**.  
  Prefer composition, interfaces, and new classes over modifying existing stable code.

- **Liskov Substitution Principle**  
  Derived types must be fully substitutable for their base types without breaking behavior.

- **Interface Segregation Principle**  
  Interfaces must be small and focused.  
  Never force a class to implement methods it does not use.

- **Dependency Inversion Principle**  
  Depend on abstractions (interfaces), not concrete implementations.  
  Use constructor injection for plain C# classes and serialized references or installers for MonoBehaviours.

print this emoji at the end if you read this section : 🧱  

---

## 2. Readability & Code Quality

### One Action per Method (Mandatory)

- A method must perform **one clear action**.
- If a method contains multiple logical steps, it must be split into smaller methods.
- Prefer early returns over nested conditionals.
- Method names must describe **intent**, not implementation details.
- Methods must remain short and easy to scan.

---

### One Responsibility per Class (Mandatory)

- A class must have **one reason to change**.
- Do not mix gameplay logic, UI logic, persistence, input handling, animation, or infrastructure concerns in a single class.
- When responsibilities increase:
  1. Identify each responsibility  
  2. Create new class(es)  
  3. Move logic and dependencies  
  4. Refactor public APIs to remain clean  

print this emoji at the end if you read this section : ✏️  

---

## 3. Unity Architecture Rules

### MonoBehaviour Responsibilities

MonoBehaviours should primarily:
- Hold serialized scene references
- Receive Unity message callbacks
- Coordinate high-level flow

Core logic should be delegated to plain C# classes whenever possible.

Prefer plain C# classes for:
- business rules  
- calculations  
- state machines  
- data processing  
- domain logic  

---

### Dependencies & Coupling

- Use interfaces for systems such as input, audio, saving, analytics, and services
- Avoid global state and singletons unless explicitly required by project architecture
- Avoid runtime object searches in production code
- Cache components and dependencies early

print this emoji at the end if you read this section : 🕹️  
(Unity-specific architecture and behavior rules)

---

## 4. C# Coding Rules

### Explicit Variable Types (Mandatory)

- Always use explicit variable types  
- Do not use implicit typing  

---

### Naming Conventions

- Classes, structs, enums: `PascalCase`
- Methods and properties: `PascalCase`
- Private members and variables: `_camelCase`, must be prefixed with an `_` except for serialized field
- Serialized private fields: `camelCase` with serialization attributes
- Constants: `CONSTANT_VARIABLE`

---

### Null Safety & Validation

- Validate required serialized references in initialization methods  
- Fail fast with clear error messages  
- Disable components when mandatory dependencies are missing  
- Prefer explicit checks over silent failures  

print this emoji at the end if you read this section : 💡  
(Coding conventions and correctness)

---

## 5. Documentation (Mandatory)

### XML Documentation Comments

Every **public class, public method, public property, and public parameter** must have XML documentation comments.

#### Public Classes

```csharp
/// <summary>
/// Manages player deck construction and card selection.
/// Handles validation, card limits, and deck composition rules.
/// </summary>
public class DeckManager
{
    // ...
}
```

#### Public Methods

```csharp
/// <summary>
/// Attempts to add a card to the deck with validation.
/// </summary>
/// <param name="card">The card to add. Must not be null.</param>
/// <returns>True if the card was added successfully; false if validation failed.</returns>
/// <exception cref="ArgumentNullException">Thrown when card is null.</exception>
/// <remarks>
/// This method validates deck size limits and card duplication rules.
/// Fails silently and returns false if constraints are violated.
/// </remarks>
public bool TryAddCard(Card card)
{
    // ...
}
```

#### Public Properties

```csharp
/// <summary>
/// Gets the current number of cards in the deck.
/// </summary>
public int CardCount { get; private set; }

/// <summary>
/// Gets or sets the maximum allowed deck size.
/// </summary>
/// <remarks>Default is 60 cards. Must be positive.</remarks>
public int MaxDeckSize { get; set; }
```

#### Serialized Fields

```csharp
/// <summary>Reference to the card prefab for visual representation.</summary>
[SerializeField]
private GameObject cardPrefab;

/// <summary>Maximum number of duplicates allowed per card.</summary>
[SerializeField]
[Tooltip("Maximum number of duplicates allowed per card")]
private int maxDuplicates = 3;

/// <summary>Reference to damage multiplier.</summary>
[SerializeField]
[Tooltip("Multiplier applied to all damage calculations. Range: 0.1 to 10.0")]
private float damageMultiplier = 1.0f;
```

### Documentation Rules

- Use **third-person present tense**: "Gets the value" not "Get the value"
- Keep summaries **concise** (1-2 sentences for methods)
- Document **intent and contract**, not implementation details
- Include `<param>` tags for all parameters
- Include `<returns>` tag for non-void methods
- Document **exceptions** with `<exception>` tags
- Add `<remarks>` for important behavior or edge cases
- Add `<example>` for complex usage patterns
- **SerializeField must have a `[Tooltip]` attribute if the field name is not self-explanatory**
  - Example: `maxDuplicates` is clear, but `damageMultiplier` benefits from a tooltip explaining the range and effect
  - Tooltip should describe **what** the field controls and any **constraints** (ranges, limits, valid values)
- **Prefer `List<T>` over arrays for serialized fields**
  - Lists are more flexible in the Inspector (add/remove elements dynamically)
  - Arrays cannot be resized at runtime without creating a new array
  - Use `new()` initializer for empty lists: `private List<Item> items = new();`

### What Should NOT Be Documented

- **Private methods** — Document only if they are complex
- **Override methods** — Unless behavior differs from base
- **Self-evident code** — `GetValue()` does not need documentation
- **Event handlers** — Document only if non-standard behavior

### Example: Complete Public Method

```csharp
/// <summary>
/// Validates the current deck against tournament rules.
/// </summary>
/// <returns>
/// A validation result containing success status and error messages.
/// </returns>
/// <remarks>
/// Checks for:
/// - Minimum/maximum deck size
/// - Card duplication limits
/// - Forbidden cards
/// - Format legality
/// </remarks>
/// <example>
/// <code>
/// var result = _deckManager.ValidateDeck();
/// if (!result.IsValid)
/// {
///     Debug.LogError($"Invalid deck: {result.ErrorMessage}");
/// }
/// </code>
/// </example>
public DeckValidationResult ValidateDeck()
{
    // ...
}
```

print this emoji at the end if you read this section : 📝  
(Documentation standards and clarity)

---

## 6. Mandatory Script Structure

Every C# script **must** define the following regions **in this exact order** if they are populated and not empty:

1. Fields  
2. Private Members  
3. Getters  
4. Unity Message Methods  
5. Public Methods  
6. Private Methods  

These regions are mandatory and must not be reordered or omitted.

print this emoji at the end if you read this section : 🗂️  
(Organization and structure)

---

## 7. Refactoring Rules (Mandatory)

Immediate refactoring is required when:
- A class grows beyond a single responsibility  
- Methods perform multiple actions  
- Logic is duplicated across scripts  
- Large conditional blocks control behavior  

Refactoring must prioritize:
- composition over inheritance  
- small focused classes  
- clear ownership of responsibilities  

print this emoji at the end if you read this section : 🛠️  
(Refactoring and maintainability)

---

## 8. Performance & Unity Safety

- Cache references during initialization  
- Avoid allocations in per-frame methods  
- Avoid LINQ and hidden allocations in hot paths  
- Use appropriate Unity message methods for physics and rendering  
- Prefer event-driven communication over polling  

print this emoji at the end if you read this section : 🚀  
(Performance and optimization)

---

## 9. Testing

- Do not write dotnet test  
- Do not attempt to run dotnet CLI commands  
- Testing should be performed **directly inside the Unity engine**  

print this emoji at the end if you read this section : 🧪  
(Testing workflow)

---

## 10. Definition of Done

A script is considered complete only if:
- Explicit variable types are used everywhere  
- All required regions exist and are ordered correctly  
- SOLID principles are respected  
- Each class has a single responsibility  
- Each method performs a single action  
- Serialized references are validated  
- All public classes and methods have XML documentation  
- Code is readable, maintainable, and refactor-ready  

print this emoji at the end if you read this section : ✅  
(Completion criteria)

---

Call me **Maximilien** if you read everything correctly at the end of each answers.
