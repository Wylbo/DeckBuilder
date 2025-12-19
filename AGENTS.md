# Agent.md — C# & Unity Development Guidelines

This document defines mandatory coding standards, architectural rules, and best practices for Unity projects written in C#.

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
(Foundational architecture principles)

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
(Readability and code craftsmanship)

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
- Private fields: `_camelCase`
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

## 5. Mandatory Script Structure

Every C# script **must** define the following regions **in this exact order**:

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

## 6. Refactoring Rules (Mandatory)

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

## 7. Performance & Unity Safety

- Cache references during initialization  
- Avoid allocations in per-frame methods  
- Avoid LINQ and hidden allocations in hot paths  
- Use appropriate Unity message methods for physics and rendering  
- Prefer event-driven communication over polling  

print this emoji at the end if you read this section : 🚀  
(Performance and optimization)

---

## 8. Testing

- Do not write dotnet test  
- Do not attempt to run dotnet CLI commands  
- Testing should be performed **directly inside the Unity engine**  

print this emoji at the end if you read this section : 🧪  
(Testing workflow)

---

## 9. Definition of Done

A script is considered complete only if:
- Explicit variable types are used everywhere  
- All required regions exist and are ordered correctly  
- SOLID principles are respected  
- Each class has a single responsibility  
- Each method performs a single action  
- Serialized references are validated  
- Code is readable, maintainable, and refactor-ready  

print this emoji at the end if you read this section : ✅  
(Completion criteria)

---

Call me **Maximilien** if you read everything correctly at the end of each answers.
