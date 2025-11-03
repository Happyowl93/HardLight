using Content.Shared._Starlight.Shadekin;
using Content.Shared.Humanoid;
using Content.Shared.Rejuvenate;

namespace Content.Server._Starlight.Shadekin;

public sealed partial class ShadekinSystem : EntitySystem
{
    public void InitializeBrighteye()
    {
        SubscribeLocalEvent<BrighteyeComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<BrighteyeComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<BrighteyeComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnInit(EntityUid uid, BrighteyeComponent component, ComponentStartup args)
    {
        _alerts.ShowAlert(uid, component.BrighteyeAlert);

        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            SetBrighteyes(uid, humanoid);
    }

    private void OnShutdown(EntityUid uid, BrighteyeComponent component, ComponentShutdown args)
    {
        _alerts.ClearAlert(uid, component.BrighteyeAlert);

        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            SetBlackeyes(uid, humanoid);
    }

    private void OnRejuvenate(EntityUid uid, BrighteyeComponent component, RejuvenateEvent args)
    {
        component.Energy = component.MaxEnergy;
        Dirty(uid, component);
    }

    /// <summary>
    /// Change the humanoid eye to be bright and glow!
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="humanoid"></param>
    public void SetBrighteyes(EntityUid uid, HumanoidAppearanceComponent humanoid)
    {
        var hsv = Color.ToHsv(humanoid.EyeColor);
        hsv.Z = 1.0f;
        humanoid.EyeColor = Color.FromHsv(hsv);

        humanoid.EyeGlowing = true;
        Dirty(uid, humanoid);
    }

    /// <summary>
    /// Change the humanoid eye to be validated by HumanoidEyeColor.Shadekin (Blackeyes)
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="humanoid"></param>
    public void SetBlackeyes(EntityUid uid, HumanoidAppearanceComponent humanoid)
    {
        humanoid.EyeColor = EyeColor.MakeShadekinValid(humanoid.EyeColor);
        humanoid.EyeGlowing = false;

        Dirty(uid, humanoid);
    }

    private void UpdateEnergy(EntityUid uid, ShadekinComponent component, BrighteyeComponent brighteye)
    {
        if (component.CurrentState == ShadekinState.Low) // On Low State, we gain and lose nothing!
            return;

        int newenergy = 0;

        if (brighteye.Energy > 0 && component.CurrentState != ShadekinState.Dark) // First we will handle energy drain on light.
        {
            if (component.CurrentState == ShadekinState.Extreme)
                newenergy = -5;
            else if (component.CurrentState == ShadekinState.High)
                newenergy = -2;
            else if (component.CurrentState == ShadekinState.Annoying)
                newenergy = -1;
        }
        else if (brighteye.Energy < brighteye.MaxEnergy && component.CurrentState == ShadekinState.Dark) // We now handle energy gain.
        {
            // TODO: Add buffs here depanding on different situations?
            newenergy = 1;
        }

        brighteye.Energy = Math.Clamp(brighteye.Energy + newenergy, 0, brighteye.MaxEnergy);
        Dirty(uid, brighteye);
    }
}