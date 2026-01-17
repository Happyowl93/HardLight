using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Stores data for <see cref="SELFRuleSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(SELFRuleSystem))]
public sealed partial class SELFRuleComponent : Component;
