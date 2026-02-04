using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Terminator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TerminatorComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Target;
}