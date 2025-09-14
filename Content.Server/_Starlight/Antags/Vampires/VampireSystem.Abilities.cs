using Content.Shared._Starlight.Antags.Vampires;
using Content.Shared.Interaction;
using Content.Shared.Body.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Speech.Muting;
using Robust.Shared.Timing;
using Content.Shared.FixedPoint;
using Content.Shared.Wieldable.Components;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using System.Numerics;
using Content.Shared.Humanoid;
using Robust.Shared.Physics.Components;
using System.Linq;
using Content.Shared.Stealth.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Flash.Components;
using Content.Shared.Charges.Components;
using Content.Shared.Hands.Components; 
using Robust.Shared.Containers;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server._Starlight.Antags.Vampires;

public sealed partial class VampireSystem
{
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    // Ability constants
    private const float GlareRange = 1f;
    private const float GlareInitialStaminaDamage = 30f;
    private const float GlareDotStaminaDamage = 15f;
    private const int GlareMuteDuration = 8;
    private const float BloodEruptionRange = 10f;
    private const int BloodEruptionDamage = 15;
    private const float BloodEruptionTargetRange = 2f;
    private const float SanguinePoolBloodDripInterval = 1.0f;
    private const int SanguinePoolDuration = 8;
    private const float BloodBringersRiteRange = 7f;
    private const float BloodBringersRiteDamage = 5f;
    private const float BloodBringersRiteMaxTargetBlood = 10f;
    private const float BloodBringersRiteHealBrute = 8f;
    private const float BloodBringersRiteHealBurn = 2f;
    private const float BloodBringersRiteHealStamina = 15f;
    private const float BloodBringersRiteToggleInterval = 2f;
    private const int BloodBringersRiteCost = 10;

    // ShadowSnare
    private const int ShadowSnareBaseHealth = 200;
    private const float ShadowSnareTickInterval = 2f;
    private const float ShadowSnareDamageDark = 0f;
    private const float ShadowSnareDamageNormal = 10f;
    private const float ShadowSnareDamageBright = 25f;
    private const float ShadowSnareTriggerBrute = 20f;
    private const float ShadowSnareSlowMultiplier = 0.5f;
    private const int MaxShadowSnaresPerPlayer = 3;
    private const float ShadowSnareStealthVisibility = -0.3f;
    private const float ShadowSnarePositionOffset = 0.5f;

    // Extinguish constants
    private const float ExtinguishRadius = 8f;

    // Shadow Boxing constants
    private const float ShadowBoxingInterval = 0.4f;
    private const int ShadowBoxingBrutePerTick = 6;
    private const float ShadowBoxingRange = 2.1f;

    // Hemomancer Tendrils Configuration
    private const float TendrilPositionOffset = 0.5f;
    private const float TendrilTargetRange = 0.9f;
    private const float TendrilVisualSpawnDelay = 0.5f;
    private const float TendrilMinDelay = 0.0f;
    private const float TendrilMinSlowDuration = 0.1f;
    private const float TendrilMinSlowMultiplier = 0.05f;

    // Eternal Darkness Configuration
    private const int EternalDarknessMaxTicks = 360;
    private const int EternalDarknessBloodPerTick = 5;
    private const float EternalDarknessFreezeRadius = 6f;
    private const float EternalDarknessLightOffRadius = 4f;
    private const float EternalDarknessTargetFreezeTemp = 233.15f;
    private const int EternalDarknessTempDropInterval = 2;
    private const float EternalDarknessTempDropPerInterval = 60f;

    // Damage type caches
    private readonly Dictionary<string, DamageTypePrototype?> _damageTypeCache = new();
    private readonly Dictionary<string, DamageGroupPrototype?> _damageGroupCache = new();
    private static readonly TimeSpan _shadowSnareBlindDuration = TimeSpan.FromSeconds(20);
    private record struct ShadowSnareData(EntityUid TrapUid, int Health);
    private readonly Dictionary<EntityUid, ShadowSnareData> _shadowSnares = new();
    private readonly Dictionary<EntityUid, List<EntityUid>> _playerShadowSnares = new();


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
    /// Apply healing of specific type to target
    /// </summary>
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

        if (gridComp == null && !TryComp<MapGridComponent>(gridUid.Value, out gridComp))
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
        comp = null;

        if (!TryComp<VampireComponent>(uid, out comp))
            return false;

        if (requiredClass.HasValue && !ValidateVampireClass(uid, comp, requiredClass.Value))
            return false;

        if (actionEntity.HasValue && !CheckAndConsumeActionCost(uid, comp, actionEntity.Value))
            return false;

        return true;
    }

    /// <summary>
    /// Unified blood cost checking and consumption
    /// </summary>
    private bool CheckAndConsumeBloodCost(EntityUid uid, VampireComponent comp, int bloodCost, EntityUid? actionEntity = null)
    {

        if (actionEntity != null && TryComp<VampireActionComponent>(actionEntity.Value, out var vac))
        {
            if (comp.TotalBlood < vac.BloodToUnlock)
            {
                return false;
            }

            if (vac.BloodCost > 0)
                bloodCost = (int)vac.BloodCost;
        }
        else
        {
            _popup.PopupEntity("DEBUG: No action entity or no VampireActionComponent found!", uid, uid);
        }

        if (bloodCost <= 0)
        {
            return true;
        }

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
    partial void SubscribeAbilities()
    {
        SubscribeLocalEvent<VampireComponent, VampireToggleFangsActionEvent>(OnToggleFangs);

        SubscribeLocalEvent<VampireComponent, VampireGlareActionEvent>(OnGlare);

        SubscribeLocalEvent<VampireComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<VampireComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
        SubscribeLocalEvent<VampireComponent, VampireDrinkBloodDoAfterEvent>(OnDrinkDoAfter);

        SubscribeLocalEvent<VampireComponent, VampireRejuvenateIActionEvent>(OnRejuvenateI);
        SubscribeLocalEvent<VampireComponent, VampireRejuvenateIIActionEvent>(OnRejuvenateII);

        SubscribeLocalEvent<VampireComponent, VampireHemomancerClawsActionEvent>(OnHemomancerClaws);
        SubscribeLocalEvent<VampireHemomancerTendrilsActionEvent>(OnHemomancerTendrils);
        SubscribeLocalEvent<VampireBloodBarrierActionEvent>(OnBloodBarrier);
        SubscribeLocalEvent<VampireComponent, VampireSanguinePoolActionEvent>(OnSanguinePool);
        SubscribeLocalEvent<VampireComponent, VampireBloodEruptionActionEvent>(OnBloodEruption);
        SubscribeLocalEvent<VampireComponent, VampireBloodBringersRiteActionEvent>(OnBloodBringersRite);

        SubscribeLocalEvent<VampireComponent, VampireCloakOfDarknessActionEvent>(OnCloackOfDarkness);

        SubscribeLocalEvent<ShadowSnareEnsnareComponent, ComponentShutdown>(OnShadowSnareEnsnareShutdown);

        SubscribeLocalEvent<VampireComponent, VampireShadowSnareActionEvent>(OnShadowSnare);
        SubscribeLocalEvent<VampireComponent, VampireDarkPassageActionEvent>(OnDarkPassage);
        SubscribeLocalEvent<VampireComponent, VampireExtinguishActionEvent>(OnExtinguish);
        SubscribeLocalEvent<VampireComponent, VampireEternalDarknessActionEvent>(OnEternalDarkness);
        SubscribeLocalEvent<VampireComponent, VampireShadowAnchorActionEvent>(OnShadowAnchor);
        SubscribeLocalEvent<VampireComponent, VampireShadowBoxingActionEvent>(OnShadowBoxing);
        SubscribeLocalEvent<ShadowSnareTrapComponent, InteractUsingEvent>(OnShadowSnareTrapInteractUsing);
        SubscribeLocalEvent<ShadowSnareTrapComponent, StartCollideEvent>(OnShadowSnareTrapCollide);

        SubscribeLocalEvent<VampireComponent, VampireClassSelectActionEvent>(OnClassSelect);

        Subs.BuiEvents<VampireComponent>(VampireClassUiKey.Key, subs =>
        {
            subs.Event<VampireClassChosenBuiMsg>(OnVampireClassChosen);
            subs.Event<VampireClassClosedBuiMsg>(OnVampireClassClosed);
        });
    }

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
            _popup.PopupEntity("Your mouth is covered!", uid, uid);
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

        if (_blood.TryModifyBloodLevel(target, (-actualSipAmount * 2)))
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

            // Rinary
            // Gargantua - увеличивает исцеление до 5 от травм и ожогов.
            // Dantalion - рабы востанавливают 3 травм-ожогов 5 гипоксии за глоток.
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

    // Rinary если сможешь поиграться с worldvec и сделать это, то вообще будешь зайкой
    /*
    Слепит в радиусе 1 тайла. Имеет два использования с интервалом в 2-3 секунды.
    В зависимости от направления взгляда и расстояния будут разные эффекты:

    Если жертва на том же месте, что и вампир, или сбоку, или лежит - она оглушиться на 4 секунды и нанесётся 40 стаминоурона

    Если жертва за спиной вампира - ей нанесётся 30 стаминоурона

    Если жертва перед вампиром - она оглушиться на 2 секунды, нанесётся 30 стаминоурона с последующим уроном (10 урона) по стамине до стаминокрита и не даст жертве говорить 8 секунд
    */
    private void OnGlare(EntityUid uid, VampireComponent comp, ref VampireGlareActionEvent args)
    {
        if (args.Handled)
            return;

        if (!CheckAndConsumeActionCost(uid, comp, comp.Actions.GlareActionEntity))
            return;

        // Find targets within 1 tile around the vampire
        var targets = _lookup.GetEntitiesInRange(uid, GlareRange, Robust.Shared.GameObjects.LookupFlags.Dynamic | Robust.Shared.GameObjects.LookupFlags.Sundries);

        foreach (var target in targets)
        {
            if (target == uid)
                continue;

            if (!TryComp<StaminaComponent>(target, out var stam))
                continue;

            _stun.TryAddParalyzeDuration(target, TimeSpan.FromSeconds(2));

            _stamina.TakeStaminaDamage(target, GlareInitialStaminaDamage, stam, source: uid);

            // Mute for 8 second
            EnsureComp<MutedComponent>(target);
            Timer.Spawn(TimeSpan.FromSeconds(GlareMuteDuration), () =>
            {
                if (Exists(target))
                    RemComp<MutedComponent>(target);
            });

            // Start DOT effect with limited ticks
            StartGlareDotEffect(target, uid, 0);
        }

        args.Handled = true;
    }

    private void StartGlareDotEffect(EntityUid target, EntityUid source, int tickCount)
    {
        const int MaxTicks = 10;

        if (tickCount >= MaxTicks || !Exists(target) || !Exists(source))
            return;

        if (!TryComp<StaminaComponent>(target, out var stam) || stam.Critical)
            return;

        _stamina.TakeStaminaDamage(target, GlareDotStaminaDamage, stam, source: source);

        Timer.Spawn(TimeSpan.FromSeconds(1), () => StartGlareDotEffect(target, source, tickCount + 1));
    }

    private void OnRejuvenateI(EntityUid uid, VampireComponent comp, ref VampireRejuvenateIActionEvent args)
    {
        if (args.Handled)
            return;

        if (!CheckAndConsumeActionCost(uid, comp, comp.Actions.RejuvenateIActionEntity))
            return;

        _stun.TryUnstun(uid);
        _stun.TryStanding(uid);

        args.Handled = true;
    }

    private void OnRejuvenateII(EntityUid uid, VampireComponent comp, ref VampireRejuvenateIIActionEvent args)
    {
        if (args.Handled)
            return;

        if (!CheckAndConsumeActionCost(uid, comp, comp.Actions.RejuvenateIIActionEntity))
            return;

        // Rinary не ебу что не так, помоги.
        // Пинаешь себя стан дубинкой, нажимаешь абилку и нихуя не происходит)
        // С первым rejivenate также
        _stun.TryUnstun(uid);
        _stun.TryStanding(uid);

        // Rinary супер не оптимизированнаное дерьмо код, зарефакторь пж пж
        // Не сможешь да и похуй
        // у меня лапки
        // Purge 10u of harmful reagents
        if (TryComp<BloodstreamComponent>(uid, out var blood))
        {
            var solEnt = blood.ChemicalSolution;
            if (solEnt != null && TryComp(solEnt.Value, out Shared.Chemistry.Components.SolutionComponent? solution))
            {
                var toRemove = FixedPoint2.Zero;
                foreach (var quant in solution.Solution.Contents.ToArray())
                {
                    if (toRemove >= FixedPoint2.New(10))
                        break;

                    if (!_proto.TryIndex<Shared.Chemistry.Reagent.ReagentPrototype>(quant.Reagent.Prototype, out var proto))
                        continue;

                    var harmful = false;
                    if (proto.Metabolisms != null)
                    {
                        foreach (var kv in proto.Metabolisms)
                        {
                            if (kv.Key.Id.Equals("Poison", StringComparison.OrdinalIgnoreCase))
                            {
                                harmful = true;
                                break;
                            }
                        }
                    }

                    if (!harmful)
                        continue;

                    var remaining = FixedPoint2.New(10) - toRemove;
                    var removeAmt = FixedPoint2.Min(quant.Quantity, remaining);
                    EntityManager.System<Shared.Chemistry.EntitySystems.SharedSolutionContainerSystem>().RemoveReagent(solEnt.Value, quant.Reagent, removeAmt);
                    toRemove += removeAmt;
                }
            }
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

    private void OnHemomancerClaws(EntityUid uid, VampireComponent comp, ref VampireHemomancerClawsActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ValidateVampireAbility(uid, out var validatedComp, VampireClassType.Hemomancer, comp.Actions.HemomancerClawsActionEntity))
            return;

        comp = validatedComp;

        if (comp.SpawnedClaws != null && EntityManager.EntityExists(comp.SpawnedClaws.Value))
        {
            EntityManager.QueueDeleteEntity(comp.SpawnedClaws.Value);
            comp.SpawnedClaws = null;
        }

        var coords = Transform(uid).Coordinates;
        var claws = EntityManager.SpawnEntity("VampiricClawsItem", coords);
        comp.SpawnedClaws = claws;
 
        if (!_hands.TryPickupAnyHand(uid, claws))
        {
            if (!_hands.TryForcePickupAnyHand(uid, claws))
            {
                if (TryComp<HandsComponent>(uid, out var handsComp))
                {
                    _wieldable.UnwieldAll((uid, handsComp), force: true);
                    foreach (var handName in handsComp.Hands.Keys.ToArray())
                        _hands.TryDrop((uid, handsComp), handName, checkActionBlocker: false);
                }

                _hands.TryPickupAnyHand(uid, claws);
            }
        }

        // Auto-wield if the claws have a wieldable component and are in hand now.
        if (TryComp<WieldableComponent>(claws, out var wieldable) && _hands.IsHolding(uid, claws, out _))
        {
            _wieldable.TryWield(claws, wieldable, uid);
        }

        args.Handled = true;
    }

    private void OnHemomancerTendrils(VampireHemomancerTendrilsActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<VampireComponent>(args.Performer, out var comp))
            return;

        if (!ValidateVampireClass(args.Performer, comp, VampireClassType.Hemomancer))
            return;

        if (!CheckAndConsumeActionCost(args.Performer, comp, comp.Actions.HemomancerTendrilsActionEntity))
            return;

        args.Handled = true;

        var targetCoords = args.Target;
        var tileCoords = targetCoords.WithPosition(targetCoords.Position.Floored() + new Vector2(TendrilPositionOffset, TendrilPositionOffset));

        if (!ValidateTendrilTarget(tileCoords, args.Performer))
            return;

        if (args.SpawnVisuals)
        {
            SpawnTendrilVisuals(tileCoords);
        }

        ScheduleTendrilEffect(args, tileCoords);
    }

    private bool ValidateTendrilTarget(EntityCoordinates tileCoords, EntityUid performer)
    {
        if (_transform.GetGrid(tileCoords) is not { } gridUid ||
            !TryComp<MapGridComponent>(gridUid, out var gridComp) ||
            !IsValidTile(tileCoords, gridUid, gridComp))
        {
            _popup.PopupEntity("Cannot cast there.", performer, performer);
            return false;
        }
        return true;
    }

    private void SpawnTendrilVisuals(EntityCoordinates tileCoords)
    {
        var gridUid = _transform.GetGrid(tileCoords);
        if (gridUid == null || !TryComp<MapGridComponent>(gridUid.Value, out var gridComp))
            return;

        foreach (var offset in _tendrilOffsets)
        {
            var coords = tileCoords.Offset(offset);
            if (IsValidTile(coords, gridUid.Value, gridComp))
            {
                EntityManager.SpawnEntity("VampireBloodTendrilVisual", coords);
            }
        }
    }

    private void ScheduleTendrilEffect(VampireHemomancerTendrilsActionEvent args, EntityCoordinates tileCoords)
    {
        var delay = TimeSpan.FromSeconds(Math.Max(TendrilMinDelay, args.Delay));
        var slowDuration = TimeSpan.FromSeconds(Math.Max(TendrilMinSlowDuration, args.SlowDuration));
        var slowMultiplier = MathF.Max(TendrilMinSlowMultiplier, args.SlowMultiplier);
        var toxinDamage = args.ToxinDamage;
        var performerUid = args.Performer;

        var gridUid = _transform.GetGrid(tileCoords);
        if (gridUid == null || !TryComp<MapGridComponent>(gridUid.Value, out var gridComp))
            return;

        Timer.Spawn(delay, () => ExecuteTendrilEffect(performerUid, tileCoords, gridUid.Value, gridComp, toxinDamage, slowDuration, slowMultiplier));
    }

    private void ExecuteTendrilEffect(EntityUid performerUid, EntityCoordinates targetCoords, EntityUid gridUid, MapGridComponent gridComp,
        float toxinDamage, TimeSpan slowDuration, float slowMultiplier)
    {
        if (!Exists(performerUid))
            return;

        // Spawn blood puddle at center
        if (IsValidTile(targetCoords, gridUid, gridComp))
        {
            Spawn("PuddleBlood", targetCoords);
        }

        // Process damage and effects
        var hitEnemies = ProcessTendrilDamage(performerUid, targetCoords, gridUid, gridComp, toxinDamage, slowDuration, slowMultiplier);

        // Schedule visual effects for hit enemies
        if (hitEnemies.Count > 0)
        {
            Timer.Spawn(TimeSpan.FromSeconds(TendrilVisualSpawnDelay), () => SpawnTendrilEffectsOnEnemies(hitEnemies, gridUid, gridComp));
        }
    }

    private List<EntityUid> ProcessTendrilDamage(EntityUid performerUid, EntityCoordinates targetCoords, EntityUid gridUid,
        MapGridComponent gridComp, float toxinDamage, TimeSpan slowDuration, float slowMultiplier)
    {
        var hitEnemies = new List<EntityUid>();

        foreach (var offset in _tendrilOffsets)
        {
            var center = targetCoords.Offset(offset);
            if (!IsValidTile(center, gridUid, gridComp))
                continue;

            foreach (var ent in _lookup.GetEntitiesInRange(center, TendrilTargetRange, LookupFlags.Dynamic | LookupFlags.Sundries))
            {
                if (ent == performerUid || !HasComp<HumanoidAppearanceComponent>(ent) ||
                    !TryComp<DamageableComponent>(ent, out var _) || hitEnemies.Contains(ent))
                    continue;

                ApplyDamage(ent, _poisonTypeId, toxinDamage, performerUid);
                _movementMod.TryAddMovementSpeedModDuration(ent, Shared.Movement.Systems.MovementModStatusSystem.FlashSlowdown, slowDuration, slowMultiplier);
                hitEnemies.Add(ent);
            }
        }

        return hitEnemies;
    }

    private void SpawnTendrilEffectsOnEnemies(List<EntityUid> hitEnemies, EntityUid gridUid, MapGridComponent gridComp)
    {
        foreach (var enemy in hitEnemies)
        {
            if (!Exists(enemy))
                continue;

            var enemyCoords = Transform(enemy).Coordinates;
            EntityManager.SpawnEntity("VampireBloodTendrilVisual", enemyCoords);

            var enemyTileCoords = enemyCoords.WithPosition(enemyCoords.Position.Floored() + new Vector2(TendrilPositionOffset, TendrilPositionOffset));
            if (IsValidTile(enemyTileCoords, gridUid, gridComp))
            {
                Spawn("PuddleBlood", enemyTileCoords);
            }
        }
    }

    private static readonly Vector2[] _tendrilOffsets = new Vector2[]
    {
    new(-1, -1), new(0, -1), new(1, -1),
    new(-1,  0), new(0,  0), new(1,  0),
    new(-1,  1), new(0,  1), new(1,  1),
    };

    private bool CheckAndConsumeActionCost(EntityUid uid, VampireComponent comp, EntityUid? actionEntity)
        => CheckAndConsumeBloodCost(uid, comp, 0, actionEntity);

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

    private void OnBloodBarrier(VampireBloodBarrierActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<VampireComponent>(args.Performer, out var comp))
            return;

        if (!ValidateVampireClass(args.Performer, comp, VampireClassType.Hemomancer))
            return;

        if (!CheckAndConsumeActionCost(args.Performer, comp, comp.Actions.BloodBarrierActionEntity))
            return;

        args.Handled = true;

        var targetCoords = args.Target;
        var tileCoords = targetCoords.WithPosition(targetCoords.Position.Floored() + new Vector2(0.5f, 0.5f));

        if (_transform.GetGrid(tileCoords) is not { } gridUid ||
            !TryComp<MapGridComponent>(gridUid, out var gridComp))
        {
            _popup.PopupEntity("Cannot place barriers there", args.Performer, args.Performer);
            return;
        }

        var performerTransform = Transform(args.Performer);
        var direction = performerTransform.LocalRotation.ToWorldVec();

        var perpendicular = new Vector2(-direction.Y, direction.X);

        var barrierPositions = new Vector2[]
        {
        tileCoords.Position + perpendicular,
        tileCoords.Position,
        tileCoords.Position - perpendicular
        };

        int successfulBarriers = 0;
        foreach (var pos in barrierPositions)
        {
            var barrierCoords = tileCoords.WithPosition(pos.Floored() + new Vector2(0.5f, 0.5f));

            if (!IsValidTile(barrierCoords, gridUid, gridComp))
                continue;

            var barrier = EntityManager.SpawnEntity("VampireBloodBarrier", barrierCoords);

            var preventCollide = EnsureComp<PreventCollideComponent>(barrier);
            preventCollide.Uid = args.Performer;

            successfulBarriers++;

        }

        if (successfulBarriers == 0)
        {
            _popup.PopupEntity("Cannot place barriers there.", args.Performer, args.Performer);
        }
    }

    // Rinary у меня лапки, помоги с этим говном пж
    // Сделай так, чтобы вампир мог превратиться в кровавую лужу на 8 секунд
    // В этой форме он невидим, неуязвим и может проходить через всё, кроме стен и тайлов космоса
    // Но не может атаковать, пить кровь и использовать способности
    // Реализация ниже, ну говно если так выразится
    // Jaunt в теории надо оформить, но как сделать так чтобы он через стены не проходил я хз
    private void OnSanguinePool(EntityUid uid, VampireComponent comp, ref VampireSanguinePoolActionEvent args)
    {
        if (args.Handled)
            return;

        if (comp.InSanguinePool)
        {
            _popup.PopupEntity("You are already in sanguine pool form!", uid, uid);
            return;
        }

        // Dont allow pooling? in invalid tiles
        var curCoords = Transform(uid).Coordinates;
        if (!IsValidTile(curCoords))
        {
            _popup.PopupEntity("You cannot become a blood pool here.", uid, uid);
            return;
        }

        if (!CheckAndConsumeActionCost(uid, comp, comp.Actions.SanguinePoolActionEntity))
            return;

        EnterSanguinePool(uid, comp);
        args.Handled = true;
    }

    private void EnterSanguinePool(EntityUid uid, VampireComponent comp)
    {
        comp.InSanguinePool = true;
        Dirty(uid, comp);

        Spawn("VampireSanguinePoolOut", Transform(uid).Coordinates);

        var stealth = EnsureComp<StealthComponent>(uid);
        _stealth.SetVisibility(uid, -1f, stealth);

        if (!HasComp<GodmodeComponent>(uid))
        {
            EnsureComp<GodmodeComponent>(uid);
            comp.PoolOwnedGodmode = true;
        }
        else
        {
            comp.PoolOwnedGodmode = false;
        }

        if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount > 0)
        {
            comp.PoolOriginalMasks = new();
            comp.PoolOriginalLayers = new();
            foreach (var (id, fix) in fixtures.Fixtures)
            {
                comp.PoolOriginalMasks[id] = fix.CollisionMask;
                comp.PoolOriginalLayers[id] = fix.CollisionLayer;
                var newMask = (int)CollisionGroup.Impassable | (int)CollisionGroup.GhostImpassable;
                _physics.SetCollisionMask(uid, id, fix, newMask, fixtures);
                var newLayer = 0;
                _physics.SetCollisionLayer(uid, id, fix, newLayer, fixtures);
            }
        }

        Timer.Spawn(TimeSpan.FromSeconds(SanguinePoolDuration), () =>
        {
            if (Exists(uid) && TryComp<VampireComponent>(uid, out var vampComp))
                ExitSanguinePool(uid, vampComp);
        });

        _popup.PopupEntity("You transform into a pool of blood!", uid, uid);

        var enterSound = new SoundPathSpecifier("/Audio/_Starlight/Effects/vampire/enter_blood.ogg");
        _audio.PlayPvs(enterSound, uid, AudioParams.Default.WithVolume(-2f));

        StartSanguinePoolBloodDrip(uid, 0);
    }

    private void ExitSanguinePool(EntityUid uid, VampireComponent comp)
    {
        if (!comp.InSanguinePool)
            return;

        comp.InSanguinePool = false;
        Dirty(uid, comp);

        Spawn("VampireSanguinePoolIn", Transform(uid).Coordinates);

        if (HasComp<StealthComponent>(uid))
            RemComp<StealthComponent>(uid);

        // invul
        if (comp.PoolOwnedGodmode && HasComp<GodmodeComponent>(uid))
            RemComp<GodmodeComponent>(uid);
        comp.PoolOwnedGodmode = false;

        if (TryComp<FixturesComponent>(uid, out var fixtures) &&
            comp.PoolOriginalMasks != null && comp.PoolOriginalLayers != null)
        {
            foreach (var (id, fix) in fixtures.Fixtures)
            {
                if (comp.PoolOriginalMasks.TryGetValue(id, out var mask))
                    _physics.SetCollisionMask(uid, id, fix, mask, fixtures);
                if (comp.PoolOriginalLayers.TryGetValue(id, out var layer))
                    _physics.SetCollisionLayer(uid, id, fix, layer, fixtures);
            }
            comp.PoolOriginalMasks = null;
            comp.PoolOriginalLayers = null;
        }

        _popup.PopupEntity("You reform from the blood pool!", uid, uid);
        var exitSound = new SoundPathSpecifier("/Audio/_Starlight/Effects/vampire/exit_blood.ogg");
        _audio.PlayPvs(exitSound, uid, AudioParams.Default.WithVolume(-2f));
    }

    private void StartSanguinePoolBloodDrip(EntityUid uid, int tickCount = 0)
    {
        const int MaxTicks = 80;

        if (tickCount >= MaxTicks || !Exists(uid))
            return;

        if (!TryComp<VampireComponent>(uid, out var v) || !v.InSanguinePool)
            return;

        var coords = Transform(uid).Coordinates;
        if (IsValidTile(coords))
            Spawn("PuddleBlood", coords);

        Timer.Spawn(TimeSpan.FromSeconds(SanguinePoolBloodDripInterval), () => StartSanguinePoolBloodDrip(uid, tickCount + 1));
    }

    private void OnBloodEruption(EntityUid uid, VampireComponent comp, ref VampireBloodEruptionActionEvent args)
    {
        if (args.Handled)
            return;

        if (!CheckAndConsumeActionCost(uid, comp, comp.Actions.BloodEruptionActionEntity))
            return;

        var coords = Transform(uid).Coordinates;

        var nearbyEntities = _lookup.GetEntitiesInRange(coords, BloodEruptionRange);

        var bloodPuddlesWithTargets = new Dictionary<EntityUid, List<EntityUid>>();

        foreach (var entity in nearbyEntities)
        {
            if (entity == uid)
                continue;
            if (MetaData(entity).EntityPrototype?.ID != "PuddleBlood")
                continue;

            if (!TryComp(entity, out TransformComponent? xform) || !xform.Anchored)
                continue;

            if (_container.IsEntityOrParentInContainer(entity))
                continue;

            var puddleCoords = xform.Coordinates;
            var targetsNearPuddle = _lookup.GetEntitiesInRange(puddleCoords, BloodEruptionTargetRange)
                .Where(target => target != uid && target != entity && HasComp<DamageableComponent>(target))
                .ToList();

            if (targetsNearPuddle.Count > 0)
                bloodPuddlesWithTargets[entity] = targetsNearPuddle;
        }

        foreach (var (puddleUid, targets) in bloodPuddlesWithTargets)
        {
            var puddleCoords = Transform(puddleUid).Coordinates;

            // // Spawn spike visual effect at each blood puddle нету пока, спрайтят
            // Spawn("VampireBloodSpikesVisual", puddleCoords);

            foreach (var targetUid in targets)
            {
                ApplyDamage(targetUid, "Blunt", BloodEruptionDamage, uid);
            }
        }

        _popup.PopupEntity("You cause blood to erupt in spikes around you!", uid, uid);
        args.Handled = true;
    }

    private void OnBloodBringersRite(EntityUid uid, VampireComponent comp, ref VampireBloodBringersRiteActionEvent args)
    {
        if (args.Handled)
            return;

        if (comp.BloodBringersRiteActive)
        {
            DeactivateBloodBringersRite(uid, comp);
            _popup.PopupEntity("Blood Bringers Rite deactivated", uid, uid);
        }
        else
        {
            if (!comp.FullPower)
            {
                _popup.PopupEntity("You lack full vampiric power (need above 1000 total blood & 8 unique victims)", uid, uid);
                return;
            }
            if (comp.DrunkBlood < BloodBringersRiteCost)
            {
                _popup.PopupEntity("Not enough blood to activate Blood Bringers Rite", uid, uid);
                return;
            }

            ActivateBloodBringersRite(uid, comp);
            _popup.PopupEntity("Blood Bringers Rite activated!", uid, uid);
        }

        if (_actions.GetAction(comp.Actions.BloodBringersRiteActionEntity) is { } action)
            _actions.SetToggled(action.AsNullable(), comp.BloodBringersRiteActive);

        args.Handled = true;
    }

    private void ActivateBloodBringersRite(EntityUid uid, VampireComponent comp)
    {
        comp.BloodBringersRiteActive = true;
        comp.BloodBringersRiteLoopId++;

        var drainBeamComp = EnsureComp<VampireDrainBeamComponent>(uid);
        drainBeamComp.ActiveBeams.Clear();

        Dirty(uid, comp);

        StartBloodBringersRiteLoop(uid, 0);
    }

    private void DeactivateBloodBringersRite(EntityUid uid, VampireComponent comp)
    {
        comp.BloodBringersRiteActive = false;

        if (TryComp<VampireDrainBeamComponent>(uid, out var drainBeamComp))
        {
            foreach (var connection in drainBeamComp.ActiveBeams.Values)
            {
                var removeEvent = new VampireDrainBeamEvent(GetNetEntity(connection.Source), GetNetEntity(connection.Target), false);
                RaiseNetworkEvent(removeEvent);
            }
            drainBeamComp.ActiveBeams.Clear();
        }

        Dirty(uid, comp);
    }

    private void StartBloodBringersRiteLoop(EntityUid uid, int tickCount)
    {
        const int MaxTicks = 150;

        if (tickCount >= MaxTicks || !Exists(uid))
            return;

        if (!TryComp<VampireComponent>(uid, out var comp) || !comp.BloodBringersRiteActive)
            return;

        if (TryComp<MobStateComponent>(uid, out var mobState) &&
            mobState.CurrentState == Shared.Mobs.MobState.Dead)
        {
            DeactivateBloodBringersRite(uid, comp);
            return;
        }

        if (comp.DrunkBlood < BloodBringersRiteCost)
        {
            DeactivateBloodBringersRite(uid, comp);
            _popup.PopupEntity("Blood Bringers Rite deactivated - not enough blood", uid, uid);

            if (_actions.GetAction(comp.Actions.BloodBringersRiteActionEntity) is { } action)
                _actions.SetToggled(action.AsNullable(), false);
            return;
        }

        comp.DrunkBlood -= BloodBringersRiteCost;
        Dirty(uid, comp);
        UpdateVampireAlert(uid);

        var coords = Transform(uid).Coordinates;
        var currentTargets = new List<EntityUid>();
        var nearbyEntities = _lookup.GetEntitiesInRange(coords, BloodBringersRiteRange);

        foreach (var entity in nearbyEntities)
        {
            if (entity == uid) continue;

            if (HasComp<HumanoidAppearanceComponent>(entity) && HasComp<BloodstreamComponent>(entity))
            {
                currentTargets.Add(entity);
            }
        }

        UpdateDrainBeamNetwork(uid, currentTargets);

        foreach (var target in currentTargets)
        {
            ApplyDamage(target, "Blunt", BloodBringersRiteDamage, uid);

            ApplyHealing(uid, _bruteGroupId, BloodBringersRiteHealBrute, true);
            ApplyHealing(uid, _burnGroupId, BloodBringersRiteHealBurn, true);
            if (TryComp<StaminaComponent>(uid, out var stam))
            {
                _stamina.TakeStaminaDamage(uid, -BloodBringersRiteHealStamina, stam);
            }
        }

        var expectedLoopId = comp.BloodBringersRiteLoopId;

        Timer.Spawn(TimeSpan.FromSeconds(BloodBringersRiteToggleInterval), () =>
        {
            if (!Exists(uid) || !TryComp<VampireComponent>(uid, out var c2)) return;
            if (!c2.BloodBringersRiteActive || c2.BloodBringersRiteLoopId != expectedLoopId) return;
            StartBloodBringersRiteLoop(uid, tickCount + 1);
        });
    }

    private void UpdateDrainBeamNetwork(EntityUid vampire, List<EntityUid> targets)
    {
        if (!TryComp<VampireDrainBeamComponent>(vampire, out var drainBeamComp))
            return;

        var requiredTargets = new HashSet<EntityUid>(targets);

        var toRemove = new List<EntityUid>();
        foreach (var (targetKey, connection) in drainBeamComp.ActiveBeams)
        {
            if (connection.Source != vampire)
            {
                var removeLegacy = new VampireDrainBeamEvent(GetNetEntity(connection.Source), GetNetEntity(connection.Target), false);
                RaiseNetworkEvent(removeLegacy);
                toRemove.Add(targetKey);
                continue;
            }

            if (!requiredTargets.Contains(connection.Target))
            {
                var removeEvent = new VampireDrainBeamEvent(GetNetEntity(connection.Source), GetNetEntity(connection.Target), false);
                RaiseNetworkEvent(removeEvent);

                toRemove.Add(targetKey);
            }
        }

        foreach (var key in toRemove)
        {
            drainBeamComp.ActiveBeams.Remove(key);
        }

        foreach (var target in requiredTargets)
        {
            if (!drainBeamComp.ActiveBeams.ContainsKey(target))
            {
                var connection = new DrainBeamConnection(vampire, target, BloodBringersRiteRange);
                drainBeamComp.ActiveBeams[target] = connection;

                var createEvent = new VampireDrainBeamEvent(GetNetEntity(vampire), GetNetEntity(target), true);
                RaiseNetworkEvent(createEvent);
            }
        }
    }
    private void OnCloackOfDarkness(EntityUid uid, VampireComponent comp, ref VampireCloakOfDarknessActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        if (comp.CloakOfDarknessActive)
        {
            DeactivateCloakOfDarkness(uid, comp);
            _popup.PopupEntity("You step out of the shadows", uid, uid);
        }
        else
        {
            ActivateCloakOfDarkness(uid, comp);
            _popup.PopupEntity("You blend into the shadows!", uid, uid);
        }

        if (_actions.GetAction(comp.Actions.VampireCloakOfDarknessActionEntity) is { } action)
            _actions.SetToggled(action.AsNullable(), comp.CloakOfDarknessActive);

        args.Handled = true;
    }

    private void ActivateCloakOfDarkness(EntityUid uid, VampireComponent comp)
    {
        comp.CloakOfDarknessActive = true;
        comp.CloakOfDarknessLoopId++;
        Dirty(uid, comp);

        StartCloakOfDarknessLoop(uid, 0);
    }

    private void DeactivateCloakOfDarkness(EntityUid uid, VampireComponent comp)
    {
        comp.CloakOfDarknessActive = false;
        Dirty(uid, comp);

        RemComp<StealthComponent>(uid);
        _movementMod.TryUpdateMovementSpeedModDuration(uid, "VampireCloakSpeedBoost", TimeSpan.Zero, 1f);
    }

    private void StartCloakOfDarknessLoop(EntityUid uid, int tickCount)
    {
        const int MaxTicks = 3000; // +-100 minutes

        if (tickCount >= MaxTicks || !Exists(uid))
            return;

        if (!TryComp<VampireComponent>(uid, out var comp) || !comp.CloakOfDarknessActive)
            return;

        if (TryComp<MobStateComponent>(uid, out var mobState) &&
            mobState.CurrentState == Shared.Mobs.MobState.Dead)
        {
            DeactivateCloakOfDarkness(uid, comp);
            if (_actions.GetAction(comp.Actions.VampireCloakOfDarknessActionEntity) is { } action)
                _actions.SetToggled(action.AsNullable(), false);
            return;
        }

        var lightLevel = _shadekin.GetLightExposure(uid);

        ApplyCloakEffects(uid, comp, lightLevel);

        var expectedLoopId = comp.CloakOfDarknessLoopId;
        Timer.Spawn(TimeSpan.FromSeconds(2f), () =>
        {
            if (!Exists(uid) || !TryComp<VampireComponent>(uid, out var c2)) return;
            if (!c2.CloakOfDarknessActive || c2.CloakOfDarknessLoopId != expectedLoopId) return;
            StartCloakOfDarknessLoop(uid, tickCount + 1);
        });
    }

    private void ApplyCloakEffects(EntityUid uid, VampireComponent comp, float lightLevel)
    {

        float stealthModifier;
        float speedModifier;

        switch (lightLevel)
        {
            case <= 1f:
                stealthModifier = -1f;
                speedModifier = 1.4f;
                break;
            case <= 5f:
                stealthModifier = -0.7f;
                speedModifier = 1.3f;
                break;
            case <= 10f:
                stealthModifier = 0.4f;
                speedModifier = 1.2f;
                break;
            default:
                stealthModifier = 0.2f;
                speedModifier = 1.1f;
                break;
        }

        var stealth = EnsureComp<StealthComponent>(uid);
        _stealth.SetVisibility(uid, stealthModifier, stealth);

        _movementMod.TryUpdateMovementSpeedModDuration(uid, "VampireCloakSpeedBoost", TimeSpan.MaxValue, speedModifier);
    }

    private void OnShadowSnare(EntityUid uid, VampireComponent comp, ref VampireShadowSnareActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        if (!CheckAndConsumeActionCost(uid, comp, comp.Actions.ShadowSnareActionEntity))
            return;

        if (!_playerShadowSnares.TryGetValue(uid, out var playerTraps))
        {
            playerTraps = new List<EntityUid>();
            _playerShadowSnares[uid] = playerTraps;
        }

        playerTraps.RemoveAll(trap => !Exists(trap) || !_shadowSnares.ContainsKey(trap));

        if (playerTraps.Count >= MaxShadowSnaresPerPlayer)
        {
            var oldestTrap = playerTraps[0];
            DeleteShadowSnare(oldestTrap);
            _popup.PopupEntity(Loc.GetString("vampire-shadow-snare-oldest-removed", ("default", "Твоя старая теневая ловушка рассеялась.")), uid, uid);
        }

        var target = args.Target;
        var place = target.WithPosition(target.Position.Floored() + new Vector2(ShadowSnarePositionOffset, ShadowSnarePositionOffset));
        if (!_transform.GetGrid(place).HasValue)
        {
            return;
        }

        var trap = EntityManager.SpawnEntity("VampireShadowSnareTrap", place);
        EnsureComp<ShadowSnareTrapComponent>(trap);

        var stealth = EnsureComp<StealthComponent>(trap);
        _stealth.SetVisibility(trap, ShadowSnareStealthVisibility, stealth);

        _shadowSnares[trap] = new ShadowSnareData(trap, ShadowSnareBaseHealth);
        playerTraps.Add(trap);
        StartShadowSnareLoop(trap, 0);
        args.Handled = true;
    }

    private void OnDarkPassage(EntityUid uid, VampireComponent comp, ref VampireDarkPassageActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        if (!CheckAndConsumeActionCost(uid, comp, comp.Actions.DarkPassageActionEntity))
            return;

        var target = args.Target;
        var curXform = Transform(uid);
        if (curXform.MapID != _transform.GetMapId(target))
            return;

        if (!_transform.GetGrid(target).HasValue)
            return;

        if (!IsValidTile(target) || !_interaction.InRangeUnobstructed(uid, target, range: 1000f, collisionMask: CollisionGroup.Opaque, popup: false))
        {
            _popup.PopupEntity("The darkness here is impenetrable...", uid, uid);
            return;
        }

        EntityManager.SpawnEntity("VampireDarkPassageMistIn", curXform.Coordinates);

        _transform.SetCoordinates(uid, target);
        _transform.AttachToGridOrMap(uid, curXform);

        EntityManager.SpawnEntity("VampireDarkPassageMistOut", target);

        _popup.PopupEntity("You slipped through the darkness...", uid, uid);
        args.Handled = true;
    }

    private void OnExtinguish(EntityUid uid, VampireComponent comp, ref VampireExtinguishActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        if (!CheckAndConsumeActionCost(uid, comp, comp.Actions.ExtinguishActionEntity))
            return;

        var center = Transform(uid).Coordinates;

        var toProcess = _lookup.GetEntitiesInRange(center, ExtinguishRadius);
        var count = 0;
        foreach (var ent in toProcess)
        {
            if (ent == uid)
                continue;

            if (TryComp<Shared.Light.Components.PoweredLightComponent>(ent, out var light))
            {
                EntityManager.System<Light.EntitySystems.PoweredLightSystem>().SetState(ent, false, light);
                count++;
            }
        }

        _popup.PopupEntity($"You absorbed the light around you...({count})", uid, uid);
        args.Handled = true;
    }

    private void OnEternalDarkness(EntityUid uid, VampireComponent comp, ref VampireEternalDarknessActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        if (!comp.EternalDarknessActive)
        {
            if (!comp.FullPower)
            {
                _popup.PopupEntity("Your power is insufficient (need >1000 total blood & 8 unique victims).", uid, uid);
                args.Handled = true;
                return;
            }
            comp.EternalDarknessActive = true;
        }
        else
        {
            comp.EternalDarknessActive = false;
        }
        Dirty(uid, comp);

        if (_actions.GetAction(comp.Actions.EternalDarknessActionEntity) is { } action)
            _actions.SetToggled(action.AsNullable(), comp.EternalDarknessActive);

        if (comp.EternalDarknessActive)
        {
            _popup.PopupEntity("You conjured eternal darkness...", uid, uid);
            comp.EternalDarknessLoopId++;
            if (comp.EternalDarknessAuraEntity == null || !Exists(comp.EternalDarknessAuraEntity))
            {
                var aura = Spawn("VampireEternalDarknessAura", Transform(uid).Coordinates);
                comp.EternalDarknessAuraEntity = aura;
                var xformSys = EntityManager.System<SharedTransformSystem>();
                xformSys.SetParent(aura, uid);
            }
            StartEternalDarknessLoop(uid, 0);
        }
        else
        {
            _popup.PopupEntity("The eternal darkness has dissipated...", uid, uid);
            if (comp.EternalDarknessAuraEntity != null && Exists(comp.EternalDarknessAuraEntity))
            {
                QueueDel(comp.EternalDarknessAuraEntity.Value);
            }
            comp.EternalDarknessAuraEntity = null;
        }

        args.Handled = true;
    }
    private void StartEternalDarknessLoop(EntityUid uid, int tick)
    {
        if (tick >= EternalDarknessMaxTicks || !Exists(uid))
            return;

        if (!TryComp<VampireComponent>(uid, out var comp) || !comp.EternalDarknessActive)
            return;

        if (!ValidateEternalDarknessConditions(uid, comp))
            return;

        if (!ConsumeEternalDarknessBlood(uid, comp))
            return;

        ProcessEternalDarknessEffects(uid, comp, tick);
        ScheduleNextEternalDarknessTick(uid, comp, tick);
    }

    private bool ValidateEternalDarknessConditions(EntityUid uid, VampireComponent comp)
    {
        if (TryComp<MobStateComponent>(uid, out var mob) && mob.CurrentState == Shared.Mobs.MobState.Dead)
        {
            DeactivateEternalDarkness(uid, comp);
            return false;
        }
        return true;
    }

    private bool ConsumeEternalDarknessBlood(EntityUid uid, VampireComponent comp)
    {
        if (comp.DrunkBlood < EternalDarknessBloodPerTick)
        {
            DeactivateEternalDarkness(uid, comp, "You have run out of blood to sustain eternal darkness.");
            return false;
        }

        comp.DrunkBlood -= EternalDarknessBloodPerTick;
        Dirty(uid, comp);
        UpdateVampireAlert(uid);
        return true;
    }

    private void DeactivateEternalDarkness(EntityUid uid, VampireComponent comp, string? message = null)
    {
        comp.EternalDarknessActive = false;

        if (_actions.GetAction(comp.Actions.EternalDarknessActionEntity) is { } action)
            _actions.SetToggled(action.AsNullable(), false);

        if (message != null)
            _popup.PopupEntity(message, uid, uid);

        Dirty(uid, comp);
    }

    private void ProcessEternalDarknessEffects(EntityUid uid, VampireComponent comp, int tick)
    {
        var vampXform = Transform(uid);
        var xformSys = EntityManager.System<SharedTransformSystem>();
        var center = xformSys.GetWorldPosition(vampXform);

        var doCoolingThisTick = (tick % EternalDarknessTempDropInterval) == 0;
        if (doCoolingThisTick)
        {
            ProcessTemperatureEffects(uid, vampXform, center, xformSys);
        }

        ProcessLightEffects(vampXform);
    }

    private void ProcessTemperatureEffects(EntityUid uid, TransformComponent vampXform, Vector2 center, SharedTransformSystem xformSys)
    {
        foreach (var ent in _lookup.GetEntitiesInRange(vampXform.Coordinates, EternalDarknessFreezeRadius))
        {
            if (ent == uid || !HasComp<HumanoidAppearanceComponent>(ent) || HasComp<VampireComponent>(ent))
                continue;

            if (!TryComp<Temperature.Components.TemperatureComponent>(ent, out var temp))
                continue;

            var targetXform = Transform(ent);
            var distance = (xformSys.GetWorldPosition(targetXform) - center).Length();

            if (distance > EternalDarknessFreezeRadius || temp.CurrentTemperature <= EternalDarknessTargetFreezeTemp)
                continue;

            var remaining = temp.CurrentTemperature - EternalDarknessTargetFreezeTemp;
            var drop = Math.Min(EternalDarknessTempDropPerInterval, remaining);

            var tempSys = EntityManager.System<Temperature.Systems.TemperatureSystem>();
            tempSys.ForceChangeTemperature(ent, temp.CurrentTemperature - drop, temp);
        }
    }

    private void ProcessLightEffects(TransformComponent vampXform)
    {
        foreach (var ent in _lookup.GetEntitiesInRange(vampXform.Coordinates, EternalDarknessLightOffRadius))
        {
            if (TryComp<Shared.Light.Components.PoweredLightComponent>(ent, out var light))
            {
                EntityManager.System<Light.EntitySystems.PoweredLightSystem>().SetState(ent, false, light);
            }
        }
    }

    private void ScheduleNextEternalDarknessTick(EntityUid uid, VampireComponent comp, int tick)
    {
        var expectedLoopId = comp.EternalDarknessLoopId;
        Timer.Spawn(TimeSpan.FromSeconds(1), () =>
        {
            if (!Exists(uid) || !TryComp<VampireComponent>(uid, out var c2))
                return;

            if (!c2.EternalDarknessActive || c2.EternalDarknessLoopId != expectedLoopId)
                return;

            StartEternalDarknessLoop(uid, tick + 1);
        });
    }

    private void StartShadowSnareLoop(EntityUid trap, int tick)
    {
        if (!Exists(trap) || !_shadowSnares.ContainsKey(trap))
            return;

        if (!_shadowSnares.TryGetValue(trap, out var data))
            return;

        var light = _shadekin.GetLightExposure(trap);
        float decay = light switch
        {
            <= 5f => ShadowSnareDamageDark,
            <= 10f => ShadowSnareDamageNormal,
            _ => ShadowSnareDamageBright
        };

        var newHealth = data.Health - (int)decay;
        if (newHealth <= 0)
        {
            DeleteShadowSnare(trap);
            return;
        }
        _shadowSnares[trap] = data with { Health = newHealth };

        Timer.Spawn(TimeSpan.FromSeconds(ShadowSnareTickInterval), () => StartShadowSnareLoop(trap, tick + 1));
    }

    private void DeleteShadowSnare(EntityUid trap)
    {
        if (_shadowSnares.Remove(trap))
        {
            foreach (var playerTraps in _playerShadowSnares.Values)
            {
                playerTraps.Remove(trap);
            }

            if (Exists(trap))
                QueueDel(trap);
        }
    }

    private void OnShadowSnareEnsnareShutdown(EntityUid uid, ShadowSnareEnsnareComponent comp, ComponentShutdown args)
    {
        if (comp.Victim != default && HasComp<ShadowSnareBlindMarkerComponent>(comp.Victim))
        {
            if (HasComp<TemporaryBlindnessComponent>(comp.Victim))
                RemCompDeferred<TemporaryBlindnessComponent>(comp.Victim);

            RemCompDeferred<ShadowSnareBlindMarkerComponent>(comp.Victim);
        }
    }

    private void OnShadowSnareTrapInteractUsing(EntityUid uid, ShadowSnareTrapComponent comp, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var used = args.Used;
        if (used == EntityUid.Invalid || !HasComp<FlashComponent>(used))
            return;

        if (TryComp<LimitedChargesComponent>(used, out var charges))
        {
            if (!_charges.IsEmpty((used, charges)))
            {
                _charges.TryUseCharge((used, charges));
            }
        }
        args.Handled = true;
        DeleteShadowSnare(uid);
        var user = args.User;
        if (user != EntityUid.Invalid)
            _popup.PopupEntity("You scattered shadow trap", user, user);
    }

    private void OnShadowSnareTrapCollide(EntityUid uid, ShadowSnareTrapComponent comp, ref StartCollideEvent args)
    {
        if (!Exists(uid) || !_shadowSnares.ContainsKey(uid))
            return;

        var ent = args.OtherEntity;
        if (ent == uid)
            return;
        if (HasComp<VampireComponent>(ent))
            return;
        if (!HasComp<HumanoidAppearanceComponent>(ent))
            return;
        if (!TryComp<DamageableComponent>(ent, out var _))
            return;

        ApplyDamage(ent, _bruteGroupId, ShadowSnareTriggerBrute);

        var hadBlind = HasComp<TemporaryBlindnessComponent>(ent);
        if (!hadBlind)
            AddComp<TemporaryBlindnessComponent>(ent);

        if (!HasComp<ShadowSnareBlindMarkerComponent>(ent))
            AddComp<ShadowSnareBlindMarkerComponent>(ent);

        Timer.Spawn(_shadowSnareBlindDuration, () => TryClearShadowSnareBlind(ent));

        var ensnareEnt = EntityManager.SpawnEntity(null, Transform(ent).Coordinates);
        var ensnaringComponent = EnsureComp<EnsnaringComponent>(ensnareEnt);
        var marker = EnsureComp<ShadowSnareEnsnareComponent>(ensnareEnt);
        marker.Victim = ent;

        ensnaringComponent.BreakoutTime = 5f;
        ensnaringComponent.FreeTime = 3.5f;
        ensnaringComponent.WalkSpeed = ShadowSnareSlowMultiplier;
        ensnaringComponent.SprintSpeed = ShadowSnareSlowMultiplier;
        ensnaringComponent.MaxEnsnares = 1;
        ensnaringComponent.CanMoveBreakout = true;

        EnsureComp<EnsnareableComponent>(ent);

        if (_ensnare.TryEnsnare(ent, ensnareEnt, ensnaringComponent))
        {
            Timer.Spawn(TimeSpan.FromSeconds(0.25f), () => ShadowSnareBlindPoll(ent));
        }
        else
        {
            QueueDel(ensnareEnt);
        }

        DeleteShadowSnare(uid);
    }

    private void TryClearShadowSnareBlind(EntityUid victim)
    {
        if (!Exists(victim))
            return;

        if (!HasComp<ShadowSnareBlindMarkerComponent>(victim))
            return;

        if (TryComp<EnsnareableComponent>(victim, out var ens) && ens.IsEnsnared)
            return;

        if (HasComp<TemporaryBlindnessComponent>(victim))
            RemCompDeferred<TemporaryBlindnessComponent>(victim);

        RemCompDeferred<ShadowSnareBlindMarkerComponent>(victim);
    }

    private void ShadowSnareBlindPoll(EntityUid victim)
    {
        if (!Exists(victim))
            return;
        if (!HasComp<ShadowSnareBlindMarkerComponent>(victim))
            return;
        if (TryComp<EnsnareableComponent>(victim, out var ens) && ens.IsEnsnared)
        {
            Timer.Spawn(TimeSpan.FromSeconds(0.25f), () => ShadowSnareBlindPoll(victim));
            return;
        }
        TryClearShadowSnareBlind(victim);
    }

    private void OnShadowAnchor(EntityUid uid, VampireComponent comp, ref VampireShadowAnchorActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        if (!CheckAndConsumeActionCost(uid, comp, comp.Actions.ShadowAnchorActionEntity))
            return;

        if (comp.SpawnedShadowAnchorBeacon != null && Exists(comp.SpawnedShadowAnchorBeacon))
        {
            var beacon = comp.SpawnedShadowAnchorBeacon.Value;
            var coords = Transform(beacon).Coordinates;
            _transform.SetCoordinates(uid, coords);
            _transform.AttachToGridOrMap(uid, Transform(uid));
            QueueDel(beacon);
            comp.SpawnedShadowAnchorBeacon = null;
            _popup.PopupEntity("You returned to the shadow anchor", uid, uid);
            args.Handled = true;
            return;
        }

        var cur = Transform(uid).Coordinates;
        var newBeacon = EntityManager.SpawnEntity("VampireShadowAnchorBeacon", cur);
        comp.SpawnedShadowAnchorBeacon = newBeacon;
        _popup.PopupEntity("You've secured a spot in the shadows", uid, uid);
        args.Handled = true;
    }

    private void OnShadowBoxing(EntityUid uid, VampireComponent comp, ref VampireShadowBoxingActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        var target = args.Target;
        if (target == EntityUid.Invalid || target == uid)
            return;
        if (!Exists(target) || !HasComp<HumanoidAppearanceComponent>(target))
            return;
        if (!TryComp<DamageableComponent>(target, out _))
            return;

        var now = _timing.CurTime;
        var totalDuration = TimeSpan.FromSeconds(10);

        if (!comp.ShadowBoxingActive)
        {
            if (!CheckAndConsumeActionCost(uid, comp, comp.Actions.ShadowBoxingActionEntity))
                return;

            comp.ShadowBoxingActive = true;
            comp.ShadowBoxingEndTime = now + totalDuration;
            _popup.PopupEntity("You begin shadow boxing", uid, uid);
        }
        else
        {
            if (comp.ShadowBoxingEndTime.HasValue && now >= comp.ShadowBoxingEndTime.Value)
            {
                comp.ShadowBoxingActive = false;
                comp.ShadowBoxingTarget = null;
                comp.ShadowBoxingEndTime = null;
                Dirty(uid, comp);
                _popup.PopupEntity("Shadow boxing has been stoped", uid, uid);
                return;
            }
        }

        comp.ShadowBoxingTarget = target;
        Dirty(uid, comp);

        void TickLoop()
        {
            if (!Exists(uid) || !TryComp<VampireComponent>(uid, out var c) || !c.ShadowBoxingActive)
                return;

            var currentNow = _timing.CurTime;
            if (!c.ShadowBoxingEndTime.HasValue || currentNow >= c.ShadowBoxingEndTime.Value)
            {
                c.ShadowBoxingActive = false;
                c.ShadowBoxingTarget = null;
                c.ShadowBoxingEndTime = null;
                c.ShadowBoxingLoopRunning = false;
                Dirty(uid, c);
                _popup.PopupEntity("Shadow boxing ends.", uid, uid);
                return;
            }

            var tgt = c.ShadowBoxingTarget;
            if (tgt == null || !Exists(tgt.Value))
            {
                Timer.Spawn(TimeSpan.FromSeconds(ShadowBoxingInterval), TickLoop);
                return;
            }

            if (!TryComp<DamageableComponent>(tgt.Value, out _))
            {
                Timer.Spawn(TimeSpan.FromSeconds(ShadowBoxingInterval), TickLoop);
                return;
            }
            if (TryComp<MobStateComponent>(tgt.Value, out var mob) && mob.CurrentState == Shared.Mobs.MobState.Dead)
            {
                Timer.Spawn(TimeSpan.FromSeconds(ShadowBoxingInterval), TickLoop);
                return;
            }

            var xformSys = EntityManager.System<SharedTransformSystem>();
            var curDist = (xformSys.GetWorldPosition(Transform(uid)) - xformSys.GetWorldPosition(Transform(tgt.Value))).Length();
            if (curDist <= ShadowBoxingRange)
            {
                ApplyDamage(tgt.Value, "Blunt", ShadowBoxingBrutePerTick, uid);
                RaiseNetworkEvent(new VampireShadowBoxingPunchEvent(GetNetEntity(uid), GetNetEntity(tgt.Value)));
            }

            Timer.Spawn(TimeSpan.FromSeconds(ShadowBoxingInterval), TickLoop);
        }

        if (!comp.ShadowBoxingLoopRunning)
        {
            comp.ShadowBoxingLoopRunning = true;
            Timer.Spawn(TimeSpan.Zero, () =>
            {
                void WrappedTick()
                {
                    if (!Exists(uid) || !TryComp<VampireComponent>(uid, out var c) || !c.ShadowBoxingActive)
                    {
                        if (TryComp<VampireComponent>(uid, out var c2))
                            c2.ShadowBoxingLoopRunning = false;
                        return;
                    }
                    TickLoop();
                }
                WrappedTick();
            });
        }
        args.Handled = true;
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
