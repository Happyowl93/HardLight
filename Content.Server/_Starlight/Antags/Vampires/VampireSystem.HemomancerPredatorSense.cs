using Content.Shared._Starlight.Antags.Vampires;
using Content.Shared._Starlight.Antags.Vampires.Components;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Warps;
using Robust.Shared.Map;

namespace Content.Server._Starlight.Antags.Vampires;

public sealed partial class VampireSystem
{
    private readonly Dictionary<EntityUid, EntityUid> _predatorSenseUiActionEntities = new();

    private void InitializeHemomancerPredatorSense()
    {
        SubscribeLocalEvent<VampireComponent, VampireLocateMindActionEvent>(OnPredatorSense);

        Subs.BuiEvents<VampireComponent>(VampireLocateUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnPredatorSenseUiOpened);
            subs.Event<BoundUIClosedEvent>(OnPredatorSenseUiClosed);
            subs.Event<VampireLocateSelectedBuiMsg>(OnPredatorSenseSelected);
        });
    }

    private void OnPredatorSense(EntityUid uid, VampireComponent comp, ref VampireLocateMindActionEvent args)
    {
        var actionEntity = args.Action.Owner;
        if (args.Handled || !ValidateVampireAbility(uid, out var validated, VampireClassType.Hemomancer, actionEntity))
            return;

        comp = validated;

        _predatorSenseUiActionEntities[uid] = actionEntity;

        _ui.CloseUi(uid, VampireLocateUiKey.Key);
        _ui.OpenUi(uid, VampireLocateUiKey.Key, uid);
        UpdatePredatorSenseUi(uid);

        args.Handled = true;
    }

    private void OnPredatorSenseUiOpened(EntityUid uid, VampireComponent comp, BoundUIOpenedEvent args)
    {
        if (!Equals(args.UiKey, VampireLocateUiKey.Key))
            return;

        UpdatePredatorSenseUi(uid);
    }

    private void OnPredatorSenseUiClosed(EntityUid uid, VampireComponent comp, BoundUIClosedEvent args)
    {
        if (!Equals(args.UiKey, VampireLocateUiKey.Key))
            return;

        _predatorSenseUiActionEntities.Remove(uid);
    }

    private void UpdatePredatorSenseUi(EntityUid uid)
    {
        var casterMap = Transform(uid).MapID;
        var targets = new List<VampireLocateTarget>();

        var query = AllEntityQuery<MindContainerComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var mindContainer, out var xform))
        {
            if (ent == uid)
                continue;

            if (!TryGetPredatorSenseTargetName(casterMap, ent, mindContainer, xform, out var display))
                continue;

            targets.Add(new VampireLocateTarget(GetNetEntity(ent), display));
        }

        targets.Sort(static (a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.CurrentCultureIgnoreCase));
        _ui.SetUiState(uid, VampireLocateUiKey.Key, new VampireLocateBuiState { Targets = targets });
    }

    private void OnPredatorSenseSelected(EntityUid uid, VampireComponent comp, VampireLocateSelectedBuiMsg args)
    {
        if (args.Actor != uid)
            return;

        if (!_predatorSenseUiActionEntities.TryGetValue(uid, out var actionEntity) || !Exists(actionEntity))
            return;

        var target = GetEntity(args.Target);
        if (!Exists(target) || !TryComp<MindContainerComponent>(target, out var mindContainer))
            return;

        var xform = Transform(target);

        var casterMap = Transform(uid).MapID;
        if (!TryGetPredatorSenseTargetName(casterMap, target, mindContainer, xform, out var targetName))
        {
            _popup.PopupEntity(Loc.GetString("vampire-locate-unknown"), uid, uid, PopupType.MediumCaution);
            _ui.CloseUi(uid, VampireLocateUiKey.Key);
            return;
        }

        if (xform.MapID != casterMap)
        {
            _popup.PopupEntity(Loc.GetString("vampire-locate-not-same-sector"), uid, uid, PopupType.MediumCaution);
            return;
        }

        if (!CheckAndConsumeBloodCost(uid, comp, actionEntity))
            return;

        var location = TryGetNearestWarpLocationName(target, out var loc)
            ? loc
            : Loc.GetString("vampire-locate-unknown");

        _popup.PopupEntity(Loc.GetString("vampire-locate-result",("target", targetName),("location", location)), uid, uid, PopupType.LargeCaution);

        _ui.CloseUi(uid, VampireLocateUiKey.Key);
    }

    private bool TryGetPredatorSenseTargetName(
        MapId casterMap,
        EntityUid target,
        MindContainerComponent mindContainer,
        TransformComponent xform,
        out string displayName)
    {
        displayName = string.Empty;

        // Must be a mind controlled character on the same map
        if (!mindContainer.HasMind)
            return false;

        if (xform.MapID != casterMap)
            return false;

        // there might be better way to filter candidates
        if (HasComp<GhostComponent>(target) || !HasComp<HumanoidAppearanceComponent>(target))
            return false;

        // Use the in-game entity name (character name), not any session/OOC handle.
        var name = MetaData(target).EntityName;
        if (string.IsNullOrWhiteSpace(name))
            return false;

        displayName = name;
        return true;
    }

    private bool TryGetNearestWarpLocationName(EntityUid target, out string location)
    {
        location = string.Empty;

        var targetXform = Transform(target);
        var targetMap = targetXform.MapID;
        var targetGrid = targetXform.GridUid;
        var targetPos = _transform.GetWorldPosition(targetXform);

        float bestDistSq = float.MaxValue;
        string? best = null;

        var warps = AllEntityQuery<WarpPointComponent, TransformComponent>();
        while (warps.MoveNext(out var warpUid, out var warp, out var warpXform))
        {
            if (_whitelist.IsWhitelistPass(warp.Blacklist, warpUid))
                continue;

            if (string.IsNullOrWhiteSpace(warp.Location))
                continue;

            if (warpXform.MapID != targetMap)
                continue;

            if (targetGrid != null && warpXform.GridUid != targetGrid)
                continue;

            var warpPos = _transform.GetWorldPosition(warpXform);
            var distSq = (warpPos - targetPos).LengthSquared();
            if (distSq >= bestDistSq)
                continue;

            bestDistSq = distSq;
            best = warp.Location;
        }

        if (best == null)
            return false;

        location = best;
        return true;
    }
}
