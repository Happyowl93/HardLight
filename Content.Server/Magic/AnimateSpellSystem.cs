using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared.Actions.Components;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Triggers;
using Content.Shared.Item;
using Content.Shared.Magic.Components;
using Content.Shared.Magic.Events;
using Content.Shared.Magic.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using Robust.Shared.Log;

namespace Content.Server.Magic;

/// <summary>
/// Server-side system for handling animated objects, specifically setting their HP based on size.
/// Starlight edit: HP ranges are configurable per-staff via AnimatedObjectHPComponent
/// </summary>
public sealed class AnimateSpellSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    
    private ISawmill _sawmill = default!;
    
    private EntityUid? _lastActionUsed; // Track the last action used for animated objects

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _logManager.GetSawmill("animate_spell");
        SubscribeLocalEvent<AnimateComponent, AnimateSpellEvent>(OnAnimateSpell);
        SubscribeLocalEvent<ChangeComponentsSpellEvent>(OnChangeComponentsSpell);
    }

    private void OnChangeComponentsSpell(ChangeComponentsSpellEvent ev)
    {
        // Store the action entity if this is an animate spell
        if (ev.ToAdd.ContainsKey("Animate"))
            _lastActionUsed = ev.Action.Owner;
    }

    private void OnAnimateSpell(EntityUid uid, AnimateComponent component, ref AnimateSpellEvent args) =>
        SetAnimatedObjectHP(uid, _lastActionUsed);

    private void SetAnimatedObjectHP(EntityUid uid, EntityUid? action)
    {
        _sawmill.Info($"SetAnimatedObjectHP called for entity {ToPrettyString(uid)}");
        
        // Try to get HP configuration from the staff (action container)
        AnimatedObjectHPComponent? hpConfig = null;
        if (action != null && TryComp<ActionComponent>(action.Value, out var actionComp) && actionComp.Container != null)
        {
            TryComp<AnimatedObjectHPComponent>(actionComp.Container.Value, out hpConfig);
            _sawmill.Info($"Action: {ToPrettyString(action.Value)}, Container: {ToPrettyString(actionComp.Container.Value)}, HP Config found: {hpConfig != null}");
        }

        // Use default values if no config found on staff
        hpConfig ??= new AnimatedObjectHPComponent();
        
        // Determine HP based on item size with random variance
        int hp;
        
        if (TryComp<ItemComponent>(uid, out var item))
        {
            var sizeId = item.Size.Id;
            
            // Get HP range based on size from component configuration
            var (min, max) = sizeId switch
            {
                "Tiny" => hpConfig.TinyHP,
                "Small" => hpConfig.SmallHP,
                "Normal" => hpConfig.NormalHP,
                "Large" => hpConfig.LargeHP,
                "Huge" => hpConfig.HugeHP,
                "Ginormous" => hpConfig.GinormousHP,
                _ => hpConfig.NormalHP
            };
            
            hp = _random.Next(min, max + 1);
            _sawmill.Info($"Entity is item with size {sizeId}, HP set to {hp} (range {min}-{max})");
        }
        else
        {
            // Objects without ItemComponent (can't be picked up) - furniture, structures, etc.
            var (min, max) = hpConfig.NonItemHP;
            hp = _random.Next(min, max + 1);
            _sawmill.Info($"Entity is not an item, HP set to {hp} (range {min}-{max})");
        }

        // Add or update Destructible component with size-based HP
        var destructible = EnsureComp<DestructibleComponent>(uid);
        destructible.Thresholds = new()
        {
            new DamageThreshold
            {
                Trigger = new DamageTrigger
                {
                    Damage = hp
                },
                Behaviors = new()
                {
                    new DoActsBehavior
                    {
                        Acts = ThresholdActs.Destruction
                    },
                    new PlaySoundBehavior
                    {
                        Sound = new SoundCollectionSpecifier("MetalBreak")
                    }
                }
            }
        };
        
        _sawmill.Info($"Destructible component added with threshold damage: {hp}");
    }
}
