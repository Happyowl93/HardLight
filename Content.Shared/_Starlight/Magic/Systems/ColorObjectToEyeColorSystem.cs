using Content.Shared._Starlight.Magic.Components;
using Content.Shared._Starlight.Magic.Events;
using Content.Shared.Atmos.Piping;
using Content.Shared.Humanoid;
using Content.Shared.Item;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Magic.Systems;

public sealed class ColorObjectToEyeColorSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ColorObjectToEyeColorComponent, AfterSpawnItemInHandEvent>(OnAfterSpawnItemInHand);
    }
    
    private void OnAfterSpawnItemInHand(Entity<ColorObjectToEyeColorComponent> entity, ref AfterSpawnItemInHandEvent ev)
    {
        var eyeColor = Comp<HumanoidAppearanceComponent>(ev.Preformer).EyeColor;
        
        var color = NormalizeColor(eyeColor);

        _pointLight.SetColor(ev.Entity, color);

        _appearance.SetData(ev.Entity, ColorObjectToEyeColorVisuals.Color, color);
    }
    
    private Color NormalizeColor(Color rgb, float targetPower = 1.8f)
    {
        var power = rgb.R + rgb.G + rgb.B;

        if (power == 0f)
            return new Color(.6f, .6f, .6f);

        var scale = targetPower / power;

        float[] newColors = [ rgb.R * scale, rgb.G * scale, rgb.B * scale ];

        var reminder = 0.0f;
        var overweight = 0;
        for (var i=0; i<newColors.Length; i++)
        {
            if (newColors[i] <= 1)
                continue;
            
            reminder += newColors[i] % 1.0f;
            newColors[i] = 1.0f;
            overweight++;
        }
        
        for (var i=0; i<newColors.Length; i++)
        {
            if (newColors[i] > .99)
                continue;
            
            newColors[i] += reminder/(newColors.Length-overweight);
        }

        return new  Color(newColors[0], newColors[1], newColors[2]);
    }
    
}

[Serializable, NetSerializable]
public enum ColorObjectToEyeColorVisuals : byte
{
    Color,
}