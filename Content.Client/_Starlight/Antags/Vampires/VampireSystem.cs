using System;
using Content.Client.Alerts;
using Content.Client.UserInterface.Systems.Alerts.Controls;
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
        var sprite = args.SpriteViewEnt.Comp;

        if (key == "VampireBlood")
        {
            // Background is set by the alert; only set the three digit layers from the counter value.
            var value = Math.Clamp(comp.DrunkBlood, 0, 999);
            var d1 = value / 100 % 10;
            var d2 = value / 10 % 10;
            var d3 = value % 10;

            _sprite.LayerSetRsiState((args.SpriteViewEnt, args.SpriteViewEnt.Comp), VampireVisualLayers.Digit1, d1.ToString());
            _sprite.LayerSetRsiState((args.SpriteViewEnt, args.SpriteViewEnt.Comp), VampireVisualLayers.Digit2, d2.ToString());
            _sprite.LayerSetRsiState((args.SpriteViewEnt, args.SpriteViewEnt.Comp), VampireVisualLayers.Digit3, d3.ToString());
        }
        else if (key == "VampireFed")
        {
            // Choose icon index based on fullness. 0..3 correspond to hungry..full
            var pct = comp.MaxBloodFullness <= 0 ? 0 : comp.BloodFullness / comp.MaxBloodFullness;
            // The icon is selected by server-side alert severity; nothing to set on client here.
        }
    }
}

