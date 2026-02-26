using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Antags.Pirate;

/// <summary>
/// Marks an entity as a pirate. Used for status icon display.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PirateComponent : Component;
