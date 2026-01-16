using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Robust.Shared.Serialization;
using Content.Shared.Inventory; // Starlight-edit

namespace Content.Shared.Starlight.Cybernetics;

/// <summary>
/// This contains all the events raised by the CyberneticDisruptionSystem
/// </summary>

/// <summary>
///     Raised directed on an entity when it is disrupted.
/// </summary>
[ByRefEvent]
public record struct CyberneticDisruptionEvent(EntityUid Target);

/// <summary>
///     Raised on a disrupted entity when something wants to remove the cybernetic disruption component.
/// </summary>
[ByRefEvent]
public record struct CyberneticDisruptionEndAttemptEvent(bool Cancelled);