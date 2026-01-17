namespace Content.Shared.Starlight.Cybernetics.Components;

/// <summary>
/// For tools which can be used to temporarily disable active cyberware.
/// </summary>
[RegisterComponent]
public sealed partial class CyberneticDisruptorComponent : Component
{
    [DataField]
    public TimeSpan UseTime = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Should the duration be replaced (true) or added (false)
    /// </summary>
    [DataField]
    public bool RefreshDuration = true;
}
