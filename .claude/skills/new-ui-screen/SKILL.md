---
name: new-ui-screen
description: Scaffold a new UIView screen with factory registration
---

Create a new UI screen named `$ARGUMENTS` following the project conventions.

## Steps

1. **Read existing patterns** to match the project style:
   - Read `Assets/_DeckBuilder/Script/UI/Framework/UIView.cs` for the base class pattern
   - Read one file in `Assets/_DeckBuilder/Script/UI/Screen/` for an example view
   - Read `Assets/_DeckBuilder/Script/UI/Framework/UIViewFactory.cs` for registration

2. **Create the View class**:
   - Create `Assets/_DeckBuilder/Script/UI/Screen/{ScreenName}View.cs`
   - Inherit from `UIView`
   - Follow the mandatory region structure (Fields, Private Members, Getters, Unity Message Methods, Public Methods, Private Methods)
   - Override `OnShow()` and `OnHide()` as needed
   - Add serialized fields for UI references (buttons, text, etc.)
   - Follow all code-style rules from `.claude/rules/code-style.md`

3. **Register in UIViewFactory**:
   - Add the prefab mapping in the appropriate registration location
   - Search for existing registrations to match the pattern

4. **Remind the user** to:
   - Create the prefab in Unity with the view component attached
   - Assign the prefab reference in the factory configuration
   - Use `Manager.Show<{ScreenName}View>()` to display it
