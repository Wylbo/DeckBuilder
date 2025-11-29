public enum UILayer
{
    Screen = 0,
    Popup = 1,
    Overlay = 2,
    Hud = 3
}

/// <summary>
/// Controls how a layer behaves when multiple views are shown.
/// Exclusive hides the previous view, Stacked keeps previous visible but tracks order,
/// Additive leaves existing views untouched.
/// </summary>
public enum UILayerBehaviour
{
    Exclusive,
    Stacked,
    Additive
}
