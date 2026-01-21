using Robust.Shared.GameStates;

namespace Content.Server._Starlight.Storage;

[RegisterComponent, NetworkedComponent]
[Access(typeof(TwinPointerSystem))]
public sealed partial class TwinPointerBoxComponent : Component
{
}