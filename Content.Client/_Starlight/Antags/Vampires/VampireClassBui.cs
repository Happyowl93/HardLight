using Content.Client.UserInterface.Controls;
using Content.Shared._Starlight.Antags.Vampires;
using JetBrains.Annotations;
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

        _menu = new SimpleRadialMenu();
        _menu.Track(Owner);
        _choiceMade = false;

        var buttonModels = CreateClassButtons();
        _menu.SetButtons(buttonModels);

        _menu.OpenOverMouseScreenPosition();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!_choiceMade)
            {
                var entMan = IoCManager.Resolve<IEntityManager>();
                var playerMgr = IoCManager.Resolve<Robust.Client.Player.IPlayerManager>();
                var local = playerMgr.LocalSession;
                var stillAttached = local != null && local.AttachedEntity == Owner;

                if (stillAttached && entMan.EntityExists(Owner) && entMan.TryGetComponent<MetaDataComponent>(Owner, out _))
                {
                    SendMessage(new VampireClassClosedBuiMsg());
                }
            }

            _menu?.Close();
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
