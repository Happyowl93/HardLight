using Robust.Shared.GameStates;

namespace Content.Shared.Roles.Components;

/// <summary>
/// Mind role marker for Vampires. Mirrors other role components (e.g., NinjaRoleComponent).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VampireRoleComponent : BaseMindRoleComponent;
