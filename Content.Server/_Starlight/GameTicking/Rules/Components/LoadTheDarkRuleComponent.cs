using Robust.Shared.Utility;

namespace Content.Server._Starlight.GameTicking.Rules.Components;

/// <summary>
/// Works with <see cref="RuleGridsComponent"/>.
/// </summary>
[RegisterComponent, Access(typeof(LoadTheDarkRuleSystem))]
public sealed partial class LoadTheDarkRuleComponent : Component
{
    /// <summary>
    /// A map to load.
    /// </summary>
    [DataField]
    public ResPath? MapPath;
}
