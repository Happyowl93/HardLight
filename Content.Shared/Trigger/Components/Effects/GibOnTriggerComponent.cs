using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will gib the entity when triggered.
/// If TargetUser is true the user will be gibbed instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GibOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Should gibbing also delete the owners items?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DeleteItems = false;

    // Starlight Start
    /// <summary>
    /// Should the entity be gibbed? If false, only items will be deleted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool GibBody = true;
    // Starlight End
}
