---
paths:
  - "Assets/_DeckBuilder/Script/**/*.cs"
---

# Code Style — C# Coding Standards

This document defines mandatory code-style rules for all C# scripts in this Unity project.

---

## Anti-Patterns (Never Do)

- ❌ `var` — Always use explicit types
- ❌ Missing XML documentation on public members
- ❌ Nested conditionals — Use early returns
- ❌ Methods doing multiple unrelated actions
- ❌ Regions out of order or missing

---

## 1. Readability & Code Quality

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

---

## 2. C# Coding Rules

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

---

## 3. Documentation (Mandatory)

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

---

## 4. Mandatory Script Structure

Every C# script **must** define the following regions **in this exact order** if they are populated and not empty:

1. Fields
2. Private Members
3. Getters
4. Unity Message Methods
5. Public Methods
6. Private Methods

These regions are mandatory and must not be reordered or omitted.
