using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Starlight.Magic.Components;

/// <summary>
/// Defines HP ranges for animated objects based on their size.
/// This allows per-staff configuration of HP ranges for animated objects.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnimatedObjectHPComponent : Component
{
    /// <summary>
    /// HP range for Tiny items (min, max)
    /// </summary>
    [DataField, AutoNetworkedField]
    public (int Min, int Max) TinyHP = (15, 25);

    /// <summary>
    /// HP range for Small items (min, max)
    /// </summary>
    [DataField, AutoNetworkedField]
    public (int Min, int Max) SmallHP = (25, 40);

    /// <summary>
    /// HP range for Normal/Medium items (min, max)
    /// </summary>
    [DataField, AutoNetworkedField]
    public (int Min, int Max) NormalHP = (40, 60);

    /// <summary>
    /// HP range for Large items (min, max)
    /// </summary>
    [DataField, AutoNetworkedField]
    public (int Min, int Max) LargeHP = (60, 90);

    /// <summary>
    /// HP range for Huge items (min, max)
    /// </summary>
    [DataField, AutoNetworkedField]
    public (int Min, int Max) HugeHP = (90, 120);

    /// <summary>
    /// HP range for Ginormous items (min, max)
    /// </summary>
    [DataField, AutoNetworkedField]
    public (int Min, int Max) GinormousHP = (120, 150);

    /// <summary>
    /// HP range for objects without ItemComponent (furniture, structures, etc.) (min, max)
    /// </summary>
    [DataField, AutoNetworkedField]
    public (int Min, int Max) NonItemHP = (150, 180);
}
