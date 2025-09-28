using Content.Shared._Starlight.Antags.Vampires;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Content.Shared.Physics;
using Content.Shared.Speech.Muting;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

namespace Content.Server._Starlight.Antags.Vampires;

public sealed partial class VampireSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    // Damage type caches
    private readonly Dictionary<string, DamageTypePrototype?> _damageTypeCache = new();
    private readonly Dictionary<string, DamageGroupPrototype?> _damageGroupCache = new();
    private static readonly TimeSpan _shadowSnareBlindDuration = TimeSpan.FromSeconds(20);
    private record struct ShadowSnareData(EntityUid TrapUid, int Health);
    private readonly Dictionary<EntityUid, ShadowSnareData> _shadowSnares = new();
    private readonly Dictionary<EntityUid, List<EntityUid>> _playerShadowSnares = new();

    private void InitializeAbilities()
    {
        SubscribeLocalEvent<VampireComponent, VampireToggleFangsActionEvent>(OnToggleFangs);

        SubscribeLocalEvent<VampireComponent, VampireGlareActionEvent>(OnGlare);

        SubscribeLocalEvent<VampireComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<VampireComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
        SubscribeLocalEvent<VampireComponent, VampireDrinkBloodDoAfterEvent>(OnDrinkDoAfter);

        SubscribeLocalEvent<VampireComponent, VampireRejuvenateIActionEvent>(OnRejuvenateI);
        SubscribeLocalEvent<VampireComponent, VampireRejuvenateIIActionEvent>(OnRejuvenateII);

        SubscribeLocalEvent<VampireComponent, VampireClassSelectActionEvent>(OnClassSelect);

        Subs.BuiEvents<VampireComponent>(VampireClassUiKey.Key, subs =>
        {
            subs.Event<VampireClassChosenBuiMsg>(OnVampireClassChosen);
            subs.Event<VampireClassClosedBuiMsg>(OnVampireClassClosed);
        });
    }

    #region Helper Methods

    private DamageTypePrototype? GetCachedDamageType(string protoId)
    {
        if (_damageTypeCache.TryGetValue(protoId, out var cached))
            return cached;

        var result = _proto.TryIndex<DamageTypePrototype>(protoId, out var proto) ? proto : null;
        _damageTypeCache[protoId] = result;
        return result;
    }

    private DamageGroupPrototype? GetCachedDamageGroup(string protoId)
    {
        if (_damageGroupCache.TryGetValue(protoId, out var cached))
            return cached;

        var result = _proto.TryIndex<DamageGroupPrototype>(protoId, out var proto) ? proto : null;
        _damageGroupCache[protoId] = result;
        return result;
    }

    /// <summary>
    /// Apply damage to target
    /// </summary>
    /// <param name="target">Target Entity</param>
    /// <param name="damageTypeId">Damage Type</param>
    /// <param name="amount">Amount of damage</param>
    /// <param name="origin">Who applied damage to target</param>
    private void ApplyDamage(EntityUid target, string damageTypeId, float amount, EntityUid? origin = null)
    {
        if (!TryComp<DamageableComponent>(target, out var damageable))
            return;

        var damageType = GetCachedDamageType(damageTypeId);
        if (damageType == null)
            return;

        var spec = new DamageSpecifier();
        spec += new DamageSpecifier(damageType, FixedPoint2.New(amount));
        EntityManager.System<DamageableSystem>().TryChangeDamage(target, spec, true, origin: origin);
    }

    /// <summary>
    /// Apply healing to target
    /// </summary>
    /// <param name="target">Target Entity</param>
    /// <param name="damageTypeOrGroupId">Damage Type or Group ID</param>
    /// <param name="amount">Amount of damage</param>
    /// <param name="isGroup">Do we need to use Group ID instead of Damage Type?</param>
    private void ApplyHealing(EntityUid target, string damageTypeOrGroupId, float amount, bool isGroup = false)
    {
        if (!TryComp<DamageableComponent>(target, out var damageable))
            return;

        var spec = new DamageSpecifier();

        if (isGroup)
        {
            var damageGroup = GetCachedDamageGroup(damageTypeOrGroupId);
            if (damageGroup != null)
                spec += new DamageSpecifier(damageGroup, -FixedPoint2.New(amount));
        }
        else
        {
            var damageType = GetCachedDamageType(damageTypeOrGroupId);
            if (damageType != null)
                spec += new DamageSpecifier(damageType, -FixedPoint2.New(amount));
        }

        if (spec.DamageDict.Count > 0)
            EntityManager.System<DamageableSystem>().TryChangeDamage(target, spec, true);
    }

    /// <summary>
    /// Check if tile coordinates are valid and not blocked
    /// </summary>
    private bool IsValidTile(EntityCoordinates coords, EntityUid? gridUid = null, MapGridComponent? gridComp = null)
    {
        gridUid ??= _transform.GetGrid(coords);
        if (gridUid == null)
            return false;

        if (gridComp == null && !TryComp(gridUid.Value, out gridComp))
            return false;

        if (!_map.TryGetTileRef(gridUid.Value, gridComp, coords, out var tileRef))
            return false;

        return !_turf.IsSpace(tileRef) &&
               !_turf.IsTileBlocked(tileRef, CollisionGroup.Impassable | CollisionGroup.Opaque) &&
               !IsTileBlockedByEntities(coords);
    }

    /// <summary>
    /// Validates if vampire has required class for the ability
    /// </summary>
    private bool ValidateVampireClass(EntityUid uid, VampireComponent comp, VampireClassType requiredClass)
    {
        if (comp.ChosenClass != requiredClass)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Common validation for vampire abilities 
    /// component check + class validation + action cost
    /// </summary>
    private bool ValidateVampireAbility(EntityUid uid, [NotNullWhen(true)] out VampireComponent? comp, VampireClassType? requiredClass = null, EntityUid? actionEntity = null)
    {
        if (!TryComp(uid, out comp))
            return false;

        if (requiredClass.HasValue && !ValidateVampireClass(uid, comp, requiredClass.Value))
            return false;

        if (actionEntity.HasValue && !CheckAndConsumeBloodCost(uid, comp, actionEntity.Value))
            return false;

        return true;
    }

    /// <summary>
    /// Unified blood cost checking and consumption
    /// </summary>
    private bool CheckAndConsumeBloodCost(EntityUid uid, VampireComponent comp, EntityUid? actionEntity = null, int bloodCost = 0)
    {

        if (bloodCost <= 0 && actionEntity != null && TryComp<VampireActionComponent>(actionEntity.Value, out var vac))
        {
            if (comp.TotalBlood < vac.BloodToUnlock)
                return false;

            if (vac.BloodCost > 0)
                bloodCost = (int)vac.BloodCost;
        }
        else if (bloodCost <= 0)
        {
            _sawmill?.Error($"No action entity or no VampireActionComponent found for: {uid.ToString()}!");
            return false;
        }

        if (bloodCost <= 0)
            return true;

        if (comp.DrunkBlood < bloodCost)
        {
            _popup.PopupEntity(Loc.GetString("vampire-not-enough-blood"), uid, uid);
            return false;
        }

        comp.DrunkBlood -= bloodCost;
        Dirty(uid, comp);
        UpdateVampireAlert(uid);
        return true;
    }

    #endregion

    private void OnToggleFangs(EntityUid uid, VampireComponent comp, ref VampireToggleFangsActionEvent args)
    {
        if (args.Handled)
            return;

        comp.FangsExtended = !comp.FangsExtended;
        if (!comp.FangsExtended)
            comp.IsDrinking = false;

        if (_actions.GetAction(comp.Actions.ToggleFangsActionEntity) is { } action)
            _actions.SetToggled(action.AsNullable(), comp.FangsExtended);
        Dirty(uid, comp);
        args.Handled = true;
    }

    private void OnAfterInteract(EntityUid uid, VampireComponent comp, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || !comp.FangsExtended)
            return;

        if (args.Target == null)
            return;

        var target = args.Target.Value;

        if (target == uid)
            return;

        if (!HasComp<BloodstreamComponent>(target))
            return;

        if (IsMouthBlocked(uid))
        {
            _popup.PopupEntity("Your mouth is covered!", uid, uid);
            return;
        }

        StartDrinkDoAfter(uid, comp, target, showPopup: true);
        args.Handled = true;
    }

    private void OnBeforeInteractHand(EntityUid uid, VampireComponent comp, ref BeforeInteractHandEvent args)
    {
        if (args.Handled || !comp.FangsExtended)
            return;

        var target = args.Target;
        if (!Exists(target) || !HasComp<BloodstreamComponent>(target))
            return;

        if (target == uid)
            return;

        if (IsMouthBlocked(uid))
        {
            _popup.PopupEntity("Your mouth is covered!", uid, uid); // Rinary - must to be moved into locale
            return;
        }

        StartDrinkDoAfter(uid, comp, target, showPopup: true);
        args.Handled = true;
    }

    private void OnDrinkDoAfter(EntityUid uid, VampireComponent comp, ref VampireDrinkBloodDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            comp.IsDrinking = false;
            return;
        }

        if (!comp.FangsExtended || args.Args.Target == null || !HasComp<BloodstreamComponent>(args.Args.Target.Value))
            return;

        var target = args.Args.Target.Value;

        if (!comp.BloodDrunkFromTargets.TryGetValue(target, out var drunkFromTarget))
            drunkFromTarget = 0;

        if (drunkFromTarget >= comp.MaxBloodPerTarget)
        {
            _popup.PopupEntity($"You have already drunk {comp.MaxBloodPerTarget} units of blood from this target.", uid, uid, Shared.Popups.PopupType.MediumCaution);
            comp.IsDrinking = false;
            return;
        }

        var maxCanDrink = comp.MaxBloodPerTarget - drunkFromTarget;
        var actualSipAmount = MathF.Min(comp.SipAmount, maxCanDrink);

        if (_blood.TryModifyBloodLevel(target, -actualSipAmount * 2))
        {
            comp.DrunkBlood += (int)actualSipAmount;
            comp.TotalBlood += (int)actualSipAmount;

            if (!comp.BloodDrunkFromTargets.ContainsKey(target))
                comp.BloodDrunkFromTargets[target] = 0;
            comp.BloodDrunkFromTargets[target] += (int)actualSipAmount;

            comp.BloodFullness = MathF.Min(comp.MaxBloodFullness, comp.BloodFullness + actualSipAmount);

            // Base healing
            ApplyHealing(uid, _bruteGroupId, 2, true);
            ApplyHealing(uid, _burnGroupId, 2, true);
            ApplyHealing(uid, _poisonTypeId, 4);
            ApplyHealing(uid, _oxyLossTypeId, 10);

            // Threshold bonuses at 300 TotalBlood
            if (comp.TotalBlood >= 300)
            {
                switch (comp.ChosenClass)
                {
                    case VampireClassType.Hemomancer:
                        comp.BloodFullness = MathF.Min(comp.MaxBloodFullness, comp.BloodFullness + 5f);
                        break;
                    case VampireClassType.Umbrae:
                        TryBreakRandomLightNear(uid, 8f);
                        break;
                    case VampireClassType.Gargantua:
                        ApplyHealing(uid, _bruteGroupId, 3, true);
                        ApplyHealing(uid, _burnGroupId, 3, true);
                        break;
                    // Dantalion - slaves heal 3 brute and burn + 5 oxygen loss
                }
            }

            UpdateFullPower(uid, comp);

            var biteSound = new SoundPathSpecifier("/Audio/Effects/bite.ogg");
            _audio.PlayPvs(biteSound, target, AudioParams.Default.WithVolume(-4f));
            var targetCoords = Transform(target).Coordinates;
            Spawn("WeaponArcBite", targetCoords);

            Dirty(uid, comp);

            UpdateVampireAlert(uid);
            UpdateVampireFedAlert(uid, comp);

            EnsureRejuvenateUpgrade(uid, comp);
            _popup.PopupEntity(Loc.GetString("vampire-drink-end", ("target", Identity.Entity(target, EntityManager))), uid, uid);

            var currentDrunkFromTarget = comp.BloodDrunkFromTargets.GetValueOrDefault(target, 0);
            if (comp.FangsExtended && currentDrunkFromTarget < comp.MaxBloodPerTarget)
            {
                comp.IsDrinking = false;
                StartDrinkDoAfter(uid, comp, target, showPopup: false);
            }
            else
            {
                comp.IsDrinking = false;
                if (currentDrunkFromTarget >= comp.MaxBloodPerTarget)
                    _popup.PopupEntity($"You have drunk the maximum amount of blood from this target ({comp.MaxBloodPerTarget} units).", uid, uid);
            }
        }
        else
        {
            comp.IsDrinking = false;
        }
    }

    partial void UpdateVampireAlert(EntityUid uid)
        => _alerts.ShowAlert(uid, VampireBloodAlertId);

    partial void UpdateVampireFedAlert(EntityUid uid, VampireComponent? comp)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        var frac = comp.MaxBloodFullness <= 0f ? 0f : comp.BloodFullness / comp.MaxBloodFullness;
        var sev = (short)Math.Clamp((int)MathF.Ceiling(frac * 4f) + 1, 1, 5);
        _alerts.ShowAlert(uid, VampireFedAlertId, sev);
    }

    private void StartDrinkDoAfter(EntityUid uid, VampireComponent comp, EntityUid target, bool showPopup)
    {
        if (comp.IsDrinking)
            return;

        if (IsMouthBlocked(uid))
        {
            if (showPopup)
                _popup.PopupEntity("Your mouth is covered!", uid, uid);
            return;
        }

        var dargs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(1.25), new VampireDrinkBloodDoAfterEvent(), uid, target)
        {
            DistanceThreshold = 1.5f,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        if (_doAfter.TryStartDoAfter(dargs))
        {
            comp.IsDrinking = true;
            if (showPopup)
                _popup.PopupEntity(Loc.GetString("vampire-drink-start", ("target", Identity.Entity(target, EntityManager))), uid, uid);
        }
    }

    private void OnGlare(EntityUid uid, VampireComponent comp, ref VampireGlareActionEvent args)
    {
        if (args.Handled)
            return;

        if (!CheckAndConsumeBloodCost(uid, comp, comp.Actions.GlareActionEntity))
            return;

        // Find targets within 1 tile around the vampire
        var targets = _lookup.GetEntitiesInRange(uid, args.Range, LookupFlags.Dynamic | LookupFlags.Sundries);

        var ourXform = Transform(uid);
        var ourDirection = ourXform.LocalRotation.ToVec();
        var ourPosition = ourXform.LocalPosition;

        foreach (var target in targets)
        {
            if (target == uid)
                continue;

            var targetPosition = Transform(target).LocalPosition;
            var vectorToTarget = Vector2.Normalize(targetPosition - ourPosition);

            var dot = Vector2.Dot(ourDirection, vectorToTarget);
            
            if (!TryComp<StaminaComponent>(target, out var stam))
                continue;

            var knockedDown = HasComp<KnockedDownComponent>(target);

            // If target in front
            if (dot > 0.7f && !knockedDown)
            {
                _stun.TryAddParalyzeDuration(target, TimeSpan.FromSeconds(2));

                _stamina.TakeStaminaDamage(target, args.FrontStaminaDamage, stam, source: uid);

                // Mute for 8 second
                EnsureComp<MutedComponent>(target);
                Timer.Spawn(TimeSpan.FromSeconds(args.MuteDuration), () =>
                {
                    if (Exists(target))
                        RemComp<MutedComponent>(target);
                });

                StartGlareDotEffect(target, uid, args.DotStaminaDamage, 0, true);

                return; 
            }
            // If target behind
            else if (dot < -0.7f && !knockedDown)
            {
                _stamina.TakeStaminaDamage(target, args.BehindStaminaDamage, stam, source: uid);
            }
            else
            {
                _stun.TryAddParalyzeDuration(target, TimeSpan.FromSeconds(4));

                _stamina.TakeStaminaDamage(target, args.SideStaminaDamage, stam, source: uid);
            }

            // Start DOT effect with limited ticks
            StartGlareDotEffect(target, uid, args.DotStaminaDamage, 0, false);
        }

        args.Handled = true;
    }

    private void StartGlareDotEffect(EntityUid target, EntityUid source, float damage, int tickCount, bool doStaminaDamage)
    {
        const int MaxTicks = 10;

        if (tickCount >= MaxTicks || !Exists(target) || !Exists(source))
            return;

        if (doStaminaDamage && TryComp<StaminaComponent>(target, out var stam) && !stam.Critical)
            _stamina.TakeStaminaDamage(target, damage, stam, source: source);

        Timer.Spawn(TimeSpan.FromSeconds(1), () => StartGlareDotEffect(target, source, damage, tickCount + 1, doStaminaDamage));
    }

    private void OnRejuvenateI(EntityUid uid, VampireComponent comp, ref VampireRejuvenateIActionEvent args)
    {
        if (args.Handled)
            return;

        if (!CheckAndConsumeBloodCost(uid, comp, comp.Actions.RejuvenateIActionEntity))
            return;

        if (TryComp<StaminaComponent>(uid, out var stamina))
        {
            stamina.StaminaDamage = 0f;
            _stamina.ExitStamCrit(uid, stamina);
            _stamina.AdjustStatus((uid, stamina));
            RemComp<ActiveStaminaComponent>(uid);
            _statusEffects.TryRemoveStatusEffect(uid, SharedStaminaSystem.StaminaLow);
            _stamina.UpdateStaminaVisuals((uid, stamina));
            Dirty(uid, stamina);
        }
        _stun.TryUnstun(uid);
        _stun.ForceStandUp(uid);

        args.Handled = true;
    }

    private void OnRejuvenateII(EntityUid uid, VampireComponent comp, ref VampireRejuvenateIIActionEvent args)
    {
        if (args.Handled)
            return;

        if (!CheckAndConsumeBloodCost(uid, comp, comp.Actions.RejuvenateIIActionEntity))
            return;

        if (TryComp<StaminaComponent>(uid, out var stamina))
        {
            stamina.StaminaDamage = 0f;
            _stamina.ExitStamCrit(uid, stamina);
            _stamina.AdjustStatus((uid, stamina));
            RemComp<ActiveStaminaComponent>(uid);
            _statusEffects.TryRemoveStatusEffect(uid, SharedStaminaSystem.StaminaLow);
            _stamina.UpdateStaminaVisuals((uid, stamina));
            Dirty(uid, stamina);
        }
        _stun.TryUnstun(uid);
        _stun.ForceStandUp(uid);

        // Purge 10u of harmful reagents
        FixedPoint2 MaxRemove = FixedPoint2.New(10);

        if (!TryComp<BloodstreamComponent>(uid, out var blood)
            || blood.ChemicalSolution is not { } solEnt
            || !TryComp(solEnt, out SolutionComponent? solution))
            return;

        var toRemove = FixedPoint2.Zero;

        foreach (var quant in solution.Solution.Contents.ToArray())
        {
            if (toRemove >= MaxRemove)
                break;

            if (!_proto.TryIndex<ReagentPrototype>(quant.Reagent.Prototype, out var proto))
                continue;

            if (proto.Metabolisms == null || !proto.Metabolisms.Keys.Any(k => k.Id.Equals("Poison", StringComparison.OrdinalIgnoreCase)))
                continue;

            var remaining = MaxRemove - toRemove;
            var removeAmt = FixedPoint2.Min(quant.Quantity, remaining);

            _solution.RemoveReagent(solEnt, quant.Reagent, removeAmt);
            toRemove += removeAmt;
        }

        // Heal over-time in 5 cycles, 3.5s apart: per tick heal Oxy 5, Brute/Burn/Toxin 4
        const int TotalTicks = 5;
        var interval = TimeSpan.FromSeconds(3.5);

        void DoHealTick(int remaining)
        {
            if (!Exists(uid))
                return;

            ApplyHealing(uid, _bruteGroupId, 4, true);
            ApplyHealing(uid, _burnGroupId, 4, true);
            ApplyHealing(uid, _poisonTypeId, 4);
            ApplyHealing(uid, _oxyLossTypeId, 5);

            if (remaining > 1)
                Timer.Spawn(interval, () => DoHealTick(remaining - 1));
        }

        DoHealTick(TotalTicks);

        args.Handled = true;
    }

    private void OnClassSelect(EntityUid uid, VampireComponent comp, ref VampireClassSelectActionEvent args)
    {
        if (args.Handled)
            return;

        if (comp.ChosenClass != VampireClassType.None)
        {
            args.Handled = true;
            return;
        }

        OpenClassUi(uid, comp);
    }

    private bool CheckAndConsumeActionCost(EntityUid uid, VampireComponent comp, EntityUid? actionEntity)
        => CheckAndConsumeBloodCost(uid, comp, actionEntity);

    /// <summary>
    /// Checks if a tile position is blocked by solid entities(walls etc.)
    /// </summary>
    private bool IsTileBlockedByEntities(EntityCoordinates coords)
    {
        // Check for anchored entities in this position that block movement
        foreach (var ent in _lookup.GetEntitiesIntersecting(_transform.ToMapCoordinates(coords), LookupFlags.Static))
        {
            // Skip non anchored entities
            if (!TryComp(ent, out TransformComponent? entTransform) || !entTransform.Anchored)
                continue;

            // Check if entity has a physics component with impassable collision
            if (TryComp<PhysicsComponent>(ent, out var physics) &&
                physics.CanCollide &&
                (physics.CollisionMask & (int)(CollisionGroup.Impassable | CollisionGroup.Opaque)) != 0)
            {
                return true;
            }

            // Check for door components that typically block movement
            if (HasComp<Shared.Doors.Components.DoorComponent>(ent))
            {
                return true;
            }

            // Check entity prototype names for common wall/structure types
            if (TryComp(ent, out MetaDataComponent? meta) &&
                meta.EntityPrototype?.ID != null)
            {
                var id = meta.EntityPrototype.ID.ToLower();
                if (id.Contains("wall") || id.Contains("grille") || id.Contains("window") ||
                    id.Contains("reinforced") || id.Contains("solid"))
                {
                    return true;
                }
            }
        }
        return false;
    }

    #region Full Power, Passives

    private void UpdateFullPower(EntityUid uid, VampireComponent comp)
    {
        int uniqueHumanoids = 0;
        foreach (var kv in comp.BloodDrunkFromTargets.Keys)
        {
            if (!Exists(kv))
                continue;
            if (HasComp<HumanoidAppearanceComponent>(kv))
                uniqueHumanoids++;
        }
        comp.UniqueHumanoidVictims = uniqueHumanoids;
        var prev = comp.FullPower;
        comp.FullPower = comp.TotalBlood > 1000 && uniqueHumanoids >= 8;
        if (!prev && comp.FullPower)
            _popup.PopupEntity("Your vampiric essence surges – full power achieved!", uid, uid);
        Dirty(uid, comp);
    }

    private void TryBreakRandomLightNear(EntityUid uid, float range)
    {
        var center = Transform(uid).Coordinates;
        var list = new List<EntityUid>();
        foreach (var ent in _lookup.GetEntitiesInRange(center, range))
        {
            if (TryComp<Shared.Light.Components.PoweredLightComponent>(ent, out var light) && light.On)
                list.Add(ent);
        }
        if (list.Count == 0)
            return;
        var pick = _rand.Pick(list);
        if (TryComp<Shared.Light.Components.PoweredLightComponent>(pick, out var pl))
            EntityManager.System<Light.EntitySystems.PoweredLightSystem>().SetState(pick, false, pl);
    }

    private bool IsMouthBlocked(EntityUid uid)
    {
        if (!HasComp<InventoryComponent>(uid))
            return false;
        var slots = new[] { "mask", "head" };
        foreach (var slot in slots)
        {
            if (_inventory.TryGetSlotEntity(uid, slot, out var ent) && HasComp<IngestionBlockerComponent>(ent.Value))
                return true;
        }
        return false;
    }

    #endregion
}
