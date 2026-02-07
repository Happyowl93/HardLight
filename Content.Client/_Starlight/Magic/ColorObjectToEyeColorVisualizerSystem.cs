using System.Linq;
using Content.Client.Items.Systems;
using Content.Shared._Starlight.Magic.Components;
using Content.Shared._Starlight.Magic.Systems;
using Content.Shared.Hands;
using Content.Shared.Item;
using Robust.Client.GameObjects;

namespace Content.Client._Starlight.Magic;

public sealed class ColorObjectToEyeColorVisualizerSystem : VisualizerSystem<ColorVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ColorVisualsComponent, GetInhandVisualsEvent>(OnGetVisuals, after: [typeof(ItemSystem)]);
    }
    
    protected override void OnAppearanceChange(EntityUid uid, ColorVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite)
            && AppearanceSystem.TryGetData<Color>(uid, ColorObjectToEyeColorVisuals.Color, out var color, args.Component))
        {
            sprite[ColorObjectToEyeColorVisuals.Color].Color = color;
        }
    }

    private void OnGetVisuals(EntityUid uid, ColorVisualsComponent item, GetInhandVisualsEvent args)
    {
        if (!AppearanceSystem.TryGetData<Color>(uid, ColorObjectToEyeColorVisuals.Color, out var color))
            return;

        foreach (var layer in args.Layers)
        {
            layer.Item2.Color = color;
        }
    }
}
