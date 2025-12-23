using Content.Client.UserInterface.Controls;
using Content.Shared._Starlight.Antags.Vampires;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client._Starlight.Antags.Vampires;

[UsedImplicitly]
public sealed class VampireClassBui : BoundUserInterface
{
    private SimpleRadialMenu? _menu;
    private bool _choiceMade;

    public VampireClassBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        _choiceMade = false;

        _menu.OnClose += OnMenuClosed;

        var buttonModels = CreateClassButtons();
        _menu.SetButtons(buttonModels);

        _menu.OpenOverMouseScreenPosition();
    }

    private void OnMenuClosed()
    {
        if (_choiceMade)
            return;

        if (!EntMan.EntityExists(Owner) || !EntMan.TryGetComponent<MetaDataComponent>(Owner, out _))
            return;

        SendMessage(new VampireClassClosedBuiMsg());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_menu != null)
                _menu.OnClose -= OnMenuClosed;
            _menu = null;
        }

        base.Dispose(disposing);
    }

    private IEnumerable<RadialMenuActionOption<VampireClassType>> CreateClassButtons() => 
        new List<RadialMenuActionOption<VampireClassType>>
        {
            new(HandleClassChoice, VampireClassType.Hemomancer)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(new SpriteSpecifier.Rsi(new ResPath("_Starlight/Vampire/Haemomancer.rsi"), "claws")),
                ToolTip = Loc.GetString("vampire-class-hemomancer-tooltip")
            },
            new(HandleClassChoice, VampireClassType.Umbrae)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(new SpriteSpecifier.Rsi(new ResPath("_Starlight/Vampire/Umbrae.rsi"), "cloak_of_darkness")),
                ToolTip = Loc.GetString("vampire-class-umbrae-tooltip")
            },
            new(HandleClassChoice, VampireClassType.Gargantua)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(new SpriteSpecifier.Rsi(new ResPath("_Starlight/Vampire/Gargantua.rsi"), "blood_swell")),
                ToolTip = Loc.GetString("vampire-class-gargantua-tooltip")
            },
            new(HandleClassChoice, VampireClassType.Dantalion)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(new SpriteSpecifier.Rsi(new ResPath("_Starlight/Vampire/Dantalion.rsi"), "enthrall")),
                ToolTip = Loc.GetString("vampire-class-dantalion-tooltip")
            }
        };

    private void HandleClassChoice(VampireClassType classType)
    {
        _choiceMade = true;
        SendPredictedMessage(new VampireClassChosenBuiMsg { Choice = classType });
        Close();
    }
}
