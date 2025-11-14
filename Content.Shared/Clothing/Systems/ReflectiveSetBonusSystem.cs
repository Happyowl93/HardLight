using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Tag;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Systems;

/// <summary>
/// Starlight: Manages the reflective armor set bonus - grants 100% reflection when both vest and helmet are equipped.
/// </summary>
/// STARLIGHT: This file was added for the reflective armor set bonus system.
/// This system grants 100% reflection to the vest and helmet only when both are equipped.
/// Uses tags to detect matching items and listens to global equip/unequip events.
public sealed class ReflectiveSetBonusSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> _vestTag = "ReflectiveArmorVest";
        // Tag for reflective vest, defined in tags.yml
    private static readonly ProtoId<TagPrototype> _helmetTag = "ReflectiveArmorHelmet";
    // Tag for reflective helmet, defined in tags.yml

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<DidEquipEvent>(OnDidEquip);
        SubscribeLocalEvent<DidUnequipEvent>(OnDidUnequip);
    }

    private void OnDidEquip(DidEquipEvent args)
    {
        // Check if the equipped item is part of the reflective set
        if (_tag.HasTag(args.Equipment, _vestTag) || _tag.HasTag(args.Equipment, _helmetTag))
        {
            CheckAllReflectiveSets(args.Equipee);
        }
    }

    private void OnDidUnequip(DidUnequipEvent args)
    {
        // Check if the unequipped item was part of the reflective set
        if (_tag.HasTag(args.Equipment, _vestTag) || _tag.HasTag(args.Equipment, _helmetTag))
        {
            CheckAllReflectiveSets(args.Equipee);
        }
    }

    private void CheckAllReflectiveSets(EntityUid wearer)
        // Checks all equipped items for the set bonus and applies correct reflection probability.
    {
        if (!TryComp<InventoryComponent>(wearer, out var inventory))
            return;

        // Check if wearer has both reflective vest and reflective helmet
        var hasVest = false;
        var hasHelmet = false;
        EntityUid? vestEntity = null;
        EntityUid? helmetEntity = null;

        // Check all equipped items
        if (_inventory.TryGetContainerSlotEnumerator(wearer, out var enumerator))
        {
            while (enumerator.MoveNext(out var slot))
            {
                if (slot.ContainedEntity == null)
                    continue;

                var item = slot.ContainedEntity.Value;

                if (_tag.HasTag(item, _vestTag))
                {
                    hasVest = true;
                    vestEntity = item;
                }

                if (_tag.HasTag(item, _helmetTag))
                {
                    hasHelmet = true;
                    helmetEntity = item;
                }
            }
        }

        // Apply set bonus if both pieces are equipped
        if (hasVest && hasHelmet && vestEntity.HasValue && helmetEntity.HasValue)
        {
            if (TryComp<ReflectComponent>(vestEntity.Value, out var vestReflect))
            {
                vestReflect.ReflectProb = 1.0f; // 100% reflection when both pieces equipped
                Dirty(vestEntity.Value, vestReflect);
            }
            if (TryComp<ReflectComponent>(helmetEntity.Value, out var helmetReflect))
            {
                helmetReflect.ReflectProb = 1.0f; // 100% reflection when both pieces equipped
                Dirty(helmetEntity.Value, helmetReflect);
            }
        }
        else
        {
            if (vestEntity.HasValue && TryComp<ReflectComponent>(vestEntity.Value, out var vestReflect))
            {
                vestReflect.ReflectProb = 0.65f; // Vest only
                Dirty(vestEntity.Value, vestReflect);
            }
            if (helmetEntity.HasValue && TryComp<ReflectComponent>(helmetEntity.Value, out var helmetReflect))
            {
                helmetReflect.ReflectProb = 0.35f; // Helmet only
                Dirty(helmetEntity.Value, helmetReflect);
            }
        }
    }
}
