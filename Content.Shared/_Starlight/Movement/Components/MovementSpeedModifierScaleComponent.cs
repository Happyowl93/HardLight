namespace Content.Shared._Starlight.Movement.Components;

/// <summary>
///     TODO: 
/// </summary>
[RegisterComponent]
public sealed partial class MovementSpeedModifierScaleComponent : Component
{
    /// <summary>
    ///     TODO: Sets the scale that all speed modifiers are scaled by.
    /// </summary>
    [DataField]
    public float MovementSpeedScale = 1f;
}