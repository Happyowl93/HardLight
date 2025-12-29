using System.Linq;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client._Starlight.Radio.Systems;

// TODO: implement this. I'm too stupid to get it working properly. -neomoth

/// <summary>
/// Handles sprite overrides for the encryption key.
/// </summary>
public sealed class ClientEncryptionKeySystem : EntitySystem
{
    // [Dependency] private readonly SpriteSystem _sprite = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        // SubscribeLocalEvent<EncryptionKeyComponent, ComponentInit>(OnInit);
        // SubscribeLocalEvent<EncryptionKeyComponent, AfterAutoHandleStateEvent>(OnAutoHandleState);
    }

    // private void OnInit(EntityUid uid, EncryptionKeyComponent component, ComponentInit args)
    // {
    //     if (!TryComp<SpriteComponent>(uid, out var sprite)) return;
    // }
    
    // private void OnAutoHandleState(EntityUid uid, EncryptionKeyComponent component, AfterAutoHandleStateEvent args)
    // {
    //     if (!TryComp<SpriteComponent>(uid, out var sprite)) return;
    // }
}