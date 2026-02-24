using Content.Shared._Starlight.Shoelaces.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared._Starlight.Visibility.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Starlight.Shoelaces.Systems;

[Serializable, NetSerializable]
public sealed partial class ShoelaceTieDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ShoelaceUntieDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public bool SelfUntie;

    public ShoelaceUntieDoAfterEvent(bool selfUntie = false)
    {
        SelfUntie = selfUntie;
    }
}

public sealed class SharedShoelaceTieSystem : EntitySystem
{
    private static readonly ProtoId<TagPrototype> HardsuitTag = "Hardsuit";
    private static readonly ProtoId<TagPrototype> SuitEvaTag = "SuitEVA";

    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedFacingFilterSystem _facingFilter = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShoelaceTieableComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<ClothingComponent, GetVerbsEvent<Verb>>(OnGetShoeItemVerbs);

        SubscribeLocalEvent<ShoelaceTieableComponent, ShoelaceTieDoAfterEvent>(OnTieDoAfter);
        SubscribeLocalEvent<ShoelaceTieableComponent, DoAfterAttemptEvent<ShoelaceTieDoAfterEvent>>(OnTieDoAfterAttempt);

        SubscribeLocalEvent<ShoelaceTiedComponent, ShoelaceUntieDoAfterEvent>(OnUntieDoAfter);
        SubscribeLocalEvent<ShoelaceTiedComponent, DoAfterAttemptEvent<ShoelaceUntieDoAfterEvent>>(OnUntieDoAfterAttempt);
        SubscribeLocalEvent<ShoelaceTiedComponent, MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<ShoelaceTiedComponent, RemoveTiedShoelacesAlertEvent>(OnRemoveTiedAlert);
        SubscribeLocalEvent<ShoelaceTiedShoesComponent, GotUnequippedEvent>(OnShoesUnequipped);
        SubscribeLocalEvent<ShoelaceTiedShoesComponent, GotEquippedEvent>(OnShoesEquipped);

        SubscribeLocalEvent<TiedShoelacesStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusApplied);
        SubscribeLocalEvent<TiedShoelacesStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusRemoved);
    }

    private void OnGetVerbs(Entity<ShoelaceTieableComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        var user = args.User;
        var target = args.Target;

        if (target == user)
        {
            if (TryComp<ShoelaceTiedComponent>(target, out var tied) && CanManipulate(user, target))
            {
                var selfUntie = new Verb
                {
                    Text = Loc.GetString("shoelaces-verb-self-untie"),
                    Act = () => StartUntie(user, target, tied, selfUntie: true),
                };

                args.Verbs.Add(selfUntie);
            }

            return;
        }

        if (!CanManipulate(user, target))
            return;

        if (TryComp<ShoelaceTiedComponent>(target, out var tiedTarget))
        {
            var assistUntie = new Verb
            {
                Text = Loc.GetString("shoelaces-verb-assist-untie"),
                Act = () => StartUntie(user, target, tiedTarget, selfUntie: false),
            };

            args.Verbs.Add(assistUntie);
            return;
        }

        if (!_inventory.TryGetSlotEntity(target, "shoes", out var shoes))
            return;

        if (IsTargetWearingTieBlockingSuit(target))
            return;

        if (IsShoelaceTieBlocked(shoes.Value))
            return;

        var tieVerb = new Verb
        {
            Text = Loc.GetString("shoelaces-verb-tie"),
            Act = () => StartTie(user, (target, ent.Comp)),
        };

        args.Verbs.Add(tieVerb);
    }

    private bool CanManipulate(EntityUid user, EntityUid target)
    {
        if (!_actionBlocker.CanInteract(user, target))
            return false;

        return _interaction.InRangeAndAccessible(user, target, range: .5f);
    }

    private void OnGetShoeItemVerbs(Entity<ClothingComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || args.Using != ent.Owner)
            return;

        if ((ent.Comp.Slots & SlotFlags.FEET) == SlotFlags.NONE)
            return;

        var user = args.User;
        var shoes = ent.Owner;
        var blocked = IsShoelaceTieBlocked(shoes);
        var tieText = Loc.GetString("shoelaces-verb-tie-inhand");
        var untieText = Loc.GetString("shoelaces-verb-untie-inhand");

        foreach (var verb in args.Verbs)
        {
            if (verb.Text == tieText || verb.Text == untieText)
                return;
        }

        if (HasComp<ShoelaceTiedShoesComponent>(shoes))
        {
            var untie = new Verb
            {
                Text = untieText,
                Act = () => UntieShoesInHand(user, shoes),
            };

            args.Verbs.Add(untie);
            return;
        }

        if (blocked)
            return;

        var tie = new Verb
        {
            Text = tieText,
            Act = () => TieShoesInHand(user, shoes),
        };

        args.Verbs.Add(tie);
    }

    private void TieShoesInHand(EntityUid user, EntityUid shoes)
    {
        if (!_hands.IsHolding(user, shoes, out _))
            return;

        if (HasComp<ShoelaceTiedShoesComponent>(shoes))
            return;

        if (IsShoelaceTieBlocked(shoes))
            return;

        EnsureComp<ShoelaceTiedShoesComponent>(shoes);
        _popup.PopupPredicted(Loc.GetString("shoelaces-popup-tie-inhand-success"), shoes, user, PopupType.Medium);
    }

    private void UntieShoesInHand(EntityUid user, EntityUid shoes)
    {
        if (!_hands.IsHolding(user, shoes, out _))
            return;

        if (!HasComp<ShoelaceTiedShoesComponent>(shoes))
            return;

        RemComp<ShoelaceTiedShoesComponent>(shoes);
        _popup.PopupPredicted(Loc.GetString("shoelaces-popup-untie-inhand-success"), shoes, user, PopupType.Medium);
    }

    private void StartTie(EntityUid user, Entity<ShoelaceTieableComponent> target)
    {
        if (!CanManipulate(user, target))
            return;

        if (IsTargetWearingTieBlockingSuit(target))
            return;

        if (!_inventory.TryGetSlotEntity(target, "shoes", out var shoes))
        {
            _popup.PopupPredicted(Loc.GetString("shoelaces-popup-no-shoes"), user, user, PopupType.MediumCaution);
            return;
        }

        if (IsShoelaceTieBlocked(shoes.Value))
            return;

        var doAfter = new DoAfterArgs(EntityManager,
            user,
            target.Comp.TieTime,
            new ShoelaceTieDoAfterEvent(),
            target,
            target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupPredicted(Loc.GetString("shoelaces-popup-tying-start"), target, user, PopupType.Medium);
    }

    private void OnTieDoAfterAttempt(Entity<ShoelaceTieableComponent> ent, ref DoAfterAttemptEvent<ShoelaceTieDoAfterEvent> args)
    {
        if (args.Cancelled)
            return;

        var doAfterArgs = args.Event.Args;

        if (!CanManipulate(doAfterArgs.User, ent))
        {
            args.Cancel();
            return;
        }

        if (!_inventory.TryGetSlotEntity(ent, "shoes", out var shoes))
        {
            args.Cancel();
            return;
        }

        if (IsTargetWearingTieBlockingSuit(ent))
        {
            args.Cancel();
            return;
        }

        if (IsShoelaceTieBlocked(shoes.Value))
            args.Cancel();
    }

    private void OnTieDoAfter(Entity<ShoelaceTieableComponent> ent, ref ShoelaceTieDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!CanManipulate(args.Args.User, ent))
            return;

        if (!_inventory.TryGetSlotEntity(ent, "shoes", out var shoes))
            return;

        if (IsTargetWearingTieBlockingSuit(ent))
            return;

        if (IsShoelaceTieBlocked(shoes.Value))
            return;

        _status.TryAddStatusEffectDuration(ent, ent.Comp.TiedStatusEffect, ent.Comp.TiedDuration);

        _popup.PopupEntity(Loc.GetString("shoelaces-popup-tying-success-user"), args.Args.User, args.Args.User, PopupType.Medium);
        var othersFilter = _facingFilter.FacingPvsExcept(args.Args.User, except: args.Args.User);
        _popup.PopupEntity(
            Loc.GetString("shoelaces-popup-tying-success-others", ("user", args.Args.User), ("target", ent.Owner)),
            ent,
            othersFilter,
            true,
            PopupType.MediumCaution);

        args.Handled = true;
    }

    private void StartUntie(EntityUid user, EntityUid target, ShoelaceTiedComponent tied, bool selfUntie)
    {
        if (selfUntie && user != target)
            return;

        if (!CanManipulate(user, target))
            return;

        if (!TryComp<ShoelaceTiedComponent>(target, out _))
            return;

        var untieTime = selfUntie ? tied.UntieSelfTime : tied.UntieAssistTime;
        var doAfter = new DoAfterArgs(EntityManager,
            user,
            untieTime,
            new ShoelaceUntieDoAfterEvent(selfUntie),
            target,
            target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        var key = selfUntie ? "shoelaces-popup-self-untie-start" : "shoelaces-popup-assist-untie-start";
        _popup.PopupPredicted(Loc.GetString(key), target, user, PopupType.Medium);
    }

    private void OnUntieDoAfterAttempt(Entity<ShoelaceTiedComponent> ent, ref DoAfterAttemptEvent<ShoelaceUntieDoAfterEvent> args)
    {
        if (args.Cancelled)
            return;

        var doAfterArgs = args.Event.Args;
        if (!CanManipulate(doAfterArgs.User, ent))
        {
            args.Cancel();
            return;
        }

        if (!HasComp<ShoelaceTiedComponent>(ent))
            args.Cancel();
    }

    private void OnUntieDoAfter(Entity<ShoelaceTiedComponent> ent, ref ShoelaceUntieDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!CanManipulate(args.Args.User, ent))
            return;

        if (!TryComp<ShoelaceTiedComponent>(ent, out var tied))
            return;

        _status.TryRemoveStatusEffect(ent, tied.StatusEffect);

        var key = args.SelfUntie ? "shoelaces-popup-self-untie-success" : "shoelaces-popup-assist-untie-success";
        _popup.PopupPredicted(Loc.GetString(key), ent, args.Args.User, PopupType.Medium);

        args.Handled = true;
    }

    private void OnMoveInput(Entity<ShoelaceTiedComponent> ent, ref MoveInputEvent args)
    {
        if (!args.State || !args.HasDirectionalMovement)
            return;

        if (!args.Entity.Comp.Sprinting)
            return;

        if (ent.Comp.NextTripAttempt > _gameTiming.CurTime)
            return;

        ent.Comp.NextTripAttempt = _gameTiming.CurTime + TimeSpan.FromSeconds(ent.Comp.TripAttemptCooldown);
        Dirty(ent, ent.Comp);

        _stun.TryKnockdown(ent.Owner, TimeSpan.FromSeconds(ent.Comp.TripKnockdownTime), force: true);
        _popup.PopupPredicted(Loc.GetString("shoelaces-popup-trip"), ent, ent, PopupType.MediumCaution);
    }

    private void OnRemoveTiedAlert(Entity<ShoelaceTiedComponent> ent, ref RemoveTiedShoelacesAlertEvent args)
    {
        if (args.Handled)
            return;

        StartUntie(ent, ent, ent.Comp, selfUntie: true);
        args.Handled = true;
    }

    private void OnStatusApplied(Entity<TiedShoelacesStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_gameTiming.ApplyingState)
            return;

        EnsureComp<ShoelaceTiedComponent>(args.Target, out var tied);
        tied.UntieSelfTime = ent.Comp.UntieSelfTime;
        tied.UntieAssistTime = ent.Comp.UntieAssistTime;
        tied.TripKnockdownTime = ent.Comp.TripKnockdownTime;
        tied.TripAttemptCooldown = ent.Comp.TripAttemptCooldown;
        Dirty(args.Target, tied);

        if (_inventory.TryGetSlotEntity(args.Target, "shoes", out var shoes))
        {
            EnsureComp<ShoelaceTiedShoesComponent>(shoes.Value, out var tiedShoes);
            tiedShoes.StatusEffect = tied.StatusEffect;
            tiedShoes.UntieSelfTime = tied.UntieSelfTime;
            tiedShoes.UntieAssistTime = tied.UntieAssistTime;
            tiedShoes.TripKnockdownTime = tied.TripKnockdownTime;
            tiedShoes.TripAttemptCooldown = tied.TripAttemptCooldown;

            if (_status.TryGetTime(args.Target, tied.StatusEffect, out var time) && time.EndEffectTime is { } endTime)
                tiedShoes.RemainingDuration = endTime - _gameTiming.CurTime;
            else
                tiedShoes.RemainingDuration = null;

            Dirty(shoes.Value, tiedShoes);
        }

        _alerts.ShowAlert(args.Target, tied.Alert);
    }

    private void OnStatusRemoved(Entity<TiedShoelacesStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (TryComp<ShoelaceTiedComponent>(args.Target, out var tied))
            _alerts.ClearAlert(args.Target, tied.Alert);

        if (_inventory.TryGetSlotEntity(args.Target, "shoes", out var shoes))
            RemComp<ShoelaceTiedShoesComponent>(shoes.Value);

        RemComp<ShoelaceTiedComponent>(args.Target);
    }

    private void OnShoesUnequipped(Entity<ShoelaceTiedShoesComponent> ent, ref GotUnequippedEvent args)
    {
        if (args.Slot != "shoes")
            return;

        if (!TryComp<ShoelaceTiedComponent>(args.Equipee, out var tied))
            return;

        ent.Comp.StatusEffect = tied.StatusEffect;
        ent.Comp.UntieSelfTime = tied.UntieSelfTime;
        ent.Comp.UntieAssistTime = tied.UntieAssistTime;
        ent.Comp.TripKnockdownTime = tied.TripKnockdownTime;
        ent.Comp.TripAttemptCooldown = tied.TripAttemptCooldown;

        if (_status.TryGetTime(args.Equipee, tied.StatusEffect, out var time) && time.EndEffectTime is { } endTime)
            ent.Comp.RemainingDuration = endTime - _gameTiming.CurTime;
        else
            ent.Comp.RemainingDuration = null;

        Dirty(ent, ent.Comp);

        _stun.TryKnockdown(args.Equipee, TimeSpan.FromSeconds(tied.TripKnockdownTime), force: true);
        _popup.PopupPredicted(Loc.GetString("shoelaces-popup-trip"), args.Equipee, args.Equipee, PopupType.MediumCaution);

        _status.TryRemoveStatusEffect(args.Equipee, tied.StatusEffect);
    }

    private void OnShoesEquipped(Entity<ShoelaceTiedShoesComponent> ent, ref GotEquippedEvent args)
    {
        if (args.Slot != "shoes")
            return;

        if (IsShoelaceTieBlocked(ent.Owner))
        {
            RemComp<ShoelaceTiedShoesComponent>(ent);
            return;
        }

        if (ent.Comp.RemainingDuration is { } duration && duration <= TimeSpan.Zero)
        {
            RemComp<ShoelaceTiedShoesComponent>(ent);
            return;
        }

        if (HasComp<ShoelaceTiedComponent>(args.Equipee))
            return;

        if (ent.Comp.RemainingDuration is { } remaining)
            _status.TrySetStatusEffectDuration(args.Equipee, ent.Comp.StatusEffect, remaining);
        else
            _status.TrySetStatusEffectDuration(args.Equipee, ent.Comp.StatusEffect, null);
    }

    private bool IsShoelaceTieBlocked(EntityUid shoes)
    {
        if (EntityManager.HasComponent(shoes, typeof(MagbootsComponent)))
            return true;

        if (!EntityManager.TryGetComponent(shoes, typeof(MetaDataComponent), out var component)
            || component is not MetaDataComponent meta)
            return false;

        var shoeName = meta.EntityName;
        var protoId = meta.EntityPrototype?.ID;

        return (protoId?.Contains("slipper", StringComparison.OrdinalIgnoreCase) ?? false)
               || (protoId?.Contains("sock", StringComparison.OrdinalIgnoreCase) ?? false)
               || (protoId?.Contains("galosh", StringComparison.OrdinalIgnoreCase) ?? false)
               || (protoId?.Contains("jump", StringComparison.OrdinalIgnoreCase) ?? false)
               || (protoId?.Contains("tourist", StringComparison.OrdinalIgnoreCase) ?? false)
               || shoeName.Contains("slipper", StringComparison.OrdinalIgnoreCase)
             || shoeName.Contains("sock", StringComparison.OrdinalIgnoreCase)
             || shoeName.Contains("galosh", StringComparison.OrdinalIgnoreCase)
             || shoeName.Contains("jump", StringComparison.OrdinalIgnoreCase)
             || shoeName.Contains("tourist", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsTargetWearingTieBlockingSuit(EntityUid target)
    {
        if (!_inventory.TryGetSlotEntity(target, "outerClothing", out var outerClothing))
            return false;

        return _tag.HasTag(outerClothing.Value, HardsuitTag)
               || _tag.HasTag(outerClothing.Value, SuitEvaTag);
    }
}
