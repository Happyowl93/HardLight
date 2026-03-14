using Content.Shared.Damage;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Clothing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DamageOnEquipComponent : Component
{
    [DataField] public DamageSpecifier? EquipDamage;
    [DataField] public DamageSpecifier? UnequipDamage;
    [DataField] public SlotFlags TargetSlots;
    [DataField] public bool IgnoreResistances;
    [DataField] public bool InterruptDoAfters;
    [DataField] public bool IgnoreGlobalModifiers;
    [DataField] public float ArmorPenetration;
    [DataField] public bool CanHeal;
    [DataField] public TimeSpan Delay = TimeSpan.Zero;
    [DataField] public TimeSpan PopupDelay = TimeSpan.Zero;
    [DataField] public LocId? PopupLocId;
    [DataField] public bool DropOnKill;
    [DataField] public bool ForceDrop;
    [DataField] public bool CanDamageDead;
}