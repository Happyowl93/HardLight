using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Cargo.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;

namespace Content.Server.Cargo.Systems;

/// <summary>
/// A variant of the ATS cargo pallets that deals with gasses
/// fed through pipe systems instead of in canisters, allowing
/// for high-volume sales.
/// </summary>
public sealed class CargoGasPalletSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CargoGasPalletComponent, AtmosDeviceUpdateEvent>(OnAtmosDeviceUpdateEvent);
        SubscribeLocalEvent<EntitySellContentsEvent>(OnEntitySellContentsEvent);
    }

    /// <summary>
    /// Handle gas movement into the internal pre-sale resivoir of the CargoGasPallet
    /// </summary>
    private void OnAtmosDeviceUpdateEvent(EntityUid uid, CargoGasPalletComponent pallet, ref AtmosDeviceUpdateEvent args)
    {
        if (!_nodeContainer.TryGetNode(uid, pallet.InletName, out PipeNode? inlet)) {
            return;
        }

        // We're effectively venting into a wormhole - if there's gas, it's moving.
        if (inlet.Air.Pressure > 0) {
            _atmosphereSystem.Merge(pallet.Air, inlet.Air);
            inlet.Air.Clear();
        }
    }

    /// <summary>
    /// Handle clearing the internal resivoir when we sell the gas, so we can't sell it twice
    /// </summary>
    private void OnEntitySellContentsEvent(ref EntitySellContentsEvent args) {
        foreach (var pallet in args.Containers) {
            pallet.Air.Clear();
        }
    }
}
