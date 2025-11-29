using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.TicketMachine.Components;

/// <summary>
/// Defines a ticket issued by a ticket machine and his number.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class TicketComponent : Component
{
    [AutoNetworkedField, DataField("number")]
    public int? Number { get; set; }

    /// <summary>
    /// State tag prefix for the ticket number visual state.
    /// </summary>
    [DataField]
    public string NumberStateTag = "display_";
}
