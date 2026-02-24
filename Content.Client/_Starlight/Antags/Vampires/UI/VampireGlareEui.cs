using Content.Client.Eui;

namespace Content.Client._Starlight.Antags.Vampires.UI;

public sealed class VampireGlareEui : BaseEui
{
    private readonly VampireGlareMenu _menu;

    public VampireGlareEui()
    {
        _menu = new VampireGlareMenu();
    }

    public override void Opened()
    {
        _menu.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        _menu.Close();
    }
}
