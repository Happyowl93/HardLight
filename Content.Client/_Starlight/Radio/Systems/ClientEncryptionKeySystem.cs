using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Starlight.Radio.Systems;

/// <summary>
/// Handles sprite overrides for the encryption key.
/// </summary>
public sealed class ClientEncryptionKeySystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EncryptionKeyComponent, AfterAutoHandleStateEvent>(OnAutoHandleState);
    }
    
    private void OnAutoHandleState(EntityUid uid, EncryptionKeyComponent component, AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite)) return;
        if(component.CustomBase?.Item2 is null) RestoreLayer((uid, sprite), 0);
        else
        {
            var rsi = component.CustomBase.Value.Item1 is not null
                ? new RSI(component.ExpectedSpriteSize, new ResPath(component.CustomBase.Value.Item1), sprite.AllLayers.Count())
                : null;
            _sprite.LayerSetRsi((uid, sprite), 0, rsi, component.CustomBase.Value.Item2);
        }
        if(component.CustomIcon?.Item2 is null) RestoreLayer((uid, sprite), 1);
        else
        {
            var rsi = component.CustomIcon.Value.Item1 is not null
                ? new RSI(component.ExpectedSpriteSize, new ResPath(component.CustomIcon.Value.Item1), sprite.AllLayers.Count())
                : null;
            _sprite.LayerSetRsi((uid, sprite), 1, rsi, component.CustomIcon.Value.Item2);
        }
    }

    private void RestoreLayer(Entity<SpriteComponent> entity, int index)
    {
        if (!TryGetPrototypeSprite(entity, out var sprite)) return;
        // Yes, this is obsolete. No, I cannot use SpriteSystem. Those require it to be attached to an entity, which does not work here.
#pragma warning disable CS0618 // Type or member is obsolete
        sprite.TryGetLayer(index, out var layer);
#pragma warning restore CS0618 // Type or member is obsolete
        if (layer is null) return;
        _sprite.LayerSetRsi((entity.Owner, entity.Comp), index, layer.RSI, layer.State);
    }

    private bool TryGetPrototypeSprite(EntityUid uid, [NotNullWhen(true)] out SpriteComponent? sprite)
    {
        sprite = null;
        var protoId = MetaData(uid).EntityPrototype;
        if(protoId is null) return false;
        if (!_proto.Resolve(protoId, out var proto)) return false;
        if (!proto.Components.TryGetComponent(_factory.GetRegistration<SpriteComponent>().Name,
                out var iComp)) return false;
        if (iComp is not SpriteComponent protoSprite) return false;
        sprite = protoSprite;
        return true;
    }
}