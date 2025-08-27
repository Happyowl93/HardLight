using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Game rule marker component for handling Vampire antagonist selection/rule state.
/// Exists so YAML `- type: VampireRule` attaches a component and systems can check for it.
/// </summary>
[RegisterComponent]
public sealed partial class VampireRuleComponent : Component;
