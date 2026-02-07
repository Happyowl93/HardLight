using Content.Shared.Damage;

namespace Content.Shared._Starlight.Clothing.Components;

[RegisterComponent]
public sealed partial class DamageOnEquipComponent : Component
{
    [DataField] public DamageSpecifier? EquipDamage;
    [DataField] public DamageSpecifier? UnequipDamage;
    [DataField] public bool IgnoreResistances;
    [DataField] public bool InterruptDoAfters;
    [DataField] public bool IgnoreGlobalModifiers;
    [DataField] public float ArmorPenetration;
    [DataField] public bool CanHeal;
    [DataField] public TimeSpan? Delay;
    [DataField] public TimeSpan? PopupDelay;
    [DataField] public LocId? PopupLocId;
}