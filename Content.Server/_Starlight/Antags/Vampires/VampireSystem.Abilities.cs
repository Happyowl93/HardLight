using Content.Shared._Starlight.Antags.Vampires;
using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Shared.Body.Components;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.Actions.Components;
using Content.Shared.Stunnable;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Speech.Muting;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server._Starlight.Antags.Vampires
{
    public sealed partial class VampireSystem
    {
        partial void SubscribeAbilities()
        {
            SubscribeLocalEvent<VampireComponent, VampireToggleFangsActionEvent>(OnToggleFangs);
            SubscribeLocalEvent<VampireComponent, VampireGlareActionEvent>(OnGlare);
            SubscribeLocalEvent<VampireComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<VampireComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
            SubscribeLocalEvent<VampireComponent, VampireDrinkBloodDoAfterEvent>(OnDrinkDoAfter);
        }

        private void OnToggleFangs(EntityUid uid, VampireComponent comp, ref VampireToggleFangsActionEvent args)
        {
            if (args.Handled)
                return;

            comp.FangsExtended = !comp.FangsExtended;
            if (!comp.FangsExtended)
                comp.IsDrinking = false; 

            if (_actions.GetAction(comp.ToggleFangsActionEntity) is { } action)
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
            if (!HasComp<BloodstreamComponent>(target))
                return;

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
            if (_blood.TryModifyBloodLevel(target, -comp.SipAmount))
            {
                comp.DrunkBlood += (int)comp.SipAmount;
                comp.BloodFullness = MathF.Min(comp.MaxBloodFullness, comp.BloodFullness + comp.SipAmount);
                Dirty(uid, comp);
                UpdateVampireAlert(uid);
                UpdateVampireFedAlert(uid, comp);
                _popup.PopupEntity(Loc.GetString("vampire-drink-end", ("target", Identity.Entity(target, EntityManager))), uid, uid);

                if (comp.FangsExtended && comp.BloodFullness < comp.MaxBloodFullness)
                {
                    comp.IsDrinking = false;
                    StartDrinkDoAfter(uid, comp, target, showPopup: false);
                }else{
                    comp.IsDrinking = false;
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
            // Map to 5 severities (1..5). 1 = starving, 5 = full.
            var sev = (short)Math.Clamp((int)MathF.Ceiling(frac * 4f) + 1, 1, 5);
            _alerts.ShowAlert(uid, VampireFedAlertId, sev);
        }

        private void StartDrinkDoAfter(EntityUid uid, VampireComponent comp, EntityUid target, bool showPopup)
        {
            if (comp.IsDrinking)
                return;

            // Start a short do-after to drink blood
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

            // Find targets within 1 tile around the vampire
            var targets = _lookup.GetEntitiesInRange(uid, 1f, Robust.Shared.GameObjects.LookupFlags.Dynamic | Robust.Shared.GameObjects.LookupFlags.Sundries);

            foreach (var target in targets)
            {
                if (target == uid)
                    continue;

                // Must have stamina to be affected in this way
                if (!TryComp<StaminaComponent>(target, out var stam))
                    continue;

                // Apply a brief paralyze/stun
                _stun.TryAddParalyzeDuration(target, TimeSpan.FromSeconds(2));

                // Apply initial stamina damage
                _stamina.TakeStaminaDamage(target, 30f, stam, source: uid);

                // Mute for 8 seconds
                EnsureComp<MutedComponent>(target);
                Timer.Spawn(TimeSpan.FromSeconds(8), () =>
                {
                    if (Exists(target))
                        RemComp<MutedComponent>(target);
                });

                // Keep applying stamina damage over time until stam-crit
                void TickDrain()
                {
                    if (!Exists(target))
                        return;

                    if (!TryComp<StaminaComponent>(target, out var s))
                        return;

                    if (s.Critical)
                        return;

                    _stamina.TakeStaminaDamage(target, 15f, s, source: uid);
                    Timer.Spawn(TimeSpan.FromSeconds(1), TickDrain);
                }

                // Only start DOT if not already in crit
                if (!stam.Critical)
                    Timer.Spawn(TimeSpan.FromSeconds(1), TickDrain);
            }

            args.Handled = true;
        }
    }
}
