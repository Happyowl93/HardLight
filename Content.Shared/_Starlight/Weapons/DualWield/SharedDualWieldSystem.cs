using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._Starlight.Weapons.DualWield;

/// <summary>
/// Handles the dual-wield toggle verb and cleanup when a gun leaves a hand.
/// The actual alternating-gun logic lives in SharedGunSystem (TryGetGun + OnShootRequest).
/// </summary>
public sealed class SharedDualWieldSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Accuracy penalty while dual-wielding
        SubscribeLocalEvent<CanDualWieldComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);

        // Deactivate dual-wield if a gun leaves the user's hand
        SubscribeLocalEvent<GunComponent, GotUnequippedHandEvent>(OnGunUnequipped);
    }

    // ── Accuracy penalty ──────────────────────────────────────────────────────────

    private void OnGunRefreshModifiers(Entity<CanDualWieldComponent> gun, ref GunRefreshModifiersEvent args)
    {
        if (gun.Comp.DualWieldInaccuracyPenalty <= 0f)
            return;

        // The gun lives in a ContainerSlot whose parent is the holder entity
        var holder = Transform(gun).ParentUid;
        if (!TryComp<DualWieldComponent>(holder, out var dw) || !dw.Active)
            return;

        if (dw.LeftGun != gun.Owner && dw.RightGun != gun.Owner)
            return;

        var penalty = Angle.FromDegrees(gun.Comp.DualWieldInaccuracyPenalty);
        args.MinAngle += penalty;
        args.MaxAngle += penalty;
    }

    // ── Toggle ────────────────────────────────────────────────────────────────

    public void ToggleDualWield(EntityUid user, EntityUid leftGun, EntityUid rightGun, bool isCurrentlyActive)
    {
        if (isCurrentlyActive)
        {
            if (TryComp<DualWieldComponent>(user, out var dw))
            {
                var left = dw.LeftGun;
                var right = dw.RightGun;
                dw.Active = false;
                Dirty(user, dw);
                // Remove accuracy penalties
                _gun.RefreshModifiers(left);
                _gun.RefreshModifiers(right);
            }
            _popup.PopupClient(Loc.GetString("dual-wield-disabled"), user, user);
        }
        else
        {
            // Safety check — both guns must have CanDualWieldComponent
            if (!HasComp<CanDualWieldComponent>(leftGun) || !HasComp<CanDualWieldComponent>(rightGun))
            {
                _popup.PopupClient(Loc.GetString("dual-wield-too-heavy"), user, user);
                return;
            }

            var dw = EnsureComp<DualWieldComponent>(user);
            dw.Active   = true;
            dw.LeftGun  = leftGun;
            dw.RightGun = rightGun;
            dw.NextIsLeft = false;
            Dirty(user, dw);
            // Refresh both guns so accuracy penalties kick in
            _gun.RefreshModifiers(leftGun);
            _gun.RefreshModifiers(rightGun);
            _popup.PopupClient(Loc.GetString("dual-wield-enabled"), user, user);
        }
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    private void OnGunUnequipped(Entity<GunComponent> gun, ref GotUnequippedHandEvent args)
    {
        // If the user that was holding this gun had dual-wield active, disable it
        if (!TryComp<DualWieldComponent>(args.User, out var dw) || !dw.Active)
            return;

        if (dw.LeftGun != gun.Owner && dw.RightGun != gun.Owner)
            return;

        dw.Active = false;
        Dirty(args.User, dw);
        // Remove accuracy penalties from the gun that stayed
        var other = dw.LeftGun == gun.Owner ? dw.RightGun : dw.LeftGun;
        _gun.RefreshModifiers(gun.Owner);
        _gun.RefreshModifiers(other);
        _popup.PopupClient(Loc.GetString("dual-wield-interrupted"), args.User, args.User);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the user has exactly one gun in each hand (left and right).
    /// </summary>
    public bool TryGetBothGuns(
        EntityUid user,
        HandsComponent handsComp,
        out EntityUid leftGun,
        out EntityUid rightGun)
    {
        leftGun  = EntityUid.Invalid;
        rightGun = EntityUid.Invalid;

        foreach (var (handName, hand) in handsComp.Hands)
        {
            var held = _hands.GetHeldItem((user, handsComp), handName);
            if (held == null || !HasComp<GunComponent>(held.Value))
                continue;

            if (hand.Location == HandLocation.Left && leftGun == EntityUid.Invalid)
                leftGun = held.Value;
            else if (hand.Location == HandLocation.Right && rightGun == EntityUid.Invalid)
                rightGun = held.Value;
        }

        return leftGun != EntityUid.Invalid && rightGun != EntityUid.Invalid;
    }
}
