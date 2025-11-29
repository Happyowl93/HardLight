using Content.Client.Alerts;
using Content.Shared._Starlight.Antags.Vampires;
using Robust.Client.GameObjects;

namespace Content.Client._Starlight.Antags.Vampires;

public sealed class VampireSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VampireComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
    }

    private void OnUpdateAlert(EntityUid uid, VampireComponent comp, ref UpdateAlertSpriteEvent args)
    {
        var key = args.Alert.AlertKey.AlertType;

        if (key == "VampireBlood")
        {
            // Background is set by the alert -> only set the digit layers from the counter value.
            var value = Math.Clamp(comp.DrunkBlood, 0, 9999);
            var d1 = value / 1000 % 10;
            var d2 = value / 100 % 10;
            var d3 = value / 10 % 10;
            var d4 = value % 10;

            _sprite.LayerSetRsiState((args.SpriteViewEnt, args.SpriteViewEnt.Comp), VampireVisualLayers.Digit1, d1.ToString());
            _sprite.LayerSetRsiState((args.SpriteViewEnt, args.SpriteViewEnt.Comp), VampireVisualLayers.Digit2, d2.ToString());
            _sprite.LayerSetRsiState((args.SpriteViewEnt, args.SpriteViewEnt.Comp), VampireVisualLayers.Digit3, d3.ToString());
            _sprite.LayerSetRsiState((args.SpriteViewEnt, args.SpriteViewEnt.Comp), VampireVisualLayers.Digit4, d4.ToString());
        }
    }
}

