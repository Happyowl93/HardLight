using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Revolutionary.Components;

/// <summary>
/// When given to entitites, prevents them from being converted by a head revolutionary.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CantBecomeRevolutionaryComponent : Component;
