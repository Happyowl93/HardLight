using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Starlight.Traits.Assorted;

/// <summary>
///     System that handles the damaged throat trait, causing damage and coughing when speaking normally.
/// </summary>
public sealed partial class DamagedThroatSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamagedThroatComponent, EntitySpokeEvent>(OnSpeak);
    }

    private void OnSpeak(EntityUid uid, DamagedThroatComponent component, EntitySpokeEvent args)
    {
        // Don't apply damage if whispering
        if (args.IsWhisper)
            return;

        // Check cooldown
        if (_gameTiming.CurTime < component.LastDamageTime + component.Cooldown)
            return;

        // Reset damage if enough time has passed since last normal speech
        if (_gameTiming.CurTime >= component.LastSpeakTime + component.ResetCooldown)
        {
            component.CurrentDamage = component.BaseDamage;
        }

        // Apply current damage level
        var damageSpec = new DamageSpecifier(_prototypeManager.Index(component.DamageType), component.CurrentDamage);
        _damageableSystem.TryChangeDamage(uid, damageSpec, ignoreResistances: false);

        // Make the entity cough with a chance
        if (_random.Prob(component.CoughChance))
        {
            _chatSystem.TryEmoteWithChat(uid, "Cough", ChatTransmitRange.Normal);
        }

        // Escalate damage for next time (capped at max)
        component.CurrentDamage = Math.Min(component.CurrentDamage + component.DamageIncrement, component.MaxDamage);

        // Update timers
        component.LastDamageTime = _gameTiming.CurTime;
        component.LastSpeakTime = _gameTiming.CurTime;
    }
}
