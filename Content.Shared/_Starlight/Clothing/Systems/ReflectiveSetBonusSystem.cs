using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Tag;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Clothing.Systems;

/// <summary>
/// Manages the reflective armor set bonus - grants 100% reflection when both vest and helmet are equipped.
/// This system grants 100% reflection to the vest and helmet only when both are equipped.
/// Uses tags to detect matching items and listens to global equip/unequip events.
/// </summary>
public sealed class ReflectiveSetBonusSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    // Tag for reflective vest, defined in tags.yml
    private static readonly ProtoId<TagPrototype> _vestTag = "ReflectiveArmorVest";
    // Tag for reflective helmet, defined in tags.yml
    private static readonly ProtoId<TagPrototype> _helmetTag = "ReflectiveArmorHelmet";

    // Store original reflection probabilities to restore when set is broken
    private readonly Dictionary<EntityUid, float> _originalReflectProbs = new();

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
            // Store original reflection probability if not already stored
            if (TryComp<ReflectComponent>(args.Equipment, out var reflect) && !_originalReflectProbs.ContainsKey(args.Equipment))
            {
                _originalReflectProbs[args.Equipment] = reflect.ReflectProb;
            }
            
            CheckAllReflectiveSets(args.Equipee);
        }
    }

    private void OnDidUnequip(DidUnequipEvent args)
    {
        // Restore original reflection probability for unequipped item (only stored if it was part of the set)
        if (_originalReflectProbs.TryGetValue(args.Equipment, out var originalProb) && 
            TryComp<ReflectComponent>(args.Equipment, out var reflect))
        {
            reflect.ReflectProb = originalProb;
            Dirty(args.Equipment, reflect);
            
            // Update remaining equipped items
            CheckAllReflectiveSets(args.Equipee);
        }
    }

    /// <summary>
    /// Checks all equipped items for the set bonus and applies correct reflection probability.
    /// </summary>
    private void CheckAllReflectiveSets(EntityUid wearer)
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
            // Set both items to 100% reflection when full set is worn
            if (TryComp<ReflectComponent>(vestEntity.Value, out var vestReflect))
            {
                vestReflect.ReflectProb = 1.0f;
                Dirty(vestEntity.Value, vestReflect);
            }
            if (TryComp<ReflectComponent>(helmetEntity.Value, out var helmetReflect))
            {
                helmetReflect.ReflectProb = 1.0f;
                Dirty(helmetEntity.Value, helmetReflect);
            }
        }
        else
        {
            // Restore original reflection values when set is incomplete
            if (vestEntity.HasValue && 
                _originalReflectProbs.TryGetValue(vestEntity.Value, out var vestOriginal) &&
                TryComp<ReflectComponent>(vestEntity.Value, out var vestReflect))
            {
                vestReflect.ReflectProb = vestOriginal;
                Dirty(vestEntity.Value, vestReflect);
            }
            
            if (helmetEntity.HasValue && 
                _originalReflectProbs.TryGetValue(helmetEntity.Value, out var helmetOriginal) &&
                TryComp<ReflectComponent>(helmetEntity.Value, out var helmetReflect))
            {
                helmetReflect.ReflectProb = helmetOriginal;
                Dirty(helmetEntity.Value, helmetReflect);
            }
        }
    }
}
