using Content.Server._Common.Consent;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.FloofStation.Traits.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.FloofStation.Traits;

/// <summary>
/// Cum-only port of HardLight's LewdTraitSystem. Other producer types (Milk, Piss) deferred.
/// Cum-related verbs are gated on the target's <c>Cum</c> consent toggle.
/// </summary>
[UsedImplicitly]
public sealed class LewdTraitSystem : EntitySystem
{
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ConsentSystem _consent = default!;

    private const string CumConsent = "Cum";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CumProducerComponent, ComponentStartup>(OnComponentInitCum);
        SubscribeLocalEvent<CumProducerComponent, GetVerbsEvent<InnateVerb>>(AddCumVerb);
        SubscribeLocalEvent<RefillableSolutionComponent, GetVerbsEvent<AlternativeVerb>>(AddRefillableInsideVerbs);
        SubscribeLocalEvent<InjectableSolutionComponent, GetVerbsEvent<AlternativeVerb>>(AddInjectableInsideVerbs);
        SubscribeLocalEvent<CumProducerComponent, CummingDoAfterEvent>(OnDoAfterCum);
    }

    private void OnComponentInitCum(Entity<CumProducerComponent> entity, ref ComponentStartup args)
    {
        if (!_solutionContainer.EnsureSolution(entity.Owner, entity.Comp.SolutionName, out var solutionCum))
            return;

        solutionCum.MaxVolume = entity.Comp.MaxVolume;
        solutionCum.AddReagent(entity.Comp.ReagentId, entity.Comp.MaxVolume - solutionCum.Volume);
    }

    private void AddCumVerb(Entity<CumProducerComponent> entity, ref GetVerbsEvent<InnateVerb> args)
    {
        if (args.Using == null
            || !args.CanInteract
            || args.User != args.Target
            || !EntityManager.HasComponent<RefillableSolutionComponent>(args.Using.Value))
            return;

        _solutionContainer.EnsureSolution(entity.Owner, entity.Comp.SolutionName, out _);

        var user = args.User;
        var used = args.Using.Value;

        args.Verbs.Add(new InnateVerb
        {
            Act = () => AttemptCum(entity, user, used),
            Text = Loc.GetString("cum-verb-get-text"),
            Priority = 1,
        });
    }

    private void AddRefillableInsideVerbs(EntityUid uid, RefillableSolutionComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract)
            return;

        if (TryComp<CumProducerComponent>(args.User, out var cumProducer)
            && _consent.HasConsent(uid, CumConsent))
        {
            _solutionContainer.EnsureSolution(args.User, cumProducer.SolutionName, out _);

            var user = args.User;
            var target = uid;

            args.Verbs.Add(new AlternativeVerb
            {
                Act = () => AttemptCum((args.User, cumProducer), user, target),
                Text = Loc.GetString("cum-verb-inside-text"),
                Priority = -50,
            });
        }
    }

    private void AddInjectableInsideVerbs(EntityUid uid, InjectableSolutionComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract)
            return;

        if (TryComp<CumProducerComponent>(args.User, out var cumProducer)
            && _consent.HasConsent(uid, CumConsent))
        {
            _solutionContainer.EnsureSolution(args.User, cumProducer.SolutionName, out _);

            var user = args.User;
            var target = uid;

            args.Verbs.Add(new AlternativeVerb
            {
                Act = () => AttemptCum((args.User, cumProducer), user, target),
                Text = Loc.GetString("cum-verb-inside-text"),
                Priority = -50,
            });
        }
    }

    private void OnDoAfterCum(Entity<CumProducerComponent> entity, ref CummingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Used == null)
            return;

        if (!_solutionContainer.ResolveSolution(entity.Owner, entity.Comp.SolutionName, ref entity.Comp.Solution, out var solution))
            return;

        if (_solutionContainer.TryGetRefillableSolution(args.Args.Used.Value, out var targetSoln, out var targetSolution))
        {
            args.Handled = true;
            var quantity = solution.Volume;
            if (quantity == 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("cum-verb-dry"), entity.Owner, args.Args.User);
                return;
            }

            if (quantity > targetSolution.AvailableVolume)
                quantity = targetSolution.AvailableVolume;

            var split = _solutionContainer.SplitSolution(entity.Comp.Solution!.Value, quantity);
            _solutionContainer.TryAddSolution(targetSoln.Value, split);
            _popupSystem.PopupEntity(Loc.GetString("cum-verb-success", ("amount", quantity), ("target", Identity.Entity(args.Args.Used.Value, EntityManager))), entity.Owner, args.Args.User, PopupType.Medium);

            return;
        }

        if (_solutionContainer.TryGetInjectableSolution(args.Args.Used.Value, out var injectSoln, out var injectSolution))
        {
            args.Handled = true;
            var quantity = solution.Volume;
            if (quantity == 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("cum-verb-dry"), entity.Owner, args.Args.User);
                return;
            }

            var available = injectSolution.AvailableVolume;
            var injected = quantity > available ? available : quantity;

            if (injected > 0)
            {
                var splitInject = _solutionContainer.SplitSolution(entity.Comp.Solution!.Value, injected);
                _solutionContainer.TryAddSolution(injectSoln.Value, splitInject);
            }

            var overflow = quantity - injected;
            if (overflow > 0)
            {
                var splitOverflow = _solutionContainer.SplitSolution(entity.Comp.Solution!.Value, overflow);
                _puddle.TrySpillAt(args.Args.Used.Value, splitOverflow, out _, sound: false);
                _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg"), args.Args.Used.Value);
                _popupSystem.PopupEntity(Loc.GetString("cum-verb-overflow", ("amount", overflow)), args.Args.Used.Value, PopupType.MediumCaution);
            }

            _popupSystem.PopupEntity(Loc.GetString("cum-verb-success", ("amount", injected), ("target", Identity.Entity(args.Args.Used.Value, EntityManager))), entity.Owner, args.Args.User, PopupType.Medium);
            _popupSystem.PopupEntity(Loc.GetString("cum-verb-success-other", ("amount", injected), ("target", Identity.Entity(args.Args.User, EntityManager))), entity.Owner, args.Args.Used.Value, PopupType.Medium);
        }
    }

    private void AttemptCum(Entity<CumProducerComponent> lewd, EntityUid userUid, EntityUid containerUid)
    {
        if (!HasComp<CumProducerComponent>(userUid))
            return;

        var doargs = new DoAfterArgs(EntityManager, userUid, 5, new CummingDoAfterEvent(), lewd, lewd, used: containerUid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1.0f,
        };

        _doAfterSystem.TryStartDoAfter(doargs);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var queryCum = EntityQueryEnumerator<CumProducerComponent>();
        var now = _timing.CurTime;

        while (queryCum.MoveNext(out var uid, out var containerCum))
        {
            if (now < containerCum.NextGrowth)
                continue;

            containerCum.NextGrowth = now + containerCum.GrowthDelay;

            if (_mobState.IsDead(uid))
                continue;

            if (!_solutionContainer.ResolveSolution(uid, containerCum.SolutionName, ref containerCum.Solution))
                continue;

            if (TryComp<HungerComponent>(uid, out var hunger))
            {
                if (_hunger.GetHungerThreshold(hunger) < HungerThreshold.Okay)
                    continue;
                _solutionContainer.TryAddReagent(containerCum.Solution!.Value, containerCum.ReagentId, containerCum.QuantityPerUpdate, out var quantity);
                if (quantity > 0)
                    _hunger.ModifyHunger(uid, -containerCum.HungerUsage, hunger);
                continue;
            }

            _solutionContainer.TryAddReagent(containerCum.Solution!.Value, containerCum.ReagentId, containerCum.QuantityPerUpdate, out _);
        }
    }
}
