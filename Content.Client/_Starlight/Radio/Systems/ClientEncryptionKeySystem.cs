using System.Linq;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client._Starlight.Radio.Systems;

/// <summary>
/// Handles sprite overrides for the encryption key. Inherits base entity system since main encryption key system runs in shared namespace and isnt abstract.
/// </summary>
public sealed class ClientEncryptionKeySystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EncryptionKeyComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<EncryptionKeyComponent, AfterAutoHandleStateEvent>(OnAutoHandleState);
    }

    private void OnInit(EntityUid uid, EncryptionKeyComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite)) return;
        foreach (var layer in sprite.AllLayers)
        {
            var rsi = layer.Rsi;
            var state = layer.RsiState.ToString();
            if (state is null) continue;
            if (rsi is null)
            {
                component.OriginalLayerData.Add((null, state));
            }
            else
            {
                component.OriginalLayerData.Add(((rsi.Path, rsi.Size), state));
            }
        }
    }
    
    private void OnAutoHandleState(EntityUid uid, EncryptionKeyComponent component, AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite)) return;
        if (component.SpriteLayerOverrides.Count == 0)
        {
            foreach (var (layer, data) in sprite.AllLayers.Zip(component.OriginalLayerData))
            {
                var (res, state) = data;

                if (res is not null)
                    layer.Rsi = new RSI(res.Value.Item2, res.Value.Item1);

                layer.RsiState = new RSI.StateId(state);
            }
        }
        else
        {
            foreach (var (layer, index) in sprite.AllLayers.Select((layer, index) => (layer, index)))
            {
                if (!component.SpriteLayerOverrides.TryGetValue(index, out var layerOverride))
                    continue;

                var (res, state) = layerOverride;

                if (res is not null)
                    layer.Rsi = new RSI(res.Value.Item2, res.Value.Item1);

                layer.RsiState = new RSI.StateId(state);
            }
        }
    }
}