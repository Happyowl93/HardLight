using Content.Server.Power.Components;
using Content.Server.Power.Events;
using Content.Shared.PowerCell;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Stunnable.Systems
{
    public sealed class StunbatonSystem : SharedStunbatonSystem
    {
        [Dependency] private readonly RiggableSystem _riggableSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly PowerCellSystem _powerCell = default!; // 🌟Starlight🌟
        [Dependency] private readonly SharedBatterySystem _battery = default!;
        [Dependency] private readonly ItemToggleSystem _itemToggle = default!;
        [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        private readonly Dictionary<EntityUid, TimeSpan> _shieldInteractionCooldown = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StunbatonComponent, AfterInteractEvent>(OnStunbatonAfterInteract);
            SubscribeLocalEvent<StunbatonComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<StunbatonComponent, SolutionContainerChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<StunbatonComponent, StaminaDamageOnHitAttemptEvent>(OnStaminaHitAttempt);
            SubscribeLocalEvent<StunbatonComponent, ChargeChangedEvent>(OnChargeChanged);
            
            // Clean up cooldown tracking when entity is deleted
            EntityManager.EntityDeleted += OnEntityDeleted;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            EntityManager.EntityDeleted -= OnEntityDeleted;
        }

        private void OnEntityDeleted(Entity<MetaDataComponent> entity)
        {
            _shieldInteractionCooldown.Remove(entity.Owner);
        }

        private void OnStunbatonAfterInteract(Entity<StunbatonComponent> entity, ref AfterInteractEvent args)
        {
            // Only handle interaction if stunbaton is the used item
            if (args.Used != entity.Owner)
                return;

            // Check if target is a riot shield
            if (args.Target == null || args.Target == entity.Owner)
                return;

            var target = args.Target.Value;
            var targetProto = MetaData(target).EntityPrototype?.ID;
            if (targetProto != "RiotShield")
                return;

            // Check if user is NOT in combat mode
            if (_combatMode.IsInCombatMode(args.User))
                return;

            // Check cooldown (3 second delay between interactions)
            if (_shieldInteractionCooldown.TryGetValue(args.User, out var lastInteractionTime))
            {
                if (_gameTiming.CurTime < lastInteractionTime + TimeSpan.FromSeconds(3))
                    return; // Still on cooldown
            }

            // Check if riot shield is held in one of the user's hands (for range limitation)
            if (!TryComp<HandsComponent>(args.User, out var hands))
                return;

            // Verify the riot shield is actually held in a hand
            var shieldInHand = false;
            foreach (var handId in hands.Hands.Keys)
            {
                if (_hands.TryGetHeldItem((args.User, hands), handId, out var held) && held == target)
                {
                    shieldInHand = true;
                    break;
                }
            }

            if (!shieldInHand)
                return;

            // Update cooldown
            _shieldInteractionCooldown[args.User] = _gameTiming.CurTime;

            // Display emote message with character name
            var userName = MetaData(args.User).EntityName;
            var emoteMessage = $"{userName} smashes their stun baton against their riot shield!";
            _popup.PopupEntity(emoteMessage, target, args.User);

            // Play bikehorn sound effect
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/Toys/pushHornHonk.ogg"), target);
        }

        private void OnStaminaHitAttempt(Entity<StunbatonComponent> entity, ref StaminaDamageOnHitAttemptEvent args)
        {
            // 🌟Starlight🌟 start
            // Stunbatons check for power cells if they have no BatteryComponent
            Entity<BatteryComponent>? batteryEntity = null;
            if (!_itemToggle.IsActivated(entity.Owner) ||
            !(TryComp(entity.Owner, out BatteryComponent? battery) ||
            _powerCell.TryGetBatteryFromSlot(entity.Owner, out batteryEntity)) ||
            !_battery.TryUseCharge(batteryEntity.HasValue ? batteryEntity.Value.AsNullable() : (entity.Owner, battery), entity.Comp.EnergyPerUse))
            {
                args.Cancelled = true;
            }
            // 🌟Starlight🌟 end
        }

        private void OnExamined(Entity<StunbatonComponent> entity, ref ExaminedEvent args)
        {
            var onMsg = _itemToggle.IsActivated(entity.Owner)
            ? Loc.GetString("comp-stunbaton-examined-on")
            : Loc.GetString("comp-stunbaton-examined-off");
            args.PushMarkup(onMsg);

            // 🌟Starlight🌟 start
            Entity<BatteryComponent>? batteryEnt = null;
            if (TryComp<BatteryComponent>(entity.Owner, out var battery) ||
                _powerCell.TryGetBatteryFromSlot(entity.Owner, out batteryEnt))
            {
                if (batteryEnt.HasValue)
                    battery = batteryEnt.Value;
                if (battery != null)
                {
                    var count = (int)(_battery.GetCharge((entity.Owner, battery)) / entity.Comp.EnergyPerUse);
                    args.PushMarkup(Loc.GetString("melee-battery-examine", ("color", "yellow"), ("count", count)));
                }
            }
            // 🌟Starlight🌟 end
        }

        protected override void TryTurnOn(Entity<StunbatonComponent> entity, ref ItemToggleActivateAttemptEvent args)
        {
            base.TryTurnOn(entity, ref args);

            // 🌟Starlight🌟 start
            Entity<BatteryComponent>? batteryEnt = null;
            if (TryComp<BatteryComponent>(entity.Owner, out var battery) ||
                _powerCell.TryGetBatteryFromSlot(entity.Owner, out batteryEnt))
            {
                if (batteryEnt.HasValue)
                    battery = batteryEnt.Value;
                if (battery != null && _battery.GetCharge((entity.Owner, battery)) < entity.Comp.EnergyPerUse)
                {
                    args.Cancelled = true;
                    if (args.User != null)
                    {
                        _popup.PopupEntity(Loc.GetString("stunbaton-component-low-charge"), (EntityUid)args.User, (EntityUid)args.User);
                    }
                    return;
                }
            }
            // 🌟Starlight🌟 end

            if (TryComp<RiggableComponent>(entity, out var rig) && rig.IsRigged)
            {
                _riggableSystem.Explode(entity.Owner, _battery.GetCharge((entity, battery)), args.User);
            }
        }

        // https://github.com/space-wizards/space-station-14/pull/17288#discussion_r1241213341
        private void OnSolutionChange(Entity<StunbatonComponent> entity, ref SolutionContainerChangedEvent args)
        {
            // Explode if baton is activated and rigged.
            if (!TryComp<RiggableComponent>(entity, out var riggable) ||
                !TryComp<BatteryComponent>(entity, out var battery))
                return;

            if (_itemToggle.IsActivated(entity.Owner) && riggable.IsRigged)
                _riggableSystem.Explode(entity.Owner, _battery.GetCharge((entity, battery)));
        }

        // TODO: Not used anywhere?
        private void SendPowerPulse(EntityUid target, EntityUid? user, EntityUid used)
        {
            RaiseLocalEvent(target, new PowerPulseEvent()
            {
                Used = used,
                User = user
            });
        }

        private void OnChargeChanged(Entity<StunbatonComponent> entity, ref ChargeChangedEvent args)
        {
            // 🌟Starlight🌟 start
            Entity<BatteryComponent>? batteryEnt = null;
            if (TryComp<BatteryComponent>(entity.Owner, out var battery) ||
             _powerCell.TryGetBatteryFromSlot(entity.Owner, out batteryEnt)) // WHY did this get changed to return an entity, aaaa >_<
            {
                if(batteryEnt.HasValue)
                    battery = batteryEnt.Value;
                if (battery != null)
                {
                    if (battery.LastCharge < entity.Comp.EnergyPerUse)
                    {
                        _itemToggle.TryDeactivate(entity.Owner, predicted: false);
                    }
                }
            }
            // 🌟Starlight🌟 end
        }
    }
}
