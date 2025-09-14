using Content.Shared._Starlight.Antags.Vampires;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Interaction.Components;
using Content.Shared.Humanoid;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Wieldable.Components;
using Content.Shared.Wieldable;
using Robust.Shared.GameObjects;

namespace Content.Server._Starlight.Antags.Vampires;

/// <summary>
/// Handles vampiric claws lifecycle and effects
/// </summary>
public sealed class VampiricClawsSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VampiricClawsComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<VampiricClawsComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<VampiricClawsComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<VampiricClawsComponent, ItemUnwieldedEvent>(OnUnwielded);
    }

    private void OnInit(Entity<VampiricClawsComponent> ent, ref MapInitEvent args)
        => EnsureComp<UnremoveableComponent>(ent);

    private void OnUseInHand(Entity<VampiricClawsComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ClearClawsReference(ent.Owner, args.User);

        QueueDel(ent);
    }

    private void OnMeleeHit(Entity<VampiricClawsComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        var bloodGained = 0;
        foreach (var hitEntity in args.HitEntities)
        {
            if (HasComp<HumanoidAppearanceComponent>(hitEntity) && TryComp<BloodstreamComponent>(hitEntity, out var victimBlood))
            {
                if (_bloodstream.TryModifyBloodLevel((hitEntity, victimBlood), -ent.Comp.BloodPerHit))
                {
                    bloodGained += ent.Comp.BloodPerHit;
                }
            }
        }

        if (bloodGained > 0 && TryComp<VampireComponent>(args.User, out var vamp))
        {
            vamp.DrunkBlood += bloodGained;
            vamp.TotalBlood += bloodGained;

            vamp.BloodFullness = MathF.Min(vamp.MaxBloodFullness, vamp.BloodFullness + bloodGained);
            Dirty(args.User, vamp);

            if (TryComp<HungerComponent>(args.User, out var hunger))
            {
                _hunger.ModifyHunger(args.User, bloodGained * 2, hunger);
            }

            ent.Comp.HitsRemaining--;
            Dirty(ent);
            if (ent.Comp.HitsRemaining <= 0)
            {
                ClearClawsReference(ent.Owner, args.User);
                QueueDel(ent);
            }
        }
    }

    private void ClearClawsReference(EntityUid claws, EntityUid user)
    {
        if (TryComp<VampireComponent>(user, out var vampire) &&
            vampire.SpawnedClaws == claws)
        {
            vampire.SpawnedClaws = null;
        }
    }

    private void OnUnwielded(Entity<VampiricClawsComponent> ent, ref ItemUnwieldedEvent args)
    {
        if (TryComp<VampireComponent>(args.User, out var vampire) &&
            vampire.SpawnedClaws == ent.Owner)
        {
            vampire.SpawnedClaws = null;
        }

        QueueDel(ent);
    }
}
