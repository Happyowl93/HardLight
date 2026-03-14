using Content.Server.Popups;
using Content.Shared._Starlight.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Server.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Starlight.Clothing.Systems;

public sealed class DamageOnEquipSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly MobStateSystem _state = default!;

    private readonly Dictionary<EntityUid, PendingDeath> _pendingDeaths = [];
    private readonly Dictionary<EntityUid, PendingPopup> _pendingPopups = [];
    private readonly List<EntityUid> _ignore = []; // ignore this time because they were forced to take it off by this system.

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOnEquipComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<DamageOnEquipComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void DoDamage(EntityUid uid, DamageOnEquipComponent comp, EntityUid target, DamageSpecifier damage)
    {
        if(_pendingDeaths.ContainsKey(target)) return;
        if (_ignore.Remove(target)) return;
        if (!comp.CanDamageDead && _state.IsDead(target)) return;
        var popupDelay = comp.PopupLocId is not null ? comp.PopupDelay : TimeSpan.Zero;
        var damageDelay = comp.PopupDelay + comp.Delay;

        if (popupDelay == TimeSpan.Zero && comp.PopupLocId is not null)
            _popup.PopupEntity(Loc.GetString(comp.PopupLocId), uid, uid, PopupType.MediumCaution);
        if (damageDelay == TimeSpan.Zero)
        {
            _damage.ChangeDamage(target, damage, comp.IgnoreResistances, comp.InterruptDoAfters,
                uid, comp.IgnoreGlobalModifiers, comp.ArmorPenetration, comp.CanHeal);
            if (comp.DropOnKill && _state.IsDead(target))
            {
                _ignore.Add(target);
                _container.TryRemoveFromContainer(uid, comp.ForceDrop);
            }
            return;
        }

        if (comp.PopupLocId is { } locId && popupDelay > TimeSpan.Zero) // should be mutually exclusive with the check above
            _pendingPopups.Add(target, new PendingPopup(uid, _timing.CurTime + popupDelay, locId));
        _pendingDeaths.Add(target, new PendingDeath(uid, _timing.CurTime + damageDelay, damage));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var (uid, pending) in _pendingPopups)
        {
            if (Deleted(uid) || Deleted(pending.Damager))
            {
                _pendingPopups.Remove(uid);
                continue;
            }
            if (_timing.CurTime < pending.Delay) continue;
            _pendingPopups.Remove(uid);
            _popup.PopupEntity(Loc.GetString(pending.Popup), uid, uid, PopupType.MediumCaution);
        }
        
        foreach (var (uid, pending) in _pendingDeaths)
        {
            if (Deleted(uid) || Deleted(pending.Damager))
            {
                _pendingPopups.Remove(uid);
                continue;
            }
            if(_timing.CurTime < pending.Delay) continue;
            _pendingDeaths.Remove(uid);
            if (!TryComp<DamageOnEquipComponent>(pending.Damager, out var comp))
                continue;
            _damage.ChangeDamage(uid, pending.Damage, comp.IgnoreResistances, comp.InterruptDoAfters,
                uid, comp.IgnoreGlobalModifiers, comp.ArmorPenetration, comp.CanHeal);
            if (comp.DropOnKill && _state.IsDead(uid))
            {
                _ignore.Add(uid);
                _container.TryRemoveFromContainer(pending.Damager, comp.ForceDrop);
            }
        }
    }

    private void OnGotEquipped(EntityUid uid, DamageOnEquipComponent comp, GotEquippedEvent ev)
    {
        if (ev.SlotFlags != comp.TargetSlots) return;
        if (comp.EquipDamage is null) return;
        DoDamage(uid, comp, ev.Equipee, comp.EquipDamage);
    }

    private void OnGotUnequipped(EntityUid uid, DamageOnEquipComponent comp, GotUnequippedEvent ev)
    {
        if (ev.SlotFlags != comp.TargetSlots) return;
        if (comp.UnequipDamage is null) return;
        DoDamage(uid, comp, ev.Equipee, comp.UnequipDamage);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _pendingDeaths.Clear();
        _pendingPopups.Clear();
        _ignore.Clear();
    }

    private record struct PendingDeath(EntityUid Damager, TimeSpan Delay, DamageSpecifier Damage);
    private record struct PendingPopup(EntityUid Damager, TimeSpan Delay, LocId Popup);
}