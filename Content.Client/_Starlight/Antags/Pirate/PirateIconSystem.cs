using Content.Shared._Starlight.Antags.Pirate;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Starlight.Antags.Pirate;

public sealed class PirateIconSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PirateComponent, GetStatusIconsEvent>(OnGetStatusIcons);
    }

    private void OnGetStatusIcons(EntityUid uid, PirateComponent component, ref GetStatusIconsEvent ev)
    {
        if (_prototype.TryIndex(component.StatusIcon, out var icon))
            ev.StatusIcons.Add(icon);
    }
}
