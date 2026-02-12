---
paths:
  - "Assets/_DeckBuilder/Script/**/*.cs"
---

# Anti-Patterns (Never Do)

- Never use `FindObjectOfType` or `GameObject.Find` in production code
- Never allocate in `Update()`, `FixedUpdate()`, or `LateUpdate()`
- Never use LINQ in hot paths
- Never use singletons without explicit project approval
- Never have multiple responsibilities in one class
