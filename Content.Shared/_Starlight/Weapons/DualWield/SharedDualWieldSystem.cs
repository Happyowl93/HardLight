using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared._Starlight.Weapons.DualWield;

/// <summary>
/// Handles the dual-wield toggle verb and cleanup when a gun leaves a hand.
/// The actual alternating-gun logic lives in SharedGunSystem (TryGetGun + OnShootRequest).
/// </summary>
public sealed class SharedDualWieldSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Deactivate dual-wield if a gun leaves the user's hand
        SubscribeLocalEvent<GunComponent, GotUnequippedHandEvent>(OnGunUnequipped);
    }

    // ── Toggle ────────────────────────────────────────────────────────────────

    public void ToggleDualWield(EntityUid user, EntityUid leftGun, EntityUid rightGun, bool isCurrentlyActive)
    {
        if (isCurrentlyActive)
        {
            if (TryComp<DualWieldComponent>(user, out var dw))
            {
                dw.Active = false;
                Dirty(user, dw);
            }
            _popup.PopupClient(Loc.GetString("dual-wield-disabled"), user, user);
        }
        else
        {
            var dw = EnsureComp<DualWieldComponent>(user);
            dw.Active   = true;
            dw.LeftGun  = leftGun;
            dw.RightGun = rightGun;
            dw.NextIsLeft = false; // First shot fires from left (off-hand) for flair
            Dirty(user, dw);
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
